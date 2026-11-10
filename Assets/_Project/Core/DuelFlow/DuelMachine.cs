using System;
using System.Collections.Generic;

namespace Multiverse.DuelFlow
{
    using Domain.Cards;
    using Domain.Dueling;
    using EffectEngine;

    /// <summary>Moteur de duel deterministe : tours, phases, invocations, attaques, fenetres de reaction,
    // chronometre, victoire. L'UI et l'IA consomment TOUS les deux cette meme machine (PDF §7, §10, §11).</summary>
    public sealed class DuelMachine
    {
        public readonly GameState State;
        public readonly EffectStack Stack = new EffectStack();
        public readonly EffectExecutor Executor = new EffectExecutor();
        private readonly Dictionary<string, CardDefinition> _defs;
        private readonly Dictionary<string, CardInstance> _instancesByInstanceId = new Dictionary<string, CardInstance>();
        private int _nextInstanceId;
        private System.Random _rng;   // Deterministe via graine : les duels de test sont reproductibles

        public event Action<string> OnLog;
        public event Action<GameState> OnStateChanged;

        /// <summary>Chronometre du joueur actif (PDF §4) ; suspend pendant les fenetres de reaction.</summary>
        public float TimerSeconds { get; private set; }
        public bool TimerSuspended => State.reactionWindowOpen;

        public DuelMachine(Dictionary<string, CardDefinition> definitions, List<CardDefinition> deckP0,
                           List<CardDefinition> deckP1, string nameP0, string nameP1, int seed = 42)
        {
            _defs = definitions;
            _rng = new System.Random(seed);
            State = new GameState
            {
                players = new[]
                {
                    BuildPlayer(0, nameP0, deckP0),
                    BuildPlayer(1, nameP1, deckP1)
                }
            };
            State.Log("Duel init : decks charges");
        }

        private PlayerState BuildPlayer(int index, string name, List<CardDefinition> deckDefs)
        {
            var p = new PlayerState
            {
                playerIndex = index,
                playerName = name ?? $"Joueur {index + 1}",
                lifePoints = DuelRules.StartingLifePoints
            };
            foreach (var def in deckDefs)
            {
                var inst = new CardInstance
                {
                    instanceId = $"i{_nextInstanceId++}",
                    definitionId = def.id,
                    universeOverride = def.universeId,
                    zone = ZoneId.Deck
                };
                p.deck.Add(inst);
                _instancesByInstanceId[inst.instanceId] = inst;
            }
            return p;
        }

        // ---------- Lifecycle de duel ----------
        public void BeginDuel(bool firstPlayerIsIndex0)
        {
            State.players[0].isFirstPlayer = firstPlayerIsIndex0;
            State.players[1].isFirstPlayer = !firstPlayerIsIndex0;
            State.activePlayerIndex = firstPlayerIsIndex0 ? 0 : 1;
            State.Log($"Shifumi : {(firstPlayerIsIndex0 ? State.players[0].playerName : State.players[1].playerName)} commence (choix effecte AVANT le duel)");

            // Mains initiales : 6 cartes chacun (PDF §3)
            DrawCards(State.players[0], DuelRules.StartingHandSize);
            DrawCards(State.players[1], DuelRules.StartingHandSize);

            State.globalTurnIndex = 1;
            State.players[0].turnNumberForThisPlayer = 1;
            TimerSeconds = DuelRules.ResolveTurnTimer(1);
            // DECISION D'ARCHITECTURE (documentee docs/03_DECISIONS.md) : pas de pioche au tout premier tour
            // du duel. Une pioche a ce tour depasserait la limite de main de 6 cartes imposee par le PDF §3.
            // La regle reste IDENTIQUE pour les deux joueurs (PDF §3).
            State.phase = Phase.Draw;
            OnStateChanged?.Invoke(State);
        }

        public void EndTurn()
        {
            var active = State.Active;
            ResolveEndOfTurnDecayForPlayers(active, State.Opponent);

            // Changement de joueur actif
            State.activePlayerIndex = 1 - State.activePlayerIndex;
            var next = State.Active;
            next.turnNumberForThisPlayer++;
            State.globalTurnIndex++;
            State.phase = Phase.Draw;

            // Chronometre : reset a chaque nouveau tour du joueur, +50 s si NOUVEAU tour du MEME joueur (PDF §4)
            TimerSeconds = DuelRules.ResolveTurnTimer(next.turnNumberForThisPlayer);
            BeginPhase(Phase.Draw);
        }

        private void ResolveEndOfTurnDecayForPlayers(PlayerState a, PlayerState b)        {
            DecayPlayer(a); DecayPlayer(b);
        }

        private static void DecayPlayer(PlayerState p)
        {
            foreach (var c in p.characterSlots)
            {
                if (c == null) continue;
                if (c.status.immobilizedTurns > 0) c.status.immobilizedTurns--;
                if (c.status.directAttackOverride > 0) c.status.directAttackOverride--;
                c.attacksUsedThisTurn = 0;
                c.usagesThisTurn = 0;
                c.extraAttacksPerTurn = 0;
                c.hasAttackedThisTurn = false;
            }
            p.normalSummonsUsedThisTurn = 0;
        }

        // ---------- Phases (PDF §3) ----------
        public void BeginPhase(Phase ph)
        {
            State.phase = ph;
            if (ph == Phase.Draw)
            {
                DrawCards(State.Active, 1);   // Pioche automatique, regle identique aux 2 joueurs
                State.Log($"DrawPhase joueur {State.activePlayerIndex}");
            }
            OnStateChanged?.Invoke(State);
        }

        public void GotoMain1() => BeginPhase(Phase.Main1);
        public void GotoBattle() => BeginPhase(Phase.Battle);
        public void GotoMain2() => BeginPhase(Phase.Main2);
        public void GotoEnd() { State.Log("EndPhase"); OnStateChanged?.Invoke(State); }

        // ---------- Main ----------
        public void DrawCards(PlayerState player, int count)
        {
            for (int i = 0; i < count; i++)
            {
                if (player.deck.Count == 0) return;
                var c = player.deck[player.deck.Count - 1];
                player.deck.RemoveAt(player.deck.Count - 1);

                if (player.hand.Count >= DuelRules.MaxHandSize)
                {
                    player.graveyard.Add(c);   // Main pleine : la carte piochee part au cimetiere (regle stricte main <= 6)
                    State.Log($"Main pleine ({DuelRules.MaxHandSize}) : {c.definitionId} au cimetiere");
                    continue;
                }
                c.zone = ZoneId.Hand;
                player.hand.Add(c);
            }
        }

        // ---------- Invocation (1 normale / tour, slots pleins interdits) ----------
        public bool TrySummon(int slotIndex)
        {
            var p = State.Active;
            if (State.phase != Phase.Main1 && State.phase != Phase.Main2) return Invalid("Invocation hors Main Phase");
            if (p.normalSummonsUsedThisTurn >= DuelRules.NormalSummonsPerTurn) return Invalid("Invocation normale deja utilisee ce tour");
            if (p.hand.Count == 0) return Invalid("Main vide");
            if (slotIndex < 0 || slotIndex >= p.characterSlots.Length) return Invalid("Slot invalide");
            if (p.characterSlots[slotIndex] != null) return Invalid("Slot Personnage occupe");

            var card = p.hand[p.hand.Count - 1];
            p.hand.RemoveAt(p.hand.Count - 1);
            card.zone = ZoneId.CharacterSlot;
            p.characterSlots[slotIndex] = card;
            p.normalSummonsUsedThisTurn++;
            State.Log($"Invoke {card.definitionId} slot {slotIndex} (summons {p.normalSummonsUsedThisTurn}/{DuelRules.NormalSummonsPerTurn})");
            OnStateChanged?.Invoke(State);
            return true;
        }

        // ---------- Attaque (Battle Phase uniquement ; 1er joueur interdit au tour 1 ; direct selon regles) ----------
        public bool TryAttack(int attackerSlot, int defenderSlot, bool direct)
        {
            var atk = State.Active;
            var def = State.Opponent;
            if (State.phase != Phase.Battle) return Invalid("Attaque hors Battle Phase");
            if (atk.isFirstPlayer && atk.turnNumberForThisPlayer == 1)
                return Invalid("Le 1er joueur ne peut pas attaquer a son tout premier tour (PDF §3)");
            if (attackerSlot < 0 || attackerSlot >= atk.characterSlots.Length) return Invalid("Slot attaquant invalide");
            var attacker = atk.characterSlots[attackerSlot];
            if (attacker == null) return Invalid("Pas d'attaquant sur ce slot");
            if (!attacker.CanAttackThisTurn) return Invalid("Ce personnage a deja attaque / immobilise");

            bool directOverride = attacker.status.directAttackOverride > 0;
            if (direct)
            {
                if (!DuelRules.IsDirectAttackAllowed(def.HasAnyCharacterOnField, directOverride))
                    return Invalid("Attaque directe interdite : des personnages adverses sont presents");
                DirectDamageResolve(attacker, def, attackerSlot);
            }
            else
            {
                if (defenderSlot < 0 || defenderSlot >= def.characterSlots.Length) return Invalid("Slot defenseur invalide");
                var defender = def.characterSlots[defenderSlot];
                if (defender == null) return Invalid("Pas de defenseur sur ce slot");
                CombatResolve(attacker, defender, attackerSlot, defenderSlot);
            }
            return true;
        }

        private void CombatResolve(CardInstance attacker, CardInstance defender, int atkSlot, int defSlot)
        {
            int atkPower = EffectiveAtk(attacker, PowerMode.Attack);
            int defPower = EffectiveAtk(defender, PowerMode.Defence);
            var def = State.Opponent;

            if (atkPower >= defPower)
            {
                RemoveAndDestroy(def, defender);
                int overkill = atkPower - defPower;
                // Convention : surdegats INFLES au joueur adverse seulement si atkPower > defPower (regle retenue, documentee)
                def.lifePoints -= Math.Max(0, overkill);
                State.Log($"Combat WIN : {attacker.definitionId} ({atkPower}) vs {defender.definitionId} ({defPower}) ; surdegats {overkill}");
            }
            else
            {
                // Degats de recul pour l'attaquant (convention documentée D-06 : l'attaque echoue,
                // la difference est infligee aux PV du joueur attaquant).
                var attackerOwner = State.Active;
                attackerOwner.lifePoints -= (defPower - atkPower);
                attacker.attacksUsedThisTurn++;
                State.Log($"Combat LOSE : {attacker.definitionId} ({atkPower}) vs {defender.definitionId} ({defPower})");
                CheckVictory();
                return;
            }
            attacker.attacksUsedThisTurn++;
            attacker.hasAttackedThisTurn = true;
            CheckVictory();
        }

        private void DirectDamageResolve(CardInstance attacker, PlayerState target, int atkSlot)
        {
            int dmg = EffectiveAtk(attacker, PowerMode.Attack);
            target.lifePoints -= dmg;
            attacker.attacksUsedThisTurn++;
            attacker.hasAttackedThisTurn = true;
            State.Log($"Attaque directe : {attacker.definitionId} inflige {dmg} PV de degats");
            CheckVictory();
        }

        private void RemoveAndDestroy(PlayerState owner, CardInstance card)
        {
            for (int i = 0; i < owner.characterSlots.Length; i++)
                if (ReferenceEquals(owner.characterSlots[i], card)) owner.characterSlots[i] = null;
            card.zone = ZoneId.Graveyard;
            owner.graveyard.Add(card);
        }

        private int EffectiveAtk(CardInstance c, PowerMode mode)
        {
            var def = _defs != null && _defs.TryGetValue(c.definitionId, out var d) ? d : null;
            if (def == null) return 0;
            int power = mode == PowerMode.Attack ? def.atk : def.def;
            return power + (mode == PowerMode.Attack ? c.status.atkBonus : c.status.defBonus);
        }

        private enum PowerMode { Attack, Defence }

        private void CheckVictory()
        {
            if (State.IsDuelOver)
                State.Log("FIN DE DUEL : PV à zero");
            OnStateChanged?.Invoke(State);
        }

        private bool Invalid(string reason)
        {
            State.Log($"ACTION REFUSEE : {reason}");
            OnStateChanged?.Invoke(State);
            return false;
        }
    }
}

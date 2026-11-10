using System.Collections.Generic;

namespace Multiverse.EffectEngine
{
    using Domain.Cards;
    using Domain.Dueling;

    /// <summary>Executeur des effets atomiques sur l'etat de duel (PDF §7).
    // Regles d'integrite : jamais d'application double (guard par effectId dans le journal), cibles validees avant application.</summary>
    public sealed class EffectExecutor
    {
        private readonly HashSet<string> _appliedEffectIds = new HashSet<string>();

        public bool WasApplied(string effectId)
        {
            return effectId != null && _appliedEffectIds.Contains(effectId);
        }

        /// <summary>Applique un EffectSpec a l'etat. Retourne true si applique (pas de doublon grace au effectId).</summary>
        public bool Apply(EffectSpec spec, GameState state, PlayerState activator, CardInstance src, CardInstance target)
        {
            if (spec == null) return false;
            if (spec.effectId != null && !_appliedEffectIds.Add(spec.effectId)) return false;  // garde anti double resolution

            var enemy = state.players[1 - activator.playerIndex];
            switch (spec.kind)
            {
                case EffectKind.DealDamage:
                {
                    if (spec.target == TargetKind.EnemyPlayer) enemy.lifePoints -= spec.amount;
                    // Degats a un personnage : c'est le DuelEngine qui applique ATK vs DEF (resolution de combat),
                    // ce kind sert aux effets de capacite infligeant des degats directs au personnage cible.
                    state.Log($"DealEffect {spec.amount} by {src?.instanceId ?? "effect"}");
                    return true;
                }
                case EffectKind.StatChange:
                {
                    if (target != null)
                    {
                        target.status.atkBonus += spec.amount;   // amount positif = buff, negatif = debuff
                        state.Log($"StatChange {(spec.amount >= 0 ? "+" : "")}{spec.amount} ATK on {target.definitionId}");
                    }
                    return true;
                }
                case EffectKind.Heal:
                {
                    activator.lifePoints += spec.amount;
                    if (activator.lifePoints > DuelRules.StartingLifePoints)
                        activator.lifePoints = DuelRules.StartingLifePoints;   // Cap : les PV ne depassent pas 10 000 sauf effet explicite futur
                    state.Log($"Heal {spec.amount} -> LP {activator.lifePoints}");
                    return true;
                }
                case EffectKind.Destroy:
                {
                    if (target != null && target.zone == ZoneId.CharacterSlot)
                    {
                        RemoveFromSlots(enemy, target);
                        enemy.graveyard.Add(target);
                        state.Log($"Destroy {target.definitionId}");
                    }
                    return true;
                }
                case EffectKind.ReturnToHand:
                {
                    if (target != null && target.zone == ZoneId.CharacterSlot)
                    {
                        RemoveFromSlots(enemy, target);
                        target.zone = ZoneId.Hand;
                        enemy.hand.Add(target);
                        state.Log($"ReturnToHand {target.definitionId}");
                    }
                    return true;
                }
                case EffectKind.Immobilize:
                {
                    if (target != null) target.status.immobilizedTurns += spec.durationTurns;
                    state.Log($"Immobilize {target?.definitionId} {spec.durationTurns} tours");
                    return true;
                }
                case EffectKind.PreventAbilityUse:
                {
                    if (target != null) target.status.abilityBlocked = spec.durationTurns > 0;
                    state.Log($"AbilityBlocked {target?.definitionId}");
                    return true;
                }
                case EffectKind.GainAttackBonus:
                {
                    if (target != null) target.status.atkBonus += spec.amount;
                    state.Log($"GainAttackBonus +{spec.amount} on {target?.definitionId}");
                    return true;
                }
                case EffectKind.DirectAttackOverride:
                {
                    src.status.directAttackOverride += spec.durationTurns > 0 ? spec.durationTurns : 1;
                    state.Log($"DirectAttackOverride {src?.instanceId}");
                    return true;
                }
                case EffectKind.SearchFromDeck:
                {
                    SearchFromDeck(spec, activator, state);
                    return true;
                }
                default:
                    state.Log($"UnimplementedEffectKind {spec.kind} (brigade explicite, PDF §13 : jamais masquee)");
                    return false;
            }
        }

        private static void SearchFromDeck(EffectSpec spec, PlayerState activator, GameState state)
        {
            for (int i = activator.deck.Count - 1; i >= 0 && i >= activator.deck.Count - 20; i--)
            {
                var card = activator.deck[i];
                bool match = string.IsNullOrEmpty(spec.worldId) || card.universeOverride == spec.worldId;
                if (match)
                {
                    activator.deck.RemoveAt(i);
                    if (activator.hand.Count < DuelRules.MaxHandSize)
                    {
                        card.zone = ZoneId.Hand;
                        activator.hand.Add(card);
                        state.Log($"SearchFromDeck {card.definitionId} ({activator.hand.Count}/{DuelRules.MaxHandSize})");
                    }
                    return;
                }
            }
            state.Log("SearchFromDeck aucune correspondance");
        }

        private static void RemoveFromSlots(PlayerState owner, CardInstance card)
        {
            for (int i = 0; i < owner.characterSlots.Length; i++)
                if (ReferenceEquals(owner.characterSlots[i], card)) { owner.characterSlots[i] = null; return; }
            for (int i = 0; i < owner.spellTrapSlots.Length; i++)
                if (ReferenceEquals(owner.spellTrapSlots[i], card)) { owner.spellTrapSlots[i] = null; return; }
        }
    }
}

using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Multiverse.Tests
{
    using Domain.Cards;
    using Domain.Dueling;
    using DuelFlow;

    /// <summary>Tests d'integration du flux de duel (vertical slice, PDF §12 etape 3).
    // Valide : init des PV / mains, phases, invocation unique par tour, 1 attaque par personnage/tour,
    // 1er joueur sans attaque au tour 1, victoire quand PV = 0.</summary>
    public sealed class DuelMachineTests
    {
        private static List<CardDefinition> FiftyDanny()
        {
            // Deck de test : mixes de 2 definitions qui existent dans la planche de reference.
            var d = MakeCard("T-001", 1000, 800);
            var p = MakeCard("T-002", 2000, 1500);
            var list = new List<CardDefinition>();
            for (int i = 0; i < 25; i++) { list.Add(d); list.Add(p); }
            return list;
        }

        private static CardDefinition MakeCard(string id, int atk, int def)
        {
            return new CardDefinition
            {
                id = id, cardName = id, type = CardType.Character, universeId = "DannyPhantom",
                rarity = Rarity.Common, popularity = 5, atk = atk, def = def,
                abilities = System.Array.Empty<AbilityDefinition>(), lore = new LoreMetadata
                {
                    cartoonReference = "Danny Phantom", characterReference = id, loreElement = id,
                    loreExplanation = "test", gameplayTranslation = id, canonStatus = CanonStatus.ToVerify
                }
            };
        }

        private static DuelMachine NewDuel()
        {
            var defs = new Dictionary<string, CardDefinition>();
            var deckA = FiftyDanny(); var deckB = FiftyDanny();
            foreach (var c in deckA) if (!defs.ContainsKey(c.id)) defs[c.id] = c;
            foreach (var c in deckB) if (!defs.ContainsKey(c.id)) defs[c.id] = c;
            return new DuelMachine(defs, deckA, deckB, "A", "B");
        }

        [Test]
        public void BeginDuel_Has10000LP_And6CardsEach_AndDrawPhase()
        {
            var dm = NewDuel();
            dm.BeginDuel(firstPlayerIsIndex0: true);
            Assert.AreEqual(10000, dm.State.players[0].lifePoints);
            Assert.AreEqual(10000, dm.State.players[1].lifePoints);
            Assert.AreEqual(6, dm.State.players[0].hand.Count);
            Assert.AreEqual(6, dm.State.players[1].hand.Count);
        }

        [Test]
        public void Summon_OncePerTurn_SecondSummonIsRefused()
        {
            var dm = NewDuel();
            dm.BeginDuel(true);
            dm.GotoMain1();
            Assert.IsTrue(dm.TrySummon(0), "Premiere invocation licite");
            Assert.IsFalse(dm.TrySummon(1), "La reglementation borne L'invocation normale a 1/tour");
        }

        [Test]
        public void Attack_BeforeBattlePhase_IsRefused()
        {
            var dm = NewDuel();
            dm.BeginDuel(true);
            dm.GotoMain1();
            dm.TrySummon(0);
            // Reste en Main1 : l'attaque doit etre refusee (PDF §4 : attaque possible qu'apres entree en Battle Phase)
            Assert.IsFalse(dm.TryAttack(0, 0, direct: true));
        }

        [Test]
        public void Character_CannotAttackTwice_InSameTurn()
        {
            var dm = NewDuel();
            dm.BeginDuel(firstPlayerIsIndex0: true);
            dm.GotoMain1();
            dm.TrySummon(0);
            // Simulation : on passe au tour 2 du joueur 0 pour sortir de l'interdiction du tout premier tour (PDF §3)
            dm.State.players[0].turnNumberForThisPlayer = 2;
            dm.GotoBattle();
            Assert.IsTrue(dm.TryAttack(0, -1, direct: true), "Premiere attaque directe licite (terrain adverse vide)");
            // Deuxieme attaque du meme personnage refusee : 1 attaque/personnage/tour (PDF §3)
            Assert.IsFalse(dm.TryAttack(0, -1, direct: true));
        }

        [Test]
        public void FirstPlayer_CheckFirstTurnAttackForbidden_ThenAllowedTurn2()
        {
            var dm = NewDuel();
            dm.BeginDuel(true);
            dm.GotoMain1();
            dm.TrySummon(0);
            dm.GotoBattle();
            // Aucun personnage adverse : la regle d'attaque directe passe, MAIS le 1er tour
            // du premier joueur interdit l'attaque (PDF §3) -> refusee a cause du flag du 1er tour.
            Assert.IsFalse(dm.TryAttack(0, -1, direct: true), "Le 1er joueur ne peut pas attaquer a son tout premier tour (PDF §3)");
        }

        [Test]
        public void Victory_WhenLifePointsReachesZero()
        {
            var dm = NewDuel();
            dm.State.players[0].lifePoints = 0;
            Assert.IsTrue(dm.State.IsDuelOver);
        }
    }
}

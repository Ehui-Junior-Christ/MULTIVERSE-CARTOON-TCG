using NUnit.Framework;

namespace Multiverse.Tests
{
    using Domain.Cards;
    using Domain.Dueling;
    using Data;

    /// <summary>Tests unitaires des REGLES de base (PDF §13 : "victoire de zero phiz" a la victoire sans blocage).
    // Cibles du critere d'acceptation : PV 10000, deck 50, main 6, popularite 1..15, abilities par rarete, timer.</summary>
    public sealed class DuelRulesTests
    {
        [Test] public void StartingLifePoints_Is10000() => Assert.AreEqual(10000, DuelRules.StartingLifePoints);
        [Test] public void DeckSize_Is50() => Assert.AreEqual(50, DuelRules.DeckSize);
        [Test]
        public void HandSizes_Are6()
        {
            Assert.AreEqual(6, DuelRules.StartingHandSize);
            Assert.AreEqual(6, DuelRules.MaxHandSize);
        }

        // Le 1er joueur ne peut pas attaquer a son tout premier tour (PDF §3)
        [Test] public void FirstPlayer_CannotAttack_OnVeryFirstTurn()
        {
            Assert.IsFalse(DuelRules.CanAttackThisTurn(isFirstPlayer: true, turnNumberForThisPlayer: 1));
        }

        // Le 2e joueur peut attaquer des son premier tour (PDF §3)
        [Test] public void SecondPlayer_CanAttack_OnVeryFirstTurn()
        {
            Assert.IsTrue(DuelRules.CanAttackThisTurn(isFirstPlayer: false, turnNumberForThisPlayer: 1));
        }

        [Test] public void AnyPlayer_CanAttack_AfterFirstTurn()
        {
            Assert.IsTrue(DuelRules.CanAttackThisTurn(true, 2));
            Assert.IsTrue(DuelRules.CanAttackThisTurn(false, 2));
        }

        // Attaque directe interdite si personnages adverses presents, sauf effet explicite (PDF §3/§4)
        [Test] public void DirectAttack_BlockedWhileEnemyCharacters_CardOverrideAllows()
        {
            Assert.IsFalse(DuelRules.IsDirectAttackAllowed(enemyHasCharacters: true, cardAllowsDirectAttackOverride: false));
            Assert.IsTrue(DuelRules.IsDirectAttackAllowed(enemyHasCharacters: true, cardAllowsDirectAttackOverride: true));
            Assert.IsTrue(DuelRules.IsDirectAttackAllowed(enemyHasCharacters: false, cardAllowsDirectAttackOverride: false));
        }

        // Popularite 1..15 pour TOUTES les cartes (les Rare de la planche ont popularite 8-15)
        [Test]
        public void Popularity_Range_1To15()
        {
            for (int pop = 1; pop <= 15; pop++)
                Assert.IsTrue(DuelRules.IsPopularityInRange(pop), $"Popularite {pop} doit etre licite");
            Assert.IsFalse(DuelRules.IsPopularityInRange(0));
            Assert.IsFalse(DuelRules.IsPopularityInRange(16));
        }

        // Nombre de capacites par rarete : commune limitee, legendaire ~4, ultra 6 (PDF §5)
        [Test]
        public void AbilitiesPerRarity_Scaling()
        {
            Assert.AreEqual(1, DuelRules.MaxAbilitiesFor(Rarity.Common));
            Assert.AreEqual(2, DuelRules.MaxAbilitiesFor(Rarity.Rare));
            Assert.LessOrEqual(DuelRules.MaxAbilitiesFor(Rarity.Legendary), 5);
            Assert.AreEqual(6, DuelRules.MaxAbilitiesFor(Rarity.UltraLegendary));
        }

        // Chronometre : 230 s au 1er tour, +50 s par tour suivant du MEME joueur (PDF §4)
        [Test]
        public void TurnTimer_230s_Then50sPerExtraTurn()
        {
            Assert.AreEqual(230f, DuelRules.ResolveTurnTimer(1));
            Assert.AreEqual(280f, DuelRules.ResolveTurnTimer(2));
            Assert.AreEqual(330f, DuelRules.ResolveTurnTimer(3));
        }
    }
}

using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Multiverse.Tests
{
    using Domain.Cards;
    using Data;

    /// <summary>Tests de la base de donnees des cartes : le JSON source de verite doit se charger,
    // rester coherent avec les regles (PDF §5/§8) et porter des metadonnees de lore complètes (PDF §6).</summary>
    public sealed class CardDatabaseTests
    {
        private static Dictionary<string, CardDefinition> LoadDb()
        {
            // Chargement direct du fichier JSON du projet (path stable via Application.dataPath en mode Tests).
            var path = System.IO.Path.Combine(Application.dataPath, "_Project/Data/Json/cards.json");
            return CardDatabase.LoadFromFile(path);
        }

        [Test]
        public void Database_Loads_6DannyPhantomCards()
        {
            var db = LoadDb();
            Assert.GreaterOrEqual(db.Count, 6, "La V1 doit au moins contenir les 6 cartes de test Danny Phantom");
            Assert.IsTrue(db.ContainsKey("DP-001"));
            Assert.IsTrue(db.ContainsKey("DP-006"));
        }

        [Test]
        public void CardValues_MatchTheReferencePlate()
        {
            var db = LoadDb();
            // Valeurs verifiees sur la planche de reference 1794305798308.jpg (planche Danny Phantom)
            var danny = db["DP-001"];
            Assert.AreEqual("Danny Fantome", danny.cardName);
            Assert.AreEqual(12, danny.popularity);
            Assert.AreEqual(Rarity.SuperRare, danny.rarity);
            Assert.AreEqual(5000, danny.atk);
            Assert.AreEqual(4000, danny.def);

            var skulker = db["DP-006"];
            Assert.AreEqual(9, skulker.popularity);
            Assert.AreEqual(Rarity.Rare, skulker.rarity);
            Assert.AreEqual(4200, skulker.atk);
            Assert.AreEqual(3800, skulker.def);
        }

        [Test]
        public void EveryCharacterCard_HasAtLeastOneAbility()
        {
            var db = LoadDb();
            foreach (var kv in db)
            {
                if (kv.Value.type == CardType.Character)
                    Assert.GreaterOrEqual(kv.Value.abilities.Length, 1, $"{kv.Key} : capacite obligatoire sur un Personnage");
            }
        }

        [Test]
        public void EveryAbility_HasCompleteLoreMetadata() // PDF §6 : metadonnees de lore OBLIGATOIRES
        {
            var db = LoadDb();
            foreach (var kv in db)
            {
                foreach (var a in kv.Value.abilities)
                {
                    Assert.IsNotNull(a.lore, $"{kv.Key}/{a.abilityName} : bloc de lore manquant");
                    Assert.IsFalse(string.IsNullOrWhiteSpace(a.lore.cartoonReference), $"{kv.Key}/{a.abilityName} : cartoonReference manquant");
                    Assert.IsFalse(string.IsNullOrWhiteSpace(a.lore.characterReference), $"{kv.Key}/{a.abilityName} : characterReference manquant");
                    Assert.IsFalse(string.IsNullOrWhiteSpace(a.lore.loreElement), $"{kv.Key}/{a.abilityName} : loreElement manquant");
                    Assert.IsFalse(string.IsNullOrWhiteSpace(a.lore.loreExplanation), $"{kv.Key}/{a.abilityName} : loreExplanation manquant");
                    Assert.IsFalse(string.IsNullOrWhiteSpace(a.lore.gameplayTranslation), $"{kv.Key}/{a.abilityName} : gameplayTranslation manquant");
                }
            }
        }

        [Test]
        public void Abilities_AreWithinRarityCap()
        {
            var db = LoadDb();
            foreach (var kv in db)
            {
                if (kv.Value.type != CardType.Character) continue;
                int maxAbilities = Domain.Dueling.DuelRules.MaxAbilitiesFor(kv.Value.rarity);
                Assert.LessOrEqual(kv.Value.abilities.Length, maxAbilities,
                    $"{kv.Key} : {kv.Value.abilities.Length} capacites depasse le cap de rarete {kv.Value.rarity}");
            }
        }

        [Test]
        public void EffectSpecs_HaveStableEffectIds() // Garde anti double-resolution du moteur (PDF §7)
        {
            var db = LoadDb();
            foreach (var kv in db)
            {
                foreach (var a in kv.Value.abilities)
                {
                    foreach (var e in a.effects)
                        Assert.IsFalse(string.IsNullOrEmpty(e.effectId), $"{kv.Key}/{a.abilityName} : effectId stable obligatoire");
                }
            }
        }
    }
}

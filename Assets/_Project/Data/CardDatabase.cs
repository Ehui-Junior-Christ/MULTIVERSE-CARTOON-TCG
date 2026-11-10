using System;
using System.Collections.Generic;
using System.IO;

namespace Multiverse.Data
{
    using Domain.Cards;

    /// <summary>Base de duree des cartes : charge le JSON source de verite (PDF §11 Card Database).
    // Le JSON est unique ; l'editeur ecrit, le runtime lit. Jamais de carte codee en dur dans l'UI.</summary>
    public static class CardDatabase
    {
        public static Dictionary<string, CardDefinition> LoadFromJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                throw new ArgumentException("JSON de cartes vide : la base de donnees doit exister (PDF §8).");

            var wrapper = JsonUtilityT.FromJson<CardDatabaseFile>(json);
            if (wrapper == null || wrapper.cards == null || wrapper.cards.Length == 0)
                throw new ArgumentException("Aucune carte dans le JSON.");

            var map = new Dictionary<string, CardDefinition>();
            foreach (var c in wrapper.cards)
            {
                if (string.IsNullOrEmpty(c.id)) throw new ArgumentException("Carte sans id.");
                if (map.ContainsKey(c.id)) throw new ArgumentException($"Id duplique : {c.id}");
                map[c.id] = c;
            }
            return map;
        }

        public static Dictionary<string, CardDefinition> LoadFromFile(string path)
        {
            return LoadFromJson(File.ReadAllText(path));
        }

        [Serializable]
        public sealed class CardDatabaseFile
        {
            public string schemaVersion = "1";
            public CardDefinition[] cards = Array.Empty<CardDefinition>();
        }

        // Pas de dependance Newtonsoft en V1 : JsonUtility standard Unity (gestion simple, pas de polymorphisme requis).
        private static class JsonUtilityT
        {
            public static T FromJson<T>(string json) where T : class
            {
                return UnityEngine.JsonUtility.FromJson<T>(json);
            }
        }
    }
}

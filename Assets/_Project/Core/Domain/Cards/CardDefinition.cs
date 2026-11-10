using System;

namespace Multiverse.Domain.Cards
{
    /// <summary>Definition d'une carte : source de verite JSON chargee en editeur/runtime (PDF §11 Card Database).
    // L'artwork reference est TEL QUEL — jamais regenere (rèf. planche Danny Phantom / Star Butterfly UR).</summary>
    [Serializable]
    public sealed class CardDefinition
    {
        public string id;                    // "DP-001" (univers - prefixe + numero)
        public string cardName;
        public string subtitle;
        public CardType type;
        public string subType;               // "Fantome", "Tech", "Nature"...
        public string universeId;            // "DannyPhantom"...
        public Rarity rarity;
        public int popularity;               // 1..7 normales, 8..15 speciales (PDF §5)
        public int atk;
        public int def;
        public string flavorText;
        public AbilityDefinition[] abilities = new AbilityDefinition[0];
        public string artworkRef;            // Chemin de l'illustration FOURNIE (placeholder documente si absente)
        public LoreMetadata lore;
    }
}

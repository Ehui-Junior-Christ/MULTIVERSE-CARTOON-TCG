namespace Multiverse.Domain.Dueling
{
    using Cards;

    /// <summary>Zones du plateau (PDF §3 : 5 slots Personnage, 4 Magie/Piege ordinaires + 1 Magie de Terrain).</summary>
    public enum ZoneId { Hand = 0, Deck = 1, CharacterSlot = 2, SpellTrapSlot = 3, FieldSpellSlot = 4, Graveyard = 5, Limbo = 6 }

    /// <summary>Modificateurs temporaires d'un personnage sur le terrain (buff/debuff, gel...).</summary>
    [System.Serializable]
    public sealed class CardStatus
    {
        public int atkBonus;
        public int defBonus;
        public int immobilizedTurns;      // Ex. "Cauchemar Vivant" : immobilisation 3 tours
        public bool abilityBlocked;       // Ex. "Piratage de Reseau" : capacites niees un tour
        public int directAttackOverride;  // Attaque directe autorisee N tours malgre la presence de personnages

        public CardStatus Clone() => new CardStatus
        {
            atkBonus = atkBonus, defBonus = defBonus, immobilizedTurns = immobilizedTurns,
            abilityBlocked = abilityBlocked, directAttackOverride = directAttackOverride
        };
    }

    /// <summary>Carte instance en jeu : une definition + un etat propre (moteur deterministe, jamais de double resolution).
    // Regle PDF §3 : 1 attaque par personnage/tour sauf effet explicite (attacksPerTurnExtra > 0).</summary>
    [System.Serializable]
    public sealed class CardInstance
    {
        public string instanceId;         // Unique par duel
        public string definitionId;       // Reference a CardDefinition.id
        public string universeOverride;   // Universe cache a l'invocation (la definition peut etre absente en runtime)
        public ZoneId zone = ZoneId.Deck;
        public CardStatus status = new CardStatus();
        public bool hasAttackedThisTurn;  // conserve pour replay/telemetrie (deprecated logique)
        public int attacksUsedThisTurn;   // Compteur strict d'attaques effectuees ce tour
        public int extraAttacksPerTurn;   // Effet explicite accordant des attaques supplementaires
        public int usagesThisTurn;        // Anti-abus des limites OncePerTurn
        public bool usedOncePerDuel;      // Anti-abus des limites OncePerDuel

        /// <summary>Adversaire du tour : 1 attaque par personnage, sauf effet explicite (extraAttacksPerTurn &gt; 0).</summary>
        public bool CanAttackThisTurn => status.immobilizedTurns <= 0
                                         && attacksUsedThisTurn < (1 + extraAttacksPerTurn);
    }
}

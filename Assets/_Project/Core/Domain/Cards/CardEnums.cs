using System;

namespace Multiverse.Domain.Cards
{
    /// <summary>Types de cartes du jeu. Extensible si de nouveaux types sont necessaires (PDF §5).
    /// Edition/compatibility note: keep in sync with docs/02_ARCHITECTURE_TECHNIQUE.md §3.1.</summary>
    public enum CardType
    {
        Character = 0,
        Spell = 1,
        Trap = 2,
        FieldSpell = 3
    }

    /// <summary>Les 7 raretes prevues par le PDF §5. L'ordre sert aux reseaux de gacha et aux skins de cadre.</summary>
    public enum Rarity
    {
        Common = 0,
        Uncommon = 1,
        Rare = 2,
        SuperRare = 3,
        Epic = 4,
        Legendary = 5,
        UltraLegendary = 6
    }

    /// <summary>Statut de fidelite au lore d'une capacite (PDF §6, metadonnees obligatoires).</summary>
    public enum CanonStatus
    {
        Confirmed = 0,
        GameplayAdaptation = 1,
        ToVerify = 2
    }
}

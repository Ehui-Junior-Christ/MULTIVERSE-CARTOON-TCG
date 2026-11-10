using Multiverse.Domain.Cards;

namespace Multiverse.Domain.Dueling
{
    /// <summary>Regles fondamentales du duel, extraites a la lettre du PDF maître §3/§4.
    // Aucune logique UI ici : classes pures uniquement, testables sans scene Unity.</summary>
    public static class DuelRules
    {
        // --- Constantes reglementaires ---
        public const int StartingLifePoints = 10000;   // Chaque joueur commence avec 10 000 PV
        public const int DeckSize = 50;                // Chaque deck contient exactement 50 cartes
        public const int StartingHandSize = 6;         // Main initiale : 6 cartes
        public const int MaxHandSize = 6;              // Limite maximale de main : 6 cartes
        public const int CharacterSlots = 5;           // Emplacements Personnage
        public const int SpellTrapSlots = 4;           // Emplacements Magie/Piege ordinaires
        public const int FieldSpellSlots = 1;          // Emplacement reserve a la Magie de Terrain
        public const int NormalSummonsPerTurn = 1;     // 1 invocation normale par tour
        public const int PopularityMin = 1;
        public const int PopularityNormalMax = 7;      // Cartes normales : Popularite 1..7
        public const int PopularitySpecialMax = 15;    // Cartes speciales : Popularite 8..15
        public const int MaxAbilitiesOnUltraLegendary = 6;  // PDF §5 : jusqu'a 6 sur ultra-legendarie
        public const int MaxAbilitiesOnLegendary = 4;       // Environ 4 sur legendaire

        // --- Chronometre (PDF §4) ---
        public const float TurnTimerSeconds = 230f;    // 230 s au debut du tour
        public const float ExtraTurnBonusSeconds = 50f;// Ajout de 50 s par nouveau tour du MEME joueur
        public const float InitialTurnSeconds = 230f;  // Premier tour du joueur actif

        /// <summary>Le 1er joueur NE PEUT PAS attaquer a son tout premier tour ; le 2e joueur peut (PDF §3).</summary>
        public static bool CanAttackThisTurn(bool isFirstPlayer, int turnNumberForThisPlayer)
        {
            if (isFirstPlayer && turnNumberForThisPlayer == 1) return false;
            return true;
        }

        /// <summary>Attaque directe autorisee uniquement si le terrain adverse n'a plus aucun personnage, sauf effet explicite (PDF §3/§4).</summary>
        public static bool IsDirectAttackAllowed(bool enemyHasCharacters, bool cardAllowsDirectAttackOverride)
        {
            return !enemyHasCharacters || cardAllowsDirectAttackOverride;
        }

        /// <summary>Popularite licite : 1..15 pour toutes les cartes (PDF §5 : normales 1..7, speciales 8..15 —
        // la classe de rarete n'est PAS liee a la classe, cf. planche de reference : Skulker 9 R, Desiree 8 R).</summary>
        public static bool IsPopularityInRange(int popularity) => popularity >= PopularityMin && popularity <= PopularitySpecialMax;

        /// <summary>Classe de la carte : "normale" (1..7) ou "speciale" (8..15) ; sert aux filtres de collection/gacha.</summary>
        public static bool IsSpecialCard(int popularity) => popularity >= 8;

        /// <summary>Nombres de capacites autorises par rarete (equilibre configurable, PDF §5).</summary>
        public static int MaxAbilitiesFor(Rarity rarity)
        {
            switch (rarity)
            {
                case Rarity.Common: return 1;
                case Rarity.Uncommon: return 2;
                case Rarity.Rare: return 2;
                case Rarity.SuperRare: return 3;
                case Rarity.Epic: return 3;
                case Rarity.Legendary: return MaxAbilitiesOnLegendary;       // ~4
                case Rarity.UltraLegendary: return MaxAbilitiesOnUltraLegendary; // jusqu'a 6
                default: return 1;
            }
        }

        /// <summary>Chronometre : temps de tour au demarrage (PDF §4 : 230 s, +50 s par nouveau tour du meme joueur).</summary>
        public static float ResolveTurnTimer(int turnNumberForThisPlayer)
        {
            return turnNumberForThisPlayer <= 1
                ? TurnTimerSeconds
                : TurnTimerSeconds + ExtraTurnBonusSeconds * (turnNumberForThisPlayer - 1);
        }
    }
}


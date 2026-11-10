using System.Collections.Generic;

namespace Multiverse.Domain.Dueling
{
    using Cards;

    /// <summary>Etat d'un joueur pendant le duel (PDF §3).</summary>
    [System.Serializable]
    public sealed class PlayerState
    {
        public int playerIndex;                       // 0 ou 1
        public string playerName;
        public int lifePoints = DuelRules.StartingLifePoints;
        public List<CardInstance> hand = new List<CardInstance>();
        public List<CardInstance> deck = new List<CardInstance>();
        public List<CardInstance> graveyard = new List<CardInstance>();
        public CardInstance[] characterSlots = new CardInstance[DuelRules.CharacterSlots];       // 5 slots
        public CardInstance[] spellTrapSlots = new CardInstance[DuelRules.SpellTrapSlots];      // 4 slots ordinaires
        public CardInstance fieldSpell;               // 1 slot Magie de Terrain (carte commune active, PDF §3)
        public int normalSummonsUsedThisTurn;
        public int turnNumberForThisPlayer;
        public bool isFirstPlayer;

        public bool HasAnyCharacterOnField
        {
            get
            {
                for (int i = 0; i < characterSlots.Length; i++)
                    if (characterSlots[i] != null) return true;
                return false;
            }
        }
    }

    /// <summary>Les 6 phases du tour (PDF §3), dans l'ordre reglementaire.</summary>
    [System.Serializable]
    public enum Phase
    {
        Draw = 0,
        Standby = 1,
        Main1 = 2,
        Battle = 3,
        Main2 = 4,
        End = 5
    }

    /// <summary>Etat global deterministe du duel : la source de verite du moteur. L'UI ne fait que le rendre.</summary>
    [System.Serializable]
    public sealed class GameState
    {
        public PlayerState[] players = new PlayerState[2];
        public Phase phase = Phase.Draw;
        public int activePlayerIndex;
        public int globalTurnIndex;
        public bool reactionWindowOpen;      // Le chronometre du joueur actif est suspendu pendant cette fenetre (PDF §4)
        public bool firstPlayerSkippedFirstBattle;   // Le 1er joueur n'attaque pas a son tout premier tour (PDF §3)
        public List<string> eventLog = new List<string>();   // Journal deterministe (replay/non-regression)

        public void Log(string message) => eventLog.Add(message);

        public PlayerState Active => players[activePlayerIndex];
        public PlayerState Opponent => players[1 - activePlayerIndex];

        public bool IsDuelOver => players[0].lifePoints <= 0 || players[1].lifePoints <= 0;
    }
}

namespace Multiverse.Domain.Cards
{
    /// <summary>Quand la capacite se declenche (PDF §7 : Active = manuelle, Passive/Triggered selon leur nature).</summary>
    public enum TriggerKind
    {
        Manual = 0,            // Active : le joueur declenche manuellement quand les conditions le permettent
        OnAttackDeclared = 1,  // Evenement : attaque declaree (typiquement les Pieges)
        OnCardSummoned = 2,    // Evenement : invocation
        OnDamageTaken = 3,     // Evenement : degats subis
        OnTurnStart = 4,       // Evenement : debut de tour
        OnTurnEnd = 5,         // Evenement : fin de tour
        OnCardDestroyed = 6,   // Evenement : destruction d'une carte
        PassiveAlways = 7      // Passif permanent
    }

    /// <summary>Limite d'utilisation affichee et enforcee par le moteur (visible sur les cartes de reference).</summary>
    public enum UsageLimit
    {
        Unlimited = 0,
        OncePerTurn = 1,
        OncePerDuel = 2
    }

    /// <summary>Precise QUI jouit de l'effet (on n'affecte jamais la cible du mauvais cote sans le vouloir).</summary>
    public enum OwnerSide { Own = 0, Enemy = 1, Any = 2 }

    /// <summary>Type de cible (PDF §7 : cible, portee). Extensible selon les cartes.</summary>
    public enum TargetKind
    {
        None = 0,
        EnemyCharacter = 1,
        OwnCharacter = 2,
        AnyCharacter = 3,
        EnemyPlayer = 4,
        OwnPlayer = 5,
        OwnDeck = 6,
        OwnHand = 7,
        OwnGraveyard = 8
    }

    /// <summary>Genre d'effet atomique. Chaque ligne d'effet est du DONNEE exploitable par le moteur, jamais du texte libre (PDF §7).</summary>
    public enum EffectKind
    {
        DealDamage = 0,          // Degats directs a une cible
        StatChange = 1,          // Buff/debuff ATK ou DEF
        Heal = 2,                // Soin des PV
        Destroy = 3,             // Destruction d'une carte
        ReturnToHand = 4,        // Renvoi a la main
        RedirectAttack = 5,      // Redirection d'attaque
        AvoidAttack = 6,         // Esquive/neutralisation d'attaque
        Immobilize = 7,          // Immobilisation N tours (ex. "Cauchemar Vivant")
        DirectAttackOverride = 8,// Attaque directe autorisee malgre la presence de personnages (ex. "Manipulation Politique")
        MoveEnemyCharacter = 9,  // Deplace un personnage adverse vers une zone (ex. Vlad)
        SearchFromDeck = 10,     // Recherche d'une carte dans le deck (ex. "Passeport dimensionnel")
        PreventAbilityUse = 11,  // Nier une capacite un tour (ex. "Piratage de Reseau")
        GainAttackBonus = 12     // Bonus ATK conditionnel (ex. "Amie Fidele")
    }

    /// <summary>Coût de la capacite (icones colorees visibles sur les cartes de reference).

    // Les copts sont payes AVANT la resolution et ne peuvent jamais etre rembourses par le moteur.</summary>
    [System.Serializable]
    public sealed class Cost
    {
        public string costId;    // Identifiant d'icone (catalogue visuel)
        public int amount;
    }

    /// <summary>Specification d'un effet du moteur : declencheur, conditions, cible, consequences, limites (PDF §7).

    // Regle d'or : jamais deux fois, jamais de boucle infinie — l'engine garde un compteur global par activation.</summary>
    [System.Serializable]
    public sealed class EffectSpec
    {
        public EffectKind kind;
        public int amount;               // Valeur principale selon kind (degats, buff...)
        public int durationTurns;        // 0 = instantane / permanent selon le kind
        public TargetKind target = TargetKind.None;
        public OwnerSide side = OwnerSide.Own;
        public string worldId;           // universe filtre (ex. rechercher une carte d'un univers)
        public int maxPopularity;        // filtre de popularite (ex. <= 4)
        public string effectId;          // Identifiant stable pour logs/telemetrie/replay
    }

    /// <summary>Definition d'une capacite. Une carte Personnage peut en avoir 1 (commune) a 6 (ultra-legendaire).</summary>
    [System.Serializable]
    public sealed class AbilityDefinition
    {
        public string abilityName;
        public TriggerKind trigger = TriggerKind.Manual;
        public ConditionTree condition;             // AND/OR/Not, groupes imbriques
        public Cost[] costs = new Cost[0];
        public Targeting targeting;
        public EffectSpec[] effects = new EffectSpec[0];
        public UsageLimit usageLimit = UsageLimit.OncePerTurn;
        public LoreMetadata lore;

        /// <summary>Aggregation de ciblage (une capacite peut proposer un choix de cibles).</summary>
        [System.Serializable]
        public sealed class Targeting
        {
            public TargetKind primary = TargetKind.None;
            public bool multiTarget = false;        // Attaques multi-cibles autorisees par effet (PDF §9)
            public bool optionalTarget = true;
        }
    }
}

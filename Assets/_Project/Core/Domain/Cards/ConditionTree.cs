namespace Multiverse.Domain.Cards
{
    /// <summary>Operateur logique d'un noeud de conditions : supporte AND, OR et groupes imbriques (PDF §7).</summary>
    public enum ConditionOp { And = 0, Or = 1, Not = 2 }

    /// <summary>Type de predicat brut evalue par le moteur d'effets.</summary>
    public enum ConditionKind
    {
        Always = 0,
        OwnLifeBelowPercent = 1,
        EnemyHaveCharacters = 2,
        OwnHaveCharacters = 3,
        TargetPopularityAtMost = 4,
        TargetIsUniverse = 5,
        SameUniverseOnField = 6,
        TurnCountAtLeast = 7,
        OwnHandCountAtLeast = 8
    }

    /// <summary>Feuille de condition. Parametre generique pour les variantes numeriques (ex: popularite &lt;= N, LP &lt; X %).
    /// #Universe : identifiant d'univers pour TargetIsUniverse / SameUniverseOnField.</summary>
    [System.Serializable]
    public sealed class ConditionLeaf
    {
        public ConditionKind kind;
        public int value;
        public string universeId;

        public ConditionLeaf() { }
        public ConditionLeaf(ConditionKind kind, int value = 0, string universeId = null)
        {
            this.kind = kind; this.value = value; this.universeId = universeId;
        }
    }

    /// <summary>Arbre de conditions serialisable par JsonUtility : un operateur, des feuilles et des sous-groupes.
    /// Exemple : OR[ A, AND[ B, C ] ]  =>  { op: "Or", leaves: [A], groups: [ { op: "And", leaves: [B, C] } ] }</summary>
    [System.Serializable]
    public sealed class ConditionTree
    {
        public ConditionOp op = ConditionOp.And;
        public ConditionLeaf[] leaves = new ConditionLeaf[0];
        public ConditionTree[] groups = new ConditionTree[0];

        public ConditionTree() { }
        public ConditionTree(ConditionOp op, params ConditionLeaf[] leaves)
        {
            this.op = op;
            this.leaves = leaves ?? new ConditionLeaf[0];
            this.groups = new ConditionTree[0];
        }

        /// <summary>Groupe vide ou liste de feuilles vide = toujours vrai (neutre, permet les effets passifs).</summary>
        public bool IsNeutral()
        {
            return (leaves == null || leaves.Length == 0) && (groups == null || groups.Length == 0);
        }
    }
}

using System.Collections.Generic;

namespace Multiverse.EffectEngine
{
    using Domain.Cards;
    using Domain.Dueling;

    /// <summary>Interface d'evaluation des conditions AND/OR/NOT imbriquees (PDF §7).
    // Le moteur evalue l'arbre recursivement ; une feuille vide/Always est toujours vraie.</summary>
    public static class ConditionEvaluator
    {
        public static bool Evaluate(ConditionTree tree, GameState state, PlayerState self, CardInstance src)
        {
            if (tree == null || tree.IsNeutral()) return true;

            // Resultats de tous les enfants (feuilles + sous-groupes) puis agregation par l'operateur.
            List<bool> results = new List<bool>();
            if (tree.leaves != null)
                foreach (var leaf in tree.leaves)
                    results.Add(EvaluateLeaf(leaf, state, self, src));
            if (tree.groups != null)
                foreach (var group in tree.groups)
                    results.Add(Evaluate(group, state, self, src));

            if (results.Count == 0) return true;

            switch (tree.op)
            {
                case ConditionOp.And:   // Toutes les sous-conditions doivent etre vraies
                    foreach (var r in results) if (!r) return false;
                    return true;
                case ConditionOp.Or:    // Au moins une condition vraie
                    foreach (var r in results) if (r) return true;
                    return false;
                case ConditionOp.Not:   // Vrai seulement si AUCUNE enfant n'est vraie
                    foreach (var r in results) if (r) return false;
                    return true;
                default: return true;
            }
        }

        private static bool EvaluateLeaf(ConditionLeaf leaf, GameState state, PlayerState self, CardInstance src)
        {
            if (leaf == null) return true;
            var enemy = state.players[1 - self.playerIndex];
            switch (leaf.kind)
            {
                case ConditionKind.Always: return true;
                case ConditionKind.OwnLifeBelowPercent:
                    return self.lifePoints * 100 < leaf.value * DuelRules.StartingLifePoints / 100;
                case ConditionKind.EnemyHaveCharacters: return enemy.HasAnyCharacterOnField;
                case ConditionKind.OwnHaveCharacters: return self.HasAnyCharacterOnField;
                case ConditionKind.SameUniverseOnField:
                    return HasCharacterOfUniverse(self, leaf.universeId);
                case ConditionKind.TurnCountAtLeast: return state.globalTurnIndex >= leaf.value;
                case ConditionKind.OwnHandCountAtLeast: return self.hand.Count >= leaf.value;
                case ConditionKind.TargetPopularityAtMost:
                case ConditionKind.TargetIsUniverse:
                default: return true;   // Ces predicats servent au filtrage de cibles, pas au gate d'activation
            }
        }

        private static bool HasCharacterOfUniverse(PlayerState p, string universeId)
        {
            if (string.IsNullOrEmpty(universeId)) return true;
            for (int i = 0; i < p.characterSlots.Length; i++)
            {
                var slot = p.characterSlots[i];
                if (slot == null) continue;
                if (slot.universeOverride == universeId) return true;            }
            return false;
        }
    }
}

using System.Collections.Generic;

namespace Multiverse.EffectEngine
{
    using Domain.Cards;
    using Domain.Dueling;

    /// <summary>Pile / chaine d'effets avec fenetres de reaction (PDF §4 et §7).
    // Garanties moteur : ordre LIFO, une seule resolution par activation, detection anti-boucle.</summary>
    public sealed class EffectStack
    {
        private readonly List<Activation> _stack = new List<Activation>();
        private int _resolutionCounter;              // Garde anti double-resolution
        private const int MaxChainDepth = 32;        // Anti boucle infinie (PDF §7)

        public sealed class Activation
        {
            public AbilityDefinition ability;
            public CardInstance source;
            public PlayerState activator;
            public CardInstance target;
            public bool resolved;                    // false = pas encore resolu, jamais deux fois
        }

        public int Count => _stack.Count;

        /// <summary>Pousser une activation ; l'adversaire peut repondre pendant la fenetre de reaction.</summary>
        public void Push(AbilityDefinition ability, CardInstance source, PlayerState activator, CardInstance target)
        {
            if (_stack.Count >= MaxChainDepth)
                throw new System.InvalidOperationException("Chaine d'effets trop profonde (protection anti-boucle).");
            _stack.Add(new Activation { ability = ability, source = source, activator = activator, target = target });
        }

        /// <summary>Resolve LIFO en marquant chaque activation : une activation n'est JAMAIS resolue deux fois.</summary>
        public Activation ResolveNext()
        {
            if (_stack.Count == 0) return null;
            var top = _stack[_stack.Count - 1];
            if (top.resolved)
                throw new System.InvalidOperationException("Double resolution bloquee : l'activation a deja ete resolue.");
            top.resolved = true;
            _resolutionCounter++;
            _stack.RemoveAt(_stack.Count - 1);
            return top;
        }

        public int TotalResolved => _resolutionCounter;
        public void Clear() { _stack.Clear(); }
    }
}

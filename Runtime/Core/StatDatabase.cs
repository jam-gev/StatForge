using System;
using System.Collections.Generic;
using UnityEngine;

namespace StatForge
{
    [Serializable]
    public sealed class StatDatabase
    {
        [SerializeField] private List<StatDefinition> definitions = new List<StatDefinition>();

        private Dictionary<StatId, StatDefinition> _lookup;

        public bool IsInitialized => this._lookup != null;

        public IReadOnlyList<StatDefinition> Definitions => this.definitions;

        public void Initialize()
        {
            var lookup = new Dictionary<StatId, StatDefinition>(this.definitions.Count);

            for (int i = 0; i < this.definitions.Count; i++)
            {
                StatDefinition definition = this.definitions[i];

                if (definition == null)
                {
                    throw new InvalidOperationException($"StatDatabase: element {i} is null");
                }

                definition.Validate();

                if (lookup.TryGetValue(definition.Id, out StatDefinition existing))
                {
                    throw new InvalidOperationException($"StatDatabase: duplicate StatId '{definition.Id}' in '{existing.name}' and '{definition.name}'");
                }

                lookup.Add(definition.Id, definition);
            }

            this._lookup = lookup;
        }

        public StatDefinition Get(StatId id)
        {
            if (!TryGet(id, out StatDefinition definition))
            {
                throw new KeyNotFoundException($"StatDatabase: StatId '{id}' not found");
            }

            return definition;
        }

        public bool TryGet(StatId id, out StatDefinition definition)
        {
            if (this._lookup == null)
            {
                throw new InvalidOperationException("StatDatabase: call Initialize() before use");
            }

            return this._lookup.TryGetValue(id, out definition);
        }
    }
}
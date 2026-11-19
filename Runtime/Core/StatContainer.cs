using System;
using System.Collections.Generic;

namespace StatForge
{
    /// <summary>
    /// Runtime collection of stats for one entity. Not serialized by Unity: build it at runtime
    /// from StatDefinitions (e.g. via StatDatabase) and persist base values separately if needed.
    /// </summary>
    public class StatContainer
    {
        // Starts at 1 so a default handle (ContainerId 0) never matches any container.
        private static int s_nextContainerId;

        private readonly int _containerId = ++s_nextContainerId;
        private readonly Dictionary<StatId, StatNode> _nodes = new();

        #region Methods

        #region public

        public void AddStat(StatDefinition definition)
        {
            if (definition == null)
            {
                throw new ArgumentNullException(nameof(definition));
            }

            if (HasStat(definition.Id))
            {
                throw new InvalidOperationException($"StatContainer already contains '{definition.Id}'");
            }

            this._nodes.Add(definition.Id, new StatNode(definition));
        }

        public bool HasStat(StatId id)
        {
            return this._nodes.ContainsKey(id);
        }

        public float GetBase(StatId id)
        {
            return GetNode(id).BaseValue;
        }

        public float GetValue(StatId id)
        {
            return GetNode(id).Value;
        }

        public float GetRoundedValue(StatId id)
        {
            return GetNode(id).RoundedValue;
        }

        public bool TryGetBase(StatId id, out float baseValue)
        {
            if (this._nodes.TryGetValue(id, out StatNode node))
            {
                baseValue = node.BaseValue;
                return true;
            }

            baseValue = 0f;
            return false;
        }

        public bool TryGetValue(StatId id, out float value)
        {
            if (this._nodes.TryGetValue(id, out StatNode node))
            {
                value = node.Value;
                return true;
            }

            value = 0f;
            return false;
        }

        public void SetBase(StatId id, float baseValue)
        {
            GetNode(id).SetBase(baseValue);
        }

        public StatModifierHandle AddModifier(StatModifier statModifier)
        {
            StatNode node = GetNode(statModifier.StatId);
            int entryId = node.AddModifier(statModifier);
            return new StatModifierHandle(this._containerId, statModifier.StatId, entryId);
        }

        /// <summary>Returns false for default, already-removed or other-container handles (not an error).</summary>
        public bool RemoveModifier(StatModifierHandle statModifierHandle)
        {
            if (!statModifierHandle.IsValid || statModifierHandle.ContainerId != this._containerId)
            {
                return false;
            }

            if (!this._nodes.TryGetValue(statModifierHandle.StatId, out StatNode node))
            {
                return false;
            }

            return node.RemoveModifier(statModifierHandle.EntryId);
        }

        #endregion

        #region Private

        private StatNode GetNode(StatId id)
        {
            if (!this._nodes.TryGetValue(id, out StatNode node))
            {
                throw new KeyNotFoundException($"StatContainer has no stat '{id}'");
            }

            return node;
        }

        #endregion

        #endregion
    }
}
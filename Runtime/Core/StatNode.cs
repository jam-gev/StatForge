using System;
using System.Collections.Generic;

namespace StatForge
{
    /// <summary>
    /// Runtime state of a single stat.
    /// BaseValue is stored unclamped; only the final Value is clamped by the definition's Min/Max
    /// (so later modifiers are applied to the raw base before clamping).
    /// </summary>
    internal sealed class StatNode
    {
        #region Fields

        private readonly StatDefinition _definition;

        private float _baseValue;
        private float _finalValue;
        private bool _isDirty;

        private int _nextEntryId;

        private readonly List<StatModifierEntry> _statModifierEntries;

        private readonly IStatAggregator _aggregator;

        #endregion

        #region Properties

        public StatId Id => this._definition.Id;

        public float BaseValue => this._baseValue;

        public float RoundedValue => this._definition.ApplyRounding(Value);

        public float Value
        {
            get
            {
                if (this._isDirty)
                {
                    SetFinal(Calculate());
                }

                return this._finalValue;
            }
        }

        #endregion

        #region Methods

        public StatNode(StatDefinition definition)
        {
            if (definition == null)
            {
                throw new ArgumentNullException(nameof(definition));
            }

            definition.Validate();

            this._definition = definition;

            this._baseValue = definition.BaseValue;
            this._finalValue = definition.BaseValue;
            this._nextEntryId = 0;

            this._aggregator = definition.Aggregator;

            this._statModifierEntries = new List<StatModifierEntry>();

            this._isDirty = true;
        }

        public void SetBase(float baseValue)
        {
            if (float.IsNaN(baseValue) || float.IsInfinity(baseValue))
            {
                throw new ArgumentOutOfRangeException(nameof(baseValue), baseValue, $"[{Id}] BaseValue must be a finite number");
            }

            // Exact compare on purpose: Mathf.Approximately would drop small increments
            // (e.g. +0.0008 per frame on a base of 1000 is ignored forever).
            // ReSharper disable once CompareOfFloatsByEqualityOperator
            if (baseValue == this._baseValue)
            {
                return;
            }

            this._baseValue = baseValue;
            this._isDirty = true;
        }

        private void SetFinal(float final)
        {
            this._finalValue = this._definition.Clamp(final);
            this._isDirty = false;
        }

        private float Calculate()
        {
            return this._aggregator.Calculate(this._baseValue, this._statModifierEntries);
        }

        public int AddModifier(StatModifier statModifier)
        {
            int id = ++this._nextEntryId;
            this._statModifierEntries.Add(new StatModifierEntry(id, statModifier));
            this._isDirty = true;
            return id;
        }

        public bool RemoveModifier(int entryId)
        {
            for (int index = 0; index < this._statModifierEntries.Count; index++)
            {
                if (this._statModifierEntries[index].EntryId != entryId)
                {
                    continue;
                }

                this._statModifierEntries.RemoveAt(index);
                this._isDirty = true;
                return true;
            }

            return false;
        }

        #endregion
    }
}
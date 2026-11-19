using System.Collections.Generic;
using UnityEngine;

namespace StatForge
{
    public abstract class StatAggregator : ScriptableObject, IStatAggregator
    {
        public abstract float Calculate(float baseValue, IReadOnlyList<StatModifierEntry> entries);
    }
}
using System.Collections.Generic;

namespace StatForge
{
    public interface IStatAggregator
    {
        public float Calculate(float baseValue, IReadOnlyList<StatModifierEntry> entries);
    }
}
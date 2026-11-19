using System.Collections.Generic;

namespace StatForge.Tests.Shared
{
    /// <summary>
    /// Test-only aggregator: base + sum of Add entries, other channels ignored.
    /// Lets container/node tests run without depending on the real aggregation order.
    /// Created by <see cref="TestStatDefinitions.Create"/> and destroyed by <see cref="TestStatDefinitions.Destroy"/>.
    /// </summary>
    public sealed class FakeStatAggregator : StatAggregator<StandardStatChannel>
    {
        // Test-only state. A real aggregator asset is shared by every stat that uses it,
        // so it must never keep runtime state like this.
        public int CallCount { get; private set; }

        public override float Calculate(float baseValue, IReadOnlyList<StatModifierEntry> entries)
        {
            CallCount++;

            float add = 0f;

            // for, not foreach: foreach over IReadOnlyList<T> boxes the enumerator (allocation tests would fail).
            for (int i = 0; i < entries.Count; i++)
            {
                if (ChannelOf(entries[i]) == StandardStatChannel.Add)
                {
                    add += entries[i].Value;
                }
            }

            return baseValue + add;
        }
    }
}

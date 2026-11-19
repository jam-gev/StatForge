using System.Collections.Generic;
using UnityEngine;

namespace StatForge
{
    [CreateAssetMenu(fileName = "StandardStatAggregator", menuName = "StatForge/StandardStatAggregator")]
    public sealed class StandardStatAggregator : StatAggregator<StandardStatChannel>
    {
        public override float Calculate(float baseValue, IReadOnlyList<StatModifierEntry> entries)
        {
            float sumAddChannel = 0;
            float sumAddPercentChannel = 0;
            float productMultiply = 1;
            float overrideValue = 0;

            bool hasOverride = false;

            for (int index = 0; index < entries.Count; index++)
            {
                switch (ChannelOf(entries[index]))
                {
                    case StandardStatChannel.Add:
                    {
                        sumAddChannel += entries[index].Value;
                        break;
                    }
                    case StandardStatChannel.AddPercent:
                    {
                        sumAddPercentChannel += entries[index].Value;
                        break;
                    }
                    case StandardStatChannel.Multiply:
                    {
                        productMultiply *= entries[index].Value;
                        break;
                    }
                    case StandardStatChannel.Override:
                    {
                        overrideValue = entries[index].Value;
                        hasOverride = true;
                        break;
                    }
                    default:
                    case StandardStatChannel.None:
                        break;
                }
            }

            if (hasOverride)
            {
                return overrideValue;
            }

            return (baseValue + sumAddChannel) * (1 + sumAddPercentChannel) * productMultiply;
        }
    }
}
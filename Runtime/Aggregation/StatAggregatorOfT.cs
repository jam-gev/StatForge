using System;

namespace StatForge
{
    /// <summary>
    /// Base for aggregators that read channels as a project-defined enum.
    /// TStatModifierChannel must be int-backed and use 0 for "None". Subclass it with a concrete enum so Unity can create the asset:
    /// <c>public sealed class MyAggregator : StatAggregator&lt;MyChannel&gt;</c>.
    /// </summary>
    public abstract class StatAggregator<TStatModifierChannel> : StatAggregator where TStatModifierChannel : unmanaged, Enum
    {
        /// <summary>Reads the stored int back as TStatModifierChannel. No boxing.</summary>
        protected static TStatModifierChannel ChannelOf(in StatModifierEntry entry)
        {
            return StatChannelCast<TStatModifierChannel>.FromInt(entry.ModifierChannel);
        }
    }
}
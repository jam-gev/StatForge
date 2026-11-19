using System;

namespace StatForge
{
    /// <summary>
    /// Base for aggregators that read channels as a project-defined enum.
    /// TChannel must be int-backed and use 0 for "None". Subclass it with a concrete enum so Unity can create the asset:
    /// <c>public sealed class MyAggregator : StatAggregator&lt;MyChannel&gt;</c>.
    /// </summary>
    public abstract class StatAggregator<TChannel> : StatAggregator where TChannel : unmanaged, Enum
    {
        /// <summary>Reads the stored int back as TChannel. No boxing.</summary>
        protected static TChannel ChannelOf(in StatModifierEntry entry)
        {
            return StatChannelCast<TChannel>.FromInt(entry.ModifierChannel);
        }
    }
}
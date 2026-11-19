using System;

namespace StatForge
{
    /// <summary>
    /// What to apply to a stat. The channel is stored as an int so the core never depends on a channel enum;
    /// create modifiers with <see cref="Create{TChannel}"/> using your project's enum (0 = None, int-backed).
    /// </summary>
    public readonly struct StatModifier
    {
        public readonly StatId StatId;

        public readonly int ModifierChannel;

        public readonly float Value;

        public StatModifier(StatId statId, int statModifierChannel, float value)
        {
            StatId = statId;
            ModifierChannel = statModifierChannel;
            Value = value;
        }

        public static StatModifier Create<TChannel>(StatId statId, TChannel channel, float value) where TChannel : unmanaged, Enum
        {
            return new StatModifier(statId, StatChannelCast<TChannel>.ToInt(channel), value);
        }
    }
}
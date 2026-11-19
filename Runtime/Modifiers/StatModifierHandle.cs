using System;

namespace StatForge
{
    /// <summary>
    /// Receipt returned by <see cref="StatContainer.AddModifier"/>; pass it back to RemoveModifier.
    /// default(StatModifierHandle) is invalid (ids start at 1).
    /// Compared by value, so it can be a Dictionary/HashSet key without boxing
    /// (e.g. a buff keeping a set of the handles it applied).
    /// </summary>
    public readonly struct StatModifierHandle : IEquatable<StatModifierHandle>
    {
        public readonly int ContainerId;

        public readonly StatId StatId;

        public readonly int EntryId;

        public StatModifierHandle(int containerId, StatId statId, int entryId)
        {
            ContainerId = containerId;
            StatId = statId;
            EntryId = entryId;
        }

        public bool IsValid => ContainerId > 0 && EntryId > 0;

        public bool Equals(StatModifierHandle other)
        {
            return ContainerId == other.ContainerId && EntryId == other.EntryId && StatId.Equals(other.StatId);
        }

        public override bool Equals(object obj) => obj is StatModifierHandle other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(ContainerId, StatId, EntryId);

        public override string ToString() => $"Handle({ContainerId}:{StatId}#{EntryId})";

        public static bool operator ==(StatModifierHandle left, StatModifierHandle right) => left.Equals(right);

        public static bool operator !=(StatModifierHandle left, StatModifierHandle right) => !left.Equals(right);
    }
}
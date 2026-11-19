using System;
using UnityEngine;

namespace StatForge
{
    /// <summary>
    /// Strongly-typed identifier for a stat. Compared by value (ordinal, case-sensitive),
    /// so it is safe to use as a Dictionary/HashSet key.
    /// </summary>
    [Serializable]
    public struct StatId : IEquatable<StatId>
    {
        [field: SerializeField] public string Value { get; private set; }

        public StatId(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("StatId cannot be null or empty.", nameof(value));
            }

            Value = value.Trim();
        }

        public bool IsValid => !string.IsNullOrEmpty(Value);

        public bool Equals(StatId other) => string.Equals(Value, other.Value, StringComparison.Ordinal);

        public override bool Equals(object obj) => obj is StatId other && Equals(other);

        public override int GetHashCode() => Value == null ? 0 : StringComparer.Ordinal.GetHashCode(Value);

        public override string ToString() => Value ?? string.Empty;

        public static bool operator ==(StatId left, StatId right) => left.Equals(right);

        public static bool operator !=(StatId left, StatId right) => !left.Equals(right);

        public static explicit operator StatId(string value) => new StatId(value);
    }
}
using System;
using UnityEngine;

namespace StatForge
{
    [CreateAssetMenu(fileName = "StatDefinition", menuName = "StatForge/StatDefinition")]
    public class StatDefinition : ScriptableObject
    {
        [field: SerializeField] public StatId Id { get; private set; }
        [field: SerializeField] public float BaseValue { get; private set; }
        [field: SerializeField] public bool HasMin { get; private set; }
        [field: SerializeField] public float Min { get; private set; }
        [field: SerializeField] public bool HasMax { get; private set; }
        [field: SerializeField] public float Max { get; private set; }
        [field: SerializeField] public StatRounding Rounding { get; private set; }
        [field: SerializeField] public StatAggregator Aggregator { get; private set; }

        private float? MinValue => HasMin ? Min : null;

        private float? MaxValue => HasMax ? Max : null;

        public void Validate()
        {
            ThrowIfInvalid(Id, BaseValue, MinValue, MaxValue, Rounding);

            // Checked here, not in StatNode: fail when the definition is loaded/validated,
            // not on the first GetValue somewhere in gameplay.
            if (Aggregator == null)
            {
                throw new InvalidOperationException($"[{Id}] Aggregator is not assigned");
            }

            OnValidateDefinition();
        }

        /// <summary>
        /// Extension point for derived definitions (extra data such as icon, display name…).
        /// Throw to reject the definition. Runs after the package's own checks, which a subclass cannot skip
        /// (that is why Validate itself is not virtual).
        /// </summary>
        protected virtual void OnValidateDefinition()
        {
        }

        private static void ThrowIfInvalid(StatId id, float defaultBase, float? minValue, float? maxValue, StatRounding rounding)
        {
            if (!Enum.IsDefined(typeof(StatRounding), rounding))
            {
                throw new ArgumentOutOfRangeException(nameof(rounding), rounding, $"[{id}] Unknown rounding mode");
            }

            // Rounding is applied to the clamped value, so non-integer bounds could round outside [Min, Max]
            // (e.g. Max 10.5 + Ceil -> 11).
            if (rounding != StatRounding.None && (!IsWhole(minValue) || !IsWhole(maxValue)))
            {
                throw new ArgumentException($"[{id}] Min/Max must be whole numbers when Rounding is {rounding}");
            }

            if (!id.IsValid)
            {
                throw new ArgumentException("StatId is invalid", nameof(id));
            }

            if (minValue.HasValue && maxValue.HasValue && minValue.Value > maxValue.Value)
            {
                throw new ArgumentException($"[{id}] Min ({minValue}) > Max ({maxValue})");
            }

            if (minValue.HasValue && defaultBase < minValue.Value || maxValue.HasValue && defaultBase > maxValue.Value)
            {
                throw new ArgumentOutOfRangeException(nameof(defaultBase), defaultBase, $"[{id}] DefaultBase not in range [{minValue}, {maxValue}]");
            }
        }

        private static bool IsWhole(float? value)
        {
            return !value.HasValue || Mathf.Approximately(MathF.Floor(value.Value), value.Value);
        }

        public float ApplyRounding(float value)
        {
            return Rounding switch
            {
                StatRounding.Floor => MathF.Floor(value),
                StatRounding.Ceil => MathF.Ceiling(value),
                StatRounding.Round => MathF.Round(value, MidpointRounding.AwayFromZero),
                _ => value,
            };
        }

        public float Clamp(float value)
        {
            if (HasMin && value < Min)
            {
                value = Min;
            }

            if (HasMax && value > Max)
            {
                value = Max;
            }

            return value;
        }
    }
}
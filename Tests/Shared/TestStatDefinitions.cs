using UnityEditor;
using UnityEngine;

namespace StatForge.Tests.Shared
{
    /// <summary>
    /// Builds in-memory StatDefinitions for tests. StatDefinition has no runtime setter on purpose
    /// (it is a shared asset), so values are written through SerializedObject, the same path the Inspector uses.
    /// Every call creates a new instance; caller owns it and must release it with <see cref="Destroy"/>.
    /// </summary>
    public static class TestStatDefinitions
    {
        /// <param name="aggregator">
        /// Null = a new <see cref="FakeStatAggregator"/> owned by the definition (released by <see cref="Destroy"/>).
        /// Non-null = owned by the caller, who destroys it.
        /// </param>
        /// <param name="noAggregator">Leave the field null to test Validate.</param>
        public static StatDefinition Create(StatId id, float baseValue, float? min = null, float? max = null,
            StatRounding rounding = StatRounding.None, StatAggregator aggregator = null, bool noAggregator = false)
        {
            if (noAggregator)
            {
                aggregator = null;
            }
            else if (aggregator == null)
            {
                aggregator = ScriptableObject.CreateInstance<FakeStatAggregator>();
            }

            var definition = ScriptableObject.CreateInstance<StatDefinition>();
            definition.name = id.ToString();

            using (var so = new SerializedObject(definition))
            {
                so.FindProperty(BackingField(nameof(StatDefinition.Id)))
                    .FindPropertyRelative(BackingField(nameof(StatId.Value))).stringValue = id.Value;
                so.FindProperty(BackingField(nameof(StatDefinition.BaseValue))).floatValue = baseValue;
                so.FindProperty(BackingField(nameof(StatDefinition.HasMin))).boolValue = min.HasValue;
                so.FindProperty(BackingField(nameof(StatDefinition.Min))).floatValue = min.GetValueOrDefault();
                so.FindProperty(BackingField(nameof(StatDefinition.HasMax))).boolValue = max.HasValue;
                so.FindProperty(BackingField(nameof(StatDefinition.Max))).floatValue = max.GetValueOrDefault();
                so.FindProperty(BackingField(nameof(StatDefinition.Rounding))).intValue = (int)rounding;
                so.FindProperty(BackingField(nameof(StatDefinition.Aggregator))).objectReferenceValue = aggregator;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            try
            {
                definition.Validate();
            }
            catch
            {
                Destroy(definition);
                throw;
            }

            return definition;
        }

        /// <summary>Destroys the definition and the fake aggregator Create made for it (never a caller-owned one).</summary>
        public static void Destroy(StatDefinition definition)
        {
            if (definition == null)
            {
                return;
            }

            if (definition.Aggregator is FakeStatAggregator fake)
            {
                Object.DestroyImmediate(fake);
            }

            Object.DestroyImmediate(definition);
        }

        // [field: SerializeField] auto-properties serialize under the compiler-generated backing field name.
        private static string BackingField(string propertyName) => $"<{propertyName}>k__BackingField";
    }
}

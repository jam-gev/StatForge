using System.Collections.Generic;
using NUnit.Framework;
using StatForge.Tests.Shared;
using UnityEngine;
using UnityEngine.TestTools.Constraints;
using Is = UnityEngine.TestTools.Constraints.Is;

namespace StatForge.Tests
{
    /// <summary>
    /// StandardStatAggregator formula: (base + ΣAdd) × (1 + ΣAddPercent) × ΠMultiply; Override (last added) wins.
    /// Called directly, no container: these tests pin the formula, not the caching.
    /// </summary>
    public class StatAggregatorTests
    {
        private const float Base = 100f;
        private const float Tolerance = 1e-4f;

        private static readonly StatId AttackSpeed = new StatId("AttackSpeed");

        private readonly List<StatModifierEntry> _entries = new();
        private StandardStatAggregator _aggregator;
        private int _nextEntryId;

        [SetUp]
        public void SetUp()
        {
            this._aggregator = ScriptableObject.CreateInstance<StandardStatAggregator>();
            this._entries.Clear();
            this._nextEntryId = 0;
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(this._aggregator);
        }

        private void Add(StandardStatChannel channel, float value)
        {
            this._entries.Add(new StatModifierEntry(++this._nextEntryId, (int)channel, value));
        }

        private float Calculate()
        {
            return this._aggregator.Calculate(Base, this._entries);
        }

        #region Formula

        [Test]
        public void NoEntries_ReturnsBase()
        {
            Assert.AreEqual(100f, Calculate(), Tolerance);
        }

        [Test]
        public void TwoAddPercent_AreSummed()
        {
            Add(StandardStatChannel.AddPercent, 0.1f);
            Add(StandardStatChannel.AddPercent, 0.1f);

            // 100 × (1 + 0.2), not 100 × 1.1 × 1.1 = 121
            Assert.AreEqual(120f, Calculate(), Tolerance);
        }

        [Test]
        public void TwoMultiply_AreMultiplied()
        {
            Add(StandardStatChannel.Multiply, 2f);
            Add(StandardStatChannel.Multiply, 1.5f);

            // Catches "=" instead of "*=" (would give 150)
            Assert.AreEqual(300f, Calculate(), Tolerance);
        }

        [Test]
        public void Add_IsAppliedBeforePercent()
        {
            Add(StandardStatChannel.Add, 10f);
            Add(StandardStatChannel.AddPercent, 0.5f);

            // (100 + 10) × 1.5, not 100 × 1.5 + 10 = 160
            Assert.AreEqual(165f, Calculate(), Tolerance);
        }

        [Test]
        public void FullOrder_AddThenPercentThenMultiply()
        {
            Add(StandardStatChannel.Add, 10f);
            Add(StandardStatChannel.AddPercent, 0.5f);
            Add(StandardStatChannel.Multiply, 2f);

            Assert.AreEqual(330f, Calculate(), Tolerance);
        }

        [Test]
        public void EntryOrder_DoesNotChangeResult()
        {
            Add(StandardStatChannel.Multiply, 2f);
            Add(StandardStatChannel.AddPercent, 0.5f);
            Add(StandardStatChannel.Add, 10f);

            Assert.AreEqual(330f, Calculate(), Tolerance);
        }

        [Test]
        public void MultiplyBelowOne_ReducesValue()
        {
            Add(StandardStatChannel.Multiply, 0.5f);

            Assert.AreEqual(50f, Calculate(), Tolerance);
        }

        [Test]
        public void NegativeAddPercent_ReducesValue()
        {
            Add(StandardStatChannel.AddPercent, -0.3f);

            Assert.AreEqual(70f, Calculate(), Tolerance);
        }

        #endregion

        #region Override

        [Test]
        public void Override_IgnoresOtherChannels()
        {
            Add(StandardStatChannel.Add, 10f);
            Add(StandardStatChannel.AddPercent, 0.5f);
            Add(StandardStatChannel.Multiply, 2f);
            Add(StandardStatChannel.Override, 50f);

            Assert.AreEqual(50f, Calculate(), Tolerance);
        }

        [Test]
        public void OverrideToZero_ReturnsZero()
        {
            Add(StandardStatChannel.Override, 0f);

            // "Override to 0" must differ from "no override"
            Assert.AreEqual(0f, Calculate(), Tolerance);
        }

        [Test]
        public void TwoOverrides_LastAddedWins()
        {
            Add(StandardStatChannel.Override, 50f);
            Add(StandardStatChannel.Override, 70f);

            Assert.AreEqual(70f, Calculate(), Tolerance);
        }

        #endregion

        #region Edge cases

        [Test]
        public void NoneChannel_IsIgnored()
        {
            this._entries.Add(default);

            Assert.AreEqual(100f, Calculate(), Tolerance);
        }

        [Test]
        public void Calculate_DoesNotAllocate()
        {
            Add(StandardStatChannel.Add, 10f);
            Add(StandardStatChannel.AddPercent, 0.5f);
            Add(StandardStatChannel.Multiply, 2f);
            Add(StandardStatChannel.Override, 50f);

            Assert.That(() => { Calculate(); }, Is.Not.AllocatingGCMemory());
        }

        #endregion

        #region Through StatContainer (real aggregator + definition clamp)

        [Test]
        public void AttackSpeed_AboveMax_IsClamped_BaseUnchanged()
        {
            StatDefinition definition = TestStatDefinitions.Create(AttackSpeed, 1f, min: 0.2f, max: 3f, aggregator: this._aggregator);
            try
            {
                var container = new StatContainer();
                container.AddStat(definition);

                container.AddModifier(StatModifier.Create(AttackSpeed, StandardStatChannel.AddPercent, 0.2f));
                container.AddModifier(StatModifier.Create(AttackSpeed, StandardStatChannel.AddPercent, 0.8f));
                Assert.AreEqual(2f, container.GetValue(AttackSpeed), Tolerance, "1 × (1 + 0.2 + 0.8)");

                container.AddModifier(StatModifier.Create(AttackSpeed, StandardStatChannel.Multiply, 2f));
                Assert.AreEqual(3f, container.GetValue(AttackSpeed), Tolerance, "4, clamped to Max 3");
                Assert.AreEqual(1f, container.GetBase(AttackSpeed), Tolerance);
            }
            finally
            {
                // Aggregator is ours (destroyed in TearDown); Destroy only frees the definition.
                TestStatDefinitions.Destroy(definition);
            }
        }

        [Test]
        public void AttackSpeed_HeavySlow_IsClampedToMin()
        {
            StatDefinition definition = TestStatDefinitions.Create(AttackSpeed, 1f, min: 0.2f, max: 3f, aggregator: this._aggregator);
            try
            {
                var container = new StatContainer();
                container.AddStat(definition);

                container.AddModifier(StatModifier.Create(AttackSpeed, StandardStatChannel.AddPercent, -0.9f));

                Assert.AreEqual(0.2f, container.GetValue(AttackSpeed), Tolerance, "0.1, clamped to Min 0.2");
            }
            finally
            {
                TestStatDefinitions.Destroy(definition);
            }
        }

        #endregion
    }
}

using System.Collections.Generic;
using NUnit.Framework;
using StatForge.Tests.Shared;
using UnityEngine.TestTools.Constraints;
using Is = UnityEngine.TestTools.Constraints.Is;

namespace StatForge.Tests
{
    /// <summary>Guards the "hot path never allocates" principle (Roadmap §2.4).</summary>
    public class StatAllocationTests
    {
        private static readonly StatId Health = new StatId("Health");
        private static readonly StatId Missing = new StatId("Missing");

        private StatDefinition _definition;
        private StatContainer _container;

        [SetUp]
        public void SetUp()
        {
            this._definition = TestStatDefinitions.Create(Health, 100f, 0f, 1000f, StatRounding.Floor);

            this._container = new StatContainer();
            this._container.AddStat(this._definition);
            this._container.GetValue(Health);
        }

        [TearDown]
        public void TearDown()
        {
            TestStatDefinitions.Destroy(this._definition);
        }

        [Test]
        public void GetValue_Cached_DoesNotAllocate()
        {
            Assert.That(() => { this._container.GetValue(Health); }, Is.Not.AllocatingGCMemory());
        }

        [Test]
        public void GetValue_Dirty_DoesNotAllocate()
        {
            Assert.That(() =>
            {
                this._container.SetBase(Health, 42f);
                this._container.GetValue(Health);
            }, Is.Not.AllocatingGCMemory());
        }

        [Test]
        public void GetRoundedValue_DoesNotAllocate()
        {
            Assert.That(() => { this._container.GetRoundedValue(Health); }, Is.Not.AllocatingGCMemory());
        }

        [Test]
        public void TryGetValue_HitAndMiss_DoNotAllocate()
        {
            Assert.That(() =>
            {
                this._container.TryGetValue(Health, out _);
                this._container.TryGetValue(Missing, out _);
            }, Is.Not.AllocatingGCMemory());
        }

        [Test]
        public void SetBase_DoesNotAllocate()
        {
            Assert.That(() => this._container.SetBase(Health, 7f), Is.Not.AllocatingGCMemory());
        }

        [Test]
        public void GetValue_DirtyWithModifiers_DoesNotAllocate()
        {
            // Added outside the lambda: List growth is a legitimate allocation, not what this test guards.
            this._container.AddModifier(StatModifier.Create(Health, StandardStatChannel.Add, 1f));
            this._container.AddModifier(StatModifier.Create(Health, StandardStatChannel.Add, 2f));
            this._container.AddModifier(StatModifier.Create(Health, StandardStatChannel.Add, 3f));
            this._container.GetValue(Health);

            Assert.That(() =>
            {
                this._container.SetBase(Health, 42f);
                this._container.GetValue(Health);
            }, Is.Not.AllocatingGCMemory());
        }

        [Test]
        public void HandleEquality_DoesNotAllocate()
        {
            // EqualityComparer<T>.Default is what Dictionary/HashSet use: without IEquatable<T> it falls back
            // to Equals(object) and boxes both handles. Experiment: remove IEquatable and watch this go red.
            StatModifierHandle a = this._container.AddModifier(StatModifier.Create(Health, StandardStatChannel.Add, 1f));
            StatModifierHandle b = this._container.AddModifier(StatModifier.Create(Health, StandardStatChannel.Add, 2f));
            EqualityComparer<StatModifierHandle> comparer = EqualityComparer<StatModifierHandle>.Default;

            Assert.That(() =>
            {
                comparer.Equals(a, b);
                comparer.GetHashCode(a);
            }, Is.Not.AllocatingGCMemory());
        }
    }
}
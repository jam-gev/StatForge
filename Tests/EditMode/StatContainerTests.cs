using System;
using System.Collections.Generic;
using NUnit.Framework;
using StatForge.Tests.Shared;

namespace StatForge.Tests
{
    public class StatContainerTests
    {
        private static readonly StatId Health = new StatId("Health");
        private static readonly StatId Missing = new StatId("Missing");

        private readonly List<StatDefinition> _createdDefinitions = new();
        private StatContainer _container;

        [SetUp]
        public void SetUp()
        {
            this._container = new StatContainer();
        }

        [TearDown]
        public void TearDown()
        {
            foreach (StatDefinition definition in this._createdDefinitions)
            {
                TestStatDefinitions.Destroy(definition);
            }

            this._createdDefinitions.Clear();
        }

        private StatDefinition CreateDefinition(StatId id, float baseValue, float? min = null, float? max = null,
            StatRounding rounding = StatRounding.None)
        {
            StatDefinition definition = TestStatDefinitions.Create(id, baseValue, min, max, rounding);
            this._createdDefinitions.Add(definition);
            return definition;
        }

        [Test]
        public void AddStat_ThenGetBase_ReturnsDefinitionBaseValue()
        {
            this._container.AddStat(CreateDefinition(Health, 100f));

            Assert.AreEqual(100f, this._container.GetBase(Health));
        }

        [Test]
        public void SetBase_ChangesBaseValue()
        {
            this._container.AddStat(CreateDefinition(Health, 100f));

            this._container.SetBase(Health, 42f);

            Assert.AreEqual(42f, this._container.GetBase(Health));
        }

        [Test]
        public void AddStat_Null_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => this._container.AddStat(null));
        }

        [Test]
        public void AddStat_DuplicateId_Throws()
        {
            this._container.AddStat(CreateDefinition(Health, 100f));

            Assert.Throws<InvalidOperationException>(() => this._container.AddStat(CreateDefinition(Health, 50f)));
        }

        [Test]
        public void GetBase_UnknownId_Throws()
        {
            Assert.Throws<KeyNotFoundException>(() => this._container.GetBase(Health));
        }

        [Test]
        public void GetValue_WithoutModifiers_EqualsBaseValue()
        {
            this._container.AddStat(CreateDefinition(Health, 100f));

            Assert.AreEqual(100f, this._container.GetValue(Health));
        }

        [Test]
        public void GetValue_IsClampedToMax()
        {
            this._container.AddStat(CreateDefinition(Health, 100f, min: 0f, max: 100f));

            this._container.SetBase(Health, 150f);

            Assert.AreEqual(100f, this._container.GetValue(Health));
        }

        [Test]
        public void SetBase_AboveMax_StoresRawBase_ButValueIsClamped()
        {
            this._container.AddStat(CreateDefinition(Health, 100f, min: 0f, max: 100f));

            this._container.SetBase(Health, 150f);

            Assert.AreEqual(150f, this._container.GetBase(Health));
            Assert.AreEqual(100f, this._container.GetValue(Health));
        }

        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        [TestCase(float.NegativeInfinity)]
        public void SetBase_NonFinite_Throws(float value)
        {
            this._container.AddStat(CreateDefinition(Health, 100f));

            Assert.Throws<ArgumentOutOfRangeException>(() => this._container.SetBase(Health, value));
            Assert.AreEqual(100f, this._container.GetBase(Health));
        }

        [Test]
        public void HasStat_ReflectsAddedStats()
        {
            Assert.IsFalse(this._container.HasStat(Health));

            this._container.AddStat(CreateDefinition(Health, 100f));

            Assert.IsTrue(this._container.HasStat(Health));
        }

        [Test]
        public void TryGetValue_UnknownId_ReturnsFalse()
        {
            Assert.IsFalse(this._container.TryGetValue(Health, out float value));
            Assert.AreEqual(0f, value);
        }

        [Test]
        public void TryGetValue_KnownId_ReturnsClampedValue()
        {
            this._container.AddStat(CreateDefinition(Health, 100f, min: 0f, max: 100f));
            this._container.SetBase(Health, 150f);

            Assert.IsTrue(this._container.TryGetValue(Health, out float value));
            Assert.AreEqual(100f, value);
        }

        [TestCase(StatRounding.None, 12.6f, 12.6f)]
        [TestCase(StatRounding.Floor, 12.9f, 12f)]
        [TestCase(StatRounding.Ceil, 12.1f, 13f)]
        [TestCase(StatRounding.Round, 12.4f, 12f)]
        [TestCase(StatRounding.Round, 12.5f, 13f)]
        [TestCase(StatRounding.Round, -2.5f, -3f)]
        [TestCase(StatRounding.Floor, -2.5f, -3f)]
        public void GetRoundedValue_AppliesRounding(StatRounding rounding, float baseValue, float expected)
        {
            this._container.AddStat(CreateDefinition(Health, 0f, rounding: rounding));

            this._container.SetBase(Health, baseValue);

            Assert.AreEqual(expected, this._container.GetRoundedValue(Health), 1e-5f);
            Assert.AreEqual(baseValue, this._container.GetValue(Health), 1e-5f, "GetValue must stay unrounded");
            Assert.AreEqual(baseValue, this._container.GetBase(Health), "base must stay unrounded");
        }

        [Test]
        public void GetRoundedValue_ClampsThenRounds_StaysWithinBounds()
        {
            this._container.AddStat(CreateDefinition(Health, 100f, min: 0f, max: 100f, rounding: StatRounding.Ceil));

            this._container.SetBase(Health, 99.2f);
            Assert.AreEqual(99.2f, this._container.GetValue(Health), 1e-5f);
            Assert.AreEqual(100f, this._container.GetRoundedValue(Health));

            this._container.SetBase(Health, 100.4f);
            Assert.AreEqual(100f, this._container.GetValue(Health));
            Assert.AreEqual(100f, this._container.GetRoundedValue(Health));
        }

        [Test]
        public void SetBase_SmallIncrements_AreNotLost()
        {
            this._container.AddStat(CreateDefinition(Health, 1000f));

            for (int i = 0; i < 1000; i++)
            {
                this._container.SetBase(Health, this._container.GetBase(Health) + 0.0008f);
            }

            Assert.Greater(this._container.GetBase(Health), 1000.5f);
        }

        [Test]
        public void Validate_RoundingWithFractionalBounds_Throws()
        {
            Assert.Throws<ArgumentException>(() =>
                CreateDefinition(Health, 10f, min: 0f, max: 10.5f, rounding: StatRounding.Round));
        }

        [Test]
        public void Validate_NoRoundingWithFractionalBounds_IsAllowed()
        {
            Assert.DoesNotThrow(() => CreateDefinition(Health, 0.1f, min: 0f, max: 0.75f));
        }

        [Test]
        public void SetBase_OnOneContainer_DoesNotAffectOtherContainerOrDefinition()
        {
            const float originBase = 100f;
            const float newBase = 50f;

            StatDefinition sharedDefinition = CreateDefinition(Health, originBase);

            StatContainer modifiedContainer = new StatContainer();
            modifiedContainer.AddStat(sharedDefinition);

            StatContainer otherContainer = new StatContainer();
            otherContainer.AddStat(sharedDefinition);

            modifiedContainer.SetBase(sharedDefinition.Id, newBase);

            Assert.AreEqual(newBase, modifiedContainer.GetBase(sharedDefinition.Id));
            Assert.AreEqual(originBase, otherContainer.GetBase(sharedDefinition.Id));
            Assert.AreEqual(originBase, otherContainer.GetValue(sharedDefinition.Id));
            Assert.AreEqual(originBase, sharedDefinition.BaseValue);
        }

        [Test]
        public void GetValue_AfterSetBase_ReturnsUpdatedValue()
        {
            const float originalBase = 100f;
            const float newBase = 50f;

            this._container.AddStat(CreateDefinition(Health, originalBase));
            Assert.AreEqual(originalBase, this._container.GetValue(Health), "precondition: value is cached");

            this._container.SetBase(Health, newBase);

            Assert.AreEqual(newBase, this._container.GetValue(Health), "SetBase must invalidate the cached value");
        }


        [Test]
        public void AddModifier_Add_IncreasesValue()
        {
            const float originalBase = 100f;
            const float resultValue = 110;

            this._container.AddStat(CreateDefinition(Health, originalBase));
            StatModifier statModifier = StatModifier.Create(Health, StandardStatModifierChannel.Add, 10);
            this._container.AddModifier(statModifier);

            Assert.AreEqual(resultValue, this._container.GetValue(Health));
        }

        [Test]
        public void AddModifier_AfterValueCached_RecalculatesValue()
        {
            const float originalBase = 100f;
            const float resultValue = 110;

            this._container.AddStat(CreateDefinition(Health, originalBase));
            Assert.AreEqual(originalBase, this._container.GetValue(Health));

            StatModifier statModifier = StatModifier.Create(Health, StandardStatModifierChannel.Add, 10);
            this._container.AddModifier(statModifier);

            Assert.AreEqual(resultValue, this._container.GetValue(Health));
        }

        [Test]
        public void AddModifier_UnknownStat_Throws()
        {
            this._container.AddStat(CreateDefinition(Health, 100));
            StatModifier statModifier = StatModifier.Create(Missing, StandardStatModifierChannel.Add, 100);

            Assert.Throws<KeyNotFoundException>(() => this._container.AddModifier(statModifier));
        }

        [Test]
        public void Validate_NoAggregator_Throws()
        {
            Assert.Throws<InvalidOperationException>(() => TestStatDefinitions.Create(Health, 100, noAggregator: true), $"[{Health}] Aggregator is not assigned");
        }

        [Test]
        public void RemoveModifier_RestoresValue()
        {
            const float originalBase = 100f;

            this._container.AddStat(CreateDefinition(Health, originalBase));
            StatModifierHandle statModifierHandle = this._container.AddModifier(StatModifier.Create(Health, StandardStatModifierChannel.Add, 10));
            this._container.RemoveModifier(statModifierHandle);

            Assert.AreEqual(originalBase, this._container.GetValue(Health));
        }

        [Test]
        public void RemoveModifier_Twice_ReturnsFalse()
        {
            const float originalBase = 100f;

            this._container.AddStat(CreateDefinition(Health, originalBase));
            StatModifierHandle statModifierHandle = this._container.AddModifier(StatModifier.Create(Health, StandardStatModifierChannel.Add, 10));

            Assert.AreEqual(true, this._container.RemoveModifier(statModifierHandle));
            Assert.AreEqual(false, this._container.RemoveModifier(statModifierHandle));
        }

        [Test]
        public void RemoveModifier_OneOfTwoSameValue_RemovesOnlyThatOne()
        {
            const float originalBase = 100f;
            const float resultValue = 110;

            this._container.AddStat(CreateDefinition(Health, originalBase));
            StatModifierHandle willBeRemovedHandle = this._container.AddModifier(StatModifier.Create(Health, StandardStatModifierChannel.Add, 10));
            this._container.AddModifier(StatModifier.Create(Health, StandardStatModifierChannel.Add, 10));

            this._container.RemoveModifier(willBeRemovedHandle);

            Assert.AreEqual(resultValue, this._container.GetValue(Health));
        }

        [Test]
        public void RemoveModifier_DefaultHandle_ReturnsFalse()
        {
            const float originalBase = 100f;
            const float resultValue = 110;

            this._container.AddStat(CreateDefinition(Health, originalBase));
            this._container.AddModifier(StatModifier.Create(Health, StandardStatModifierChannel.Add, 10));

            Assert.AreEqual(false, this._container.RemoveModifier(default));
            Assert.AreEqual(resultValue, this._container.GetValue(Health));
        }

        [Test]
        public void RemoveModifier_UnknownStat_ReturnsFalse()
        {
            const float originalBase = 100f;
            const float resultValue = 110;

            this._container.AddStat(CreateDefinition(Health, originalBase));
            StatModifierHandle real = this._container.AddModifier(StatModifier.Create(Health, StandardStatModifierChannel.Add, 10));
            // Same container and entry id, only the stat is unknown -> exercises the missing-node branch.
            var unknownStat = new StatModifierHandle(real.ContainerId, Missing, real.EntryId);

            Assert.AreEqual(false, this._container.RemoveModifier(unknownStat));
            Assert.AreEqual(resultValue, this._container.GetValue(Health));
        }

        [Test]
        public void RemoveModifier_StaleHandle_DoesNotRemoveNewModifier()
        {
            const float originalBase = 100f;
            const float resultValue = 110;

            this._container.AddStat(CreateDefinition(Health, originalBase));
            StatModifierHandle willBeRemovedHandle = this._container.AddModifier(StatModifier.Create(Health, StandardStatModifierChannel.Add, 10));
            Assert.IsTrue(this._container.RemoveModifier(willBeRemovedHandle));

            this._container.AddModifier(StatModifier.Create(Health, StandardStatModifierChannel.Add, 10));
            Assert.IsFalse(this._container.RemoveModifier(willBeRemovedHandle));

            Assert.AreEqual(resultValue, this._container.GetValue(Health));
        }

        [Test]
        public void RemoveModifier_HandleFromOtherContainer_DoesNotRemove()
        {
            var other = new StatContainer();
            this._container.AddStat(CreateDefinition(Health, 100f));
            other.AddStat(CreateDefinition(Health, 100f));

            StatModifierHandle handleFromThis = this._container.AddModifier(StatModifier.Create(Health, StandardStatModifierChannel.Add, 10));
            StatModifierHandle handleFromOther = other.AddModifier(StatModifier.Create(Health, StandardStatModifierChannel.Add, 10));

            // Precondition: same StatId + EntryId, so only ContainerId tells them apart.
            Assert.AreEqual(handleFromThis.EntryId, handleFromOther.EntryId);

            Assert.IsFalse(other.RemoveModifier(handleFromThis));
            Assert.AreEqual(110f, other.GetValue(Health));

            // The handle itself is fine: it still works on the container that issued it.
            Assert.IsTrue(this._container.RemoveModifier(handleFromThis));
            Assert.AreEqual(100f, this._container.GetValue(Health));
        }

        [Test]
        public void AddModifier_TwoAdds_Stack()
        {
            this._container.AddStat(CreateDefinition(Health, 100f));

            this._container.AddModifier(StatModifier.Create(Health, StandardStatModifierChannel.Add, 10));
            this._container.AddModifier(StatModifier.Create(Health, StandardStatModifierChannel.Add, 5));

            Assert.AreEqual(115f, this._container.GetValue(Health));
        }

        [Test]
        public void AddModifier_NegativeAdd_DecreasesValue()
        {
            this._container.AddStat(CreateDefinition(Health, 100f));

            this._container.AddModifier(StatModifier.Create(Health, StandardStatModifierChannel.Add, -30));

            Assert.AreEqual(70f, this._container.GetValue(Health));
        }

        [Test]
        public void AddModifier_ReturnsHandleWithStatId()
        {
            this._container.AddStat(CreateDefinition(Health, 100f));

            StatModifierHandle handle = this._container.AddModifier(StatModifier.Create(Health, StandardStatModifierChannel.Add, 10));

            Assert.AreEqual(Health, handle.StatId);
        }

        [Test]
        public void AddModifier_TwoAdds_ReturnDifferentHandles()
        {
            this._container.AddStat(CreateDefinition(Health, 100f));

            StatModifierHandle first = this._container.AddModifier(StatModifier.Create(Health, StandardStatModifierChannel.Add, 10));
            StatModifierHandle second = this._container.AddModifier(StatModifier.Create(Health, StandardStatModifierChannel.Add, 10));

            Assert.AreNotEqual(first, second);
        }

        [Test]
        public void Handle_SameValues_AreEqual()
        {
            var a = new StatModifierHandle(1, Health, 7);
            var b = new StatModifierHandle(1, Health, 7);

            Assert.IsTrue(a == b);
            Assert.AreEqual(a.GetHashCode(), b.GetHashCode());
        }

        [TestCase(2, "Health", 7)]
        [TestCase(1, "Mana", 7)]
        [TestCase(1, "Health", 8)]
        public void Handle_AnyFieldDiffers_NotEqual(int containerId, string statId, int entryId)
        {
            var a = new StatModifierHandle(1, Health, 7);
            var b = new StatModifierHandle(containerId, new StatId(statId), entryId);

            Assert.IsTrue(a != b);
        }

        [Test]
        public void Handle_WorksAsHashSetKey()
        {
            this._container.AddStat(CreateDefinition(Health, 100f));
            StatModifierHandle handle = this._container.AddModifier(StatModifier.Create(Health, StandardStatModifierChannel.Add, 10));

            var applied = new HashSet<StatModifierHandle> { handle };

            Assert.IsTrue(applied.Contains(handle));
            Assert.IsFalse(applied.Contains(default));
        }

        [Test]
        public void AddModifier_FirstHandle_IsNotDefault()
        {
            this._container.AddStat(CreateDefinition(Health, 100f));

            StatModifierHandle handle = this._container.AddModifier(StatModifier.Create(Health, StandardStatModifierChannel.Add, 10));

            Assert.IsTrue(handle.IsValid);
            Assert.AreNotEqual(default(StatModifierHandle), handle);
        }

        [Test]
        public void AddModifier_ResultAboveMax_IsClamped()
        {
            this._container.AddStat(CreateDefinition(Health, 100f, max: 120f));

            this._container.AddModifier(StatModifier.Create(Health, StandardStatModifierChannel.Add, 50));

            Assert.AreEqual(120f, this._container.GetValue(Health));
            Assert.AreEqual(100f, this._container.GetBase(Health));
        }

        [Test]
        public void AddModifier_DoesNotChangeBase()
        {
            this._container.AddStat(CreateDefinition(Health, 100f));

            this._container.AddModifier(StatModifier.Create(Health, StandardStatModifierChannel.Add, 10));

            Assert.AreEqual(100f, this._container.GetBase(Health));
        }
    }
}
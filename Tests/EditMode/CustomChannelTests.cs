using System;
using System.Collections.Generic;
using NUnit.Framework;
using StatForge.Tests.Shared;
using UnityEngine;
using UnityEngine.TestTools.Constraints;
using Is = UnityEngine.TestTools.Constraints.Is;
using Object = UnityEngine.Object;

namespace StatForge.Tests
{
    /// <summary>
    /// Proves a project can define its own channel enum and aggregator without touching the library (ADR-001).
    /// </summary>
    public class CustomChannelTests
    {
        // What a game project would write.
        public enum GameChannel
        {
            None = 0,
            Add,
            Multiply,
            FinalAdd, // applied after Multiply: not expressible with StandardStatModifierChannel
        }

        public enum ByteChannel : byte
        {
            None = 0,
            Add,
        }

        public sealed class GameAggregator : StatAggregator<GameChannel>
        {
            public override float Calculate(float baseValue, IReadOnlyList<StatModifierEntry> entries)
            {
                float add = 0f;
                float multiply = 1f;
                float finalAdd = 0f;

                for (int i = 0; i < entries.Count; i++)
                {
                    switch (ChannelOf(entries[i]))
                    {
                        case GameChannel.Add:
                            add += entries[i].Value;
                            break;
                        case GameChannel.Multiply:
                            multiply *= entries[i].Value;
                            break;
                        case GameChannel.FinalAdd:
                            finalAdd += entries[i].Value;
                            break;
                    }
                }

                return (baseValue + add) * multiply + finalAdd;
            }

            // Exposes the protected helper to the tests.
            public static GameChannel Read(in StatModifierEntry entry) => ChannelOf(entry);
        }

        private static readonly StatId Attack = new StatId("Attack");

        private GameAggregator _aggregator;

        [SetUp]
        public void SetUp()
        {
            this._aggregator = ScriptableObject.CreateInstance<GameAggregator>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(this._aggregator);
        }

        [Test]
        public void CustomChannel_RoundTrips()
        {
            StatModifier modifier = StatModifier.Create(Attack, GameChannel.FinalAdd, 1f);
            var entry = new StatModifierEntry(1, modifier);

            Assert.AreEqual(GameChannel.FinalAdd, GameAggregator.Read(entry));
        }

        [Test]
        public void CustomAggregator_FinalAdd_AppliedAfterMultiply()
        {
            StatDefinition definition = TestStatDefinitions.Create(Attack, 100f, aggregator: this._aggregator);
            try
            {
                var container = new StatContainer();
                container.AddStat(definition);

                container.AddModifier(StatModifier.Create(Attack, GameChannel.Multiply, 2f));
                container.AddModifier(StatModifier.Create(Attack, GameChannel.FinalAdd, 10f));

                // 100 × 2 + 10. Treating FinalAdd as a normal Add would give (100 + 10) × 2 = 220.
                Assert.AreEqual(210f, container.GetValue(Attack), 1e-4f);
            }
            finally
            {
                TestStatDefinitions.Destroy(definition);
            }
        }

        [Test]
        public void DefaultModifier_IsNoneInAnyEnum()
        {
            StatModifierEntry entry = default;

            Assert.AreEqual(GameChannel.None, GameAggregator.Read(entry));
        }

        [Test]
        public void ByteBackedEnum_Throws()
        {
            // The check lives in a static constructor, so the runtime wraps it in TypeInitializationException.
            var exception = Assert.Throws<TypeInitializationException>(
                () => StatModifier.Create(Attack, ByteChannel.Add, 1f));

            Assert.IsInstanceOf<InvalidOperationException>(exception.InnerException);
        }

        [Test]
        public void ChannelOf_DoesNotAllocate()
        {
            var entry = new StatModifierEntry(1, StatModifier.Create(Attack, GameChannel.Multiply, 2f));
            GameAggregator.Read(entry); // warm up: static constructor runs outside the measured block

            Assert.That(() => { GameAggregator.Read(entry); }, Is.Not.AllocatingGCMemory());
        }

        [Test]
        public void Create_DoesNotAllocate()
        {
            StatModifier.Create(Attack, GameChannel.Add, 1f); // warm up

            Assert.That(() => { StatModifier.Create(Attack, GameChannel.Add, 1f); }, Is.Not.AllocatingGCMemory());
        }
    }
}

using System;
using NUnit.Framework;
using StatForge.Tests.Shared;

namespace StatForge.Tests
{
    /// <summary>
    /// A project extends StatDefinition with its own data and checks, without changing the package.
    /// </summary>
    public class DerivedDefinitionTests
    {
        // What a game project would write: extra rule on top of the package's checks.
        public sealed class EvenBaseDefinition : StatDefinition
        {
            public int HookCalls { get; private set; }

            protected override void OnValidateDefinition()
            {
                HookCalls++;

                if (BaseValue % 2 != 0)
                {
                    throw new ArgumentException($"[{Id}] BaseValue must be even");
                }
            }
        }

        private static readonly StatId Health = new StatId("Health");

        [Test]
        public void DerivedDefinition_CustomValidation_Rejects()
        {
            Assert.Throws<ArgumentException>(() => TestStatDefinitions.Create<EvenBaseDefinition>(Health, 101f));
        }

        [Test]
        public void DerivedDefinition_CustomValidation_Accepts()
        {
            EvenBaseDefinition definition = TestStatDefinitions.Create<EvenBaseDefinition>(Health, 100f);
            try
            {
                Assert.AreEqual(1, definition.HookCalls);

                var container = new StatContainer();
                container.AddStat(definition);
                Assert.AreEqual(100f, container.GetValue(Health));
            }
            finally
            {
                TestStatDefinitions.Destroy(definition);
            }
        }

        [Test]
        public void DerivedDefinition_PackageChecksStillRun_BeforeHook()
        {
            // Min > Max is rejected by the package even though the subclass only checks parity,
            // and the hook is not reached (so it never sees an invalid definition).
            Assert.Throws<ArgumentException>(
                () => TestStatDefinitions.Create<EvenBaseDefinition>(Health, 100f, min: 200f, max: 50f),
                $"[{Health}] Min (200) > Max (50)");
        }
    }
}

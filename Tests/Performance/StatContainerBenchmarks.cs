using System.Collections.Generic;
using NUnit.Framework;
using StatForge.Tests.Shared;
using Unity.PerformanceTesting;

namespace StatForge.Tests.Performance
{
    /// <summary>
    /// Phase 1.5 baseline benchmarks. EditMode numbers include Editor overhead:
    /// compare them between phases, do not read them as build performance.
    /// </summary>
    public class StatContainerBenchmarks
    {
        private const int Warmup = 5;
        private const int Measurements = 20;
        private const int Iterations = 1000;

        private static readonly StatId Health = new StatId("Health");
        private static readonly StatId Missing = new StatId("Missing");

        private readonly List<StatDefinition> _createdDefinitions = new();

        [TearDown]
        public void TearDown()
        {
            foreach (StatDefinition definition in this._createdDefinitions)
            {
                TestStatDefinitions.Destroy(definition);
            }

            this._createdDefinitions.Clear();
        }

        private StatDefinition CreateDefinition(StatId id, float baseValue, StatRounding rounding = StatRounding.None)
        {
            StatDefinition definition = TestStatDefinitions.Create(id, baseValue, 0f, 1000f, rounding);
            this._createdDefinitions.Add(definition);
            return definition;
        }

        private StatDefinition[] CreateDefinitions(int count)
        {
            var definitions = new StatDefinition[count];
            for (int i = 0; i < count; i++)
            {
                definitions[i] = CreateDefinition(i == 0 ? Health : new StatId($"Stat{i}"), 100f, StatRounding.Floor);
            }

            return definitions;
        }

        private StatContainer CreateContainer(StatDefinition[] definitions)
        {
            var container = new StatContainer();
            foreach (StatDefinition definition in definitions)
            {
                container.AddStat(definition);
            }

            return container;
        }

        private static void Run(System.Action action, int iterations = Iterations)
        {
            Measure.Method(action)
                .WarmupCount(Warmup)
                .MeasurementCount(Measurements)
                .IterationsPerMeasurement(iterations)
                .GC()
                .Run();
        }

        [Test, Performance]
        public void GetValue_Cached()
        {
            StatContainer container = CreateContainer(CreateDefinitions(10));
            container.GetValue(Health);

            Run(() => container.GetValue(Health));
        }

        [Test, Performance]
        public void SetBase_ThenGetValue_Dirty()
        {
            StatContainer container = CreateContainer(CreateDefinitions(10));
            bool toggle = false;

            Run(() =>
            {
                toggle = !toggle;
                container.SetBase(Health, toggle ? 50f : 60f);
                container.GetValue(Health);
            });
        }

        [Test, Performance]
        public void SetBase_Only()
        {
            StatContainer container = CreateContainer(CreateDefinitions(10));
            bool toggle = false;

            Run(() =>
            {
                toggle = !toggle;
                container.SetBase(Health, toggle ? 50f : 60f);
            });
        }

        [Test, Performance]
        public void GetRoundedValue_Cached()
        {
            StatContainer container = CreateContainer(CreateDefinitions(10));
            container.GetValue(Health);

            Run(() => container.GetRoundedValue(Health));
        }

        [Test, Performance]
        public void TryGetValue_Miss()
        {
            StatContainer container = CreateContainer(CreateDefinitions(10));

            Run(() => container.TryGetValue(Missing, out _));
        }

        [Test, Performance]
        public void BuildContainer([Values(10, 50)] int statCount)
        {
            StatDefinition[] definitions = CreateDefinitions(statCount);

            Run(() => CreateContainer(definitions), 100);
        }

        [Test, Performance]
        public void ManyContainers_ReadOneStatEach([Values(1000, 10000)] int containerCount)
        {
            StatDefinition[] definitions = CreateDefinitions(10);
            var containers = new StatContainer[containerCount];
            for (int i = 0; i < containerCount; i++)
            {
                containers[i] = CreateContainer(definitions);
                containers[i].GetValue(Health);
            }

            // One measurement = one "frame" reading Health on every container.
            Run(() =>
            {
                float sum = 0f;
                for (int i = 0; i < containers.Length; i++)
                {
                    sum += containers[i].GetValue(Health);
                }
            }, 1);
        }
    }
}

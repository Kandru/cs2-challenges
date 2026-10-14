using BenchmarkDotNet.Attributes;
using Challenges.Utils;

namespace Challenges.Bench
{
    /// <summary>
    /// Event data for one <c>EventPlayerDeath</c> from fake values (no game reads): every key as text (old) vs.
    /// only the keys the loaded rules read, as <see cref="EventValue"/> (new).
    /// </summary>
    [MemoryDiagnoser]
    public class FillBenchmarks
    {
        private readonly FakeDeath _death = new();
        private FillDemand _demand = null!;

        /// <summary>Loaded blueprints: one weapon challenge, or all examples at once (widest demand).</summary>
        [Params("weapon_ak47", "all")]
        public string Blueprints { get; set; } = "weapon_ak47";

        [GlobalSetup]
        public void Setup()
        {
            _demand = BenchData.DeathDemand(BenchData.Schedule(Blueprints));
        }

        [Benchmark(Baseline = true, Description = "fill: all keys as strings")]
        public int Legacy()
        {
            Dictionary<string, string> data = [];
            LegacyFill.Fill(_death, data);
            return data.Count;
        }

        [Benchmark(Description = "fill: demand as EventValue")]
        public int Demand()
        {
            // Sized up front like the engine's event dictionaries.
            Dictionary<string, EventValue> data = new(_demand.Capacity);
            DemandFill.Fill(_death, data, _demand);
            return data.Count;
        }
    }
}

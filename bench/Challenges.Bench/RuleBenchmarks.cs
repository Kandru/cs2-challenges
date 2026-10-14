using BenchmarkDotNet.Attributes;
using Challenges.Utils;

namespace Challenges.Bench
{
    /// <summary>
    /// One kill checked against every <c>player_kill</c> task of the example blueprints, as the worker does per
    /// target: string rules over string data (old) vs. compiled rules over <see cref="EventValue"/> data (new).
    /// </summary>
    [MemoryDiagnoser]
    public class RuleBenchmarks
    {
        private ChallengeTask[] _tasks = [];
        private Dictionary<string, string> _legacy = [];
        private Dictionary<string, EventValue> _typed = [];

        /// <summary>Kill weapon; <c>ak47</c> passes the AK47 tasks, <c>awp</c> also reads the noscope rules.</summary>
        [Params("ak47", "awp")]
        public string Weapon { get; set; } = "ak47";

        [GlobalSetup]
        public void Setup()
        {
            RunningSchedule schedule = BenchData.Schedule("all");
            List<(ChallengeDefinition Challenge, ChallengeTask Task)> tasks = BenchData.TasksOfType(schedule, "player_kill");
            _tasks = [.. tasks.Select(t => t.Task)];
            (_legacy, _typed) = BenchData.KillData(new FakeDeath { Weapon = Weapon }, schedule, tasks);

            foreach (ChallengeTask task in _tasks)
            {
                bool old = LegacyRules.RulesPass(task, _legacy, false, false, out _);
                bool now = ChallengeRules.RulesPass(task, _typed, false, false, out _);
                if (old != now)
                {
                    throw new InvalidOperationException($"old and new rules disagree on {task.Id}");
                }
            }
        }

        [Benchmark(Baseline = true, Description = "rules: old strings")]
        public int Legacy()
        {
            int passed = 0;
            foreach (ChallengeTask task in _tasks)
            {
                if (LegacyRules.RulesPass(task, _legacy, false, false, out _))
                {
                    passed++;
                }
            }

            return passed;
        }

        [Benchmark(Description = "rules: CompiledRule + EventValue")]
        public int Compiled()
        {
            int passed = 0;
            foreach (ChallengeTask task in _tasks)
            {
                if (ChallengeRules.RulesPass(task, _typed, false, false, out _))
                {
                    passed++;
                }
            }

            return passed;
        }
    }
}

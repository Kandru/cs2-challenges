using BenchmarkDotNet.Attributes;
using Challenges.Utils;

namespace Challenges.Bench
{
    /// <summary>
    /// A series of kills for one player with progress on every example task: the worker copy cloned from the
    /// archive for every event (old seed per idle steam id) vs. one shadow kept across events (new). Rule checks,
    /// +1 and the apply to the archive are the same in both; amounts are restored at the start of each series.
    /// </summary>
    [MemoryDiagnoser]
    public class ShadowBenchmarks
    {
        private const int StartAmount = 1;

        private RunningSchedule _schedule = null!;
        private (ChallengeDefinition Challenge, ChallengeTask Task)[] _candidates = [];
        private Dictionary<string, EventValue> _data = [];
        private PlayerState _archive = null!;
        private PlayerState _shadow = null!;
        private TaskProgress[] _archiveProgress = [];
        private TaskProgress[] _shadowProgress = [];

        /// <summary>Kills in the series.</summary>
        [Params(20)]
        public int Events { get; set; } = 20;

        [GlobalSetup]
        public void Setup()
        {
            _schedule = BenchData.Schedule("all");
            List<(ChallengeDefinition Challenge, ChallengeTask Task)> kills = BenchData.TasksOfType(_schedule, "player_kill");
            _candidates = [.. kills];
            _data = BenchData.KillData(new FakeDeath(), _schedule, kills).Typed;
            _archive = BenchData.ProgressState(_schedule, StartAmount);
            _shadow = new PlayerState
            {
                SteamId = _archive.SteamId,
                Challenges = ChallengeProgress.CloneChallenges(_archive.Challenges),
            };
            _archiveProgress = AllProgress(_archive);
            _shadowProgress = AllProgress(_shadow);
            if (ClonePerEvent() != Persistent())
            {
                throw new InvalidOperationException("clone and shadow series disagree");
            }
        }

        [Benchmark(Baseline = true, Description = "shadow: CloneChallenges per event")]
        public int ClonePerEvent()
        {
            Restore();
            int applied = 0;
            for (int i = 0; i < Events; i++)
            {
                PlayerState copy = new()
                {
                    SteamId = _archive.SteamId,
                    Challenges = ChallengeProgress.CloneChallenges(_archive.Challenges),
                };
                applied += Simulate(copy, i);
            }

            return applied;
        }

        [Benchmark(Description = "shadow: persistent")]
        public int Persistent()
        {
            Restore();
            int applied = 0;
            for (int i = 0; i < Events; i++)
            {
                applied += Simulate(_shadow, i);
            }

            return applied;
        }

        /// <summary>Eligible tasks get +1 on <paramref name="worker"/>; the amounts are written to the archive.</summary>
        private int Simulate(PlayerState worker, int index)
        {
            long now = 1_000 + index;
            int applied = 0;
            foreach ((ChallengeDefinition challenge, ChallengeTask task) in _candidates)
            {
                if (ChallengeRules.IsSkipped(worker, BenchData.ScheduleKey, challenge, task, _data, now, false, false, out _))
                {
                    continue;
                }

                TaskProgress progress = ChallengeProgress.GetOrCreate(worker, BenchData.ScheduleKey, challenge.Id, task.Id);
                progress.Amount++;
                progress.LastUpdate = now;
                TaskProgress target = ChallengeProgress.GetOrCreate(_archive, BenchData.ScheduleKey, challenge.Id, task.Id);
                target.Amount = progress.Amount;
                target.LastUpdate = now;
                applied++;
            }

            return applied;
        }

        private void Restore()
        {
            foreach (TaskProgress progress in _archiveProgress)
            {
                progress.Amount = StartAmount;
                progress.LastUpdate = 1;
            }

            foreach (TaskProgress progress in _shadowProgress)
            {
                progress.Amount = StartAmount;
                progress.LastUpdate = 1;
            }
        }

        private static TaskProgress[] AllProgress(PlayerState state)
        {
            List<TaskProgress> all = [];
            foreach (Dictionary<string, Dictionary<string, TaskProgress>> challenges in state.Challenges.Values)
            {
                foreach (Dictionary<string, TaskProgress> tasks in challenges.Values)
                {
                    all.AddRange(tasks.Values);
                }
            }

            return [.. all];
        }
    }
}

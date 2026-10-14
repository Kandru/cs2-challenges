using System.Diagnostics;
using Challenges.Utils;

namespace Challenges.Tests
{
    /// <summary>
    /// A <see cref="ProgressPipeline"/> with a real worker; the test thread plays the game thread (enqueue,
    /// tick, queries). Applied patches are recorded instead of played to players.
    /// </summary>
    internal sealed class PipelineHarness : IDisposable
    {
        private static readonly TimeSpan SettleTimeout = TimeSpan.FromSeconds(20);

        private readonly Dictionary<string, ChallengeEngine.TypePlan> _types;
        private readonly HashSet<string> _steamIds = new(StringComparer.OrdinalIgnoreCase);

        public PipelineHarness(RunningSchedule schedule)
        {
            Schedule = schedule;
            _types = ChallengeEngine.BuildTypePlans(schedule, [], out _);
            Pipeline = new ProgressPipeline(Archive, (_, _) => { }, (patch, _) => Applied.Add(patch));
        }

        public RunningSchedule Schedule { get; }
        public PlayerArchive Archive { get; } = new();
        public ProgressPipeline Pipeline { get; }
        /// <summary>Patches with changes, in apply order.</summary>
        public List<ProgressPatch> Applied { get; } = [];
        public long Now { get; set; } = 1_000_000;
        public bool AllowBots { get; set; }

        public ChallengeEngine.TypePlan Type(string type) => _types[type];

        /// <summary>The open-type index the engine builds for this schedule.</summary>
        public OpenTypeIndex OpenTypes()
        {
            var tasksById = new List<(ChallengeDefinition Challenge, ChallengeTask Task)>[_types.Count];
            foreach (ChallengeEngine.TypePlan plan in _types.Values)
            {
                tasksById[plan.Id] = plan.Tasks;
            }

            OpenTypeIndex index = new();
            index.Rebuild(tasksById, Schedule.Key);
            return index;
        }

        /// <summary>The engine's <c>FilterTargets</c> skip: nothing left to earn and no work in flight.</summary>
        public bool Skips(OpenTypeIndex index, string steamId, string type) =>
            !index.IsOpen(Archive.GetOrCreate(steamId), _types[type].Id) && !Pipeline.HasPending(steamId);

        /// <summary>One work item; each target gets the candidates of its type like <c>FilterTargets</c> gives them.</summary>
        public ProgressWorkItem Item(Dictionary<string, EventValue> data, params (string SteamId, string Type)[] targets)
        {
            List<ProgressTarget> list = new(targets.Length);
            foreach ((string steamId, string type) in targets)
            {
                _steamIds.Add(steamId);
                list.Add(new ProgressTarget
                {
                    SteamId = steamId,
                    Slot = list.Count,
                    UserId = 100 + list.Count,
                    Name = string.Empty,
                    Index = (uint)(list.Count + 1),
                    Candidates = _types[type].Tasks,
                });
            }

            return new ProgressWorkItem
            {
                Source = "test",
                Data = data,
                ScheduleKey = Schedule.Key,
                Schedule = Schedule,
                Debug = false,
                AllowBots = AllowBots,
                Now = Now,
                Targets = list,
            };
        }

        public ProgressWorkItem Enqueue(Dictionary<string, EventValue> data, params (string SteamId, string Type)[] targets)
        {
            ProgressWorkItem item = Item(data, targets);
            Assert.True(Pipeline.TryEnqueue(item));
            return item;
        }

        public bool AnyPending()
        {
            foreach (string steamId in _steamIds)
            {
                if (Pipeline.HasPending(steamId))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>Ticks like the game thread until no steam id has work in flight.</summary>
        public void Settle()
        {
            Stopwatch watch = Stopwatch.StartNew();
            while (AnyPending())
            {
                if (watch.Elapsed > SettleTimeout)
                {
                    throw new TimeoutException("progress pipeline did not settle");
                }

                Pipeline.OnTick();
                Thread.Yield();
            }
        }

        public int Amount(string steamId, string challengeId, string taskId) =>
            Archive.TryGet(steamId) is { } state
                ? ChallengeProgress.GetTaskAmount(state, Schedule.Key, challengeId, taskId)
                : 0;

        public void Dispose() => Pipeline.Flush();
    }
}

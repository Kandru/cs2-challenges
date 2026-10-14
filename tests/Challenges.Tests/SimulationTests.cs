using Challenges.Utils;

namespace Challenges.Tests
{
    /// <summary>Worker simulation of single events (<c>ProgressPipeline.Simulate</c>) and the applied result.</summary>
    public sealed class SimulationTests
    {
        private const string Player = "76561198000000001";
        private static readonly Dictionary<string, EventValue> AnyKill = TestData.Data(("weapon", EventValue.Of("ak47")));

        private static List<ProgressEffectKind> Kinds(ProgressPatch patch) => patch.Effects?.ConvertAll(effect => effect.Kind) ?? [];

        private static ProgressPatch Run(PipelineHarness h, string type = "player_kill", Dictionary<string, EventValue>? data = null)
        {
            int before = h.Applied.Count;
            h.Enqueue(data ?? AnyKill, (Player, type));
            h.Settle();
            Assert.Equal(before + 1, h.Applied.Count);
            return h.Applied[^1];
        }

        private static void RunWithoutChanges(PipelineHarness h, string type = "player_kill", Dictionary<string, EventValue>? data = null)
        {
            int before = h.Applied.Count;
            h.Enqueue(data ?? AnyKill, (Player, type));
            h.Settle();
            Assert.Equal(before, h.Applied.Count);
        }

        [Fact]
        public void Progress_adds_one_and_notifies()
        {
            using PipelineHarness h = new(TestData.Schedule(TestData.Challenge("c", TestData.Task("t", "player_kill", amount: 3))));

            ProgressPatch patch = Run(h);

            Assert.Equal(1, h.Amount(Player, "c", "t"));
            Assert.Equal(h.Now, h.Archive.TryGet(Player)!.Challenges[TestData.ScheduleKey]["c"]["t"].LastUpdate);
            Assert.Equal([ProgressEffectKind.NotifyProgress, ProgressEffectKind.TriggerProgress], Kinds(patch));
            Assert.Equal(1, patch.Effects![0].Amount);
            Assert.Null(patch.SolvedAbsolute);
            Assert.Equal(TrackerProgressKind.Progress, Assert.Single(patch.Touched!).Kind);
            Assert.True(h.Archive.TryGet(Player)!.Dirty);
        }

        [Fact]
        public void Completion_caps_at_amount()
        {
            ChallengeDefinition c = TestData.Challenge("c",
                TestData.Task("t", "player_kill", amount: 2),
                TestData.Task("other", "player_kill", amount: 100));
            using PipelineHarness h = new(TestData.Schedule(c));

            Run(h);
            ProgressPatch done = Run(h);
            Assert.Equal(2, h.Amount(Player, "c", "t"));
            Assert.Contains(ProgressEffectKind.NotifyCompletion, Kinds(done));
            Assert.Contains(ProgressEffectKind.DiscordCompletion, Kinds(done));
            Assert.Contains(ProgressEffectKind.TriggerCompleted, Kinds(done));
            Assert.DoesNotContain(ProgressEffectKind.TriggerChallengeSolved, Kinds(done));

            Run(h);
            Assert.Equal(2, h.Amount(Player, "c", "t"));
            Assert.Equal(3, h.Amount(Player, "c", "other"));
        }

        [Fact]
        public void Challenge_is_solved_by_its_visible_tasks_only()
        {
            ChallengeDefinition c = TestData.Challenge("c",
                TestData.Task("shown", "player_kill", amount: 1),
                TestData.Task("hidden", "player_kill", amount: 5, visible: false));
            using PipelineHarness h = new(TestData.Schedule(c));

            ProgressPatch patch = Run(h);

            Assert.Equal(1, patch.SolvedAbsolute);
            Assert.Equal(1, h.Archive.TryGet(Player)!.Statistics.AmountChallengesSolved);
            Assert.Contains(ProgressEffectKind.TriggerChallengeSolved, Kinds(patch));
            ProgressEffect discord = patch.Effects!.Find(effect => effect.Kind == ProgressEffectKind.DiscordCompletion);
            Assert.True(discord.ChallengeSolvedNow);
            // The hidden task progressed but is not shown on the tracker.
            Assert.Equal(1, h.Amount(Player, "c", "hidden"));
            TrackerProgressItem touched = Assert.Single(patch.Touched!);
            Assert.Equal(("shown", TrackerProgressKind.ChallengeSolved), (touched.TaskId, touched.Kind));

            // Solved once only.
            for (int i = 0; i < 4; i++)
            {
                Assert.Null(Run(h).SolvedAbsolute);
            }

            Assert.Equal(1, h.Archive.TryGet(Player)!.Statistics.AmountChallengesSolved);
        }

        [Fact]
        public void Hidden_tasks_never_solve_a_challenge()
        {
            ChallengeDefinition c = TestData.Challenge("c", TestData.Task("hidden", "player_kill", amount: 1, visible: false));
            using PipelineHarness h = new(TestData.Schedule(c));

            ProgressPatch patch = Run(h);

            Assert.Equal(1, h.Amount(Player, "c", "hidden"));
            Assert.Null(patch.SolvedAbsolute);
            Assert.Null(patch.Touched);
            Assert.Equal(0, h.Archive.TryGet(Player)!.Statistics.AmountChallengesSolved);
        }

        [Fact]
        public void Requires_locks_until_the_required_task_was_complete_before_the_event()
        {
            ChallengeDefinition c = TestData.Challenge("c",
                TestData.Task("first", "player_kill", amount: 1),
                TestData.Task("second", "player_kill", amount: 2, requires: ["first"]));
            using PipelineHarness h = new(TestData.Schedule(c));

            Run(h);
            Assert.Equal(1, h.Amount(Player, "c", "first"));
            Assert.Equal(0, h.Amount(Player, "c", "second"));

            Run(h);
            Assert.Equal(1, h.Amount(Player, "c", "second"));
        }

        [Fact]
        public void Cooldown_skips_until_it_expired()
        {
            using PipelineHarness h = new(TestData.Schedule(TestData.Challenge("c",
                TestData.Task("t", "player_kill", amount: 5, cooldown: 60))));
            h.Now = 1000;
            Run(h);

            h.Now = 1059;
            RunWithoutChanges(h);
            Assert.Equal(1, h.Amount(Player, "c", "t"));

            h.Now = 1060;
            Run(h);
            Assert.Equal(2, h.Amount(Player, "c", "t"));
        }

        [Fact]
        public void Failing_rule_gives_no_progress()
        {
            using PipelineHarness h = new(TestData.Schedule(TestData.Challenge("c",
                TestData.Task("t", "player_kill", amount: 5, rules: [TestData.Rule("weapon", "==", "awp")]))));

            RunWithoutChanges(h);
            Run(h, data: TestData.Data(("weapon", EventValue.Of("AWP"))));
            Assert.Equal(1, h.Amount(Player, "c", "t"));
        }

        [Fact]
        public void Reset_progress_removes_an_unfinished_task_only()
        {
            ChallengeDefinition c = TestData.Challenge("c",
                TestData.Task("kills", "player_kill", amount: 3),
                TestData.Task("streak", "player_kill", amount: 1),
                TestData.Task("death", "player_death", amount: 1,
                    actions: [TestData.Action("task.reset_progress", "kills", "streak", "missing")]));
            using PipelineHarness h = new(TestData.Schedule(c));
            Run(h);
            Run(h);
            Assert.Equal(2, h.Amount(Player, "c", "kills"));
            Assert.Equal(1, h.Amount(Player, "c", "streak"));

            ProgressPatch patch = Run(h, "player_death");

            Assert.Equal(0, h.Amount(Player, "c", "kills"));
            Assert.Equal(1, h.Amount(Player, "c", "streak"));
            Assert.Equal([("c", "kills")], patch.Removals);
            ProgressEffect reset = Assert.Single(patch.Effects!, effect => effect.Kind == ProgressEffectKind.NotifyTaskReset);
            Assert.Equal("kills", reset.Task!.Id);
            Assert.False(h.Archive.TryGet(Player)!.Challenges[TestData.ScheduleKey]["c"].ContainsKey("kills"));
        }

        [Fact]
        public void Reset_completed_removes_a_finished_task_only()
        {
            ChallengeDefinition c = TestData.Challenge("c",
                TestData.Task("kills", "player_kill", amount: 3),
                TestData.Task("streak", "player_kill", amount: 1, visible: false),
                TestData.Task("death", "player_death", amount: 1,
                    actions: [TestData.Action("task.reset_completed", "kills", "streak")]));
            using PipelineHarness h = new(TestData.Schedule(c));
            Run(h);

            ProgressPatch patch = Run(h, "player_death");

            Assert.Equal(1, h.Amount(Player, "c", "kills"));
            Assert.Equal(0, h.Amount(Player, "c", "streak"));
            Assert.Equal([("c", "streak")], patch.Removals);
            // Hidden tasks reset silently.
            Assert.DoesNotContain(ProgressEffectKind.NotifyTaskReset, Kinds(patch));
        }

        [Fact]
        public void Mark_completed_raises_a_task_to_its_amount()
        {
            ChallengeDefinition c = TestData.Challenge("c",
                TestData.Task("kills", "player_kill", amount: 3),
                TestData.Task("bonus", "player_death", amount: 1,
                    actions: [TestData.Action("task.mark_completed", "kills")]));
            using PipelineHarness h = new(TestData.Schedule(c));
            Run(h);

            ProgressPatch patch = Run(h, "player_death");

            Assert.Equal(3, h.Amount(Player, "c", "kills"));
            Assert.Contains(patch.Upserts!, upsert => upsert is { TaskId: "kills", Amount: 3 });
            Assert.Equal(h.Now, h.Archive.TryGet(Player)!.Challenges[TestData.ScheduleKey]["c"]["kills"].LastUpdate);
        }

        [Theory]
        [InlineData(0, new string[0])]
        [InlineData(1, new[] { "progress" })]
        [InlineData(2, new[] { "progress", "completed" })]
        public void Rule_broken_notifies_when_the_watched_task_has_progress_or_is_complete(int kills, string[] expected)
        {
            ChallengeDefinition c = TestData.Challenge("c",
                TestData.Task("kills", "player_kill", amount: 2),
                TestData.Task("progress", "player_death", amount: 1,
                    actions: [TestData.Action("notify.player.progress.rule_broken", "kills")]),
                TestData.Task("completed", "player_death", amount: 1,
                    actions: [TestData.Action("notify.player.completed.rule_broken", "kills")]));
            using PipelineHarness h = new(TestData.Schedule(c));
            for (int i = 0; i < kills; i++)
            {
                Run(h);
            }

            ProgressPatch death = Run(h, "player_death");

            List<string> notified = death.Effects!
                .FindAll(effect => effect.Kind == ProgressEffectKind.NotifyRuleBroken)
                .ConvertAll(effect => effect.Task!.Id);
            Assert.Equal(expected, notified);
        }

        [Fact]
        public void Server_command_fills_placeholders()
        {
            ChallengeDefinition c = TestData.Challenge("c", TestData.Task("t", "player_kill", amount: 1,
                actions: [TestData.Action("server.runcommand", "css_give {steamid} #{userid} @{index}")]));
            using PipelineHarness h = new(TestData.Schedule(c));

            ProgressPatch patch = Run(h);

            ProgressEffect command = Assert.Single(patch.Effects!, effect => effect.Kind == ProgressEffectKind.ServerCommand);
            Assert.Equal($"css_give {Player} #100 @1", command.Command);
        }
    }
}

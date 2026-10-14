using Challenges.Utils;

namespace Challenges.Tests
{
    /// <summary>
    /// <see cref="ProgressPipeline"/> under load and at its edges: game-thread bookkeeping (S1/S2), persistent
    /// shadows (T1.3), the open-type filter with a reset in flight (T1.4) and the worker failure path.
    /// </summary>
    public sealed class PipelineTests
    {
        private const int Players = 200;
        private const int EventsPerPlayer = 1000;
        /// <summary>Players [0, Churn) also disconnect and get outside progress changes during the run.</summary>
        private const int Churn = 20;

        private static readonly Dictionary<string, EventValue> AnyEvent = TestData.Data(("weapon", EventValue.Of("ak47")));

        [Fact]
        public void Interleaved_enqueue_and_apply_keep_exact_progress()
        {
            ChallengeDefinition grind = TestData.Challenge("grind",
                TestData.Task("ak", "player_kill", amount: 1_000_000, rules: [TestData.Rule("weapon", "==", "ak47")]),
                TestData.Task("hs", "player_kill", amount: 50, rules: [TestData.Rule("headshot", "bool==", "true")]),
                TestData.Task("any", "player_kill", amount: 300));
            ChallengeDefinition quick = TestData.Challenge("quick",
                TestData.Task("ten", "player_kill", amount: 10),
                TestData.Task("deaths", "player_death", amount: 400));
            using PipelineHarness h = new(TestData.Schedule(grind, quick));

            Dictionary<string, EventValue>[] shapes =
            [
                TestData.Data(("weapon", EventValue.Of("ak47")), ("headshot", EventValue.Of(true))),
                TestData.Data(("weapon", EventValue.Of("ak47")), ("headshot", EventValue.Of(false))),
                TestData.Data(("weapon", EventValue.Of("m4a1")), ("headshot", EventValue.Of(true))),
                TestData.Data(("weapon", EventValue.Of("m4a1")), ("headshot", EventValue.Of(false))),
            ];
            string[] ids = new string[Players];
            for (int p = 0; p < Players; p++)
            {
                ids[p] = TestData.SteamId(p);
            }

            int[] kills = new int[Players];
            int[] akKills = new int[Players];
            int[] headshots = new int[Players];
            int[] deaths = new int[Players];
            bool[] armed = new bool[Players];
            List<int> saveCalls = [];
            int deferredSaves = 0;
            bool pendingDuringSave = false;
            int[] seeds = new int[Players];
            Random random = new(4711);

            for (int n = 0; n < Players * EventsPerPlayer; n++)
            {
                int killer = random.Next(Players);
                int victim = (killer + 1 + random.Next(Players - 1)) % Players;
                int shape = random.Next(shapes.Length);
                ProgressWorkItem item = h.Enqueue(shapes[shape], (ids[killer], "player_kill"), (ids[victim], "player_death"));
                seeds[killer] += item.Targets[0].Seed != null ? 1 : 0;
                seeds[victim] += item.Targets[1].Seed != null ? 1 : 0;

                kills[killer]++;
                akKills[killer] += shape < 2 ? 1 : 0;
                headshots[killer] += shape % 2 == 0 ? 1 : 0;
                deaths[victim]++;

                // The game thread applies a budget of patches between events, like the plugin tick. It enqueues
                // faster than the worker simulates, so it waits for the worker now and then (a quiet moment).
                if (n % 5 == 0)
                {
                    h.Pipeline.OnTick();
                }

                if (n % 1000 == 999)
                {
                    h.Settle();
                }

                // Disconnects drop the shadow once idle; outside changes (prune, adoption, admin) bump the version.
                // Either way the next seed must start from an archive that already holds every applied patch.
                if (n % 89 == 0)
                {
                    int p = random.Next(Churn);
                    if (random.Next(2) == 0)
                    {
                        h.Pipeline.DiscardShadow(ids[p]);
                    }
                    else
                    {
                        h.Archive.GetOrCreate(ids[p]).ProgressVersion++;
                    }
                }

                // Disconnect saves: deferred while work is in flight, at most one outstanding per player.
                if (n % 211 == 0)
                {
                    int p = random.Next(Players);
                    if (!armed[p])
                    {
                        int registration = saveCalls.Count;
                        saveCalls.Add(0);
                        armed[p] = true;
                        string steamId = ids[p];
                        deferredSaves += h.Pipeline.HasPending(steamId) ? 1 : 0;
                        h.Pipeline.WhenIdle(steamId, () =>
                        {
                            saveCalls[registration]++;
                            pendingDuringSave |= h.Pipeline.HasPending(steamId);
                            armed[p] = false;
                        });
                    }
                }
            }

            h.Settle();

            for (int p = 0; p < Players; p++)
            {
                string steamId = ids[p];
                Assert.False(h.Pipeline.HasPending(steamId));
                Assert.Equal(akKills[p], h.Amount(steamId, "grind", "ak"));
                Assert.Equal(Math.Min(50, headshots[p]), h.Amount(steamId, "grind", "hs"));
                Assert.Equal(Math.Min(300, kills[p]), h.Amount(steamId, "grind", "any"));
                Assert.Equal(Math.Min(10, kills[p]), h.Amount(steamId, "quick", "ten"));
                Assert.Equal(Math.Min(400, deaths[p]), h.Amount(steamId, "quick", "deaths"));
                int solved = kills[p] >= 10 && deaths[p] >= 400 ? 1 : 0;
                Assert.Equal(solved, h.Archive.TryGet(steamId)!.Statistics.AmountChallengesSolved);
            }

            Assert.All(saveCalls, calls => Assert.Equal(1, calls));
            Assert.True(deferredSaves > 0, "no save was deferred; the test did not cover the in-flight path");
            Assert.False(pendingDuringSave);
            // Without outside changes a player is cloned exactly once (T1.3); churn players are reseeded.
            for (int p = 0; p < Players; p++)
            {
                if (p < Churn)
                {
                    Assert.True(seeds[p] > 1, $"player {p} was never reseeded");
                }
                else
                {
                    Assert.Equal(1, seeds[p]);
                }
            }
        }

        [Fact]
        public void Shadow_is_reseeded_only_after_an_external_change_while_idle()
        {
            const string player = "76561198000000001";
            using PipelineHarness h = new(TestData.Schedule(TestData.Challenge("c", TestData.Task("t", "player_kill", amount: 100))));

            Assert.NotNull(Seed(h.Enqueue(AnyEvent, (player, "player_kill"))));
            h.Settle();
            Assert.Null(Seed(h.Enqueue(AnyEvent, (player, "player_kill"))));
            h.Settle();
            Assert.Equal(2, h.Amount(player, "c", "t"));

            // Admin reset while idle: the next event reseeds from the archive.
            PlayerState state = h.Archive.TryGet(player)!;
            state.Challenges.Clear();
            state.ProgressVersion++;
            Assert.NotNull(Seed(h.Enqueue(AnyEvent, (player, "player_kill"))));
            h.Settle();
            Assert.Equal(1, h.Amount(player, "c", "t"));

            // A change while work is in flight is not seeded: the shadow is ahead and its patches win.
            Assert.Null(Seed(h.Enqueue(AnyEvent, (player, "player_kill"))));
            ChallengeProgress.GetOrCreate(state, TestData.ScheduleKey, "c", "t").Amount = 50;
            state.ProgressVersion++;
            Assert.Null(Seed(h.Enqueue(AnyEvent, (player, "player_kill"))));
            h.Settle();
            Assert.Equal(3, h.Amount(player, "c", "t"));
            // Idle again: the version still differs from the seed, so the next event reseeds once.
            Assert.NotNull(Seed(h.Enqueue(AnyEvent, (player, "player_kill"))));
            h.Settle();
            Assert.Equal(4, h.Amount(player, "c", "t"));
            Assert.Null(Seed(h.Enqueue(AnyEvent, (player, "player_kill"))));
            h.Settle();
            Assert.Equal(5, h.Amount(player, "c", "t"));

            // A change without a version bump is invisible to the worker.
            ChallengeProgress.GetOrCreate(state, TestData.ScheduleKey, "c", "t").Amount = 50;
            Assert.Null(Seed(h.Enqueue(AnyEvent, (player, "player_kill"))));
            h.Settle();
            Assert.Equal(6, h.Amount(player, "c", "t"));
        }

        [Fact]
        public void Discarded_shadow_is_dropped_when_idle_and_reseeded()
        {
            const string player = "76561198000000002";
            using PipelineHarness h = new(TestData.Schedule(TestData.Challenge("c", TestData.Task("t", "player_kill", amount: 100))));
            h.Enqueue(AnyEvent, (player, "player_kill"));
            h.Settle();

            // Disconnect while in flight: dropped after the last patch, not before.
            h.Enqueue(AnyEvent, (player, "player_kill"));
            h.Pipeline.DiscardShadow(player);
            Assert.Null(Seed(h.Enqueue(AnyEvent, (player, "player_kill"))));
            h.Settle();
            Assert.Equal(3, h.Amount(player, "c", "t"));
            Assert.NotNull(Seed(h.Enqueue(AnyEvent, (player, "player_kill"))));
            h.Settle();

            h.Pipeline.DiscardShadow(player);
            Assert.NotNull(Seed(h.Enqueue(AnyEvent, (player, "player_kill"))));
            h.Settle();
            Assert.Equal(5, h.Amount(player, "c", "t"));
        }

        [Fact]
        public void Open_type_filter_never_drops_an_event_while_a_reset_is_in_flight()
        {
            const string player = "76561198000000003";
            ChallengeDefinition c = TestData.Challenge("c",
                TestData.Task("kill", "player_kill", amount: 1),
                TestData.Task("death", "player_death", amount: 2,
                    actions: [TestData.Action("task.reset_completed", "kill")]));
            using PipelineHarness h = new(TestData.Schedule(c));
            OpenTypeIndex open = h.OpenTypes();

            Assert.False(h.Skips(open, player, "player_kill"));
            h.Enqueue(AnyEvent, (player, "player_kill"));
            h.Settle();
            Assert.Equal(1, h.Amount(player, "c", "kill"));
            Assert.True(h.Skips(open, player, "player_kill"));

            // First death: no completion, no reset; kills stay closed.
            h.Enqueue(AnyEvent, (player, "player_death"));
            h.Settle();
            Assert.True(h.Skips(open, player, "player_kill"));

            // Second death completes and resets the kill task; a kill arrives before its patch is applied.
            h.Enqueue(AnyEvent, (player, "player_death"));
            Assert.False(open.IsOpen(h.Archive.GetOrCreate(player), h.Type("player_kill").Id));
            Assert.False(h.Skips(open, player, "player_kill"));
            h.Enqueue(AnyEvent, (player, "player_kill"));
            h.Settle();

            Assert.Equal(2, h.Amount(player, "c", "death"));
            Assert.Equal(1, h.Amount(player, "c", "kill"));
            Assert.True(h.Skips(open, player, "player_kill"));
            Assert.True(h.Skips(open, player, "player_death"));
        }

        [Fact]
        public void Applied_reset_reopens_the_type()
        {
            const string player = "76561198000000004";
            ChallengeDefinition c = TestData.Challenge("c",
                TestData.Task("kill", "player_kill", amount: 1),
                TestData.Task("death", "player_death", amount: 1,
                    actions: [TestData.Action("task.reset_completed", "kill")]));
            using PipelineHarness h = new(TestData.Schedule(c));
            OpenTypeIndex open = h.OpenTypes();
            h.Enqueue(AnyEvent, (player, "player_kill"));
            h.Settle();
            Assert.True(h.Skips(open, player, "player_kill"));

            h.Enqueue(AnyEvent, (player, "player_death"));
            h.Settle();

            Assert.Equal(0, h.Amount(player, "c", "kill"));
            Assert.False(h.Skips(open, player, "player_kill"));
            // A progress change outside the pipeline is seen through the version.
            PlayerState state = h.Archive.GetOrCreate(player);
            ChallengeProgress.GetOrCreate(state, TestData.ScheduleKey, "c", "kill").Amount = 1;
            state.ProgressVersion++;
            Assert.True(h.Skips(open, player, "player_kill"));
        }

        [Fact]
        public void Failed_target_finishes_pending_runs_the_save_and_drops_the_shadow()
        {
            const string player = "76561198000000005";
            // A challenge without id makes the worker throw while simulating that target.
            ChallengeDefinition broken = TestData.Challenge(null!, TestData.Task("t", "player_death"));
            ChallengeDefinition good = TestData.Challenge("good", TestData.Task("t", "player_kill", amount: 10));
            using PipelineHarness h = new(TestData.Schedule(broken, good));
            int saves = 0;

            h.Enqueue(AnyEvent, (player, "player_kill"), (player, "player_death"));
            h.Pipeline.WhenIdle(player, () => saves++);
            Assert.Equal(0, saves);
            h.Settle();

            Assert.Equal(1, saves);
            Assert.False(h.Pipeline.HasPending(player));
            Assert.Equal(1, h.Amount(player, "good", "t"));
            Assert.NotNull(Seed(h.Enqueue(AnyEvent, (player, "player_kill"))));
            h.Settle();
            Assert.Equal(2, h.Amount(player, "good", "t"));
        }

        [Fact]
        public void Idle_save_runs_at_once()
        {
            using PipelineHarness h = new(TestData.Schedule(TestData.Challenge("c", TestData.Task("t", "player_kill"))));
            int saves = 0;
            h.Pipeline.WhenIdle("76561198000000006", () => saves++);
            h.Pipeline.WhenIdle("BOT", () => saves++);
            Assert.Equal(2, saves);
        }

        [Fact]
        public void Flush_applies_everything_and_runs_deferred_saves()
        {
            using PipelineHarness h = new(TestData.Schedule(TestData.Challenge("c", TestData.Task("t", "player_kill", amount: 10_000))));
            int saves = 0;
            for (int i = 0; i < 2000; i++)
            {
                h.Enqueue(AnyEvent, (TestData.SteamId(i % 10), "player_kill"));
            }

            h.Pipeline.WhenIdle(TestData.SteamId(3), () => saves++);
            h.Pipeline.Flush();

            Assert.Equal(1, saves);
            Assert.False(h.AnyPending());
            for (int p = 0; p < 10; p++)
            {
                Assert.Equal(200, h.Amount(TestData.SteamId(p), "c", "t"));
            }

            Assert.True(h.Pipeline.IsFlushed);
            Assert.False(h.Pipeline.TryEnqueue(h.Item(AnyEvent, (TestData.SteamId(0), "player_kill"))));
            h.Pipeline.OnTick();
            Assert.Equal(200, h.Amount(TestData.SteamId(0), "c", "t"));
        }

        private static ProgressSeed? Seed(ProgressWorkItem item) => Assert.Single(item.Targets).Seed;
    }
}

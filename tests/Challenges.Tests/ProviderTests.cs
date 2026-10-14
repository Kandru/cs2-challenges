using Challenges.Attributes;
using Challenges.Utils;

namespace Challenges.Tests
{
    /// <summary>Attribute providers fed through slot data instead of game events and controllers.</summary>
    public sealed class ProviderTests
    {
        private const int T = 2;
        private const int CT = 3;

        private static ChallengeTask[] Tasks(params ChallengeTask[] tasks) => [.. TestData.Challenge("c", tasks).Tasks];

        /// <summary>Ace provider for the default bounds (2..5) plus <c>isace</c> with <c>min_players: 1</c>.</summary>
        private static AceProvider Ace(RoundRoster roster)
        {
            AceProvider ace = new(roster);
            Assert.True(ace.Configure(Tasks(
                TestData.Task("ace", "player_kill", rules: [TestData.Rule("isace", "bool==", "true")]),
                TestData.Task("duel", "player_kill", rules: [TestData.Rule("isace", "bool==", "true", min: 1, max: 1)]))));
            return ace;
        }

        /// <summary>A built roster: slots [0, ts) human terrorists, then <paramref name="cts"/> human CTs.</summary>
        private static RoundRoster Roster(int ts, int cts, int ctBots = 0) => Fill(new RoundRoster(), ts, cts, ctBots);

        /// <summary>Rebuilds <paramref name="roster"/> empty and places the players like <see cref="Roster"/>.</summary>
        private static RoundRoster Fill(RoundRoster roster, int ts, int cts, int ctBots = 0)
        {
            roster.RebuildEmpty();
            int slot = 0;
            for (int i = 0; i < ts; i++)
            {
                roster.Set(slot++, T, human: true);
            }

            for (int i = 0; i < cts; i++)
            {
                roster.Set(slot++, CT, human: true);
            }

            for (int i = 0; i < ctBots; i++)
            {
                roster.Set(slot++, CT, human: false);
            }

            return roster;
        }

        /// <summary>Attacker kills victim, returns the written ace keys. The roster keeps dead players.</summary>
        private static Dictionary<string, EventValue> Kill(AceProvider ace, RoundRoster roster, int attacker, int victim)
        {
            ace.OnHumanKill(attacker, roster.TeamOf(attacker), victim, roster.TeamOf(victim));
            Dictionary<string, EventValue> d = [];
            ace.WriteEvent("EventPlayerDeath", null!, d, 1);
            return d;
        }

        private static bool Bool(EventValue value)
        {
            Assert.True(value.TryGetBool(out bool b));
            return b;
        }

        private static bool IsAce(Dictionary<string, EventValue> d) => Bool(d["isace.2.5"]);

        [Theory]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(5)]
        public void Ace_needs_every_enemy_human(int size)
        {
            RoundRoster roster = Roster(size, size);
            AceProvider ace = Ace(roster);
            for (int i = 0; i < size; i++)
            {
                Dictionary<string, EventValue> d = Kill(ace, roster, attacker: 0, victim: size + i);
                Assert.Equal(i == size - 1, IsAce(d));
                Assert.Equal(i == 0, Bool(d["isace.1.1"]));
            }
        }

        [Fact]
        public void Ace_caps_at_max_players()
        {
            RoundRoster roster = Roster(6, 6);
            AceProvider ace = Ace(roster);
            for (int i = 0; i < 4; i++)
            {
                Assert.False(IsAce(Kill(ace, roster, 0, 6 + i)));
            }

            Assert.True(IsAce(Kill(ace, roster, 0, 10)));
            Assert.False(IsAce(Kill(ace, roster, 0, 11)));
        }

        [Fact]
        public void Ace_follows_a_leaving_enemy()
        {
            RoundRoster roster = Roster(3, 3);
            AceProvider ace = Ace(roster);
            Assert.False(IsAce(Kill(ace, roster, 0, 3)));
            roster.Remove(5);
            Assert.Equal(2, roster.HumansInTeam(CT));
            Assert.True(IsAce(Kill(ace, roster, 0, 4)));
        }

        [Fact]
        public void Ace_counts_humans_only_and_the_victim_once()
        {
            RoundRoster roster = Roster(2, 2, ctBots: 3);
            AceProvider ace = Ace(roster);
            Assert.Equal(2, roster.HumansInTeam(CT));
            Assert.False(IsAce(Kill(ace, roster, 0, 2)));
            Assert.True(IsAce(Kill(ace, roster, 0, 3)));

            // A victim missing from the roster still counts once: 1 listed + the victim = 2 enemies.
            AceProvider late = Ace(Roster(1, 1));
            Assert.False(IsAce(KillUnlisted(late, victim: 9)));
            Assert.True(IsAce(KillUnlisted(late, victim: 10)));
        }

        private static Dictionary<string, EventValue> KillUnlisted(AceProvider ace, int victim)
        {
            ace.OnHumanKill(0, T, victim, CT);
            Dictionary<string, EventValue> d = [];
            ace.WriteEvent("EventPlayerDeath", null!, d, 1);
            return d;
        }

        [Fact]
        public void Ace_ignores_team_and_self_kills_and_resets_per_round()
        {
            RoundRoster roster = Roster(1, 1);
            AceProvider ace = Ace(roster);
            // 1v1 never reaches the default minimum of 2 enemies, but satisfies min_players 1.
            Dictionary<string, EventValue> duel = Kill(ace, roster, 0, 1);
            Assert.False(IsAce(duel));
            Assert.True(Bool(duel["isace.1.1"]));

            ace.Clear();
            Fill(roster, 2, 1);
            Assert.False(IsAce(Kill(ace, roster, 0, 1)));
            Assert.False(IsAce(Kill(ace, roster, 0, 0)));
            Assert.True(Bool(Kill(ace, roster, 0, 2)["isace.1.1"]));

            ace.Clear();
            ace.ForgetSlot(0);
            Assert.False(ace.Configure(Tasks(TestData.Task("t", "player_kill"))));
        }

        [Fact]
        public void Ace_credits_each_bound_once_per_round_and_slot()
        {
            RoundRoster roster = Roster(2, 2);
            AceProvider ace = Ace(roster);
            Assert.False(IsAce(Kill(ace, roster, 0, 2)));
            Assert.True(IsAce(Kill(ace, roster, 0, 3)));
            Assert.False(IsAce(Kill(ace, roster, 1, 2)));
            Assert.True(IsAce(Kill(ace, roster, 1, 3)));

            // A late joiner makes the kill count match the enemy count again; both slots were credited already.
            roster.Set(4, CT, human: true);
            Assert.False(IsAce(Kill(ace, roster, 0, 4)));
            Assert.False(IsAce(Kill(ace, roster, 1, 4)));

            // A new player in slot 0 starts without kills and credits; slot 1 keeps its credit.
            ace.ForgetSlot(0);
            Assert.False(IsAce(Kill(ace, roster, 0, 2)));
            Assert.False(IsAce(Kill(ace, roster, 0, 3)));
            Assert.True(IsAce(Kill(ace, roster, 0, 4)));
            roster.Set(5, CT, human: true);
            Assert.False(IsAce(Kill(ace, roster, 1, 5)));

            // A new round clears every credit.
            ace.Clear();
            Fill(roster, 2, 2);
            Assert.False(IsAce(Kill(ace, roster, 1, 2)));
            Assert.True(IsAce(Kill(ace, roster, 1, 3)));
        }

        [Fact]
        public void Ace_credit_bits_cover_every_bound_up_to_the_limit()
        {
            // 65 distinct bound pairs: only the first 64 get a credit bit and a key.
            ChallengeTask[] tasks = new ChallengeTask[65];
            for (int i = 0; i < tasks.Length; i++)
            {
                tasks[i] = TestData.Task($"t{i}", "player_kill", rules: [TestData.Rule("isace", "bool==", "true", min: 1, max: i + 1)]);
            }

            RoundRoster roster = Roster(1, 1);
            AceProvider ace = new(roster);
            Assert.True(ace.Configure(Tasks(tasks)));

            Dictionary<string, EventValue> first = Kill(ace, roster, 0, 1);
            Assert.Equal(64, first.Count);
            Assert.DoesNotContain("isace.1.65", first.Keys);
            Assert.All(first.Values, value => Assert.True(Bool(value)));

            // Kill 2 against 2 enemies matches every bound with max >= 2 again, but all are credited.
            roster.Set(2, CT, human: true);
            Dictionary<string, EventValue> second = Kill(ace, roster, 0, 2);
            Assert.All(second.Values, value => Assert.False(Bool(value)));

            // Forgotten slot: the highest bit is clear again.
            ace.ForgetSlot(0);
            Assert.False(Bool(Kill(ace, roster, 0, 1)["isace.1.64"]));
            Assert.True(Bool(Kill(ace, roster, 0, 2)["isace.1.64"]));
        }

        [Fact]
        public void Roster_counts_teams_and_moves()
        {
            RoundRoster roster = Roster(2, 3, ctBots: 1);
            Assert.Equal(2, roster.HumansInTeam(T));
            Assert.Equal(3, roster.HumansInTeam(CT));
            Assert.True(roster.IsHumanInTeam(2, CT));
            Assert.False(roster.IsHumanInTeam(5, CT));

            roster.Set(2, T, human: true);
            Assert.Equal(3, roster.HumansInTeam(T));
            Assert.Equal(2, roster.HumansInTeam(CT));
            Assert.Equal(T, roster.TeamOf(2));

            roster.Remove(2);
            roster.Remove(2);
            Assert.Equal(2, roster.HumansInTeam(T));
            Assert.Equal(0, roster.TeamOf(2));
            Assert.Equal(0, roster.HumansInTeam(7));

            roster.Set(40, 9, human: true);
            Assert.Equal(0, roster.TeamOf(40));
            Assert.Equal(1, roster.HumansInTeam(0));
        }

        private static TradeProvider Trade(params ChallengeRule[] rules)
        {
            TradeProvider trade = NewTrade();
            Assert.True(trade.Configure(Tasks(TestData.Task("t", "player_trade", rules: rules))));
            return trade;
        }

        private static TradeProvider NewTrade() => new((_, _) => null, (_, _, _) => { });

        private static TradeProvider.KillRecord Death(int attacker, int victim, double at, string weapon = "weapon_ak47") =>
            new(attacker, victim, weapon, Headshot: false, TeamKill: false, at);

        /// <summary>The earlier reverse kill this kill trades with, or null when it is only remembered.</summary>
        private static TradeProvider.KillRecord? Record(TradeProvider trade, TradeProvider.KillRecord kill) =>
            trade.TryTakeReverse(kill, out TradeProvider.KillRecord first) ? first : null;

        [Fact]
        public void Trade_matches_the_reverse_kill_inside_the_default_window()
        {
            TradeProvider trade = Trade();
            Assert.Null(Record(trade, Death(1, 2, at: 100)));
            Assert.Null(Record(trade, Death(3, 4, at: 101)));

            TradeProvider.KillRecord? first = Record(trade, Death(2, 1, at: 104.5));
            Assert.NotNull(first);
            Assert.Equal((1, 2, 100.0), (first.Value.AttackerSlot, first.Value.VictimSlot, first.Value.At));
            // Consumed: the same pair trades once.
            Assert.Null(Record(trade, Death(2, 1, at: 104.6)));

            // Outside 5 s: no trade.
            Assert.Null(Record(trade, Death(4, 3, at: 106.5)));
        }

        [Fact]
        public void Trade_window_follows_the_largest_seconds_rule()
        {
            TradeProvider trade = Trade(TestData.Rule("seconds", "<=", "8"));
            Assert.Null(Record(trade, Death(1, 2, at: 0)));
            Assert.NotNull(Record(trade, Death(2, 1, at: 7.5)));

            TradeProvider tight = Trade(TestData.Rule("seconds", "<=", "2"));
            Assert.Null(Record(tight, Death(1, 2, at: 0)));
            Assert.Null(Record(tight, Death(2, 1, at: 2.5)));
            Assert.False(NewTrade().Configure(Tasks(TestData.Task("t", "player_kill"))));
        }

        [Fact]
        public void Trade_picks_the_latest_reverse_kill_and_forgets_slots()
        {
            TradeProvider trade = Trade();
            Assert.Null(Record(trade, Death(1, 2, at: 10)));
            Assert.Null(Record(trade, Death(1, 2, at: 11)));
            Assert.Equal(11.0, Record(trade, Death(2, 1, at: 12))!.Value.At);
            Assert.Equal(10.0, Record(trade, Death(2, 1, at: 12.5))!.Value.At);

            Assert.Null(Record(trade, Death(5, 6, at: 20)));
            trade.ForgetSlot(6);
            Assert.Null(Record(trade, Death(6, 5, at: 21)));
            trade.Clear();
            Assert.Null(Record(trade, Death(5, 6, at: 21.5)));
        }

        [Fact]
        public void Kill_record_is_a_value_without_controllers()
        {
            Type record = typeof(TradeProvider.KillRecord);
            Assert.True(record.IsValueType);
            foreach (System.Reflection.FieldInfo field in record.GetFields(
                         System.Reflection.BindingFlags.Instance
                         | System.Reflection.BindingFlags.Public
                         | System.Reflection.BindingFlags.NonPublic))
            {
                Assert.True(field.FieldType.IsValueType || field.FieldType == typeof(string), field.Name);
            }
        }

        [Fact]
        public void Trade_keeps_the_earlier_kill_until_the_window_ends_and_returns_it_whole()
        {
            TradeProvider trade = Trade();
            Assert.Null(Record(trade, new TradeProvider.KillRecord(1, 2, "weapon_awp", Headshot: true, TeamKill: true, At: 100)));
            // Same direction again is no trade, only a second record.
            Assert.Null(Record(trade, Death(1, 2, at: 101)));

            // Exactly 5 s after the first kill it is still inside the window.
            TradeProvider.KillRecord? latest = Record(trade, Death(2, 1, at: 105));
            Assert.Equal(101.0, latest!.Value.At);
            TradeProvider.KillRecord first = Record(trade, Death(2, 1, at: 105))!.Value;
            Assert.Equal(new TradeProvider.KillRecord(1, 2, "weapon_awp", true, true, 100), first);

            Assert.Null(Record(trade, Death(3, 4, at: 200)));
            Assert.Null(Record(trade, Death(4, 3, at: 205.001)));
            // The late kill was remembered instead; the window now runs from it.
            Assert.NotNull(Record(trade, Death(3, 4, at: 210)));
        }

        [Fact]
        public void Weapon_names_compare_short_and_knives_alike()
        {
            Assert.Equal("ak47", Weapons.Short("weapon_ak47"));
            Assert.Equal("", Weapons.Short(null));
            Assert.True(Weapons.Same("knife_t", "KNIFE_karambit"));
            Assert.False(Weapons.Same("ak47", "m4a1"));
        }

        [Fact]
        public void Smoke_probe_reports_covering_or_nearest_smoke()
        {
            SmokeProvider smoke = new();
            smoke.Probe(0, 0, 0, now: 0, out bool inside, out float distance, out float age);
            Assert.Equal((false, 9999f, 0f), (inside, distance, age));

            smoke.AddSmoke(1, 0, 0, 0, at: 100);
            smoke.AddSmoke(2, 1000, 0, 0, at: 104);

            smoke.Probe(100, 0, 0, now: 106, out inside, out distance, out age);
            Assert.Equal((true, 100f, 6f), (inside, distance, age));

            smoke.Probe(0, 143.9f, 0, now: 106, out inside, out _, out _);
            Assert.True(inside);
            smoke.Probe(0, 0, 144.1f, now: 106, out inside, out distance, out _);
            Assert.False(inside);
            Assert.Equal(144.1f, distance, 0.001f);

            smoke.Probe(700, 0, 0, now: 106, out inside, out distance, out age);
            Assert.Equal((false, 300f, 2f), (inside, distance, age));

            // Overlapping smokes: the closest covering one wins.
            smoke.AddSmoke(3, 150, 0, 0, at: 105);
            smoke.Probe(120, 0, 0, now: 106, out inside, out distance, out age);
            Assert.Equal((true, 30f, 1f), (inside, distance, age));

            smoke.RemoveSmoke(3);
            smoke.PruneSmokes(now: 123);
            smoke.Probe(0, 0, 0, now: 123, out inside, out distance, out _);
            Assert.Equal((false, 1000f), (inside, distance));
            smoke.Clear();
            smoke.Probe(1000, 0, 0, now: 123, out inside, out distance, out _);
            Assert.Equal((false, 9999f), (inside, distance));
        }

        [Fact]
        public void Smoke_provider_activates_for_player_and_bomb_keys()
        {
            SmokeProvider smoke = new();
            Assert.False(smoke.Configure(Tasks(TestData.Task("t", "player_kill"))));
            Assert.True(smoke.Configure(Tasks(TestData.Task("t", "player_kill", rules: [TestData.Rule("attacker.insmoke", "bool==", "true")]))));
            Assert.DoesNotContain("EventBombDefused", smoke.WatchedEvents);
            Assert.True(smoke.Configure(Tasks(TestData.Task("t", "player_bomb_defused", rules: [TestData.Rule("smoke.distance", "<", "50")]))));
            Assert.Contains("EventBombDefused", smoke.WatchedEvents);
        }
    }
}

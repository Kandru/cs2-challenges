using Challenges.Extractors;
using Challenges.Utils;
using YamlDotNet.Serialization;
using GlobalGate = Challenges.ChallengeEngine.GlobalGate;
using GlobalKeyFlags = Challenges.ChallengeEngine.GlobalKeyFlags;

namespace Challenges.Tests
{
    /// <summary>What is computed once at load: fill demand (T1.1), gates (T1.2), aliases (T3.2), validation (T3.4).</summary>
    public sealed class LoadTimeTests
    {
        private static IExtractor Extractor(string eventClassName)
        {
            foreach (IExtractor extractor in Registry.All)
            {
                if (extractor.EventClassName == eventClassName)
                {
                    return extractor;
                }
            }

            throw new InvalidOperationException(eventClassName);
        }

        private static FillDemand Demand(IExtractor extractor, params ChallengeTask[] tasks)
        {
            Dictionary<string, ChallengeEngine.TypePlan> plans =
                ChallengeEngine.BuildTypePlans(TestData.Schedule(TestData.Challenge("c", tasks)), [], out _);
            ChallengeEngine.TypePlan[] types = [.. plans.Values];
            return ChallengeEngine.BuildDemand(extractor, types, ChallengeEngine.RuleKeysOf(types), extraKeys: 0);
        }

        private static int Index(IReadOnlyList<string> list, string value)
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] == value)
                {
                    return i;
                }
            }

            throw new InvalidOperationException(value);
        }

        [Fact]
        public void Demand_holds_only_what_the_rules_read()
        {
            IExtractor death = Extractor("EventPlayerDeath");
            FillDemand need = Demand(death, TestData.Task("t", "player_kill", rules:
            [
                TestData.Rule("weapon", "contains", "knife"),
                TestData.Rule("victim.isbot", "bool==", "false"),
                TestData.Rule("global.iswarmup", "bool==", "false"),
                TestData.Rule("isace", "bool==", "true"),
                TestData.Rule("attacker.insmoke", "bool==", "true"),
                TestData.Rule("attacker.grenade", "==", "flashbang"),
            ]));

            for (int i = 0; i < death.Keys.Count; i++)
            {
                Assert.Equal(death.Keys[i] == "weapon", need.Keys[i]);
            }

            Assert.Equal(PlayerFields.IsBot, need.Prefixes[Index(death.PlayerPrefixes, "userid")]);
            Assert.Equal(PlayerFields.None, need.Prefixes[Index(death.PlayerPrefixes, "attacker")]);
            Assert.Equal(PlayerFields.None, need.Prefixes[Index(death.PlayerPrefixes, "assister")]);
            Assert.False(need.Derived);
            Assert.True(need.Types[Index(death.ChallengeTypes, "player_kill")]);
            Assert.False(need.Types[Index(death.ChallengeTypes, "player_death")]);
            Assert.False(need.Types[Index(death.ChallengeTypes, "player_kill_assist")]);
        }

        [Fact]
        public void Demand_unites_types_prefixes_aliases_and_derived_keys()
        {
            IExtractor death = Extractor("EventPlayerDeath");
            FillDemand need = Demand(death,
                TestData.Task("a", "player_kill", rules:
                [
                    TestData.Rule("dmgarmor", ">", "10"),
                    TestData.Rule("attacker.stats.kills", ">", "3"),
                    TestData.Rule("isteamkill", "bool==", "false"),
                ]),
                TestData.Task("b", "player_death", rules:
                [
                    TestData.Rule("attacker.health", "<", "10"),
                    TestData.Rule("userid.money", ">", "100"),
                ]));

            Assert.True(need.Keys[Index(death.Keys, "dmg_armor")]);
            Assert.Equal(PlayerFields.StatsKills | PlayerFields.Health, need.Prefixes[Index(death.PlayerPrefixes, "attacker")]);
            Assert.Equal(PlayerFields.Money, need.Prefixes[Index(death.PlayerPrefixes, "userid")]);
            Assert.True(need.Derived);
            Assert.True(need.Types[Index(death.ChallengeTypes, "player_kill")]);
            Assert.True(need.Types[Index(death.ChallengeTypes, "player_death")]);
            Assert.Equal("attacker.stats.kills", need.PrefixKeys[Index(death.PlayerPrefixes, "attacker")][7]);
        }

        [Theory]
        [InlineData("global.iswarmup", "bool==", "false", (byte)GlobalGate.NoWarmup)]
        [InlineData("global.iswarmup", "bool==", "true", (byte)GlobalGate.Warmup)]
        [InlineData("global.iswarmup", "bool!=", "true", (byte)GlobalGate.NoWarmup)]
        [InlineData("global.iswarmup", "==", "False", (byte)GlobalGate.NoWarmup)]
        [InlineData("global.iswarmup", "!=", "false", (byte)GlobalGate.Warmup)]
        [InlineData("global.isduringround", "bool==", "true", (byte)GlobalGate.DuringRound)]
        [InlineData("global.isduringround", "bool!=", "true", (byte)GlobalGate.NotDuringRound)]
        [InlineData("global.isduringround", "==", "true", (byte)GlobalGate.DuringRound)]
        [InlineData("global.iswarmup", "bool==", "maybe", (byte)GlobalGate.None)]
        [InlineData("global.iswarmup", "contains", "false", (byte)GlobalGate.None)]
        [InlineData("global.iswarmup", "==", "0", (byte)GlobalGate.None)]
        [InlineData("global.mapname", "==", "de_dust2", (byte)GlobalGate.None)]
        public void Task_gate_follows_the_rule(string key, string op, string value, byte expected)
        {
            ChallengeTask task = TestData.Challenge("c", TestData.Task("t", "player_kill", rules: [TestData.Rule(key, op, value)])).Tasks[0];
            Assert.Equal((GlobalGate)expected, ChallengeEngine.TaskGate(task));
        }

        [Fact]
        public void Type_gate_keeps_what_every_task_of_the_type_requires()
        {
            ChallengeRule noWarmup = TestData.Rule("global.iswarmup", "bool==", "false");
            ChallengeRule duringRound = TestData.Rule("global.isduringround", "bool==", "true");
            ChallengeDefinition c = TestData.Challenge("c",
                TestData.Task("a", "player_kill", rules: [noWarmup, duringRound]),
                TestData.Task("b", "player_kill", rules: [TestData.Rule("global.iswarmup", "bool==", "false")]),
                TestData.Task("c", "player_death", rules: [TestData.Rule("global.iswarmup", "bool==", "false"), duringRound]),
                TestData.Task("d", "player_hurt_attacker", rules: [TestData.Rule("global.isduringround", "bool==", "true")]),
                TestData.Task("e", "player_hurt_attacker"));
            List<ChallengeTask> all = [];
            Dictionary<string, ChallengeEngine.TypePlan> plans =
                ChallengeEngine.BuildTypePlans(TestData.Schedule(c), all, out GlobalKeyFlags globalKeys);

            Assert.Equal(5, all.Count);
            Assert.Equal(GlobalKeyFlags.IsWarmup | GlobalKeyFlags.IsDuringRound, globalKeys);
            Assert.Equal(GlobalGate.NoWarmup, plans["player_kill"].Gate);
            Assert.Equal(GlobalGate.NoWarmup | GlobalGate.DuringRound, plans["player_death"].Gate);
            Assert.Equal(GlobalGate.None, plans["player_hurt_attacker"].Gate);
            Assert.Equal([0, 1, 2], new[] { plans["player_kill"].Id, plans["player_death"].Id, plans["player_hurt_attacker"].Id });

            // Warmup closes NoWarmup types; outside a round closes DuringRound types.
            Assert.True(plans["player_kill"].IsClosed(GlobalGate.NoWarmup | GlobalGate.DuringRound));
            Assert.False(plans["player_kill"].IsClosed(GlobalGate.Warmup | GlobalGate.DuringRound));
            Assert.True(plans["player_death"].IsClosed(GlobalGate.Warmup | GlobalGate.DuringRound));
            Assert.False(plans["player_death"].IsClosed(GlobalGate.Warmup | GlobalGate.NotDuringRound));
            Assert.False(plans["player_hurt_attacker"].IsClosed(GlobalGate.NoWarmup | GlobalGate.DuringRound));
        }

        [Fact]
        public void Global_keys_are_flagged_only_when_a_rule_reads_them()
        {
            ChallengeDefinition c = TestData.Challenge("c",
                TestData.Task("a", "player_kill", rules: [TestData.Rule("global.hashostages", "bool==", "true")]),
                TestData.Task("b", "player_death", rules: [TestData.Rule("global.mapname", "==", "de_dust2")]));
            ChallengeEngine.BuildTypePlans(TestData.Schedule(c), [], out GlobalKeyFlags globalKeys);
            Assert.Equal(GlobalKeyFlags.HasHostages | GlobalKeyFlags.MapName, globalKeys);

            ChallengeEngine.BuildTypePlans(TestData.Schedule(TestData.Challenge("c", TestData.Task("a", "player_kill"))), [], out globalKeys);
            Assert.Equal(GlobalKeyFlags.None, globalKeys);
        }

        [Theory]
        [InlineData("player_kill", "victim.health", "userid.health")]
        [InlineData("player_kill", "victim.grenade", "userid.grenade")]
        [InlineData("player_kill", "victim.insmoke", "userid.insmoke")]
        [InlineData("player_kill", "userid.health", "userid.health")]
        [InlineData("player_kill", "dmgarmor", "dmg_armor")]
        [InlineData("player_kill", "weaponitemid", "weapon_itemid")]
        [InlineData("player_kill", "weapon", "weapon")]
        [InlineData("player_kill", "isace", "isace")]
        [InlineData("player_kill", "global.iswarmup", "global.iswarmup")]
        [InlineData("player_kill", "nosuchkey", "nosuchkey")]
        [InlineData("player_bomb_defused", "defuser.insmoke", "userid.insmoke")]
        [InlineData("player_bomb_defused", "bomb_site", "site")]
        [InlineData("weapon_fire", "player.team", "userid.team")]
        [InlineData("player_trade", "userid.name", "victim.name")]
        [InlineData("player_trade", "attacker.name", "attacker.name")]
        [InlineData("no_such_type", "victim.health", "victim.health")]
        public void Aliases_resolve_to_canonical_keys(string type, string key, string canonical)
        {
            Assert.Equal(canonical, RuleKeys.Canonical(type, key));
        }

        [Fact]
        public void Normalize_trims_lowercases_and_compiles_canonical_keys()
        {
            ChallengeTask task = TestData.Challenge("c", TestData.Task(" t ", " Player_Kill ", amount: 0, rules:
            [
                TestData.Rule(" Victim.IsBot ", " bool== ", "false"),
                TestData.Rule("DmgArmor", ">", "5"),
            ])).Tasks[0];

            Assert.Equal("t", task.Id);
            Assert.Equal("player_kill", task.Type);
            Assert.Equal(1, task.Amount);
            Assert.Equal(2, task.CompiledRules.Length);
            Assert.Equal("victim.isbot", task.CompiledRules[0].Source.Key);
            Assert.Equal("userid.isbot", task.CompiledRules[0].Key);
            Assert.Equal(RuleOperator.BoolEqual, task.CompiledRules[0].Op);
            Assert.Equal("dmg_armor", task.CompiledRules[1].LookupKey);
            Assert.Equal(5.0, task.CompiledRules[1].Number);
        }

        [Fact]
        public void Validation_reports_unknown_types_keys_and_operators_with_suggestions()
        {
            ChallengeDefinition def = TestData.Challenge("bp",
                TestData.Task("a", "player_kil"),
                TestData.Task("b", "player_kill", rules:
                [
                    TestData.Rule("weapn", "==", "ak47"),
                    TestData.Rule("completely.unrelated.key", "==", "1"),
                    TestData.Rule("victim.helth", ">", "1"),
                    TestData.Rule("headshot", "=>", "true"),
                ]),
                TestData.Task("c", "player_death", rules: [TestData.Rule("isace", "bool==", "true")]));

            List<string> problems = RuleValidation.Check(def);

            Assert.Equal(
            [
                "blueprint bp, task a: unknown type player_kil; did you mean player_kill?",
                "blueprint bp, task b: unknown key weapn for type player_kill; did you mean weapon?",
                "blueprint bp, task b: unknown key completely.unrelated.key for type player_kill",
                "blueprint bp, task b: unknown key victim.helth for type player_kill; did you mean victim.health?",
                "blueprint bp, task b: unknown operator => for key headshot",
                "blueprint bp, task c: key isace only works for type player_kill",
            ], problems);
        }

        [Fact]
        public void Validation_accepts_aliases_provider_keys_and_globals()
        {
            ChallengeDefinition def = TestData.Challenge("bp",
                TestData.Task("a", "player_kill", rules:
                [
                    TestData.Rule("victim.isbot", "bool==", "false"),
                    TestData.Rule("userid.isbot", "bool==", "false"),
                    TestData.Rule("attacker.insmoke", "bool==", "true"),
                    TestData.Rule("victim.grenade.holding", "==", "hegrenade"),
                    TestData.Rule("isace", "bool==", "true"),
                    TestData.Rule("isteamkill", "bool==", "false"),
                    TestData.Rule("dmgarmor", ">", "0"),
                    TestData.Rule("global.mapname", "contains", "dust"),
                ]),
                TestData.Task("b", "player_bomb_defused", rules:
                [
                    TestData.Rule("defuser.insmoke", "bool==", "true"),
                    TestData.Rule("insmoke", "bool==", "true"),
                    TestData.Rule("smoke.distance", "<", "100"),
                ]),
                TestData.Task("c", "player_trade", rules:
                [
                    TestData.Rule("seconds", "<=", "3"),
                    TestData.Rule("userid.isbot", "bool==", "false"),
                ]));

            Assert.Empty(RuleValidation.Check(def));
        }

        /// <summary>T3.4 acceptance: every example blueprint loads without a warning.</summary>
        [Fact]
        public void Example_blueprints_load_without_warnings()
        {
            IDeserializer yaml = new DeserializerBuilder().IgnoreUnmatchedProperties().Build();
            string[] files = Directory.GetFiles(Path.Combine(AppContext.BaseDirectory, "blueprints"), "*.yaml");
            Assert.NotEmpty(files);

            List<string> problems = [];
            foreach (string file in files)
            {
                ChallengeDefinition def = yaml.Deserialize<ChallengeDefinition>(File.ReadAllText(file));
                def.Id = Path.GetFileNameWithoutExtension(file);
                global::Challenges.Challenges.NormalizeDefinition(def);
                problems.AddRange(RuleValidation.Check(def));
                foreach (ChallengeTask task in def.Tasks)
                {
                    Assert.Equal(task.Rules.Count, task.CompiledRules.Length);
                }
            }

            Assert.Empty(problems);
        }
    }
}

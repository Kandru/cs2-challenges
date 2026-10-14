using System.Globalization;
using Challenges.Utils;

namespace Challenges.Tests
{
    /// <summary><see cref="ChallengeRules"/>, <see cref="CompiledRule"/> and <see cref="EventValue"/>.</summary>
    public sealed class RuleTests
    {
        private static bool Pass(ChallengeTask task, Dictionary<string, EventValue> data, bool allowBots = false) =>
            ChallengeRules.RulesPass(task, data, allowBots, explain: false, out _);

        private static ChallengeTask KillTask(params ChallengeRule[] rules) =>
            TestData.Challenge("c", TestData.Task("t", "player_kill", rules: rules)).Tasks[0];

        public static TheoryData<string, string, string, string, bool> OperatorCases => new()
        {
            // numbers
            { "==", "5", "int", "5", true },
            { "==", "5", "int", "6", false },
            { "!=", "5", "int", "6", true },
            { "!=", "5", "int", "5", false },
            { ">", "5", "int", "6", true },
            { ">", "5", "int", "5", false },
            { "<", "5", "int", "4", true },
            { "<", "5", "int", "5", false },
            { ">=", "5", "int", "5", true },
            { ">=", "5", "int", "4", false },
            { "<=", "5", "int", "5", true },
            { "<=", "5", "int", "6", false },
            { ">", "1.5", "float", "1.75", true },
            { "<", "1.5", "float", "1.25", true },
            { ">=", "500.5", "float", "500.5", true },
            { "==", "1.5", "float", "1.5", true },
            { "contains", "2", "int", "123", true },
            { "!contains", "9", "int", "123", true },
            { "bool==", "true", "int", "1", false },
            { ">", "abc", "int", "1", false },
            // bools
            { "bool==", "true", "bool", "true", true },
            { "bool==", "false", "bool", "true", false },
            { "bool!=", "true", "bool", "false", true },
            { "bool!=", "false", "bool", "false", false },
            { "bool==", "TRUE", "bool", "true", true },
            { "bool==", "yes", "bool", "true", false },
            { "==", "true", "bool", "true", true },
            { "==", "TRUE", "bool", "true", true },
            { "!=", "false", "bool", "true", true },
            { ">", "0", "bool", "true", false },
            { "contains", "ru", "bool", "true", true },
            // texts
            { "==", "ak47", "text", "AK47", true },
            { "!=", "ak47", "text", "m4a1", true },
            { "contains", "knife", "text", "weapon_knife_t", true },
            { "contains", "KNIFE", "text", "weapon_knife_t", true },
            { "!contains", "knife", "text", "ak47", true },
            { "!contains", "knife", "text", "knife", false },
            { "bool==", "true", "text", "True", true },
            { "bool==", "true", "text", "1", false },
            { ">", "2", "text", "3", true },
            { ">", "2", "text", "x", false },
            { "==", "1", "text", "1", true },
            // invalid operator never matches
            { "=>", "5", "int", "5", false },
            { "~", "ak47", "text", "ak47", false },
        };

        [Theory]
        [MemberData(nameof(OperatorCases))]
        public void Operators_match_by_value_kind(string op, string value, string kind, string current, bool expected)
        {
            ChallengeTask task = KillTask(TestData.Rule("weapon", op, value));
            Assert.Equal(expected, Pass(task, TestData.Data(("weapon", Typed(kind, current)))));
        }

        private static EventValue Typed(string kind, string raw) =>
            kind switch
            {
                "int" => EventValue.Of(int.Parse(raw, CultureInfo.InvariantCulture)),
                "float" => EventValue.Of(float.Parse(raw, CultureInfo.InvariantCulture)),
                "bool" => EventValue.Of(bool.Parse(raw)),
                _ => EventValue.Of(raw),
            };

        [Fact]
        public void Missing_key_fails_with_reason()
        {
            ChallengeTask task = KillTask(TestData.Rule("weapon", "==", "ak47"));
            Assert.False(ChallengeRules.RulesPass(task, TestData.Data(), false, explain: true, out string? reason));
            Assert.Equal("weapon == ak47 (missing)", reason);
        }

        [Fact]
        public void All_rules_must_pass()
        {
            ChallengeTask task = KillTask(
                TestData.Rule("weapon", "==", "ak47"),
                TestData.Rule("headshot", "bool==", "true"));
            Assert.True(Pass(task, TestData.Data(("weapon", EventValue.Of("ak47")), ("headshot", EventValue.Of(true)))));
            Assert.False(Pass(task, TestData.Data(("weapon", EventValue.Of("ak47")), ("headshot", EventValue.Of(false)))));
            Assert.True(Pass(KillTask(), TestData.Data()));
        }

        [Fact]
        public void Uncompiled_rules_fail_closed()
        {
            ChallengeTask task = TestData.Task("t", "player_kill", rules: [TestData.Rule("weapon", "==", "ak47")]);
            Assert.Empty(task.CompiledRules);
            Assert.False(ChallengeRules.RulesPass(task, TestData.Data(("weapon", EventValue.Of("ak47"))), false, true, out string? reason));
            Assert.Equal("rules not compiled", reason);
        }

        [Fact]
        public void Allow_bots_skips_only_isbot_false_rules()
        {
            ChallengeTask task = KillTask(
                TestData.Rule("victim.isbot", "bool==", "false"),
                TestData.Rule("weapon", "==", "ak47"));
            CompiledRule isBot = task.CompiledRules[0];
            Assert.True(isBot.SkipWhenBotsAllowed);
            Assert.Equal("userid.isbot", isBot.Key);
            Assert.False(task.CompiledRules[1].SkipWhenBotsAllowed);

            Dictionary<string, EventValue> botKill = TestData.Data(
                ("userid.isbot", EventValue.Of(true)),
                ("weapon", EventValue.Of("ak47")));
            Assert.False(Pass(task, botKill));
            Assert.True(Pass(task, botKill, allowBots: true));
            // The key may even be missing while bots are allowed.
            Assert.True(Pass(task, TestData.Data(("weapon", EventValue.Of("ak47"))), allowBots: true));
            Assert.False(Pass(task, TestData.Data(("weapon", EventValue.Of("m4a1"))), allowBots: true));

            Assert.False(KillTask(TestData.Rule("victim.isbot", "bool==", "true")).CompiledRules[0].SkipWhenBotsAllowed);
            Assert.False(KillTask(TestData.Rule("victim.isbot", "==", "false")).CompiledRules[0].SkipWhenBotsAllowed);
        }

        [Fact]
        public void Isace_reads_bounded_key_and_requires_player_kill()
        {
            ChallengeTask kill = KillTask(TestData.Rule("isace", "bool==", "true"));
            Assert.Equal("isace.2.5", kill.CompiledRules[0].LookupKey);
            Assert.True(Pass(kill, TestData.Data(("isace.2.5", EventValue.Of(true)))));
            Assert.False(Pass(kill, TestData.Data(("isace.3.3", EventValue.Of(true)))));

            ChallengeTask bounded = KillTask(TestData.Rule("isace", "bool==", "true", min: 3, max: 4));
            Assert.Equal("isace.3.4", bounded.CompiledRules[0].LookupKey);
            ChallengeTask swapped = KillTask(TestData.Rule("isace", "bool==", "true", min: 4, max: 2));
            Assert.Equal("isace.4.4", swapped.CompiledRules[0].LookupKey);

            ChallengeTask death = TestData.Challenge("c", TestData.Task("t", "player_death",
                rules: [TestData.Rule("isace", "bool==", "true")])).Tasks[0];
            Assert.False(ChallengeRules.RulesPass(death, TestData.Data(("isace.2.5", EventValue.Of(true))), false, true, out string? reason));
            Assert.Equal("isace requires player_kill", reason);
        }

        [Fact]
        public void Rule_values_and_numbers_use_invariant_culture()
        {
            CultureInfo previous = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("de-DE");
                ChallengeTask dot = KillTask(TestData.Rule("distance", ">", "1.5"));
                Assert.Equal(1.5, dot.CompiledRules[0].Number);
                Assert.True(Pass(dot, TestData.Data(("distance", EventValue.Of(2.25f)))));
                Assert.True(Pass(dot, TestData.Data(("distance", EventValue.Of("2.25")))));
                Assert.False(Pass(dot, TestData.Data(("distance", EventValue.Of("2,25")))));

                ChallengeTask comma = KillTask(TestData.Rule("distance", ">", "1,5"));
                Assert.Null(comma.CompiledRules[0].Number);
                Assert.False(Pass(comma, TestData.Data(("distance", EventValue.Of(2.25f)))));

                Assert.Equal("2.25", EventValue.Of(2.25f).ToString());
                Assert.True(Pass(KillTask(TestData.Rule("distance", "==", "2.25")), TestData.Data(("distance", EventValue.Of(2.25f)))));
            }
            finally
            {
                CultureInfo.CurrentCulture = previous;
            }
        }

        [Fact]
        public void Format_params_show_yaml_keys_and_invariant_values()
        {
            ChallengeTask task = KillTask(
                TestData.Rule("Victim.Health", ">", "0"),
                TestData.Rule("distance", ">", "1"),
                TestData.Rule("weapon", "==", "ak47"));
            string text = ChallengeRules.FormatParams(task, TestData.Data(
                ("userid.health", EventValue.Of(42)),
                ("distance", EventValue.Of(12.5f))));
            Assert.Equal("victim.health=42 distance=12.5 weapon=?", text);
        }

        [Fact]
        public void Event_value_text_matches_former_string_values()
        {
            Assert.Equal("true", EventValue.Of(true).ToString());
            Assert.Equal("false", EventValue.Of(false).ToString());
            Assert.Equal("-12", EventValue.Of(-12).ToString());
            Assert.Equal("4294967295", EventValue.Of(uint.MaxValue).ToString());
            Assert.Equal(ulong.MaxValue.ToString(CultureInfo.InvariantCulture), EventValue.Of(ulong.MaxValue).ToString());
            Assert.Equal(0.1f.ToString(CultureInfo.InvariantCulture), EventValue.Of(0.1f).ToString());
            Assert.Equal(0.1.ToString(CultureInfo.InvariantCulture), EventValue.Of(0.1).ToString());
            Assert.Equal(string.Empty, EventValue.Of((string?)null).ToString());
        }

        /// <summary>
        /// T3.5: the typed comparison gives the same result as the former string comparison (values formatted
        /// with the invariant culture, rules evaluated on text) for every operator and value kind.
        /// </summary>
        [Fact]
        public void Typed_values_match_former_string_rules()
        {
            (EventValue Value, string Text)[] values =
            [
                (EventValue.Of(0), "0"), (EventValue.Of(1), "1"), (EventValue.Of(-1), "-1"), (EventValue.Of(2), "2"),
                (EventValue.Of(10), "10"), (EventValue.Of(100), "100"), (EventValue.Of(16777217), "16777217"),
                (EventValue.Of(uint.MaxValue), Inv(uint.MaxValue)), (EventValue.Of(1L << 54), Inv(1L << 54)),
                (EventValue.Of((1L << 54) + 1), Inv((1L << 54) + 1)), (EventValue.Of(ulong.MaxValue), Inv(ulong.MaxValue)),
                (EventValue.Of(0.5f), Inv(0.5f)), (EventValue.Of(1.5f), Inv(1.5f)), (EventValue.Of(1.1f), Inv(1.1f)),
                (EventValue.Of(2f), Inv(2f)), (EventValue.Of(1e-7f), Inv(1e-7f)), (EventValue.Of(123456.7f), Inv(123456.7f)),
                (EventValue.Of(-0f), Inv(-0f)), (EventValue.Of(float.NaN), Inv(float.NaN)), (EventValue.Of(3e38f), Inv(3e38f)),
                (EventValue.Of(0.1), Inv(0.1)), (EventValue.Of(1.5), Inv(1.5)), (EventValue.Of(1e300), Inv(1e300)),
                (EventValue.Of(true), "true"), (EventValue.Of(false), "false"),
                (EventValue.Of("ak47"), "ak47"), (EventValue.Of("AK47"), "AK47"), (EventValue.Of("weapon_knife_t"), "weapon_knife_t"),
                (EventValue.Of("True"), "True"), (EventValue.Of("false"), "false"), (EventValue.Of("1.5"), "1.5"),
                (EventValue.Of("1"), "1"), (EventValue.Of(""), ""), (EventValue.Of("Terrorist"), "Terrorist"),
            ];
            string[] ruleValues =
            [
                "0", "1", "-1", "2", "1.5", "1.1", "1.0", "100", "16777216", "1e2", "-0", "NaN", "0.5",
                "true", "false", "TRUE", "ak47", "AK", "knife", "terrorist", "", "abc", "1,5",
            ];
            string[] operators = ["==", "!=", ">", "<", ">=", "<=", "bool==", "bool!=", "contains", "!contains", "=>"];

            List<string> mismatches = [];
            foreach ((EventValue value, string text) in values)
            {
                Assert.Equal(text, value.ToString());
                foreach (string op in operators)
                {
                    foreach (string ruleValue in ruleValues)
                    {
                        ChallengeTask task = KillTask(TestData.Rule("weapon", op, ruleValue));
                        bool typed = Pass(task, TestData.Data(("weapon", value)));
                        bool legacy = LegacyEvaluate(op, text, ruleValue);
                        if (typed != legacy)
                        {
                            mismatches.Add($"'{text}' {op} '{ruleValue}': typed {typed}, legacy {legacy}");
                        }
                    }
                }
            }

            Assert.Empty(mismatches);
        }

        private static string Inv<T>(T value) where T : IFormattable => value.ToString(null, CultureInfo.InvariantCulture);

        /// <summary>Rule evaluation as it was on string event data before T3.1/T3.5.</summary>
        private static bool LegacyEvaluate(string op, string current, string target)
        {
            switch (op)
            {
                case "==":
                    return string.Equals(current, target, StringComparison.OrdinalIgnoreCase);
                case "!=":
                    return !string.Equals(current, target, StringComparison.OrdinalIgnoreCase);
                case ">":
                    return LegacyCompare(current, target, out int gt) && gt > 0;
                case "<":
                    return LegacyCompare(current, target, out int lt) && lt < 0;
                case ">=":
                    return LegacyCompare(current, target, out int ge) && ge >= 0;
                case "<=":
                    return LegacyCompare(current, target, out int le) && le <= 0;
                case "bool==":
                    return bool.TryParse(current, out bool a) && bool.TryParse(target, out bool b) && a == b;
                case "bool!=":
                    return bool.TryParse(current, out bool c) && bool.TryParse(target, out bool d) && c != d;
                case "contains":
                    return current.Contains(target, StringComparison.OrdinalIgnoreCase);
                case "!contains":
                    return !current.Contains(target, StringComparison.OrdinalIgnoreCase);
                default:
                    return false;
            }
        }

        private static bool LegacyCompare(string current, string target, out int result)
        {
            if (!float.TryParse(current, NumberStyles.Float, CultureInfo.InvariantCulture, out float left)
                || !float.TryParse(target, NumberStyles.Float, CultureInfo.InvariantCulture, out float right))
            {
                result = 0;
                return false;
            }

            result = left.CompareTo(right);
            return true;
        }
    }
}

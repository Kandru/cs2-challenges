using System.Globalization;
using Challenges.Extractors;
using Challenges.Utils;
using YamlDotNet.Serialization;

namespace Challenges.Bench
{
    internal enum FakeTeam
    {
        None,
        Spectator,
        Terrorist,
        CounterTerrorist,
    }

    /// <summary>The controller properties a fill reads, typed like CounterStrikeSharp returns them.</summary>
    internal sealed class FakePlayer
    {
        public string Name = "player";
        public bool IsBot = false;
        public FakeTeam Team = FakeTeam.CounterTerrorist;
        public bool Alive = true;
        public uint Ping = 24;
        public int Money = 3150;
        public int Score = 17;
        public int Kills = 9;
        public int Assists = 2;
        public int Deaths = 7;
        public int Damage = 1240;
        public uint Health = 87;
        public uint Armor = 64;
        public bool HasDefuser = false;
        public bool HasHelmet = true;
    }

    /// <summary>The fields of <c>EventPlayerDeath</c>, typed like CounterStrikeSharp returns them.</summary>
    internal sealed class FakeDeath
    {
        public bool Assistedflash = false;
        public bool Attackerblind = false;
        public bool Attackerinair = false;
        public float Distance = 12.75f;
        public int DmgArmor = 12;
        public int DmgHealth = 100;
        public int Dominated = 0;
        public bool Headshot = true;
        public int Hitgroup = 1;
        public bool Noreplay = false;
        public bool Noscope = false;
        public int Penetrated = 0;
        public int Revenge = 0;
        public bool Thrusmoke = false;
        public string Weapon = "ak47";
        public string WeaponFauxitemid = "17293822569102704647";
        public string WeaponItemid = "0";
        public string WeaponOriginalownerXuid = "76561198000000001";
        public int Wipe = 0;
        public FakePlayer? Assister = null;
        public FakePlayer? Attacker = new() { Name = "attacker" };
        public FakePlayer? Userid = new() { Name = "victim", Team = FakeTeam.Terrorist, Alive = false, Health = 0 };
    }

    /// <summary>Example blueprints, schedules and the two event data shapes of a kill.</summary>
    internal static class BenchData
    {
        public const string ScheduleKey = "bench";

        private static readonly IDeserializer Yaml = new DeserializerBuilder().IgnoreUnmatchedProperties().Build();

        /// <summary>Blueprints from <c>examples/blueprints</c>, normalized and compiled like the plugin loads them.</summary>
        public static RunningSchedule Schedule(string selection)
        {
            string dir = BlueprintDirectory();
            List<ChallengeDefinition> challenges = [];
            foreach (string file in Directory.GetFiles(dir, "*.yaml").Order(StringComparer.Ordinal))
            {
                string id = Path.GetFileNameWithoutExtension(file);
                if (selection != "all" && id != selection)
                {
                    continue;
                }

                ChallengeDefinition def = Yaml.Deserialize<ChallengeDefinition>(File.ReadAllText(file));
                def.Id = id;
                global::Challenges.Challenges.NormalizeDefinition(def);
                challenges.Add(def);
            }

            if (challenges.Count == 0)
            {
                throw new InvalidOperationException($"no blueprint '{selection}' in {dir}");
            }

            return new RunningSchedule { Key = ScheduleKey, ScheduleId = ScheduleKey, Challenges = challenges };
        }

        public static List<(ChallengeDefinition Challenge, ChallengeTask Task)> TasksOfType(
            RunningSchedule schedule,
            string type)
        {
            List<(ChallengeDefinition, ChallengeTask)> tasks = [];
            foreach (ChallengeDefinition challenge in schedule.Challenges)
            {
                foreach (ChallengeTask task in challenge.Tasks)
                {
                    if (task.Type == type)
                    {
                        tasks.Add((challenge, task));
                    }
                }
            }

            return tasks;
        }

        public static IExtractor PlayerDeathExtractor()
        {
            foreach (IExtractor extractor in Registry.All)
            {
                if (extractor.EventClassName == "EventPlayerDeath")
                {
                    if (!extractor.Keys.SequenceEqual(DemandFill.EventKeys)
                        || !extractor.PlayerPrefixes.SequenceEqual(["assister", "attacker", "userid"]))
                    {
                        throw new InvalidOperationException("PlayerDeath key layout changed; update DemandFill");
                    }

                    return extractor;
                }
            }

            throw new InvalidOperationException("EventPlayerDeath extractor missing");
        }

        /// <summary>What the engine fills for <c>EventPlayerDeath</c> under <paramref name="schedule"/>.</summary>
        public static FillDemand DeathDemand(RunningSchedule schedule)
        {
            IExtractor extractor = PlayerDeathExtractor();
            Dictionary<string, ChallengeEngine.TypePlan> plans = ChallengeEngine.BuildTypePlans(schedule, [], out _);
            List<ChallengeEngine.TypePlan> types = [];
            foreach (ChallengeEngine.TypePlan plan in plans.Values)
            {
                if (extractor.ChallengeTypes.Contains(plan.Type))
                {
                    types.Add(plan);
                }
            }

            ChallengeEngine.TypePlan[] active = [.. types];
            return ChallengeEngine.BuildDemand(extractor, active, ChallengeEngine.RuleKeysOf(active), extraKeys: 0);
        }

        /// <summary>
        /// Kill event data both ways: the old full string fill plus string globals, and the demand fill plus
        /// typed globals. Keys only an attribute provider writes get the same neutral value in both.
        /// </summary>
        public static (Dictionary<string, string> Legacy, Dictionary<string, EventValue> Typed) KillData(
            FakeDeath death,
            RunningSchedule schedule,
            List<(ChallengeDefinition Challenge, ChallengeTask Task)> tasks)
        {
            Dictionary<string, string> legacy = [];
            LegacyFill.Fill(death, legacy);
            legacy[GlobalKeys.IsWarmup] = "false";
            legacy[GlobalKeys.IsDuringRound] = "true";
            legacy[GlobalKeys.MapName] = "de_dust2";
            legacy[GlobalKeys.HasHostages] = "false";

            Dictionary<string, EventValue> typed = [];
            DemandFill.Fill(death, typed, DeathDemand(schedule));
            typed[GlobalKeys.IsWarmup] = EventValue.Of(false);
            typed[GlobalKeys.IsDuringRound] = EventValue.Of(true);
            typed[GlobalKeys.MapName] = EventValue.Of("de_dust2");
            typed[GlobalKeys.HasHostages] = EventValue.Of(false);

            foreach ((_, ChallengeTask task) in tasks)
            {
                for (int i = 0; i < task.Rules.Count; i++)
                {
                    ChallengeRule rule = task.Rules[i];
                    CompiledRule compiled = task.CompiledRules[i];
                    (string text, EventValue value) = NeutralValue(compiled.Op);
                    legacy.TryAdd(LegacyRules.DataLookupKey(rule), text);
                    typed.TryAdd(compiled.LookupKey, value);
                }
            }

            return (legacy, typed);
        }

        private static (string Text, EventValue Value) NeutralValue(RuleOperator op) =>
            op switch
            {
                RuleOperator.BoolEqual or RuleOperator.BoolNotEqual => ("false", EventValue.Of(false)),
                RuleOperator.Greater or RuleOperator.Less or RuleOperator.GreaterOrEqual or RuleOperator.LessOrEqual
                    => ("0", EventValue.Of(0)),
                _ => (string.Empty, EventValue.Of(string.Empty)),
            };

        /// <summary>A player who has progress on every task of <paramref name="schedule"/>.</summary>
        public static PlayerState ProgressState(RunningSchedule schedule, int amount)
        {
            PlayerState state = new() { SteamId = "76561198000000042" };
            foreach (ChallengeDefinition challenge in schedule.Challenges)
            {
                foreach (ChallengeTask task in challenge.Tasks)
                {
                    TaskProgress progress = ChallengeProgress.GetOrCreate(state, schedule.Key, challenge.Id, task.Id);
                    progress.Amount = amount;
                    progress.LastUpdate = 1;
                }
            }

            return state;
        }

        private static string BlueprintDirectory()
        {
            string copied = Path.Combine(AppContext.BaseDirectory, "blueprints");
            if (Directory.Exists(copied))
            {
                return copied;
            }

            for (DirectoryInfo? dir = new(AppContext.BaseDirectory); dir != null; dir = dir.Parent)
            {
                string candidate = Path.Combine(dir.FullName, "examples", "blueprints");
                if (Directory.Exists(candidate))
                {
                    return candidate;
                }
            }

            throw new DirectoryNotFoundException("examples/blueprints not found");
        }
    }

    /// <summary>
    /// The pre-demand <c>PlayerDeath.Fill</c> (phase 0 and earlier): every key and alias as invariant text,
    /// player keys formatted per call for every prefix including the <c>victim</c> alias.
    /// </summary>
    internal static class LegacyFill
    {
        public static void Fill(FakeDeath e, Dictionary<string, string> d)
        {
            d["assistedflash"] = Bool(e.Assistedflash);
            d["attackerblind"] = Bool(e.Attackerblind);
            d["attackerinair"] = Bool(e.Attackerinair);
            d["distance"] = Num(e.Distance);
            d["dmg_armor"] = Num(e.DmgArmor);
            d["dmgarmor"] = d["dmg_armor"];
            d["dmg_health"] = Num(e.DmgHealth);
            d["dmghealth"] = d["dmg_health"];
            d["dominated"] = Bool(e.Dominated > 0);
            d["headshot"] = Bool(e.Headshot);
            d["hitgroup"] = Num(e.Hitgroup);
            d["noreplay"] = Bool(e.Noreplay);
            d["noscope"] = Bool(e.Noscope);
            d["penetrated"] = Bool(e.Penetrated > 0);
            d["revenge"] = Bool(e.Revenge > 0);
            d["thrusmoke"] = Bool(e.Thrusmoke);
            d["weapon"] = e.Weapon;
            d["weapon_fauxitemid"] = e.WeaponFauxitemid;
            d["weapon_itemid"] = e.WeaponItemid;
            d["weaponitemid"] = d["weapon_itemid"];
            d["weapon_originalowner_xuid"] = e.WeaponOriginalownerXuid;
            d["wipe"] = Num(e.Wipe);
            FillPlayer(d, e.Assister, "assister");
            FillPlayer(d, e.Attacker, "attacker");
            FillPlayer(d, e.Userid, "userid");
            FillPlayer(d, e.Userid, "victim");
            d["isteamkill"] = Bool(e.Attacker != null && e.Userid != null && e.Attacker.Team == e.Userid.Team);
            d["isselfkill"] = Bool(e.Attacker == e.Userid);
        }

        private static string Bool(bool value) => value ? "true" : "false";

        private static string Num<T>(T value) where T : IFormattable =>
            value.ToString(null, CultureInfo.InvariantCulture);

        private static void FillPlayer(Dictionary<string, string> d, FakePlayer? player, string prefix)
        {
            if (player == null)
            {
                return;
            }

            d[$"{prefix}.name"] = player.Name;
            d[$"{prefix}.isbot"] = Bool(player.IsBot);
            d[$"{prefix}.team"] = player.Team.ToString();
            d[$"{prefix}.alive"] = Bool(player.Alive);
            d[$"{prefix}.ping"] = Num(player.Ping);
            d[$"{prefix}.money"] = Num(player.Money);
            d[$"{prefix}.score"] = Num(player.Score);
            d[$"{prefix}.stats.kills"] = Num(player.Kills);
            d[$"{prefix}.stats.assists"] = Num(player.Assists);
            d[$"{prefix}.stats.deaths"] = Num(player.Deaths);
            d[$"{prefix}.stats.damage"] = Num(player.Damage);
            d[$"{prefix}.health"] = Num(player.Health);
            d[$"{prefix}.armor"] = Num(player.Armor);
            d[$"{prefix}.hasdefusor"] = Bool(player.HasDefuser);
            d[$"{prefix}.hashelmet"] = Bool(player.HasHelmet);
        }
    }

    /// <summary>The generated <c>PlayerDeath.Fill</c> and <c>EventData.FillPlayer</c> over fake values.</summary>
    internal static class DemandFill
    {
        public static readonly string[] EventKeys =
        [
            "assistedflash", "attackerblind", "attackerinair", "distance", "dmg_armor", "dmg_health", "dominated",
            "headshot", "hitgroup", "noreplay", "noscope", "penetrated", "revenge", "thrusmoke", "weapon",
            "weapon_fauxitemid", "weapon_itemid", "weapon_originalowner_xuid", "wipe",
        ];

        public static void Fill(FakeDeath e, Dictionary<string, EventValue> d, FillDemand need)
        {
            if (need.Keys[0])
            {
                d["assistedflash"] = EventValue.Of(e.Assistedflash);
            }
            if (need.Keys[1])
            {
                d["attackerblind"] = EventValue.Of(e.Attackerblind);
            }
            if (need.Keys[2])
            {
                d["attackerinair"] = EventValue.Of(e.Attackerinair);
            }
            if (need.Keys[3])
            {
                d["distance"] = EventValue.Of(e.Distance);
            }
            if (need.Keys[4])
            {
                d["dmg_armor"] = EventValue.Of(e.DmgArmor);
            }
            if (need.Keys[5])
            {
                d["dmg_health"] = EventValue.Of(e.DmgHealth);
            }
            if (need.Keys[6])
            {
                d["dominated"] = EventValue.Of(e.Dominated > 0);
            }
            if (need.Keys[7])
            {
                d["headshot"] = EventValue.Of(e.Headshot);
            }
            if (need.Keys[8])
            {
                d["hitgroup"] = EventValue.Of(e.Hitgroup);
            }
            if (need.Keys[9])
            {
                d["noreplay"] = EventValue.Of(e.Noreplay);
            }
            if (need.Keys[10])
            {
                d["noscope"] = EventValue.Of(e.Noscope);
            }
            if (need.Keys[11])
            {
                d["penetrated"] = EventValue.Of(e.Penetrated > 0);
            }
            if (need.Keys[12])
            {
                d["revenge"] = EventValue.Of(e.Revenge > 0);
            }
            if (need.Keys[13])
            {
                d["thrusmoke"] = EventValue.Of(e.Thrusmoke);
            }
            if (need.Keys[14])
            {
                d["weapon"] = EventValue.Of(e.Weapon);
            }
            if (need.Keys[15])
            {
                d["weapon_fauxitemid"] = EventValue.Of(e.WeaponFauxitemid);
            }
            if (need.Keys[16])
            {
                d["weapon_itemid"] = EventValue.Of(e.WeaponItemid);
            }
            if (need.Keys[17])
            {
                d["weapon_originalowner_xuid"] = EventValue.Of(e.WeaponOriginalownerXuid);
            }
            if (need.Keys[18])
            {
                d["wipe"] = EventValue.Of(e.Wipe);
            }
            if (need.Prefixes[0] != PlayerFields.None)
            {
                FillPlayer(d, e.Assister, need.PrefixKeys[0], need.Prefixes[0]);
            }
            if (need.Prefixes[1] != PlayerFields.None)
            {
                FillPlayer(d, e.Attacker, need.PrefixKeys[1], need.Prefixes[1]);
            }
            if (need.Prefixes[2] != PlayerFields.None)
            {
                FillPlayer(d, e.Userid, need.PrefixKeys[2], need.Prefixes[2]);
            }
            if (need.Derived)
            {
                d["isteamkill"] = EventValue.Of(e.Attacker != null && e.Userid != null && e.Attacker.Team == e.Userid.Team);
                d["isselfkill"] = EventValue.Of(e.Attacker == e.Userid);
            }
        }

        private static void FillPlayer(
            Dictionary<string, EventValue> d,
            FakePlayer? player,
            string[] keys,
            PlayerFields fields)
        {
            if (fields == PlayerFields.None || player == null)
            {
                return;
            }

            for (int i = 0; i < PlayerKeys.Count; i++)
            {
                PlayerFields bit = (PlayerFields)(1 << i);
                if ((fields & bit) != 0)
                {
                    d[keys[i]] = Read(player, bit);
                }
            }
        }

        private static EventValue Read(FakePlayer player, PlayerFields field) =>
            field switch
            {
                PlayerFields.Name => EventValue.Of(player.Name),
                PlayerFields.IsBot => EventValue.Of(player.IsBot),
                PlayerFields.Team => EventValue.Of(player.Team.ToString()),
                PlayerFields.Alive => EventValue.Of(player.Alive),
                PlayerFields.Ping => EventValue.Of(player.Ping),
                PlayerFields.Money => EventValue.Of(player.Money),
                PlayerFields.Score => EventValue.Of(player.Score),
                PlayerFields.StatsKills => EventValue.Of(player.Kills),
                PlayerFields.StatsAssists => EventValue.Of(player.Assists),
                PlayerFields.StatsDeaths => EventValue.Of(player.Deaths),
                PlayerFields.StatsDamage => EventValue.Of(player.Damage),
                PlayerFields.Health => EventValue.Of(player.Health),
                PlayerFields.Armor => EventValue.Of(player.Armor),
                PlayerFields.HasDefusor => EventValue.Of(player.HasDefuser),
                PlayerFields.HasHelmet => EventValue.Of(player.HasHelmet),
                _ => EventValue.Of(string.Empty),
            };
    }
}

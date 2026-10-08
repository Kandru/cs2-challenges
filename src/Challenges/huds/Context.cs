using System.Globalization;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Translations;
using Challenges.Classes;
using Challenges.Configs;
using Challenges.Enums;
using Challenges.Utils;
using Microsoft.Extensions.Localization;

namespace Challenges.Huds
{
    /// <summary>Shared access to plugin state for the static HUD painters. Bound by <see cref="HudDriver"/>.</summary>
    public static class Context
    {
        private static Dictionary<GlobalStates, object>? _globals;
        private static IStringLocalizer? _localizer;

        public static bool IsBound => _globals != null && _localizer != null;

        public static void Bind(Dictionary<GlobalStates, object> globals, IStringLocalizer localizer)
        {
            _globals = globals;
            _localizer = localizer;
        }

        public static void Unbind()
        {
            _globals = null;
            _localizer = null;
        }

        public static PluginConfig Config =>
            (PluginConfig)(_globals?[GlobalStates.GlobalConfig] ?? new PluginConfig());

        public static Dictionary<CCSPlayerController, PlayerState>? States =>
            _globals?[GlobalStates.PlayerStates] as Dictionary<CCSPlayerController, PlayerState>;

        public static PlayerArchive? Archive =>
            _globals?[GlobalStates.PlayerArchive] as PlayerArchive;

        public static RunningSchedule? Schedule
        {
            get
            {
                if (_globals?[GlobalStates.ClassInstances] is Dictionary<string, ClassesBlueprint> classes
                    && classes.TryGetValue(nameof(Schedules), out ClassesBlueprint? entry)
                    && entry is Schedules schedules)
                {
                    return schedules.Current;
                }

                return null;
            }
        }

        public static IReadOnlyDictionary<string, ChallengeSchedule> ScheduleMap =>
            _globals?[GlobalStates.Schedules] as Dictionary<string, ChallengeSchedule>
            ?? new Dictionary<string, ChallengeSchedule>();

        public static IReadOnlyDictionary<string, ChallengeDefinition> ChallengeMap =>
            _globals?[GlobalStates.Challenges] as Dictionary<string, ChallengeDefinition>
            ?? new Dictionary<string, ChallengeDefinition>();

        public static PlayerState? GetState(CCSPlayerController player) =>
            States != null && States.TryGetValue(player, out PlayerState? state) ? state : null;

        public static string Text(CCSPlayerController player, string key) =>
            _localizer == null ? key : LocalizerExtensions.ForPlayer(_localizer, player, key);

        public static string Text(
            CCSPlayerController player,
            string key,
            params (string Placeholder, string Value)[] replacements)
        {
            string value = Text(player, key);
            foreach ((string placeholder, string replacement) in replacements)
            {
                value = value.Replace(placeholder, replacement);
            }

            return value;
        }

        public static string FormatPage(CCSPlayerController player, int page, int pages) =>
            Text(player, "hud.format.page", ("{page}", page.ToString()), ("{pages}", pages.ToString()));

        public static string FormatPercent(CCSPlayerController player, int percent) =>
            Text(player, "hud.format.percent", ("{percent}", percent.ToString()));

        public static string FormatCount(CCSPlayerController player, int solved, int total) =>
            Text(
                player,
                "hud.format.count",
                ("{solved}", FormatNumber(player, solved)),
                ("{total}", FormatNumber(player, total)));

        public static string FormatNumber(CCSPlayerController player, int value)
        {
            CultureInfo culture = player.GetLanguage();
            return value.ToString("N0", culture);
        }

        public static string FormatRank(CCSPlayerController player, int rank) =>
            Text(player, "hud.format.rank", ("{rank}", FormatNumber(player, rank)));

        public static string FormatOverflow(CCSPlayerController player, int count) =>
            Text(player, "hud.menu.task.overflow", ("{count}", count.ToString()));

        /// <summary>
        /// Relative schedule phrase: time left while active, otherwise time until next start
        /// ("two days left", "in a week", "this week", …).
        /// </summary>
        public static string FormatWhen(CCSPlayerController player, ScheduleTiming.Info info, DateTime now)
        {
            if (info.Target is not { } target || target <= now)
            {
                return string.Empty;
            }

            TimeSpan span = target - now;
            bool upcoming = !info.IsActive;

            if (span.TotalHours < 36)
            {
                return WhenUnit(player, upcoming, "hour", Math.Max(1, (int)Math.Round(span.TotalHours)));
            }

            if (span.TotalDays < 3)
            {
                return WhenUnit(player, upcoming, "day", Math.Max(1, (int)Math.Round(span.TotalDays)));
            }

            if (!upcoming && SameUtcWeek(now, target))
            {
                return Text(player, "hud.menu.when.this_week");
            }

            if (span.TotalDays < 25)
            {
                return WhenUnit(player, upcoming, "week", Math.Max(1, (int)Math.Round(span.TotalDays / 7)));
            }

            return WhenUnit(player, upcoming, "month", Math.Max(1, (int)Math.Round(span.TotalDays / 30)));
        }

        private static string WhenUnit(CCSPlayerController player, bool upcoming, string unit, int count)
        {
            string side = upcoming ? "in" : "left";
            string form = count switch
            {
                1 => "one",
                2 => "two",
                _ => "n",
            };
            string key = $"hud.menu.when.{side}.{unit}.{form}";
            return form == "n"
                ? Text(player, key, ("{n}", count.ToString()))
                : Text(player, key);
        }

        private static bool SameUtcWeek(DateTime a, DateTime b)
        {
            static DateTime Monday(DateTime d) => d.Date.AddDays(-(((int)d.DayOfWeek + 6) % 7));
            return Monday(a) == Monday(b);
        }
    }
}

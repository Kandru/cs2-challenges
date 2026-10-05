using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Translations;
using Challenges.Classes;
using Challenges.Configs;
using Challenges.Enums;
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
    }
}

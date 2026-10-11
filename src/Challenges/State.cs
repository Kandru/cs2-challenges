using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Translations;

namespace Challenges
{
    /// <summary>Shared runtime state for modules and HUD painters.</summary>
    public sealed class State
    {
        public Dictionary<string, Module> Modules { get; } = new();
        public PluginConfig Config { get; set; } = new();
        public Dictionary<CCSPlayerController, PlayerState> PlayerStates { get; } = new();
        public PlayerArchive Archive { get; } = new();
        public PlayerLanguageManager? PlayerLanguageManager { get; set; }
        public Dictionary<string, ChallengeDefinition> Challenges { get; set; } =
            new(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, ChallengeSchedule> Schedules { get; set; } =
            new(StringComparer.OrdinalIgnoreCase);
        public bool DuringRound { get; set; }
        public bool FreezeActive { get; set; }
    }
}

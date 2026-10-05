using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Translations;
using Challenges.Configs;
using Challenges.Enums;
using Challenges.Utils;
using Microsoft.Extensions.Localization;

namespace Challenges.Classes
{
    public class Blueprint(Dictionary<GlobalStates, object> GlobalState, IStringLocalizer Localizer, bool IsHotReloaded)
    {
        public PluginConfig GlobalConfig => (PluginConfig)_globalStates[GlobalStates.GlobalConfig];
        public readonly Dictionary<CCSPlayerController, PlayerState> PlayerStates =
            (Dictionary<CCSPlayerController, PlayerState>)GlobalState[GlobalStates.PlayerStates];
        public PlayerLanguageManager PlayerLanguageManager =>
            (PlayerLanguageManager)_globalStates[GlobalStates.PlayerLanguageManager];
        public readonly Dictionary<GlobalStates, object> _globalStates = GlobalState;
        public readonly IStringLocalizer Localizer = Localizer;
        public readonly bool IsHotReloaded = IsHotReloaded;

        public virtual List<string> Events => [];
        public virtual List<string> Listeners => [];
        public virtual Dictionary<int, HookMode> UserMessages => [];
        public virtual Dictionary<string, HookMode> UserMessageNames => [];
        public virtual Dictionary<string, string> Commands => [];
        public virtual Dictionary<string, HookMode> CommandListeners => [];

        public virtual void Destroy()
        {
        }

        public PlayerState GetPlayerState(CCSPlayerController player)
        {
            if (!PlayerStates.TryGetValue(player, out PlayerState? state))
            {
                state = new PlayerState();
                PlayerStates[player] = state;
            }
            return state;
        }

        protected T GetClass<T>() where T : Blueprint
        {
            Dictionary<string, Blueprint> classInstances =
                (Dictionary<string, Blueprint>)_globalStates[GlobalStates.ClassInstances];
            return (T)classInstances[typeof(T).Name];
        }

        public void DebugPrint(string message)
        {
            if (!GlobalConfig.Debug)
            {
                return;
            }
            Console.WriteLine(Localizer["core.debugprint"].Value.Replace("{message}", message));
        }
    }
}

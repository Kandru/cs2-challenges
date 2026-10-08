using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Translations;
using Challenges.Configs;
using Challenges.Enums;
using Challenges.Utils;
using Microsoft.Extensions.Localization;

namespace Challenges.Classes
{
    public readonly record struct CommandBinding(string Description, string Method);

    public class ClassesBlueprint(Dictionary<GlobalStates, object> GlobalState, IStringLocalizer Localizer, bool IsHotReloaded)
    {
        public PluginConfig GlobalConfig => (PluginConfig)_globalStates[GlobalStates.GlobalConfig];
        public readonly Dictionary<CCSPlayerController, PlayerState> PlayerStates =
            (Dictionary<CCSPlayerController, PlayerState>)GlobalState[GlobalStates.PlayerStates];
        public PlayerArchive Archive => (PlayerArchive)_globalStates[GlobalStates.PlayerArchive];
        public PlayerLanguageManager PlayerLanguageManager =>
            (PlayerLanguageManager)_globalStates[GlobalStates.PlayerLanguageManager];
        public readonly Dictionary<GlobalStates, object> _globalStates = GlobalState;
        public readonly IStringLocalizer Localizer = Localizer;
        public readonly bool IsHotReloaded = IsHotReloaded;

        public virtual List<string> Events => [];
        public virtual List<string> Listeners => [];
        public virtual Dictionary<int, HookMode> UserMessages => [];
        public virtual Dictionary<string, HookMode> UserMessageNames => [];
        public virtual Dictionary<string, CommandBinding> Commands => [];
        public virtual Dictionary<string, HookMode> CommandListeners => [];

        public virtual void Destroy()
        {
        }

        /// <summary>
        /// Hot path: already-bound controllers return on the first dictionary probe.
        /// Archive is only touched when the session entry is missing or unbound.
        /// </summary>
        public PlayerState GetPlayerState(CCSPlayerController player)
        {
            if (PlayerStates.TryGetValue(player, out PlayerState? state) && state.SteamId.Length > 0)
            {
                return state;
            }

            string steamId = player.IsValid ? player.NetworkIDString ?? string.Empty : string.Empty;
            if (steamId.Length == 0)
            {
                if (state != null)
                {
                    return state;
                }

                state = new PlayerState();
                PlayerStates[player] = state;
                return state;
            }

            return AdoptArchive(player, steamId, state);
        }

        /// <summary>Binds a controller to an archive entry once a Steam id is known.</summary>
        public PlayerState BindPlayer(CCSPlayerController player, string steamId) =>
            AdoptArchive(
                player,
                steamId,
                PlayerStates.TryGetValue(player, out PlayerState? existing) ? existing : null);

        private PlayerState AdoptArchive(CCSPlayerController player, string steamId, PlayerState? placeholder)
        {
            PlayerState archived = Archive.GetOrCreate(steamId);
            if (placeholder != null
                && !ReferenceEquals(placeholder, archived)
                && placeholder.Dirty
                && placeholder.HasProgress
                && !archived.HasProgress)
            {
                archived.Language = placeholder.Language;
                archived.Challenges = placeholder.Challenges;
                archived.Statistics = placeholder.Statistics;
                archived.Dirty = true;
            }

            DropStaleControllers(player, archived);
            PlayerStates[player] = archived;
            if (archived.SteamId.Length == 0)
            {
                archived.SteamId = steamId;
            }

            return archived;
        }

        private void DropStaleControllers(CCSPlayerController player, PlayerState archived)
        {
            CCSPlayerController? stale = null;
            foreach ((CCSPlayerController other, PlayerState otherState) in PlayerStates)
            {
                if (other == player || !ReferenceEquals(otherState, archived) || other.IsValid)
                {
                    continue;
                }

                // At most one stale controller shares an archive entry in practice.
                stale = other;
                break;
            }

            if (stale == null)
            {
                return;
            }

            PlayerStates.Remove(stale);
            if (_globalStates[GlobalStates.ClassInstances] is Dictionary<string, ClassesBlueprint> classes
                && classes.TryGetValue(nameof(ChallengeEngine), out ClassesBlueprint? engine)
                && engine is ChallengeEngine challengeEngine)
            {
                challengeEngine.ForgetPlayer(archived);
            }
        }

        protected T GetClass<T>() where T : ClassesBlueprint
        {
            Dictionary<string, ClassesBlueprint> classInstances =
                (Dictionary<string, ClassesBlueprint>)_globalStates[GlobalStates.ClassInstances];
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

        protected string ChatMessage(CCSPlayerController player, string key) =>
            LocalizerExtensions.ForPlayer(Localizer, player, key)
                .Replace("{prefix}", LocalizerExtensions.ForPlayer(Localizer, player, "chat.prefix"));
    }
}

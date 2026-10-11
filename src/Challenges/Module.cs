using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Translations;
using Microsoft.Extensions.Localization;

namespace Challenges
{
    public class Module(State state, IStringLocalizer localizer)
    {
        public PluginConfig GlobalConfig => State.Config;
        public Dictionary<CCSPlayerController, PlayerState> PlayerStates => State.PlayerStates;
        public PlayerArchive Archive => State.Archive;
        public PlayerLanguageManager PlayerLanguageManager => State.PlayerLanguageManager!;
        public readonly State State = state;
        public readonly IStringLocalizer Localizer = localizer;

        public virtual List<string> Events => [];
        public virtual List<string> Listeners => [];
        public virtual Dictionary<string, CommandBinding> Commands => [];

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

                stale = other;
                break;
            }

            if (stale == null)
            {
                return;
            }

            PlayerStates.Remove(stale);
            if (State.Modules.TryGetValue(nameof(ChallengeEngine), out Module? engine)
                && engine is ChallengeEngine challengeEngine)
            {
                challengeEngine.ForgetPlayer(stale, archived);
            }
        }

        protected T GetModule<T>() where T : Module => (T)State.Modules[typeof(T).Name];

        public void DebugPrint(string message)
        {
            if (!GlobalConfig.Debug.Enable)
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

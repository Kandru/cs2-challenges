using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Translations;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Entities;
using CounterStrikeSharp.API.Modules.Events;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Localization;

namespace Challenges
{
    public class PlayerManagement : Module
    {
        private const string MenuCommandDescription = "Toggle challenges menu";
        private static readonly Action<string> Warn = static message => Console.WriteLine($"[Challenges] {message}");

        public PlayerManagement(State state, IStringLocalizer localizer, bool isHotReloaded)
            : base(state, localizer)
        {
            Commands = BuildMenuCommands(GlobalConfig.MenuCommands);
            Events = BuildEvents(GlobalConfig.Persistence);
            Archive.EnsureLoaded(GlobalConfig, warn: Warn, debug: DebugPrint);
            Saves = new PlayerSaveQueue(Archive, GlobalConfig, Warn, DebugPrint);
            Server.NextFrame(BindOnlinePlayers);
        }

        public override List<string> Events { get; }

        public override Dictionary<string, CommandBinding> Commands { get; }

        /// <summary>Budgeted background saves; pumped from the plugin tick.</summary>
        internal PlayerSaveQueue Saves { get; }

        private static List<string> BuildEvents(PluginConfigPersistence persistence)
        {
            List<string> events =
            [
                "EventPlayerConnectFull",
                "EventPlayerDisconnect",
                "EventPlayerChat",
                "EventRoundStart",
            ];
            if (persistence.AutosaveOnRoundEnd)
            {
                events.Add("EventRoundEnd");
            }

            return events;
        }

        private static Dictionary<string, CommandBinding> BuildMenuCommands(IEnumerable<string> entries)
        {
            Dictionary<string, CommandBinding> commands = new(StringComparer.OrdinalIgnoreCase);
            foreach (string entry in entries)
            {
                string? name = MenuCommandNames.DisplayName(entry);
                if (name != null)
                {
                    commands.TryAdd("css_" + name, new CommandBinding(MenuCommandDescription, nameof(CommandMenu)));
                }
            }

            return commands;
        }

        public HookResult EventPlayerConnectFull(EventPlayerConnectFull @event, GameEventInfo info)
        {
            ScheduleBind(@event.Userid);
            return HookResult.Continue;
        }

        public HookResult EventRoundStart(EventRoundStart @event, GameEventInfo info)
        {
            BindOnlinePlayers();
            return HookResult.Continue;
        }

        public HookResult EventRoundEnd(EventRoundEnd @event, GameEventInfo info)
        {
            int queued = 0;
            foreach (PlayerState state in PlayerStates.Values)
            {
                if (!state.Dirty || state.SteamId.Length == 0)
                {
                    continue;
                }

                Saves.Enqueue(state);
                queued++;
            }

            if (queued > 0 && GlobalConfig.Debug.Enable)
            {
                DebugPrint($"round end: queued {queued} dirty player(s) for autosave");
            }

            return HookResult.Continue;
        }

        public HookResult EventPlayerDisconnect(EventPlayerDisconnect @event, GameEventInfo info)
        {
            CCSPlayerController? player = @event.Userid;
            if (player == null || !PlayerStates.TryGetValue(player, out PlayerState? state))
            {
                return HookResult.Continue;
            }

            Detach(player, state);
            return HookResult.Continue;
        }

        public HookResult EventPlayerChat(EventPlayerChat @event, GameEventInfo info)
        {
            CCSPlayerController? player = Utilities.GetPlayerFromUserid(@event.Userid);
            if (!Players.IsHumanViewer(player)
                || !@event.Text.StartsWith("!lang", StringComparison.OrdinalIgnoreCase))
            {
                return HookResult.Continue;
            }

            string[] parts = @event.Text.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (parts.Length < 2)
            {
                return HookResult.Continue;
            }

            string language = parts[1];
            if (!CultureInfo.GetCultures(CultureTypes.AllCultures)
                    .Any(c => c.Name.Equals(language, StringComparison.OrdinalIgnoreCase)))
            {
                return HookResult.Continue;
            }

            PlayerState state = GetPlayerState(player);
            if (!string.Equals(state.Language, language, StringComparison.Ordinal))
            {
                string previous = state.Language;
                state.Language = language;
                state.Dirty = true;
                DebugPrint($"{player.PlayerName} language {previous} -> {language}");
            }

            TrySetLanguage(player.NetworkIDString, language);
            Server.NextFrame(() =>
            {
                if (!player.IsValid || !PlayerStates.TryGetValue(player, out PlayerState? live))
                {
                    return;
                }

                live.TrackerContentDirty = true;
                Tracker.Refresh(player);
                if (HudMenu.IsOpen(player))
                {
                    Menu.Paint(player);
                }
            });
            return HookResult.Continue;
        }

        public void CommandMenu(CCSPlayerController? player, CommandInfo _)
        {
            if (!Players.IsHumanViewer(player) || !GlobalConfig.Enabled)
            {
                return;
            }

            if (HudMenu.IsOpen(player) || HudMenu.IsOpenPending(player))
            {
                HudMenu.Close(player);
                GetPlayerState(player).ActiveMenu = ActiveMenu.None;
                return;
            }

            HudMenu.RequestOpen(player, static live => Menu.Open(live));
        }

        public override void Destroy()
        {
            Saves.FlushSync();
            int sessionDirty = 0;
            foreach ((CCSPlayerController player, PlayerState state) in PlayerStates)
            {
                if (player.IsValid)
                {
                    ApplyIdentity(player, state);
                }

                if (state.Dirty)
                {
                    sessionDirty++;
                    Archive.TryWrite(GlobalConfig, state, warn: Warn, debug: DebugPrint);
                }

                state.ResetSession();
            }

            DebugPrint($"player teardown: session={PlayerStates.Count} dirty={sessionDirty}");
            Archive.FlushDirty(GlobalConfig, warn: Warn, debug: DebugPrint);
        }

        private void ScheduleBind(CCSPlayerController? player)
        {
            if (!Players.IsHumanViewer(player))
            {
                return;
            }

            Bind(player);
            Server.NextFrame(() =>
            {
                if (player.IsValid)
                {
                    Bind(player);
                }
            });
        }

        private void BindOnlinePlayers()
        {
            foreach (CCSPlayerController player in Players.GetHumans())
            {
                Bind(player);
            }
        }

        private void Bind(CCSPlayerController player)
        {
            if (!Players.IsHumanViewer(player))
            {
                return;
            }

            string steamId = player.NetworkIDString;
            if (!SteamIds.IsCreditable(steamId))
            {
                DebugPrint($"bind skipped for {player.PlayerName}: missing steam id");
                return;
            }

            // Already bound: leave identity alone (updated on disconnect / destroy).
            if (PlayerStates.TryGetValue(player, out PlayerState? existing)
                && existing.SteamId.Length > 0
                && existing.SteamId.Equals(steamId, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            bool created = Archive.TryGet(steamId) == null;
            PlayerState state = BindPlayer(player, steamId);
            ApplyIdentity(player, state);
            Menu.InvalidateScoreboard();
            TrySetLanguage(steamId, state.Language);
            if (created)
            {
                DebugPrint($"connect {state.Username} ({steamId}): new player created in archive");
            }
            else
            {
                DebugPrint(
                    $"connect {state.Username} ({steamId}): loaded from archive "
                    + $"(solved={state.Statistics.AmountChallengesSolved}, schedules={state.Challenges.Count})");
            }
        }

        private void Detach(CCSPlayerController player, PlayerState state)
        {
            ApplyIdentity(player, state);
            string label = $"{state.Username} ({state.SteamId})";
            string steamId = state.SteamId;
            ChallengeEngine engine = GetModule<ChallengeEngine>();

            CustomHud.ReleasePlayer(player);
            PlayerStates.Remove(player);
            engine.ForgetPlayer(player, state);
            state.ResetSession();
            Archive.PatchOfflineScore(state, Context.Schedule, DebugPrint);
            Menu.InvalidateScoreboard();

            void Save()
            {
                if (!state.Dirty)
                {
                    DebugPrint($"disconnect {label}: no save (unchanged)");
                    return;
                }

                DebugPrint($"disconnect {label}: queued dirty state for saving");
                Saves.Enqueue(state);
            }

            if (engine.WhenIdle(steamId, Save))
            {
                DebugPrint($"disconnect {label}: deferring save until progress queue drains");
            }
        }

        private static void ApplyIdentity(CCSPlayerController player, PlayerState state)
        {
            if (!player.IsValid)
            {
                return;
            }

            string steamId = player.NetworkIDString;
            if (string.IsNullOrEmpty(steamId))
            {
                return;
            }

            string name = player.PlayerName ?? string.Empty;
            string clan = player.Clan ?? string.Empty;
            if (state.SteamId.Equals(steamId, StringComparison.OrdinalIgnoreCase)
                && state.Username == name
                && state.ClanTag == clan)
            {
                return;
            }

            state.SteamId = steamId;
            state.Username = name;
            state.ClanTag = clan;
            state.Dirty = true;
        }

        private void TrySetLanguage(string steamId, string language)
        {
            if (string.IsNullOrEmpty(steamId) || string.IsNullOrEmpty(language))
            {
                return;
            }

            try
            {
                PlayerLanguageManager.SetLanguage(new SteamID(steamId), new CultureInfo(language));
            }
            catch
            {
            }
        }
    }
}

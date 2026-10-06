using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Core.Translations;
using CounterStrikeSharp.API.Modules.Entities;
using CounterStrikeSharp.API.Modules.Events;
using Challenges.Configs;
using Challenges.Enums;
using Challenges.Huds;
using Challenges.Utils;
using Microsoft.Extensions.Localization;
using System.Globalization;

namespace Challenges.Classes
{
    public class PlayerManagement : ClassesBlueprint
    {
        private const string MenuCommandDescription = "Toggle challenges menu";

        public PlayerManagement(Dictionary<GlobalStates, object> globalState, IStringLocalizer localizer, bool isHotReloaded)
            : base(globalState, localizer, isHotReloaded)
        {
            Commands = BuildMenuCommands(GlobalConfig.MenuCommands);
            Server.NextFrame(LoadOnlinePlayers);
        }

        public override List<string> Events =>
        [
            "EventPlayerConnectFull",
            "EventPlayerDisconnect",
            "EventPlayerChat",
            "EventRoundStart",
        ];

        public override Dictionary<string, CommandBinding> Commands { get; }

        private static Dictionary<string, CommandBinding> BuildMenuCommands(IEnumerable<string> entries)
        {
            Dictionary<string, CommandBinding> commands = new(StringComparer.OrdinalIgnoreCase);
            foreach (string entry in entries)
            {
                string? name = NormalizeMenuCommand(entry);
                if (name != null)
                {
                    commands.TryAdd(name, new CommandBinding(MenuCommandDescription, nameof(CommandMenu)));
                }
            }

            return commands;
        }

        private static string? NormalizeMenuCommand(string entry)
        {
            string name = entry.Trim().TrimStart('!', '/').Trim();
            if (name.Length == 0)
            {
                return null;
            }

            return name.StartsWith("css_", StringComparison.OrdinalIgnoreCase)
                ? name
                : "css_" + name;
        }

        public HookResult EventPlayerConnectFull(EventPlayerConnectFull @event, GameEventInfo info)
        {
            ScheduleLoad(@event.Userid);
            return HookResult.Continue;
        }

        public HookResult EventRoundStart(EventRoundStart @event, GameEventInfo info)
        {
            LoadOnlinePlayers();
            return HookResult.Continue;
        }

        public HookResult EventPlayerDisconnect(EventPlayerDisconnect @event, GameEventInfo info)
        {
            CCSPlayerController? player = @event.Userid;
            if (player == null || !PlayerStates.TryGetValue(player, out PlayerState? state))
            {
                return HookResult.Continue;
            }

            Save(player, state);
            CustomHud.ReleasePlayer(player);
            PlayerStates.Remove(player);
            GetClass<ChallengeEngine>().ForgetPlayer(state);
            return HookResult.Continue;
        }

        public HookResult EventPlayerChat(EventPlayerChat @event, GameEventInfo info)
        {
            CCSPlayerController? player = Utilities.GetPlayerFromUserid(@event.Userid);
            if (!Players.IsHumanViewer(player))
            {
                return HookResult.Continue;
            }

            if (!@event.Text.StartsWith("!lang", StringComparison.OrdinalIgnoreCase))
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
            state.Language = language;
            TrySetLanguage(player.NetworkIDString, language);
            Server.NextFrame(() =>
            {
                if (!player.IsValid || !PlayerStates.ContainsKey(player))
                {
                    return;
                }

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

            if (HudMenu.IsOpen(player))
            {
                HudMenu.Close(player);
                GetPlayerState(player).ActiveMenu = ActiveMenu.None;
                return;
            }

            Menu.Open(player);
        }

        public override void Destroy()
        {
            foreach ((CCSPlayerController player, PlayerState state) in PlayerStates)
            {
                Save(player, state);
            }
        }

        private void ScheduleLoad(CCSPlayerController? player)
        {
            if (!Players.IsHumanViewer(player))
            {
                return;
            }

            Server.NextFrame(() =>
            {
                if (player.IsValid)
                {
                    Load(player);
                }
            });
        }

        private void LoadOnlinePlayers()
        {
            foreach (CCSPlayerController player in Players.GetHumans())
            {
                Load(player);
            }
        }

        private void Load(CCSPlayerController player)
        {
            if (!Players.IsHumanViewer(player))
            {
                return;
            }

            string steamId = player.NetworkIDString;
            if (string.IsNullOrEmpty(steamId))
            {
                return;
            }

            PlayerState state = GetPlayerState(player);
            if (state.Loaded)
            {
                return;
            }

            string path = PlayerFiles.FilePath(GlobalConfig, steamId);
            PlayerState? saved = PlayerFiles.TryRead(path, ex => DebugPrint(ex.Message));
            if (saved != null)
            {
                state.CopyProgressFrom(saved);
            }

            ApplyIdentity(player, state, steamId);
            state.Loaded = true;
            TrySetLanguage(steamId, state.Language);
        }

        private void Save(CCSPlayerController player, PlayerState state)
        {
            string steamId = state.SteamId;
            if (player.IsValid && !string.IsNullOrEmpty(player.NetworkIDString))
            {
                steamId = player.NetworkIDString;
                ApplyIdentity(player, state, steamId);
            }

            if (string.IsNullOrEmpty(steamId))
            {
                return;
            }

            PlayerFiles.Write(PlayerFiles.FilePath(GlobalConfig, steamId), state);
        }

        private static void ApplyIdentity(CCSPlayerController player, PlayerState state, string steamId)
        {
            state.Username = player.PlayerName;
            state.SteamId = steamId;
            state.ClanTag = player.Clan ?? string.Empty;
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

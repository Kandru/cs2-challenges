using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Extensions;
using CounterStrikeSharp.API.Core.Translations;
using CounterStrikeSharp.API.Modules.Entities;
using CounterStrikeSharp.API.Modules.Events;
using Challenges.Configs;
using Challenges.Enums;
using Challenges.Huds;
using Challenges.Utils;
using Microsoft.Extensions.Localization;
using System.Globalization;
using System.Text.Json;

namespace Challenges.Classes
{
    public class PlayerManagement : ClassesBlueprint
    {
        private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

        public PlayerManagement(Dictionary<GlobalStates, object> globalState, IStringLocalizer localizer, bool isHotReloaded)
            : base(globalState, localizer, isHotReloaded)
        {
            if (isHotReloaded)
            {
                foreach (CCSPlayerController player in Players.GetHumans())
                {
                    LoadPlayerData(player);
                }
            }
        }

        public override List<string> Events =>
        [
            "EventPlayerConnectFull",
            "EventPlayerDisconnect",
            "EventPlayerChat",
        ];

        public override Dictionary<string, string> Commands => new()
        {
            ["css_c"] = "Toggle challenges menu",
            ["css_challenges"] = "Toggle challenges menu",
            ["css_topc"] = "Show top players by challenges solved",
        };

        private string PlayersDir => Path.Combine(
            Path.GetDirectoryName(GlobalConfig.GetConfigPath()) ?? ".",
            "players");

        public HookResult EventPlayerConnectFull(EventPlayerConnectFull @event, GameEventInfo info)
        {
            CCSPlayerController? player = @event.Userid;
            if (!Players.IsHumanViewer(player))
            {
                return HookResult.Continue;
            }

            LoadPlayerData(player);
            return HookResult.Continue;
        }

        public HookResult EventPlayerDisconnect(EventPlayerDisconnect @event, GameEventInfo info)
        {
            CCSPlayerController? player = @event.Userid;
            if (player == null || !PlayerStates.TryGetValue(player, out PlayerState? state))
            {
                return HookResult.Continue;
            }

            WritePlayerFile(player, state);
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
            PlayerLanguageManager.SetLanguage(new SteamID(player.NetworkIDString), new CultureInfo(language));
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

        public void CommandCss_c(CCSPlayerController? player, CounterStrikeSharp.API.Modules.Commands.CommandInfo info) =>
            ToggleMenu(player);

        public void CommandCss_challenges(CCSPlayerController? player, CounterStrikeSharp.API.Modules.Commands.CommandInfo info) =>
            ToggleMenu(player);

        public void CommandCss_topc(CCSPlayerController? player, CounterStrikeSharp.API.Modules.Commands.CommandInfo info)
        {
            if (!Players.IsHumanViewer(player))
            {
                return;
            }

            RunningSchedule? schedule = GetClass<Schedules>().Current;
            List<(string Name, int Current, int Total)> ranking = [];
            foreach ((CCSPlayerController human, PlayerState s) in PlayerStates)
            {
                if (!human.IsValid)
                {
                    continue;
                }

                int current = schedule != null
                    ? ChallengeProgress.CountSolvedInSchedule(s, schedule)
                    : 0;
                ranking.Add((human.PlayerName, current, s.Statistics.AmountChallengesSolved));
            }

            ranking.Sort(static (a, b) =>
            {
                int byTotal = b.Total.CompareTo(a.Total);
                return byTotal != 0 ? byTotal : b.Current.CompareTo(a.Current);
            });

            if (ranking.Count == 0)
            {
                player!.PrintToChat(Localizer["command.topc.nodata"]);
                return;
            }

            player!.PrintToChat(Localizer["command.topc"]);
            int limit = Math.Min(5, ranking.Count);
            for (int i = 0; i < limit; i++)
            {
                (string name, int current, int total) = ranking[i];
                player.PrintToChat($"{i + 1}. {name}: {current} / {total}");
            }
        }

        private void ToggleMenu(CCSPlayerController? player)
        {
            if (!Players.IsHumanViewer(player) || !GlobalConfig.Enabled)
            {
                return;
            }

            if (HudMenu.IsOpen(player))
            {
                HudMenu.Close(player);
                GetPlayerState(player).ActiveMenu = ActiveMenu.None;
                player.PrintToChat(Localizer["command.hidegui"]);
                return;
            }

            Menu.Open(player);
            player.PrintToChat(Localizer["command.showgui"]);
        }

        public override void Destroy()
        {
            foreach ((CCSPlayerController player, PlayerState state) in PlayerStates)
            {
                if (player.IsValid)
                {
                    WritePlayerFile(player, state);
                }
            }
            PlayerStates.Clear();
        }

        private void LoadPlayerData(CCSPlayerController player)
        {
            PlayerState state = GetPlayerState(player);
            string steamId = player.NetworkIDString;
            string path = PlayerFilePath(steamId);

            if (File.Exists(path))
            {
                try
                {
                    PlayerState? loaded = JsonSerializer.Deserialize<PlayerState>(File.ReadAllText(path), JsonOptions);
                    if (loaded != null)
                    {
                        state.Username = loaded.Username;
                        state.SteamId = loaded.SteamId;
                        state.ClanTag = loaded.ClanTag;
                        state.Language = loaded.Language;
                        state.Challenges = loaded.Challenges;
                        state.Statistics = loaded.Statistics;
                    }
                }
                catch (Exception ex)
                {
                    DebugPrint(ex.Message);
                }
            }

            state.Username = player.PlayerName;
            state.SteamId = steamId;
            state.ClanTag = player.Clan;
            if (!string.IsNullOrEmpty(state.Language))
            {
                try
                {
                    PlayerLanguageManager.SetLanguage(new SteamID(steamId), new CultureInfo(state.Language));
                }
                catch
                {
                }
            }
        }

        private void WritePlayerFile(CCSPlayerController player, PlayerState state)
        {
            string steamId = player.NetworkIDString;
            state.Username = player.PlayerName;
            state.SteamId = steamId;
            state.ClanTag = player.Clan;
            File.WriteAllText(PlayerFilePath(steamId), JsonSerializer.Serialize(state, JsonOptions));
        }

        private string PlayerFilePath(string steamId)
        {
            string safe = string.Concat(steamId.Split(Path.GetInvalidFileNameChars()));
            Directory.CreateDirectory(PlayersDir);
            return Path.Combine(PlayersDir, $"{safe}.json");
        }
    }
}

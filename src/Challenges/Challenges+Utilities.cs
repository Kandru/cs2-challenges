using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Translations;
using CounterStrikeSharp.API.Modules.Entities;

namespace Challenges
{
    public partial class Challenges : BasePlugin
    {
        private void DebugPrint(string message)
        {
            if (Config.Debug)
            {
                Console.WriteLine(Localizer["core.debugprint"].Value.Replace("{message}", message));
            }
        }

        private CCSGameRules? GetGameRules()
        {
            return Utilities.FindAllEntitiesByDesignerName<CCSGameRulesProxy>("cs_gamerules").First().GameRules;
        }

        private long GetUnixTimestamp(DateTime? currentTime = null)
        {
            if (currentTime == null)
                currentTime = DateTime.UtcNow;
            return ((DateTimeOffset)currentTime).ToUnixTimeSeconds();
        }

        private void LoadPlayerLanguage(string steamID)
        {
            if (!_playerConfigs.ContainsKey(steamID)
                || _playerConfigs[steamID].Language == "") return;
            playerLanguageManager.SetLanguage(
                new SteamID(steamID),
                new System.Globalization.CultureInfo(_playerConfigs[steamID].Language));
        }

        private void SavePlayerLanguage(string steamID, string language)
        {
            if (!_playerConfigs.ContainsKey(steamID)
                || language == null
                || language == "") return;
            // set language for player
            _playerConfigs[steamID].Language = language;
            playerLanguageManager.SetLanguage(new SteamID(steamID), new System.Globalization.CultureInfo(language));
        }

        private static string GetChallengeTitle(ChallengesBlueprint challenge, CCSPlayerController player)
        {
            return challenge.Title.TryGetValue(PlayerLanguageExtensions.GetLanguage(player).TwoLetterISOLanguageName, out var userTitle)
                ? userTitle
                : (challenge.Title.TryGetValue(CoreConfig.ServerLanguage, out var serverTitle)
                    ? serverTitle
                    : challenge.Title.First().Value);
        }

        private static string GetScheduleTitle(RunningChallengeSchedule challenge, CCSPlayerController player)
        {
            return challenge.Title.TryGetValue(PlayerLanguageExtensions.GetLanguage(player).TwoLetterISOLanguageName, out var userTitle)
                ? userTitle
                : (challenge.Title.TryGetValue(CoreConfig.ServerLanguage, out var serverTitle)
                    ? serverTitle
                    : challenge.Title.First().Value);
        }

        private bool CanChallengeBeCompleted(ChallengesBlueprint challenge, CCSPlayerController player)
        {
            if (!_playerConfigs.ContainsKey(player.NetworkIDString)) return false;
            if (challenge.Dependencies.Count > 0)
            {
                foreach (var dependency in challenge.Dependencies)
                {
                    if (!_playerConfigs[player.NetworkIDString].Challenges.ContainsKey(_currentSchedule.Key)
                        || !_playerConfigs[player.NetworkIDString].Challenges[_currentSchedule.Key].ContainsKey(dependency)
                        || _playerConfigs[player.NetworkIDString].Challenges[_currentSchedule.Key][dependency].Amount < _availableChallenges.Blueprints[dependency].Amount)
                        return false;
                }
            }
            return true;
        }

        private Dictionary<string, string> GetGlobalEventData()
        {
            return new Dictionary<string, string>
            {
                { "global.iswarmup", GetGameRules()?.WarmupPeriod.ToString() ?? "false" },
                { "global.isduringround", _isDuringRound.ToString() }
            };
        }

        private static Dictionary<string, string> GetCCSPlayerControllerProperties(CCSPlayerController? player, string prefix)
        {
            if (player == null || !player.IsValid) return new Dictionary<string, string>();
            return new Dictionary<string, string>{
                { $"{prefix}.name", player.PlayerName },
                { $"{prefix}.isbot", player.IsBot.ToString() },
                { $"{prefix}.team", player.Team.ToString() },
                { $"{prefix}.alive", player.PawnIsAlive.ToString() },
                { $"{prefix}.ping", player.Ping.ToString() },
                { $"{prefix}.money", player.InGameMoneyServices?.Account.ToString() ?? "0" },
                { $"{prefix}.score", player.Score.ToString() },
                { $"{prefix}.stats.kills", player.ActionTrackingServices?.MatchStats?.Kills.ToString() ?? "0" },
                { $"{prefix}.stats.assists", player.ActionTrackingServices?.MatchStats?.Assists.ToString() ?? "0" },
                { $"{prefix}.stats.deaths", player.ActionTrackingServices?.MatchStats?.Deaths.ToString() ?? "0" },
                { $"{prefix}.stats.damage", player.ActionTrackingServices?.MatchStats?.Damage.ToString() ?? "0" },
                { $"{prefix}.health", player.PawnHealth.ToString() },
                { $"{prefix}.armor", player.PawnArmor.ToString() },
                { $"{prefix}.hasdefusor", player.PawnHasDefuser.ToString() },
                { $"{prefix}.hashelmet", player.PawnHasHelmet.ToString() }
            };
        }
    }
}
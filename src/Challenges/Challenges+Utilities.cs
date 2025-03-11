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
            if (challenge.Rules.Count > 0)
            {
                // check if map is not correct
                foreach (var kvp in challenge.Rules)
                {
                    if (kvp.Key == "global.mapname")
                    {
                        switch (kvp.Operator)
                        {
                            case "==":
                                if (kvp.Value.ToLower() != Server.MapName.ToLower()) return false;
                                break;
                            case "!=":
                                if (kvp.Value.ToLower() == Server.MapName.ToLower()) return false;
                                break;
                        }
                    }
                }
            }
            if (challenge.Dependencies.Count > 0)
            {
                // check if dependencies are not met
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
    }
}
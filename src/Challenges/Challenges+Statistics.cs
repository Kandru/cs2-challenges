using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Extensions;
using System.Text.Json;

namespace Challenges
{
    public partial class Challenges : BasePlugin
    {
        private List<PlayerConfig> _playersWithMostChallengesSolved = [];

        private List<PlayerConfig>? GetAllPlayersFromConfig()
        {
            string playerConfigPath = $"{Path.GetDirectoryName(Config.GetConfigPath())}/players/" ?? "./players/";
            if (!Directory.Exists(playerConfigPath)) return null;
            List<PlayerConfig> playerConfigs = [];
            // iterate through all configuration files
            foreach (string file in Directory.EnumerateFiles(playerConfigPath, "*.json"))
            {
                try
                {
                    string json = File.ReadAllText(file);
                    PlayerConfig? playerConfig = JsonSerializer.Deserialize<PlayerConfig>(json);
                    if (playerConfig != null)
                        playerConfigs.Add(playerConfig);
                }
                catch (Exception e)
                {
                    Console.WriteLine(Localizer["core.faultyconfig"].Value.Replace("{config}", playerConfigPath).Replace("{error}", e.Message));
                }
            }
            return playerConfigs;
        }

        public List<PlayerConfig> GetPlayersWithMostChallengesSolved(List<PlayerConfig> players)
        {
            // sort players by amount of challenges solved
            var playersWithChallenges = players
                .Select(player => new
                {
                    Player = player,
                    CompletedChallenges = player.Challenges.Sum(
                        category => category.Value.Count(
                            challenge => _currentSchedule.Challenges.ContainsKey(challenge.Key)
                                && challenge.Value.Amount >= _currentSchedule.Challenges[challenge.Key].Amount))
                })
                .OrderByDescending(player => player.CompletedChallenges)
                .ToList();
            // add amount of challenges solved to player statistics
            foreach (var playerWithChallenges in playersWithChallenges)
            {
                playerWithChallenges.Player.Statistics.AmountChallengesSolved = playerWithChallenges.CompletedChallenges;
            }
            return [.. playersWithChallenges.Select(player => player.Player)];
        }

        private void CalculatePlayersWithMostChallengesSolved()
        {
            List<PlayerConfig>? players = GetAllPlayersFromConfig();
            if (players == null) return;
            _playersWithMostChallengesSolved = GetPlayersWithMostChallengesSolved(players);
        }
    }
}
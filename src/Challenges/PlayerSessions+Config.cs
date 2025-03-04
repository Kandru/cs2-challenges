using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Extensions;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Challenges
{
    public class PluginConfigGUI
    {
        [JsonPropertyName("show_on_round_start")] public bool ShowOnRoundStart { get; set; } = true;
        [JsonPropertyName("on_round_start_duration")] public int OnRoundStartDuration { get; set; } = 3; // buy time + this value
        [JsonPropertyName("show_after_respawn")] public bool ShowAfterRespawn { get; set; } = true;
        [JsonPropertyName("after_respawn_duration")] public int AfterRespawnDuration { get; set; } = 5;
        [JsonPropertyName("show_on_challenge_update")] public bool ShowOnChallengeUpdate { get; set; } = true;
        [JsonPropertyName("on_challenge_update_duration")] public float OnChallengeUpdateDuration { get; set; } = 5f;
        [JsonPropertyName("menu_display_maximum")] public int DisplayMaximum { get; set; } = 4;
        [JsonPropertyName("menu_font_size")] public int FontSize { get; set; } = 28;
        [JsonPropertyName("menu_font_name")] public string FontName { get; set; } = "Arial Black Standard";
        [JsonPropertyName("menu_font_color")] public string FontColor { get; set; } = "#ffffff";
        [JsonPropertyName("menu_pos_x")] public float PositionX { get; set; } = 3.6f; // for 16:9 & 16:10
        [JsonPropertyName("menu_pos_y")] public float PositionY { get; set; } = 4f; // for 16:9 & 16:10
        [JsonPropertyName("menu_background")] public bool Background { get; set; } = true;
        [JsonPropertyName("menu_backgroundfactor")] public float BackgroundFactor { get; set; } = 1f;
    }

    public class PluginConfigNotifications
    {
        [JsonPropertyName("notify_player_on_challenge_progress")] public bool NotifyPlayerOnChallengeProgress { get; set; } = true;
        [JsonPropertyName("notify_player_on_challenge_complete")] public bool NotifyPlayerOnChallengeComplete { get; set; } = true;
        [JsonPropertyName("notify_other_on_challenge_complete")] public bool NotifyOtherOnChallengeComplete { get; set; } = true;
    }

    public class PluginConfig : BasePluginConfig
    {
        // disabled
        [JsonPropertyName("enabled")] public bool Enabled { get; set; } = true;
        // debug prints
        [JsonPropertyName("debug")] public bool Debug { get; set; } = false;
        // challenges
        [JsonPropertyName("gui")] public PluginConfigGUI GUI { get; set; } = new();
        // notifications
        [JsonPropertyName("notifications")] public PluginConfigNotifications Notifications { get; set; } = new();
    }

    public partial class Challenges : BasePlugin, IPluginConfig<PluginConfig>
    {
        public required PluginConfig Config { get; set; }
        private Dictionary<string, PlayerConfig> _playerConfigs = [];
        private ChallengesConfig _playerChallenges = new();

        private PlayerConfig LoadPlayerConfig(string steamId)
        {
            // check if player config does not exist
            if (!_playerConfigs.ContainsKey(steamId))
            {
                string safeSteamId = string.Concat(steamId.Split(Path.GetInvalidFileNameChars()));
                string playerConfigPath = Path.Combine(
                    $"{Path.GetDirectoryName(Config.GetConfigPath())}/players/" ?? "./players/", $"{safeSteamId}.json"
                );
                // check if player config exists
                if (!Path.Exists(playerConfigPath))
                {
                    // create new player config
                    _playerConfigs.Add(steamId, new PlayerConfig());
                }
                else
                {
                    // try to load player config
                    try
                    {
                        var jsonString = File.ReadAllText(playerConfigPath);
                        var playerConfig = JsonSerializer.Deserialize<PlayerConfig>(jsonString);
                        if (playerConfig != null)
                        {
                            _playerConfigs.Add(steamId, playerConfig);
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(Localizer["core.faultyconfig"].Value.Replace("{config}", playerConfigPath).Replace("{error}", e.Message));
                        // save backup of faulty config to prevent data loss
                        if (Path.Exists(playerConfigPath))
                        {
                            File.Copy(playerConfigPath, playerConfigPath + ".bak", true);
                        }
                        // create new player config
                        _playerConfigs.Add(steamId, new PlayerConfig());
                    }
                }
            }
            return _playerConfigs[steamId];
        }

        private void LoadActivePlayerConfigs()
        {
            foreach (CCSPlayerController entry in Utilities.GetPlayers())
            {
                if (entry == null
                    || !entry.IsValid
                    || entry.IsBot
                    || _playerConfigs.ContainsKey(entry.NetworkIDString)) return;
                LoadPlayerConfig(entry.NetworkIDString);
            }
        }

        private void SavePlayerConfig(string steamId)
        {
            if (!_playerConfigs.ContainsKey(steamId)) return;
            string safeSteamId = string.Concat(steamId.Split(Path.GetInvalidFileNameChars()));
            string playerConfigPath = Path.Combine(
                    $"{Path.GetDirectoryName(Config.GetConfigPath())}/players/" ?? "./players/", $"{safeSteamId}.json"
                );
            // check if folder exists and create otherwise
            if (!Path.Exists(Path.GetDirectoryName(playerConfigPath)))
            {
                var directoryPath = Path.GetDirectoryName(playerConfigPath);
                if (directoryPath != null)
                {
                    Directory.CreateDirectory(directoryPath);
                }
            }
            DebugPrint($"Saving player config for {steamId} to {playerConfigPath}");
            var jsonString = JsonSerializer.Serialize(_playerConfigs[steamId], new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(playerConfigPath, jsonString);
        }

        private void SavePlayerConfigs()
        {
            foreach (var kvp in _playerConfigs)
            {
                SavePlayerConfig(kvp.Key);
            }
        }

        private void UnloadPlayerConfig(string steamId)
        {
            if (!_playerConfigs.ContainsKey(steamId)) return;
            SavePlayerConfig(steamId);
            _playerConfigs.Remove(steamId);
        }

        private void LoadChallenges()
        {
            string challengesPath = Path.Combine(Path.GetDirectoryName(Config.GetConfigPath()) ?? "./", "challenges.json");
            DebugPrint($"Loading challenges");
            if (Path.Exists(challengesPath))
            {
                try
                {
                    var jsonString = File.ReadAllText(challengesPath);
                    _playerChallenges = JsonSerializer.Deserialize<ChallengesConfig>(jsonString) ?? new();
                }
                catch
                {
                    Console.WriteLine(Localizer["core.faultyconfig"].Value.Replace("{config}", challengesPath));
                }
            }
            else
            {
                SaveChallenges();
            }
        }

        private void SaveChallenges()
        {
            string challengesPath = Path.Combine(Path.GetDirectoryName(Config.GetConfigPath()) ?? "./", "challenges.json");
            DebugPrint($"Saving challenges");
            var jsonString = JsonSerializer.Serialize(_playerChallenges, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(challengesPath, jsonString);
        }

        public void OnConfigParsed(PluginConfig config)
        {
            Config = config;
            // update config and write new values from plugin to config file if changed after update
            Config.Update();
            Console.WriteLine(Localizer["core.config"]);
        }
    }
}

using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Extensions;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

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
        [JsonPropertyName("notification_sound_on_challenge_progress")] public string ChallengeProgressSound { get; set; } = "";
        [JsonPropertyName("notification_sound_on_challenge_complete")] public string ChallengeCompleteSound { get; set; } = "sounds/ui/xp_levelup.vsnd";
        [JsonPropertyName("notification_sound_on_action_rule_broken")] public string ChallengeRuleBrokenSound { get; set; } = "sounds/ui/xp_rankdown_02.vsnd";
    }

    public class PluginConfigDiscord
    {
        [JsonPropertyName("language")] public string Language { get; set; } = "en";
        [JsonPropertyName("webhook_on_challenge_completed")] public string WebhookChallengeCompleted { get; set; } = "";
        [JsonPropertyName("webhook_on_new_schedule")] public string WebhookNewSchedule { get; set; } = "";
    }

    public class PluginConfig : BasePluginConfig
    {
        // disabled
        [JsonPropertyName("enabled")] public bool Enabled { get; set; } = true;
        // debug prints
        [JsonPropertyName("debug")] public bool Debug { get; set; } = false;
        // gui
        [JsonPropertyName("gui")] public PluginConfigGUI GUI { get; set; } = new();
        // notifications
        [JsonPropertyName("notifications")] public PluginConfigNotifications Notifications { get; set; } = new();
        // discord notifications
        [JsonPropertyName("discord")] public PluginConfigDiscord Discord { get; set; } = new();
        // temporary data
        [JsonPropertyName("temp_data")] public TempDataConfig TempData { get; set; } = new();
    }

    public partial class Challenges : BasePlugin, IPluginConfig<PluginConfig>
    {
        public required PluginConfig Config { get; set; }
        private Dictionary<string, PlayerConfig> _playerConfigs = [];
        private ChallengesConfig _availableChallenges = new();

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
                        // set language if available
                        LoadPlayerLanguage(steamId);
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
                    || _playerConfigs.ContainsKey(entry.NetworkIDString)) continue;
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
            DebugPrint($"Loading challenges");
            string blueprintsPath = Path.Combine(Path.GetDirectoryName(Config.GetConfigPath()) ?? "./", "blueprints/");
            string schedulesPath = Path.Combine(Path.GetDirectoryName(Config.GetConfigPath()) ?? "./", "schedules.yaml");
            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(UnderscoredNamingConvention.Instance)
                .Build();
            // create new and empty challenges config
            _availableChallenges = new();
            // load all blueprints
            if (Path.Exists(blueprintsPath))
            {
                foreach (string file in Directory.GetFiles(blueprintsPath, "*.yaml"))
                {
                    try
                    {
                        using var reader = new StreamReader(file);
                        var challenges = deserializer.Deserialize<Dictionary<string, ChallengesBlueprint>>(reader);
                        if (challenges != null)
                        {
                            foreach (var kvp in challenges)
                            {
                                // check if title contains at least one entry
                                if (kvp.Value.Title.Count == 0)
                                {
                                    Console.WriteLine(Localizer["core.faultyconfig"].Value
                                        .Replace("{config}", file)
                                        .Replace("{error}", $"title of challenge {kvp.Key} is missing"));
                                    continue;
                                }
                                // check if data contains at least one entry
                                if (kvp.Value.Data.Count == 0 && kvp.Value.Actions.Count == 0)
                                {
                                    Console.WriteLine(Localizer["core.faultyconfig"].Value
                                        .Replace("{config}", file)
                                        .Replace("{error}", $"data & actions of challenge {kvp.Key} are missing"));
                                    continue;
                                }
                                // update key of dependencies to match the filename prefix of given blueprint
                                for (int i = 0; i < kvp.Value.Dependencies.Count; i++)
                                {
                                    // check if dependency already contains filename prefix
                                    if (kvp.Value.Dependencies[i].Contains(':')) continue;
                                    kvp.Value.Dependencies[i] = $"{Path.GetFileNameWithoutExtension(file).ToLower()}:{kvp.Value.Dependencies[i]}";
                                }
                                // update key of actions to match the filename prefix of given blueprint
                                foreach (var action in kvp.Value.Actions)
                                {
                                    switch (action.Type)
                                    {
                                        case "challenge.delete.progress" when action.Values.Count >= 1:
                                            if (action.Values[0].Contains(':')) break;
                                            action.Values[0] = $"{Path.GetFileNameWithoutExtension(file).ToLower()}:{action.Values[0]}";
                                            break;
                                        case "challenge.delete.completed" when action.Values.Count >= 1:
                                            if (action.Values[0].Contains(':')) break;
                                            action.Values[0] = $"{Path.GetFileNameWithoutExtension(file).ToLower()}:{action.Values[0]}";
                                            break;
                                        case "challenge.mark.completed" when action.Values.Count >= 1:
                                            if (action.Values[0].Contains(':')) break;
                                            action.Values[0] = $"{Path.GetFileNameWithoutExtension(file).ToLower()}:{action.Values[0]}";
                                            break;
                                        case "notify.player.progress.rule_broken":
                                            for (int i = 0; i < action.Values.Count; i++)
                                            {
                                                if (action.Values[i].Contains(':')) continue;
                                                action.Values[i] = $"{Path.GetFileNameWithoutExtension(file).ToLower()}:{action.Values[i]}";
                                            }
                                            break;
                                        case "notify.player.completed.rule_broken":
                                            for (int i = 0; i < action.Values.Count; i++)
                                            {
                                                if (action.Values[i].Contains(':')) continue;
                                                action.Values[i] = $"{Path.GetFileNameWithoutExtension(file).ToLower()}:{action.Values[i]}";
                                            }
                                            break;
                                    }
                                }
                                // add filename prefix to blueprint
                                kvp.Value.Key = $"{Path.GetFileNameWithoutExtension(file).ToLower()}:{kvp.Key}";
                                // add blueprint to available challenges
                                _availableChallenges.Blueprints.Add(
                                    $"{Path.GetFileNameWithoutExtension(file).ToLower()}:{kvp.Key}",
                                    kvp.Value
                                );
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(Localizer["core.faultyconfig"].Value
                            .Replace("{config}", file)
                            .Replace("{error}", e.Message));
                    }
                }
                // check if blueprints dependencies are valid (and remove blueprint with invalid dependencies)
                foreach (var kvp in _availableChallenges.Blueprints)
                {
                    for (int i = 0; i < kvp.Value.Dependencies.Count; i++)
                    {
                        if (!_availableChallenges.Blueprints.ContainsKey(kvp.Value.Dependencies[i]))
                        {
                            Console.WriteLine(Localizer["core.faultyconfig"].Value
                                .Replace("{config}", kvp.Value.Key)
                                .Replace("{error}", $"dependency {kvp.Value.Dependencies[i]} is missing. Removing blueprint."));
                            _availableChallenges.Blueprints.Remove(kvp.Key);
                            break;
                        }
                    }
                }
            }
            else
            {
                // create folder
                Directory.CreateDirectory(blueprintsPath);
            }
            // load all schedules
            if (Path.Exists(schedulesPath))
            {
                try
                {
                    using var reader = new StreamReader(schedulesPath);
                    var schedules = deserializer.Deserialize<Dictionary<string, ChallengesSchedule>>(reader);
                    if (schedules != null)
                    {
                        _availableChallenges.Schedules = schedules;
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(Localizer["core.faultyconfig"].Value.Replace("{config}", schedulesPath).Replace("{error}", e.Message));
                }
            }
            else
            {
                DebugPrint($"Creating empty schedules yaml");
                var serializer = new SerializerBuilder()
                    .WithNamingConvention(CamelCaseNamingConvention.Instance)
                    .Build();
                var yamlString = serializer.Serialize(_availableChallenges.Schedules);
                File.WriteAllText(schedulesPath, yamlString);
            }
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

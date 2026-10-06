using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Capabilities;
using CounterStrikeSharp.API.Core.Translations;
using CounterStrikeSharp.API.Modules.Extensions;
using Challenges.Classes;
using Challenges.Configs;
using Challenges.Enums;
using Challenges.Utils;
using ChallengesShared;
using System.Text.Json;

namespace Challenges
{
    public partial class Challenges : BasePlugin, IPluginConfig<PluginConfig>
    {
        public override string ModuleName => "Challenges";
        public override string ModuleAuthor => "Kalle <kalle@kandru.de>";

        private static readonly PluginCapability<IChallengesEventSender> ChallengesEvents = new("challenges:events");
        private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

        private readonly PlayerLanguageManager _playerLanguageManager = new();
        private readonly Dictionary<GlobalStates, object> _globalStates = new()
        {
            { GlobalStates.ClassInstances, new Dictionary<string, ClassesBlueprint>() },
            { GlobalStates.GlobalConfig, new PluginConfig() },
            { GlobalStates.PlayerStates, new Dictionary<CCSPlayerController, PlayerState>() },
            { GlobalStates.Challenges, new Dictionary<string, ChallengeDefinition>(StringComparer.OrdinalIgnoreCase) },
            { GlobalStates.Schedules, new Dictionary<string, ChallengeSchedule>(StringComparer.OrdinalIgnoreCase) },
            { GlobalStates.DuringRound, false },
            { GlobalStates.FreezeActive, false },
        };

        public required PluginConfig Config { get; set; }

        private Dictionary<string, ClassesBlueprint> ClassInstances =>
            (Dictionary<string, ClassesBlueprint>)_globalStates[GlobalStates.ClassInstances];

        public override void Load(bool hotReload)
        {
            _globalStates[GlobalStates.PlayerLanguageManager] = _playerLanguageManager;
            RegisterListener<Listeners.OnMapStart>(OnMapStart);
            RegisterListener<Listeners.OnMapEnd>(OnMapEnd);
            RegisterListener<Listeners.CheckTransmit>(CustomHud.OnCheckTransmit);
            RegisterListener<Listeners.OnTick>(CustomHud.OnTick);

            var sender = new CustomEventsSender();
            CustomEventsSender.Instance = sender;
            Capabilities.RegisterPluginCapability(ChallengesEvents, () => sender);

            if (hotReload)
            {
                LoadChallengeFiles();
                LoadSchedules();
            }

            InitializeClasses(hotReload);
        }

        public override void Unload(bool hotReload)
        {
            DestroyClasses();
            RemoveListener<Listeners.OnMapStart>(OnMapStart);
            RemoveListener<Listeners.OnMapEnd>(OnMapEnd);
            RemoveListener<Listeners.CheckTransmit>(CustomHud.OnCheckTransmit);
            RemoveListener<Listeners.OnTick>(CustomHud.OnTick);
            CustomEventsSender.Instance = null;
        }

        public void OnConfigParsed(PluginConfig config)
        {
            Config = config;
            Config.Update();
            _globalStates[GlobalStates.GlobalConfig] = config;
            Console.WriteLine(Localizer["core.config"]);
            LogCatalogMismatches();
            LoadChallengeFiles();
            LoadSchedules();
        }

        private void OnMapStart(string mapName)
        {
            Reset();
            LoadChallengeFiles();
            LoadSchedules();
            InitializeClasses();
        }

        private void OnMapEnd()
        {
            Reset();
        }

        private void Reset()
        {
            DestroyClasses();
            _globalStates[GlobalStates.DuringRound] = false;
            _globalStates[GlobalStates.FreezeActive] = false;
            ((Dictionary<CCSPlayerController, PlayerState>)_globalStates[GlobalStates.PlayerStates]).Clear();
        }

        private void InitializeClasses(bool isHotReloaded = false)
        {
            if (ClassInstances.Count > 0)
            {
                return;
            }

            ClassInstances.Add(nameof(PlayerManagement), new PlayerManagement(_globalStates, Localizer, isHotReloaded));
            ClassInstances.Add(nameof(Schedules), new Schedules(_globalStates, Localizer, isHotReloaded));
            ClassInstances.Add(nameof(ChallengeEngine), new ChallengeEngine(_globalStates, Localizer, isHotReloaded));
            ClassInstances.Add(nameof(Notifications), new Notifications(_globalStates, Localizer, isHotReloaded));
            ClassInstances.Add(nameof(HudDriver), new HudDriver(_globalStates, Localizer, isHotReloaded));
            BindModuleHandlers(register: true);
        }

        private void DestroyClasses()
        {
            BindModuleHandlers(register: false);
            foreach (ClassesBlueprint entry in ClassInstances.Values)
            {
                entry.Destroy();
            }
            ClassInstances.Clear();
            CustomHud.Shutdown();
        }

        private void BindModuleHandlers(bool register)
        {
            foreach (ClassesBlueprint entry in ClassInstances.Values)
            {
                foreach (string listenerName in entry.Listeners)
                {
                    DynamicHandlers.BindModuleListener(this, listenerName, entry, register);
                }

                foreach (string eventName in entry.Events)
                {
                    DynamicHandlers.BindModuleEventHandler(this, eventName, entry, register);
                }

                foreach ((int userMessageId, HookMode hookMode) in entry.UserMessages)
                {
                    DynamicHandlers.BindUserMessageHook(this, userMessageId, entry, hookMode, register);
                }

                foreach ((string messageName, HookMode hookMode) in entry.UserMessageNames)
                {
                    DynamicHandlers.BindNamedUserMessageHook(this, messageName, entry, hookMode, register);
                }

                foreach ((string command, string description) in entry.Commands)
                {
                    DynamicHandlers.BindCommand(this, command, description, entry, register);
                }

                foreach ((string command, HookMode hookMode) in entry.CommandListeners)
                {
                    DynamicHandlers.BindCommandListener(this, command, entry, hookMode, register);
                }
            }
        }

        public string GetConfigDir() =>
            Path.GetDirectoryName(Config.GetConfigPath()) ?? ".";

        public List<string> CheckCatalog()
        {
            string path = Path.Combine(ModuleDirectory, "catalog.json");
            if (!File.Exists(path))
            {
                path = Path.Combine(GetConfigDir(), "catalog.json");
            }

            if (!File.Exists(path))
            {
                return [Localizer["core.faultyconfig"].Value
                    .Replace("{config}", "catalog.json")
                    .Replace("{error}", "file not found")];
            }

            Catalog catalog;
            try
            {
                catalog = JsonSerializer.Deserialize<Catalog>(File.ReadAllText(path), JsonOptions) ?? new();
            }
            catch (Exception ex)
            {
                return [Localizer["core.faultyconfig"].Value
                    .Replace("{config}", path)
                    .Replace("{error}", ex.Message)];
            }

            return Catalog.Mismatches(catalog, Localizer);
        }

        private void LogCatalogMismatches()
        {
            foreach (string message in CheckCatalog())
            {
                Console.WriteLine(message);
            }
        }

        private void LoadChallengeFiles()
        {
            string dir = Path.Combine(GetConfigDir(), "blueprints");
            Directory.CreateDirectory(dir);
            var map = new Dictionary<string, ChallengeDefinition>(StringComparer.OrdinalIgnoreCase);
            var deserializer = new YamlDotNet.Serialization.DeserializerBuilder()
                .IgnoreUnmatchedProperties()
                .Build();

            foreach (string file in Directory.GetFiles(dir, "*.yaml"))
            {
                if (Path.GetFileName(file).Equals("schedules.yaml", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                try
                {
                    string id = Path.GetFileNameWithoutExtension(file);
                    ChallengeDefinition def = deserializer.Deserialize<ChallengeDefinition>(File.ReadAllText(file))
                        ?? new ChallengeDefinition();
                    def.Id = id;
                    if (def.Title.Count == 0)
                    {
                        Console.WriteLine(Localizer["core.faultyconfig"].Value
                            .Replace("{config}", file)
                            .Replace("{error}", "title is missing"));
                        continue;
                    }

                    if (def.Tasks.Count == 0)
                    {
                        Console.WriteLine(Localizer["core.faultyconfig"].Value
                            .Replace("{config}", file)
                            .Replace("{error}", "tasks list is empty"));
                        continue;
                    }

                    NormalizeDefinition(def);
                    map[id] = def;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(Localizer["core.faultyconfig"].Value
                        .Replace("{config}", file)
                        .Replace("{error}", ex.Message));
                }
            }

            _globalStates[GlobalStates.Challenges] = map;
            Console.WriteLine(Localizer["core.config"].Value.Replace("{config}", $"blueprints ({map.Count})"));
        }

        private static void NormalizeDefinition(ChallengeDefinition def)
        {
            foreach (ChallengeTask task in def.Tasks)
            {
                task.Id = task.Id.Trim();
                task.Type = task.Type.Trim().ToLowerInvariant();
                if (task.Amount < 1)
                {
                    task.Amount = 1;
                }

                foreach (ChallengeRule rule in task.Rules)
                {
                    rule.Key = rule.Key.Trim().ToLowerInvariant();
                    rule.Operator = rule.Operator.Trim();
                }

                foreach (ChallengeAction action in task.Actions)
                {
                    action.Type = action.Type.Trim().ToLowerInvariant();
                }

                for (int i = 0; i < task.Requires.Count; i++)
                {
                    task.Requires[i] = task.Requires[i].Trim();
                }
            }

            def.BuildTaskIndex();
        }

        private void LoadSchedules()
        {
            string path = Path.Combine(GetConfigDir(), "schedules.yaml");
            var schedules = new Dictionary<string, ChallengeSchedule>(StringComparer.OrdinalIgnoreCase);
            if (File.Exists(path))
            {
                try
                {
                    var deserializer = new YamlDotNet.Serialization.DeserializerBuilder()
                        .IgnoreUnmatchedProperties()
                        .Build();
                    schedules = deserializer.Deserialize<Dictionary<string, ChallengeSchedule>>(File.ReadAllText(path))
                        ?? new(StringComparer.OrdinalIgnoreCase);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(Localizer["core.faultyconfig"].Value
                        .Replace("{config}", path)
                        .Replace("{error}", ex.Message));
                }
            }
            else
            {
                File.WriteAllText(path, "{}\n");
            }

            _globalStates[GlobalStates.Schedules] = schedules;
        }
    }
}

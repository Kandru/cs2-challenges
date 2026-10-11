using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Capabilities;
using CounterStrikeSharp.API.Core.Translations;
using CounterStrikeSharp.API.Modules.Extensions;
using ChallengesShared;
using ChallengesShared.Events;
using System.Text.Json;
using YamlDotNet.Serialization;

namespace Challenges
{
    public partial class Challenges : BasePlugin, IPluginConfig<PluginConfig>
    {
        public override string ModuleName => "Challenges";
        public override string ModuleAuthor => "Kalle <kalle@kandru.de>";

        private static readonly PluginCapability<IChallengesEventSender> ChallengesEvents = new("challenges:events");
        private static readonly IDeserializer Yaml = new DeserializerBuilder()
            .IgnoreUnmatchedProperties()
            .Build();

        private readonly PlayerLanguageManager _playerLanguageManager = new();
        private readonly State _state = new();

        public required PluginConfig Config { get; set; }

        private Dictionary<string, Module> Modules => _state.Modules;

        public override void Load(bool hotReload)
        {
            _state.PlayerLanguageManager = _playerLanguageManager;
            RegisterListener<Listeners.OnMapStart>(OnMapStart);
            RegisterListener<Listeners.OnMapEnd>(OnMapEnd);
            RegisterListener<Listeners.CheckTransmit>(CustomHud.OnCheckTransmit);
            CustomHud.CacheTickRate();
            RegisterListener<Listeners.OnTick>(CustomHud.OnTick);

            var sender = new CustomEventsSender();
            CustomEventsSender.Instance = sender;
            Capabilities.RegisterPluginCapability(ChallengesEvents, () => sender);

            InitializeModules(hotReload);
        }

        public override void Unload(bool hotReload)
        {
            DestroyModules();
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
            _state.Config = config;
            LogCatalogMismatches();
            (int challenges, int schedules) = LoadSchedulesAndChallenges();
            Console.WriteLine(Localizer["core.config"].Value
                .Replace("{challenges}", challenges.ToString())
                .Replace("{schedules}", schedules.ToString()));
        }

        private void OnMapStart(string mapName)
        {
            Reset();
            LoadSchedulesAndChallenges();
            InitializeModules();
        }

        private void OnMapEnd()
        {
            Reset();
        }

        private void Reset()
        {
            DestroyModules();
            _state.DuringRound = false;
            _state.FreezeActive = false;
        }

        private void InitializeModules(bool isHotReloaded = false)
        {
            if (Modules.Count > 0)
            {
                return;
            }

            Modules.Add(nameof(PlayerManagement), new PlayerManagement(_state, Localizer, isHotReloaded));
            Modules.Add(nameof(Schedules), new Schedules(_state, Localizer, isHotReloaded));
            Modules.Add(nameof(ChallengeEngine), new ChallengeEngine(_state, Localizer, isHotReloaded));
            Modules.Add(nameof(Notifications), new Notifications(_state, Localizer, isHotReloaded));
            Modules.Add(nameof(HudDriver), new HudDriver(_state, Localizer, isHotReloaded));
            BindModuleHandlers(register: true);
        }

        private void DestroyModules()
        {
            BindModuleHandlers(register: false);
            if (Modules.TryGetValue(nameof(ChallengeEngine), out Module? engine)
                && engine is ChallengeEngine challengeEngine)
            {
                challengeEngine.Flush();
            }

            foreach (Module entry in Modules.Values)
            {
                entry.Destroy();
            }
            Modules.Clear();
            CustomHud.Shutdown();
            _state.PlayerStates.Clear();
        }

        private void BindModuleHandlers(bool register)
        {
            foreach (Module entry in Modules.Values)
            {
                foreach (string listenerName in entry.Listeners)
                {
                    Bindings.BindModuleListener(this, listenerName, entry, register);
                }

                foreach (string eventName in entry.Events)
                {
                    Bindings.BindModuleEventHandler(this, eventName, entry, register);
                }

                foreach ((string command, CommandBinding binding) in entry.Commands)
                {
                    Bindings.BindCommand(this, command, binding.Description, binding.Method, entry, register);
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
                catalog = JsonSerializer.Deserialize<Catalog>(File.ReadAllText(path), PlayerFiles.JsonOptions) ?? new();
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

        /// <summary>Re-reads schedules.yaml, then blueprints for current or upcoming schedules only.</summary>
        private (int Challenges, int Schedules) LoadSchedulesAndChallenges()
        {
            Dictionary<string, ChallengeSchedule> schedules = ReadSchedules();
            _state.Schedules = schedules;
            _state.Challenges = ReadBlueprints(ChallengeIdsForLoad(schedules));
            return (_state.Challenges.Count, schedules.Count);
        }

        /// <summary>Ids from schedules that have not ended yet (current or upcoming).</summary>
        private static HashSet<string> ChallengeIdsForLoad(
            IReadOnlyDictionary<string, ChallengeSchedule> schedules)
        {
            HashSet<string> ids = new(StringComparer.OrdinalIgnoreCase);
            DateTime now = DateTime.UtcNow;
            foreach (ChallengeSchedule schedule in schedules.Values)
            {
                if (!Schedules.TryParseDate(schedule.StartDate, out _)
                    || !Schedules.TryParseDate(schedule.EndDate, out DateTime end)
                    || end < now)
                {
                    continue;
                }

                foreach (string rawId in schedule.Challenges)
                {
                    string id = Schedules.ChallengeId(rawId);
                    if (id.Length > 0)
                    {
                        ids.Add(id);
                    }
                }
            }

            return ids;
        }

        private Dictionary<string, ChallengeDefinition> ReadBlueprints(HashSet<string> neededIds)
        {
            string dir = Path.Combine(GetConfigDir(), "blueprints");
            Directory.CreateDirectory(dir);
            var map = new Dictionary<string, ChallengeDefinition>(StringComparer.OrdinalIgnoreCase);
            if (neededIds.Count == 0)
            {
                return map;
            }

            Dictionary<string, string> filesById = new(StringComparer.OrdinalIgnoreCase);
            foreach (string path in Directory.EnumerateFiles(dir, "*.yaml"))
            {
                filesById.TryAdd(Path.GetFileNameWithoutExtension(path), path);
            }

            foreach (string id in neededIds)
            {
                if (!filesById.TryGetValue(id, out string? file))
                {
                    continue;
                }

                try
                {
                    ChallengeDefinition def = Yaml.Deserialize<ChallengeDefinition>(File.ReadAllText(file))
                        ?? new ChallengeDefinition();
                    def.Id = id;
                    if (def.Title.Count == 0)
                    {
                        LogBadConfig(file, "title is missing");
                        continue;
                    }

                    if (def.Tasks.Count == 0)
                    {
                        LogBadConfig(file, "tasks list is empty");
                        continue;
                    }

                    NormalizeDefinition(def);
                    map[id] = def;
                }
                catch (Exception ex)
                {
                    LogBadConfig(file, ex.Message);
                }
            }

            return map;
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

        private Dictionary<string, ChallengeSchedule> ReadSchedules()
        {
            string path = Path.Combine(GetConfigDir(), "schedules.yaml");
            if (!File.Exists(path))
            {
                File.WriteAllText(path, "{}\n");
                return new(StringComparer.OrdinalIgnoreCase);
            }

            try
            {
                return Yaml.Deserialize<Dictionary<string, ChallengeSchedule>>(File.ReadAllText(path))
                    ?? new(StringComparer.OrdinalIgnoreCase);
            }
            catch (Exception ex)
            {
                LogBadConfig(path, ex.Message);
                return new(StringComparer.OrdinalIgnoreCase);
            }
        }

        private void LogBadConfig(string config, string error) =>
            Console.WriteLine(Localizer["core.faultyconfig"].Value
                .Replace("{config}", config)
                .Replace("{error}", error));
    }
}

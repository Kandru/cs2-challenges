using ChallengesShared;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Capabilities;
using CounterStrikeSharp.API.Modules.Extensions;

namespace Challenges
{
    public partial class Challenges : BasePlugin
    {
        public override string ModuleName => "Challenges";
        public override string ModuleAuthor => "Kalle <kalle@kandru.de>";

        private static PluginCapability<IChallengesEventSender> ChallengesEvents { get; } = new("challenges:events");
        private readonly Random _random = new Random(Guid.NewGuid().GetHashCode());
        private bool _isDuringRound = false;

        public override void Load(bool hotReload)
        {
            // load challenges
            LoadChallenges();
            CheckForRunningChallenge();
            // register listeners
            // map events
            RegisterListener<Listeners.OnServerHibernationUpdate>(OnServerHibernationUpdate);
            RegisterListener<Listeners.OnMapStart>(OnMapStart);
            RegisterListener<Listeners.OnMapEnd>(OnMapEnd);
            RegisterEventHandler<EventRoundStart>(OnRoundStart);
            RegisterEventHandler<EventRoundEnd>(OnRoundEnd);
            // player events
            RegisterEventHandler<EventPlayerConnectFull>(OnPlayerConnectFull);
            RegisterEventHandler<EventPlayerDisconnect>(OnPlayerDisconnect);
            RegisterEventHandler<EventPlayerSpawn>(OnPlayerSpawn);
            RegisterEventHandler<EventPlayerDeath>(OnPlayerDeath);
            RegisterEventHandler<EventPlayerJump>(OnPlayerJump);
            RegisterEventHandler<EventPlayerBlind>(OnPlayerBlind);
            RegisterEventHandler<EventPlayerAvengedTeammate>(OnPlayerAvengedTeammate);
            RegisterEventHandler<EventPlayerChangename>(OnPlayerChangename);
            RegisterEventHandler<EventPlayerChat>(OnPlayerChat);
            RegisterEventHandler<EventPlayerDecal>(OnPlayerDecal);
            RegisterEventHandler<EventPlayerFalldamage>(OnPlayerFalldamage);
            RegisterEventHandler<EventPlayerFootstep>(OnPlayerFootstep);
            RegisterEventHandler<EventPlayerGivenC4>(OnPlayerGivenC4);
            RegisterEventHandler<EventPlayerHurt>(OnPlayerHurt);
            RegisterEventHandler<EventPlayerPing>(OnPlayerPing);
            RegisterEventHandler<EventPlayerRadio>(OnPlayerRadio);
            RegisterEventHandler<EventPlayerScore>(OnPlayerScore);
            RegisterEventHandler<EventPlayerSound>(OnPlayerSound);
            RegisterEventHandler<EventPlayerSpawned>(OnPlayerSpawned);
            RegisterEventHandler<EventPlayerTeam>(OnPlayerTeam);
            RegisterEventHandler<EventAchievementEarned>(OnAchievementEarned);
            RegisterEventHandler<EventAddPlayerSonarIcon>(OnAddPlayerSonarIcon);
            RegisterEventHandler<EventAmmoPickup>(OnAmmoPickup);
            RegisterEventHandler<EventBombAbortdefuse>(OnBombAbortdefuse);
            RegisterEventHandler<EventBombAbortplant>(OnBombAbortplant);
            RegisterEventHandler<EventBombBegindefuse>(OnBombBegindefuse);
            RegisterEventHandler<EventBombBeginplant>(OnBombBeginplant);
            RegisterEventHandler<EventBombDropped>(OnBombDropped);
            // other events
            RegisterEventHandler<EventBombExploded>(OnBombExploded);
            RegisterEventHandler<EventBombDefused>(OnBombDefused);
            // initialize custom events
            var customEventsSender = new CustomEventsSender();
            Capabilities.RegisterPluginCapability(ChallengesEvents, () => customEventsSender);
            // print message if hot reload
            if (hotReload)
            {
                // load player configs
                LoadActivePlayerConfigs();
                Console.WriteLine(Localizer["core.hotreload"]);
            }
        }

        public override void Unload(bool hotReload)
        {
            // unregister listeners
            // map events
            RemoveListener<Listeners.OnServerHibernationUpdate>(OnServerHibernationUpdate);
            RemoveListener<Listeners.OnMapStart>(OnMapStart);
            RemoveListener<Listeners.OnMapEnd>(OnMapEnd);
            DeregisterEventHandler<EventRoundStart>(OnRoundStart);
            DeregisterEventHandler<EventRoundEnd>(OnRoundEnd);
            // player events
            DeregisterEventHandler<EventPlayerConnectFull>(OnPlayerConnectFull);
            DeregisterEventHandler<EventPlayerDisconnect>(OnPlayerDisconnect);
            DeregisterEventHandler<EventPlayerSpawn>(OnPlayerSpawn);
            DeregisterEventHandler<EventPlayerDeath>(OnPlayerDeath);
            DeregisterEventHandler<EventPlayerJump>(OnPlayerJump);
            DeregisterEventHandler<EventPlayerBlind>(OnPlayerBlind);
            DeregisterEventHandler<EventPlayerAvengedTeammate>(OnPlayerAvengedTeammate);
            DeregisterEventHandler<EventPlayerChangename>(OnPlayerChangename);
            DeregisterEventHandler<EventPlayerChat>(OnPlayerChat);
            DeregisterEventHandler<EventPlayerDecal>(OnPlayerDecal);
            DeregisterEventHandler<EventPlayerFalldamage>(OnPlayerFalldamage);
            DeregisterEventHandler<EventPlayerFootstep>(OnPlayerFootstep);
            DeregisterEventHandler<EventPlayerGivenC4>(OnPlayerGivenC4);
            DeregisterEventHandler<EventPlayerHurt>(OnPlayerHurt);
            DeregisterEventHandler<EventPlayerPing>(OnPlayerPing);
            DeregisterEventHandler<EventPlayerRadio>(OnPlayerRadio);
            DeregisterEventHandler<EventPlayerScore>(OnPlayerScore);
            DeregisterEventHandler<EventPlayerSound>(OnPlayerSound);
            DeregisterEventHandler<EventPlayerSpawned>(OnPlayerSpawned);
            DeregisterEventHandler<EventPlayerTeam>(OnPlayerTeam);
            DeregisterEventHandler<EventAchievementEarned>(OnAchievementEarned);
            DeregisterEventHandler<EventAddPlayerSonarIcon>(OnAddPlayerSonarIcon);
            DeregisterEventHandler<EventAmmoPickup>(OnAmmoPickup);
            DeregisterEventHandler<EventBombAbortdefuse>(OnBombAbortdefuse);
            DeregisterEventHandler<EventBombAbortplant>(OnBombAbortplant);
            DeregisterEventHandler<EventBombBegindefuse>(OnBombBegindefuse);
            DeregisterEventHandler<EventBombBeginplant>(OnBombBeginplant);
            DeregisterEventHandler<EventBombDropped>(OnBombDropped);
            // other events
            DeregisterEventHandler<EventBombExploded>(OnBombExploded);
            DeregisterEventHandler<EventBombDefused>(OnBombDefused);
            // save config(s)
            Config.Update();
            SavePlayerConfigs();
            // hide GUI(s)
            HideAllGui();
            Console.WriteLine(Localizer["core.unload"]);
        }

        private void OnServerHibernationUpdate(bool isHibernating)
        {
            if (isHibernating)
            {
                // save config(s)
                SavePlayerConfigs();
                // hide GUI(s)
                HideAllGui();
            }
            else
            {
                // load
                LoadActivePlayerConfigs();
                LoadChallenges();
                CheckForRunningChallenge();
            }
        }

        private void OnMapStart(string mapName)
        {
            // load possible challenge changes
            Config.Reload();
            // load player configs
            LoadActivePlayerConfigs();
            LoadChallenges();
            CheckForRunningChallenge();
        }

        private void OnMapEnd()
        {
            // save config
            SavePlayerConfigs();
        }

        private HookResult OnRoundStart(EventRoundStart @event, GameEventInfo info)
        {
            // set variables
            _isDuringRound = true;
            ShowGuiOnRoundStart();
            return HookResult.Continue;
        }

        private HookResult OnRoundEnd(EventRoundEnd @event, GameEventInfo info)
        {
            // reset variables
            _isDuringRound = false;
            return HookResult.Continue;
        }

        private HookResult OnPlayerConnectFull(EventPlayerConnectFull @event, GameEventInfo info)
        {
            CCSPlayerController player = @event.Userid!;
            // skip bots
            if (player == null
                || player.IsBot) return HookResult.Continue;
            // read user configuration
            LoadPlayerConfig(player.NetworkIDString);
            // update player data
            _playerConfigs[player.NetworkIDString].ClanTag = player.PlayerName;
            _playerConfigs[player.NetworkIDString].ClanTag = player.ClanName;
            // bugfix: show empty worldtext on connect to allow instant display of worldtext entity
            WorldTextManager.Create(player, "");
            return HookResult.Continue;
        }

        private HookResult OnPlayerDisconnect(EventPlayerDisconnect @event, GameEventInfo info)
        {
            CCSPlayerController player = @event.Userid!;
            // skip bots
            if (player == null
                || player.IsBot) return HookResult.Continue;
            // add data
            if (!_playerConfigs.ContainsKey(@event.Networkid)) return HookResult.Continue;
            _playerConfigs[@event.Networkid].Username = player.PlayerName;
            _playerConfigs[@event.Networkid].ClanTag = player.ClanName;
            // save and unload player config
            UnloadPlayerConfig(@event.Networkid);
            return HookResult.Continue;
        }

        private HookResult OnPlayerSpawn(EventPlayerSpawn @event, GameEventInfo info)
        {
            CCSPlayerController player = @event.Userid!;
            // skip bots
            if (player == null
            || player.IsBot) return HookResult.Continue;
            // show GUIs
            ShowGuiOnSpawn(player);
            return HookResult.Continue;
        }
    }
}

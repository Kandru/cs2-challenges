using ChallengesShared;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Capabilities;
using CounterStrikeSharp.API.Core.Translations;
using CounterStrikeSharp.API.Modules.Extensions;
using System.Globalization;

namespace Challenges
{
    public partial class Challenges : BasePlugin
    {
        public override string ModuleName => "Challenges";
        public override string ModuleAuthor => "Kalle <kalle@kandru.de>";

        private static PluginCapability<IChallengesEventSender> ChallengesEvents { get; } = new("challenges:events");
        private readonly PlayerLanguageManager playerLanguageManager = new();
        private readonly Random _random = new Random(Guid.NewGuid().GetHashCode());
        private bool _isDuringRound = false;

        public override void Load(bool hotReload)
        {
            // Start the queue processing task
            Task.Run(() => ProcessChallengeQueueAsync(cancellationToken.Token));
            // load challenges
            LoadChallenges();
            CheckForRunningSchedule();
            // register listeners
            // map events
            RegisterListener<Listeners.OnServerHibernationUpdate>(OnServerHibernationUpdate);
            RegisterListener<Listeners.OnMapStart>(OnMapStart);
            RegisterListener<Listeners.OnMapEnd>(OnMapEnd);
            RegisterEventHandler<EventPlayerChat>(OnPlayerChatCommand);
            RegisterEventHandler<EventRoundStart>(OnRoundStart);
            RegisterEventHandler<EventRoundEnd>(OnRoundEnd);
            // player events
            RegisterEventHandler<EventPlayerConnectFull>(OnPlayerConnectFull);
            RegisterEventHandler<EventPlayerDisconnect>(OnPlayerDisconnect);
            RegisterEventHandler<EventPlayerSpawn>(OnPlayerSpawn);
            // register challenge listeners
            RegisterListeners();
            // initialize custom events
            var customEventsSender = new CustomEventsSender();
            Capabilities.RegisterPluginCapability(ChallengesEvents, () => customEventsSender);
            // print message if hot reload
            if (hotReload)
            {
                // calculate statistics
                CalculatePlayersWithMostChallengesSolved();
                // load player configs
                LoadActivePlayerConfigs();
                // check if it is during a round (no matter if warmup or not, simply not in between a round or end of match)
                if (GetGameRules()?.GamePhase <= 3)
                {
                    // set variables
                    _isDuringRound = true;
                    // show GUIs
                    ShowGuiOnRoundStart();
                }
                Console.WriteLine(Localizer["core.hotreload"]);
            }
            // save config
            Config.Update();
        }

        public override void Unload(bool hotReload)
        {
            // stop the queue processing task
            cancellationToken.Cancel();
            // remove listeners
            // unregister listeners
            // map events
            RemoveListener<Listeners.OnServerHibernationUpdate>(OnServerHibernationUpdate);
            RemoveListener<Listeners.OnMapStart>(OnMapStart);
            RemoveListener<Listeners.OnMapEnd>(OnMapEnd);
            DeregisterEventHandler<EventRoundStart>(OnRoundStart);
            DeregisterEventHandler<EventRoundEnd>(OnRoundEnd);
            DeregisterEventHandler<EventPlayerChat>(OnPlayerChatCommand);
            // player events
            DeregisterEventHandler<EventPlayerConnectFull>(OnPlayerConnectFull);
            DeregisterEventHandler<EventPlayerDisconnect>(OnPlayerDisconnect);
            DeregisterEventHandler<EventPlayerSpawn>(OnPlayerSpawn);
            // remove challenge listeners
            RemoveListeners();
            // save config(s)
            Config.Update();
            SavePlayerConfigs();
            // hide GUI(s)
            HideAllGui();
            Console.WriteLine(Localizer["core.unload"]);
        }

        private void RegisterListeners()
        {
            // remove old listeners
            RemoveListeners();
            // player events
            var challengeTypes = _currentSchedule.Challenges.Select(c => c.Value.Type).ToList();
            if (challengeTypes.Contains("player_kill_assist")
                || challengeTypes.Contains("player_kill")
                || challengeTypes.Contains("player_death"))
                RegisterEventHandler<EventPlayerDeath>(OnPlayerDeath);
            if (challengeTypes.Contains("player_jump"))
                RegisterEventHandler<EventPlayerJump>(OnPlayerJump);
            if (challengeTypes.Contains("player_has_blinded")
                || challengeTypes.Contains("player_got_blinded")) RegisterEventHandler<EventPlayerBlind>(OnPlayerBlind);
            if (challengeTypes.Contains("player_has_avenged_teammate")
                || challengeTypes.Contains("player_got_avenged_teammate")) RegisterEventHandler<EventPlayerAvengedTeammate>(OnPlayerAvengedTeammate);
            if (challengeTypes.Contains("player_changed_name"))
                RegisterEventHandler<EventPlayerChangename>(OnPlayerChangename);
            if (challengeTypes.Contains("player_chat"))
                RegisterEventHandler<EventPlayerChat>(OnPlayerChat);
            if (challengeTypes.Contains("player_decal"))
                RegisterEventHandler<EventPlayerDecal>(OnPlayerDecal);
            if (challengeTypes.Contains("player_falldamage"))
                RegisterEventHandler<EventPlayerFalldamage>(OnPlayerFalldamage);
            if (challengeTypes.Contains("player_footstep"))
                RegisterEventHandler<EventPlayerFootstep>(OnPlayerFootstep);
            if (challengeTypes.Contains("player_givenc4"))
                RegisterEventHandler<EventPlayerGivenC4>(OnPlayerGivenC4);
            if (challengeTypes.Contains("player_hurt_attacker")
                || challengeTypes.Contains("player_hurt_victim")) RegisterEventHandler<EventPlayerHurt>(OnPlayerHurt);
            if (challengeTypes.Contains("player_ping"))
                RegisterEventHandler<EventPlayerPing>(OnPlayerPing);
            if (challengeTypes.Contains("player_radio"))
                RegisterEventHandler<EventPlayerRadio>(OnPlayerRadio);
            if (challengeTypes.Contains("player_score"))
                RegisterEventHandler<EventPlayerScore>(OnPlayerScore);
            if (challengeTypes.Contains("player_sound"))
                RegisterEventHandler<EventPlayerSound>(OnPlayerSound);
            if (challengeTypes.Contains("player_spawned"))
                RegisterEventHandler<EventPlayerSpawned>(OnPlayerSpawned);
            if (challengeTypes.Contains("player_team"))
                RegisterEventHandler<EventPlayerTeam>(OnPlayerTeam);
            if (challengeTypes.Contains("player_achievement_earned"))
                RegisterEventHandler<EventAchievementEarned>(OnAchievementEarned);
            if (challengeTypes.Contains("player_add_sonar_icon"))
                RegisterEventHandler<EventAddPlayerSonarIcon>(OnAddPlayerSonarIcon);
            if (challengeTypes.Contains("player_ammo_pickup"))
                RegisterEventHandler<EventAmmoPickup>(OnAmmoPickup);
            if (challengeTypes.Contains("player_bomb_abortdefuse"))
                RegisterEventHandler<EventBombAbortdefuse>(OnBombAbortdefuse);
            if (challengeTypes.Contains("player_bomb_abortplant"))
                RegisterEventHandler<EventBombAbortplant>(OnBombAbortplant);
            if (challengeTypes.Contains("player_bomb_begindefuse"))
                RegisterEventHandler<EventBombBegindefuse>(OnBombBegindefuse);
            if (challengeTypes.Contains("player_bomb_beginplant"))
                RegisterEventHandler<EventBombBeginplant>(OnBombBeginplant);
            if (challengeTypes.Contains("player_bomb_dropped"))
                RegisterEventHandler<EventBombDropped>(OnBombDropped);
            if (challengeTypes.Contains("player_bomb_pickup"))
                RegisterEventHandler<EventBombPickup>(OnBombPickup);
            if (challengeTypes.Contains("player_bomb_planted"))
                RegisterEventHandler<EventBombPlanted>(OnBombPlanted);
            if (challengeTypes.Contains("player_bot_takeover"))
                RegisterEventHandler<EventBotTakeover>(OnBotTakeover);
            if (challengeTypes.Contains("player_defuser_pickup"))
                RegisterEventHandler<EventDefuserPickup>(OnDefuserPickup);
            // other events
            if (challengeTypes.Contains("bomb_beep"))
                RegisterEventHandler<EventBombBeep>(OnBombBeep);
            if (challengeTypes.Contains("bomb_exploded"))
                RegisterEventHandler<EventBombExploded>(OnBombExploded);
            if (challengeTypes.Contains("bomb_defused"))
                RegisterEventHandler<EventBombDefused>(OnBombDefused);
            if (challengeTypes.Contains("break_breakable"))
                RegisterEventHandler<EventBreakBreakable>(OnBreakBreakable);
            if (challengeTypes.Contains("break_prop"))
                RegisterEventHandler<EventBreakProp>(OnBreakProp);
            if (challengeTypes.Contains("bullet_impact"))
                RegisterEventHandler<EventBulletImpact>(OnBulletImpact);
            if (challengeTypes.Contains("buymenu_close"))
                RegisterEventHandler<EventBuymenuClose>(OnBuymenuClose);
            if (challengeTypes.Contains("buytime_ended"))
                RegisterEventHandler<EventBuytimeEnded>(OnBuytimeEnded);
            if (challengeTypes.Contains("bullet_damage_given")
               || challengeTypes.Contains("bullet_damage_taken"))
                RegisterEventHandler<EventBulletDamage>(OnBulletDamage);
            if (challengeTypes.Contains("door_closed"))
                RegisterEventHandler<EventDoorClosed>(OnDoorClosed);
            if (challengeTypes.Contains("door_open"))
                RegisterEventHandler<EventDoorOpen>(OnDoorOpen);
            if (challengeTypes.Contains("enter_bombzone"))
                RegisterEventHandler<EventEnterBombzone>(OnEnterBombzone);
            if (challengeTypes.Contains("exit_bombzone"))
                RegisterEventHandler<EventExitBombzone>(OnExitBombzone);
            if (challengeTypes.Contains("enter_buyzone"))
                RegisterEventHandler<EventEnterBuyzone>(OnEnterBuyzone);
            if (challengeTypes.Contains("exit_buyzone"))
                RegisterEventHandler<EventExitBuyzone>(OnExitBuyzone);
            if (challengeTypes.Contains("enter_rescuezone"))
                RegisterEventHandler<EventEnterRescueZone>(OnEnterRescuezone);
            if (challengeTypes.Contains("exit_rescuezone"))
                RegisterEventHandler<EventExitRescueZone>(OnExitRescuezone);
            if (challengeTypes.Contains("grenade_bounce"))
                RegisterEventHandler<EventGrenadeBounce>(OnGrenadeBounce);
            if (challengeTypes.Contains("grenade_thrown"))
                RegisterEventHandler<EventGrenadeThrown>(OnGrenadeThrown);
            if (challengeTypes.Contains("hostage_follows"))
                RegisterEventHandler<EventHostageFollows>(OnHostageFollows);
            if (challengeTypes.Contains("hostage_hurt"))
                RegisterEventHandler<EventHostageHurt>(OnHostageHurt);
            if (challengeTypes.Contains("hostage_killed"))
                RegisterEventHandler<EventHostageKilled>(OnHostageKilled);
            if (challengeTypes.Contains("hostage_rescued"))
                RegisterEventHandler<EventHostageRescued>(OnHostageRescued);
            if (challengeTypes.Contains("hostage_rescued_all"))
                RegisterEventHandler<EventHostageRescuedAll>(OnHostageRescuedAll);
            if (challengeTypes.Contains("hostage_stops_following"))
                RegisterEventHandler<EventHostageStopsFollowing>(OnHostageStopsFollowing);
            if (challengeTypes.Contains("inspect_weapon"))
                RegisterEventHandler<EventInspectWeapon>(OnInspectWeapon);
            if (challengeTypes.Contains("item_pickup"))
                RegisterEventHandler<EventItemPickup>(OnItemPickup);
            if (challengeTypes.Contains("item_purchase"))
                RegisterEventHandler<EventItemPurchase>(OnItemPurchase);
            if (challengeTypes.Contains("team_score"))
                RegisterEventHandler<EventTeamScore>(OnTeamScore);
            if (challengeTypes.Contains("weapon_fire"))
                RegisterEventHandler<EventWeaponFire>(OnWeaponFire);
            if (challengeTypes.Contains("weapon_fire_on_empty"))
                RegisterEventHandler<EventWeaponFireOnEmpty>(OnWeaponFireOnEmpty);
            if (challengeTypes.Contains("weapon_reload"))
                RegisterEventHandler<EventWeaponReload>(OnWeaponReload);
            if (challengeTypes.Contains("weapon_zoom"))
                RegisterEventHandler<EventWeaponZoom>(OnWeaponZoom);
            if (challengeTypes.Contains("weapon_zoom_rifle"))
                RegisterEventHandler<EventWeaponZoomRifle>(OnWeaponZoomRifle);
        }

        private void RemoveListeners()
        {
            // player events
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
            DeregisterEventHandler<EventBombPickup>(OnBombPickup);
            DeregisterEventHandler<EventBombPlanted>(OnBombPlanted);
            DeregisterEventHandler<EventBotTakeover>(OnBotTakeover);
            DeregisterEventHandler<EventDefuserPickup>(OnDefuserPickup);
            // other events
            DeregisterEventHandler<EventBombBeep>(OnBombBeep);
            DeregisterEventHandler<EventBombExploded>(OnBombExploded);
            DeregisterEventHandler<EventBombDefused>(OnBombDefused);
            DeregisterEventHandler<EventBreakBreakable>(OnBreakBreakable);
            DeregisterEventHandler<EventBreakProp>(OnBreakProp);
            DeregisterEventHandler<EventBulletImpact>(OnBulletImpact);
            DeregisterEventHandler<EventBuymenuClose>(OnBuymenuClose);
            DeregisterEventHandler<EventBuytimeEnded>(OnBuytimeEnded);
            DeregisterEventHandler<EventBulletDamage>(OnBulletDamage);
            DeregisterEventHandler<EventDoorClosed>(OnDoorClosed);
            DeregisterEventHandler<EventDoorOpen>(OnDoorOpen);
            DeregisterEventHandler<EventEnterBombzone>(OnEnterBombzone);
            DeregisterEventHandler<EventExitBombzone>(OnExitBombzone);
            DeregisterEventHandler<EventEnterBuyzone>(OnEnterBuyzone);
            DeregisterEventHandler<EventExitBuyzone>(OnExitBuyzone);
            DeregisterEventHandler<EventEnterRescueZone>(OnEnterRescuezone);
            DeregisterEventHandler<EventExitRescueZone>(OnExitRescuezone);
            DeregisterEventHandler<EventGrenadeBounce>(OnGrenadeBounce);
            DeregisterEventHandler<EventGrenadeThrown>(OnGrenadeThrown);
            DeregisterEventHandler<EventHostageFollows>(OnHostageFollows);
            DeregisterEventHandler<EventHostageHurt>(OnHostageHurt);
            DeregisterEventHandler<EventHostageKilled>(OnHostageKilled);
            DeregisterEventHandler<EventHostageRescued>(OnHostageRescued);
            DeregisterEventHandler<EventHostageRescuedAll>(OnHostageRescuedAll);
            DeregisterEventHandler<EventHostageStopsFollowing>(OnHostageStopsFollowing);
            DeregisterEventHandler<EventInspectWeapon>(OnInspectWeapon);
            DeregisterEventHandler<EventItemPickup>(OnItemPickup);
            DeregisterEventHandler<EventItemPurchase>(OnItemPurchase);
            DeregisterEventHandler<EventTeamScore>(OnTeamScore);
            DeregisterEventHandler<EventWeaponFire>(OnWeaponFire);
            DeregisterEventHandler<EventWeaponFireOnEmpty>(OnWeaponFireOnEmpty);
            DeregisterEventHandler<EventWeaponReload>(OnWeaponReload);
            DeregisterEventHandler<EventWeaponZoom>(OnWeaponZoom);
            DeregisterEventHandler<EventWeaponZoomRifle>(OnWeaponZoomRifle);
        }

        private void OnServerHibernationUpdate(bool isHibernating)
        {
            if (isHibernating)
            {
                // save config(s)
                SavePlayerConfigs();
                // hide GUI(s)
                HideAllGui();
                // remove challenge listeners
                RemoveListeners();
            }
            else
            {
                // load
                LoadActivePlayerConfigs();
                LoadChallenges();
                CheckForRunningSchedule();
                // register challenge listeners
                RegisterListeners();
            }
        }

        private void OnMapStart(string mapName)
        {
            // load current config
            Config.Reload();
            // calculate statistics
            CalculatePlayersWithMostChallengesSolved();
            // load player configs
            LoadActivePlayerConfigs();
            LoadChallenges();
            CheckForRunningSchedule();
            // register challenge listeners
            RegisterListeners();
        }

        private void OnMapEnd()
        {
            // save config
            SavePlayerConfigs();
            // remove challenge listeners
            RemoveListeners();
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
            _playerConfigs[player.NetworkIDString].Username = player.PlayerName;
            _playerConfigs[player.NetworkIDString].SteamId = player.NetworkIDString;
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

        private HookResult OnPlayerChatCommand(EventPlayerChat @event, GameEventInfo info)
        {
            CCSPlayerController? player = Utilities.GetPlayerFromUserid(@event.Userid);
            if (player == null
                || !player.IsValid
                || player.IsBot) return HookResult.Continue;
            if (@event.Text.StartsWith("!lang", StringComparison.OrdinalIgnoreCase))
            {
                // get language from command instead of player because it defaults to english all the time Oo
                string? language = @event.Text.Split(' ').Skip(1).FirstOrDefault()?.Trim();
                if (language == null
                    || !CultureInfo.GetCultures(CultureTypes.AllCultures).Any(c => c.Name.Equals(language, StringComparison.OrdinalIgnoreCase)))
                {
                    return HookResult.Continue;
                }
                // set language for player
                SavePlayerLanguage(player.NetworkIDString, language);
                // delay one frame to ensure the language is set
                Server.NextFrame(() =>
                {
                    if (player == null
                        || !player.IsValid
                        || !_playerConfigs.ContainsKey(player.NetworkIDString)) return;
                    float duration = _playerConfigs[player.NetworkIDString].Settings.Challenges.ShowAlways
                        ? 0
                        : Config.GUI.OnRoundStartDuration;
                    ShowGui(player, duration);
                });
                return HookResult.Continue;
            }
            // redraw GUI
            return HookResult.Continue;
        }
    }
}

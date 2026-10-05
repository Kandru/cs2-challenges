# Events

Use a **challenge type** as the task `type:` in your blueprint. Open an event page for the rule keys you can check.

Source: CounterStrikeSharp `main` (synced tip `653d651f1ac0`).

## Common types

| Challenge type | Typical use |
|----------------|-------------|
| `player_kill` | Player got a kill |
| `player_hurt_attacker` | Player dealt damage |
| `player_bomb_planted` | Player planted the bomb |
| `player_bomb_begindefuse` | Player started defusing |
| `weapon_fire` | Player fired a weapon |
| `round_start` | Round started |
| `on_player_chat` | Player sent a chat message (listener) |
| `player_chat` | Player sent a chat message |

## All types

<details><summary><b>Players</b> (45)</summary>

| Challenge type | Page |
|----------------|------|
| `entity_killed` | [EventEntityKilled](events/EventEntityKilled.md) |
| `gg_killed_enemy_attackerid` | [EventGgKilledEnemy](events/EventGgKilledEnemy.md) |
| `gg_killed_enemy_victimid` | [EventGgKilledEnemy](events/EventGgKilledEnemy.md) |
| `local_player_controller_team` | [EventLocalPlayerControllerTeam](events/EventLocalPlayerControllerTeam.md) |
| `local_player_pawn_changed` | [EventLocalPlayerPawnChanged](events/EventLocalPlayerPawnChanged.md) |
| `local_player_team` | [EventLocalPlayerTeam](events/EventLocalPlayerTeam.md) |
| `other_death` | [EventOtherDeath](events/EventOtherDeath.md) |
| `player_achievement_earned` | [EventAchievementEarned](events/EventAchievementEarned.md) |
| `player_activate` | [EventPlayerActivate](events/EventPlayerActivate.md) |
| `player_add_sonar_icon` | [EventAddPlayerSonarIcon](events/EventAddPlayerSonarIcon.md) |
| `player_bot_takeover` | [EventBotTakeover](events/EventBotTakeover.md) |
| `player_changed_name` | [EventPlayerChangename](events/EventPlayerChangename.md) |
| `player_connect` | [EventPlayerConnect](events/EventPlayerConnect.md) |
| `player_connect_full` | [EventPlayerConnectFull](events/EventPlayerConnectFull.md) |
| `player_death` | [EventPlayerDeath](events/EventPlayerDeath.md) |
| `player_decal` | [EventPlayerDecal](events/EventPlayerDecal.md) |
| `player_disconnect` | [EventPlayerDisconnect](events/EventPlayerDisconnect.md) |
| `player_falldamage` | [EventPlayerFalldamage](events/EventPlayerFalldamage.md) |
| `player_footstep` | [EventPlayerFootstep](events/EventPlayerFootstep.md) |
| `player_full_update` | [EventPlayerFullUpdate](events/EventPlayerFullUpdate.md) |
| `player_givenc4` | [EventPlayerGivenC4](events/EventPlayerGivenC4.md) |
| `player_got_avenged_teammate` | [EventPlayerAvengedTeammate](events/EventPlayerAvengedTeammate.md) |
| `player_got_blinded` | [EventPlayerBlind](events/EventPlayerBlind.md) |
| `player_has_avenged_teammate` | [EventPlayerAvengedTeammate](events/EventPlayerAvengedTeammate.md) |
| `player_has_blinded` | [EventPlayerBlind](events/EventPlayerBlind.md) |
| `player_hintmessage` | [EventPlayerHintmessage](events/EventPlayerHintmessage.md) |
| `player_hurt_attacker` | [EventPlayerHurt](events/EventPlayerHurt.md) |
| `player_hurt_victim` | [EventPlayerHurt](events/EventPlayerHurt.md) |
| `player_info` | [EventPlayerInfo](events/EventPlayerInfo.md) |
| `player_jump` | [EventPlayerJump](events/EventPlayerJump.md) |
| `player_kill` | [EventPlayerDeath](events/EventPlayerDeath.md) |
| `player_kill_assist` | [EventPlayerDeath](events/EventPlayerDeath.md) |
| `player_ping` | [EventPlayerPing](events/EventPlayerPing.md) |
| `player_ping_stop` | [EventPlayerPingStop](events/EventPlayerPingStop.md) |
| `player_radio` | [EventPlayerRadio](events/EventPlayerRadio.md) |
| `player_score` | [EventPlayerScore](events/EventPlayerScore.md) |
| `player_shoot` | [EventPlayerShoot](events/EventPlayerShoot.md) |
| `player_sound` | [EventPlayerSound](events/EventPlayerSound.md) |
| `player_spawn` | [EventPlayerSpawn](events/EventPlayerSpawn.md) |
| `player_spawned` | [EventPlayerSpawned](events/EventPlayerSpawned.md) |
| `player_stats_updated` | [EventPlayerStatsUpdated](events/EventPlayerStatsUpdated.md) |
| `player_team` | [EventPlayerTeam](events/EventPlayerTeam.md) |
| `vip_escaped` | [EventVipEscaped](events/EventVipEscaped.md) |
| `vip_killed_attacker` | [EventVipKilled](events/EventVipKilled.md) |
| `vip_killed_userid` | [EventVipKilled](events/EventVipKilled.md) |

</details>

<details><summary><b>Round</b> (33)</summary>

| Challenge type | Page |
|----------------|------|
| `announce_phase_end` | [EventAnnouncePhaseEnd](events/EventAnnouncePhaseEnd.md) |
| `begin_new_match` | [EventBeginNewMatch](events/EventBeginNewMatch.md) |
| `cs_intermission` | [EventCsIntermission](events/EventCsIntermission.md) |
| `cs_match_end_restart` | [EventCsMatchEndRestart](events/EventCsMatchEndRestart.md) |
| `cs_pre_restart` | [EventCsPreRestart](events/EventCsPreRestart.md) |
| `cs_round_final_beep` | [EventCsRoundFinalBeep](events/EventCsRoundFinalBeep.md) |
| `cs_round_start_beep` | [EventCsRoundStartBeep](events/EventCsRoundStartBeep.md) |
| `cs_win_panel_match` | [EventCsWinPanelMatch](events/EventCsWinPanelMatch.md) |
| `cs_win_panel_round` | [EventCsWinPanelRound](events/EventCsWinPanelRound.md) |
| `game_end` | [EventGameEnd](events/EventGameEnd.md) |
| `game_phase_changed` | [EventGamePhaseChanged](events/EventGamePhaseChanged.md) |
| `game_start` | [EventGameStart](events/EventGameStart.md) |
| `match_end_conditions` | [EventMatchEndConditions](events/EventMatchEndConditions.md) |
| `round_announce_final` | [EventRoundAnnounceFinal](events/EventRoundAnnounceFinal.md) |
| `round_announce_last_round_half` | [EventRoundAnnounceLastRoundHalf](events/EventRoundAnnounceLastRoundHalf.md) |
| `round_announce_match_point` | [EventRoundAnnounceMatchPoint](events/EventRoundAnnounceMatchPoint.md) |
| `round_announce_match_start` | [EventRoundAnnounceMatchStart](events/EventRoundAnnounceMatchStart.md) |
| `round_announce_warmup` | [EventRoundAnnounceWarmup](events/EventRoundAnnounceWarmup.md) |
| `round_end` | [EventRoundEnd](events/EventRoundEnd.md) |
| `round_end_upload_stats` | [EventRoundEndUploadStats](events/EventRoundEndUploadStats.md) |
| `round_freeze_end` | [EventRoundFreezeEnd](events/EventRoundFreezeEnd.md) |
| `round_mvp` | [EventRoundMvp](events/EventRoundMvp.md) |
| `round_officially_ended` | [EventRoundOfficiallyEnded](events/EventRoundOfficiallyEnded.md) |
| `round_poststart` | [EventRoundPoststart](events/EventRoundPoststart.md) |
| `round_prestart` | [EventRoundPrestart](events/EventRoundPrestart.md) |
| `round_start` | [EventRoundStart](events/EventRoundStart.md) |
| `round_start_post_nav` | [EventRoundStartPostNav](events/EventRoundStartPostNav.md) |
| `round_start_pre_entity` | [EventRoundStartPreEntity](events/EventRoundStartPreEntity.md) |
| `round_time_warning` | [EventRoundTimeWarning](events/EventRoundTimeWarning.md) |
| `start_halftime` | [EventStartHalftime](events/EventStartHalftime.md) |
| `survival_announce_phase` | [EventSurvivalAnnouncePhase](events/EventSurvivalAnnouncePhase.md) |
| `teamplay_round_start` | [EventTeamplayRoundStart](events/EventTeamplayRoundStart.md) |
| `warmup_end` | [EventWarmupEnd](events/EventWarmupEnd.md) |

</details>

<details><summary><b>Bomb</b> (14)</summary>

| Challenge type | Page |
|----------------|------|
| `bomb_beep` | [EventBombBeep](events/EventBombBeep.md) |
| `bomb_defused` | [EventBombDefused](events/EventBombDefused.md) |
| `bomb_exploded` | [EventBombExploded](events/EventBombExploded.md) |
| `defuser_dropped` | [EventDefuserDropped](events/EventDefuserDropped.md) |
| `enter_bombzone` | [EventEnterBombzone](events/EventEnterBombzone.md) |
| `exit_bombzone` | [EventExitBombzone](events/EventExitBombzone.md) |
| `player_bomb_abortdefuse` | [EventBombAbortdefuse](events/EventBombAbortdefuse.md) |
| `player_bomb_abortplant` | [EventBombAbortplant](events/EventBombAbortplant.md) |
| `player_bomb_begindefuse` | [EventBombBegindefuse](events/EventBombBegindefuse.md) |
| `player_bomb_beginplant` | [EventBombBeginplant](events/EventBombBeginplant.md) |
| `player_bomb_dropped` | [EventBombDropped](events/EventBombDropped.md) |
| `player_bomb_pickup` | [EventBombPickup](events/EventBombPickup.md) |
| `player_bomb_planted` | [EventBombPlanted](events/EventBombPlanted.md) |
| `player_defuser_pickup` | [EventDefuserPickup](events/EventDefuserPickup.md) |

</details>

<details><summary><b>Weapons</b> (25)</summary>

| Challenge type | Page |
|----------------|------|
| `ammo_refill` | [EventAmmoRefill](events/EventAmmoRefill.md) |
| `buymenu_close` | [EventBuymenuClose](events/EventBuymenuClose.md) |
| `buymenu_open` | [EventBuymenuOpen](events/EventBuymenuOpen.md) |
| `buytime_ended` | [EventBuytimeEnded](events/EventBuytimeEnded.md) |
| `cart_updated` | [EventCartUpdated](events/EventCartUpdated.md) |
| `dm_bonus_weapon_start` | [EventDmBonusWeaponStart](events/EventDmBonusWeaponStart.md) |
| `dz_item_interaction` | [EventDzItemInteraction](events/EventDzItemInteraction.md) |
| `inspect_weapon` | [EventInspectWeapon](events/EventInspectWeapon.md) |
| `inventory_updated` | [EventInventoryUpdated](events/EventInventoryUpdated.md) |
| `item_equip` | [EventItemEquip](events/EventItemEquip.md) |
| `item_pickup` | [EventItemPickup](events/EventItemPickup.md) |
| `item_pickup_failed` | [EventItemPickupFailed](events/EventItemPickupFailed.md) |
| `item_pickup_slerp` | [EventItemPickupSlerp](events/EventItemPickupSlerp.md) |
| `item_purchase` | [EventItemPurchase](events/EventItemPurchase.md) |
| `item_remove` | [EventItemRemove](events/EventItemRemove.md) |
| `item_schema_initialized` | [EventItemSchemaInitialized](events/EventItemSchemaInitialized.md) |
| `player_ammo_pickup` | [EventAmmoPickup](events/EventAmmoPickup.md) |
| `silencer_detach` | [EventSilencerDetach](events/EventSilencerDetach.md) |
| `silencer_off` | [EventSilencerOff](events/EventSilencerOff.md) |
| `silencer_on` | [EventSilencerOn](events/EventSilencerOn.md) |
| `weapon_fire` | [EventWeaponFire](events/EventWeaponFire.md) |
| `weapon_fire_on_empty` | [EventWeaponFireOnEmpty](events/EventWeaponFireOnEmpty.md) |
| `weapon_reload` | [EventWeaponReload](events/EventWeaponReload.md) |
| `weapon_zoom` | [EventWeaponZoom](events/EventWeaponZoom.md) |
| `weapon_zoom_rifle` | [EventWeaponZoomRifle](events/EventWeaponZoomRifle.md) |

</details>

<details><summary><b>Grenades</b> (16)</summary>

| Challenge type | Page |
|----------------|------|
| `decoy_detonate` | [EventDecoyDetonate](events/EventDecoyDetonate.md) |
| `decoy_firing` | [EventDecoyFiring](events/EventDecoyFiring.md) |
| `decoy_started` | [EventDecoyStarted](events/EventDecoyStarted.md) |
| `flashbang_detonate` | [EventFlashbangDetonate](events/EventFlashbangDetonate.md) |
| `grenade_bounce` | [EventGrenadeBounce](events/EventGrenadeBounce.md) |
| `grenade_thrown` | [EventGrenadeThrown](events/EventGrenadeThrown.md) |
| `hegrenade_detonate` | [EventHegrenadeDetonate](events/EventHegrenadeDetonate.md) |
| `helicopter_grenade_punt_miss` | [EventHelicopterGrenadePuntMiss](events/EventHelicopterGrenadePuntMiss.md) |
| `inferno_expire` | [EventInfernoExpire](events/EventInfernoExpire.md) |
| `inferno_extinguish` | [EventInfernoExtinguish](events/EventInfernoExtinguish.md) |
| `inferno_startburn` | [EventInfernoStartburn](events/EventInfernoStartburn.md) |
| `molotov_detonate` | [EventMolotovDetonate](events/EventMolotovDetonate.md) |
| `smoke_beacon_paradrop` | [EventSmokeBeaconParadrop](events/EventSmokeBeaconParadrop.md) |
| `smokegrenade_detonate` | [EventSmokegrenadeDetonate](events/EventSmokegrenadeDetonate.md) |
| `smokegrenade_expired` | [EventSmokegrenadeExpired](events/EventSmokegrenadeExpired.md) |
| `tagrenade_detonate` | [EventTagrenadeDetonate](events/EventTagrenadeDetonate.md) |

</details>

<details><summary><b>Hostage</b> (9)</summary>

| Challenge type | Page |
|----------------|------|
| `enter_rescuezone` | [EventEnterRescueZone](events/EventEnterRescueZone.md) |
| `exit_rescuezone` | [EventExitRescueZone](events/EventExitRescueZone.md) |
| `hostage_call_for_help` | [EventHostageCallForHelp](events/EventHostageCallForHelp.md) |
| `hostage_follows` | [EventHostageFollows](events/EventHostageFollows.md) |
| `hostage_hurt` | [EventHostageHurt](events/EventHostageHurt.md) |
| `hostage_killed` | [EventHostageKilled](events/EventHostageKilled.md) |
| `hostage_rescued` | [EventHostageRescued](events/EventHostageRescued.md) |
| `hostage_rescued_all` | [EventHostageRescuedAll](events/EventHostageRescuedAll.md) |
| `hostage_stops_following` | [EventHostageStopsFollowing](events/EventHostageStopsFollowing.md) |

</details>

<details><summary><b>Vote</b> (13)</summary>

| Challenge type | Page |
|----------------|------|
| `enable_restart_voting` | [EventEnableRestartVoting](events/EventEnableRestartVoting.md) |
| `endmatch_mapvote_selecting_map` | [EventEndmatchMapvoteSelectingMap](events/EventEndmatchMapvoteSelectingMap.md) |
| `player_reset_vote` | [EventPlayerResetVote](events/EventPlayerResetVote.md) |
| `start_vote` | [EventStartVote](events/EventStartVote.md) |
| `vote_cast` | [EventVoteCast](events/EventVoteCast.md) |
| `vote_cast_no` | [EventVoteCastNo](events/EventVoteCastNo.md) |
| `vote_cast_yes` | [EventVoteCastYes](events/EventVoteCastYes.md) |
| `vote_changed` | [EventVoteChanged](events/EventVoteChanged.md) |
| `vote_ended` | [EventVoteEnded](events/EventVoteEnded.md) |
| `vote_failed` | [EventVoteFailed](events/EventVoteFailed.md) |
| `vote_options` | [EventVoteOptions](events/EventVoteOptions.md) |
| `vote_passed` | [EventVotePassed](events/EventVotePassed.md) |
| `vote_started` | [EventVoteStarted](events/EventVoteStarted.md) |

</details>

<details><summary><b>Server</b> (56)</summary>

| Challenge type | Page |
|----------------|------|
| `achievement_earned_local` | [EventAchievementEarnedLocal](events/EventAchievementEarnedLocal.md) |
| `achievement_event` | [EventAchievementEvent](events/EventAchievementEvent.md) |
| `achievement_info_loaded` | [EventAchievementInfoLoaded](events/EventAchievementInfoLoaded.md) |
| `achievement_write_failed` | [EventAchievementWriteFailed](events/EventAchievementWriteFailed.md) |
| `client_disconnect` | [EventClientDisconnect](events/EventClientDisconnect.md) |
| `client_loadout_changed` | [EventClientLoadoutChanged](events/EventClientLoadoutChanged.md) |
| `clientside_lesson_closed` | [EventClientsideLessonClosed](events/EventClientsideLessonClosed.md) |
| `clientside_reload_custom_econ` | [EventClientsideReloadCustomEcon](events/EventClientsideReloadCustomEcon.md) |
| `demo_skip` | [EventDemoSkip](events/EventDemoSkip.md) |
| `demo_start` | [EventDemoStart](events/EventDemoStart.md) |
| `demo_stop` | [EventDemoStop](events/EventDemoStop.md) |
| `difficulty_changed` | [EventDifficultyChanged](events/EventDifficultyChanged.md) |
| `game_init` | [EventGameInit](events/EventGameInit.md) |
| `game_message` | [EventGameMessage](events/EventGameMessage.md) |
| `game_newmap` | [EventGameNewmap](events/EventGameNewmap.md) |
| `gameinstructor_draw` | [EventGameinstructorDraw](events/EventGameinstructorDraw.md) |
| `gameinstructor_nodraw` | [EventGameinstructorNodraw](events/EventGameinstructorNodraw.md) |
| `gameui_hidden` | [EventGameuiHidden](events/EventGameuiHidden.md) |
| `gc_connected` | [EventGcConnected](events/EventGcConnected.md) |
| `hostname_changed` | [EventHostnameChanged](events/EventHostnameChanged.md) |
| `instructor_close_lesson` | [EventInstructorCloseLesson](events/EventInstructorCloseLesson.md) |
| `instructor_server_hint_create_hint_activator_userid` | [EventInstructorServerHintCreate](events/EventInstructorServerHintCreate.md) |
| `instructor_server_hint_create_userid` | [EventInstructorServerHintCreate](events/EventInstructorServerHintCreate.md) |
| `instructor_server_hint_stop` | [EventInstructorServerHintStop](events/EventInstructorServerHintStop.md) |
| `instructor_start_lesson` | [EventInstructorStartLesson](events/EventInstructorStartLesson.md) |
| `jointeam_failed` | [EventJointeamFailed](events/EventJointeamFailed.md) |
| `map_shutdown` | [EventMapShutdown](events/EventMapShutdown.md) |
| `map_transition` | [EventMapTransition](events/EventMapTransition.md) |
| `nav_blocked` | [EventNavBlocked](events/EventNavBlocked.md) |
| `nav_generate` | [EventNavGenerate](events/EventNavGenerate.md) |
| `nextlevel_changed` | [EventNextlevelChanged](events/EventNextlevelChanged.md) |
| `seasoncoin_levelup` | [EventSeasoncoinLevelup](events/EventSeasoncoinLevelup.md) |
| `server_cvar` | [EventServerCvar](events/EventServerCvar.md) |
| `server_message` | [EventServerMessage](events/EventServerMessage.md) |
| `server_pre_shutdown` | [EventServerPreShutdown](events/EventServerPreShutdown.md) |
| `server_shutdown` | [EventServerShutdown](events/EventServerShutdown.md) |
| `server_spawn` | [EventServerSpawn](events/EventServerSpawn.md) |
| `set_instructor_group_enabled` | [EventSetInstructorGroupEnabled](events/EventSetInstructorGroupEnabled.md) |
| `sfuievent` | [EventSfuievent](events/EventSfuievent.md) |
| `spec_mode_updated` | [EventSpecModeUpdated](events/EventSpecModeUpdated.md) |
| `spec_target_updated` | [EventSpecTargetUpdated](events/EventSpecTargetUpdated.md) |
| `store_pricesheet_updated` | [EventStorePricesheetUpdated](events/EventStorePricesheetUpdated.md) |
| `switch_team` | [EventSwitchTeam](events/EventSwitchTeam.md) |
| `team_info` | [EventTeamInfo](events/EventTeamInfo.md) |
| `team_intro_end` | [EventTeamIntroEnd](events/EventTeamIntroEnd.md) |
| `team_intro_start` | [EventTeamIntroStart](events/EventTeamIntroStart.md) |
| `team_score` | [EventTeamScore](events/EventTeamScore.md) |
| `teamchange_pending` | [EventTeamchangePending](events/EventTeamchangePending.md) |
| `tournament_reward` | [EventTournamentReward](events/EventTournamentReward.md) |
| `trial_time_expired` | [EventTrialTimeExpired](events/EventTrialTimeExpired.md) |
| `ugc_file_download_finished` | [EventUgcFileDownloadFinished](events/EventUgcFileDownloadFinished.md) |
| `ugc_file_download_start` | [EventUgcFileDownloadStart](events/EventUgcFileDownloadStart.md) |
| `ugc_map_download_error` | [EventUgcMapDownloadError](events/EventUgcMapDownloadError.md) |
| `ugc_map_info_received` | [EventUgcMapInfoReceived](events/EventUgcMapInfoReceived.md) |
| `ugc_map_unsubscribed` | [EventUgcMapUnsubscribed](events/EventUgcMapUnsubscribed.md) |
| `user_data_downloaded` | [EventUserDataDownloaded](events/EventUserDataDownloaded.md) |

</details>

<details><summary><b>Listeners</b> (28)</summary>

| Challenge type | Page |
|----------------|------|
| `on_client_authorized` | [OnClientAuthorized](events/OnClientAuthorized.md) |
| `on_client_connect` | [OnClientConnect](events/OnClientConnect.md) |
| `on_client_connected` | [OnClientConnected](events/OnClientConnected.md) |
| `on_client_disconnect` | [OnClientDisconnect](events/OnClientDisconnect.md) |
| `on_client_disconnect_post` | [OnClientDisconnectPost](events/OnClientDisconnectPost.md) |
| `on_client_put_in_server` | [OnClientPutInServer](events/OnClientPutInServer.md) |
| `on_client_voice` | [OnClientVoice](events/OnClientVoice.md) |
| `on_custom_hud_clicked` | [OnCustomHudClicked](events/OnCustomHudClicked.md) |
| `on_entity_created` | [OnEntityCreated](events/OnEntityCreated.md) |
| `on_entity_deleted` | [OnEntityDeleted](events/OnEntityDeleted.md) |
| `on_entity_parent_changed` | [OnEntityParentChanged](events/OnEntityParentChanged.md) |
| `on_entity_spawned` | [OnEntitySpawned](events/OnEntitySpawned.md) |
| `on_entity_take_damage_post` | [OnEntityTakeDamagePost](events/OnEntityTakeDamagePost.md) |
| `on_entity_take_damage_pre` | [OnEntityTakeDamagePre](events/OnEntityTakeDamagePre.md) |
| `on_game_server_steam_api_activated` | [OnGameServerSteamAPIActivated](events/OnGameServerSteamAPIActivated.md) |
| `on_game_server_steam_api_deactivated` | [OnGameServerSteamAPIDeactivated](events/OnGameServerSteamAPIDeactivated.md) |
| `on_host_name_changed` | [OnHostNameChanged](events/OnHostNameChanged.md) |
| `on_map_end` | [OnMapEnd](events/OnMapEnd.md) |
| `on_map_start` | [OnMapStart](events/OnMapStart.md) |
| `on_metamod_all_plugins_loaded` | [OnMetamodAllPluginsLoaded](events/OnMetamodAllPluginsLoaded.md) |
| `on_player_buttons_changed` | [OnPlayerButtonsChanged](events/OnPlayerButtonsChanged.md) |
| `on_player_chat` | [OnPlayerChat](events/OnPlayerChat.md) |
| `on_player_take_damage_post` | [OnPlayerTakeDamagePost](events/OnPlayerTakeDamagePost.md) |
| `on_player_take_damage_pre` | [OnPlayerTakeDamagePre](events/OnPlayerTakeDamagePre.md) |
| `on_server_hibernation_update` | [OnServerHibernationUpdate](events/OnServerHibernationUpdate.md) |
| `on_server_pre_fatal_shutdown` | [OnServerPreFatalShutdown](events/OnServerPreFatalShutdown.md) |
| `on_server_precache_resources` | [OnServerPrecacheResources](events/OnServerPrecacheResources.md) |
| `player_chat` | [OnPlayerChat](events/OnPlayerChat.md) |

</details>

<details><summary><b>Other</b> (58)</summary>

| Challenge type | Page |
|----------------|------|
| `add_bullet_hit_marker` | [EventAddBulletHitMarker](events/EventAddBulletHitMarker.md) |
| `bonus_updated` | [EventBonusUpdated](events/EventBonusUpdated.md) |
| `break_breakable` | [EventBreakBreakable](events/EventBreakBreakable.md) |
| `break_prop` | [EventBreakProp](events/EventBreakProp.md) |
| `broken_breakable` | [EventBrokenBreakable](events/EventBrokenBreakable.md) |
| `bullet_damage_given` | [EventBulletDamage](events/EventBulletDamage.md) |
| `bullet_damage_taken` | [EventBulletDamage](events/EventBulletDamage.md) |
| `bullet_impact` | [EventBulletImpact](events/EventBulletImpact.md) |
| `choppers_incoming_warning` | [EventChoppersIncomingWarning](events/EventChoppersIncomingWarning.md) |
| `cs_game_disconnected` | [EventCsGameDisconnected](events/EventCsGameDisconnected.md) |
| `cs_prev_next_spectator` | [EventCsPrevNextSpectator](events/EventCsPrevNextSpectator.md) |
| `door_break` | [EventDoorBreak](events/EventDoorBreak.md) |
| `door_close` | [EventDoorClose](events/EventDoorClose.md) |
| `door_closed` | [EventDoorClosed](events/EventDoorClosed.md) |
| `door_moving` | [EventDoorMoving](events/EventDoorMoving.md) |
| `door_open` | [EventDoorOpen](events/EventDoorOpen.md) |
| `drone_above_roof` | [EventDroneAboveRoof](events/EventDroneAboveRoof.md) |
| `drone_cargo_detached` | [EventDroneCargoDetached](events/EventDroneCargoDetached.md) |
| `drone_dispatched` | [EventDroneDispatched](events/EventDroneDispatched.md) |
| `dronegun_attack` | [EventDronegunAttack](events/EventDronegunAttack.md) |
| `drop_rate_modified` | [EventDropRateModified](events/EventDropRateModified.md) |
| `dynamic_shadow_light_changed` | [EventDynamicShadowLightChanged](events/EventDynamicShadowLightChanged.md) |
| `endmatch_cmm_start_reveal_items` | [EventEndmatchCmmStartRevealItems](events/EventEndmatchCmmStartRevealItems.md) |
| `enter_buyzone` | [EventEnterBuyzone](events/EventEnterBuyzone.md) |
| `entity_visible` | [EventEntityVisible](events/EventEntityVisible.md) |
| `event_ticket_modified` | [EventEventTicketModified](events/EventEventTicketModified.md) |
| `exit_buyzone` | [EventExitBuyzone](events/EventExitBuyzone.md) |
| `finale_start` | [EventFinaleStart](events/EventFinaleStart.md) |
| `firstbombs_incoming_warning` | [EventFirstbombsIncomingWarning](events/EventFirstbombsIncomingWarning.md) |
| `flare_ignite_npc` | [EventFlareIgniteNpc](events/EventFlareIgniteNpc.md) |
| `guardian_wave_restart` | [EventGuardianWaveRestart](events/EventGuardianWaveRestart.md) |
| `hide_deathpanel` | [EventHideDeathpanel](events/EventHideDeathpanel.md) |
| `loot_crate_opened` | [EventLootCrateOpened](events/EventLootCrateOpened.md) |
| `loot_crate_visible` | [EventLootCrateVisible](events/EventLootCrateVisible.md) |
| `material_default_complete` | [EventMaterialDefaultComplete](events/EventMaterialDefaultComplete.md) |
| `mb_input_lock_cancel` | [EventMbInputLockCancel](events/EventMbInputLockCancel.md) |
| `mb_input_lock_success` | [EventMbInputLockSuccess](events/EventMbInputLockSuccess.md) |
| `open_crate_instr` | [EventOpenCrateInstr](events/EventOpenCrateInstr.md) |
| `parachute_deploy` | [EventParachuteDeploy](events/EventParachuteDeploy.md) |
| `parachute_pickup` | [EventParachutePickup](events/EventParachutePickup.md) |
| `physgun_pickup` | [EventPhysgunPickup](events/EventPhysgunPickup.md) |
| `ragdoll_dissolved` | [EventRagdollDissolved](events/EventRagdollDissolved.md) |
| `read_game_titledata` | [EventReadGameTitledata](events/EventReadGameTitledata.md) |
| `repost_xbox_achievements` | [EventRepostXboxAchievements](events/EventRepostXboxAchievements.md) |
| `reset_game_titledata` | [EventResetGameTitledata](events/EventResetGameTitledata.md) |
| `show_deathpanel_killer_controller` | [EventShowDeathpanel](events/EventShowDeathpanel.md) |
| `show_deathpanel_victim` | [EventShowDeathpanel](events/EventShowDeathpanel.md) |
| `show_survival_respawn_status` | [EventShowSurvivalRespawnStatus](events/EventShowSurvivalRespawnStatus.md) |
| `survival_no_respawns_final` | [EventSurvivalNoRespawnsFinal](events/EventSurvivalNoRespawnsFinal.md) |
| `survival_no_respawns_warning` | [EventSurvivalNoRespawnsWarning](events/EventSurvivalNoRespawnsWarning.md) |
| `survival_paradrop_break` | [EventSurvivalParadropBreak](events/EventSurvivalParadropBreak.md) |
| `survival_paradrop_spawn` | [EventSurvivalParadropSpawn](events/EventSurvivalParadropSpawn.md) |
| `survival_teammate_respawn` | [EventSurvivalTeammateRespawn](events/EventSurvivalTeammateRespawn.md) |
| `teamplay_broadcast_audio` | [EventTeamplayBroadcastAudio](events/EventTeamplayBroadcastAudio.md) |
| `update_matchmaking_stats` | [EventUpdateMatchmakingStats](events/EventUpdateMatchmakingStats.md) |
| `weaponhud_selection` | [EventWeaponhudSelection](events/EventWeaponhudSelection.md) |
| `write_game_titledata` | [EventWriteGameTitledata](events/EventWriteGameTitledata.md) |
| `write_profile_data` | [EventWriteProfileData](events/EventWriteProfileData.md) |

</details>

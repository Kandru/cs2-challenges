# CounterstrikeSharp - Challenges

[![UpdateManager Compatible](https://img.shields.io/badge/CS2-UpdateManager-darkgreen)](https://github.com/Kandru/cs2-update-manager/)
[![Discord Support](https://img.shields.io/discord/289448144335536138?label=Discord%20Support&color=darkgreen)](https://discord.gg/bkuF8xKHUt)
[![GitHub release](https://img.shields.io/github/release/Kandru/cs2-challenges?include_prereleases=&sort=semver&color=blue)](https://github.com/Kandru/cs2-challenges/releases/)
[![License](https://img.shields.io/badge/License-GPLv3-blue)](#license)
[![issues - cs2-challenges](https://img.shields.io/github/issues/Kandru/cs2-challenges?color=darkgreen)](https://github.com/Kandru/cs2-challenges/issues)
[![](https://www.paypalobjects.com/en_US/i/btn/btn_donateCC_LG.gif)](https://www.paypal.com/donate/?hosted_button_id=C2AVYKGVP9TRG)

This plugin allow to create Challenges for players. Challenges are tasks a player has to achieve in a given amount of time (e.g. daily, weekly, monthly). Each task can be defined around an event that happens in the game. For example, when a user kills somebody, you can create a Challenge that counts how many times this has happened with a headshot at a minimum distance of 15 meters. If it has happen 3 times, the player has the challenge finished successfully. Another CounterstrikeSharp plugin can then be notified to do something with it. This plugin only provides the interface for further actions after a challenge has been completed. It does not grant special items on its own.

Information: this is currently NOT ready for implementation by other plugins. DO NOT USE right now :)

## Feature Road Map

- [ ] Easy Webinterface to create your own Challenges
- [x] Looking through all game events to decide which to add
- [x] Event PlayerKill
- [x] Event PlayerDeath
- [x] Event PlayerJump
- [x] Event PlayerBlind
- [x] Event PlayerAvengedTeammate
- [x] Event PlayerChangename
- [x] Event PlayerChat
- [X] Event PlayerDecal
- [X] Event PlayerFalldamage
- [X] Event PlayerFootstep
- [X] Event PlayerGivenC4
- [X] Event PlayerHurt
- [X] Event PlayerPing
- [X] Event PlayerRadio
- [X] Event PlayerScore
- [X] Event PlayerSound
- [X] Event PlayerSpawned
- [X] Event PlayerTeam
- [X] Event AchievementEarned
- [X] Event AddPlayerSonarIcon
- [X] Event AmmoPickup
- [X] Event BombAbortdefuse
- [X] Event BombAbortplant
- [X] Event BombBegindefuse
- [X] Event BombBeginplant
- [X] Event BombDefused
- [X] Event BombDropped
- [X] Event BombExploded
- [X] Event BombPickup
- [X] Event BombPlanted
- [X] Event BotTakeover
- [X] Event BreakBreakable
- [X] Event BreakProp
- [X] Event BulletDamage
- [X] Event DefuserPickup
- [X] Event DoorClosed
- [X] Event DoorOpen
- [X] Event EnterBombzone
- [X] Event EnterBuyzone
- [X] Event EnterRescueZone
- [X] Event ExitBombzone
- [X] Event ExitBuyzone
- [X] Event ExitRescueZone
- [X] Event GrenadeBounce
- [X] Event HostageFollows
- [X] Event HostageHurt
- [X] Event HostageKilled
- [X] Event HostageRescued
- [X] Event HostageRescuedAll
- [ ] Event HostageStopsFollowing
- [ ] Event InspectWeapon
- [ ] Event ItemPickup
- [ ] Event ItemPurchase
- [ ] Event TeamScore
- [ ] Event VipEscaped
- [ ] Event VipKilled
- [ ] Event WeaponFireOnEmpty
- [ ] Event WeaponReload
- [ ] Event WeaponZoom
- [ ] Event WeaponZoomRifle
- [ ] Add ability to hide challenge updates on a per challenge basis (e.g. hiding how many times an event needs to occure until finished)
- [ ] Add ability for a cooldown for an event to avoid toggling too fast (e.g. closing / opening doors too fast, dropping bomb 100times in a second)

## Plugin Installation

1. Download and extract the latest release from the [GitHub releases page](https://github.com/Kandru/cs2-challenges/releases/).
2. Move the "Challenges" folder to the `/addons/counterstrikesharp/configs/plugins/` directory of your gameserver.
3. Restart the server.

## Plugin Update

Simply overwrite all plugin files and they will be reloaded automatically or just use the [Update Manager](https://github.com/Kandru/cs2-update-manager/) itself for an easy automatic or manual update by using the *um update Challenges* command.

## Commands

There is currently no client-side command.

## Configuration

This plugin automatically creates a readable JSON configuration file. This configuration file can be found in `/addons/counterstrikesharp/configs/plugins/Challenges/Challenges.json`.

```json
{

}
```

You can either disable or enable the complete Challenges Plugin by simply setting the *enable* boolean to *false* or *true*.

### debug

Shows debug messages useful when developing for this plugin.

## Compile Yourself

Clone the project:

```bash
git clone https://github.com/Kandru/cs2-challenges.git
```

Go to the project directory

```bash
  cd cs2-challenges
```

Install dependencies

```bash
  dotnet restore
```

Build debug files (to use on a development game server)

```bash
  dotnet build
```

Build release files (to use on a production game server)

```bash
  dotnet publish
```

## License

Released under [GPLv3](/LICENSE) by [@Kandru](https://github.com/Kandru).

## Authors

- [@derkalle4](https://www.github.com/derkalle4)

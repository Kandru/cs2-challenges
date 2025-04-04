# CounterstrikeSharp - Challenges

[![Discord Support](https://img.shields.io/discord/289448144335536138?label=Discord%20Support&color=darkgreen)](https://discord.gg/NtHCk5PWEt)
[![GitHub release](https://img.shields.io/github/release/Kandru/cs2-challenges?include_prereleases=&sort=semver&color=blue)](https://github.com/Kandru/cs2-challenges/releases/)
[![License](https://img.shields.io/badge/License-GPLv3-blue)](#license)
[![issues - cs2-challenges](https://img.shields.io/github/issues/Kandru/cs2-challenges?color=darkgreen)](https://github.com/Kandru/cs2-challenges/issues)
[![](https://www.paypalobjects.com/en_US/i/btn/btn_donateCC_LG.gif)](https://www.paypal.com/donate/?hosted_button_id=C2AVYKGVP9TRG)

This plugin allows you to create Challenges for players. Challenges are tasks that players need to complete within a certain time frame (e.g., daily, weekly, monthly). Each task is based on an event in the game. For example, you can create a Challenge that tracks how many times a player kills an opponent with a headshot from at least 15 meters away. If the player achieves this 3 times, the Challenge is completed.

Other CounterstrikeSharp plugins will then be notified to take further actions. This plugin only provides the interface for tracking and completing Challenges; it does not grant rewards on its own. So make sure to ask your favorite plug-in developer to build an integration for you.

> [!TIP]
> Please consider a [Donation](https://www.paypal.com/donate/?hosted_button_id=C2AVYKGVP9TRG) when you're using this plugin. It took me 40+ hours of development time to get to this state so far.

## Documentation

> [!IMPORTANT]  
> Create an Github Issue for Bugs, Features and Improvements! Use our Discord Support-Channel for everything else!

- [How to Start](./documentation/howto.md)
- [Third-Party Plugin Integration](./documentation/plugin-integration.md)
- [Blueprints](./documentation/blueprints.md)
- [Events](./documentation/events.md)
- [Rules](./documentation/rules.md)
- [Actions](./documentation/actions.md)
- [Schedules](./documentation/schedules.md)

## Features

- Create custom challenges for your players.
- Track challenge progress and completion, and send this data to other plugins.
- Set challenges to depend on each other (e.g., complete one challenge to unlock another).
- Define rules that can reset a challenge (e.g., make 10 headshots in a row, but reset if only 9 or less headshots are made).
- Challenges GUI which can be opened with *!c* or *!challenges*
- Player language chosen with *!lang en* gets saved and load again upon re-joining (no more setting !lang every map <3)

## Road Map

- [ ] Easy Webinterface to create your own Challenges
- [ ] Add ability to spawn custom props on the map as a challenge
- [ ] Link possible values for all rules in documentation
- [ ] Discord integration
  - [X] Webhook for Challenge completion
  - [X] Webhook for notification of a new Schedule starting (with all challenges listed)
  - [ ] Webhook for sending statistics at a given interval about all players (like a top list)

## Compatible plugins

- [CS2 Player Sessions (simple player sessions plugin)](https://github.com/kandru/cs2-player-sessions)

# Servers to test this Challenges-Plugin on

- CounterStrike.Party Ballerbude (connect via https://counterstrike.party or server.counterstrike.party:27030)

## Plugin Installation

1. Download and extract the latest release from the [GitHub releases page](https://github.com/Kandru/cs2-challenges/releases/).
2. Move the "Challenges" folder to the `/addons/counterstrikesharp/configs/plugins/` directory of your gameserver.
3. Move the "ChallengesShared" folder to the `/addons/counterstrikesharp/configs/shared/` directory of your gameserver.
4. Restart the server.

## Plugin Update

Simply overwrite all plugin files and they will be reloaded automatically.

## Commands

### !challenges / !c

This activates the challenges GUI when the player is alive. The state of the popup is saved as a user preference.

### !sendtestchallengeevent

This needs permission *@css/root* to work. Sends the first found Challenge for the current user to all plugins. Useful for testing purposes when creating a custom listener in your own plugin for this Challenge-Plugin.

## Configuration

This plugin automatically creates a readable JSON configuration file. This configuration file can be found in `/addons/counterstrikesharp/configs/plugins/Challenges/Challenges.json`.

```json
{
  "enabled": true,
  "debug": false,
  "gui": {
    "show_on_round_start": true,
    "on_round_start_duration": 3,
    "show_after_respawn": true,
    "after_respawn_duration": 5,
    "show_on_challenge_update": true,
    "on_challenge_update_duration": 5,
    "menu_display_maximum": 4,
    "menu_font_size": 28,
    "menu_font_name": "Arial Black Standard",
    "menu_font_color": "#ffffff",
    "menu_pos_x": 3.6,
    "menu_pos_y": 4,
    "menu_background": true,
    "menu_backgroundfactor": 1
  },
  "notifications": {
    "notify_player_on_challenge_progress": true,
    "notify_player_on_challenge_complete": true,
    "notify_other_on_challenge_complete": true,
    "notification_sound_on_challenge_progress": "",
    "notification_sound_on_challenge_complete": "sounds/ui/xp_levelup.vsnd",
    "notification_sound_on_action_rule_broken": "sounds/ui/xp_rankdown_02.vsnd"
  },
  "discord": {
    "language": "de",
    "webhook_on_challenge_completed": ""
  },
  "ConfigVersion": 1
}
```

### enable 

You can either disable or enable the complete Challenges Plugin by simply setting the *enable* boolean to *false* or *true*.

### debug

Shows debug messages useful when developing for this plugin.

### gui

Settings for the position of the *!c* challenge menu.

### notifications

Notification settings. To disable sounds simply remove the sound file from the setting. Sounds can either be a sound file path (e.g. sounds/test/test.vsnd) or a soundevents_addon.vsndevts name (e.g. MySound.Command). Please note: sound file paths will be played at 100% volume regardless of the player's volume choices. This cannot be changed. Only sound events do support changing the volume via the soundevents_addon.vsndevts file.

### discord

Discord Webhook settings. Simply change the language to your desired language. Will fallback to server default (if found) or to the first language available of a challenge.

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

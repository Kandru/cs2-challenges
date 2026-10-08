> [!WARNING]  
> You need to configure challenges! This plug-in comes with examples. READ THE WIKI before asking!

# CounterstrikeSharp - Challenges

[![Discord Support](https://img.shields.io/discord/289448144335536138?label=Discord%20Support&color=darkgreen)](https://discord.gg/NtHCk5PWEt)
[![GitHub release](https://img.shields.io/github/release/Kandru/cs2-challenges?include_prereleases=&sort=semver&color=blue)](https://github.com/Kandru/cs2-challenges/releases/)
[![License](https://img.shields.io/badge/License-GPLv3-blue)](#license)
[![issues - cs2-challenges](https://img.shields.io/github/issues/Kandru/cs2-challenges?color=darkgreen)](https://github.com/Kandru/cs2-challenges/issues)
[![](https://www.paypalobjects.com/en_US/i/btn/btn_donateCC_LG.gif)](https://www.paypal.com/donate/?hosted_button_id=C2AVYKGVP9TRG)

![Challenges menu](media/challenges_menu.png)

Create time-limited challenges for players. Each challenge is a YAML file with tasks that listen for game events (for example three headshots in a row). This plugin tracks progress and tells other plugins when a task or challenge is done. It does not grant rewards on its own. Each challenge consists of at least one task player has to do. You can also define rules which reset a task (for example when somebody did not do a headshot for his three headshots in a row).

> [!TIP]
> Please consider a [donation](https://www.paypal.com/donate/?hosted_button_id=C2AVYKGVP9TRG) when you're using this plugin - it took a long time to build and improve - it only costs you a few dollars and would help me very much!

## Documentation

> [!IMPORTANT]
> Create a GitHub Issue for bugs, features, and improvements. Use Discord for everything else.

Read in this order:

1. [How to start](./documentation/howto.md)
2. [Schedules](./documentation/schedules.md)
3. [Blueprints](./documentation/blueprints.md)
4. [Rules](./documentation/rules.md)
5. [Actions](./documentation/actions.md)
6. [Events](./documentation/events.md)
7. [Settings](./documentation/settings.md)
8. [Third-party plugin integration](./documentation/plugin-integration.md) (plugin authors)

## Features

- One YAML file per challenge, with ordered tasks and `requires` dependencies inside the file.
- Track progress and notify other plugins via `ChallengesShared`.
- Panorama HUD: round-start tracker and fullscreen `!c` / `!challenges` menu ([Workshop](https://steamcommunity.com/sharedfiles/filedetails/?id=3814533794)).
- Player language from `!lang` is stored and restored on reconnect to properly translate the whole plug-in.
- Build with Docker (`make debug` / `make release`) — no local .NET install required.
- Challenge builder for GitHub Pages under [`builder/`](./builder/) ([live site](https://kandru.github.io/cs2-challenges/)).

## Compatible plugins

- if your plug-in (e.g. cs2 stores) does provide server commands (e.g. givemoney) you can use placeholders to send commands to the server upon task completion
- if you have a plug-in which is compatible please tell me!

## Test servers

- CounterStrike.Party Ballerbude ([counterstrike.party](https://counterstrike.party) or `server.counterstrike.party:27030`)
- CounterStrike.Party Wingman ([counterstrike.party](https://counterstrike.party) or `server.counterstrike.party:27020`)

## Road map

- [X] Web builder to create challenges ([builder/](./builder/))
- [ ] Spawn custom props on the map as a challenge
- [ ] Link possible values for all rules in documentation
- [ ] Discord integration
  - [X] Webhook for challenge completion
  - [X] Webhook when a new schedule starts
  - [ ] Webhook for player statistics at an interval

## License

Released under [GPLv3](/LICENSE) by [@Kandru](https://github.com/Kandru).

## Authors

- [@derkalle4](https://www.github.com/derkalle4)

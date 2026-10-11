# How to start

This page takes you from a clean server to a working challenge set. Field tables and HUD details live on the linked pages.

Config folder (created on first start):

`/game/csgo/addons/counterstrikesharp/configs/plugins/Challenges/`

## 1. Install

1. Install [Metamod:Source](https://www.metamodsource.net/downloads.php?branch=dev).
2. Install [MultiAddonManager](https://github.com/Source2ZE/MultiAddonManager/releases) and configure it to load workshop item [3814533794](https://steamcommunity.com/sharedfiles/filedetails/?id=3814533794) (Panorama HUD).
3. Install [CounterStrikeSharp](https://docs.cssharp.dev/) and configure it (admins, and so on).
4. Download the latest release from [GitHub Releases](https://github.com/Kandru/cs2-challenges/releases/).
5. Copy the `Challenges` folder into `/game/csgo/addons/counterstrikesharp/plugins/`.
6. Copy the `ChallengesShared` folder into `/game/csgo/addons/counterstrikesharp/shared/`.
7. Restart the server once so it creates the config folder.

To update later: stop the server, overwrite those folders, start again.

## 2. Load the examples

With the server stopped:

1. Copy `examples/schedules.yaml` into the Challenges config folder.
2. Copy the `examples/blueprints/` folder into that same config folder (so you have `…/Challenges/blueprints/*.yaml`).
3. Open `schedules.yaml` and set `date_start` / `date_end` so **today** falls inside the window. Times are **UTC**.

See [Schedules](schedules.md) for every field.

## 3. Start and check in game

1. Start the server (or change map). Files also reload with the server console command `challenges reload`.
2. During freeze time (`mp_freezetime` above 0), a tracker appears top-right for players on Terrorist or Counter-Terrorist.
3. Type `!c` or `!challenges` for the fullscreen menu (defaults; change them in [Settings](settings.md)).

What the tracker, menu, scoreboard, language, and saved progress do is in [In game](ingame.md).

## Using the builder

The [challenge builder](https://kandru.github.io/cs2-challenges/) writes **one blueprint file**. After you export:

1. Copy the `.yaml` into `…/Challenges/blueprints/`.
2. Add that filename (without `.yaml`) under `challenges:` in [schedules.yaml](schedules.md).
3. Change map or run `challenges reload`.

Each `?` in the builder opens the matching wiki page.

## If nothing shows

1. Confirm the schedule dates include now (UTC).
2. Confirm every id under `challenges:` matches a blueprint filename without `.yaml`.
3. Check the CounterStrikeSharp log for YAML errors.
4. Set `"development": { "enable": true }` in `Challenges.json` and change map again (see [Settings](settings.md)).
5. Confirm MultiAddonManager loads workshop item `3814533794`.
6. Confirm `mp_freezetime` is above 0 if you expect the freeze-time tracker.

## Rewards

This plugin only tracks progress. To grant money, credits, or items when a task or challenge finishes, use another plugin’s server console command or a C# listener - see [Plugin integration](plugin-integration.md).

## Next steps

1. [In game](ingame.md) - tracker, menu, language, saved progress
2. [Schedules](schedules.md) - when challenges are active
3. [Blueprints](blueprints.md) - how one challenge file works
4. [Settings](settings.md) - `Challenges.json`, chat and server commands, Discord

Stuck after that? Ask in [Discord](https://discord.gg/NtHCk5PWEt). Ready-made files live in `examples/`.

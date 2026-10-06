# How to start

Config folder (created on first start):

`/game/csgo/addons/counterstrikesharp/configs/plugins/Challenges/`

## 1. Install

1. Install [CounterStrikeSharp](https://docs.cssharp.dev/) on your CS2 server.
2. Download the latest release from [GitHub Releases](https://github.com/Kandru/cs2-challenges/releases/).
3. Copy the `Challenges` folder into `/game/csgo/addons/counterstrikesharp/plugins/`.
4. Copy the `ChallengesShared` folder into `/game/csgo/addons/counterstrikesharp/shared/`.
5. Restart the server once so it creates the config folder.

To update later: stop the server, overwrite those folders, start again.

## 2. Load the examples

With the server stopped:

1. Copy `examples/schedules.yaml` into the Challenges config folder.
2. Copy the `examples/blueprints/` folder into that same config folder (so you have `…/Challenges/blueprints/*.yaml`).
3. Open `schedules.yaml` and set `date_start` / `date_end` so **today** falls inside the window. Times are **UTC**.
4. Mount the Panorama addon from [`workshop/content`](../workshop/content) on the server so the HUD and `!c` menu can load.

## 3. Start and check in game

1. Start the server (or change map). Files also reload with the server command `challenges reload`.
2. During freeze time, a tracker appears top-right.
3. Type `!c` or `!challenges` for the fullscreen menu.

> [!NOTE]
> Prefer writing challenges in the browser? Use the [challenge builder](https://kandru.github.io/cs2-challenges/).

<a id="using-the-builder"></a>

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
4. Set `"debug": true` in `Challenges.json` and change map again.

## What this plugin does not do

It only tracks progress. Rewards need another plugin that listens for completions — see [Third-party plugin integration](plugin-integration.md).

## Next steps

1. [Schedules](schedules.md) — when challenges are active
2. [Blueprints](blueprints.md) — how one challenge file works
3. [Settings](settings.md) — config, chat commands, Discord

Stuck after that? Ask in [Discord](https://discord.gg/NtHCk5PWEt). Ready-made files live in `examples/`.

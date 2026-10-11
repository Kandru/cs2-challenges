# In game

What players see and what the plugin stores. Config knobs that change this behavior are in [Settings](settings.md).

## Tracker

During freeze time the plugin can show a small card (top-right):

- Only when `mp_freezetime` is above 0.
- Only for players on Terrorist or Counter-Terrorist.
- Controlled by `gui.show_on_round_start` (default on).

When a visible task advances, a progress tracker can appear for `gui.progress_duration` seconds (minimum 1). Row count is `gui.tracker_rows`, clamped to 1–5. Controlled by `gui.show_on_progress` (default on).

The tracker hint uses `command_prefix` plus the shortest name in `menu_commands` (default `!c`). An empty `menu_commands` list disables chat menu commands and hides that hint.

## Menu

Chat names from `menu_commands` with `command_prefix` (default `!c` / `!challenges`) toggle the fullscreen menu.

Filters:

| Filter | Contents |
|--------|----------|
| All | Loaded challenges (current or upcoming schedules) |
| Progress | Unsolved challenges in the **active** schedule |
| Solved | Solved challenges in the **active** schedule |
| Ending soon | Challenges whose schedule ends within 7 days |
| Starting soon | Challenges whose schedule starts in the future |

Scoreboard:

- Filter all players or online only.
- Sort by solved in this schedule, or by lifetime solved count.
- Click a name to view that player’s challenges (list title becomes “Challenges of …”).

Challenge and task titles use the player’s language (`!lang`), then the two-letter code, then the **first language** in the YAML map. Schedule titles are not shown in the menu or tracker - see [Schedules](schedules.md).

## Language

Chat command `!lang <culture>` (for example `!lang en`, `!lang de`) always uses a leading `!`. It does not use `command_prefix`.

The language must be a valid culture name. It is stored in the player file and applied again on reconnect so titles and HUD text match.

## Saved progress

Path: `/addons/counterstrikesharp/configs/plugins/Challenges/players/<steamid>.json`

Progress is keyed by the active schedule’s progress key (see [Schedules](schedules.md)). When that key changes, task progress for the old key is removed from the file. The lifetime solved count is kept.

Who can store progress:

- Humans with a real Steam id.
- Not bots, HLTV, empty ids, `0`, or `BOT`.

`development.allow_bots` does **not** let bots earn progress. It only skips rules of the form `*.isbot` with operator `bool==` and value `false`, so a human can still get credit when the victim or attacker is a bot.

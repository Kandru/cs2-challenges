# Settings

## Config file

Path: `/addons/counterstrikesharp/configs/plugins/Challenges/Challenges.json`

Created on first start. Example:

```json
{
  "enabled": true,
  "development": {
    "enable": false,
    "allow_bots": false
  },
  "menu_commands": ["c", "challenges"],
  "command_prefix": "!",
  "gui": {
    "show_on_round_start": true,
    "show_on_progress": true,
    "progress_duration": 5,
    "tracker_rows": 3,
    "theme": "gold"
  },
  "notifications": {
    "notify_player_on_challenge_progress": true,
    "notify_player_on_challenge_complete": true,
    "notify_other_on_challenge_complete": true,
    "notification_sound_on_challenge_progress": "",
    "notification_sound_on_challenge_complete": "sounds/ui/achievement_earned.vsnd",
    "notification_sound_on_action_rule_broken": "sounds/ui/xp_rankdown_02.vsnd"
  },
  "discord": {
    "language": "en",
    "timezone": "Europe/Berlin",
    "webhook_url": "",
    "on_task_completed": false,
    "on_challenge_completed": true,
    "on_new_schedule": true,
    "task_list": "full"
  },
  "persistence": {
    "autosave_on_round_end": true,
    "autosave_per_tick": 2
  },
  "temp_data": {
    "current_schedule_key": ""
  },
  "ConfigVersion": 1
}
```

| Setting | Meaning |
|---------|---------|
| `enabled` | Turns the whole plugin on or off. |
| `development.enable` | Extra log messages (YAML / matching hints). |
| `development.allow_bots` | Skip `*.isbot` rules that are `bool==` `"false"` so bot victims/attackers still count for **humans**. Default `false`. Does not let bots earn progress. |
| `menu_commands` | Chat names that open the challenges menu (default `["c", "challenges"]`). Leading `!` / `/` / `.` optional. Empty list disables chat commands and hides the tracker hint. |
| `command_prefix` | Chat trigger shown on the tracker before the shortest menu command (default `"!"`). Does not apply to `!lang`. |
| `gui.show_on_round_start` | Show the tracker during freeze time. |
| `gui.show_on_progress` | Show the tracker when a visible task advances. |
| `gui.progress_duration` | Seconds the progress tracker stays up (minimum 1). |
| `gui.tracker_rows` | Rows on the tracker (clamped 1–5). |
| `gui.theme` | Accent color: `red`, `coral`, `orange`, `copper`, `gold`, `t`, `lime`, `green`, `teal`, `cyan`, `blue`, `ct`, `purple`, `magenta`, `pink`, `silver`. Unknown names become `gold`. |
| `notifications.*` | Chat and sound on progress / complete / rule broken. Empty sound string = no sound. A value starting with `sounds/` is a client-only `play`. Any other value is a sound event (`EmitSound`). With `notify_other_on_challenge_complete`, several completions from one progress update reach the other players as a single message. |
| `discord.language` | Language for Discord embed titles, field labels, and dates (`en`, `de`, …). Challenge/task names use the matching title from the challenge file. |
| `discord.timezone` | Timezone for schedule start/end (IANA/Windows id, e.g. `Europe/Berlin`). Empty = server local. |
| `discord.webhook_url` | Discord webhook URL. Empty disables all Discord posts. Posts are sent one at a time from a queue (up to 100 pending; the oldest is dropped when full). Rate limits (HTTP 429) are retried after Discord's `retry_after`, up to 3 tries. The queue keeps sending across map changes; on plugin unload, posts still pending are dropped. |
| `discord.on_task_completed` | Post when a visible task is completed (default `false`). Skipped when a challenge-complete post is sent for the same finish. Challenges with only one visible task never get a task card — only the challenge embed. Task posts also require the task’s `announce_completion`. |
| `discord.on_challenge_completed` | Post when every visible task of a challenge is completed (default `true`). |
| `discord.on_new_schedule` | Post when a new schedule becomes active (default `true`). |
| `discord.task_list` | Task-completion body: `full` (challenge name + all-tasks checklist), `short` (only the finished task line), or `none` (no task embeds). Default `full`. Ignored for single-task challenges. |
| `persistence.autosave_on_round_end` | Save players with unsaved progress at the end of every round (default `true`), so a server crash loses at most the current round. `false` saves only on disconnect, map change, and unload. |
| `persistence.autosave_per_tick` | Maximum number of player files prepared per server tick while saves are queued (default `2`, minimum `1`). Files are written in the background; map change and unload still write everything before continuing. |
| `temp_data.current_schedule_key` | Written by the plugin when the active schedule’s progress key changes. Do not edit by hand. |

Tracker, menu, and language behavior: [In game](../tutorial/in-game.md).

## Chat commands

| Command | Who | Effect |
|---------|-----|--------|
| Names in `menu_commands` with `command_prefix` (default `!c` / `!challenges`) | Players | Toggle the fullscreen challenges menu. |
| `!lang <language>` | Players | Set language (e.g. `!lang en`, `!lang de`). Always `!`, not `command_prefix`. Stored and restored on reconnect. |

## Server console

| Command | Effect |
|---------|--------|
| `challenges reload` | Reload config, schedules, and current/upcoming challenge files from `blueprints/`; rebuild the running schedule. |
| `challenges disable` | Disable the plugin and remember that. |
| `challenges enable` | Enable the plugin and remember that. |
| `challenges test-discord` | Post sample Discord embeds (schedule, challenge, task full/short) to `discord.webhook_url` using the loaded schedule/challenges. Fails if the URL or data is missing. |
| `challenges check-catalog` | Compare `catalog.json` to the loaded extractors. Reports mismatches. |

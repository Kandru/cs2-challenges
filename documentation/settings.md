# Settings

## Config file

Path: `/addons/counterstrikesharp/configs/plugins/Challenges/Challenges.json`

Created on first start. Example:

```json
{
  "enabled": true,
  "debug": {
    "enable": false,
    "allow_bots": false
  },
  "menu_commands": ["c", "challenges"],
  "command_prefix": "!",
  "gui": {
    "show_on_round_start": true,
    "show_on_progress": true,
    "progress_duration": 5,
    "tracker_rows": 3
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
    "language": "en",
    "timezone": "Europe/Berlin",
    "webhook_url": "",
    "on_task_completed": false,
    "on_challenge_completed": true,
    "on_new_schedule": true,
    "task_list": "full"
  },
  "ConfigVersion": 1
}
```

| Setting | Meaning |
|---------|---------|
| `enabled` | Turns the whole plugin on or off. |
| `debug.enable` | Extra log messages (YAML / matching hints). |
| `debug.allow_bots` | Ignore `*.isbot` == false rules so bot victims/attackers still count. Default `false`. |
| `menu_commands` | Chat names that open the challenges menu (default `["c", "challenges"]`). Leading `!` / `/` / `.` optional. Empty list disables chat commands and hides the tracker hint. |
| `command_prefix` | Chat trigger shown on the tracker before the shortest menu command (default `"!"`). |
| `gui.show_on_round_start` | Show the tracker during freeze time. |
| `gui.show_on_progress` | Show the tracker when a visible task advances. |
| `gui.progress_duration` | Seconds the progress tracker stays up. |
| `gui.tracker_rows` | Rows on the tracker GUI (clamped 1–5). |
| `notifications.*` | Chat and sound on progress / complete / rule broken. Empty sound string = no sound. Sound paths play at full volume; soundevent names respect player volume. |
| `discord.language` | Language for Discord embed titles, field labels, and dates (`en`, `de`, …). Challenge/task names use the matching title from the blueprint. |
| `discord.timezone` | Timezone for schedule start/end (IANA/Windows id, e.g. `Europe/Berlin`). Empty = server local. Shown as `UTC`, `UTC+1`, … after a culture-formatted date (`discord.language`). |
| `discord.webhook_url` | Discord webhook URL. Empty disables all Discord posts. |
| `discord.on_task_completed` | Post an embed when a visible task is completed (default `false`). Skipped when a challenge-complete post is sent for the same finish. Challenges with only one visible task never get a task card — only the challenge embed. |
| `discord.on_challenge_completed` | Post an embed when every visible task of a challenge is completed (default `true`). |
| `discord.on_new_schedule` | Post an embed when a new schedule becomes active (default `true`). |
| `discord.task_list` | Task-completion body: `full` (challenge name + all-tasks checklist with counts), `short` (only `✅ count/total` for the finished task), or `none` (no task embeds). Default `full`. Ignored for single-task challenges. |

## Chat commands

| Command | Who | Effect |
|---------|-----|--------|
| Names in `menu_commands` with `command_prefix` (default `!c` / `!challenges`) | Players | Toggle the fullscreen challenges menu. |
| `!lang <language>` | Players | Set language (e.g. `!lang en`, `!lang de`). Stored and restored on reconnect. |

## Server console

| Command | Effect |
|---------|--------|
| `challenges reload` | Reload config, blueprints, and schedules. |
| `challenges disable` | Disable the plugin and remember that. |
| `challenges enable` | Enable the plugin and remember that. |
| `challenges test-discord` | Post sample Discord embeds (schedule, challenge, task full/short) to `discord.webhook_url` using the loaded schedule/challenges. Fails if the URL or data is missing. |

## Build from source

Needs Docker. From the repo root:

```bash
make debug    # development build
make release  # production build
```

No local .NET SDK required.

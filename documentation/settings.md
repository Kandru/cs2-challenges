# Settings

## Config file

Path: `/addons/counterstrikesharp/configs/plugins/Challenges/Challenges.json`

Created on first start. Example:

```json
{
  "enabled": true,
  "debug": false,
  "allow_bots": false,
  "gui": {
    "show_on_round_start": true,
    "show_on_progress": true,
    "progress_duration": 5,
    "tracker_rows": 3,
    "menu_page_size": 4
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
    "webhook_on_challenge_completed": "",
    "webhook_on_new_schedule": ""
  },
  "ConfigVersion": 1
}
```

| Setting | Meaning |
|---------|---------|
| `enabled` | Turns the whole plugin on or off. |
| `debug` | Extra log messages (YAML / matching hints). |
| `allow_bots` | Let bots earn challenges (default `false`). |
| `gui.show_on_round_start` | Show the tracker during freeze time. |
| `gui.show_on_progress` | Show the tracker when a visible task advances. |
| `gui.progress_duration` | Seconds the progress tracker stays up. |
| `gui.tracker_rows` | Rows on the tracker (clamped 3–5). |
| `gui.menu_page_size` | Challenges per menu page (clamped 1–4). |
| `notifications.*` | Chat and sound on progress / complete / rule broken. Empty sound string = no sound. Sound paths play at full volume; soundevent names respect player volume. |
| `discord.language` | Language for Discord messages. |
| `discord.webhook_on_challenge_completed` | Webhook URL when a challenge is completed. |
| `discord.webhook_on_new_schedule` | Webhook URL when a new schedule becomes active. |

## Chat commands

| Command | Who | Effect |
|---------|-----|--------|
| `!c` / `!challenges` | Players | Toggle the fullscreen challenges menu. |
| `!lang <language>` | Players | Set language (e.g. `!lang en`, `!lang de`). Stored and restored on reconnect. |

## Server console

| Command | Effect |
|---------|--------|
| `challenges reload` | Reload config, blueprints, and schedules. |
| `challenges disable` | Disable the plugin and remember that. |
| `challenges enable` | Enable the plugin and remember that. |

## Build from source

Needs Docker. From the repo root:

```bash
make debug    # development build
make release  # production build
```

No local .NET SDK required.

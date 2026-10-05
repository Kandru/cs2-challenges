# Blueprints

One YAML file = one challenge. The **filename without `.yaml`** is the challenge id. Schedules list that id.

Put files in:

`/addons/counterstrikesharp/configs/plugins/Challenges/blueprints/`

## Minimal example

```yaml
title:
  en: Headshots in a row
  de: Kopfschüsse hintereinander
tasks:
  - id: easy
    title:
      en: "{count}/{total} headshots"
      de: "{count}/{total} Kopfschüsse"
    type: player_kill
    amount: 3
    rules:
      - key: global.iswarmup
        operator: bool==
        value: "false"
      - key: headshot
        operator: bool==
        value: "true"
    requires: []
  - id: medium
    title:
      en: "{count}/{total} headshots"
    type: player_kill
    amount: 10
    rules:
      - key: headshot
        operator: bool==
        value: "true"
    requires:
      - easy
```

Full patterns (streaks, resets, rewards data) are in `examples/blueprints/`. Or use the [builder](https://kandru.github.io/cs2-challenges/).

## Fields

### Challenge

| Field | Meaning |
|-------|---------|
| `title` | Name in the HUD. Uses the player’s language (`!lang`), then the two-letter code, then the first language in the file. |
| `data` | Optional payload sent to other plugins when the **whole challenge** is done (every visible task finished). |
| `tasks` | Ordered list of tasks. |

### Task

| Field | Meaning |
|-------|---------|
| `id` | Unique inside this file. |
| `title` | Language map. Supports `{count}` and `{total}`. |
| `type` | Game event type — see [Events](events.md). |
| `amount` | How many matching events are needed. |
| `cooldown` | Seconds before this task can count again (default `0`). |
| `visible` | Show in HUD and count toward “solved” (default `true`). |
| `announce_progress` / `announce_completion` | Chat notifications (default `true`). |
| `data` | Optional payload on this task’s progress/completion events. |
| `rules` | Conditions on the event — see [Rules](rules.md). |
| `actions` | Side effects when the task completes — see [Actions](actions.md). |
| `requires` | Other task ids in this file that must be finished first. Empty = available immediately. Several ids = all must be done (`AND`). |

## How tasks unlock

- Tasks with empty `requires` can run in parallel.
- A challenge is **done** when every `visible: true` task is done.
- Hidden tasks (`visible: false`) are control steps. They stay off the HUD and do not count toward the solved total.
- Finishing a task does **not** also credit the next task on the same event. List stages from first unlockable task at the top to last at the bottom.

## Next

- [Rules](rules.md) — when an event counts
- [Actions](actions.md) — what happens when a task finishes
- [Events](events.md) — which `type` to use

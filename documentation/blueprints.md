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

Full patterns (streaks, resets, rewards) are in `examples/blueprints/`. Or use the [builder](https://kandru.github.io/cs2-challenges/).

## Challenge fields

| Field | Meaning |
|-------|---------|
| id | Snake_case filename without `.yaml`. Must match the entry in `schedules.yaml`. |
| `title` | Name in the HUD. At least one language is required. Fallback: player language → two-letter code → first language in the map. |
| `data` | Optional payload for other plugins when the **whole challenge** is solved. See [Plugin integration](plugin-integration.md). |
| `tasks` | Ordered list of tasks. Put the first unlockable task at the top. |

## Task fields

| Field | Meaning |
|-------|---------|
| `id` | Unique inside this file. |
| `title` | Language map. Supports `{count}` and `{total}`. If empty, the challenge title is used. Same language fallback as the challenge title. |
| `type` | Game event type - see [Events](events.md). |
| `amount` | How many matching events are needed. Values below 1 are treated as 1. |
| `cooldown` | Minimum seconds between counted events for this task (default `0` = every match can count). |
| `visible` | Show in HUD and count toward “solved” (default `true`). |
| `announce_progress` / `announce_completion` | Per-task chat (and Discord completion) switches (default `true`). Global switches are in [Settings](settings.md). |
| `data` | Optional payload on this task’s progress/completion events. See [Plugin integration](plugin-integration.md). |
| `rules` | Conditions on the event - see [Rules](rules.md). Every rule must pass (`AND`). |
| `actions` | Side effects when the task reaches its amount - see [Actions](actions.md). |
| `requires` | Other task ids in this file that must be finished first. Empty = available immediately. Several ids = all must be done (`AND`). |

## How tasks unlock

- Tasks with empty `requires` can run in parallel.
- A challenge is **solved** when every `visible: true` task is done.
- Hidden tasks (`visible: false`) are control steps. They stay off the HUD and do not count toward the solved total.
- Finishing a task does **not** also credit the next task on the same event. List stages from first unlockable task at the top to last at the bottom.

## Next

- [Rules](rules.md) - when an event counts
- [Actions](actions.md) - what happens when a task finishes
- [Events](events.md) - which `type` to use

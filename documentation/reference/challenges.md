# Challenges and tasks {#challenge}

One YAML file is one **challenge**. The **filename without `.yaml`** is the challenge id. Schedules list that id.

Put files in:

`/addons/counterstrikesharp/configs/plugins/Challenges/blueprints/`

Only files named by a current or upcoming schedule are loaded. See [Schedules](../howto/schedules.md).

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

Full patterns (streaks, resets, rewards) are in `examples/blueprints/`. Or use the [builder](https://kandru.github.io/cs2-challenges/builder/).

## Challenge fields

### Id {#id}

Snake_case filename without `.yaml`. Must match the entry in `schedules.yaml`.

### Title {#title}

Name in the HUD. At least one language is required. Fallback: player language → two-letter code → first language in the map.

### Data

Optional payload for other plugins when the **whole challenge** is solved. See [Challenge data](../howto/rewards.md#challenge-data).

### Tasks

Ordered list of tasks. Put the first unlockable task at the top.

## Task fields

### Task id {#task-id}

Unique inside this file.

### Task title {#task-title}

Language map. Supports `{count}` and `{total}`. If empty, the challenge title is used. Same language fallback as the challenge title.

### Type {#type}

Game event or listener type — see [Events](events.md) and [Listeners](listeners.md). Put one value in the task `type:` field.

### Amount {#amount}

How many matching events are needed. Values below 1 are treated as 1.

### Cooldown {#cooldown}

Minimum seconds between counted events for this task (default `0` = every match can count).

### Visible {#visible}

Show in HUD and count toward “solved” (default `true`).

### Announce {#announce}

`announce_progress` / `announce_completion` — per-task chat (and Discord completion) switches (default `true`). Global switches are in [Settings](settings.md).

### Data

Optional payload on this task’s progress/completion events. See [Task data](../howto/rewards.md#task-data).

### Rules

Conditions on the event — see [Rules](rules.md). Every rule must pass (`AND`).

### Actions

Side effects when the task reaches its amount — see [Actions](actions.md).

### Requires {#requires}

Other task ids in this file that must be finished first. Empty = available immediately. Several ids = all must be done (`AND`).

## How tasks unlock {#how-tasks-unlock}

- Tasks with empty `requires` can run in parallel.
- A challenge is **solved** when every `visible: true` task is done.
- Hidden tasks (`visible: false`) are control steps. They stay off the HUD and do not count toward the solved total.
- Finishing a task does **not** also credit the next task on the same event. List stages from first unlockable task at the top to last at the bottom.

## See also

- [Rules](rules.md) — when an event counts
- [Actions](actions.md) — what happens when a task finishes
- [Events](events.md) / [Listeners](listeners.md) — which `type` to use

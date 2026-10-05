# Blueprints Documentation

Blueprints are YAML files that define one challenge each. A challenge is self-contained: titles live in the file (not in plugin `lang/`), and work is split into ordered **tasks**.

## Creating a Challenge File

1. Create a `.yaml` file (e.g. `headshots_in_a_row.yaml`).
2. Place it in the `blueprints` folder of the Challenges plugin config directory.
3. The challenge id is the **filename without extension** (e.g. `headshots_in_a_row`). Schedules reference that id.

```yaml
title:
  en: Headshots in a row
  de: Kopfschüsse hintereinander
data:
  PlayerSessions:
    setpoints: "10"
tasks:
  - id: easy
    title:
      en: "{count}/{total} headshots"
      de: "{count}/{total} Kopfschüsse"
    type: player_kill
    amount: 3
    cooldown: 0
    visible: true
    announce_progress: true
    announce_completion: true
    data: {}
    rules:
      - key: global.iswarmup
        operator: bool==
        value: "false"
      - key: headshot
        operator: bool==
        value: "true"
    actions:
      - type: task.mark_completed
        values:
          - easy_noheadshot
    requires: []
  - id: easy_noheadshot
    title:
      en: "Rule broken: without headshot"
    type: player_kill
    amount: 1
    visible: false
    announce_progress: false
    announce_completion: false
    rules:
      - key: headshot
        operator: bool==
        value: "false"
    actions:
      - type: task.reset_progress
        values:
          - easy
    requires: []
  - id: medium
    title:
      en: "{count}/{total} headshots"
    type: player_kill
    amount: 10
    requires:
      - easy
```

### title

Language map for the challenge name shown in the HUD. Lookup uses the player's language (`!lang`). If that key is missing, the **first** language written in the map is used. Do not rely on the server language for challenge titles.

### data

Optional payload forwarded to third-party plugins when the **whole challenge** is completed (all visible tasks done). Task-level `data` is sent on that task's progress/completion events.

### tasks

Ordered list. The plugin evaluates matching tasks **top to bottom**. Tasks with an empty `requires` list are available immediately and can run in parallel. A task that lists one id waits for that task. A task that lists several ids waits until **all** of them are complete (`AND`).

Hidden tasks (`visible: false`) are control rules. They do not count toward progress bars or the solved total. Put controls directly under the task they reset, with the same `requires`.

### Task fields

| Field | Meaning |
|-------|---------|
| `id` | Unique within the file |
| `title` | Language map; supports `{count}` / `{total}` |
| `type` | Game event type (see Events docs / `catalog.json`) |
| `amount` | Times the event must match |
| `cooldown` | Seconds between credits |
| `visible` | Show in HUD / count toward solved |
| `announce_*` | Chat notifications |
| `rules` | Conditions on event data |
| `actions` | Side effects when the task completes |
| `requires` | Other task ids in this file that must be finished first |

### Actions

- `task.reset_progress` / `task.reset_completed` / `task.mark_completed` — values are task ids in the same file
- `notify.player.progress.rule_broken` / `notify.player.completed.rule_broken`
- `server.runcommand` — supports `{steamid}`, `{userid}`, `{index}`

### Ordering note

List stages from the **first** unlockable task at the top to the **last** at the bottom. The plugin snapshots eligibility before applying progress, so finishing a task cannot unlock and credit the next task on the same event.

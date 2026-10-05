# Actions

Actions run when a **task** completes. Values that point at other work use **task ids in the same challenge file**.

## Available actions

| Type | What it does | `values` |
|------|----------------|----------|
| `task.reset_progress` | Clears progress of an **unfinished** task. | Task id(s) |
| `task.reset_completed` | Clears progress of a **finished** task. | Task id(s) |
| `task.mark_completed` | Marks a task complete regardless of current progress. | Task id(s) |
| `notify.player.progress.rule_broken` | Tells the player they broke a rule. Fires only if a listed task already has progress. | Related task id(s) |
| `notify.player.completed.rule_broken` | Same, but only if a listed task is already complete. | Related task id(s) |
| `server.runcommand` | Runs a server console command. Placeholders: `{steamid}`, `{userid}`, `{index}`. | One command string |

## Streak broken (hidden task)

Use a hidden task (`visible: false`) that matches the “bad” case (for example a kill without headshot). On completion, reset the main streak and notify the player.

```yaml
- id: easy
  type: player_kill
  amount: 3
  rules:
    - key: headshot
      operator: bool==
      value: "true"
  actions:
    - type: task.mark_completed
      values:
        - easy_noheadshot

- id: easy_noheadshot
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
    - type: notify.player.progress.rule_broken
      values:
        - easy
    - type: task.reset_progress
      values:
        - easy
    - type: task.reset_completed
      values:
        - easy_noheadshot
```

See `examples/blueprints/headshots_in_a_row.yaml` for the full pattern.

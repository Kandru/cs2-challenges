# Actions

Actions run when a **task** completes. Values that point at other work use **task ids in the same challenge file**.

## Available actions

<a id="task-reset-progress"></a>

### `task.reset_progress`

Clears progress of an **unfinished** task. `values`: task id(s).

<a id="task-reset-completed"></a>

### `task.reset_completed`

Clears progress of a **finished** task. `values`: task id(s).

<a id="task-mark-completed"></a>

### `task.mark_completed`

Marks a task complete regardless of current progress. `values`: task id(s).

<a id="notify-player-progress-rule-broken"></a>

### `notify.player.progress.rule_broken`

Tells the player they broke a rule. Fires only if a listed task already has progress. `values`: related task id(s).

<a id="notify-player-completed-rule-broken"></a>

### `notify.player.completed.rule_broken`

Same as above, but only if a listed task is already complete. `values`: related task id(s).

<a id="server-runcommand"></a>

### `server.runcommand`

Runs a server console command. Placeholders: `{steamid}`, `{userid}`, `{index}`. `values`: one command string.

<a id="streak-broken"></a>

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

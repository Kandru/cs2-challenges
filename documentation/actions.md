# Actions

Actions run when a **task** reaches its `amount` (completion), not on each progress point. Values that point at other work use **task ids in the same challenge file**.

## Available actions

### `task.reset_progress`

Clears progress of an **unfinished** task. `values`: task id(s).

### `task.reset_completed`

Clears progress of a **finished** task. `values`: task id(s).

Resetting another **visible** task prints the “Challenge deleted” chat line for that task.

### `task.mark_completed`

Sets a task’s progress to its goal. `values`: task id(s).

Does **not** run that task’s actions, completion chat, Discord, or shared events. Use it to mark control tasks done so later events cannot reset finished work.

### `notify.player.progress.rule_broken`

Tells the player they broke a rule (chat + sound + tracker). Fires only if a listed task already has progress. `values`: related task id(s).

### `notify.player.completed.rule_broken`

Same as above, but only if a listed task is already complete. `values`: related task id(s).

### `server.runcommand`

Runs a server console command when the task completes. Placeholders and a full example are in [Plugin integration](plugin-integration.md#server-commands). `values`: one command string.

## Streak broken (hidden task)

Use a hidden task (`visible: false`) that matches the “bad” case (for example a kill without headshot). On completion it resets the streak and notifies the player. When the streak task finishes, it marks the breaker complete so a later non-headshot cannot wipe a finished streak.

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

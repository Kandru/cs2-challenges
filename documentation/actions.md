# Actions

Actions run when a **task** completes. Values that refer to other work use **task ids in the same challenge file**.

## Available actions

### task.reset_progress

Deletes progress of an unfinished task. Value: task id.

### task.reset_completed

Deletes progress of a completed task. Value: task id.

### task.mark_completed

Marks a task complete regardless of current progress. Value: task id.

### notify.player.progress.rule_broken

Notifies the player that they broke a rule while progressing. Values: related task ids to check for prior progress.

### notify.player.completed.rule_broken

Notifies the player that they broke a rule after completing a related task. Values: task ids that must already be completed.

### server.runcommand

Runs a server console command. Placeholders: `{steamid}`, `{userid}`, `{index}`.

## Control tasks

Use a hidden task (`visible: false`) with rules that detect a broken condition (e.g. body shot during a headshot streak). On completion, reset the main task with `task.reset_progress` and optionally notify the player.

See the `examples/blueprints/` folder for full patterns.

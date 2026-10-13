# Write your first challenge

One YAML file is one **challenge**. Inside it, a `tasks:` list holds one or more **tasks**. The filename without `.yaml` is the challenge id. Schedules list that id.

Save the file under:

`/addons/counterstrikesharp/configs/plugins/Challenges/blueprints/<challenge_id>.yaml`

## Minimal challenge

Create `blueprints/headshots_in_a_row.yaml` (or any snake_case name):

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

What this does:

- The challenge title appears in the HUD menu.
- Task `easy` needs three headshot kills (and not during warmup).
- Task `medium` unlocks only after `easy` finishes (`requires: [easy]`).
- Task titles may use `{count}` and `{total}` for progress text.

## Wire it into a schedule

In `schedules.yaml`, list the challenge id (filename without `.yaml`) under an active schedule’s `challenges:` list. Set `date_start` / `date_end` in **UTC** so the window includes now. See [Schedules](../howto/schedules.md).

Then change map or run `challenges reload`.

## How tasks unlock

- Tasks with empty `requires` can run in parallel.
- A challenge is **solved** when every `visible: true` task is done.
- Hidden tasks (`visible: false`) are control steps. They stay off the HUD and do not count toward the solved total.
- Finishing a task does **not** also credit the next task on the same event. List stages from first unlockable task at the top to last at the bottom.

## Next

- Full field lists: [Challenges and tasks](../reference/challenges.md)
- When an event counts: [Rules](../reference/rules.md)
- What happens on completion: [Actions](../reference/actions.md)
- Which `type` to use: [Events](../reference/events.md) and [Listeners](../reference/listeners.md)
- Visual editor: [Challenge builder](builder.md)

Full patterns (streaks, resets, rewards) are in `examples/blueprints/`.

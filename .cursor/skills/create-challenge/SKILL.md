---
name: create-challenge
description: >-
  Author CS2 Challenges plugin challenge YAML files and schedules from a
  plain-language description. Use when the user asks to create, add, design,
  edit, or schedule a challenge, challenge file, task streak, or schedules.yaml
  entry.
---

# Create a CS2 challenge

Turn a user’s goal into valid challenge YAML and a schedule entry. Docs in this repo are the source of truth - read them before inventing keys. Public site: https://kandru.github.io/cs2-challenges/

## Steps

1. Restate: event `type`, counts, stages (`requires`), filters, streak/reset, optional `data`.
2. Read only what you need (in order): [challenges.md](../../../documentation/reference/challenges.md), [schedules.md](../../../documentation/howto/schedules.md), [rules.md](../../../documentation/reference/rules.md), [actions.md](../../../documentation/reference/actions.md), [events.md](../../../documentation/reference/events.md) / [listeners.md](../../../documentation/reference/listeners.md) + the linked event page, [GlobalEventData.md](../../../documentation/rules/GlobalEventData.md) / [GlobalPlayerData.md](../../../documentation/rules/GlobalPlayerData.md), [CsTeam.md](../../../documentation/enums/CsTeam.md), [rewards.md](../../../documentation/howto/rewards.md) only for `data`.
3. Copy the closest example from [examples/blueprints/](../../../examples/blueprints/) (streaks, weapons, knife/taser/scout, utility, trade/grenade/smoke/killerqueen/swap - see [situation memory](../../../documentation/reference/rules.md#situation-memory)). Weapons: [catalog_weapons.json](../../../tools/catalog_weapons.json); prefer `contains` with short names (`ak47`, `knife`, `awp`).
4. Write YAML → wire schedule → self-check → tell user how to reload.

Ask only if the goal is ambiguous (missing event, counts, or whether a streak resets).

## Challenge file

- Default path: `examples/blueprints/<challenge_id>.yaml` (or `<server>/blueprints/…`). Id = filename without `.yaml` (snake_case). The folder name is `blueprints/`; the content is a **challenge** with **tasks**.
- `title` (and optional task titles) need at least one language; missing player language → first map entry. Task titles may use `{count}` / `{total}`.
- Rule keys only from the event page + globals. Unknown keys never match. Every `value` is a **string**. All rules AND; no OR.
- Operators: `==` `!=` `<` `>` `<=` `>=` `bool==` `bool!=` `contains` `!contains`.
- Competitive defaults unless the user wants otherwise:

```yaml
- key: global.iswarmup
  operator: bool==
  value: "false"
- key: global.isduringround
  operator: bool==
  value: "true"
```

- Exclude bots (`*.isbot` `bool==` `"false"`) and team/self kills when those keys exist.
- Stages: unlockable tasks top → bottom; later use `requires: [prior_id]`. Empty `requires` = available now (parallel OK).
- Solved = every `visible: true` task done. Hidden tasks are control only (off HUD, not in solved count). Completing a task does not credit the next stage on the same event.
- Optional `data` is for other plugins; this plugin only tracks progress.

**Streak / rule-broken:** visible task marks the hidden breaker complete; breaker notifies, resets the streak task, resets itself:

```yaml
# On the visible streak task:
actions:
  - type: task.mark_completed
    values: [easy_noheadshot]

# Hidden breaker:
- id: easy_noheadshot
  type: player_kill
  amount: 1
  visible: false
  announce_progress: false
  announce_completion: false
  rules: []  # "bad" case
  actions:
    - type: notify.player.progress.rule_broken
      values: [easy]
    - type: task.reset_progress
      values: [easy]
    - type: task.reset_completed
      values: [easy_noheadshot]
```

Actions: `task.reset_progress`, `task.reset_completed`, `task.mark_completed`, `notify.player.progress.rule_broken`, `notify.player.completed.rule_broken`, `server.runcommand` (`{steamid}`, `{userid}`, `{index}`). Values are task ids (except `server.runcommand`).

## Schedule

- Default: [examples/schedules.yaml](../../../examples/schedules.yaml). Append the challenge id under `challenges:`.
- One active schedule: first UTC window containing now. Dates: `"YYYY-MM-DD HH:MM:SS"` UTC.
- Changing title/dates starts a new progress bucket - warn the user. Unknown ids are skipped.

## Self-check

Filename matches schedule entry; every `requires` / action id exists; every rule key is documented; bools use `bool==`/`bool!=` with `"true"`/`"false"`; streak breakers `visible: false`; schedule window includes now if expected live.

Reload: copy into `/addons/counterstrikesharp/configs/plugins/Challenges/` if needed, then change map or `challenges reload`. Debug: `"development": { "enable": true }` in `Challenges.json` - [tutorial](../../../documentation/tutorial/index.md), [settings.md](../../../documentation/reference/settings.md).

## Skeleton

```yaml
title:
  en: Example challenge
  de: Beispiel-Herausforderung
tasks:
  - id: easy
    title:
      en: "{count}/{total} example"
      de: "{count}/{total} Beispiel"
    type: player_kill
    amount: 5
    rules:
      - key: global.iswarmup
        operator: bool==
        value: "false"
      - key: global.isduringround
        operator: bool==
        value: "true"
      - key: victim.isbot
        operator: bool==
        value: "false"
    requires: []
```

## Do not

Invent event types or rule keys; put OR in rules; count hidden tasks toward solved; rely on the browser builder alone without event pages.

---
name: create-challenge
description: >-
  Author CS2 Challenges plugin blueprints and schedules from a plain-language
  description. Use when the user asks to create, add, design, edit, or schedule
  a challenge, challenge blueprint, task streak, or schedules.yaml entry.
---

# Create a CS2 challenge

Turn a user’s goal into valid blueprint YAML and a schedule entry. Docs in this repo are the source of truth — read them before inventing keys.

## Workflow

Copy and track:

```
Progress:
- [ ] 1. Restate tasks
- [ ] 2. Read needed docs + closest example
- [ ] 3. Write blueprint YAML
- [ ] 4. Wire into schedules.yaml
- [ ] 5. Self-check and tell user how to reload
```

### 1. Restate tasks

Map the request to concrete pieces:

- Event / challenge `type` (e.g. kill, plant, hurt)
- Counts (`amount`) and stages (`requires`)
- Filters (weapon, headshot, noscope, map, team, …)
- Streak / “rule broken” behavior
- Optional reward `data` for other plugins

Ask only if the goal is ambiguous (missing event, counts, or whether a streak resets).

### 2. Read docs (only what you need)

Read in this order before writing keys:

1. [documentation/blueprints.md](../../../documentation/blueprints.md) — file shape, fields, unlock rules
2. [documentation/schedules.md](../../../documentation/schedules.md) — UTC windows, active schedule
3. [documentation/rules.md](../../../documentation/rules.md) — operators, AND semantics
4. [documentation/actions.md](../../../documentation/actions.md) — resets, notifies, streak pattern
5. [documentation/events.md](../../../documentation/events.md) — pick `type`, then open the linked page under `documentation/events/` for allowed rule keys
6. [documentation/rules/GlobalEventData.md](../../../documentation/rules/GlobalEventData.md) and [documentation/rules/GlobalPlayerData.md](../../../documentation/rules/GlobalPlayerData.md) when using `global.*` or player prefixes
7. [documentation/enums/CsTeam.md](../../../documentation/enums/CsTeam.md) for team string values
8. [documentation/plugin-integration.md](../../../documentation/plugin-integration.md) only when adding `data` payloads

Copy structure from the closest example in [examples/blueprints/](../../../examples/blueprints/):

| Pattern | Example |
|---------|---------|
| Headshot / noscope streaks | `headshots_in_a_row.yaml`, `noscopes_in_a_row.yaml` |
| Weapon mastery (buy/pickup + kills) | `weapon_ak47.yaml` |
| Knife / special weapon | `knife_challenge.yaml`, `taser_challenge.yaml`, `scout_challenge.yaml` |
| Utility / objective | `blind_them_by_the_light.yaml`, `hostage_rescue.yaml`, `inflatable_castle.yaml` |

Weapon short names: [tools/catalog_weapons.json](../../../tools/catalog_weapons.json). Prefer `contains` with the short form (`ak47`, `knife`, `awp`).

### 3. Write the blueprint

**Output path**

- Default (this repo): `examples/blueprints/<challenge_id>.yaml`
- If the user gives a server config root: `<that>/blueprints/<challenge_id>.yaml`

Rules:

- Challenge id = filename without `.yaml` (snake_case).
- Provide `title` (and task titles) with at least `en` and `de`.
- Task titles may use `{count}` and `{total}`.
- Use only rule keys listed on that event’s page plus global keys. Unknown keys never match.
- Every rule `value` is a **string** in YAML (`"true"`, `"false"`, numbers as strings).
- All rules on a task are AND. There is no OR.
- Operators: `==` `!=` `<` `>` `<=` `>=` `bool==` `bool!=` `contains` `!contains` (see rules.md).
- For live competitive tasks, unless the user wants otherwise, include:

```yaml
- key: global.iswarmup
  operator: bool==
  value: "false"
- key: global.isduringround
  operator: bool==
  value: "true"
```

- Exclude bots when the event exposes it (`victim.isbot` / equivalent `bool==` `"false"`).
- Exclude team kills / self kills when those keys exist (`isteamkill`, `isselfkill`).
- Stages: list unlockable tasks top → bottom; later stages use `requires: [prior_id]` (AND if several). Empty `requires` = available immediately (can run in parallel).
- A challenge is done when every `visible: true` task is done. Hidden tasks (`visible: false`) are control steps — off HUD, do not count toward solved.
- Completing a task does **not** also credit the next stage on the same event.
- Optional `data` is a nested map for other plugins (e.g. `PlayerSessions.setpoints`). This plugin only tracks progress; it does not grant rewards.

**Streak / rule-broken pattern** (from actions.md): matching task marks the hidden breaker complete; the breaker notifies, resets the streak task, then resets itself:

```yaml
# On the visible streak task (success path):
actions:
  - type: task.mark_completed
    values:
      - easy_noheadshot

# Hidden breaker task:
- id: easy_noheadshot
  type: player_kill   # or player_death, etc.
  amount: 1
  visible: false
  announce_progress: false
  announce_completion: false
  rules:
    # "bad" case (e.g. headshot == false)
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

Action types: `task.reset_progress`, `task.reset_completed`, `task.mark_completed`, `notify.player.progress.rule_broken`, `notify.player.completed.rule_broken`, `server.runcommand` (placeholders `{steamid}`, `{userid}`, `{index}`). Values are task ids in the same file (except `server.runcommand`).

### 4. Wire the schedule

**Output path**

- Default: [examples/schedules.yaml](../../../examples/schedules.yaml)
- Server path: `<config>/schedules.yaml`

- Append the challenge id under the intended schedule’s `challenges:` list.
- Only **one** schedule is active: the **first** entry whose UTC window contains now.
- Dates: `"YYYY-MM-DD HH:MM:SS"` in **UTC**.
- Changing a schedule’s **title or dates** starts a **new** progress bucket (old progress is not carried over). Warn the user if you change those fields.
- Unknown challenge ids are skipped — filename and list entry must match.

### 5. Self-check

Before finishing:

- [ ] Filename id matches schedule entry
- [ ] Every `requires` / action `values` id exists in the same file
- [ ] Every rule `key` is on the event page or a global page
- [ ] Bools use `bool==` / `bool!=` with `"true"` / `"false"`
- [ ] Stages ordered; streak breakers are `visible: false`
- [ ] Schedule dates include “now” if the user expects it live

Tell the user: copy into `/addons/counterstrikesharp/configs/plugins/Challenges/` (if writing under `examples/`), then change map or run `challenges reload`. Debug with `"debug": true` in `Challenges.json` if nothing matches — see [documentation/howto.md](../../../documentation/howto.md) and [documentation/settings.md](../../../documentation/settings.md).

## Minimal skeleton

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

- Invent event types or rule keys not in the docs
- Put OR logic in rules (split into tasks/actions instead)
- Count hidden tasks as part of the solved total
- Rely on the browser builder alone — still validate against event pages when writing YAML by hand

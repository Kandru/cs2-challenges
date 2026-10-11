# Rules

Rules decide whether an event counts for a task. Every rule on a task must pass (`AND`). There is no `OR`.

```yaml
rules:
  - key: global.iswarmup
    operator: bool==
    value: "false"
  - key: global.isduringround
    operator: bool==
    value: "true"
  - key: headshot
    operator: bool==
    value: "true"
```

| Field | Meaning |
|-------|---------|
| `key` | Value from the event (see that event’s page under [Events](events.md)) or a [global key](#global-keys). |
| `operator` | How to compare - see below. |
| `value` | Always a string in YAML. Match the key’s type (`true` / `false` for bools, numbers for ints/floats). |

> [!WARNING]
> If you use an unknown key, the task will not progress: the key is missing from the event data, so the rule fails. Use only keys listed on that event’s page (plus the global keys below).

## Operators

| Operator | Use for |
|----------|---------|
| `==` | Equal (case-insensitive for strings) |
| `!=` | Not equal (case-insensitive) |
| `<` `>` `<=` `>=` | Numbers |
| `bool==` `bool!=` | Bools (`true` / `false`) |
| `contains` `!contains` | Substring (case-insensitive) |

## Global keys

Available on almost every event:

- [Global event data](rules/GlobalEventData.md) - warmup, round, map, hostages
- [Global player data](rules/GlobalPlayerData.md) - name, team, health, … (use the prefix from the event page, e.g. `attacker`, `victim`, `player`)

Team names: [CsTeam](enums/CsTeam.md).

`global.isduringround` is `true` from round start (including freeze time) until round end, and `false` between rounds. `global.iswarmup` is separate (warmup period from game rules).

Rules only decide whether an event **counts**. They never hide a challenge from the menu.

## Situation memory

Some keys come from short-lived round memory, not from a single CSS field. They only appear when a loaded task uses them. Memory is cleared on round start and never saved to the player file.

| Pattern | Challenge type / event | Behavior |
|---------|------------------------|----------|
| `player_trade` + `seconds`, `same_weapon`, … | [PlayerTrade](events/PlayerTrade.md) | Mutual kill within a window (either order, including same tick). Default window **5** seconds if no `seconds` rule is set. |
| `{attacker\|victim\|userid}.grenade` | [EventPlayerDeath](events/EventPlayerDeath.md) | `none` / `holding` / `thrown` / `both` |
| `{prefix}.grenade.holding` / `.thrown` | same | Weapon short name(s); thrown is comma-separated. Thrown names are kept about **8** seconds. |
| `insmoke`, `smoke.distance`, `smoke.age` | [EventBombDefused](events/EventBombDefused.md) / begin-defuse | Bomb origin inside a **144**-unit smoke sphere. Smokes are kept about **22** seconds. |
| Prefer `player_bomb_defused` over `bomb_defused` | same | Credit only the defuser (the all-player type shares one data bag). |

Examples: `examples/blueprints/traded_kills.yaml`, `grenade_and_die.yaml`, `ninja_defuse.yaml`.

Key names and types for each event stay on that event’s page. This section only describes timing and values the key tables do not state.

## Example

Require a headshot kill that is not a suicide, during a live round, against a human:

```yaml
rules:
  - key: global.iswarmup
    operator: bool==
    value: "false"
  - key: global.isduringround
    operator: bool==
    value: "true"
  - key: headshot
    operator: bool==
    value: "true"
  - key: isselfkill
    operator: bool==
    value: "false"
  - key: victim.isbot
    operator: bool==
    value: "false"
```

See `examples/blueprints/` for more.

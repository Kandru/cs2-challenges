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
| `key` | Value from the event (see [Events](events.md)) or a [global key](#global-keys). |
| `operator` | How to compare — see below. |
| `value` | Always a string in YAML. Match the key’s type (`true` / `false` for bools, numbers for ints/floats). |

> [!WARNING]
> If you use an unknown key, the task will not work because that condition can never be met. Use only keys listed on that event’s page (plus the global keys below).

<a id="operators"></a>

## Operators

| Operator | Use for |
|----------|---------|
| `==` | Equal (case-insensitive for strings) |
| `!=` | Not equal (case-insensitive) |
| `<` `>` `<=` `>=` | Numbers |
| `bool==` `bool!=` | Bools (`true` / `false`) |
| `contains` `!contains` | Substring (case-insensitive) |

<a id="global-keys"></a>

## Global keys

Available on almost every event:

- [Global event data](rules/GlobalEventData.md) — warmup, round, map, hostages
- [Global player data](rules/GlobalPlayerData.md) — name, team, health, … (use the prefix from the event page, e.g. `attacker`, `victim`, `player`)

Team names: [CsTeam](enums/CsTeam.md).

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

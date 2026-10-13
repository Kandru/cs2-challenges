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
| `key` | Value from the event (see that event’s page under [Events](events.md) or [Listeners](listeners.md)) or a [global key](#global-keys). |
| `operator` | How to compare — see [Operators](#operators). |
| `value` | Always a string in YAML. Match the key’s type (`true` / `false` for bools, numbers for ints/floats). |
| `min_players` / `max_players` | Optional integers on `isace` only (defaults **2** / **5**). See [Situation memory](#situation-memory). |

!!! warning
    If you use an unknown key, the task will not progress: the key is missing from the event data, so the rule fails. Use only keys listed on that event’s page (plus the global keys below).

## Operators {#operators}

| Operator | Use for |
|----------|---------|
| `==` | Equal (case-insensitive for strings) |
| `!=` | Not equal (case-insensitive) |
| `<` `>` `<=` `>=` | Numbers |
| `bool==` `bool!=` | Bools (`true` / `false`) |
| `contains` `!contains` | Substring (case-insensitive) |

An unknown operator never matches; the server console reports it once when the challenge file loads.

## Global keys {#global-keys}

Available on almost every event:

- [Global event data](../rules/GlobalEventData.md) — warmup, round, map, hostages
- [Global player data](../rules/GlobalPlayerData.md) — name, team, health, … (use the prefix from the event page, e.g. `attacker`, `victim`, `player`)

Team names: [CsTeam](../enums/CsTeam.md).

Some keys have more than one spelling (for example `dmg_armor` and `dmgarmor`), and some players more than one prefix (for example `userid` and `victim` on `player_death`, `userid` and `defuser` on `player_bomb_defused`). The event pages list every spelling; they are interchangeable. The plugin resolves them once when the blueprint loads, so using an alias costs nothing per event. Debug output shows the key as written in the blueprint.

`global.isduringround` is `true` from round start (including freeze time) until round end, and `false` between rounds. `global.iswarmup` is separate (warmup period from game rules).

Rules only decide whether an event **counts**. They never hide a challenge from the menu.

## Situation memory {#situation-memory}

Some keys come from short-lived round memory, not from a single CSS field. They only appear when a loaded task uses them; memory for keys no task uses is not kept at all. Memory is cleared on round start and never saved to the player file.

| Pattern | Task type / event | Behavior |
|---------|-------------------|----------|
| `player_trade` + `seconds`, `same_weapon`, … | [PlayerTrade](events/PlayerTrade.md) | Mutual kill within a window (either order, including same tick). Default window **5** seconds if no `seconds` rule is set. |
| `{prefix}.grenade` | any event with players, see [below](#situation-attributes-for-any-player-prefix) | `none` / `holding` / `thrown` / `both` |
| `{prefix}.grenade.holding` / `.thrown` | same | Weapon short name(s); thrown is comma-separated. Thrown names are kept about **8** seconds. On `player_death` the victim's holding state is dropped after the event. |
| `{prefix}.grenade.throwing` | same | Comma-separated short names while a grenade is in hand (active weapon), pin pulled, or thrown within the last **1** second. Empty when none apply. |
| `{prefix}.insmoke` | same | Player origin inside a **144**-unit smoke sphere. |
| `insmoke`, `smoke.distance`, `smoke.age` | [EventBombDefused](events/EventBombDefused.md) / begin-defuse | Bomb origin inside a **144**-unit smoke sphere; distance and age of the covering (or nearest) smoke. Smokes are kept about **22** seconds. |
| `isace` | EventPlayerDeath (`player_kill` only) | `true` on the kill that completes the enemy team for this attacker. Only **human** players on the victim’s team count (alive or dead) at the time of the kill. Optional rule fields `min_players` (default **2**) and `max_players` (default **5**): team size below min never aces; otherwise the target is `min(team_size, max)`. Team kills, suicides, and bot victims never count. A further kill after the ace does not set `isace` again for that `(min, max)` pair. |
| Prefer `player_bomb_defused` over `bomb_defused` | same | Credit only the defuser (the all-player type shares one data bag). |

Examples: `examples/blueprints/traded_kills.yaml`, `grenade_and_die.yaml`, `killerqueen.yaml`, `ninja_defuse.yaml`, `swap_hegrenade.yaml`, `solo_hero.yaml`, `flawless_ace.yaml`.

Key names and types for each event stay on that event’s page. This section only describes timing and values the key tables do not state.

## Situation attributes for any player prefix

The player suffixes `grenade`, `grenade.holding`, `grenade.thrown`, `grenade.throwing` and `insmoke` work after **every** player prefix of every event and listener, aliases included, like the [player data](rules/GlobalPlayerData.md) suffixes. For example:

| Challenge type | Rule key | Meaning |
|----------------|----------|---------|
| `player_kill` | `attacker.insmoke` | The killer stood in a smoke. |
| `player_kill` | `victim.grenade.holding` | The victim died with the pin pulled. |
| `player_hurt_victim` | `victim.grenade.throwing` | Hit while throwing a grenade. |
| `weapon_fire` | `player.insmoke` | Shot fired from inside a smoke. |

The event pages list these keys for each prefix. Event-scoped situation keys (`isace`, the bomb `insmoke` / `smoke.*` keys) stay limited to the events named in the table above. `player_trade` only offers its own keys and the player data suffixes.

The value is read when the event fires, for the player behind that prefix; a missing player reads like a player without grenades or smoke (`none`, empty, `false`).

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

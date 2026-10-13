# EventPlayerHurt (player_hurt_attacker / player_hurt_victim)

CSS game event `player_hurt` (`EventPlayerHurt`).

Who gets credit: the **event player(s)** listed under task types.

## Task types

Put one of these in your task `type:` field:

- `player_hurt_attacker`
- `player_hurt_victim`

## Minimal YAML

Save under `blueprints/`. Add rules from the table below — see [Rules](../rules.md).

```yaml
tasks:
  - id: example
    type: player_hurt_attacker
    amount: 1
    rules: []
```

## Attributes (rule keys)

Unknown keys never match — see [Rules](../rules.md).

| Key | Type |
|-----|------|
| [global event data](../../rules/GlobalEventData.md) | see page |
| [player data](../../rules/GlobalPlayerData.md) | prefixes `attacker`, `userid`, `victim` |
| `armor` | int |
| `dmg_armor` | int |
| `dmgarmor` | int |
| `dmg_health` | int |
| `dmghealth` | int |
| `health` | int |
| `hitgroup` | int |
| `weapon` | string |
| `isteamdamage` | bool |
| `isselfdamage` | bool |
| `attacker.grenade` | string |
| `attacker.grenade.holding` | string |
| `attacker.grenade.thrown` | string |
| `attacker.grenade.throwing` | string |
| `attacker.insmoke` | bool |
| `userid.grenade` | string |
| `userid.grenade.holding` | string |
| `userid.grenade.thrown` | string |
| `userid.grenade.throwing` | string |
| `userid.insmoke` | bool |
| `victim.grenade` | string |
| `victim.grenade.holding` | string |
| `victim.grenade.thrown` | string |
| `victim.grenade.throwing` | string |
| `victim.insmoke` | bool |

# EventDronegunAttack (dronegun_attack)

CSS game event `dronegun_attack` (`EventDronegunAttack`).

Who gets credit: the **event player(s)** listed under task types.

## Task types

Put one of these in your task `type:` field:

- `dronegun_attack`

## Minimal YAML

Save under `blueprints/`. Add rules from the table below — see [Rules](../rules.md).

```yaml
tasks:
  - id: example
    type: dronegun_attack
    amount: 1
    rules: []
```

## Attributes (rule keys)

Unknown keys never match — see [Rules](../rules.md).

| Key | Type |
|-----|------|
| [global event data](../../rules/GlobalEventData.md) | see page |
| [player data](../../rules/GlobalPlayerData.md) | prefixes `userid`, `player` |
| `userid.grenade` | string |
| `userid.grenade.holding` | string |
| `userid.grenade.thrown` | string |
| `userid.grenade.throwing` | string |
| `userid.insmoke` | bool |
| `player.grenade` | string |
| `player.grenade.holding` | string |
| `player.grenade.thrown` | string |
| `player.grenade.throwing` | string |
| `player.insmoke` | bool |

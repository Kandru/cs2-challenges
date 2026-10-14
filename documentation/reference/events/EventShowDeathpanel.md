# EventShowDeathpanel (show_deathpanel_killer_controller / show_deathpanel_victim)

CSS game event `show_deathpanel` (`EventShowDeathpanel`).

Who gets credit: the **event player(s)** listed under task types.

## Task types

Put one of these in your task `type:` field:

- `show_deathpanel_killer_controller`
- `show_deathpanel_victim`

## Minimal YAML

Save under `blueprints/`. Add rules from the table below — see [Rules](../rules.md).

```yaml
tasks:
  - id: example
    type: show_deathpanel_killer_controller
    amount: 1
    rules: []
```

## Attributes (rule keys)

Unknown keys never match — see [Rules](../rules.md).

| Key | Type |
|-----|------|
| [global event data](../../rules/GlobalEventData.md) | see page |
| [player data](../../rules/GlobalPlayerData.md) | prefixes `killer_controller`, `victim` |
| `damage_given` | int |
| `damage_taken` | int |
| `hits_given` | int |
| `hits_taken` | int |
| `killer` | string |
| `killer_controller.grenade` | string |
| `killer_controller.grenade.holding` | string |
| `killer_controller.grenade.thrown` | string |
| `killer_controller.grenade.throwing` | string |
| `killer_controller.insmoke` | bool |
| `victim.grenade` | string |
| `victim.grenade.holding` | string |
| `victim.grenade.thrown` | string |
| `victim.grenade.throwing` | string |
| `victim.insmoke` | bool |

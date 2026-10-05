# EventShowDeathpanel (show_deathpanel_killer_controller / show_deathpanel_victim)

CSS game event `show_deathpanel` (`EventShowDeathpanel`).

Who gets credit: the **event player(s)** listed under challenge types.

## Challenge types

Put one of these in your task `type:` field:

- `show_deathpanel_killer_controller`
- `show_deathpanel_victim`

## Rule keys

> [!WARNING]
> If you use an unknown key, the task will not work because that condition can never be met.

| Key | Type |
|-----|------|
| [global event data](../rules/GlobalEventData.md) | see page |
| [player data](../rules/GlobalPlayerData.md) | prefixes `killer_controller`, `victim` |
| `damage_given` | int |
| `damage_taken` | int |
| `hits_given` | int |
| `hits_taken` | int |
| `killer` | string |

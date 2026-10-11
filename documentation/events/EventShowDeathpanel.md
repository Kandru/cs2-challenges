# EventShowDeathpanel (show_deathpanel_killer_controller / show_deathpanel_victim)

CSS game event `show_deathpanel` (`EventShowDeathpanel`).

Who gets credit: the **event player(s)** listed under challenge types.

## Challenge types

Put one of these in your task `type:` field:

- `show_deathpanel_killer_controller`
- `show_deathpanel_victim`

## Rule keys

Unknown keys never match - see [Rules](../rules.md).

| Key | Type |
|-----|------|
| [global event data](../rules/GlobalEventData.md) | see page |
| [player data](../rules/GlobalPlayerData.md) | prefixes `killer_controller`, `victim` |
| `damage_given` | int |
| `damage_taken` | int |
| `hits_given` | int |
| `hits_taken` | int |
| `killer` | string |

# EventShowSurvivalRespawnStatus (show_survival_respawn_status)

CSS game event `show_survival_respawn_status` (`EventShowSurvivalRespawnStatus`).

Who gets credit: the **event player(s)** listed under challenge types.

## Challenge types

Put one of these in your task `type:` field:

- `show_survival_respawn_status`

## Rule keys

> [!WARNING]
> If you use an unknown key, the task will not work because that condition can never be met.

| Key | Type |
|-----|------|
| [global event data](../rules/GlobalEventData.md) | see page |
| [player data](../rules/GlobalPlayerData.md) | prefixes `userid`, `player` |
| `duration` | int |
| `loc_token` | string |

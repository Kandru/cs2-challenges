# EventGgKilledEnemy (gg_killed_enemy_attackerid / gg_killed_enemy_victimid)

CSS game event `gg_killed_enemy` (`EventGgKilledEnemy`).

Who gets credit: the **event player(s)** listed under challenge types.

## Challenge types

Put one of these in your task `type:` field:

- `gg_killed_enemy_attackerid`
- `gg_killed_enemy_victimid`

## Rule keys

Unknown keys never match - see [Rules](../rules.md).

| Key | Type |
|-----|------|
| [global event data](../rules/GlobalEventData.md) | see page |
| [player data](../rules/GlobalPlayerData.md) | prefixes `attackerid`, `victimid` |
| `bonus` | bool |
| `dominated` | int |
| `revenge` | int |

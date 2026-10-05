# EventGgKilledEnemy (gg_killed_enemy_attackerid / gg_killed_enemy_victimid)

CSS game event `gg_killed_enemy` (`EventGgKilledEnemy`).

Who gets credit: the **event player(s)** listed under challenge types.

## Challenge types

Put one of these in your task `type:` field:

- `gg_killed_enemy_attackerid`
- `gg_killed_enemy_victimid`

## Rule keys

> [!WARNING]
> If you use an unknown key, the task will not work because that condition can never be met.

| Key | Type |
|-----|------|
| [global event data](../rules/GlobalEventData.md) | see page |
| [player data](../rules/GlobalPlayerData.md) | prefixes `attackerid`, `victimid` |
| `bonus` | bool |
| `dominated` | int |
| `revenge` | int |

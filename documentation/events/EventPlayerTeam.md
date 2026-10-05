# EventPlayerTeam (player_team)

CSS game event `player_team` (`EventPlayerTeam`).

Who gets credit: the **event player(s)** listed under challenge types.

## Challenge types

Put one of these in your task `type:` field:

- `player_team`

## Rule keys

> [!WARNING]
> If you use an unknown key, the task will not work because that condition can never be met.

| Key | Type |
|-----|------|
| [global event data](../rules/GlobalEventData.md) | see page |
| [player data](../rules/GlobalPlayerData.md) | prefixes `userid`, `player` |
| `disconnect` | bool |
| `isbot` | bool |
| `name` | string |
| `oldteam` | int |
| `silent` | bool |
| `team` | int |
| `old_team` | string |
| `new_team` | string |

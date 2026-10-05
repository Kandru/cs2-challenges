# EventPlayerChangename (player_changed_name)

CSS game event `player_changename` (`EventPlayerChangename`).

Who gets credit: the **event player(s)** listed under challenge types.

## Challenge types

Put one of these in your task `type:` field:

- `player_changed_name`

## Rule keys

> [!WARNING]
> If you use an unknown key, the task will not work because that condition can never be met.

| Key | Type |
|-----|------|
| [global event data](../rules/GlobalEventData.md) | see page |
| [player data](../rules/GlobalPlayerData.md) | prefixes `userid`, `player` |
| `newname` | string |
| `new_name` | string |
| `oldname` | string |
| `old_name` | string |

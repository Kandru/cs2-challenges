# EventBombDefused (bomb_defused / player_bomb_defused)

CSS game event `bomb_defused` (`EventBombDefused`).

Who gets credit: **all connected players**.

## Challenge types

Put one of these in your task `type:` field:

- `bomb_defused`
- `player_bomb_defused`

## Rule keys

Unknown keys never match - see [Rules](../rules.md).

| Key | Type |
|-----|------|
| [global event data](../rules/GlobalEventData.md) | see page |
| [player data](../rules/GlobalPlayerData.md) | prefixes `userid`, `defuser` |
| `site` | int |
| `bomb_site` | int |
| `insmoke` | bool |
| `smoke.distance` | float |
| `smoke.age` | float |
| `userid.insmoke` | bool |
| `defuser.insmoke` | bool |

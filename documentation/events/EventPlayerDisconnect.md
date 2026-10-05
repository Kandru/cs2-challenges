# EventPlayerDisconnect (player_disconnect)

CSS game event `player_disconnect` (`EventPlayerDisconnect`).

Who gets credit: the **event player(s)** listed under challenge types.

## Challenge types

Put one of these in your task `type:` field:

- `player_disconnect`

## Rule keys

> [!WARNING]
> If you use an unknown key, the task will not work because that condition can never be met.

| Key | Type |
|-----|------|
| [global event data](../rules/GlobalEventData.md) | see page |
| [player data](../rules/GlobalPlayerData.md) | prefixes `userid`, `player` |
| `ever_fully_connected` | bool |
| `name` | string |
| `networkid` | string |
| `PlayerID` | int |
| `reason` | int |
| `xuid` | int |

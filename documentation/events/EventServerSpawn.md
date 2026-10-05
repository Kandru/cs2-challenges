# EventServerSpawn (server_spawn)

CSS game event `server_spawn` (`EventServerSpawn`).

Who gets credit: **all connected players**.

## Challenge types

Put one of these in your task `type:` field:

- `server_spawn`

## Rule keys

> [!WARNING]
> If you use an unknown key, the task will not work because that condition can never be met.

| Key | Type |
|-----|------|
| [global event data](../rules/GlobalEventData.md) | see page |
| `addonname` | string |
| `address` | string |
| `dedicated` | bool |
| `game` | string |
| `hostname` | string |
| `mapname` | string |
| `maxplayers` | int |
| `os` | string |
| `password` | bool |
| `port` | int |

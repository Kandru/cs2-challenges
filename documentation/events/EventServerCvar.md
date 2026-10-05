# EventServerCvar (server_cvar)

CSS game event `server_cvar` (`EventServerCvar`).

Who gets credit: **all connected players**.

## Challenge types

Put one of these in your task `type:` field:

- `server_cvar`

## Rule keys

> [!WARNING]
> If you use an unknown key, the task will not work because that condition can never be met.

| Key | Type |
|-----|------|
| [global event data](../rules/GlobalEventData.md) | see page |
| `cvarname` | string |
| `cvarvalue` | string |

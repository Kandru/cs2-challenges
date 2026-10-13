# EventServerSpawn (server_spawn)

CSS game event `server_spawn` (`EventServerSpawn`).

Who gets credit: **all connected players**.

## Task types

Put one of these in your task `type:` field:

- `server_spawn`

## Minimal YAML

Save under `blueprints/`. Add rules from the table below — see [Rules](../rules.md).

```yaml
tasks:
  - id: example
    type: server_spawn
    amount: 1
    rules: []
```

## Attributes (rule keys)

Unknown keys never match — see [Rules](../rules.md).

| Key | Type |
|-----|------|
| [global event data](../../rules/GlobalEventData.md) | see page |
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

# EventPlayerDisconnect (player_disconnect)

CSS game event `player_disconnect` (`EventPlayerDisconnect`).

Who gets credit: the **event player(s)** listed under task types.

## Task types

Put one of these in your task `type:` field:

- `player_disconnect`

## Minimal YAML

Save under `blueprints/`. Add rules from the table below — see [Rules](../rules.md).

```yaml
tasks:
  - id: example
    type: player_disconnect
    amount: 1
    rules: []
```

## Attributes (rule keys)

Unknown keys never match — see [Rules](../rules.md).

| Key | Type |
|-----|------|
| [global event data](../../rules/GlobalEventData.md) | see page |
| [player data](../../rules/GlobalPlayerData.md) | prefixes `userid`, `player` |
| `ever_fully_connected` | bool |
| `name` | string |
| `networkid` | string |
| `PlayerID` | int |
| `reason` | int |
| `xuid` | int |

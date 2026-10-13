# EventPlayerPingStop (player_ping_stop)

CSS game event `player_ping_stop` (`EventPlayerPingStop`).

Who gets credit: **all connected players**.

## Task types

Put one of these in your task `type:` field:

- `player_ping_stop`

## Minimal YAML

Save under `blueprints/`. Add rules from the table below — see [Rules](../rules.md).

```yaml
tasks:
  - id: example
    type: player_ping_stop
    amount: 1
    rules: []
```

## Attributes (rule keys)

Unknown keys never match — see [Rules](../rules.md).

| Key | Type |
|-----|------|
| [global event data](../../rules/GlobalEventData.md) | see page |
| `entityid` | int |

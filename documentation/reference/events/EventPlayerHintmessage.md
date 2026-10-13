# EventPlayerHintmessage (player_hintmessage)

CSS game event `player_hintmessage` (`EventPlayerHintmessage`).

Who gets credit: **all connected players**.

## Task types

Put one of these in your task `type:` field:

- `player_hintmessage`

## Minimal YAML

Save under `blueprints/`. Add rules from the table below — see [Rules](../rules.md).

```yaml
tasks:
  - id: example
    type: player_hintmessage
    amount: 1
    rules: []
```

## Attributes (rule keys)

Unknown keys never match — see [Rules](../rules.md).

| Key | Type |
|-----|------|
| [global event data](../../rules/GlobalEventData.md) | see page |
| `hintmessage` | string |

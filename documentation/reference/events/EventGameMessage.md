# EventGameMessage (game_message)

CSS game event `game_message` (`EventGameMessage`).

Who gets credit: **all connected players**.

## Task types

Put one of these in your task `type:` field:

- `game_message`

## Minimal YAML

Save under `blueprints/`. Add rules from the table below — see [Rules](../rules.md).

```yaml
tasks:
  - id: example
    type: game_message
    amount: 1
    rules: []
```

## Attributes (rule keys)

Unknown keys never match — see [Rules](../rules.md).

| Key | Type |
|-----|------|
| [global event data](../../rules/GlobalEventData.md) | see page |
| `target` | int |
| `text` | string |

# EventGameEnd (game_end)

CSS game event `game_end` (`EventGameEnd`).

Who gets credit: **all connected players**.

## Task types

Put one of these in your task `type:` field:

- `game_end`

## Minimal YAML

Save under `blueprints/`. Add rules from the table below — see [Rules](../rules.md).

```yaml
tasks:
  - id: example
    type: game_end
    amount: 1
    rules: []
```

## Attributes (rule keys)

Unknown keys never match — see [Rules](../rules.md).

| Key | Type |
|-----|------|
| [global event data](../../rules/GlobalEventData.md) | see page |
| `winner` | int |

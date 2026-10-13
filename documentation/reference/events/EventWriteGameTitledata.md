# EventWriteGameTitledata (write_game_titledata)

CSS game event `write_game_titledata` (`EventWriteGameTitledata`).

Who gets credit: **all connected players**.

## Task types

Put one of these in your task `type:` field:

- `write_game_titledata`

## Minimal YAML

Save under `blueprints/`. Add rules from the table below — see [Rules](../rules.md).

```yaml
tasks:
  - id: example
    type: write_game_titledata
    amount: 1
    rules: []
```

## Attributes (rule keys)

Unknown keys never match — see [Rules](../rules.md).

| Key | Type |
|-----|------|
| [global event data](../../rules/GlobalEventData.md) | see page |
| `controllerId` | int |

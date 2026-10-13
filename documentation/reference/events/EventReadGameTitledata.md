# EventReadGameTitledata (read_game_titledata)

CSS game event `read_game_titledata` (`EventReadGameTitledata`).

Who gets credit: **all connected players**.

## Task types

Put one of these in your task `type:` field:

- `read_game_titledata`

## Minimal YAML

Save under `blueprints/`. Add rules from the table below — see [Rules](../rules.md).

```yaml
tasks:
  - id: example
    type: read_game_titledata
    amount: 1
    rules: []
```

## Attributes (rule keys)

Unknown keys never match — see [Rules](../rules.md).

| Key | Type |
|-----|------|
| [global event data](../../rules/GlobalEventData.md) | see page |
| `controllerId` | int |

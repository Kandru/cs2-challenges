# EventRoundStart (round_start)

CSS game event `round_start` (`EventRoundStart`).

Who gets credit: **all connected players**.

## Task types

Put one of these in your task `type:` field:

- `round_start`

## Minimal YAML

Save under `blueprints/`. Add rules from the table below — see [Rules](../rules.md).

```yaml
tasks:
  - id: example
    type: round_start
    amount: 1
    rules: []
```

## Attributes (rule keys)

Unknown keys never match — see [Rules](../rules.md).

| Key | Type |
|-----|------|
| [global event data](../../rules/GlobalEventData.md) | see page |
| `fraglimit` | int |
| `objective` | string |
| `timelimit` | int |

# EventMatchEndConditions (match_end_conditions)

CSS game event `match_end_conditions` (`EventMatchEndConditions`).

Who gets credit: **all connected players**.

## Task types

Put one of these in your task `type:` field:

- `match_end_conditions`

## Minimal YAML

Save under `blueprints/`. Add rules from the table below — see [Rules](../rules.md).

```yaml
tasks:
  - id: example
    type: match_end_conditions
    amount: 1
    rules: []
```

## Attributes (rule keys)

Unknown keys never match — see [Rules](../rules.md).

| Key | Type |
|-----|------|
| [global event data](../../rules/GlobalEventData.md) | see page |
| `frags` | int |
| `max_rounds` | int |
| `time` | int |
| `win_rounds` | int |

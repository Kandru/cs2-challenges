# EventDifficultyChanged (difficulty_changed)

CSS game event `difficulty_changed` (`EventDifficultyChanged`).

Who gets credit: **all connected players**.

## Task types

Put one of these in your task `type:` field:

- `difficulty_changed`

## Minimal YAML

Save under `blueprints/`. Add rules from the table below — see [Rules](../rules.md).

```yaml
tasks:
  - id: example
    type: difficulty_changed
    amount: 1
    rules: []
```

## Attributes (rule keys)

Unknown keys never match — see [Rules](../rules.md).

| Key | Type |
|-----|------|
| [global event data](../../rules/GlobalEventData.md) | see page |
| `newDifficulty` | int |
| `oldDifficulty` | int |
| `strDifficulty` | string |

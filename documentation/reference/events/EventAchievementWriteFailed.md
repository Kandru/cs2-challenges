# EventAchievementWriteFailed (achievement_write_failed)

CSS game event `achievement_write_failed` (`EventAchievementWriteFailed`).

Who gets credit: **all connected players**.

## Task types

Put one of these in your task `type:` field:

- `achievement_write_failed`

## Minimal YAML

Save under `blueprints/`. Add rules from the table below — see [Rules](../rules.md).

```yaml
tasks:
  - id: example
    type: achievement_write_failed
    amount: 1
    rules: []
```

## Attributes (rule keys)

Unknown keys never match — see [Rules](../rules.md).

| Key | Type |
|-----|------|
| [global event data](../../rules/GlobalEventData.md) | see page |

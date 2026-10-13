# EventAchievementEvent (achievement_event)

CSS game event `achievement_event` (`EventAchievementEvent`).

Who gets credit: **all connected players**.

## Task types

Put one of these in your task `type:` field:

- `achievement_event`

## Minimal YAML

Save under `blueprints/`. Add rules from the table below — see [Rules](../rules.md).

```yaml
tasks:
  - id: example
    type: achievement_event
    amount: 1
    rules: []
```

## Attributes (rule keys)

Unknown keys never match — see [Rules](../rules.md).

| Key | Type |
|-----|------|
| [global event data](../../rules/GlobalEventData.md) | see page |
| `achievement_name` | string |
| `cur_val` | int |
| `max_val` | int |

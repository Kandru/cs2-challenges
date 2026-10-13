# EventClientsideLessonClosed (clientside_lesson_closed)

CSS game event `clientside_lesson_closed` (`EventClientsideLessonClosed`).

Who gets credit: **all connected players**.

## Task types

Put one of these in your task `type:` field:

- `clientside_lesson_closed`

## Minimal YAML

Save under `blueprints/`. Add rules from the table below — see [Rules](../rules.md).

```yaml
tasks:
  - id: example
    type: clientside_lesson_closed
    amount: 1
    rules: []
```

## Attributes (rule keys)

Unknown keys never match — see [Rules](../rules.md).

| Key | Type |
|-----|------|
| [global event data](../../rules/GlobalEventData.md) | see page |
| `lesson_name` | string |

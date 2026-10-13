# EventInstructorStartLesson (instructor_start_lesson)

CSS game event `instructor_start_lesson` (`EventInstructorStartLesson`).

Who gets credit: the **event player(s)** listed under task types.

## Task types

Put one of these in your task `type:` field:

- `instructor_start_lesson`

## Minimal YAML

Save under `blueprints/`. Add rules from the table below — see [Rules](../rules.md).

```yaml
tasks:
  - id: example
    type: instructor_start_lesson
    amount: 1
    rules: []
```

## Attributes (rule keys)

Unknown keys never match — see [Rules](../rules.md).

| Key | Type |
|-----|------|
| [global event data](../../rules/GlobalEventData.md) | see page |
| [player data](../../rules/GlobalPlayerData.md) | prefixes `userid`, `player` |
| `hint_name` | string |
| `hint_target` | int |
| `vr_controller_type` | int |
| `vr_movement_type` | int |
| `vr_single_controller` | bool |

# EventSetInstructorGroupEnabled (set_instructor_group_enabled)

CSS game event `set_instructor_group_enabled` (`EventSetInstructorGroupEnabled`).

Who gets credit: **all connected players**.

## Task types

Put one of these in your task `type:` field:

- `set_instructor_group_enabled`

## Minimal YAML

Save under `blueprints/`. Add rules from the table below — see [Rules](../rules.md).

```yaml
tasks:
  - id: example
    type: set_instructor_group_enabled
    amount: 1
    rules: []
```

## Attributes (rule keys)

Unknown keys never match — see [Rules](../rules.md).

| Key | Type |
|-----|------|
| [global event data](../../rules/GlobalEventData.md) | see page |
| `enabled` | int |
| `group` | string |

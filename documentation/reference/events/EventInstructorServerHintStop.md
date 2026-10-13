# EventInstructorServerHintStop (instructor_server_hint_stop)

CSS game event `instructor_server_hint_stop` (`EventInstructorServerHintStop`).

Who gets credit: **all connected players**.

## Task types

Put one of these in your task `type:` field:

- `instructor_server_hint_stop`

## Minimal YAML

Save under `blueprints/`. Add rules from the table below — see [Rules](../rules.md).

```yaml
tasks:
  - id: example
    type: instructor_server_hint_stop
    amount: 1
    rules: []
```

## Attributes (rule keys)

Unknown keys never match — see [Rules](../rules.md).

| Key | Type |
|-----|------|
| [global event data](../../rules/GlobalEventData.md) | see page |
| `hint_entindex` | int |
| `hint_name` | string |

# EventHostageCallForHelp (hostage_call_for_help)

CSS game event `hostage_call_for_help` (`EventHostageCallForHelp`).

Who gets credit: **all connected players**.

## Task types

Put one of these in your task `type:` field:

- `hostage_call_for_help`

## Minimal YAML

Save under `blueprints/`. Add rules from the table below — see [Rules](../rules.md).

```yaml
tasks:
  - id: example
    type: hostage_call_for_help
    amount: 1
    rules: []
```

## Attributes (rule keys)

Unknown keys never match — see [Rules](../rules.md).

| Key | Type |
|-----|------|
| [global event data](../../rules/GlobalEventData.md) | see page |
| `hostage` | int |

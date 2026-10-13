# EventChoppersIncomingWarning (choppers_incoming_warning)

CSS game event `choppers_incoming_warning` (`EventChoppersIncomingWarning`).

Who gets credit: **all connected players**.

## Task types

Put one of these in your task `type:` field:

- `choppers_incoming_warning`

## Minimal YAML

Save under `blueprints/`. Add rules from the table below — see [Rules](../rules.md).

```yaml
tasks:
  - id: example
    type: choppers_incoming_warning
    amount: 1
    rules: []
```

## Attributes (rule keys)

Unknown keys never match — see [Rules](../rules.md).

| Key | Type |
|-----|------|
| [global event data](../../rules/GlobalEventData.md) | see page |
| `global` | bool |

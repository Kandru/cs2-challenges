# EventSfuievent (sfuievent)

CSS game event `sfuievent` (`EventSfuievent`).

Who gets credit: **all connected players**.

## Task types

Put one of these in your task `type:` field:

- `sfuievent`

## Minimal YAML

Save under `blueprints/`. Add rules from the table below — see [Rules](../rules.md).

```yaml
tasks:
  - id: example
    type: sfuievent
    amount: 1
    rules: []
```

## Attributes (rule keys)

Unknown keys never match — see [Rules](../rules.md).

| Key | Type |
|-----|------|
| [global event data](../../rules/GlobalEventData.md) | see page |
| `action` | string |
| `data` | string |
| `slot` | int |

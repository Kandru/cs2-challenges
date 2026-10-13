# EventFinaleStart (finale_start)

CSS game event `finale_start` (`EventFinaleStart`).

Who gets credit: **all connected players**.

## Task types

Put one of these in your task `type:` field:

- `finale_start`

## Minimal YAML

Save under `blueprints/`. Add rules from the table below — see [Rules](../rules.md).

```yaml
tasks:
  - id: example
    type: finale_start
    amount: 1
    rules: []
```

## Attributes (rule keys)

Unknown keys never match — see [Rules](../rules.md).

| Key | Type |
|-----|------|
| [global event data](../../rules/GlobalEventData.md) | see page |
| `rushes` | int |

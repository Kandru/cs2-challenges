# EventInfernoExtinguish (inferno_extinguish)

CSS game event `inferno_extinguish` (`EventInfernoExtinguish`).

Who gets credit: **all connected players**.

## Task types

Put one of these in your task `type:` field:

- `inferno_extinguish`

## Minimal YAML

Save under `blueprints/`. Add rules from the table below — see [Rules](../rules.md).

```yaml
tasks:
  - id: example
    type: inferno_extinguish
    amount: 1
    rules: []
```

## Attributes (rule keys)

Unknown keys never match — see [Rules](../rules.md).

| Key | Type |
|-----|------|
| [global event data](../../rules/GlobalEventData.md) | see page |
| `entityid` | int |
| `x` | float |
| `y` | float |
| `z` | float |

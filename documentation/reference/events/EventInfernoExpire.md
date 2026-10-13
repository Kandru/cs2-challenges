# EventInfernoExpire (inferno_expire)

CSS game event `inferno_expire` (`EventInfernoExpire`).

Who gets credit: **all connected players**.

## Task types

Put one of these in your task `type:` field:

- `inferno_expire`

## Minimal YAML

Save under `blueprints/`. Add rules from the table below — see [Rules](../rules.md).

```yaml
tasks:
  - id: example
    type: inferno_expire
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

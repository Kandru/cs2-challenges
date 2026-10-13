# EventDefuserDropped (defuser_dropped)

CSS game event `defuser_dropped` (`EventDefuserDropped`).

Who gets credit: **all connected players**.

## Task types

Put one of these in your task `type:` field:

- `defuser_dropped`

## Minimal YAML

Save under `blueprints/`. Add rules from the table below — see [Rules](../rules.md).

```yaml
tasks:
  - id: example
    type: defuser_dropped
    amount: 1
    rules: []
```

## Attributes (rule keys)

Unknown keys never match — see [Rules](../rules.md).

| Key | Type |
|-----|------|
| [global event data](../../rules/GlobalEventData.md) | see page |
| `entityid` | int |

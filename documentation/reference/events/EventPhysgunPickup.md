# EventPhysgunPickup (physgun_pickup)

CSS game event `physgun_pickup` (`EventPhysgunPickup`).

Who gets credit: **all connected players**.

## Task types

Put one of these in your task `type:` field:

- `physgun_pickup`

## Minimal YAML

Save under `blueprints/`. Add rules from the table below — see [Rules](../rules.md).

```yaml
tasks:
  - id: example
    type: physgun_pickup
    amount: 1
    rules: []
```

## Attributes (rule keys)

Unknown keys never match — see [Rules](../rules.md).

| Key | Type |
|-----|------|
| [global event data](../../rules/GlobalEventData.md) | see page |
| `target` | string |

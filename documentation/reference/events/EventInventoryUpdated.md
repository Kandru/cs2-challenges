# EventInventoryUpdated (inventory_updated)

CSS game event `inventory_updated` (`EventInventoryUpdated`).

Who gets credit: **all connected players**.

## Task types

Put one of these in your task `type:` field:

- `inventory_updated`

## Minimal YAML

Save under `blueprints/`. Add rules from the table below — see [Rules](../rules.md).

```yaml
tasks:
  - id: example
    type: inventory_updated
    amount: 1
    rules: []
```

## Attributes (rule keys)

Unknown keys never match — see [Rules](../rules.md).

| Key | Type |
|-----|------|
| [global event data](../../rules/GlobalEventData.md) | see page |
| `itemdef` | int |
| `itemid` | int |

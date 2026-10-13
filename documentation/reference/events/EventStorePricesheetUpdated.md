# EventStorePricesheetUpdated (store_pricesheet_updated)

CSS game event `store_pricesheet_updated` (`EventStorePricesheetUpdated`).

Who gets credit: **all connected players**.

## Task types

Put one of these in your task `type:` field:

- `store_pricesheet_updated`

## Minimal YAML

Save under `blueprints/`. Add rules from the table below — see [Rules](../rules.md).

```yaml
tasks:
  - id: example
    type: store_pricesheet_updated
    amount: 1
    rules: []
```

## Attributes (rule keys)

Unknown keys never match — see [Rules](../rules.md).

| Key | Type |
|-----|------|
| [global event data](../../rules/GlobalEventData.md) | see page |

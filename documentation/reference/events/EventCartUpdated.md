# EventCartUpdated (cart_updated)

CSS game event `cart_updated` (`EventCartUpdated`).

Who gets credit: **all connected players**.

## Task types

Put one of these in your task `type:` field:

- `cart_updated`

## Minimal YAML

Save under `blueprints/`. Add rules from the table below — see [Rules](../rules.md).

```yaml
tasks:
  - id: example
    type: cart_updated
    amount: 1
    rules: []
```

## Attributes (rule keys)

Unknown keys never match — see [Rules](../rules.md).

| Key | Type |
|-----|------|
| [global event data](../../rules/GlobalEventData.md) | see page |

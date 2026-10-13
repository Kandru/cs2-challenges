# EventDropRateModified (drop_rate_modified)

CSS game event `drop_rate_modified` (`EventDropRateModified`).

Who gets credit: **all connected players**.

## Task types

Put one of these in your task `type:` field:

- `drop_rate_modified`

## Minimal YAML

Save under `blueprints/`. Add rules from the table below — see [Rules](../rules.md).

```yaml
tasks:
  - id: example
    type: drop_rate_modified
    amount: 1
    rules: []
```

## Attributes (rule keys)

Unknown keys never match — see [Rules](../rules.md).

| Key | Type |
|-----|------|
| [global event data](../../rules/GlobalEventData.md) | see page |

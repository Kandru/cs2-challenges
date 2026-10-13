# EventHostageRescuedAll (hostage_rescued_all)

CSS game event `hostage_rescued_all` (`EventHostageRescuedAll`).

Who gets credit: **all connected players**.

## Task types

Put one of these in your task `type:` field:

- `hostage_rescued_all`

## Minimal YAML

Save under `blueprints/`. Add rules from the table below — see [Rules](../rules.md).

```yaml
tasks:
  - id: example
    type: hostage_rescued_all
    amount: 1
    rules: []
```

## Attributes (rule keys)

Unknown keys never match — see [Rules](../rules.md).

| Key | Type |
|-----|------|
| [global event data](../../rules/GlobalEventData.md) | see page |

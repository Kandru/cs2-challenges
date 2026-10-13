# EventHostnameChanged (hostname_changed)

CSS game event `hostname_changed` (`EventHostnameChanged`).

Who gets credit: **all connected players**.

## Task types

Put one of these in your task `type:` field:

- `hostname_changed`

## Minimal YAML

Save under `blueprints/`. Add rules from the table below — see [Rules](../rules.md).

```yaml
tasks:
  - id: example
    type: hostname_changed
    amount: 1
    rules: []
```

## Attributes (rule keys)

Unknown keys never match — see [Rules](../rules.md).

| Key | Type |
|-----|------|
| [global event data](../../rules/GlobalEventData.md) | see page |
| `hostname` | string |

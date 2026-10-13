# EventGcConnected (gc_connected)

CSS game event `gc_connected` (`EventGcConnected`).

Who gets credit: **all connected players**.

## Task types

Put one of these in your task `type:` field:

- `gc_connected`

## Minimal YAML

Save under `blueprints/`. Add rules from the table below — see [Rules](../rules.md).

```yaml
tasks:
  - id: example
    type: gc_connected
    amount: 1
    rules: []
```

## Attributes (rule keys)

Unknown keys never match — see [Rules](../rules.md).

| Key | Type |
|-----|------|
| [global event data](../../rules/GlobalEventData.md) | see page |

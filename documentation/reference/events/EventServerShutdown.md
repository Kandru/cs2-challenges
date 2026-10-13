# EventServerShutdown (server_shutdown)

CSS game event `server_shutdown` (`EventServerShutdown`).

Who gets credit: **all connected players**.

## Task types

Put one of these in your task `type:` field:

- `server_shutdown`

## Minimal YAML

Save under `blueprints/`. Add rules from the table below — see [Rules](../rules.md).

```yaml
tasks:
  - id: example
    type: server_shutdown
    amount: 1
    rules: []
```

## Attributes (rule keys)

Unknown keys never match — see [Rules](../rules.md).

| Key | Type |
|-----|------|
| [global event data](../../rules/GlobalEventData.md) | see page |
| `reason` | string |

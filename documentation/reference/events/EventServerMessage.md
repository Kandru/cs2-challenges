# EventServerMessage (server_message)

CSS game event `server_message` (`EventServerMessage`).

Who gets credit: **all connected players**.

## Task types

Put one of these in your task `type:` field:

- `server_message`

## Minimal YAML

Save under `blueprints/`. Add rules from the table below — see [Rules](../rules.md).

```yaml
tasks:
  - id: example
    type: server_message
    amount: 1
    rules: []
```

## Attributes (rule keys)

Unknown keys never match — see [Rules](../rules.md).

| Key | Type |
|-----|------|
| [global event data](../../rules/GlobalEventData.md) | see page |
| `text` | string |

# EventEnableRestartVoting (enable_restart_voting)

CSS game event `enable_restart_voting` (`EventEnableRestartVoting`).

Who gets credit: **all connected players**.

## Task types

Put one of these in your task `type:` field:

- `enable_restart_voting`

## Minimal YAML

Save under `blueprints/`. Add rules from the table below — see [Rules](../rules.md).

```yaml
tasks:
  - id: example
    type: enable_restart_voting
    amount: 1
    rules: []
```

## Attributes (rule keys)

Unknown keys never match — see [Rules](../rules.md).

| Key | Type |
|-----|------|
| [global event data](../../rules/GlobalEventData.md) | see page |
| `enable` | bool |

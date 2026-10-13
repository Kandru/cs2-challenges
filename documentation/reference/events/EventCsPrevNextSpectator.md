# EventCsPrevNextSpectator (cs_prev_next_spectator)

CSS game event `cs_prev_next_spectator` (`EventCsPrevNextSpectator`).

Who gets credit: **all connected players**.

## Task types

Put one of these in your task `type:` field:

- `cs_prev_next_spectator`

## Minimal YAML

Save under `blueprints/`. Add rules from the table below — see [Rules](../rules.md).

```yaml
tasks:
  - id: example
    type: cs_prev_next_spectator
    amount: 1
    rules: []
```

## Attributes (rule keys)

Unknown keys never match — see [Rules](../rules.md).

| Key | Type |
|-----|------|
| [global event data](../../rules/GlobalEventData.md) | see page |
| `next` | bool |

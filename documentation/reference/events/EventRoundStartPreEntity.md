# EventRoundStartPreEntity (round_start_pre_entity)

CSS game event `round_start_pre_entity` (`EventRoundStartPreEntity`).

Who gets credit: **all connected players**.

## Task types

Put one of these in your task `type:` field:

- `round_start_pre_entity`

## Minimal YAML

Save under `blueprints/`. Add rules from the table below — see [Rules](../rules.md).

```yaml
tasks:
  - id: example
    type: round_start_pre_entity
    amount: 1
    rules: []
```

## Attributes (rule keys)

Unknown keys never match — see [Rules](../rules.md).

| Key | Type |
|-----|------|
| [global event data](../../rules/GlobalEventData.md) | see page |

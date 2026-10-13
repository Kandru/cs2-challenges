# EventEntityKilled (entity_killed)

CSS game event `entity_killed` (`EventEntityKilled`).

Who gets credit: **all connected players**.

## Task types

Put one of these in your task `type:` field:

- `entity_killed`

## Minimal YAML

Save under `blueprints/`. Add rules from the table below — see [Rules](../rules.md).

```yaml
tasks:
  - id: example
    type: entity_killed
    amount: 1
    rules: []
```

## Attributes (rule keys)

Unknown keys never match — see [Rules](../rules.md).

| Key | Type |
|-----|------|
| [global event data](../../rules/GlobalEventData.md) | see page |
| `damagebits` | int |
| `entindex_attacker` | int |
| `entindex_inflictor` | int |
| `entindex_killed` | int |

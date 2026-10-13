# EventDoorBreak (door_break)

CSS game event `door_break` (`EventDoorBreak`).

Who gets credit: **all connected players**.

## Task types

Put one of these in your task `type:` field:

- `door_break`

## Minimal YAML

Save under `blueprints/`. Add rules from the table below — see [Rules](../rules.md).

```yaml
tasks:
  - id: example
    type: door_break
    amount: 1
    rules: []
```

## Attributes (rule keys)

Unknown keys never match — see [Rules](../rules.md).

| Key | Type |
|-----|------|
| [global event data](../../rules/GlobalEventData.md) | see page |
| `dmgstate` | int |
| `entindex` | int |

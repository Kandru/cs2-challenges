# EventSurvivalParadropSpawn (survival_paradrop_spawn)

CSS game event `survival_paradrop_spawn` (`EventSurvivalParadropSpawn`).

Who gets credit: **all connected players**.

## Task types

Put one of these in your task `type:` field:

- `survival_paradrop_spawn`

## Minimal YAML

Save under `blueprints/`. Add rules from the table below — see [Rules](../rules.md).

```yaml
tasks:
  - id: example
    type: survival_paradrop_spawn
    amount: 1
    rules: []
```

## Attributes (rule keys)

Unknown keys never match — see [Rules](../rules.md).

| Key | Type |
|-----|------|
| [global event data](../../rules/GlobalEventData.md) | see page |
| `entityid` | int |

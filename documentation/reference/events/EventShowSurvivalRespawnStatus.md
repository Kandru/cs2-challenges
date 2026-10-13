# EventShowSurvivalRespawnStatus (show_survival_respawn_status)

CSS game event `show_survival_respawn_status` (`EventShowSurvivalRespawnStatus`).

Who gets credit: the **event player(s)** listed under task types.

## Task types

Put one of these in your task `type:` field:

- `show_survival_respawn_status`

## Minimal YAML

Save under `blueprints/`. Add rules from the table below — see [Rules](../rules.md).

```yaml
tasks:
  - id: example
    type: show_survival_respawn_status
    amount: 1
    rules: []
```

## Attributes (rule keys)

Unknown keys never match — see [Rules](../rules.md).

| Key | Type |
|-----|------|
| [global event data](../../rules/GlobalEventData.md) | see page |
| [player data](../../rules/GlobalPlayerData.md) | prefixes `userid`, `player` |
| `duration` | int |
| `loc_token` | string |

# EventBombPlanted (player_bomb_planted)

CSS game event `bomb_planted` (`EventBombPlanted`).

Who gets credit: the **event player(s)** listed under task types.

## Task types

Put one of these in your task `type:` field:

- `player_bomb_planted`

## Minimal YAML

Save under `blueprints/`. Add rules from the table below — see [Rules](../rules.md).

```yaml
tasks:
  - id: example
    type: player_bomb_planted
    amount: 1
    rules: []
```

## Attributes (rule keys)

Unknown keys never match — see [Rules](../rules.md).

| Key | Type |
|-----|------|
| [global event data](../../rules/GlobalEventData.md) | see page |
| [player data](../../rules/GlobalPlayerData.md) | prefixes `userid`, `planter` |
| `site` | int |
| `bomb_site` | int |
| `userid.grenade` | string |
| `userid.grenade.holding` | string |
| `userid.grenade.thrown` | string |
| `userid.grenade.throwing` | string |
| `userid.insmoke` | bool |
| `planter.grenade` | string |
| `planter.grenade.holding` | string |
| `planter.grenade.thrown` | string |
| `planter.grenade.throwing` | string |
| `planter.insmoke` | bool |

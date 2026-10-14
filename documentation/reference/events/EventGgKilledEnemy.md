# EventGgKilledEnemy (gg_killed_enemy_attackerid / gg_killed_enemy_victimid)

CSS game event `gg_killed_enemy` (`EventGgKilledEnemy`).

Who gets credit: the **event player(s)** listed under task types.

## Task types

Put one of these in your task `type:` field:

- `gg_killed_enemy_attackerid`
- `gg_killed_enemy_victimid`

## Minimal YAML

Save under `blueprints/`. Add rules from the table below — see [Rules](../rules.md).

```yaml
tasks:
  - id: example
    type: gg_killed_enemy_attackerid
    amount: 1
    rules: []
```

## Attributes (rule keys)

Unknown keys never match — see [Rules](../rules.md).

| Key | Type |
|-----|------|
| [global event data](../../rules/GlobalEventData.md) | see page |
| [player data](../../rules/GlobalPlayerData.md) | prefixes `attackerid`, `victimid` |
| `bonus` | bool |
| `dominated` | int |
| `revenge` | int |
| `attackerid.grenade` | string |
| `attackerid.grenade.holding` | string |
| `attackerid.grenade.thrown` | string |
| `attackerid.grenade.throwing` | string |
| `attackerid.insmoke` | bool |
| `victimid.grenade` | string |
| `victimid.grenade.holding` | string |
| `victimid.grenade.thrown` | string |
| `victimid.grenade.throwing` | string |
| `victimid.insmoke` | bool |

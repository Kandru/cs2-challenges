# EventTagrenadeDetonate (tagrenade_detonate)

CSS game event `tagrenade_detonate` (`EventTagrenadeDetonate`).

Who gets credit: the **event player(s)** listed under task types.

## Task types

Put one of these in your task `type:` field:

- `tagrenade_detonate`

## Minimal YAML

Save under `blueprints/`. Add rules from the table below — see [Rules](../rules.md).

```yaml
tasks:
  - id: example
    type: tagrenade_detonate
    amount: 1
    rules: []
```

## Attributes (rule keys)

Unknown keys never match — see [Rules](../rules.md).

| Key | Type |
|-----|------|
| [global event data](../../rules/GlobalEventData.md) | see page |
| [player data](../../rules/GlobalPlayerData.md) | prefixes `userid`, `player` |
| `entityid` | int |
| `x` | float |
| `y` | float |
| `z` | float |
| `userid.grenade` | string |
| `userid.grenade.holding` | string |
| `userid.grenade.thrown` | string |
| `userid.grenade.throwing` | string |
| `userid.insmoke` | bool |
| `player.grenade` | string |
| `player.grenade.holding` | string |
| `player.grenade.thrown` | string |
| `player.grenade.throwing` | string |
| `player.insmoke` | bool |

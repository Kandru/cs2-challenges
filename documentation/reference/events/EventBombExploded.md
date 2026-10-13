# EventBombExploded (bomb_exploded)

CSS game event `bomb_exploded` (`EventBombExploded`).

Who gets credit: **all connected players**.

## Task types

Put one of these in your task `type:` field:

- `bomb_exploded`

## Minimal YAML

Save under `blueprints/`. Add rules from the table below — see [Rules](../rules.md).

```yaml
tasks:
  - id: example
    type: bomb_exploded
    amount: 1
    rules: []
```

## Attributes (rule keys)

Unknown keys never match — see [Rules](../rules.md).

| Key | Type |
|-----|------|
| [global event data](../../rules/GlobalEventData.md) | see page |
| [player data](../../rules/GlobalPlayerData.md) | prefixes `userid`, `exploder` |
| `site` | int |
| `bomb_site` | int |
| `userid.grenade` | string |
| `userid.grenade.holding` | string |
| `userid.grenade.thrown` | string |
| `userid.grenade.throwing` | string |
| `userid.insmoke` | bool |
| `exploder.grenade` | string |
| `exploder.grenade.holding` | string |
| `exploder.grenade.thrown` | string |
| `exploder.grenade.throwing` | string |
| `exploder.insmoke` | bool |

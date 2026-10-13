# EventBombDefused (bomb_defused / player_bomb_defused)

CSS game event `bomb_defused` (`EventBombDefused`).

Who gets credit: **all connected players**.

## Task types

Put one of these in your task `type:` field:

- `bomb_defused`
- `player_bomb_defused`

## Minimal YAML

Save under `blueprints/`. Add rules from the table below — see [Rules](../rules.md).

```yaml
tasks:
  - id: example
    type: bomb_defused
    amount: 1
    rules: []
```

## Attributes (rule keys)

Unknown keys never match — see [Rules](../rules.md).

| Key | Type |
|-----|------|
| [global event data](../../rules/GlobalEventData.md) | see page |
| [player data](../../rules/GlobalPlayerData.md) | prefixes `userid`, `defuser` |
| `site` | int |
| `bomb_site` | int |
| `userid.grenade` | string |
| `userid.grenade.holding` | string |
| `userid.grenade.thrown` | string |
| `userid.grenade.throwing` | string |
| `userid.insmoke` | bool |
| `defuser.grenade` | string |
| `defuser.grenade.holding` | string |
| `defuser.grenade.thrown` | string |
| `defuser.grenade.throwing` | string |
| `defuser.insmoke` | bool |
| `insmoke` | bool |
| `smoke.distance` | float |
| `smoke.age` | float |

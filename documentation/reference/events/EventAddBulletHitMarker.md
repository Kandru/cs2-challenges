# EventAddBulletHitMarker (add_bullet_hit_marker)

CSS game event `add_bullet_hit_marker` (`EventAddBulletHitMarker`).

Who gets credit: the **event player(s)** listed under task types.

## Task types

Put one of these in your task `type:` field:

- `add_bullet_hit_marker`

## Minimal YAML

Save under `blueprints/`. Add rules from the table below — see [Rules](../rules.md).

```yaml
tasks:
  - id: example
    type: add_bullet_hit_marker
    amount: 1
    rules: []
```

## Attributes (rule keys)

Unknown keys never match — see [Rules](../rules.md).

| Key | Type |
|-----|------|
| [global event data](../../rules/GlobalEventData.md) | see page |
| [player data](../../rules/GlobalPlayerData.md) | prefixes `userid`, `player` |
| `ang_x` | int |
| `ang_y` | int |
| `ang_z` | int |
| `bone` | int |
| `hit` | bool |
| `pos_x` | int |
| `pos_y` | int |
| `pos_z` | int |
| `start_x` | int |
| `start_y` | int |
| `start_z` | int |

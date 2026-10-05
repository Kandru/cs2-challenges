# EventAddBulletHitMarker (add_bullet_hit_marker)

CSS game event `add_bullet_hit_marker` (`EventAddBulletHitMarker`).

Who gets credit: the **event player(s)** listed under challenge types.

## Challenge types

Put one of these in your task `type:` field:

- `add_bullet_hit_marker`

## Rule keys

> [!WARNING]
> If you use an unknown key, the task will not work because that condition can never be met.

| Key | Type |
|-----|------|
| [global event data](../rules/GlobalEventData.md) | see page |
| [player data](../rules/GlobalPlayerData.md) | prefixes `userid`, `player` |
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

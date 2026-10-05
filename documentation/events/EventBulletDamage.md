# EventBulletDamage (bullet_damage_given / bullet_damage_taken)

CSS game event `bullet_damage` (`EventBulletDamage`).

Who gets credit: the **event player(s)** listed under challenge types.

## Challenge types

Put one of these in your task `type:` field:

- `bullet_damage_given`
- `bullet_damage_taken`

## Rule keys

> [!WARNING]
> If you use an unknown key, the task will not work because that condition can never be met.

| Key | Type |
|-----|------|
| [global event data](../rules/GlobalEventData.md) | see page |
| [player data](../rules/GlobalPlayerData.md) | prefixes `attacker`, `victim` |
| `aim_punch_x` | float |
| `aim_punch_y` | float |
| `aim_punch_z` | float |
| `attack_tick_count` | int |
| `attack_tick_frac` | float |
| `damage_dir_x` | float |
| `damage_dir_y` | float |
| `damage_dir_z` | float |
| `distance` | float |
| `inaccuracy_air` | float |
| `inaccuracy_move` | float |
| `inaccuracy_total` | float |
| `in_air` | bool |
| `attackerinair` | bool |
| `no_scope` | bool |
| `noscope` | bool |
| `num_penetrations` | int |
| `numpenetrations` | int |
| `recoil_index` | float |
| `render_tick_count` | int |
| `render_tick_frac` | float |
| `shoot_ang_x` | float |
| `shoot_ang_y` | float |
| `shoot_ang_z` | float |
| `type` | int |
| `isteamdamage` | bool |
| `isselfdamage` | bool |

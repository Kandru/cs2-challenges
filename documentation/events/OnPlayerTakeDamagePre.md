# OnPlayerTakeDamagePre (on_player_take_damage_pre)

CSS listener `OnPlayerTakeDamagePre`.

Who gets credit: the **event player(s)** listed under challenge types.

## Challenge types

Put one of these in your task `type:` field:

- `on_player_take_damage_pre`

## Rule keys

> [!WARNING]
> If you use an unknown key, the task will not work because that condition can never be met.

| Key | Type |
|-----|------|
| [global event data](../rules/GlobalEventData.md) | see page |
| [player data](../rules/GlobalPlayerData.md) | prefixes `player` |
| `damage.damage` | float |
| `damage.totalled_damage` | float |
| `damage.bits_damage_type` | string |
| `damage.damage_custom` | int |
| `damage.ammo_type` | int |
| `damage.original_damage` | float |
| `damage.should_bleed` | bool |
| `damage.should_spark` | bool |
| `damage.damage_flags` | string |
| `damage.hit_group_id` | string |
| `damage.num_objects_penetrated` | int |
| `damage.friendly_fire_damage_reduction_ratio` | float |
| `damage.stopped_bullet` | bool |
| `damage.in_take_damage_flow` | bool |

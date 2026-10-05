# OnPlayerTakeDamagePost (on_player_take_damage_post)

CSS listener `OnPlayerTakeDamagePost`.

Who gets credit: the **event player(s)** listed under challenge types.

## Challenge types

Put one of these in your task `type:` field:

- `on_player_take_damage_post`

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
| `result.health_lost` | int |
| `result.health_before` | int |
| `result.damage_dealt` | float |
| `result.pre_modified_damage` | float |
| `result.totalled_health_lost` | int |
| `result.totalled_damage_dealt` | float |
| `result.totalled_pre_modified_damage` | float |
| `result.new_damage_accumulator_value` | float |
| `result.damage_flags` | string |
| `result.was_damage_suppressed` | bool |
| `result.suppress_flinch` | bool |
| `result.override_flinch_hit_group` | string |

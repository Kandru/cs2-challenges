# OnPlayerTakeDamagePre (on_player_take_damage_pre)

CSS listener `OnPlayerTakeDamagePre`.

Who gets credit: the **event player(s)** listed under task types.

## Task types

Put one of these in your task `type:` field:

- `on_player_take_damage_pre`

## Minimal YAML

Save under `blueprints/`. Add rules from the table below — see [Rules](../rules.md).

```yaml
tasks:
  - id: example
    type: on_player_take_damage_pre
    amount: 1
    rules: []
```

## Attributes (rule keys)

Unknown keys never match — see [Rules](../rules.md).

| Key | Type |
|-----|------|
| [global event data](../../rules/GlobalEventData.md) | see page |
| [player data](../../rules/GlobalPlayerData.md) | prefixes `player` |
| `player.grenade` | string |
| `player.grenade.holding` | string |
| `player.grenade.thrown` | string |
| `player.grenade.throwing` | string |
| `player.insmoke` | bool |
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

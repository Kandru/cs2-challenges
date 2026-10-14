# EventPlayerAvengedTeammate (player_has_avenged_teammate / player_got_avenged_teammate)

CSS game event `player_avenged_teammate` (`EventPlayerAvengedTeammate`).

Who gets credit: the **event player(s)** listed under task types.

## Task types

Put one of these in your task `type:` field:

- `player_has_avenged_teammate`
- `player_got_avenged_teammate`

## Minimal YAML

Save under `blueprints/`. Add rules from the table below — see [Rules](../rules.md).

```yaml
tasks:
  - id: example
    type: player_has_avenged_teammate
    amount: 1
    rules: []
```

## Attributes (rule keys)

Unknown keys never match — see [Rules](../rules.md).

| Key | Type |
|-----|------|
| [global event data](../../rules/GlobalEventData.md) | see page |
| [player data](../../rules/GlobalPlayerData.md) | prefixes `avenged_player_id`, `victim`, `avenger_id`, `avenger` |
| `isselfavenged` | bool |
| `avenged_player_id.grenade` | string |
| `avenged_player_id.grenade.holding` | string |
| `avenged_player_id.grenade.thrown` | string |
| `avenged_player_id.grenade.throwing` | string |
| `avenged_player_id.insmoke` | bool |
| `victim.grenade` | string |
| `victim.grenade.holding` | string |
| `victim.grenade.thrown` | string |
| `victim.grenade.throwing` | string |
| `victim.insmoke` | bool |
| `avenger_id.grenade` | string |
| `avenger_id.grenade.holding` | string |
| `avenger_id.grenade.thrown` | string |
| `avenger_id.grenade.throwing` | string |
| `avenger_id.insmoke` | bool |
| `avenger.grenade` | string |
| `avenger.grenade.holding` | string |
| `avenger.grenade.thrown` | string |
| `avenger.grenade.throwing` | string |
| `avenger.insmoke` | bool |

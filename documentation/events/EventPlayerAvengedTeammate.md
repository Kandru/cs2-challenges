# EventPlayerAvengedTeammate (player_has_avenged_teammate / player_got_avenged_teammate)

CSS game event `player_avenged_teammate` (`EventPlayerAvengedTeammate`).

Who gets credit: the **event player(s)** listed under challenge types.

## Challenge types

Put one of these in your task `type:` field:

- `player_has_avenged_teammate`
- `player_got_avenged_teammate`

## Rule keys

> [!WARNING]
> If you use an unknown key, the task will not work because that condition can never be met.

| Key | Type |
|-----|------|
| [global event data](../rules/GlobalEventData.md) | see page |
| [player data](../rules/GlobalPlayerData.md) | prefixes `avenged_player_id`, `victim`, `avenger_id`, `avenger` |
| `isselfavenged` | bool |

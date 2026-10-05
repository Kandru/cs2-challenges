# EventPlayerBlind (player_has_blinded / player_got_blinded)

CSS game event `player_blind` (`EventPlayerBlind`).

Who gets credit: the **event player(s)** listed under challenge types.

## Challenge types

Put one of these in your task `type:` field:

- `player_has_blinded`
- `player_got_blinded`

## Rule keys

> [!WARNING]
> If you use an unknown key, the task will not work because that condition can never be met.

| Key | Type |
|-----|------|
| [global event data](../rules/GlobalEventData.md) | see page |
| [player data](../rules/GlobalPlayerData.md) | prefixes `attacker`, `userid`, `victim` |
| `blind_duration` | float |
| `blindduration` | float |
| `entityid` | int |
| `isteamflash` | bool |
| `isselfflash` | bool |

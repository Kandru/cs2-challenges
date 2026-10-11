# PlayerTrade (player_trade)

Synthetic challenge type from situation memory (two `player_death` events).

Who gets credit: the **event player(s)** listed under challenge types.

## Challenge types

Put one of these in your task `type:` field:

- `player_trade`

## Rule keys

> [!WARNING]
> If you use an unknown key, the task will not work because that condition can never be met.

| Key | Type |
|-----|------|
| [global event data](../rules/GlobalEventData.md) | see page |
| [player data](../rules/GlobalPlayerData.md) | prefixes `attacker`, `victim`, `userid` |
| `seconds` | float |
| `weapon` | string |
| `other_weapon` | string |
| `same_weapon` | bool |
| `headshot` | bool |
| `other_headshot` | bool |
| `isteamkill` | bool |
| `isselfkill` | bool |

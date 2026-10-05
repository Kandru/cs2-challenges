# EventPlayerHurt (player_hurt_attacker / player_hurt_victim)

CSS game event `player_hurt` (`EventPlayerHurt`).

Who gets credit: the **event player(s)** listed under challenge types.

## Challenge types

Put one of these in your task `type:` field:

- `player_hurt_attacker`
- `player_hurt_victim`

## Rule keys

> [!WARNING]
> If you use an unknown key, the task will not work because that condition can never be met.

| Key | Type |
|-----|------|
| [global event data](../rules/GlobalEventData.md) | see page |
| [player data](../rules/GlobalPlayerData.md) | prefixes `attacker`, `userid`, `victim` |
| `armor` | int |
| `dmg_armor` | int |
| `dmgarmor` | int |
| `dmg_health` | int |
| `dmghealth` | int |
| `health` | int |
| `hitgroup` | int |
| `weapon` | string |
| `isteamdamage` | bool |
| `isselfdamage` | bool |

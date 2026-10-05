# EventPlayerDeath (player_kill / player_kill_assist / player_death)

CSS game event `player_death` (`EventPlayerDeath`).

Who gets credit: the **event player(s)** listed under challenge types.

## Challenge types

Put one of these in your task `type:` field:

- `player_kill`
- `player_kill_assist`
- `player_death`

## Rule keys

> [!WARNING]
> If you use an unknown key, the task will not work because that condition can never be met.

| Key | Type |
|-----|------|
| [global event data](../rules/GlobalEventData.md) | see page |
| [player data](../rules/GlobalPlayerData.md) | prefixes `assister`, `attacker`, `userid`, `victim` |
| `assistedflash` | bool |
| `attackerblind` | bool |
| `attackerinair` | bool |
| `distance` | float |
| `dmg_armor` | int |
| `dmgarmor` | int |
| `dmg_health` | int |
| `dmghealth` | int |
| `dominated` | bool |
| `headshot` | bool |
| `hitgroup` | int |
| `noreplay` | bool |
| `noscope` | bool |
| `penetrated` | bool |
| `revenge` | bool |
| `thrusmoke` | bool |
| `weapon` | string |
| `weapon_fauxitemid` | string |
| `weapon_itemid` | string |
| `weaponitemid` | string |
| `weapon_originalowner_xuid` | string |
| `wipe` | int |
| `isteamkill` | bool |
| `isselfkill` | bool |

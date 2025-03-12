# OnPlayerDeath (player_kill / player_kill_assist / player_death)

This blueprint is triggered when a player kills someone, helped with the kill (assist) or got killed.

## Available rules

- [Event Data](../rules/GlobalEventData.md)
- [Player Data](../rules/GlobalPlayerData.md): with prefixex: *attacker*, *assister*, *victim*
- `isteamkill (bool)`: if this is a team kill
- `isselfkill (bool)`: if the player killed himself
- `assistedflash (bool)`: if the assister used a flash to help
- `attackerblind (bool)`: if the attacker is blind
- `attackerinair (bool)`: if the attacker is in the air
- `distance (number)`: distance between attacker and victim
- `dmghealth (number)`: amount damage to the health
- `dmgarmor (number)`: amount damage to the armor
- `dominated (number)`: if this was a domination (TODO: list of possible values)
- `headshot (bool)`: if the kill was a headshot
- `hitgroup (number)`: the hitgroup of the bullet (TODO: list of possible hitgroups)
- `noscope (bool)`: if the kill was done noscope
- `penetrated (number)`: if the bullet penetrated (TODO: list of possible values)
- `revenge (number)`: if this was a revenge (TODO: list of possible values)
- `thrusmoke (bool)`: if the kill was through the smoke
- `weapon (string)`: name of the weapon (TODO: list of possible values)
- `weaponitemid (string)`: weapon item id (TODO: list of possible values)

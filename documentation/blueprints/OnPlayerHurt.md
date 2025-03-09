# OnPlayerHurt (player_hurt_attacker / player_hurt_victim)

This blueprint is triggered when the attacker hurt someone or the victim got hurt by an attacker.

> [!CAUTION]
> This event will trigger offen and could slow down the server with many players online!

## Available rules

- [Event Data](GlobalEventData.md)
- [Player Data](GlobalPlayerData.md): with prefix: *attacker*, *victim*
- `isteamdamage (bool)`: if this is team damage
- `isselfdamage (bool)`: if the player damaged himself
- `dmghealth (bool)`: amount damage to the health
- `dmgarmor (bool)`: amount damage to the armor
- `health (bool)`: health left
- `armor (bool)`: armor left
- `hitgroup (bool)`: the hitgroup of the bullet (TODO: list of possible hitgroups)
- `weapon (bool)`: name of the weapon (TODO: list of possible values)

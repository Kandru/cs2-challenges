# OnBulletDamage (bullet_damage_given / bullet_damage_taken)

This blueprint is triggered when either bullet damage was given or taken.

## Available rules

- [Event Data](../rules/GlobalEventData.md)
- [Player Data](../rules/GlobalPlayerData.md): with prefixes: *attacker* and *victim*
- `isteamdamage (bool)`: if this is team damage
- `isselfdamage (bool)`: if this is self damage
- `attackerinair (bool)`: if the *attacker* was in the air
- `distance (number)`: the distance of the shot
- `noscope (bool)`: if this damage was caused noscope
- `numpenetrations (number)`: amount of penetrations

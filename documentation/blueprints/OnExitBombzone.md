# OnExitBombzone (exit_bombzone)

This blueprint is triggered when a player exits a bomb zone.

> [!CAUTION]
> Depending on the workshop map (default maps should be no problem) this event will trigger very often. This could slow down the server with many players online!

## Available rules

- [Event Data](GlobalEventData.md)
- [Player Data](GlobalPlayerData.md): with prefix: *player*
- `hasbomb (bool)`: if the player has the bomb
- `isplanted (bool)`: if the bomb has already been panted

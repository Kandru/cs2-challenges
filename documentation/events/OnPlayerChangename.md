# OnPlayerChangename (player_changed_name)

This blueprint is triggered when a player changed his name.

> [!IMPORTANT]  
> This event does not seem to get triggered because a simple name change via steam is ignored until reconnection.

## Available rules

- [Event Data](GlobalEventData.md)
- [Player Data](GlobalPlayerData.md): with prefix: *player*
- `new_name (string)`: new name of the player
- `old_name (string)`: old name of the player

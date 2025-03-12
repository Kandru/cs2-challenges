# OnPlayerTeam (player_team)

This blueprint is triggered when a player switches a team.

## Available rules

- [Event Data](../rules/GlobalEventData.md)
- [Player Data](../rules/GlobalPlayerData.md): with prefix: *player*
- `disconnect (bool)`: if the player disconnected
- `silent (bool)`: if the player got swapped silently (via script or auto team balance)
- `old_team (string)`: old team name
- `new_team (string)`: new team name

# OnBombDefused (bomb_defused)

This blueprint is triggered when the bomb has been defused. **Each player on the server will get checked for this event!**.

## Available Parameters

- `isduringround` (bool): Whether a round is currently active
- `defuser` (string): The defuser's name
- `defuser_isbot` (bool): Whether the defuser is a bot
- `defuser_team` (string): The defuser's team name
- `player` (string): The player's name
- `player_isbot` (bool): Whether the player is a bot
- `player_team` (string): The player's team name
- `player_is_defuser` (bool): Whether the player is the defuser
- `bomb_site` (int): bomb site

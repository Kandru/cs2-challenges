# OnBombExploded (bomb_exploded)

This blueprint is triggered when the bomb has exploded. **Each player on the server will get checked for this event!**.

## Available Parameters

- `isduringround` (bool): Whether a round is currently active
- `exploder` (string): The exploder's name
- `exploder_isbot` (bool): Whether the exploder is a bot
- `exploder_team` (string): The exploder's team name
- `player` (string): The player's name
- `player_isbot` (bool): Whether the player is a bot
- `player_team` (string): The player's team name
- `player_is_exploder` (bool): Whether the player is the exploder
- `bomb_site` (int): bomb site

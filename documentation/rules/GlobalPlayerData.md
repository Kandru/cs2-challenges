# Rule: Global Player Data

The player data consists of data of the player which can be used to design rules. This player data is valid for all events which contain player data in various forms regardless if it the prefix is *attacker*, *victim*, *defuser*, *exploder*, ...

## Available rules

Swap *prefix* with the given prefix of the event.

- `prefix.name (string)`: Name of the player
- `prefix.isbot (bool)`: If the player is a bot
- `prefix.team (string)`: Name of the Team of the player (see [CsTeam](../enums/CsTeam.md))
- `prefix.alive (bool)`: If the player is alive or not
- `prefix.ping (number)`: The current ping
- `prefix.money (number)`: The current money
- `prefix.score (score)`: The current score
- `prefix.stats.kills (number)`: The amount of kills
- `prefix.stats.assists (number)`: The amount of assists
- `prefix.stats.deaths (string)`: The amount of deaths
- `prefix.stats.damage (string)`: The total amount of damage the player dealt
- `prefix.health (number)`: The amount of health
- `prefix.armor (number)`: The amount of armor
- `prefix.hasdefusor (bool)`: If the player has a defusor
- `prefix.hashelmet (bool)`: If the player has a helmet

# Rule: Global Event Data

The global event data consists of data which is applicable for (almost) all events.

## Available rules

- `global.iswarmup (bool)`: If the warmup mode is currently active
- `global.isduringround (bool)`: If the round is currently active (both warmup and game rounds are counted)
- `global.mapname (string)`: The name of the current map. The challenge will be invisible to the player if the map is not matching (Operator can either be == or !=)

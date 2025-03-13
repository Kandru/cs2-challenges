> [!TIP]
> Blueprints can be complex. Please check the *examples* folder in this repository before asking questions in our Discord.

# Blueprints Documentation

Blueprints are files which contain definitions for in-game challenges that players can see and complete.

## Creating a Blueprint File

To create your first challenge, follow these steps:

1. Create a new file with a `.yaml` extension (e.g., `example.yaml`).
2. Place this file in the `blueprints` folder of the Challenges-Plugin.
3. You can create multiple files and organize them as you prefer (e.g., one file per blueprint type or plugin).

Each blueprint file must contain at least one challenge. A challenge is defined by an event type and various settings. Each challenge needs a unique name within the same blueprint file. The Challenges-Plugin will automatically generate a global identifier for each challenge.

For example:
- Blueprint filename: `example.yaml`
- Unique Challenge name: `YourUniqueChallengeName`
- Resulting global identifier: `example:YourUniqueChallengeName`

When the blueprint file is loaded into the Challenges-Plugin, it will be referenced by this global identifier. The **":"** is reserved within the plugin. Do **NOT** use it for any challenge name or blueprint filename. Only use it *once* to set the blueprint filename of your identifier within the *actions* and/or *dependencies* when referencing another file.

Each YAML file should be structured as follows and can contain multiple blueprints, each with a unique name:

```yaml
YourUniqueChallengeName:
  title:
    en: "My Unique Challenge ({count}/{total})"
    de: "Meine einzigartige Herausforderung ({count}/{total})"
  type: player_kill
  amount: 10
  cooldown: 0
  is_visible: true
  announce_progress: true
  announce_completion: true
  data:
    ExamplePlugin:
      setpoints: "30"
  rules:
    - key: global.iswarmup
      operator: bool==
      value: "false"
    - key: global.isduringround
      operator: bool==
      value: "true"
    - key: weapon
      operator: contains
      value: taser
    - key: isteamkill
      operator: bool==
      value: "false"
    - key: victim.isbot
      operator: bool==
      value: "false"
  actions:
  dependencies:
```

### title

The title is what the player sees in the upper right corner of the GUI by default or in the text chat when progress is made or the challenge is completed. You can use the placeholders *{count}* and *{total}* to show the current and required number of events needed to complete the challenge.

The default language is the player's language (changeable with !lang en/de/...). If that language is not available, it will fall back to the server's language (set this default in the CounterstrikeSharp settings json). If neither is available, the first entry in the list will be used.

### type

The type is the event that triggers this blueprint. In our example, we use the *player_jump* event. This means that every time a player jumps, this challenge will be activated.

### amount

The number of times this event must occur to complete the challenge. Choose a reasonable number based on the duration the blueprint will be active. For example, if the challenge lasts only 24 hours, the required amount should not exceed 1,000. Jumping 1,000 times in regular gameplay is difficult, and players should focus on playing rather than just jumping.

### cooldown

The cooldown is the time in seconds that must pass before the event can be counted again. For example, if you set the cooldown to 10 seconds, a player can only trigger the event once every 10 seconds. This can make the challenge more difficult by limiting how often the event can occur.

### is_visible

Whether this challenge should be visible to the player. If set to false, the challenge can still be completed, but the player will not see any notifications or progress updates.

### announce_progress

Whether you want to notify the player about their progress. This setting does not affect notifications sent to third-party plugins.

### announce_completion

Whether you want to notify the player when this challenge is completed. This setting does not affect notifications sent to third-party plugins.

### data

This section contains a dictionary of strings, where each string represents data to be sent to a third-party plugin. The Challenges-Plugin itself does not manage the actions taken when a player completes a challenge. You can include multiple plugins here. If your preferred plugin does not support the Challenges-Plugin, you can request the plugin developer to add support. We provide documentation and an example plugin to facilitate easy integration.

### rules

Rules make the Challenges-Plugin very powerful. Almost all events have parameters that you can compare against values you choose. In our example, we check if there is an active round. If not, we ignore this challenge until a round starts. You can also check for specific weapons, distances, teams, and more.

### actions

Actions modify challenges of your choice after completion. Please refer to the actions documentation for further information.

### dependencies

Dependencies are conditions that must be met before a challenge becomes available to the player. These conditions are other challenges that the player must complete first. This allows you to create a series of challenges with increasing difficulty.

To set a dependency, list the unique name of the required challenge. If the required challenge is in the same file, just use its unique name. For example, *YourUniqueChallengeName*. If the required challenge is in a different file, include the filename. For example, *example:YourUniqueChallengeName*.

**Important**: Challenges are executed in the order they appear, from top to bottom. If you have multiple challenges of the same type (e.g., *player_kill*) and they depend on each other, you should list them from the most dependent (bottom) to the least dependent (top). This way, the next challenge won't trigger prematurely.

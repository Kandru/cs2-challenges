# Plugin integration

This plugin tracks challenges. It does not grant money, items, or store credits by itself. Use one of the paths below.

## Server commands

On task completion, a blueprint can run a server console command. Any other plugin that exposes a console command can be wired this way - no C# reference to Challenges is required.

Action type: `server.runcommand` (see [Actions](actions.md)).

- Runs when the task reaches its `amount`.
- Uses the **first** string in `values`.
- Placeholders (replaced for the player who completed the task):

| Placeholder | Value |
|-------------|--------|
| `{steamid}` | Player `NetworkIDString` |
| `{userid}` | Player user id (empty string if missing) |
| `{index}` | Entity index (`player.Index`) |

Example - give store credits when a task finishes (replace the command with whatever your store plugin documents):

```yaml
title:
  en: AK-47 challenge
tasks:
  - id: kills
    title:
      en: "{count}/{total} AK kills"
    type: player_kill
    amount: 10
    rules:
      - key: weapon
        operator: contains
        value: "ak47"
    actions:
      - type: server.runcommand
        values:
          - "css_store_give {steamid} 100"
```

You can add several `server.runcommand` actions on the same task; each runs as its own console command.

<a id="challenge-data"></a>

## Blueprint `data`

Optional nested map on the challenge or on a task. Top-level keys are usually your plugin name. Values are strings in YAML; parse them in your plugin with a fallback if they are wrong.

```yaml
data:
  MyStorePlugin:
    credits: "100"
```

- Task `data` is attached to that task’s progress and completion events.
- Challenge `data` is attached when the whole challenge is solved.
- A group named `info` is never merged (reserved for the built-in payload below).

## For plugin authors

1. Reference `ChallengesShared` from this repository (also installed under `addons/counterstrikesharp/shared/ChallengesShared/`).
2. Resolve the `challenges:events` capability (`IChallengesEventSender`).
3. Subscribe to `Events`. You get progress and completion on that one handler.

Handlers run on the game thread - keep work short or offload it.

Event types:

| Event | When |
|-------|------|
| `PlayerProgressedChallengeEvent` | A task gained progress but is not finished |
| `PlayerCompletedChallengeEvent` | A task finished, **or** every visible task of a challenge finished |

Both carry `UserId` (resolve the `CCSPlayerController` yourself) and `Data`: `Dictionary<string, Dictionary<string, string>>`.

### Built-in `info` group

Every event includes an `info` map:

**Task progress** (`PlayerProgressedChallengeEvent`):

| Key | Meaning |
|-----|---------|
| `title` | Localized task (or challenge) title with `{count}` / `{total}` expanded |
| `type` | Task `type` |
| `challenge` | Challenge id |
| `task` | Task id |
| `cooldown` | Task cooldown as string |
| `current_amount` | Progress after this event |
| `total_amount` | Task `amount` |

**Task completed** (`PlayerCompletedChallengeEvent` for a task):

Same as above, except `amount` (goal) instead of `current_amount` / `total_amount`.

**Challenge solved** (second `PlayerCompletedChallengeEvent` when every visible task is done):

| Key | Meaning |
|-----|---------|
| `title` | Localized challenge title |
| `type` | Empty |
| `challenge` | Challenge id |
| `task` | Empty |
| `amount` | Number of visible tasks |
| `cooldown` | `"0"` |

### Example listener

See [src/ExampleEventListenerPlugin/ExampleEventListenerPlugin.cs](../src/ExampleEventListenerPlugin/ExampleEventListenerPlugin.cs) in this repository for a minimal subscribe/unsubscribe sample.

When your integration is tested, open a pull request against this README to mention your plugin, and link back to Challenges from your own docs.

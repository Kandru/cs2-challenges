# Plugin API

This page covers the C# integration surface. For wiring rewards with server console commands and YAML `data`, see [Rewards](../howto/rewards.md).

## For plugin authors

1. Reference `ChallengesShared` from this repository (also installed under `addons/counterstrikesharp/shared/ChallengesShared/`).
2. Resolve the `challenges:events` capability (`IChallengesEventSender`).
3. Subscribe to `Events`. You get progress and completion on that one handler.

Handlers run on the game thread — keep work short or offload it.

Event types:

| Event | When |
|-------|------|
| `PlayerProgressedChallengeEvent` | A task gained progress but is not finished |
| `PlayerCompletedChallengeEvent` | A task finished, **or** every visible task of a challenge finished |

Both carry `UserId` (resolve the `CCSPlayerController` yourself) and `Data`: `Dictionary<string, Dictionary<string, string>>`.

Optional challenge and task `data` maps from YAML are merged into `Data`. A group named `info` is never overwritten by YAML (reserved below). See [Challenge data](../howto/rewards.md#challenge-data) and [Task data](../howto/rewards.md#task-data).

## Built-in `info` group

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

## Example listener

See [src/ExampleEventListenerPlugin/ExampleEventListenerPlugin.cs](https://github.com/Kandru/cs2-challenges/blob/main/src/ExampleEventListenerPlugin/ExampleEventListenerPlugin.cs) in this repository for a minimal subscribe/unsubscribe sample.

When your integration is tested, open a pull request against this README to mention your plugin, and link back to Challenges from your own docs.

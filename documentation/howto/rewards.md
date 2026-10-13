# Rewards

This plugin tracks challenges. It does not grant money, items, or store credits by itself. Use a server console command on task completion, or subscribe to the C# events from another plugin ([Plugin API](../reference/integration.md)).

## Server commands

On task completion, a challenge file can run a server console command. Any other plugin that exposes a console command can be wired this way — no C# reference to Challenges is required.

Action type: `server.runcommand` (see [Actions](../reference/actions.md#server-runcommand)).

- Runs when the task reaches its `amount`.
- Uses the **first** string in `values`.
- Placeholders (replaced for the player who completed the task):

| Placeholder | Value |
|-------------|--------|
| `{steamid}` | Player `NetworkIDString` |
| `{userid}` | Player user id (empty string if missing) |
| `{index}` | Entity index (`player.Index`) |

Example — give store credits when a task finishes (replace the command with whatever your store plugin documents):

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

## Challenge data {#challenge-data}

Optional nested map on the **challenge** (top level of the YAML file). Top-level keys are usually your plugin name. Values are strings in YAML; parse them in your plugin with a fallback if they are wrong.

```yaml
title:
  en: AK-47 challenge
data:
  MyStorePlugin:
    credits: "100"
tasks:
  - id: kills
    type: player_kill
    amount: 10
```

Challenge `data` is attached when the **whole challenge** is solved (every visible task done). A group named `info` is never merged (reserved for the built-in payload in the [Plugin API](../reference/integration.md)).

## Task data {#task-data}

Optional nested map on a **task**. Same shape as challenge data.

```yaml
tasks:
  - id: kills
    type: player_kill
    amount: 10
    data:
      MyStorePlugin:
        credits: "25"
```

Task `data` is attached to that task’s progress and completion events. A group named `info` is never merged.

## C# events

For subscribe/unsubscribe and the built-in `info` payload, see [Plugin API](../reference/integration.md).

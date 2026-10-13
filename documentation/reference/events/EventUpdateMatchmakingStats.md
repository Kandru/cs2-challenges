# EventUpdateMatchmakingStats (update_matchmaking_stats)

CSS game event `update_matchmaking_stats` (`EventUpdateMatchmakingStats`).

Who gets credit: **all connected players**.

## Task types

Put one of these in your task `type:` field:

- `update_matchmaking_stats`

## Minimal YAML

Save under `blueprints/`. Add rules from the table below — see [Rules](../rules.md).

```yaml
tasks:
  - id: example
    type: update_matchmaking_stats
    amount: 1
    rules: []
```

## Attributes (rule keys)

Unknown keys never match — see [Rules](../rules.md).

| Key | Type |
|-----|------|
| [global event data](../../rules/GlobalEventData.md) | see page |

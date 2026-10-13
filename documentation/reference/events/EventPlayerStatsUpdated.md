# EventPlayerStatsUpdated (player_stats_updated)

CSS game event `player_stats_updated` (`EventPlayerStatsUpdated`).

Who gets credit: **all connected players**.

## Task types

Put one of these in your task `type:` field:

- `player_stats_updated`

## Minimal YAML

Save under `blueprints/`. Add rules from the table below — see [Rules](../rules.md).

```yaml
tasks:
  - id: example
    type: player_stats_updated
    amount: 1
    rules: []
```

## Attributes (rule keys)

Unknown keys never match — see [Rules](../rules.md).

| Key | Type |
|-----|------|
| [global event data](../../rules/GlobalEventData.md) | see page |
| `forceupload` | bool |

# EventSwitchTeam (switch_team)

CSS game event `switch_team` (`EventSwitchTeam`).

Who gets credit: **all connected players**.

## Task types

Put one of these in your task `type:` field:

- `switch_team`

## Minimal YAML

Save under `blueprints/`. Add rules from the table below — see [Rules](../rules.md).

```yaml
tasks:
  - id: example
    type: switch_team
    amount: 1
    rules: []
```

## Attributes (rule keys)

Unknown keys never match — see [Rules](../rules.md).

| Key | Type |
|-----|------|
| [global event data](../../rules/GlobalEventData.md) | see page |
| `avg_rank` | int |
| `numCTSlotsFree` | int |
| `numPlayers` | int |
| `numSpectators` | int |
| `numTSlotsFree` | int |

# EventTeamScore (team_score)

CSS game event `team_score` (`EventTeamScore`).

Who gets credit: **all connected players**.

## Task types

Put one of these in your task `type:` field:

- `team_score`

## Minimal YAML

Save under `blueprints/`. Add rules from the table below — see [Rules](../rules.md).

```yaml
tasks:
  - id: example
    type: team_score
    amount: 1
    rules: []
```

## Attributes (rule keys)

Unknown keys never match — see [Rules](../rules.md).

| Key | Type |
|-----|------|
| [global event data](../../rules/GlobalEventData.md) | see page |
| `score` | int |
| `teamid` | int |

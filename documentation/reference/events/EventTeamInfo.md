# EventTeamInfo (team_info)

CSS game event `team_info` (`EventTeamInfo`).

Who gets credit: **all connected players**.

## Task types

Put one of these in your task `type:` field:

- `team_info`

## Minimal YAML

Save under `blueprints/`. Add rules from the table below — see [Rules](../rules.md).

```yaml
tasks:
  - id: example
    type: team_info
    amount: 1
    rules: []
```

## Attributes (rule keys)

Unknown keys never match — see [Rules](../rules.md).

| Key | Type |
|-----|------|
| [global event data](../../rules/GlobalEventData.md) | see page |
| `teamid` | int |
| `teamname` | string |

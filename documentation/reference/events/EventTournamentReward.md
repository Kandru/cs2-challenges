# EventTournamentReward (tournament_reward)

CSS game event `tournament_reward` (`EventTournamentReward`).

Who gets credit: **all connected players**.

## Task types

Put one of these in your task `type:` field:

- `tournament_reward`

## Minimal YAML

Save under `blueprints/`. Add rules from the table below — see [Rules](../rules.md).

```yaml
tasks:
  - id: example
    type: tournament_reward
    amount: 1
    rules: []
```

## Attributes (rule keys)

Unknown keys never match — see [Rules](../rules.md).

| Key | Type |
|-----|------|
| [global event data](../../rules/GlobalEventData.md) | see page |
| `accountid` | int |
| `defindex` | int |
| `totalrewards` | int |

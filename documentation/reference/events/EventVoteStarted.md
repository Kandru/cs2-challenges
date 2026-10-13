# EventVoteStarted (vote_started)

CSS game event `vote_started` (`EventVoteStarted`).

Who gets credit: **all connected players**.

## Task types

Put one of these in your task `type:` field:

- `vote_started`

## Minimal YAML

Save under `blueprints/`. Add rules from the table below — see [Rules](../rules.md).

```yaml
tasks:
  - id: example
    type: vote_started
    amount: 1
    rules: []
```

## Attributes (rule keys)

Unknown keys never match — see [Rules](../rules.md).

| Key | Type |
|-----|------|
| [global event data](../../rules/GlobalEventData.md) | see page |
| `initiator` | int |
| `issue` | string |
| `param1` | string |
| `reliable` | int |
| `team` | int |
| `votedata` | string |

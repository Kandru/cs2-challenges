# EventVoteChanged (vote_changed)

CSS game event `vote_changed` (`EventVoteChanged`).

Who gets credit: **all connected players**.

## Task types

Put one of these in your task `type:` field:

- `vote_changed`

## Minimal YAML

Save under `blueprints/`. Add rules from the table below — see [Rules](../rules.md).

```yaml
tasks:
  - id: example
    type: vote_changed
    amount: 1
    rules: []
```

## Attributes (rule keys)

Unknown keys never match — see [Rules](../rules.md).

| Key | Type |
|-----|------|
| [global event data](../../rules/GlobalEventData.md) | see page |
| `noVotes` | int |
| `potentialVotes` | int |
| `vote_option1` | int |
| `vote_option2` | int |
| `vote_option3` | int |
| `vote_option4` | int |
| `vote_option5` | int |
| `yesVotes` | int |

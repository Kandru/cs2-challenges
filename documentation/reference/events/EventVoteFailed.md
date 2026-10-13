# EventVoteFailed (vote_failed)

CSS game event `vote_failed` (`EventVoteFailed`).

Who gets credit: **all connected players**.

## Task types

Put one of these in your task `type:` field:

- `vote_failed`

## Minimal YAML

Save under `blueprints/`. Add rules from the table below — see [Rules](../rules.md).

```yaml
tasks:
  - id: example
    type: vote_failed
    amount: 1
    rules: []
```

## Attributes (rule keys)

Unknown keys never match — see [Rules](../rules.md).

| Key | Type |
|-----|------|
| [global event data](../../rules/GlobalEventData.md) | see page |
| `reliable` | int |
| `team` | int |

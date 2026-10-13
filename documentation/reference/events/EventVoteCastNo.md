# EventVoteCastNo (vote_cast_no)

CSS game event `vote_cast_no` (`EventVoteCastNo`).

Who gets credit: **all connected players**.

## Task types

Put one of these in your task `type:` field:

- `vote_cast_no`

## Minimal YAML

Save under `blueprints/`. Add rules from the table below — see [Rules](../rules.md).

```yaml
tasks:
  - id: example
    type: vote_cast_no
    amount: 1
    rules: []
```

## Attributes (rule keys)

Unknown keys never match — see [Rules](../rules.md).

| Key | Type |
|-----|------|
| [global event data](../../rules/GlobalEventData.md) | see page |
| `entityid` | int |
| `team` | int |

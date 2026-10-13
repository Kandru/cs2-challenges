# EventVoteCastYes (vote_cast_yes)

CSS game event `vote_cast_yes` (`EventVoteCastYes`).

Who gets credit: **all connected players**.

## Task types

Put one of these in your task `type:` field:

- `vote_cast_yes`

## Minimal YAML

Save under `blueprints/`. Add rules from the table below — see [Rules](../rules.md).

```yaml
tasks:
  - id: example
    type: vote_cast_yes
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

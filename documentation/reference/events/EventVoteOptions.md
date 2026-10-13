# EventVoteOptions (vote_options)

CSS game event `vote_options` (`EventVoteOptions`).

Who gets credit: **all connected players**.

## Task types

Put one of these in your task `type:` field:

- `vote_options`

## Minimal YAML

Save under `blueprints/`. Add rules from the table below — see [Rules](../rules.md).

```yaml
tasks:
  - id: example
    type: vote_options
    amount: 1
    rules: []
```

## Attributes (rule keys)

Unknown keys never match — see [Rules](../rules.md).

| Key | Type |
|-----|------|
| [global event data](../../rules/GlobalEventData.md) | see page |
| `count` | int |
| `option1` | string |
| `option2` | string |
| `option3` | string |
| `option4` | string |
| `option5` | string |

# EventRoundAnnounceMatchPoint (round_announce_match_point)

CSS game event `round_announce_match_point` (`EventRoundAnnounceMatchPoint`).

Who gets credit: **all connected players**.

## Task types

Put one of these in your task `type:` field:

- `round_announce_match_point`

## Minimal YAML

Save under `blueprints/`. Add rules from the table below — see [Rules](../rules.md).

```yaml
tasks:
  - id: example
    type: round_announce_match_point
    amount: 1
    rules: []
```

## Attributes (rule keys)

Unknown keys never match — see [Rules](../rules.md).

| Key | Type |
|-----|------|
| [global event data](../../rules/GlobalEventData.md) | see page |

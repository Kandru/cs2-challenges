# EventBuytimeEnded (buytime_ended)

CSS game event `buytime_ended` (`EventBuytimeEnded`).

Who gets credit: **all connected players**.

## Task types

Put one of these in your task `type:` field:

- `buytime_ended`

## Minimal YAML

Save under `blueprints/`. Add rules from the table below — see [Rules](../rules.md).

```yaml
tasks:
  - id: example
    type: buytime_ended
    amount: 1
    rules: []
```

## Attributes (rule keys)

Unknown keys never match — see [Rules](../rules.md).

| Key | Type |
|-----|------|
| [global event data](../../rules/GlobalEventData.md) | see page |

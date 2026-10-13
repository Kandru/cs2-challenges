# EventBonusUpdated (bonus_updated)

CSS game event `bonus_updated` (`EventBonusUpdated`).

Who gets credit: **all connected players**.

## Task types

Put one of these in your task `type:` field:

- `bonus_updated`

## Minimal YAML

Save under `blueprints/`. Add rules from the table below — see [Rules](../rules.md).

```yaml
tasks:
  - id: example
    type: bonus_updated
    amount: 1
    rules: []
```

## Attributes (rule keys)

Unknown keys never match — see [Rules](../rules.md).

| Key | Type |
|-----|------|
| [global event data](../../rules/GlobalEventData.md) | see page |
| `numadvanced` | int |
| `numbronze` | int |
| `numgold` | int |
| `numsilver` | int |

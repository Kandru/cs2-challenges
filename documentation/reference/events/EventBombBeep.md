# EventBombBeep (bomb_beep)

CSS game event `bomb_beep` (`EventBombBeep`).

Who gets credit: **all connected players**.

## Task types

Put one of these in your task `type:` field:

- `bomb_beep`

## Minimal YAML

Save under `blueprints/`. Add rules from the table below — see [Rules](../rules.md).

```yaml
tasks:
  - id: example
    type: bomb_beep
    amount: 1
    rules: []
```

## Attributes (rule keys)

Unknown keys never match — see [Rules](../rules.md).

| Key | Type |
|-----|------|
| [global event data](../../rules/GlobalEventData.md) | see page |
| `entindex` | int |

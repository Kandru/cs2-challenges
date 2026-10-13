# EventNextlevelChanged (nextlevel_changed)

CSS game event `nextlevel_changed` (`EventNextlevelChanged`).

Who gets credit: **all connected players**.

## Task types

Put one of these in your task `type:` field:

- `nextlevel_changed`

## Minimal YAML

Save under `blueprints/`. Add rules from the table below — see [Rules](../rules.md).

```yaml
tasks:
  - id: example
    type: nextlevel_changed
    amount: 1
    rules: []
```

## Attributes (rule keys)

Unknown keys never match — see [Rules](../rules.md).

| Key | Type |
|-----|------|
| [global event data](../../rules/GlobalEventData.md) | see page |
| `mapgroup` | string |
| `nextlevel` | string |
| `skirmishmode` | string |

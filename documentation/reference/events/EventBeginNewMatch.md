# EventBeginNewMatch (begin_new_match)

CSS game event `begin_new_match` (`EventBeginNewMatch`).

Who gets credit: **all connected players**.

## Task types

Put one of these in your task `type:` field:

- `begin_new_match`

## Minimal YAML

Save under `blueprints/`. Add rules from the table below — see [Rules](../rules.md).

```yaml
tasks:
  - id: example
    type: begin_new_match
    amount: 1
    rules: []
```

## Attributes (rule keys)

Unknown keys never match — see [Rules](../rules.md).

| Key | Type |
|-----|------|
| [global event data](../../rules/GlobalEventData.md) | see page |

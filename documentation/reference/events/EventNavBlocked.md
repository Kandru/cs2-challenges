# EventNavBlocked (nav_blocked)

CSS game event `nav_blocked` (`EventNavBlocked`).

Who gets credit: **all connected players**.

## Task types

Put one of these in your task `type:` field:

- `nav_blocked`

## Minimal YAML

Save under `blueprints/`. Add rules from the table below — see [Rules](../rules.md).

```yaml
tasks:
  - id: example
    type: nav_blocked
    amount: 1
    rules: []
```

## Attributes (rule keys)

Unknown keys never match — see [Rules](../rules.md).

| Key | Type |
|-----|------|
| [global event data](../../rules/GlobalEventData.md) | see page |
| `area` | int |
| `blocked` | bool |

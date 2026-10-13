# EventHostageRescued (hostage_rescued)

CSS game event `hostage_rescued` (`EventHostageRescued`).

Who gets credit: **all connected players**.

## Task types

Put one of these in your task `type:` field:

- `hostage_rescued`

## Minimal YAML

Save under `blueprints/`. Add rules from the table below — see [Rules](../rules.md).

```yaml
tasks:
  - id: example
    type: hostage_rescued
    amount: 1
    rules: []
```

## Attributes (rule keys)

Unknown keys never match — see [Rules](../rules.md).

| Key | Type |
|-----|------|
| [global event data](../../rules/GlobalEventData.md) | see page |
| [player data](../../rules/GlobalPlayerData.md) | prefixes `userid`, `player` |
| `hostage` | int |
| `site` | int |
| `rescue_site` | int |

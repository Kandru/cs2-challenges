# EventAchievementInfoLoaded (achievement_info_loaded)

CSS game event `achievement_info_loaded` (`EventAchievementInfoLoaded`).

Who gets credit: **all connected players**.

## Task types

Put one of these in your task `type:` field:

- `achievement_info_loaded`

## Minimal YAML

Save under `blueprints/`. Add rules from the table below — see [Rules](../rules.md).

```yaml
tasks:
  - id: example
    type: achievement_info_loaded
    amount: 1
    rules: []
```

## Attributes (rule keys)

Unknown keys never match — see [Rules](../rules.md).

| Key | Type |
|-----|------|
| [global event data](../../rules/GlobalEventData.md) | see page |

# EventRepostXboxAchievements (repost_xbox_achievements)

CSS game event `repost_xbox_achievements` (`EventRepostXboxAchievements`).

Who gets credit: **all connected players**.

## Task types

Put one of these in your task `type:` field:

- `repost_xbox_achievements`

## Minimal YAML

Save under `blueprints/`. Add rules from the table below — see [Rules](../rules.md).

```yaml
tasks:
  - id: example
    type: repost_xbox_achievements
    amount: 1
    rules: []
```

## Attributes (rule keys)

Unknown keys never match — see [Rules](../rules.md).

| Key | Type |
|-----|------|
| [global event data](../../rules/GlobalEventData.md) | see page |
| `splitscreenplayer` | int |

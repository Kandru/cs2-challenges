# EventBotTakeover (player_bot_takeover)

CSS game event `bot_takeover` (`EventBotTakeover`).

Who gets credit: the **event player(s)** listed under task types.

## Task types

Put one of these in your task `type:` field:

- `player_bot_takeover`

## Minimal YAML

Save under `blueprints/`. Add rules from the table below — see [Rules](../rules.md).

```yaml
tasks:
  - id: example
    type: player_bot_takeover
    amount: 1
    rules: []
```

## Attributes (rule keys)

Unknown keys never match — see [Rules](../rules.md).

| Key | Type |
|-----|------|
| [global event data](../../rules/GlobalEventData.md) | see page |
| [player data](../../rules/GlobalPlayerData.md) | prefixes `botid`, `userid`, `player` |
| `p` | float |
| `r` | float |
| `y` | float |
| `botid.grenade` | string |
| `botid.grenade.holding` | string |
| `botid.grenade.thrown` | string |
| `botid.grenade.throwing` | string |
| `botid.insmoke` | bool |
| `userid.grenade` | string |
| `userid.grenade.holding` | string |
| `userid.grenade.thrown` | string |
| `userid.grenade.throwing` | string |
| `userid.insmoke` | bool |
| `player.grenade` | string |
| `player.grenade.holding` | string |
| `player.grenade.thrown` | string |
| `player.grenade.throwing` | string |
| `player.insmoke` | bool |

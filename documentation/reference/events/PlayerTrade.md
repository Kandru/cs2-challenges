# PlayerTrade (player_trade)

Synthetic task type from situation memory (two `player_death` events).

Who gets credit: the **event player(s)** listed under task types.

## Task types

Put one of these in your task `type:` field:

- `player_trade`

## Minimal YAML

Save under `blueprints/`. Add rules from the table below — see [Rules](../rules.md).

```yaml
tasks:
  - id: example
    type: player_trade
    amount: 1
    rules: []
```

## Attributes (rule keys)

Unknown keys never match — see [Rules](../rules.md).

| Key | Type |
|-----|------|
| [global event data](../../rules/GlobalEventData.md) | see page |
| [player data](../../rules/GlobalPlayerData.md) | prefixes `attacker`, `victim`, `userid` |
| `seconds` | float |
| `weapon` | string |
| `other_weapon` | string |
| `same_weapon` | bool |
| `headshot` | bool |
| `other_headshot` | bool |
| `isteamkill` | bool |
| `isselfkill` | bool |

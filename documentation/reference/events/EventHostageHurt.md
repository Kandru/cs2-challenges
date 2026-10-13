# EventHostageHurt (hostage_hurt)

CSS game event `hostage_hurt` (`EventHostageHurt`).

Who gets credit: the **event player(s)** listed under task types.

## Task types

Put one of these in your task `type:` field:

- `hostage_hurt`

## Minimal YAML

Save under `blueprints/`. Add rules from the table below — see [Rules](../rules.md).

```yaml
tasks:
  - id: example
    type: hostage_hurt
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

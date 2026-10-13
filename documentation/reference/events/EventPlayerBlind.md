# EventPlayerBlind (player_has_blinded / player_got_blinded)

CSS game event `player_blind` (`EventPlayerBlind`).

Who gets credit: the **event player(s)** listed under task types.

## Task types

Put one of these in your task `type:` field:

- `player_has_blinded`
- `player_got_blinded`

## Minimal YAML

Save under `blueprints/`. Add rules from the table below — see [Rules](../rules.md).

```yaml
tasks:
  - id: example
    type: player_has_blinded
    amount: 1
    rules: []
```

## Attributes (rule keys)

Unknown keys never match — see [Rules](../rules.md).

| Key | Type |
|-----|------|
| [global event data](../../rules/GlobalEventData.md) | see page |
| [player data](../../rules/GlobalPlayerData.md) | prefixes `attacker`, `userid`, `victim` |
| `blind_duration` | float |
| `blindduration` | float |
| `entityid` | int |
| `isteamflash` | bool |
| `isselfflash` | bool |

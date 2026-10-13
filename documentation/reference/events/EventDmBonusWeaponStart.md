# EventDmBonusWeaponStart (dm_bonus_weapon_start)

CSS game event `dm_bonus_weapon_start` (`EventDmBonusWeaponStart`).

Who gets credit: **all connected players**.

## Task types

Put one of these in your task `type:` field:

- `dm_bonus_weapon_start`

## Minimal YAML

Save under `blueprints/`. Add rules from the table below — see [Rules](../rules.md).

```yaml
tasks:
  - id: example
    type: dm_bonus_weapon_start
    amount: 1
    rules: []
```

## Attributes (rule keys)

Unknown keys never match — see [Rules](../rules.md).

| Key | Type |
|-----|------|
| [global event data](../../rules/GlobalEventData.md) | see page |
| `Pos` | int |
| `time` | int |

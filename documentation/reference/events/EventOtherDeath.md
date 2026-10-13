# EventOtherDeath (other_death)

CSS game event `other_death` (`EventOtherDeath`).

Who gets credit: **all connected players**.

## Task types

Put one of these in your task `type:` field:

- `other_death`

## Minimal YAML

Save under `blueprints/`. Add rules from the table below — see [Rules](../rules.md).

```yaml
tasks:
  - id: example
    type: other_death
    amount: 1
    rules: []
```

## Attributes (rule keys)

Unknown keys never match — see [Rules](../rules.md).

| Key | Type |
|-----|------|
| [global event data](../../rules/GlobalEventData.md) | see page |
| `attacker` | int |
| `attackerblind` | bool |
| `headshot` | bool |
| `noscope` | bool |
| `otherid` | int |
| `othertype` | string |
| `penetrated` | int |
| `thrusmoke` | bool |
| `weapon` | string |
| `weapon_fauxitemid` | string |
| `weapon_itemid` | string |
| `weapon_originalowner_xuid` | string |

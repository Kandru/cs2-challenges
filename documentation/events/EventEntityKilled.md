# EventEntityKilled (entity_killed)

CSS game event `entity_killed` (`EventEntityKilled`).

Who gets credit: **all connected players**.

## Challenge types

Put one of these in your task `type:` field:

- `entity_killed`

## Rule keys

> [!WARNING]
> If you use an unknown key, the task will not work because that condition can never be met.

| Key | Type |
|-----|------|
| [global event data](../rules/GlobalEventData.md) | see page |
| `damagebits` | int |
| `entindex_attacker` | int |
| `entindex_inflictor` | int |
| `entindex_killed` | int |

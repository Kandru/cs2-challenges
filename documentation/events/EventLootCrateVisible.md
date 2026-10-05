# EventLootCrateVisible (loot_crate_visible)

CSS game event `loot_crate_visible` (`EventLootCrateVisible`).

Who gets credit: the **event player(s)** listed under challenge types.

## Challenge types

Put one of these in your task `type:` field:

- `loot_crate_visible`

## Rule keys

> [!WARNING]
> If you use an unknown key, the task will not work because that condition can never be met.

| Key | Type |
|-----|------|
| [global event data](../rules/GlobalEventData.md) | see page |
| [player data](../rules/GlobalPlayerData.md) | prefixes `userid`, `player` |
| `subject` | int |
| `type` | string |

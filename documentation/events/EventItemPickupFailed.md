# EventItemPickupFailed (item_pickup_failed)

CSS game event `item_pickup_failed` (`EventItemPickupFailed`).

Who gets credit: the **event player(s)** listed under challenge types.

## Challenge types

Put one of these in your task `type:` field:

- `item_pickup_failed`

## Rule keys

> [!WARNING]
> If you use an unknown key, the task will not work because that condition can never be met.

| Key | Type |
|-----|------|
| [global event data](../rules/GlobalEventData.md) | see page |
| [player data](../rules/GlobalPlayerData.md) | prefixes `userid`, `player` |
| `item` | string |
| `limit` | int |
| `reason` | int |

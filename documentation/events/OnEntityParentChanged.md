# OnEntityParentChanged (on_entity_parent_changed)

CSS listener `OnEntityParentChanged`.

Who gets credit: the **event player(s)** listed under challenge types.

## Challenge types

Put one of these in your task `type:` field:

- `on_entity_parent_changed`

## Rule keys

> [!WARNING]
> If you use an unknown key, the task will not work because that condition can never be met.

| Key | Type |
|-----|------|
| [global event data](../rules/GlobalEventData.md) | see page |
| `entity.designername` | string |
| `entity.index` | int |
| `newParent.designername` | string |
| `newParent.index` | int |

# OnEntityParentChanged (on_entity_parent_changed)

CSS listener `OnEntityParentChanged`.

Who gets credit: the **event player(s)** listed under task types.

## Task types

Put one of these in your task `type:` field:

- `on_entity_parent_changed`

## Minimal YAML

Save under `blueprints/`. Add rules from the table below — see [Rules](../rules.md).

```yaml
tasks:
  - id: example
    type: on_entity_parent_changed
    amount: 1
    rules: []
```

## Attributes (rule keys)

Unknown keys never match — see [Rules](../rules.md).

| Key | Type |
|-----|------|
| [global event data](../../rules/GlobalEventData.md) | see page |
| `entity.designername` | string |
| `entity.index` | int |
| `newParent.designername` | string |
| `newParent.index` | int |

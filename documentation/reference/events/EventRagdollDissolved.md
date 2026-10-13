# EventRagdollDissolved (ragdoll_dissolved)

CSS game event `ragdoll_dissolved` (`EventRagdollDissolved`).

Who gets credit: **all connected players**.

## Task types

Put one of these in your task `type:` field:

- `ragdoll_dissolved`

## Minimal YAML

Save under `blueprints/`. Add rules from the table below — see [Rules](../rules.md).

```yaml
tasks:
  - id: example
    type: ragdoll_dissolved
    amount: 1
    rules: []
```

## Attributes (rule keys)

Unknown keys never match — see [Rules](../rules.md).

| Key | Type |
|-----|------|
| [global event data](../../rules/GlobalEventData.md) | see page |
| `entindex` | int |

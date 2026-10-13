# EventFlareIgniteNpc (flare_ignite_npc)

CSS game event `flare_ignite_npc` (`EventFlareIgniteNpc`).

Who gets credit: **all connected players**.

## Task types

Put one of these in your task `type:` field:

- `flare_ignite_npc`

## Minimal YAML

Save under `blueprints/`. Add rules from the table below — see [Rules](../rules.md).

```yaml
tasks:
  - id: example
    type: flare_ignite_npc
    amount: 1
    rules: []
```

## Attributes (rule keys)

Unknown keys never match — see [Rules](../rules.md).

| Key | Type |
|-----|------|
| [global event data](../../rules/GlobalEventData.md) | see page |
| `entindex` | int |

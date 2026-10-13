# EventDynamicShadowLightChanged (dynamic_shadow_light_changed)

CSS game event `dynamic_shadow_light_changed` (`EventDynamicShadowLightChanged`).

Who gets credit: **all connected players**.

## Task types

Put one of these in your task `type:` field:

- `dynamic_shadow_light_changed`

## Minimal YAML

Save under `blueprints/`. Add rules from the table below — see [Rules](../rules.md).

```yaml
tasks:
  - id: example
    type: dynamic_shadow_light_changed
    amount: 1
    rules: []
```

## Attributes (rule keys)

Unknown keys never match — see [Rules](../rules.md).

| Key | Type |
|-----|------|
| [global event data](../../rules/GlobalEventData.md) | see page |

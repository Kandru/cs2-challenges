# EventDemoStop (demo_stop)

CSS game event `demo_stop` (`EventDemoStop`).

Who gets credit: **all connected players**.

## Task types

Put one of these in your task `type:` field:

- `demo_stop`

## Minimal YAML

Save under `blueprints/`. Add rules from the table below — see [Rules](../rules.md).

```yaml
tasks:
  - id: example
    type: demo_stop
    amount: 1
    rules: []
```

## Attributes (rule keys)

Unknown keys never match — see [Rules](../rules.md).

| Key | Type |
|-----|------|
| [global event data](../../rules/GlobalEventData.md) | see page |

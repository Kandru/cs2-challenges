# EventAnnouncePhaseEnd (announce_phase_end)

CSS game event `announce_phase_end` (`EventAnnouncePhaseEnd`).

Who gets credit: **all connected players**.

## Task types

Put one of these in your task `type:` field:

- `announce_phase_end`

## Minimal YAML

Save under `blueprints/`. Add rules from the table below — see [Rules](../rules.md).

```yaml
tasks:
  - id: example
    type: announce_phase_end
    amount: 1
    rules: []
```

## Attributes (rule keys)

Unknown keys never match — see [Rules](../rules.md).

| Key | Type |
|-----|------|
| [global event data](../../rules/GlobalEventData.md) | see page |

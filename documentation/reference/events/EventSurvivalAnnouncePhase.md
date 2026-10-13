# EventSurvivalAnnouncePhase (survival_announce_phase)

CSS game event `survival_announce_phase` (`EventSurvivalAnnouncePhase`).

Who gets credit: **all connected players**.

## Task types

Put one of these in your task `type:` field:

- `survival_announce_phase`

## Minimal YAML

Save under `blueprints/`. Add rules from the table below — see [Rules](../rules.md).

```yaml
tasks:
  - id: example
    type: survival_announce_phase
    amount: 1
    rules: []
```

## Attributes (rule keys)

Unknown keys never match — see [Rules](../rules.md).

| Key | Type |
|-----|------|
| [global event data](../../rules/GlobalEventData.md) | see page |
| `phase` | int |

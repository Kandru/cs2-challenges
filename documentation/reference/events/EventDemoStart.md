# EventDemoStart (demo_start)

CSS game event `demo_start` (`EventDemoStart`).

Who gets credit: **all connected players**.

## Task types

Put one of these in your task `type:` field:

- `demo_start`

## Minimal YAML

Save under `blueprints/`. Add rules from the table below — see [Rules](../rules.md).

```yaml
tasks:
  - id: example
    type: demo_start
    amount: 1
    rules: []
```

## Attributes (rule keys)

Unknown keys never match — see [Rules](../rules.md).

| Key | Type |
|-----|------|
| [global event data](../../rules/GlobalEventData.md) | see page |
| `dota_combatlog_list` | int |
| `dota_hero_chase_list` | int |
| `dota_pick_hero_list` | int |
| `local` | int |

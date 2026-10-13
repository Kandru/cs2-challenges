# EventDemoSkip (demo_skip)

CSS game event `demo_skip` (`EventDemoSkip`).

Who gets credit: **all connected players**.

## Task types

Put one of these in your task `type:` field:

- `demo_skip`

## Minimal YAML

Save under `blueprints/`. Add rules from the table below — see [Rules](../rules.md).

```yaml
tasks:
  - id: example
    type: demo_skip
    amount: 1
    rules: []
```

## Attributes (rule keys)

Unknown keys never match — see [Rules](../rules.md).

| Key | Type |
|-----|------|
| [global event data](../../rules/GlobalEventData.md) | see page |
| `dota_hero_chase_list` | int |
| `local` | int |
| `playback_tick` | int |
| `skipto_tick` | int |
| `user_message_list` | int |

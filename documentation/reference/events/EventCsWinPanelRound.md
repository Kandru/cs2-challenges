# EventCsWinPanelRound (cs_win_panel_round)

CSS game event `cs_win_panel_round` (`EventCsWinPanelRound`).

Who gets credit: the **event player(s)** listed under task types.

## Task types

Put one of these in your task `type:` field:

- `cs_win_panel_round`

## Minimal YAML

Save under `blueprints/`. Add rules from the table below — see [Rules](../rules.md).

```yaml
tasks:
  - id: example
    type: cs_win_panel_round
    amount: 1
    rules: []
```

## Attributes (rule keys)

Unknown keys never match — see [Rules](../rules.md).

| Key | Type |
|-----|------|
| [global event data](../../rules/GlobalEventData.md) | see page |
| [player data](../../rules/GlobalPlayerData.md) | prefixes `funfact_player`, `player` |
| `final_event` | int |
| `funfact_data1` | int |
| `funfact_data2` | int |
| `funfact_data3` | int |
| `funfact_token` | string |
| `show_timer_attack` | bool |
| `show_timer_defend` | bool |
| `timer_time` | int |

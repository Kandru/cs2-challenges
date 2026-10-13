# EventTeamplayBroadcastAudio (teamplay_broadcast_audio)

CSS game event `teamplay_broadcast_audio` (`EventTeamplayBroadcastAudio`).

Who gets credit: **all connected players**.

## Task types

Put one of these in your task `type:` field:

- `teamplay_broadcast_audio`

## Minimal YAML

Save under `blueprints/`. Add rules from the table below — see [Rules](../rules.md).

```yaml
tasks:
  - id: example
    type: teamplay_broadcast_audio
    amount: 1
    rules: []
```

## Attributes (rule keys)

Unknown keys never match — see [Rules](../rules.md).

| Key | Type |
|-----|------|
| [global event data](../../rules/GlobalEventData.md) | see page |
| `sound` | string |
| `team` | int |

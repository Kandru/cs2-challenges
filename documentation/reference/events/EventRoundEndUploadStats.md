# EventRoundEndUploadStats (round_end_upload_stats)

CSS game event `round_end_upload_stats` (`EventRoundEndUploadStats`).

Who gets credit: **all connected players**.

## Task types

Put one of these in your task `type:` field:

- `round_end_upload_stats`

## Minimal YAML

Save under `blueprints/`. Add rules from the table below — see [Rules](../rules.md).

```yaml
tasks:
  - id: example
    type: round_end_upload_stats
    amount: 1
    rules: []
```

## Attributes (rule keys)

Unknown keys never match — see [Rules](../rules.md).

| Key | Type |
|-----|------|
| [global event data](../../rules/GlobalEventData.md) | see page |

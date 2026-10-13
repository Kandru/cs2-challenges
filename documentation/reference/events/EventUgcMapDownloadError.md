# EventUgcMapDownloadError (ugc_map_download_error)

CSS game event `ugc_map_download_error` (`EventUgcMapDownloadError`).

Who gets credit: **all connected players**.

## Task types

Put one of these in your task `type:` field:

- `ugc_map_download_error`

## Minimal YAML

Save under `blueprints/`. Add rules from the table below — see [Rules](../rules.md).

```yaml
tasks:
  - id: example
    type: ugc_map_download_error
    amount: 1
    rules: []
```

## Attributes (rule keys)

Unknown keys never match — see [Rules](../rules.md).

| Key | Type |
|-----|------|
| [global event data](../../rules/GlobalEventData.md) | see page |
| `error_code` | int |
| `published_file_id` | int |

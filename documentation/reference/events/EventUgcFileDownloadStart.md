# EventUgcFileDownloadStart (ugc_file_download_start)

CSS game event `ugc_file_download_start` (`EventUgcFileDownloadStart`).

Who gets credit: **all connected players**.

## Task types

Put one of these in your task `type:` field:

- `ugc_file_download_start`

## Minimal YAML

Save under `blueprints/`. Add rules from the table below — see [Rules](../rules.md).

```yaml
tasks:
  - id: example
    type: ugc_file_download_start
    amount: 1
    rules: []
```

## Attributes (rule keys)

Unknown keys never match — see [Rules](../rules.md).

| Key | Type |
|-----|------|
| [global event data](../../rules/GlobalEventData.md) | see page |
| `hcontent` | int |
| `published_file_id` | int |

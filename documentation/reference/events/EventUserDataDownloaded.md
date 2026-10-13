# EventUserDataDownloaded (user_data_downloaded)

CSS game event `user_data_downloaded` (`EventUserDataDownloaded`).

Who gets credit: **all connected players**.

## Task types

Put one of these in your task `type:` field:

- `user_data_downloaded`

## Minimal YAML

Save under `blueprints/`. Add rules from the table below — see [Rules](../rules.md).

```yaml
tasks:
  - id: example
    type: user_data_downloaded
    amount: 1
    rules: []
```

## Attributes (rule keys)

Unknown keys never match — see [Rules](../rules.md).

| Key | Type |
|-----|------|
| [global event data](../../rules/GlobalEventData.md) | see page |

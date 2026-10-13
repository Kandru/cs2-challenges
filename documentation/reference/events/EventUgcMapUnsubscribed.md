# EventUgcMapUnsubscribed (ugc_map_unsubscribed)

CSS game event `ugc_map_unsubscribed` (`EventUgcMapUnsubscribed`).

Who gets credit: **all connected players**.

## Task types

Put one of these in your task `type:` field:

- `ugc_map_unsubscribed`

## Minimal YAML

Save under `blueprints/`. Add rules from the table below — see [Rules](../rules.md).

```yaml
tasks:
  - id: example
    type: ugc_map_unsubscribed
    amount: 1
    rules: []
```

## Attributes (rule keys)

Unknown keys never match — see [Rules](../rules.md).

| Key | Type |
|-----|------|
| [global event data](../../rules/GlobalEventData.md) | see page |
| `published_file_id` | int |

# EventWriteProfileData (write_profile_data)

CSS game event `write_profile_data` (`EventWriteProfileData`).

Who gets credit: **all connected players**.

## Task types

Put one of these in your task `type:` field:

- `write_profile_data`

## Minimal YAML

Save under `blueprints/`. Add rules from the table below — see [Rules](../rules.md).

```yaml
tasks:
  - id: example
    type: write_profile_data
    amount: 1
    rules: []
```

## Attributes (rule keys)

Unknown keys never match — see [Rules](../rules.md).

| Key | Type |
|-----|------|
| [global event data](../../rules/GlobalEventData.md) | see page |

# EventItemSchemaInitialized (item_schema_initialized)

CSS game event `item_schema_initialized` (`EventItemSchemaInitialized`).

Who gets credit: **all connected players**.

## Task types

Put one of these in your task `type:` field:

- `item_schema_initialized`

## Minimal YAML

Save under `blueprints/`. Add rules from the table below — see [Rules](../rules.md).

```yaml
tasks:
  - id: example
    type: item_schema_initialized
    amount: 1
    rules: []
```

## Attributes (rule keys)

Unknown keys never match — see [Rules](../rules.md).

| Key | Type |
|-----|------|
| [global event data](../../rules/GlobalEventData.md) | see page |

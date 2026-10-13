# EventEventTicketModified (event_ticket_modified)

CSS game event `event_ticket_modified` (`EventEventTicketModified`).

Who gets credit: **all connected players**.

## Task types

Put one of these in your task `type:` field:

- `event_ticket_modified`

## Minimal YAML

Save under `blueprints/`. Add rules from the table below — see [Rules](../rules.md).

```yaml
tasks:
  - id: example
    type: event_ticket_modified
    amount: 1
    rules: []
```

## Attributes (rule keys)

Unknown keys never match — see [Rules](../rules.md).

| Key | Type |
|-----|------|
| [global event data](../../rules/GlobalEventData.md) | see page |

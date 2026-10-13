# EventMbInputLockCancel (mb_input_lock_cancel)

CSS game event `mb_input_lock_cancel` (`EventMbInputLockCancel`).

Who gets credit: **all connected players**.

## Task types

Put one of these in your task `type:` field:

- `mb_input_lock_cancel`

## Minimal YAML

Save under `blueprints/`. Add rules from the table below — see [Rules](../rules.md).

```yaml
tasks:
  - id: example
    type: mb_input_lock_cancel
    amount: 1
    rules: []
```

## Attributes (rule keys)

Unknown keys never match — see [Rules](../rules.md).

| Key | Type |
|-----|------|
| [global event data](../../rules/GlobalEventData.md) | see page |

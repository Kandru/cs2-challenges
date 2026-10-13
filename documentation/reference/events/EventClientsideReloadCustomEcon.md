# EventClientsideReloadCustomEcon (clientside_reload_custom_econ)

CSS game event `clientside_reload_custom_econ` (`EventClientsideReloadCustomEcon`).

Who gets credit: **all connected players**.

## Task types

Put one of these in your task `type:` field:

- `clientside_reload_custom_econ`

## Minimal YAML

Save under `blueprints/`. Add rules from the table below — see [Rules](../rules.md).

```yaml
tasks:
  - id: example
    type: clientside_reload_custom_econ
    amount: 1
    rules: []
```

## Attributes (rule keys)

Unknown keys never match — see [Rules](../rules.md).

| Key | Type |
|-----|------|
| [global event data](../../rules/GlobalEventData.md) | see page |
| `steamid` | string |

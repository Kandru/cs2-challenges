# OnPlayerChat (on_player_chat / player_chat)

CSS listener `OnPlayerChat`.

Who gets credit: the **event player(s)** listed under task types.

## Task types

Put one of these in your task `type:` field:

- `on_player_chat`
- `player_chat`

## Minimal YAML

Save under `blueprints/`. Add rules from the table below — see [Rules](../rules.md).

```yaml
tasks:
  - id: example
    type: on_player_chat
    amount: 1
    rules: []
```

## Attributes (rule keys)

Unknown keys never match — see [Rules](../rules.md).

| Key | Type |
|-----|------|
| [global event data](../../rules/GlobalEventData.md) | see page |
| [player data](../../rules/GlobalPlayerData.md) | prefixes `player` |
| `message` | string |
| `text` | string |
| `teamchat` | bool |
| `teamonly` | bool |

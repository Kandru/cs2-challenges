# OnPlayerChat (on_player_chat / player_chat)

CSS listener `OnPlayerChat`.

Who gets credit: the **event player(s)** listed under challenge types.

## Challenge types

Put one of these in your task `type:` field:

- `on_player_chat`
- `player_chat`

## Rule keys

> [!WARNING]
> If you use an unknown key, the task will not work because that condition can never be met.

| Key | Type |
|-----|------|
| [global event data](../rules/GlobalEventData.md) | see page |
| [player data](../rules/GlobalPlayerData.md) | prefixes `player` |
| `message` | string |
| `text` | string |
| `teamchat` | bool |
| `teamonly` | bool |

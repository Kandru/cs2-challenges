# OnPlayerButtonsChanged (on_player_buttons_changed)

CSS listener `OnPlayerButtonsChanged`.

Who gets credit: the **event player(s)** listed under challenge types.

## Challenge types

Put one of these in your task `type:` field:

- `on_player_buttons_changed`

## Rule keys

> [!WARNING]
> If you use an unknown key, the task will not work because that condition can never be met.

| Key | Type |
|-----|------|
| [global event data](../rules/GlobalEventData.md) | see page |
| [player data](../rules/GlobalPlayerData.md) | prefixes `player` |
| `pressed` | int |
| `released` | int |

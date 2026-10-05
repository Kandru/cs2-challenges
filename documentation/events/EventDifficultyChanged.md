# EventDifficultyChanged (difficulty_changed)

CSS game event `difficulty_changed` (`EventDifficultyChanged`).

Who gets credit: **all connected players**.

## Challenge types

Put one of these in your task `type:` field:

- `difficulty_changed`

## Rule keys

> [!WARNING]
> If you use an unknown key, the task will not work because that condition can never be met.

| Key | Type |
|-----|------|
| [global event data](../rules/GlobalEventData.md) | see page |
| `newDifficulty` | int |
| `oldDifficulty` | int |
| `strDifficulty` | string |

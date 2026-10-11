# Global event data

These keys are added to almost every event.

| Key | Type | Meaning |
|-----|------|---------|
| `global.iswarmup` | bool | Warmup period from game rules. |
| `global.isduringround` | bool | `true` from round start (including freeze time) until round end; `false` between rounds. |
| `global.mapname` | string | Current map name. |
| `global.hashostages` | bool | Map currently has hostage entities. Only filled when some active task uses this key. |

> [!NOTE]
> These keys only decide whether an event **counts**. They do not hide the challenge from the menu.

Use `==` / `!=` for map name, and `bool==` / `bool!=` for the bool keys.

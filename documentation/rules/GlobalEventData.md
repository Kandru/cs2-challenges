# Global event data

These keys are added to almost every event.

| Key | Type | Meaning |
|-----|------|---------|
| `global.iswarmup` | bool | Warmup is active. |
| `global.isduringround` | bool | A round is active (warmup or live). |
| `global.mapname` | string | Current map name. |
| `global.hashostages` | bool | Map currently has hostage entities. Only filled when some active task uses this key. |

> [!NOTE]
> `global.mapname` and `global.hashostages` only decide whether an event **counts**. They do not hide the challenge from the HUD.

Use `==` / `!=` for map name, and `bool==` / `bool!=` for the bool keys.

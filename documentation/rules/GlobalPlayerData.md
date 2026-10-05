# Global player data

When an event involves a player, these keys are available with a **prefix** from that event’s page (`attacker`, `victim`, `player`, `defuser`, …).

Swap `prefix` for the real prefix, e.g. `attacker.alive` or `victim.isbot`.

| Key | Type | Meaning |
|-----|------|---------|
| `prefix.name` | string | Player name |
| `prefix.isbot` | bool | Player is a bot |
| `prefix.team` | string | Team — see [CsTeam](../enums/CsTeam.md) |
| `prefix.alive` | bool | Player is alive |
| `prefix.ping` | int | Ping |
| `prefix.money` | int | Money |
| `prefix.score` | int | Score |
| `prefix.stats.kills` | int | Kills |
| `prefix.stats.assists` | int | Assists |
| `prefix.stats.deaths` | int | Deaths |
| `prefix.stats.damage` | int | Damage dealt |
| `prefix.health` | int | Health |
| `prefix.armor` | int | Armor |
| `prefix.hasdefusor` | bool | Has a defuse kit |
| `prefix.hashelmet` | bool | Has a helmet |

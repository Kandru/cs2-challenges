# Schedules

A schedule picks which challenges are active, and for how long. Only **one** schedule runs at a time: the **first** entry in `schedules.yaml` whose UTC window contains now.

File: `/addons/counterstrikesharp/configs/plugins/Challenges/schedules.yaml`

An empty file is created on first start. Changes apply on the next map start, or after `challenges reload`.

Only blueprints listed on a schedule that has not ended (`date_end` ≥ now) are loaded. Ended schedules are ignored for loading. Progress still uses only the first in-window schedule.

```yaml
test_challenge:
  title:
    en: "Season challenges"
    de: "Saison-Herausforderungen"
  date_start: "2026-01-01 00:00:00"
  date_end: "2027-01-01 00:00:00"
  challenges:
    - headshots_in_a_row
    - knife_challenge
    - weapon_ak47
```

| Field | Meaning |
|-------|---------|
| `title` | Language map. Used for the Discord “new schedule” embed heading (see below). The **first** title string in the map is also part of the progress key. |
| `date_start` / `date_end` | `"YYYY-MM-DD HH:MM:SS"` in **UTC**. |
| `challenges` | List of challenge ids = blueprint filenames without `.yaml`. An optional `:*` suffix is stripped. Unknown ids are skipped. Duplicates are ignored. |

> [!TIP]
> Put the schedule you want first among those that overlap. Later overlapping entries are ignored.

## Schedule title

The HUD menu and tracker do **not** display the schedule title. They use fixed plugin strings and challenge/task titles.

Discord’s new-schedule embed expands the title with:

| Placeholder | Value |
|-------------|--------|
| `{total}` | Number of challenges in that schedule |
| `{count}` | Always `0` |
| `{playerName}` | The word `Player` |

Language fallback for Discord uses `discord.language` from [Settings](settings.md), then the two-letter code, then the first language in the map.

## Progress key

Progress for a schedule is stored under a key built from:

1. The **first** title string in `title`
2. `date_start`
3. `date_end`

> [!IMPORTANT]
> Changing any of those three starts a **new** progress key. Task progress for the old key is removed from the player file. The lifetime solved count is kept.

See [In game](ingame.md) for where player files live.

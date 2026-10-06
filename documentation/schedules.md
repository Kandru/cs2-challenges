# Schedules

<a id="active-schedule"></a>

A schedule picks which challenges are active, and for how long. Only **one** schedule runs at a time: the **first** entry in `schedules.yaml` whose UTC window contains now.

File: `/addons/counterstrikesharp/configs/plugins/Challenges/schedules.yaml`

An empty file is created on first start. Changes apply on the next map start, or after `challenges reload`.

```yaml
test_challenge:
  title:
    en: "== {playerName}'s Challenges ({count} / {total}) =="
    de: "== {playerName}'s Herausforderungen ({count} / {total}) =="
  date_start: "2026-01-01 00:00:00"
  date_end: "2027-01-01 00:00:00"
  challenges:
    - headshots_in_a_row
    - knife_challenge
    - weapon_ak47
```

| Field | Meaning |
|-------|---------|
| <a id="schedule-title"></a>`title` | Language map for the HUD. Placeholders: `{playerName}` (cut to 12 characters), `{count}`, `{total}`. Uses the player’s language, then the two-letter code, then the **first language** in the map. |
| <a id="dates"></a>`date_start` / `date_end` | `"YYYY-MM-DD HH:MM:SS"` in **UTC**. |
| <a id="challenge-list"></a>`challenges` | List of challenge ids = blueprint filenames without `.yaml`. Unknown ids are skipped. |

<a id="progress-bucket"></a>

> [!IMPORTANT]
> Changing a schedule’s title or dates starts a **new** progress bucket. Player progress for the old title/dates is not carried over.

> [!TIP]
> Put the schedule you want first among those that overlap. Later overlapping entries are ignored.

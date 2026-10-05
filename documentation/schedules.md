> [!TIP]
> An empty *schedules.yaml* is created in the plugin config directory on first start. Edits apply after the next map change.

# Schedules

A schedule activates a set of challenges for a time window. Only one schedule is active: the first entry whose UTC window contains now.

```yaml
test_challenge:
  title:
    en: "== Challenges ({count} / {total}) =="
    de: "== Herausforderungen ({count} / {total}) =="
  date_start: "2026-01-01 00:00:00"
  date_end: "2027-01-01 00:00:00"
  challenges:
    - headshots_in_a_row
    - knife_challenge
    - weapon_ak47
```

### title

Language map for the schedule. Fallback is the first language in the map (same rule as challenge titles).

### date_start / date_end

`"YYYY-MM-DD HH:MM:SS"` in UTC. Applied after map change.

### challenges

List of challenge **ids** = blueprint filenames without `.yaml`. Wildcards and `file:key` ids are no longer used (one file = one challenge).

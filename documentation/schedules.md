> [!TIP]
> An empty *schedules.yaml* file is automatically created in the plugin config directory after the first start of the plugin. You can update the *schedules.json* file at any time, and it will automatically reload when the map changes. There's no need to reload the plugin manually.

# Schedules

## What are schedules used for?

A schedule is like a timetable for challenges. With a schedule, you can plan challenges ahead without needing to change the config files before a special event. For example, if you have a theme for the next weekend called *Happy Headshots*, you can plan it ahead, and it will automatically switch to that schedule at the given time (a map change is necessary). You can have as many schedules as you like, but only one schedule can be active at a time. The list will be searched from top to bottom, and the first schedule matching the current time will be used.

## Example Schedule

```yaml
# test challenge for the whole year 2025
test_challenge:
  title:
    en: "== {playerName}'s Challenges ({count} / {total}) =="
    de: "== {playerName}'s Herausforderungen ({count} / {total}) =="
  date_start: "2025-01-01 00:00:00"
  date_end: "2026-01-01 00:00:00"
  # all example challenges are enabled by default
  challenges:
    - "blind_them_by_the_light:*"
    - "headshots_in_a_row:*"
    - "hostage_rescue:*"
    - "inflatable_castle:*"
    - "knife_challenge:*"
    - "noscopes_in_a_row:*"
    - "revolver_challenge:*"
    - "scout_challenge:*"
    - "taser_challenge:*"

```

This document describes each schedule, identified by a unique identifier, such as *test_schedule_1*. It includes basic information about the activation dates and the challenges that will be enabled.

### title

The title is displayed in the upper right corner of the GUI above all challenges. You can use the placeholders *{playerName}*, *{count}*, and *{total}* to show the player's name, the current number of events completed, and the total number of events needed to complete the challenge. It's important to use the *{playerName}* placeholder because spectators will see the challenges of the player they are watching, not their own. This helps avoid confusion.

The default language is the player's language (changeable with !lang en/de/...). If that language is not available, it will fall back to the server's language (set this default in the CounterstrikeSharp settings json). If neither is available, the first entry in the list will be used.

### date_start

The date when this event will be available to users. The schedule will be enabled after the map changes. The format is: "YYYY-MM-DD HH:MM:SS".

### date_end

The date when this event will no longer be available. The schedule will be disabled after the map changes. The format is: "YYYY-MM-DD HH:MM:SS".

### challenges

This is a list of all challenges that should be enabled. To enable all challenges in a file, simply use the file name followed by a colon **":"** and an asterisk **"*"**. (See example above). You can also only enable single challenges from a file by simply using the global identifier of a challenge. Please refer to the actions documentation for further information.

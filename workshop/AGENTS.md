# HUD (AI)

Shared driver: [`src/Challenges/utils/CustomHud.cs`](../src/Challenges/utils/CustomHud.cs) (layout spawn, transmit, `SetText` / `SetHasClass`, staggered `OnTick`). One painter per HUD under [`src/Challenges/huds/`](../src/Challenges/huds/): `Tracker` and `Menu` (`Context` gives them plugin state, bound by `HudDriver`). Menu input: [`src/Challenges/utils/HudMenu.cs`](../src/Challenges/utils/HudMenu.cs). **One stylesheet** for both layouts: [`hud.vcss`](content/panorama/styles/custom_game/challenges/hud.vcss) (validator-safe, see [panorama-css.md](panorama-css.md)). [`example/hud.vcss`](example/hud.vcss) is the full Prophunt sheet, **reference only** and never loaded by the game.

`workshop/content` is the addon root: copy that folder into `csgo_addons/<addon>/` and publish. `AGENTS.md`, `panorama-css.md`, `example/` and `preview/` stay beside it and are not part of the paste. Layouts are loaded as `panorama/layout/custom_game/challenges/<name>.vxml_c`.

## Terminology

- **Blueprint**: one possible challenge (YAML under `blueprints/`, type `ChallengeDefinition`).
- **Challenge**: a blueprint a player can win (every visible task complete). A schedule is a set of challenges.
- **Task**: one step inside a challenge. Hidden tasks (`visible: false`) stay off both HUDs.

Plugin modules (`PlayerManagement`, `ChallengeEngine`, …) inherit `ClassesBlueprint`.

## Server can only

1. Dialog variable on the layout's root card id → Label `text="{s:name}"` (text only, never a path or colour).
2. `SetHasClass` on a panel **id**.

No panel creation, no width/colour/image from C#. Bars = `clip` class ladder (`p0`…`p100` via `CustomHud.SetStepPercent`, 10% steps). Accent / state = class. Layout edits need a VPK republish; C# does not.

**Round restart:** `custom_hud_layout` entities stay alive across rounds. The client rebuilds panels from XML defaults while the server would keep last round's classes/dialog vars, and repainting identical values is a zero netvar diff. `HudDriver.EventRoundStart` calls `CustomHud.ResetRound()`, which writes the XML defaults through `Tracker.WriteDefaults` / `Menu.WriteDefaults` and drops the per-slot ledger. **Whatever an XML file ships as default must match `WriteDefaults`.** Never Kill/respawn a live layout; `CustomHud.Shutdown` only runs on map end / unload.

**Staggered refresh:** `CustomHud.OnTick` walks slots at 2 slots/tick and calls `Tracker.Refresh`, which only paints while freeze mode or the progress timer is active and hides the card when both are over.

## Layouts

| Layout | Root card id | Routed ids (see `CustomHud.LayoutIndexForPanel`) |
| --- | --- | --- |
| `tracker.xml` | `Tracker` | `Tracker`, `ch-trow-*`, `ch-tfill-*`, `ch-ttask-*`, `ch-tby-*` |
| `menu.xml` | `Menu` | every other `ch-*` / `ph-*` id |

`tools/validate_panorama.py` (`make panorama`) checks that the ids and `{s:…}` variables in both XML files are exactly the set the C# writes.

### Tracker ([`Tracker`](../src/Challenges/huds/Tracker.cs))

Top-right card (`margin-top` = `margin-right` = 20px, width 420px), never clickable. Title `{s:tr_title}` (`hud.tracker.title`), `{s:tr_count}` = `solved / total` of the active schedule. Up to `MaxRows = 5` rows `ch-trow-N` (`is-off` hides a row): challenge title `{s:tr_tN}`, value `{s:tr_vN}` (`NN%`), bar `ch-tfill-N`. Body stacks full-width `MenuTasksCol` then `MenuByCol` (`TrackerRowSplit`): headers `{s:tr_tasks_h}` / `{s:tr_by_h}`, up to 3 tasks `ch-ttask-N-T` / `{s:tr_N_tT}`, and up to 6 completers in a 2-column grid `ch-tby-N-B` / `{s:tr_N_byB}` (ellipsis on long titles/tasks/names). Rows are ordered by completion percentage, highest first. Default `gui.tracker_rows` = 3 (clamped 3–5).

- **Freeze mode:** `HudDriver.EventRoundStart` shows it for T/CT humans when `gui.show_on_round_start` and `mp_freezetime > 0`; `EventRoundFreezeEnd` hides it. Rows: unsolved challenges, highest percent first, `gui.tracker_rows`.
- **Progress mode:** `ChallengeEngine` calls `Tracker.ShowProgress(player, challengeIds)` when visible tasks advance (`gui.show_on_progress`, `gui.progress_duration`). Up to 5 rows from those challenges, percent descending. It replaces freeze rows and freeze mode returns until freeze end.

### Menu ([`Menu`](../src/Challenges/huds/Menu.cs))

`!c` / `!challenges` → `Menu.Open` → `HudMenu.Open` (mouse capture, no freeze). Root uses `.ph-fs` (68px inset). `MenuBody` is `fill-parent-flow`; inner `MenuColumns` is `height: 100%` + `flow-children: right` so list/score can stretch and pin their footers.

- Filters (`ch-filter-all|progress|ending|starting`, exclusive `active` class): All, Progress (default), Ending soon, Starting soon.
- Challenge cards `ch-mrow-0…3` (max 4 per page, no list scroll): title `{s:mN_title}`, meta `{s:mN_meta}`, bar `ch-mfill-N`. Compact body is a 50/50 split: left `MenuTasksCol` (header `{s:menu_tasks_h}` + up to 3 tasks `ch-mtask-N-T` / `{s:mN_tT}`, `is-done` when complete; last slot becomes `+N more` when needed) and right `MenuByCol` (header `{s:menu_by_h}` + up to 6 completer slots in 2-column `MenuByRow`s, `ch-mby-N-B` / `{s:mN_byB}`; if more than 6, show 5 names + `+N more`; `is-empty` for nobody/overflow; long names ellipsis). List order: completion % desc, then title A–Z. `gui.menu_page_size` defaults to 4 (clamped 1–4). `MenuListSpacer` + `ph-prev` / `ph-next` + `{s:menu_page}` pin the footer.
- Scoreboard: connected humans with **current** (active schedule, shown as `solved / available`) and **total** (`statistics.amount_challenges_solved`) counts. Title is always `Scoreboard`; click toggles sort (active column highlighted via `sort-active`). Self card `ch-spin` (`ScoreSelfCard`: rank, name, labeled schedule/lifetime stats). Table header `# | Name | Solved | Total`. Rows `ch-srow-0…11` are fixed 32px height with rank and zebra `alt`; unused slots collapse; the row list scrolls when full. `ch-score-prev` / `ch-score-next` + `{s:score_page}` at the bottom.
- `ph-close` closes. `HudDriver` forwards `OnCustomHudClicked` to `HudMenu`.

## Shared classes (`hud.vcss`)

`.ph-fs` fullscreen inset · `.ph-card` / `.ph-off` · `.ph-bg` / `.ph-bar` · `.ph-titlebar` / `Label.ph-title` / `Label.ph-money` · `Label.ph-btn-label` · `.ph-icon-btn` / `.ph-close-x` · `.ph-stat` (label + gold clip fill) · `.MenuFilterBtn` / `.MenuFooterBtn` / `.MenuRow` / `.MenuRowSplit` / `.MenuTask` / `.MenuBy` / `.ScoreRow` / `.ScoreSortBtn` · `.Tracker*`.

## Check before inventing CSS

[panorama-css.md](panorama-css.md) for the layout model. `make panorama` for the reject list (unknown properties, `@define` / bareword colours, `font-size` with units, `vertical-align: center`, `background-size: contain`, `rgba`, `background-blur`, class `hidden`, …). Unknown properties are detected against the known-good [`example/hud.vcss`](example/hud.vcss). Browser previews: [`preview/tracker.html`](preview/tracker.html), [`preview/menu.html`](preview/menu.html), styled by [`preview/kit.css`](preview/kit.css) (a web approximation; the game is authoritative).

Update this file when HUD behaviour changes.

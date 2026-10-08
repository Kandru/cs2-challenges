# HUD (AI)

Shared driver: [`src/Challenges/utils/CustomHud.cs`](../src/Challenges/utils/CustomHud.cs) (layout spawn, transmit, `SetText` / `SetHasClass`, staggered `OnTick`, 1s opacity fade via `ph-fading` then `ph-off`). One painter per HUD under [`src/Challenges/huds/`](../src/Challenges/huds/): `Tracker` and `Menu` (`Context` gives them plugin state, bound by `HudDriver`). Menu input: [`src/Challenges/utils/HudMenu.cs`](../src/Challenges/utils/HudMenu.cs). **One stylesheet** for both layouts: [`hud.vcss`](content/panorama/styles/custom_game/challenges/hud.vcss) (validator-safe, see [panorama-css.md](panorama-css.md)). [`example/hud.vcss`](example/hud.vcss) is the full Prophunt sheet, **reference only** and never loaded by the game.

`workshop/content` is the addon root: copy that folder into `csgo_addons/<addon>/` and publish. `AGENTS.md`, `panorama-css.md`, `example/` and `preview/` stay beside it and are not part of the paste. Layouts are loaded as `panorama/layout/custom_game/challenges/<name>.vxml_c`.

## Panorama versions

Source names in this repo stay `tracker.xml`, `menu.xml`, and `hud.vcss`. Do **not** add `v2` / `v3` to those filenames or to the stylesheet include here.

`PanoramaVersion` in [`src/Challenges/Version.cs`](../src/Challenges/Version.cs) is the generation this build spawns (`tracker_vN.vxml_c` / `menu_vN.vxml_c` via `CustomHud`). On copy into the workshop addon, rename the layouts to `tracker_vN.vxml` and `menu_vN.vxml` and update the stylesheet include by hand.

Bump `PanoramaVersion` (`v2` → `v3`, …) only when a layout or stylesheet change would break plugins still bound to the previous generation (panel ids, dialog variables, class names, structure). C#-only changes do not bump it. Leave every older generation in the published addon so servers that have not updated the DLL keep working.

## Theme / UX design

Baseline chrome: dark gradient card (`#12171e` → `#0b0f14`), hairline borders `#ffffff14`, gold accent `#f0a531` (default `gui.theme` = `gold`). Accent themes swap that gold via root classes `theme-*` (`HudTheme.Apply`). Palette source: [`tools/hud_themes.py`](../tools/hud_themes.py) (emits `hud.vcss` theme block + preview `theme-data.js` during `make panorama`). CT / T use stock CS2 colours `#96c8fa` / `#eabe54`.

**Type scale (unitless Panorama `font-size`):** floor is **14** (tracker challenge title / `Label.ph-stat-name`). Small labels (filter chips, task lines, completer names, score cells, nav glyphs, column heads) are **14** and usually `stratum-bold-tf`. Titles / self-card values stay **15–18**. Do not grow panel padding just to fit larger type.

**Bars:** clip ladder `p0`…`p100` in 10% steps. `p0` clips to **0%** (no sliver). Prefer bold text over enlarging containers.

## Text and translation

Every visible Label uses `text="{s:…}"`, except `Label.MenuCredit` in `menu.xml` (literal English credit, not a dialog variable). The server fills dialog variables from the player's language (`lang/en.json`, `lang/de.json` via `Context.Text` / `LocalizerExtensions.ForPlayer`). Blueprint and task titles stay in the YAML title map and resolve through `Titles.For` (full culture name, then two-letter code, then first entry). Format templates: `hud.format.page` (`({page} / {pages})`), `hud.format.percent`, `hud.format.count`, `hud.format.rank`, `hud.menu.when.*` (relative schedule phrases). Integers use `Context.FormatNumber` (`N0` culture: `1,000` / `1.000`). Nav glyphs: `hud.menu.prev` / `hud.menu.next` → scoreboard title-bar prev/next vars. Left column title: `hud.menu.list` / `hud.menu.viewing` (`{name}`) → `{s:list_title}`. Player names are not translated except the viewer’s completer slot → `hud.menu.you` (`YOU` / `DU`) with class `is-you`. Preview HTML is an English mock only.

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

**Fade:** `ShowPanel` mounts with `ph-fading` then clears it next frame (~1s opacity in). `HidePanel` / `BeginFadeOut` set `ph-fading` and keep transmit for 1s, then collapse with `ph-off`. Do not set `ph-off` in the same write as the fade-out start.

**Staggered refresh:** `CustomHud.OnTick` walks slots at 2 slots/tick and calls `Tracker.Refresh`, which only paints while freeze mode or the progress timer is active and hides the card when both are over (including Up-next sequencing).

## Layouts

| Layout | Root card id | Routed ids (see `CustomHud.LayoutIndexForPanel`) |
| --- | --- | --- |
| `tracker.xml` | `Tracker` | `Tracker`, `ch-tr-hint`, `ch-trow-*`, `ch-tfill-*`, `ch-ttask-*`, `ch-ttimer*` |
| `menu.xml` | `Menu` | every other `ch-*` / `ph-*` id |

`tools/validate_panorama.py` (`make panorama`) checks that the ids and `{s:…}` variables in both XML files are exactly the set the C# writes.

### Tracker ([`Tracker`](../src/Challenges/huds/Tracker.cs))

Top-right card (`margin-top` = `margin-right` = 10px, width 420px), never clickable. No “solved by” column. Title `{s:tr_title}`, `{s:tr_count}` = `hud.format.count` of the active schedule. Under the title, `{s:tr_hint}` / `ch-tr-hint` shows `hud.tracker.open` with the shortest `menu_commands` name plus `command_prefix` (default `!c`); collapsed (`is-off`) when there is no command. Up to `MaxRows = 5` rows `ch-trow-N` (`is-off` hides a row): challenge title `{s:tr_tN}`, value `{s:tr_vN}`, bar `ch-tfill-N`, tasks header `{s:tr_tasks_h}`, up to 3 tasks `ch-ttask-N-T` / `{s:tr_N_tT}`. Default `gui.tracker_rows` = 3 (clamped 1–5).

- **Freeze mode:** only when `mp_freezetime > 0` (skip when `<= 0`). Unsolved challenges only, highest percent first; each card lists **unsolved** visible tasks only. Bottom drain bar (`ch-ttimer`) tracks remaining freezetime.
- **Progress mode:** `ChallengeEngine` calls `Tracker.ShowProgress` with per-task kinds (`Progress` / `TaskSolved` / `ChallengeSolved`). Compact rows for touched challenges, percent descending. Progress = that task; task solved = that task done + next unsolved tasks; challenge solved = that challenge, then after `gui.progress_duration` fade out/in with `hud.tracker.up_next` and the next unsolved challenge’s remaining tasks. Same drain bar tracks remaining hold time.
- **Rule broken:** no center alert. `Notifications.NotifyRuleBroken` → `Tracker.ShowRuleBroken` enqueues the **breaker** task (hidden notify owner). Cards show one after another until round start: title = breaker task title (`{s:tr_title}`), `{s:tr_count}` empty, row marks the visible reset targets with `is-broken`. Round-start freeze clears the queue so freeze overview always wins; after freeze ends, any still-queued breaks play.

### Menu ([`Menu`](../src/Challenges/huds/Menu.cs))

`!c` / `!challenges` → `Menu.Open` → `HudMenu.Open` (mouse capture, no freeze). Root uses `.ph-fs` (68px inset). `MenuBody` is `fill-parent-flow`; inner `MenuColumns` is `height: 100%` + `flow-children: right`.

- Left column title `{s:list_title}` (`.MenuListTitle`, centered) above the filters. Own view → `hud.menu.list` (“My challenges”); inspecting someone → `hud.menu.viewing` (“Challenges of {name}”). Re-clicking the highlighted score row (or your own) clears the subject and closes detail.
- Filters (`ch-filter-all|solved|progress|ending|starting`, exclusive `active`): All, Solved, Progress (default), Ending soon, Starting soon — each label includes `(count)` for the viewed player (scoreboard subject or self). Empty filters get `is-disabled`. Challenge list and detail scroll (`.MenuRows` / `.DetailRows` with `overflow: squish scroll`, `padding-right` gutter); unused slots collapse. Scoreboard keeps `ScoreTitleBar` pagination (prev | title + `(page)` | next).
- Challenge cards `ch-mrow-0…99` (`ListSlots = 100`) are clickable `Button`s. Compact body 50/50 tasks|completers. Completers come from the player archive (online + offline); viewer slot = green `is-you` + `hud.menu.you`. Selected card gets `is-selected`. Percents, solved/progress membership, and task marks come from the **menu subject** (viewer by default).
- **Inspect from scoreboard:** click a `ch-srow-*` row to set `MenuSubjectSteamId` and repaint the left column with that player’s progress (online live state, or archive for offline). Re-click the same row or your own row clears the subject. Inspected row gets `is-viewing`. Filter choice stays; empty filters still fall back.
- Tracker uses `.HudScreen.TrackerScreen` (`z-index: 99998`) so an open menu paints above it.
- **Detail column:** clicking a card toggles `is-off` on `ch-score` / `ch-detail` (hides scoreboard, shows `MenuDetail`). Header is Back (`ch-detail-back`) + `{s:detail_title}`. Lists visible tasks in completion order (`requires`-aware), each followed by its hidden rule-broken tasks (`is-broken`). Each row title is followed by up to 8 half-width rule chips (`ch-drule-R-S` / `{s:dR_rS}`, two per row like solved-by) from `TaskRuleSummary.Parts` (skip `global.*`, short `hud.rule.*` phrases; overflow joins on the last chip). No completer list. Up to `DetailSlots = 20` rows `ch-drow-0…19`; Back or re-click closes detail. Task counts use the menu subject’s progress.
- **Ending soon:** schedule end in `(now, now+7d]`. Sort: % desc, end time, title A–Z. **Starting soon:** % desc, start time, A–Z. **All:** % desc, active before inactive, time, A–Z. **Progress** / **Solved:** % desc, A–Z.
- Scoreboard: audience filters All / Online (`ch-score-f-*`). Online = connected humans; All = online + archived players. Sort filters Solved / Lifetime (`ch-score-s-*`, default Solved). Column + self-card label is **Lifetime** (not Total). Lifetime counts use grouped `FormatNumber`. Rows are clickable `Button`s.

## Shared classes (`hud.vcss`)

`.ph-fs` · `.ph-card` / `.ph-fading` / `.ph-off` · `.ph-bg` / `.ph-bar` · `.ph-titlebar` / `Label.ph-title` / `Label.ph-money` · `Label.ph-btn-label` · `.ph-icon-btn` / `.ph-close-x` · `.ph-stat` · `.MenuFilterBtn` / `.is-disabled` / `.MenuTitleNav` / `.MenuListTitle` / `.MenuRow` / `.is-selected` / `.MenuScore` / `.MenuDetail` / `.DetailTitleBar` / `.DetailTitle` / `.DetailRow` / `.DetailRule` / `.MenuTask` / `.is-done` / `.is-broken` / `.MenuBy` / `.is-you` · `.ScoreRow` / `.is-viewing` / `.ScoreFilterBar` / `.ScoreTitleBar` / `.ScoreTitlePage` · `.HudScreen.TrackerScreen` · `.Tracker*` · `.theme-*`.

## Check before inventing CSS

[panorama-css.md](panorama-css.md) for the layout model. `make panorama` for the reject list. Previews: [`preview/tracker.html`](preview/tracker.html) (JS mode cycle; freeze drains over mocked `mp_freezetime`, progress over `gui.progress_duration`), [`preview/menu.html`](preview/menu.html), [`preview/kit.css`](preview/kit.css) (accent via CSS vars), [`preview/theme.js`](preview/theme.js) + [`preview/theme-data.js`](preview/theme-data.js) (swatches from `hud_themes.py`).

Update this file when HUD behaviour changes.

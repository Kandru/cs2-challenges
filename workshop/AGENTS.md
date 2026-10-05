# HUD (AI)

Shared driver: [`src/Challenges/utils/CustomHud.cs`](../src/Challenges/utils/CustomHud.cs) (layout spawn, transmit, `SetText` / `SetHasClass`, staggered `OnTick`). One painter per HUD under [`src/Challenges/huds/`](../src/Challenges/huds/): `Tracker` and `Menu` (`Context` gives them plugin state, bound by `HudDriver`). Menu input: [`src/Challenges/utils/HudMenu.cs`](../src/Challenges/utils/HudMenu.cs). **One stylesheet** for both layouts: [`hud.vcss`](content/panorama/styles/custom_game/challenges/hud.vcss) (validator-safe, see [panorama-css.md](panorama-css.md)). [`example/hud.vcss`](example/hud.vcss) is the full Prophunt sheet, **reference only** and never loaded by the game.

`workshop/content` is the addon root: copy that folder into `csgo_addons/<addon>/` and publish. `AGENTS.md`, `panorama-css.md`, `example/` and `preview/` stay beside it and are not part of the paste. Layouts are loaded as `panorama/layout/custom_game/challenges/<name>.vxml_c`.

## Server can only

1. Dialog variable on the layout's root card id → Label `text="{s:name}"` (text only, never a path or colour).
2. `SetHasClass` on a panel **id**.

No panel creation, no width/colour/image from C#. Bars = `clip` class ladder (`p0`…`p100` via `CustomHud.SetStepPercent`, 10% steps). Accent / state = class. Layout edits need a VPK republish; C# does not.

**Round restart:** `custom_hud_layout` entities stay alive across rounds. The client rebuilds panels from XML defaults while the server would keep last round's classes/dialog vars, and repainting identical values is a zero netvar diff. `HudDriver.EventRoundStart` calls `CustomHud.ResetRound()`, which writes the XML defaults through `Tracker.WriteDefaults` / `Menu.WriteDefaults` and drops the per-slot ledger. **Whatever an XML file ships as default must match `WriteDefaults`.** Never Kill/respawn a live layout; `CustomHud.Shutdown` only runs on map end / unload.

**Staggered refresh:** `CustomHud.OnTick` walks slots at 2 slots/tick and calls `Tracker.Refresh`, which only paints while freeze mode or the progress timer is active and hides the card when both are over.

## Layouts

| Layout | Root card id | Routed ids (see `CustomHud.LayoutIndexForPanel`) |
| --- | --- | --- |
| `tracker.xml` | `Tracker` | `Tracker`, `ch-trow-0…4`, `ch-tfill-0…4` |
| `menu.xml` | `Menu` | every other `ch-*` / `ph-*` id |

`tools/validate_panorama.py` (`make panorama`) checks that the ids and `{s:…}` variables in both XML files are exactly the set the C# writes.

### Tracker ([`Tracker`](../src/Challenges/huds/Tracker.cs))

Top-right card, never clickable. Title `{s:tr_title}` (`hud.tracker.title`), `{s:tr_count}` = `solved / total` of the active schedule. Up to `MaxRows = 5` rows `ch-trow-N` (`is-off` hides a row): label `{s:tr_tN}` (challenge title, `{count}/{total}` filled from the current task), value `{s:tr_vN}` (`NN%`), bar `ch-tfill-N`.

- **Freeze mode:** `HudDriver.EventRoundStart` shows it for T/CT humans when `gui.show_on_round_start` and `mp_freezetime > 0`; `EventRoundFreezeEnd` hides it. Rows: unsolved challenges, highest percent first, `gui.tracker_rows` (3–5).
- **Progress mode:** `ChallengeEngine` calls `Tracker.ShowProgress(player, challengeIds)` when visible tasks advance (`gui.show_on_progress`, `gui.progress_duration`). Up to 5 rows from those challenges, percent descending. It replaces freeze rows and freeze mode returns until freeze end.

### Menu ([`Menu`](../src/Challenges/huds/Menu.cs))

`!c` / `!challenges` → `Menu.Open` → `HudMenu.Open` (mouse capture, no freeze). Root uses `.ph-fs` (68px inset). Body is `flow-children: right`: list `fill-parent-flow(2)`, scoreboard `fill-parent-flow(1)`.

- Filters (`ch-filter-all|progress|ending|starting`, exclusive `active` class): All, Progress (default), Ending soon, Starting soon. Rows `ch-mrow-0…9` (`{s:mN_title}`, `{s:mN_meta}`, bar `ch-mfill-N`); slots beyond `gui.menu_page_size` get `is-off`, unused slots on the page get `empty` (chrome stays, content collapses). `ph-prev` / `ph-next` + `{s:menu_page}`.
- Scoreboard: connected humans, solved count in the active schedule. Pinned self row `ch-spin` (`spin_rank`, `spin_name`, `spin_val`) above rows `ch-srow-0…7` (`is-self` marks the local player). `ch-score-prev` / `ch-score-next` + `{s:score_page}`.
- `ph-close` closes. `HudDriver` forwards `OnCustomHudClicked` to `HudMenu`.

## Shared classes (`hud.vcss`)

`.ph-fs` fullscreen inset · `.ph-card` / `.ph-off` · `.ph-bg` / `.ph-bar` · `.ph-titlebar` / `Label.ph-title` / `Label.ph-money` · `Label.ph-btn-label` · `.ph-icon-btn` / `.ph-close-x` · `.ph-stat` (label + gold clip fill) · `.MenuFilterBtn` / `.MenuFooterBtn` / `.MenuRow` / `.ScoreRow` · `.Tracker*`.

## Check before inventing CSS

[panorama-css.md](panorama-css.md) for the layout model. `make panorama` for the reject list (unknown properties, `@define` / bareword colours, `font-size` with units, `vertical-align: center`, `background-size: contain`, `rgba`, `background-blur`, class `hidden`, …). Unknown properties are detected against the known-good [`example/hud.vcss`](example/hud.vcss). Browser previews: [`preview/tracker.html`](preview/tracker.html), [`preview/menu.html`](preview/menu.html), styled by [`preview/kit.css`](preview/kit.css) (a web approximation; the game is authoritative).

Update this file when HUD behaviour changes.

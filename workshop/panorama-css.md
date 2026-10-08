# Panorama CSS (this HUD)

Web CSS is dropped with no log. Run `make panorama` (`tools/validate_panorama.py`) before calling a layout done — it rejects unknown properties, forbidden values, and the layout traps below.

**One stylesheet:** [`content/panorama/styles/custom_game/challenges/hud.vcss`](content/panorama/styles/custom_game/challenges/hud.vcss) (symlink `hud.css` for validators). It is a trimmed copy of the Prophunt kit ([`example/hud.vcss`](example/hud.vcss), reference only) with the `ph-*` chrome and the tracker / menu classes, using values this client accepts. Keep the filename and layout includes as `hud.vcss` in this repo; versioned stylesheet paths are applied by hand when copying into the workshop addon (see [AGENTS.md](AGENTS.md)#panorama-versions).

## Layout model

`flow-children: down|right|none`. Size with `width`/`height` (`px`, `%`, `fit-children`, `fill-parent-flow(n)`). Parent must be sized before a child `%` resolves — `%` under an unsized `flow-children: none` parent collapses.

**Padding does not inset `width/height: 100%` children.** Those measure the border box. Fullscreen shells that need a clear margin use spacer panels in the flow, not `padding` on the sized parent. Shared `.ph-fs` (68px longhand padding) works when `ph-bg` uses `ignore-parent-flow`.

**Centered text:** give the Label an **explicit px width** (or `%` under a sized parent) plus `text-align: center`. Prefer stock `horizontal-center` / `text-align-center` from `csgostyles` as well. `text-overflow: shrink` also needs that box. **`text-overflow: shrink` left-paints and overrides `text-align`** — when the glyph must stay centered at a fixed size, use `width: fit-children` + `horizontal-align: center` + `text-overflow: noclip` instead (see RoundStatus).

**Button labels:** parent `flow-children: none` and **sized** (px / `%` / `fill-parent-flow`). Label uses `Label.ph-btn-label` (`width: 100%`, `horizontal-align: center`, `vertical-align: middle`, `text-align: center`). Do **not** set `height: 100%` on the Label — that pins the glyph to the top. Fit-children buttons (weapon Shop link) skip `ph-btn-label` and only set `vertical-align: middle` on the Label.

`horizontal-align` / `vertical-align` position the panel in its parent. Use `vertical-align: middle` (not `center`). `ignore-parent-flow` for overlays (`ph-bg`). Height `fill-parent-flow` needs a down/up parent; width needs a right parent. Cross-axis `height: 100%` under `flow-children: right` is only valid when that parent's height is **not** `fill-parent-flow`.

## Values that work here

`background-color` `#rrggbb` / `#rrggbbaa` / `gradient(linear, …)` / `none`. **No `rgba()`/`rgb()`/`hsl()`.** `background-image` + `background-size: contains` + `background-position` / `background-repeat` / `background-img-opacity`. The engine rejects web `contain` (falls back to `auto`).

`font-size` is **unitless** (`font-size: 14`, not `14px`). Beat stock Stratum with `Label.ClassName` selectors.

Colours are hex literals only. No `@define` / `var()`. A bareword like `color: accent` drops the declaration (often taking `font-size` in the same block with it). Shared chrome and kit pieces live in `hud.vcss`.

`visibility: collapse` (there is no `display`). `overflow: noclip|clip|scroll`. `z-index` (siblings only; HUD-above-crosshair = `99999` on `.HudScreen`; RoundStatus overrides to `100000`; HiderModes root `.HudScreen.HiderModesScreen` underlays at `99998`).

Bars: `clip: rect(top, right, bottom, left)` with class ladder `p0`…`p100` — **not `width`** (a dialog-var write in the same subtree restarts a width transition). Blur: `world-blur: gaussian(n)` — not `background-blur`.

`box-shadow`: **colour first** — `#000000aa 0px 4px 8px 0px`. Transitions: split `transition-property` / `transition-duration` / `transition-timing-function`.

## Shared classes (in `hud.vcss`)

`.ph-fs` fullscreen inset · `.ph-titlebar` / `Label.ph-title` / `Label.ph-money` · `Label.ph-btn-label` · `.ph-bg` / `.ph-bar` · `.ph-icon-btn` / `.ph-close-x` · `.ph-stat` (label + gold clip fill). Hide with `ph-off` / `is-off`. Do not name a class `hidden` / `Hidden` — both exist on `csgostyles`.

## Do not

`display` / flex / grid, `calc()`, `var()`, `@define`, `@media`, `::before` / `::after`, `background-size: contain` (use `contains`), `visibility: hidden`, `overflow: hidden`, class name `hidden`, `font-size` with a unit, `vertical-align: center`, bareword theme tokens, `background-blur`, width-transition fill ladders.

`:hover` / `:active` work only while input capture is on. `:selected` / `:disabled` / `:focus` attrs are rejected.

## This project

Show/hide: `.ph-card.ph-off` for cards, `.is-off` for rows. Chrome: `.ph-bg` + `.ph-bar` (gold). Fills: `.ph-stat-fill.p0`…`.p100` via `clip`. `wash-color` on a parent tints children — clear it on the `Button` and `:hover`/`:active`. Previews: [`preview/tracker.html`](preview/tracker.html), [`preview/menu.html`](preview/menu.html) with [`preview/kit.css`](preview/kit.css).

# Panorama CSS (this HUD)

Web CSS is dropped with no log. Run `make panorama` (`tools/validate_panorama.py`) before calling a layout done.

**One stylesheet:** [`hud.vcss`](content/panorama/styles/custom_game/challenges/hud.vcss) (symlink `hud.css` for validators). Trimmed from Prophunt [`example/hud.vcss`](example/hud.vcss) (reference only). Keep `hud.vcss` in-repo; versioned paths are applied by hand on workshop copy (see [AGENTS.md](AGENTS.md)#panorama-versions).

## Layout model

`flow-children: down|right|none`. Size with `width`/`height` (`px`, `%`, `fit-children`, `fill-parent-flow(n)`). Parent must be sized before child `%` resolves.

**Padding does not inset `width/height: 100%` children** (border box). Fullscreen margins → spacer panels; `.ph-fs` works with `ph-bg` + `ignore-parent-flow`.

**Centered text:** Label needs explicit width + `text-align: center`. Prefer stock `horizontal-center` / `text-align-center`. **`text-overflow: shrink` left-paints** - for fixed centered glyphs use `fit-children` + `horizontal-align: center` + `noclip`.

**Button labels:** parent `flow-children: none` and sized; `Label.ph-btn-label` (`width: 100%`, middle align). Do not set `height: 100%` on the Label. Use `vertical-align: middle` (not `center`). Height `fill-parent-flow` needs a down/up parent; width needs a right parent.

## Values that work

`background-color` hex / `#rrggbbaa` / `gradient(linear, …)` / `none` - **no `rgba()`/`rgb()`/`hsl()`**. `background-size: contains` (not web `contain`). `font-size` **unitless**. Hex colours only - no `@define` / `var()` / bareword tokens. `visibility: collapse` (no `display`). `overflow: noclip|clip|scroll`. HUD `z-index` ~`99999` (siblings only).

Bars: `clip: rect(…)` class ladder `p0`…`p100` - **not `width`**. Countdown bars run on the client: `.TrackerTimerFill` (0s default, linear), `span-N` sets `transition-duration`, `is-running` sets the empty `clip`; these rules must follow the ladder so they win at equal specificity. Blur: `world-blur: gaussian(n)`. `box-shadow`: colour first. Split transition properties.

## Shared / do not

Shared: `.ph-fs`, `.ph-titlebar` / `Label.ph-title` / `Label.ph-money` / `Label.ph-btn-label`, `.ph-bg` / `.ph-bar`, `.ph-icon-btn` / `.ph-close-x`, `.ph-stat`. Hide with `ph-off` / `is-off` - never class name `hidden`/`Hidden`.

Do not: `display`/flex/grid, `calc()`, `var()`, `@define`, `@media`, `::before`/`::after`, `background-size: contain`, `visibility: hidden`, `overflow: hidden`, unitful `font-size`, `vertical-align: center`, `background-blur`, width-transition fills. `:hover`/`:active` need input capture; `:selected`/`:disabled`/`:focus` rejected.

## This project

Show/hide: `.ph-card.ph-off`, `.is-off` for rows. Fills: `.ph-stat-fill.p0`…`.p100`. Clear `wash-color` on Buttons. `hittest="false"` on every Panel and Label (also in the menu); Buttons carry no `hittest` and still receive clicks. Previews: [`preview/tracker.html`](preview/tracker.html), [`preview/menu.html`](preview/menu.html), [`preview/kit.css`](preview/kit.css).

#!/usr/bin/env python3
"""Validate the Challenges Panorama addon (workshop/content/panorama): XML layouts + hud.vcss."""
from __future__ import annotations

import re
import sys
import xml.etree.ElementTree as ET
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
TOOLS = Path(__file__).resolve().parent
WORKSHOP = ROOT / "workshop"
CONTENT = WORKSHOP / "content" / "panorama"
LAYOUTS = CONTENT / "layout" / "custom_game" / "challenges"
STYLES = CONTENT / "styles" / "custom_game" / "challenges"
HUD_VCSS = STYLES / "hud.vcss"
HUD_CSS = STYLES / "hud.css"
EXAMPLE_VCSS = WORKSHOP / "example" / "hud.vcss"
PREVIEW = WORKSHOP / "preview"
HUD_THEME_CS = ROOT / "src" / "Challenges" / "huds" / "HudTheme.cs"
THEME_DATA_JS = PREVIEW / "theme-data.js"

if str(TOOLS) not in sys.path:
    sys.path.insert(0, str(TOOLS))
import hud_themes  # noqa: E402

INCLUDE_RE = re.compile(r"s2r://panorama/styles/custom_game/challenges/([A-Za-z0-9_]+\.vcss)")
VAR_RE = re.compile(r"\{s:([A-Za-z0-9_]+)\}")
HIDDEN_CLASS = re.compile(r"(?<![\w-])(?:hidden|Hidden)(?![\w-])")
PROPERTY_RE = re.compile(r"(?:^|[{;])\s*([a-z][a-z0-9-]*)\s*:", re.M)
BLOCK_RE = re.compile(r"\{([^{}]*)\}", re.S)

# Tokens Panorama drops silently (even mid-declaration).
BAD_CSS = re.compile(
    r"(?:"
    r"(?:[{;]|^)\s*display\s*:"
    r"|\b(?:rgba?|hsla?|calc|var)\s*\("
    r"|background-size\s*:\s*contain\b"
    r"|@media\b"
    r"|@define\b"
    r"|::(?:before|after)\b"
    r"|(?:[{;]|^)\s*visibility\s*:\s*hidden\b"
    r"|(?:[{;]|^)\s*overflow\s*:\s*[^;]*\bhidden\b"
    r"|(?:[{;]|^)\s*background-blur\s*:"
    r"|(?:[{;]|^)\s*vertical-align\s*:\s*center\b"
    r"|(?:[{;]|^)\s*font-size\s*:\s*[0-9.]+[a-z%]+\b"
    r")",
    re.I | re.M,
)
REJECTED_ATTRS = frozenset({"texturewidth", "textureheight"})
REJECTED_PSEUDO = re.compile(r":(?:selected|disabled|focus)\b")

# Mirrors CustomHud.LayoutIndexForPanel: tracker ids route to layout 0, everything else to layout 1.
TRACKER_FILE = "tracker.xml"
MENU_FILE = "menu.xml"
MENU_CS = ROOT / "src" / "Challenges" / "huds" / "Menu.cs"
CARD_CS = ROOT / "src" / "Challenges" / "huds" / "ChallengeCardPaint.cs"
TRACKER_CS = ROOT / "src" / "Challenges" / "huds" / "Tracker.cs"
# Engine hard limit on CCSCustomHudLayout.m_vecDialogVariableNames.
MENU_DIALOG_VAR_ENGINE_CAP = 1244


def csharp_consts(path: Path, *names: str) -> dict[str, int]:
    text = path.read_text(encoding="utf-8")
    values: dict[str, int] = {}
    for name in names:
        match = re.search(rf"public const int {name} = (\d+);", text)
        if not match:
            raise SystemExit(f"missing public const int {name} in {path.relative_to(ROOT)}")
        values[name] = int(match.group(1))
    return values


_TRACKER = csharp_consts(TRACKER_CS, "MaxRows")
_MENU = csharp_consts(MENU_CS, "ListSlots", "ScoreSlots", "DetailSlots", "DetailRuleSlots")
_CARD = csharp_consts(CARD_CS, "TaskSlots", "CompleterSlots")
TRACKER_ROWS = _TRACKER["MaxRows"]
MENU_LIST_SLOTS = _MENU["ListSlots"]
MENU_SCORE_SLOTS = _MENU["ScoreSlots"]
MENU_TASK_SLOTS = _CARD["TaskSlots"]
MENU_BY_SLOTS = _CARD["CompleterSlots"]
MENU_DETAIL_SLOTS = _MENU["DetailSlots"]
MENU_DETAIL_RULE_SLOTS = _MENU["DetailRuleSlots"]

TRACKER_IDS = (
    {"Tracker", "ch-ttimer", "ch-ttimer-fill", "ch-tr-hint"}
    | {f"ch-trow-{i}" for i in range(TRACKER_ROWS)}
    | {f"ch-tfill-{i}" for i in range(TRACKER_ROWS)}
    | {f"ch-ttask-{i}-{t}" for i in range(TRACKER_ROWS) for t in range(MENU_TASK_SLOTS)}
)
TRACKER_VARS = (
    {"tr_title", "tr_count", "tr_tasks_h", "tr_hint"}
    | {f"tr_t{i}" for i in range(TRACKER_ROWS)}
    | {f"tr_v{i}" for i in range(TRACKER_ROWS)}
    | {f"tr_{i}_t{t}" for i in range(TRACKER_ROWS) for t in range(MENU_TASK_SLOTS)}
)
MENU_IDS = (
    {
        "Menu",
        "ph-close",
        "ch-score-prev",
        "ch-score-next",
        "ch-score-h-cur",
        "ch-score-h-tot",
        "ch-score-f-all",
        "ch-score-f-online",
        "ch-score-s-solved",
        "ch-score-s-lifetime",
        "ch-filter-all",
        "ch-filter-solved",
        "ch-filter-progress",
        "ch-filter-ending",
        "ch-filter-starting",
        "ch-menu-empty",
        "ch-spin",
        "ch-score",
        "ch-detail",
        "ch-detail-back",
    }
    | {f"ch-mrow-{i}" for i in range(MENU_LIST_SLOTS)}
    | {f"ch-mfill-{i}" for i in range(MENU_LIST_SLOTS)}
    | {f"ch-mtask-{i}-{t}" for i in range(MENU_LIST_SLOTS) for t in range(MENU_TASK_SLOTS)}
    | {f"ch-mby-{i}-{b}" for i in range(MENU_LIST_SLOTS) for b in range(MENU_BY_SLOTS)}
    | {f"ch-srow-{i}" for i in range(MENU_SCORE_SLOTS)}
    | {f"ch-drow-{i}" for i in range(MENU_DETAIL_SLOTS)}
    | {f"ch-drule-{i}-{r}" for i in range(MENU_DETAIL_SLOTS) for r in range(MENU_DETAIL_RULE_SLOTS)}
)
MENU_VARS = (
    {
        "menu_title",
        "list_title",
        "menu_f_all",
        "menu_f_solved",
        "menu_f_progress",
        "menu_f_ending",
        "menu_f_starting",
        "menu_empty",
        "menu_tasks_h",
        "menu_by_h",
        "score_title",
        "score_page",
        "score_prev",
        "score_next",
        "score_f_all",
        "score_f_online",
        "score_s_solved",
        "score_s_lifetime",
        "score_h_rank",
        "score_h_name",
        "score_h_cur",
        "score_h_tot",
        "spin_name",
        "spin_cur",
        "spin_tot",
        "spin_rank",
        "spin_l_cur",
        "spin_l_tot",
        "detail_title",
        "detail_back",
    }
    | {f"m{i}_title" for i in range(MENU_LIST_SLOTS)}
    | {f"m{i}_when" for i in range(MENU_LIST_SLOTS)}
    | {f"m{i}_meta" for i in range(MENU_LIST_SLOTS)}
    | {f"m{i}_t{t}" for i in range(MENU_LIST_SLOTS) for t in range(MENU_TASK_SLOTS)}
    | {f"m{i}_by{b}" for i in range(MENU_LIST_SLOTS) for b in range(MENU_BY_SLOTS)}
    | {f"s{i}_rank" for i in range(MENU_SCORE_SLOTS)}
    | {f"s{i}_name" for i in range(MENU_SCORE_SLOTS)}
    | {f"s{i}_cur" for i in range(MENU_SCORE_SLOTS)}
    | {f"s{i}_tot" for i in range(MENU_SCORE_SLOTS)}
    | {f"d{i}_title" for i in range(MENU_DETAIL_SLOTS)}
    | {f"d{i}_r{r}" for i in range(MENU_DETAIL_SLOTS) for r in range(MENU_DETAIL_RULE_SLOTS)}
)
REQUIRED_CLASSES = {
    "ph-off",
    "ph-fading",
    "is-off",
    "is-done",
    "is-empty",
    "is-disabled",
    "is-you",
    "is-selected",
    "active",
    "sort-active",
    "empty",
    "is-self",
    "alt",
    *hud_themes.class_names(),
    *(f"p{p}" for p in range(0, 101, 10)),
}


def strip_comments(css: str) -> str:
    return re.sub(r"/\*.*?\*/", "", css, flags=re.S)


def css_properties(css: str) -> set[str]:
    props: set[str] = set()
    for block in BLOCK_RE.findall(strip_comments(css)):
        props.update(PROPERTY_RE.findall("{" + block))
    return props


def check_css(failures: list[str]) -> None:
    if not HUD_VCSS.is_file():
        failures.append(f"missing {HUD_VCSS.relative_to(ROOT)}")
        return

    if not (HUD_CSS.is_symlink() and HUD_CSS.resolve() == HUD_VCSS.resolve()):
        failures.append("hud.css must be a symlink to hud.vcss")

    text = HUD_VCSS.read_text(encoding="utf-8")
    code = strip_comments(text)

    for match in BAD_CSS.finditer(code):
        line = code.count("\n", 0, match.start()) + 1
        failures.append(f"hud.vcss:{line}: forbidden token '{match.group(0).strip()}'")

    if REJECTED_PSEUDO.search(code):
        failures.append("hud.vcss: :selected/:disabled/:focus pseudo-classes are rejected by the client")

    if any(cls.lower() == "hidden" for cls in re.findall(r"\.([A-Za-z_][\w-]*)", code)):
        failures.append("hud.vcss: class name 'hidden' collides with csgostyles - use ph-off / is-off")

    if code.count("{") != code.count("}"):
        failures.append("hud.vcss: unbalanced braces")

    if EXAMPLE_VCSS.is_file():
        known = css_properties(EXAMPLE_VCSS.read_text(encoding="utf-8"))
        for prop in sorted(css_properties(text) - known):
            failures.append(f"hud.vcss: property '{prop}' is not used by the known-good example sheet")

    classes = set(re.findall(r"\.([A-Za-z_][\w-]*)", code))
    for cls in sorted(REQUIRED_CLASSES - classes):
        failures.append(f"hud.vcss: missing class .{cls}")
    for pct in range(0, 101):
        if f".ph-stat-fill.p{pct}" not in code:
            failures.append(f"hud.vcss: missing .ph-stat-fill.p{pct} (SetPercent / SetStepPercent ladder)")


def check_layout(name: str, expected_ids: set[str], expected_vars: set[str], failures: list[str]) -> None:
    path = LAYOUTS / name
    rel = path.relative_to(ROOT)
    if not path.is_file():
        failures.append(f"missing {rel}")
        return

    try:
        tree = ET.parse(path)
    except ET.ParseError as exc:
        failures.append(f"{rel}: XML parse error: {exc}")
        return

    root = tree.getroot()
    ids: list[str] = []
    variables: set[str] = set()
    for element in root.iter():
        for attr in REJECTED_ATTRS & set(element.attrib):
            failures.append(f"{rel}: attribute '{attr}' is rejected")
        if element.tag == "Button" and "hittest" in element.attrib:
            failures.append(f"{rel}: attribute 'hittest' is disallowed on Button")
        if "id" in element.attrib:
            ids.append(element.attrib["id"])
        for classes in [element.attrib.get("class", "")]:
            if HIDDEN_CLASS.search(classes):
                failures.append(f"{rel}: class '{classes}' collides with csgostyles - use ph-off")
        variables.update(VAR_RE.findall(element.attrib.get("text", "")))

    duplicates = {i for i in ids if ids.count(i) > 1}
    for dup in sorted(duplicates):
        failures.append(f"{rel}: duplicate id '{dup}'")

    for missing in sorted(expected_ids - set(ids)):
        failures.append(f"{rel}: missing id '{missing}' (CustomHud routes it to this layout)")
    for extra in sorted(set(ids) - expected_ids):
        failures.append(f"{rel}: id '{extra}' is not routed to this layout by CustomHud.LayoutIndexForPanel")
    for missing in sorted(expected_vars - variables):
        failures.append(f"{rel}: dialog variable {{s:{missing}}} is never bound by a Label")
    for extra in sorted(variables - expected_vars):
        failures.append(f"{rel}: dialog variable {{s:{extra}}} is not written by C#")

    includes = {m for el in root.iter("include") for m in INCLUDE_RE.findall(el.attrib.get("src", ""))}
    if "hud.vcss" not in includes:
        failures.append(f"{rel}: must include challenges/hud.vcss")
    for include in includes:
        if not (STYLES / include).is_file():
            failures.append(f"{rel}: include {include} does not exist")

    panel_ids_with_flag = [el for el in root.iter("Panel") if el.attrib.get("id") == name.split(".")[0].capitalize()]
    for panel in panel_ids_with_flag:
        if "ph-off" not in panel.attrib.get("class", "").split():
            failures.append(f"{rel}: root card '{panel.attrib['id']}' must start with class ph-off")

    root_panels = [el for el in root if el.tag == "Panel"]
    for panel in root_panels:
        if "id" in panel.attrib:
            failures.append(f"{rel}: layout root panel must not have an id")


def check_preview(failures: list[str]) -> None:
    for name in ("kit.css", "theme.js", "theme-data.js", "tracker.html", "menu.html"):
        if not (PREVIEW / name).is_file():
            failures.append(f"missing workshop/preview/{name}")
    for html_name in ("tracker.html", "menu.html"):
        html = (PREVIEW / html_name).read_text(encoding="utf-8")
        if "theme-data.js" not in html:
            failures.append(f"workshop/preview/{html_name}: must load theme-data.js before theme.js")


def check_hud_theme_cs(failures: list[str]) -> None:
    if not HUD_THEME_CS.is_file():
        failures.append(f"missing {HUD_THEME_CS.relative_to(ROOT)}")
        return
    text = HUD_THEME_CS.read_text(encoding="utf-8")
    match = re.search(r"string\[\]\s+Names\s*=\s*\[(.*?)\]", text, re.S)
    if not match:
        failures.append("HudTheme.cs: missing Names array")
        return
    found = re.findall(r'"([a-z]+)"', match.group(1))
    expected = hud_themes.names()
    if found != expected:
        failures.append(
            f"HudTheme.cs Names must match tools/hud_themes.py ({', '.join(expected)})"
        )


def sync_generated() -> list[str]:
    """Refresh generated theme artifacts. Returns relative paths that were written."""
    written: list[str] = []
    if hud_themes.sync_vcss_section(HUD_VCSS):
        written.append(str(HUD_VCSS.relative_to(ROOT)))
    if hud_themes.sync_file(THEME_DATA_JS, hud_themes.render_theme_data_js()):
        written.append(str(THEME_DATA_JS.relative_to(ROOT)))
    return written


def main() -> int:
    failures: list[str] = []
    written = sync_generated()
    if len(MENU_VARS) > MENU_DIALOG_VAR_ENGINE_CAP:
        failures.append(
            f"menu dialog variables: {len(MENU_VARS)} exceeds "
            f"m_vecDialogVariableNames cap {MENU_DIALOG_VAR_ENGINE_CAP} "
            f"(lower MENU_LIST_SLOTS)"
        )
    check_css(failures)
    check_layout(TRACKER_FILE, TRACKER_IDS, TRACKER_VARS, failures)
    check_layout(MENU_FILE, MENU_IDS, MENU_VARS, failures)
    check_preview(failures)
    check_hud_theme_cs(failures)

    if failures:
        print("Panorama validation failed:")
        for failure in failures:
            print(f"  - {failure}")
        return 1

    note = f"; synced {', '.join(written)}" if written else ""
    print(f"Panorama validation OK (tracker.xml, menu.xml, hud.vcss){note}")
    return 0


if __name__ == "__main__":
    sys.exit(main())

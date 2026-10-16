"""Single source of truth for the tracker size steps (player setting, gui.tracker_size).

Used by validate_panorama.py to emit the size-* rules in hud_vN.vcss. Each rule scales one metric
of the 100% tracker, so the card keeps its anchor corner and real layout size at every step
(no transform: the known-good sheet has no transform-origin). 100 is the base sheet, no class.
"""
from __future__ import annotations

from pathlib import Path

# Percent steps, ascending. Must match HudSettings.Sizes / DefaultSize (checked by validate_panorama.py).
SIZES: list[int] = [80, 90, 100, 110, 120]
DEFAULT = 100

BEGIN = "/* >>> hud_scales:begin */"
END = "/* <<< hud_scales:end */"

# Tracker metrics at 100%: (selector below the tracker root, property, base value). Fonts are unitless.
METRICS: list[tuple[str, str, int]] = [
    ("", "width", 420),
    (" .TxHead", "height", 48),
    (" .TxHead", "padding-left", 16),
    (" .TxHead", "padding-right", 16),
    (" Label.TxKicker", "font-size", 16),
    (" Label.TxKicker", "margin-right", 10),
    (" Label.TxTitle", "font-size", 18),
    (" Label.TxCount", "font-size", 22),
    (" .TxPad", "width", 16),
    (" .TrackerRow", "padding-top", 12),
    (" .TrackerRow", "padding-bottom", 10),
    (" .TrackerRow .TxCardHead", "height", 24),
    (" .TrackerRow .TxIdx", "width", 28),
    (" .TrackerRow .TxIdx", "height", 20),
    (" .TrackerRow .TxIdx", "margin-right", 10),
    (" Label.TxIdxLabel", "font-size", 14),
    (" .TrackerRow .TxSeg", "margin-top", 8),
    (" .TrackerRow .TxSeg", "margin-bottom", 8),
    (" .TrackerRow .MenuTask", "height", 22),
    (" .MenuTaskMark", "width", 12),
    (" .MenuTaskMark", "height", 12),
    (" .MenuTaskMark", "margin-right", 10),
    (" Label.MenuTaskLabel", "font-size", 15),
    (" Label.TxRowTitle", "font-size", 16),
    (" Label.TxRowPct", "font-size", 18),
    (" Label.TxRowPct", "margin-left", 12),
    (" Label.TrackerHint", "font-size", 14),
    (" Label.TrackerHint", "margin-top", 10),
    (" Label.TrackerHint", "margin-left", 16),
    (" .TrackerTimerRow", "margin-top", 10),
    (" .TrackerTimerRow", "margin-bottom", 12),
]


def class_names() -> list[str]:
    return [f"size-{size}" for size in SIZES if size != DEFAULT]


def scaled(value: int, size: int) -> int:
    return int(value * size / 100 + 0.5)


def render_panorama_css() -> str:
    chunks = [
        BEGIN,
        "/* Tracker sizes (player setting / gui.tracker_size). Generated from tools/hud_scales.py - do not edit by hand. */",
    ]
    selectors: list[str] = []
    for selector, _, _ in METRICS:
        if selector not in selectors:
            selectors.append(selector)
    for size in SIZES:
        if size == DEFAULT:
            continue
        chunks.append("")
        for selector in selectors:
            decls = " ".join(
                f"{prop}: {scaled(value, size)}{'' if prop == 'font-size' else 'px'};"
                for sel, prop, value in METRICS
                if sel == selector
            )
            chunks.append(f".Tracker.size-{size}{selector} {{ {decls} }}")
    chunks.append(END)
    return "\n".join(chunks) + "\n"


def sync_vcss_section(vcss_path: Path) -> bool:
    """Rewrites the block between the markers. Returns True when the file changed."""
    text = vcss_path.read_text(encoding="utf-8")
    if BEGIN not in text or END not in text:
        raise ValueError(f"{vcss_path.name}: missing {BEGIN} / {END} markers")
    start = text.index(BEGIN)
    end = text.index(END) + len(END) + 1
    new_text = text[:start] + render_panorama_css() + text[end:]
    if new_text == text:
        return False
    vcss_path.write_text(new_text, encoding="utf-8")
    return True

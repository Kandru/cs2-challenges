#!/usr/bin/env python3
"""Generate builder/catalog.json from extractors + engine sources."""

from __future__ import annotations

import json
import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
EXTRACTORS_DIR = ROOT / "src" / "Challenges" / "extractors"
EVENT_DATA = ROOT / "src" / "Challenges" / "utils" / "EventData.cs"
ENGINE = ROOT / "src" / "Challenges" / "classes" / "ChallengeEngine.cs"
ACTIONS = ROOT / "src" / "Challenges" / "classes" / "ChallengeEngine.Actions.cs"
OUT = ROOT / "builder" / "catalog.json"
WEAPONS_FILE = ROOT / "tools" / "catalog_weapons.json"

SKIP_FILES = {"IExtractor.cs", "Registry.cs"}

RE_CLASS = re.compile(r"public\s+sealed\s+class\s+(\w+)\s*:\s*IExtractor")
RE_EVENT_CLASS = re.compile(r'EventClassName\s*=>\s*"([^"]+)"')
RE_CHALLENGE_TYPES = re.compile(
    r"ChallengeTypes\s*\{\s*get;\s*\}\s*=\s*\[(.*?)\]",
    re.S,
)
RE_STRING = re.compile(r'"([^"]+)"')
RE_SUFFIX = re.compile(r'd\s*\[\s*\$"\{prefix\}\.([^"]+)"\s*\]')
RE_CASE = re.compile(r'case\s+"([^"]+)"')
RE_FILL_TOKEN = re.compile(
    r'd\s*\[\s*"([^"]+)"\s*\]|EventData\.FillPlayer\s*\(\s*\w+\s*,\s*[^,]+,\s*"([^"]+)"\s*\)'
)


def read(path: Path) -> str:
    return path.read_text(encoding="utf-8")


def player_suffixes(source: str) -> list[str]:
    found = RE_SUFFIX.findall(source)
    if not found:
        raise SystemExit(f"no player property suffixes found in {EVENT_DATA}")
    return found


def parse_string_list(blob: str) -> list[str]:
    return RE_STRING.findall(blob)


def parse_extractor(path: Path, suffixes: list[str]) -> list[dict]:
    source = read(path)
    class_match = RE_CLASS.search(source)
    event_match = RE_EVENT_CLASS.search(source)
    types_match = RE_CHALLENGE_TYPES.search(source)
    if not class_match or not event_match or not types_match:
        raise SystemExit(f"failed to parse extractor: {path.name}")

    class_name = class_match.group(1)
    event_class = event_match.group(1)
    challenge_types = parse_string_list(types_match.group(1))
    if not challenge_types:
        raise SystemExit(f"no challenge types in {path.name}")

    keys: list[str] = []
    seen: set[str] = set()

    def add(key: str) -> None:
        if key not in seen:
            seen.add(key)
            keys.append(key)

    fill_start = source.find("void Fill(")
    fill_body = source[fill_start:] if fill_start >= 0 else source
    for match in RE_FILL_TOKEN.finditer(fill_body):
        literal, prefix = match.group(1), match.group(2)
        if literal:
            add(literal)
        else:
            for suffix in suffixes:
                add(f"{prefix}.{suffix}")

    return [
        {
            "type": challenge_type,
            "event_class": event_class,
            "extractor": class_name,
            "keys": list(keys),
        }
        for challenge_type in challenge_types
    ]


def parse_cases(path: Path) -> list[str]:
    return RE_CASE.findall(read(path))


def load_weapons() -> list[str]:
    if not WEAPONS_FILE.is_file():
        raise SystemExit(f"missing curated weapons list: {WEAPONS_FILE}")
    weapons = json.loads(WEAPONS_FILE.read_text(encoding="utf-8"))
    if not isinstance(weapons, list) or not all(isinstance(w, str) for w in weapons):
        raise SystemExit(f"{WEAPONS_FILE} must be a JSON array of strings")
    return weapons


def main() -> int:
    suffixes = player_suffixes(read(EVENT_DATA))
    operators = parse_cases(ENGINE)
    action_types: list[str] = []
    seen_actions: set[str] = set()
    for action in parse_cases(ACTIONS):
        if action not in seen_actions:
            seen_actions.add(action)
            action_types.append(action)

    events: list[dict] = []
    for path in sorted(EXTRACTORS_DIR.glob("*.cs")):
        if path.name in SKIP_FILES:
            continue
        events.extend(parse_extractor(path, suffixes))

    events.sort(key=lambda e: (e["type"], e["event_class"]))

    catalog = {
        "operators": operators,
        "action_types": action_types,
        "weapons": load_weapons(),
        "player_property_suffixes": suffixes,
        "events": events,
    }

    OUT.parent.mkdir(parents=True, exist_ok=True)
    OUT.write_text(json.dumps(catalog, indent=2, ensure_ascii=False) + "\n", encoding="utf-8")
    print(
        f"wrote {OUT.relative_to(ROOT)} "
        f"({len(events)} events, {len(operators)} operators, {len(action_types)} actions)"
    )
    return 0


if __name__ == "__main__":
    sys.exit(main())

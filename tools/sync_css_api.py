#!/usr/bin/env python3
"""Download and normalize CounterStrikeSharp GameEvents + Listeners into tools/css/snapshot.json."""

from __future__ import annotations

import json
import re
import sys
import urllib.error
import urllib.request
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
OUT_DIR = ROOT / "tools" / "css"
OUT = OUT_DIR / "snapshot.json"

REF = "main"
RAW = f"https://raw.githubusercontent.com/roflmuffin/CounterStrikeSharp/{REF}"
API_TREE = (
    "https://api.github.com/repos/roflmuffin/CounterStrikeSharp/git/trees/"
    f"{REF}:managed/CounterStrikeSharp.API/Generated/GameEvents"
)
API_COMMIT = f"https://api.github.com/repos/roflmuffin/CounterStrikeSharp/commits/{REF}"

SKIP_LISTENERS = {
    "OnTick",
    "OnServerPreEntityThink",
    "OnServerPostEntityThink",
    "OnServerPreWorldUpdate",
    "OnUpdateWhenNotInGame",
    "CheckTransmit",
}

# Game event names (EventName attribute) to exclude from the snapshot.
SKIP_EVENT_NAME_PREFIXES = (
    "hltv_",
)


def is_skipped_event(event_name: str) -> bool:
    return event_name.startswith(SKIP_EVENT_NAME_PREFIXES)

RE_EVENT_NAME = re.compile(r'\[EventName\("([^"]+)"\)\]')
RE_CLASS = re.compile(r"public\s+class\s+(Event\w+)\s*:\s*GameEvent")
RE_PROP = re.compile(
    r"public\s+(?P<type>[\w\.<>\?]+)\s+(?P<name>\w+)\s*\{[^}]*?"
    r"get\s*=>\s*(?P<getter>Get(?:Player)?)\s*(?:<[^>]+>)?\s*\(\s*\"(?P<field>[^\"]+)\"\s*\)",
    re.S,
)
RE_LISTENER_NAME = re.compile(r"\[ListenerName\(\"([^\"]+)\"\)\]")
RE_DELEGATE_HEAD = re.compile(
    r"public\s+delegate\s+(?P<ret>\w+)\s+(?P<method>\w+)\s*\(",
)
RE_PARAM = re.compile(r"(?P<type>[\w\.<>\?]+)\s+(?P<name>\w+)\s*(?:=[^,]*)?$")
RE_DAMAGE_PROP = re.compile(
    r"public\s+(?:ref\s+)?(?P<type>[\w\.<>\?]+)\s+(?P<name>\w+)\s*=>",
)


def fetch(url: str) -> str:
    req = urllib.request.Request(url, headers={"User-Agent": "cs2-challenges-css-sync"})
    with urllib.request.urlopen(req, timeout=60) as resp:
        return resp.read().decode("utf-8")


def fetch_json(url: str) -> object:
    return json.loads(fetch(url))


def map_event_kind(csharp_type: str, getter: str) -> str:
    if getter == "GetPlayer" or csharp_type.rstrip("?").endswith("CCSPlayerController"):
        return "player"
    base = csharp_type.rstrip("?").split(".")[-1]
    if base == "bool":
        return "bool"
    if base in {"int", "Int32", "long", "Int64", "short", "Int16", "byte", "sbyte", "uint", "ulong", "UInt32", "UInt64"}:
        return "int"
    if base in {"float", "Single", "double", "Double"}:
        return "float"
    if base == "string" or base == "String":
        return "string"
    return "tostring"


def parse_event(source: str) -> dict:
    event_name_match = RE_EVENT_NAME.search(source)
    class_match = RE_CLASS.search(source)
    if not event_name_match or not class_match:
        raise ValueError("missing EventName or class")
    props: list[dict] = []
    seen: set[str] = set()
    for match in RE_PROP.finditer(source):
        field = match.group("field")
        if field in seen:
            continue
        seen.add(field)
        kind = map_event_kind(match.group("type"), match.group("getter"))
        props.append(
            {
                "csharp": match.group("name"),
                "field": field,
                "type": match.group("type").rstrip("?"),
                "kind": kind,
            }
        )
    return {
        "class_name": class_match.group(1),
        "event_name": event_name_match.group(1),
        "properties": props,
    }


def map_listener_param_kind(csharp_type: str, name: str) -> str:
    base = csharp_type.rstrip("?").split(".")[-1]
    if base == "CCSPlayerController":
        return "player"
    if base == "CCSPlayerPawn":
        return "pawn"
    if base in {"int", "Int32"} and ("slot" in name.lower() or name.lower() in {"player", "playerslot"}):
        return "slot"
    if base in {"int", "Int32", "long", "Int64", "short", "byte", "uint", "ulong"}:
        return "int"
    if base in {"float", "Single", "double"}:
        return "float"
    if base == "bool":
        return "bool"
    if base == "string" or base == "String":
        return "string"
    if base == "SteamID":
        return "steamid"
    if base in {"CEntityInstance", "CBaseEntity"}:
        return "entity"
    if base == "CTakeDamageInfo":
        return "damage_info"
    if base == "CTakeDamageResult":
        return "damage_result"
    if base == "PlayerButtons":
        return "buttons"
    if base == "ResourceManifest":
        return "skip"
    if base == "CCSCustomHudLayout":
        return "skip"
    if base == "CCheckTransmitInfoList":
        return "skip"
    return "tostring"


def _split_params(blob: str) -> list[str]:
    parts: list[str] = []
    depth = 0
    start = 0
    for i, ch in enumerate(blob):
        if ch in "(<[":
            depth += 1
        elif ch in ")>]":
            depth = max(0, depth - 1)
        elif ch == "," and depth == 0:
            parts.append(blob[start:i])
            start = i + 1
    tail = blob[start:].strip()
    if tail:
        parts.append(tail)
    return parts


def _closing_paren(source: str, open_index: int) -> int:
    depth = 0
    for i in range(open_index, len(source)):
        ch = source[i]
        if ch == "(":
            depth += 1
        elif ch == ")":
            depth -= 1
            if depth == 0:
                return i
    raise ValueError("unbalanced parentheses")


def parse_listeners(source: str) -> list[dict]:
    listeners: list[dict] = []
    for name_match in RE_LISTENER_NAME.finditer(source):
        name = name_match.group(1)
        rest = source[name_match.end() : name_match.end() + 400]
        del_match = RE_DELEGATE_HEAD.search(rest)
        if not del_match:
            raise ValueError(f"missing delegate after ListenerName {name}")
        if name in SKIP_LISTENERS:
            continue
        abs_open = name_match.end() + del_match.end() - 1
        abs_close = _closing_paren(source, abs_open)
        blob = source[abs_open + 1 : abs_close].strip()
        params: list[dict] = []
        if blob:
            for part in _split_params(blob):
                part = re.sub(r"\[[^\]]*\]", "", part).strip()
                pm = RE_PARAM.search(part)
                if not pm:
                    raise ValueError(f"unparsed listener param in {name}: {part!r}")
                ptype = pm.group("type")
                pname = pm.group("name")
                params.append(
                    {
                        "name": pname,
                        "type": ptype,
                        "kind": map_listener_param_kind(ptype, pname),
                    }
                )
        method = del_match.group("method")
        listeners.append(
            {
                "name": method,
                "listener_name": name,
                "method": method,
                "return": del_match.group("ret"),
                "parameters": params,
            }
        )
    return listeners


def parse_damage_schema(source: str, class_name: str) -> list[dict]:
    props: list[dict] = []
    for match in RE_DAMAGE_PROP.finditer(source):
        csharp_type = match.group("type")
        name = match.group("name")
        base = csharp_type.rstrip("?").split(".")[-1]
        if base in {"Vector", "CHandle", "CTakeDamageInfo"} or "CHandle" in csharp_type:
            continue
        if base == "bool":
            kind = "bool"
        elif base in {"float", "Single", "double"}:
            kind = "float"
        elif base in {"int", "Int32", "byte", "long", "Int64"}:
            kind = "int"
        else:
            kind = "tostring"
        props.append({"csharp": name, "field": _camel_to_snake(name), "kind": kind, "type": csharp_type})
    return props


def _camel_to_snake(name: str) -> str:
    out: list[str] = []
    for i, ch in enumerate(name):
        if ch.isupper() and i > 0:
            out.append("_")
        out.append(ch.lower())
    return "".join(out)


def main() -> int:
    print(f"ref {REF}")
    commit = fetch_json(API_COMMIT)
    commit_sha = commit.get("sha", "")
    print(f"  tip {commit_sha[:12] or '?'}")

    tree = fetch_json(API_TREE)
    files = sorted(
        t["path"]
        for t in tree["tree"]
        if t["path"].endswith(".g.cs") and t["path"].startswith("Event")
    )
    events: list[dict] = []
    skipped_events: list[str] = []
    for i, name in enumerate(files, 1):
        url = f"{RAW}/managed/CounterStrikeSharp.API/Generated/GameEvents/{name}"
        try:
            source = fetch(url)
        except urllib.error.HTTPError as exc:
            raise SystemExit(f"failed to fetch {name}: {exc}") from exc
        try:
            parsed = parse_event(source)
        except ValueError as exc:
            raise SystemExit(f"failed to parse {name}: {exc}") from exc
        if is_skipped_event(parsed["event_name"]):
            skipped_events.append(parsed["event_name"])
        else:
            events.append(parsed)
        if i % 50 == 0 or i == len(files):
            print(f"  events {i}/{len(files)}")

    skipped_events.sort()
    print(f"  kept {len(events)} events (skipped {len(skipped_events)})")

    listeners_src = fetch(f"{RAW}/managed/CounterStrikeSharp.API/Core/Listeners.g.cs")
    listeners = parse_listeners(listeners_src)
    print(f"  listeners {len(listeners)} (skipped {len(SKIP_LISTENERS)} hot)")

    damage_info = parse_damage_schema(
        fetch(f"{RAW}/managed/CounterStrikeSharp.API/Generated/Schema/Classes/CTakeDamageInfo.g.cs"),
        "CTakeDamageInfo",
    )
    damage_result = parse_damage_schema(
        fetch(f"{RAW}/managed/CounterStrikeSharp.API/Generated/Schema/Classes/CTakeDamageResult.g.cs"),
        "CTakeDamageResult",
    )

    snapshot = {
        "ref": REF,
        "commit": commit_sha,
        "source": {
            "game_events": "managed/CounterStrikeSharp.API/Generated/GameEvents",
            "listeners": "managed/CounterStrikeSharp.API/Core/Listeners.g.cs",
        },
        "skipped_listeners": sorted(SKIP_LISTENERS),
        "skipped_event_name_prefixes": list(SKIP_EVENT_NAME_PREFIXES),
        "skipped_events": skipped_events,
        "events": events,
        "listeners": listeners,
        "damage_info_fields": damage_info,
        "damage_result_fields": damage_result,
    }

    OUT_DIR.mkdir(parents=True, exist_ok=True)
    OUT.write_text(json.dumps(snapshot, indent=2, ensure_ascii=False) + "\n", encoding="utf-8")
    print(f"wrote {OUT.relative_to(ROOT)} ({len(events)} events, {len(listeners)} listeners)")
    return 0


if __name__ == "__main__":
    sys.exit(main())

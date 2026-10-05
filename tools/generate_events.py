#!/usr/bin/env python3
"""Generate extractors, forwarders, docs, and catalog entries from the CSS snapshot."""

from __future__ import annotations

import json
import re
import shutil
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SNAPSHOT = ROOT / "tools" / "css" / "snapshot.json"
ALIASES = ROOT / "tools" / "event_aliases.json"
EXTRACTORS_DIR = ROOT / "src" / "Challenges" / "extractors"
ENGINE_DIR = ROOT / "src" / "Challenges" / "classes"
DOCS_EVENTS = ROOT / "documentation" / "events"
DOCS_INDEX = ROOT / "documentation" / "events.md"
EVENT_DATA = ROOT / "src" / "Challenges" / "utils" / "EventData.cs"
ENGINE = ROOT / "src" / "Challenges" / "classes" / "ChallengeEngine.cs"
ACTIONS = ROOT / "src" / "Challenges" / "classes" / "ChallengeEngine.Actions.cs"
WEAPONS_FILE = ROOT / "tools" / "catalog_weapons.json"
CATALOG_OUT = ROOT / "builder" / "catalog.json"

KEEP_EXTRACTOR_FILES = {"IExtractor.cs", "DerivedKeys.cs", "Registry.cs"}

# Keep in sync with tools/sync_css_api.py SKIP_EVENT_NAME_PREFIXES.
SKIP_EVENT_NAME_PREFIXES = (
    "hltv_",
)

RE_SUFFIX = re.compile(r'd\s*\[\s*\$"\{prefix\}\.([^"]+)"\s*\]\s*=\s*(.+);')
RE_CASE = re.compile(r'case\s+"([^"]+)"')

KIND_TO_TYPE = {
    "bool": "bool",
    "int": "int",
    "float": "float",
    "string": "string",
    "tostring": "string",
    "buttons": "int",
    "steamid": "string",
}


def load_json(path: Path) -> dict:
    return json.loads(path.read_text(encoding="utf-8"))


def camel_to_snake(name: str) -> str:
    out: list[str] = []
    for i, ch in enumerate(name):
        if ch.isupper() and i > 0:
            prev = name[i - 1]
            nxt = name[i + 1] if i + 1 < len(name) else ""
            if not prev.isupper() or (nxt and not nxt.isupper()):
                out.append("_")
        out.append(ch.lower())
    return "".join(out)


def extractor_class_name(event_class: str) -> str:
    return event_class[5:] if event_class.startswith("Event") else event_class


def player_props(event: dict) -> list[dict]:
    return [p for p in event["properties"] if p["kind"] == "player"]


def alias_for(aliases: dict, key: str) -> dict:
    return aliases.get("events", {}).get(key) or aliases.get("listeners", {}).get(key) or {}


def ordered_unique(items: list[str]) -> list[str]:
    seen: set[str] = set()
    out: list[str] = []
    for item in items:
        if item not in seen:
            seen.add(item)
            out.append(item)
    return out


def event_challenge_types(event: dict, alias: dict) -> tuple[list[str], str]:
    """Return (challenge_types, audience). Alias types replace CSS auto-names when present."""
    players = player_props(event)
    extra = alias.get("types", {})
    audience = "all" if alias.get("audience") == "all" or not players else "players"

    if extra:
        types: list[str] = []
        for ts in extra.values():
            types.extend(ts)
        return ordered_unique(types), audience

    if not players or audience == "all" or len(players) == 1:
        return [event["event_name"]], audience
    return [f"{event['event_name']}_{p['field']}" for p in players], audience


def listener_challenge_types(listener: dict, alias: dict) -> list[str]:
    types = [camel_to_snake(listener["name"])]
    for ts in alias.get("types", {}).values():
        types.extend(ts)
    return ordered_unique(types)


def csharp_expr_for_prop(prop: dict, bool_coerce: set[str]) -> str:
    field = prop["field"]
    csharp = prop["csharp"]
    kind = prop["kind"]
    if kind == "player":
        return ""  # handled via FillPlayer
    if field in bool_coerce:
        return f'EventData.Bool(e.{csharp} > 0)'
    if kind == "bool":
        return f"EventData.Bool(e.{csharp})"
    if kind in {"int", "float"}:
        return f"EventData.Num(e.{csharp})"
    if kind == "string":
        return f"e.{csharp}"
    if kind == "tostring":
        return f"e.{csharp}.ToString()"
    raise SystemExit(f"unsupported property kind {kind} for {field}")


def emit_fill_keys(props: list[dict], alias: dict, indent: str = "            ") -> list[str]:
    bool_coerce = set(alias.get("bool_coerce", []))
    key_aliases: dict[str, list[str]] = alias.get("key_aliases", {})
    lines: list[str] = []
    for prop in props:
        if prop["kind"] == "player":
            continue
        expr = csharp_expr_for_prop(prop, bool_coerce)
        field = prop["field"]
        lines.append(f'{indent}d["{field}"] = {expr};')
        for alt in key_aliases.get(field, []):
            lines.append(f'{indent}d["{alt}"] = d["{field}"];')
    return lines


def emit_fill_players(props: list[dict], alias: dict, indent: str = "            ") -> list[str]:
    players = [p for p in props if p["kind"] == "player"]
    lines: list[str] = []
    for prop in players:
        field = prop["field"]
        csharp = prop["csharp"]
        for prefix in player_prefixes_for(field, players, alias):
            lines.append(f'{indent}EventData.FillPlayer(d, e.{csharp}, "{prefix}");')
    return lines


def emit_targets(event: dict, alias: dict, types: list[str], audience: str) -> list[str]:
    players = player_props(event)
    extra = alias.get("types", {})
    lines: list[str] = ["        {"]
    needs_event = audience != "all" and bool(players)
    if needs_event:
        lines.append(f"            var e = ({event['class_name']})gameEvent;")

    if audience == "all":
        lines.append("            foreach (var entry in Utilities.GetPlayers())")
        lines.append("            {")
        for t in types:
            lines.append(f'                yield return (entry, "{t}");')
        lines.append("            }")
    elif extra:
        for prop in players:
            for t in extra.get(prop["field"], []):
                lines.append(f'            yield return (e.{prop["csharp"]}, "{t}");')
    elif len(players) == 1:
        prop = players[0]
        lines.append(f'            yield return (e.{prop["csharp"]}, "{types[0]}");')
    else:
        for prop, t in zip(players, types, strict=True):
            lines.append(f'            yield return (e.{prop["csharp"]}, "{t}");')

    lines.append("        }")
    return lines


def generate_event_extractor(event: dict, alias: dict) -> str:
    cls = extractor_class_name(event["class_name"])
    types, audience = event_challenge_types(event, alias)
    types_literal = ", ".join(f'"{t}"' for t in types)
    derived = alias.get("derived")
    fill_lines = emit_fill_keys(event["properties"], alias)
    fill_lines.extend(emit_fill_players(event["properties"], alias))
    if derived:
        fill_lines.append(f'            DerivedKeys.Apply("{derived}", gameEvent, d);')

    needs_utilities = audience == "all"
    needs_utils = bool(fill_lines)
    usings = ["using CounterStrikeSharp.API.Core;", "using CounterStrikeSharp.API.Modules.Events;"]
    if needs_utilities:
        usings.insert(0, "using CounterStrikeSharp.API;")
    if needs_utils:
        usings.append("using Challenges.Utils;")

    if fill_lines:
        body_fill = "\n".join(
            [
                "        public void Fill(GameEvent gameEvent, Dictionary<string, string> d)",
                "        {",
                f"            var e = ({event['class_name']})gameEvent;",
                *fill_lines,
                "        }",
            ]
        )
    else:
        body_fill = "\n".join(
            [
                "        public void Fill(GameEvent gameEvent, Dictionary<string, string> d)",
                "        {",
                "        }",
            ]
        )

    body_targets = "\n".join(
        [
            "        public IEnumerable<(CCSPlayerController? Player, string ChallengeType)> Targets(GameEvent gameEvent)",
            *emit_targets(event, alias, types, audience),
        ]
    )

    return "\n".join(
        [
            "// <auto-generated> by tools/generate_events.py — do not edit.",
            "#nullable enable",
            *usings,
            "",
            "namespace Challenges.Extractors",
            "{",
            f"    public sealed class {cls} : IExtractor",
            "    {",
            "        public ExtractorKind Kind => ExtractorKind.GameEvent;",
            f'        public string EventClassName => "{event["class_name"]}";',
            f"        public IReadOnlyList<string> ChallengeTypes {{ get; }} = [{types_literal}];",
            "",
            body_fill,
            "",
            body_targets,
            "    }",
            "}",
            "",
        ]
    )


def generate_listener_extractor(listener: dict, alias: dict) -> str:
    name = listener["name"]
    types = listener_challenge_types(listener, alias)
    types_literal = ", ".join(f'"{t}"' for t in types)
    cls = name  # OnMapStart etc.
    return "\n".join(
        [
            "// <auto-generated> by tools/generate_events.py — do not edit.",
            "#nullable enable",
            "using CounterStrikeSharp.API.Core;",
            "using CounterStrikeSharp.API.Modules.Events;",
            "",
            "namespace Challenges.Extractors",
            "{",
            f"    public sealed class Listener{cls} : IExtractor",
            "    {",
            "        public ExtractorKind Kind => ExtractorKind.Listener;",
            f'        public string EventClassName => "{name}";',
            f"        public IReadOnlyList<string> ChallengeTypes {{ get; }} = [{types_literal}];",
            "",
            "        public void Fill(GameEvent gameEvent, Dictionary<string, string> d)",
            "        {",
            "        }",
            "",
            "        public IEnumerable<(CCSPlayerController? Player, string ChallengeType)> Targets(GameEvent gameEvent)",
            "        {",
            "            yield break;",
            "        }",
            "    }",
            "}",
            "",
        ]
    )


def listener_player_params(listener: dict) -> list[dict]:
    return [p for p in listener["parameters"] if p["kind"] in {"player", "pawn", "slot"}]


def emit_listener_fill(listener: dict, alias: dict, snapshot: dict) -> tuple[list[str], list[str]]:
    """Return (setup_lines, target_player_expr_or_empty)."""
    key_aliases = alias.get("key_aliases", {})
    prefixes = alias.get("player_prefixes", {})
    lines: list[str] = ["            Dictionary<string, string> d = [];"]
    target_exprs: list[str] = []

    for param in listener["parameters"]:
        kind = param["kind"]
        name = param["name"]
        if kind == "skip":
            continue
        if kind == "player":
            prefix = prefixes.get("player", "player")
            lines.append(f'            EventData.FillPlayer(d, {name}, "{prefix}");')
            if name != prefix:
                lines.append(f'            EventData.FillPlayer(d, {name}, "{name}");')
            target_exprs.append(name)
        elif kind == "pawn":
            lines.append(f"            CCSPlayerController? {name}Controller = {name}.OriginalController?.Value;")
            lines.append(f'            EventData.FillPlayer(d, {name}Controller, "player");')
            target_exprs.append(f"{name}Controller")
        elif kind == "slot":
            lines.append(f"            CCSPlayerController? {name}Player = Utilities.GetPlayerFromSlot({name});")
            lines.append(f'            EventData.FillPlayer(d, {name}Player, "player");')
            target_exprs.append(f"{name}Player")
        elif kind == "string":
            lines.append(f'            d["{name.lower()}"] = {name};')
            for alt in key_aliases.get(name.lower(), []):
                lines.append(f'            d["{alt}"] = d["{name.lower()}"];')
        elif kind == "bool":
            lines.append(f'            d["{name.lower()}"] = EventData.Bool({name});')
            for alt in key_aliases.get(name.lower(), []):
                lines.append(f'            d["{alt}"] = d["{name.lower()}"];')
        elif kind == "int":
            lines.append(f'            d["{name.lower()}"] = EventData.Num({name});')
        elif kind == "float":
            lines.append(f'            d["{name.lower()}"] = EventData.Num({name});')
        elif kind == "steamid":
            lines.append(f'            d["{name.lower()}"] = {name}.SteamId64.ToString();')
        elif kind == "entity":
            lines.append(f'            d["{name}.designername"] = {name}.DesignerName;')
            lines.append(f'            d["{name}.index"] = EventData.Num({name}.Index);')
        elif kind == "buttons":
            lines.append(f'            d["{name.lower()}"] = EventData.Num((ulong){name});')
        elif kind == "damage_info":
            prefix = "damage"
            for field in snapshot["damage_info_fields"]:
                fk = field["field"]
                cs = field["csharp"]
                k = field["kind"]
                if k == "bool":
                    lines.append(f'            d["{prefix}.{fk}"] = EventData.Bool({name}.{cs});')
                elif k in {"int", "float"}:
                    lines.append(f'            d["{prefix}.{fk}"] = EventData.Num({name}.{cs});')
                else:
                    lines.append(f'            d["{prefix}.{fk}"] = {name}.{cs}.ToString();')
        elif kind == "damage_result":
            prefix = "result"
            for field in snapshot["damage_result_fields"]:
                fk = field["field"]
                cs = field["csharp"]
                k = field["kind"]
                if k == "bool":
                    lines.append(f'            d["{prefix}.{fk}"] = EventData.Bool({name}.{cs});')
                elif k in {"int", "float"}:
                    lines.append(f'            d["{prefix}.{fk}"] = EventData.Num({name}.{cs});')
                else:
                    lines.append(f'            d["{prefix}.{fk}"] = {name}.{cs}.ToString();')
        elif kind == "tostring":
            lines.append(f'            d["{name.lower()}"] = {name}.ToString();')
        else:
            raise SystemExit(f"unsupported listener param kind {kind} in {listener['name']}")

    return lines, target_exprs


def generate_listener_method(listener: dict, alias: dict, snapshot: dict) -> str:
    name = listener["name"]
    types = listener_challenge_types(listener, alias)
    params = ", ".join(f"{p['type']} {p['name']}" for p in listener["parameters"])
    ret = listener["return"]
    fill_lines, target_exprs = emit_listener_fill(listener, alias, snapshot)

    target_block: list[str] = []
    if target_exprs:
        target_block.append("            List<(CCSPlayerController? Player, string Type)> targets = [];")
        target_block.append(f"            CCSPlayerController? targetPlayer = {target_exprs[0]};")
        for t in types:
            target_block.append(f'            targets.Add((targetPlayer, "{t}"));')
        target_block.append(f'            HandleListener("{name}", d, targets);')
    else:
        target_block.append("            List<(CCSPlayerController? Player, string Type)> targets = [];")
        target_block.append("            foreach (var entry in Utilities.GetPlayers())")
        target_block.append("            {")
        for t in types:
            target_block.append(f'                targets.Add((entry, "{t}"));')
        target_block.append("            }")
        target_block.append(f'            HandleListener("{name}", d, targets);')

    lines = [
        f"        public {ret} {name}({params})",
        "        {",
        *fill_lines,
        *target_block,
    ]
    if ret == "HookResult":
        lines.append("            return HookResult.Continue;")
    lines.append("        }")
    return "\n".join(lines)


def write_generated_extractors(snapshot: dict, aliases: dict) -> list[str]:
    parts: list[str] = [
        "// <auto-generated> by tools/generate_events.py — do not edit.",
        "",
    ]
    registry_names: list[str] = []

    # One file with all extractors would be huge for usings conflicts - emit per-class files
    # under extractors/generated/ instead for clarity.
    gen_dir = EXTRACTORS_DIR / "generated"
    if gen_dir.exists():
        shutil.rmtree(gen_dir)
    gen_dir.mkdir(parents=True)

    for event in snapshot["events"]:
        alias = alias_for(aliases, event["class_name"])
        cls = extractor_class_name(event["class_name"])
        source = generate_event_extractor(event, alias)
        (gen_dir / f"{cls}.g.cs").write_text(source, encoding="utf-8")
        registry_names.append(cls)

    for listener in snapshot["listeners"]:
        alias = alias_for(aliases, listener["name"])
        # Also allow alias keyed by listener_name attribute
        if not alias and listener.get("listener_name"):
            alias = alias_for(aliases, listener["listener_name"])
        source = generate_listener_extractor(listener, alias)
        (gen_dir / f"Listener{listener['name']}.g.cs").write_text(source, encoding="utf-8")
        registry_names.append(f"Listener{listener['name']}")

    return registry_names


def write_registry(names: list[str]) -> None:
    lines = [
        "// <auto-generated> by tools/generate_events.py — do not edit.",
        "namespace Challenges.Extractors",
        "{",
        "    public static class Registry",
        "    {",
        "        public static IReadOnlyList<IExtractor> All { get; } =",
        "        [",
    ]
    for name in names:
        lines.append(f"            new {name}(),")
    lines.extend(
        [
            "        ];",
            "    }",
            "}",
            "",
        ]
    )
    (EXTRACTORS_DIR / "Registry.cs").write_text("\n".join(lines), encoding="utf-8")


def write_engine_events(snapshot: dict) -> None:
    lines = [
        "// <auto-generated> by tools/generate_events.py — do not edit.",
        "#nullable enable",
        "using CounterStrikeSharp.API.Core;",
        "using CounterStrikeSharp.API.Modules.Events;",
        "",
        "namespace Challenges.Classes",
        "{",
        "    public partial class ChallengeEngine",
        "    {",
    ]
    for event in snapshot["events"]:
        cls = event["class_name"]
        lines.append(
            f"        public HookResult {cls}({cls} @event, GameEventInfo info) => HandleEvent(\"{cls}\", @event);"
        )
    lines.extend(["    }", "}", ""])
    (ENGINE_DIR / "ChallengeEngine.Events.cs").write_text("\n".join(lines), encoding="utf-8")


def write_engine_listeners(snapshot: dict, aliases: dict) -> None:
    lines = [
        "// <auto-generated> by tools/generate_events.py — do not edit.",
        "#nullable enable",
        "using CounterStrikeSharp.API;",
        "using CounterStrikeSharp.API.Core;",
        "using CounterStrikeSharp.API.Modules.Entities;",
        "using CounterStrikeSharp.API.Modules.Utils;",
        "using Challenges.Utils;",
        "",
        "namespace Challenges.Classes",
        "{",
        "    public partial class ChallengeEngine",
        "    {",
    ]
    for listener in snapshot["listeners"]:
        alias = alias_for(aliases, listener["name"])
        if not alias and listener.get("listener_name"):
            alias = alias_for(aliases, listener["listener_name"])
        lines.append(generate_listener_method(listener, alias, snapshot))
        lines.append("")
    lines.extend(["    }", "}", ""])
    (ENGINE_DIR / "ChallengeEngine.Listeners.cs").write_text("\n".join(lines), encoding="utf-8")


def player_suffixes() -> list[dict]:
    """Return [{suffix, type}, ...] inferred from EventData.FillPlayer assignments."""
    found: list[dict] = []
    seen: set[str] = set()
    for suffix, rhs in RE_SUFFIX.findall(EVENT_DATA.read_text(encoding="utf-8")):
        if suffix in seen:
            continue
        seen.add(suffix)
        if "Bool(" in rhs:
            typ = "bool"
        elif "Num(" in rhs:
            typ = "int"
        else:
            typ = "string"
        found.append({"suffix": suffix, "type": typ})
    if not found:
        raise SystemExit(f"no player property suffixes in {EVENT_DATA}")
    return found


def value_type(kind: str) -> str:
    return KIND_TO_TYPE.get(kind, "string")


def player_prefixes_for(prop_field: str, players: list[dict], alias: dict) -> list[str]:
    prefixes = alias.get("player_prefixes", {})
    legacy = prefixes.get(prop_field)
    names = [prop_field]
    if legacy and legacy != prop_field:
        names.append(legacy)
    elif len(players) == 1 and prop_field != "player":
        names.append("player")
    return ordered_unique(names)


def catalog_keys_for_event(event: dict, alias: dict, suffixes: list[dict]) -> list[dict]:
    keys: list[dict] = []
    seen: set[str] = set()

    def add(key: str, typ: str) -> None:
        if key not in seen:
            seen.add(key)
            keys.append({"key": key, "type": typ})

    key_aliases = alias.get("key_aliases", {})
    players = player_props(event)
    bool_coerce = set(alias.get("bool_coerce", []))

    for prop in event["properties"]:
        if prop["kind"] == "player":
            for prefix in player_prefixes_for(prop["field"], players, alias):
                for suffix in suffixes:
                    add(f"{prefix}.{suffix['suffix']}", suffix["type"])
            continue

        typ = "bool" if prop["field"] in bool_coerce else value_type(prop["kind"])
        add(prop["field"], typ)
        for alt in key_aliases.get(prop["field"], []):
            add(alt, typ)

    derived = alias.get("derived")
    if derived == "team_self_kill":
        add("isteamkill", "bool")
        add("isselfkill", "bool")
    elif derived == "team_self_damage":
        add("isteamdamage", "bool")
        add("isselfdamage", "bool")
    elif derived == "team_self_flash":
        add("isteamflash", "bool")
        add("isselfflash", "bool")
    elif derived == "self_avenged":
        add("isselfavenged", "bool")
    elif derived == "player_team_names":
        add("old_team", "string")
        add("new_team", "string")

    return keys


def catalog_keys_for_listener(listener: dict, alias: dict, suffixes: list[dict], snapshot: dict) -> list[dict]:
    keys: list[dict] = []
    seen: set[str] = set()

    def add(key: str, typ: str) -> None:
        if key not in seen:
            seen.add(key)
            keys.append({"key": key, "type": typ})

    key_aliases = alias.get("key_aliases", {})
    for param in listener["parameters"]:
        kind = param["kind"]
        name = param["name"]
        if kind == "skip":
            continue
        if kind in {"player", "pawn", "slot"}:
            for suffix in suffixes:
                add(f"player.{suffix['suffix']}", suffix["type"])
        elif kind == "entity":
            add(f"{name}.designername", "string")
            add(f"{name}.index", "int")
        elif kind == "damage_info":
            for field in snapshot["damage_info_fields"]:
                add(f"damage.{field['field']}", value_type(field["kind"]))
        elif kind == "damage_result":
            for field in snapshot["damage_result_fields"]:
                add(f"result.{field['field']}", value_type(field["kind"]))
        else:
            key = name.lower()
            typ = value_type(kind)
            add(key, typ)
            for alt in key_aliases.get(key, []):
                add(alt, typ)
    return keys


def format_key_docs(keys: list[dict], prefixes: list[str]) -> list[str]:
    lines: list[str] = []
    if prefixes:
        joined = ", ".join(f"*{p}*" for p in prefixes)
        lines.append(f"- [Player Data](../rules/GlobalPlayerData.md): prefixes {joined}")
    for entry in keys:
        if any(entry["key"].startswith(f"{p}.") for p in prefixes):
            continue
        lines.append(f"- `{entry['key']} ({entry['type']})`")
    return lines


def write_docs(snapshot: dict, aliases: dict, suffixes: list[dict]) -> None:
    if DOCS_EVENTS.exists():
        shutil.rmtree(DOCS_EVENTS)
    DOCS_EVENTS.mkdir(parents=True)

    index_links: list[tuple[str, str]] = []

    for event in snapshot["events"]:
        alias = alias_for(aliases, event["class_name"])
        types, audience = event_challenge_types(event, alias)
        keys = catalog_keys_for_event(event, alias, suffixes)
        players = player_props(event)
        prefixes = ordered_unique(
            [p for prop in players for p in player_prefixes_for(prop["field"], players, alias)]
        )
        doc_name = event["class_name"]
        lines = [
            f"# {doc_name} ({' / '.join(types)})",
            "",
            f"CSS game event `{event['event_name']}` (`{event['class_name']}`).",
            "",
            f"Audience: {'all connected players' if audience == 'all' else 'event player(s)'}.",
            "",
            "## Challenge types",
            "",
            *[f"- `{t}`" for t in types],
            "",
            "## Available rule keys",
            "",
            "- [Event Data](../rules/GlobalEventData.md)",
            *format_key_docs(keys, prefixes),
            "",
        ]
        (DOCS_EVENTS / f"{doc_name}.md").write_text("\n".join(lines), encoding="utf-8")
        index_links.append((types[0], f"events/{doc_name}.md"))

    for listener in snapshot["listeners"]:
        alias = alias_for(aliases, listener["name"])
        if not alias and listener.get("listener_name"):
            alias = alias_for(aliases, listener["listener_name"])
        types = listener_challenge_types(listener, alias)
        keys = catalog_keys_for_listener(listener, alias, suffixes, snapshot)
        has_player = any(p["kind"] in {"player", "pawn", "slot"} for p in listener["parameters"])
        prefixes = ["player"] if has_player else []
        doc_name = listener["name"]
        lines = [
            f"# {doc_name} ({' / '.join(types)})",
            "",
            f"CSS listener `{listener.get('listener_name', listener['name'])}`.",
            "",
            "## Challenge types",
            "",
            *[f"- `{t}`" for t in types],
            "",
            "## Available rule keys",
            "",
            "- [Event Data](../rules/GlobalEventData.md)",
            *format_key_docs(keys, prefixes),
            "",
        ]
        (DOCS_EVENTS / f"{doc_name}.md").write_text("\n".join(lines), encoding="utf-8")
        index_links.append((types[0], f"events/{doc_name}.md"))

    index_links.sort(key=lambda x: x[0])
    ref = snapshot.get("ref", "main")
    commit = snapshot.get("commit") or ""
    commit_note = f" (synced tip `{commit[:12]}`)" if commit else ""
    DOCS_INDEX.write_text(
        "\n".join(
            [
                "# Event Documentation",
                "",
                "Events and listeners come from CounterStrikeSharp. Challenge types are listed below.",
                "",
                f"Source: CounterStrikeSharp `{ref}`{commit_note}.",
                "",
                "## List of events and listeners",
                "",
                *[f"- [{label}]({href})" for label, href in index_links],
                "",
            ]
        ),
        encoding="utf-8",
    )


def write_catalog(snapshot: dict, aliases: dict, suffixes: list[dict]) -> None:
    operators = RE_CASE.findall(ENGINE.read_text(encoding="utf-8"))
    action_types: list[str] = []
    seen_actions: set[str] = set()
    for action in RE_CASE.findall(ACTIONS.read_text(encoding="utf-8")):
        if action not in seen_actions:
            seen_actions.add(action)
            action_types.append(action)

    if not WEAPONS_FILE.is_file():
        raise SystemExit(f"missing {WEAPONS_FILE}")
    weapons = json.loads(WEAPONS_FILE.read_text(encoding="utf-8"))

    key_sets: dict[str, list[dict]] = {}
    events_out: list[dict] = []

    for event in snapshot["events"]:
        alias = alias_for(aliases, event["class_name"])
        types, _ = event_challenge_types(event, alias)
        key_sets[event["class_name"]] = catalog_keys_for_event(event, alias, suffixes)
        extractor = extractor_class_name(event["class_name"])
        for challenge_type in types:
            events_out.append(
                {
                    "type": challenge_type,
                    "event_class": event["class_name"],
                    "extractor": extractor,
                    "kind": "game_event",
                }
            )

    for listener in snapshot["listeners"]:
        alias = alias_for(aliases, listener["name"])
        if not alias and listener.get("listener_name"):
            alias = alias_for(aliases, listener["listener_name"])
        types = listener_challenge_types(listener, alias)
        key_sets[listener["name"]] = catalog_keys_for_listener(listener, alias, suffixes, snapshot)
        extractor = f"Listener{listener['name']}"
        for challenge_type in types:
            events_out.append(
                {
                    "type": challenge_type,
                    "event_class": listener["name"],
                    "extractor": extractor,
                    "kind": "listener",
                }
            )

    events_out.sort(key=lambda e: (e["type"], e["event_class"]))
    catalog = {
        "operators": operators,
        "action_types": action_types,
        "weapons": weapons,
        "player_property_suffixes": suffixes,
        "css_ref": snapshot.get("ref", "main"),
        "css_commit": snapshot.get("commit") or "",
        "key_sets": key_sets,
        "events": events_out,
    }
    CATALOG_OUT.parent.mkdir(parents=True, exist_ok=True)
    CATALOG_OUT.write_text(json.dumps(catalog, indent=2, ensure_ascii=False) + "\n", encoding="utf-8")
    print(f"wrote {CATALOG_OUT.relative_to(ROOT)} ({len(events_out)} challenge types)")


def clean_old_extractors() -> None:
    for path in EXTRACTORS_DIR.glob("*.cs"):
        if path.name not in KEEP_EXTRACTOR_FILES:
            path.unlink()



def main() -> int:
    if not SNAPSHOT.is_file():
        raise SystemExit(f"missing {SNAPSHOT}; run tools/sync_css_api.py first")
    snapshot = load_json(SNAPSHOT)
    aliases = load_json(ALIASES)
    suffixes = player_suffixes()

    prefixes = tuple(snapshot.get("skipped_event_name_prefixes") or SKIP_EVENT_NAME_PREFIXES)
    before = len(snapshot["events"])
    snapshot["events"] = [
        e for e in snapshot["events"] if not e["event_name"].startswith(prefixes)
    ]
    skipped = before - len(snapshot["events"])

    clean_old_extractors()
    names = write_generated_extractors(snapshot, aliases)
    write_registry(names)
    write_engine_events(snapshot)
    write_engine_listeners(snapshot, aliases)
    write_docs(snapshot, aliases, suffixes)
    write_catalog(snapshot, aliases, suffixes)

    print(
        f"generated {len(snapshot['events'])} events + {len(snapshot['listeners'])} listeners "
        f"({len(names)} registry entries; skipped {skipped} events)"
    )
    return 0


if __name__ == "__main__":
    sys.exit(main())

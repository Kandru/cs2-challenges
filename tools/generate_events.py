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
ENGINE_DIR = ROOT / "src" / "Challenges"
DOCS_EVENTS = ROOT / "documentation" / "reference" / "events"
DOCS_EVENTS_INDEX = ROOT / "documentation" / "reference" / "events.md"
DOCS_LISTENERS_INDEX = ROOT / "documentation" / "reference" / "listeners.md"
# PlayerKeys.Suffixes: player property suffixes, bits, and catalog types.
FILL_DEMAND = ROOT / "src" / "Challenges" / "Utils" / "FillDemand.cs"
# Rule operators live in ChallengeRules; action type cases in ChallengeEngine.Simulate.
RULES = ROOT / "src" / "Challenges" / "Utils" / "ChallengeRules.cs"
ACTIONS = ROOT / "src" / "Challenges" / "ChallengeEngine.Simulate.cs"
WEAPONS_FILE = ROOT / "tools" / "catalog_weapons.json"
CATALOG_OUT = ROOT / "builder" / "catalog.json"

KEEP_EXTRACTOR_FILES = {"IExtractor.cs", "DerivedKeys.cs", "Registry.cs"}

# Keep in sync with tools/sync_css_api.py SKIP_EVENT_NAME_PREFIXES.
SKIP_EVENT_NAME_PREFIXES = (
    "hltv_",
)

RE_SUFFIX = re.compile(r'new\s*\(\s*"([^"]+)"\s*,\s*PlayerFields\.(\w+)\s*,\s*"(\w+)"\s*\)')
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


# Derived key kinds handled by DerivedKeys.Apply, with catalog value types.
DERIVED_KEYS: dict[str, list[tuple[str, str]]] = {
    "team_self_kill": [("isteamkill", "bool"), ("isselfkill", "bool")],
    "team_self_damage": [("isteamdamage", "bool"), ("isselfdamage", "bool")],
    "team_self_flash": [("isteamflash", "bool"), ("isselfflash", "bool")],
    "self_avenged": [("isselfavenged", "bool")],
    "player_team_names": [("old_team", "string"), ("new_team", "string")],
}

FILL_SIGNATURE = "        public void Fill(GameEvent gameEvent, Dictionary<string, string> d, FillDemand need)"
TARGETS_SIGNATURE = (
    "        public void Targets(GameEvent gameEvent, "
    "List<(CCSPlayerController? Player, string ChallengeType)> buffer, FillDemand need)"
)


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


def register(names: list[str], name: str, owner: str) -> int:
    if name in names:
        raise SystemExit(f"duplicate key or prefix {name} in {owner}")
    names.append(name)
    return len(names) - 1


def csharp_list(items: list[str]) -> str:
    return "[" + ", ".join(f'"{i}"' for i in items) + "]"


def emit_key_group(names: list[str], expr: str, keys: list[str], owner: str, indent: str) -> list[str]:
    """Write one value under its key and key aliases; computed only when one of them is needed."""
    idx = [register(keys, n, owner) for n in names]
    inner = indent + "    "
    if len(idx) == 1:
        return [
            f"{indent}if (need.Keys[{idx[0]}])",
            f"{indent}{{",
            f'{inner}d["{names[0]}"] = {expr};',
            f"{indent}}}",
        ]

    cond = " || ".join(f"need.Keys[{i}]" for i in idx)
    lines = [f"{indent}if ({cond})", f"{indent}{{", f"{inner}string value = {expr};"]
    for i, n in zip(idx, names):
        lines.extend(
            [
                f"{inner}if (need.Keys[{i}])",
                f"{inner}{{",
                f'{inner}    d["{n}"] = value;',
                f"{inner}}}",
            ]
        )
    lines.append(f"{indent}}}")
    return lines


def emit_player_fill(expr: str, names: list[str], prefixes: list[str], owner: str, indent: str) -> list[str]:
    """One FillPlayer call per controller; reads each property once even under two prefixes."""
    idx = [register(prefixes, n, owner) for n in names]
    inner = indent + "    "
    if len(idx) == 1:
        i = idx[0]
        return [
            f"{indent}if (need.Prefixes[{i}] != PlayerFields.None)",
            f"{indent}{{",
            f"{inner}EventData.FillPlayer(d, {expr}, need.PrefixKeys[{i}], need.Prefixes[{i}]);",
            f"{indent}}}",
        ]
    if len(idx) == 2:
        a, b = idx
        return [
            f"{indent}if ((need.Prefixes[{a}] | need.Prefixes[{b}]) != PlayerFields.None)",
            f"{indent}{{",
            f"{inner}EventData.FillPlayer(",
            f"{inner}    d,",
            f"{inner}    {expr},",
            f"{inner}    need.PrefixKeys[{a}],",
            f"{inner}    need.Prefixes[{a}],",
            f"{inner}    need.PrefixKeys[{b}],",
            f"{inner}    need.Prefixes[{b}]);",
            f"{indent}}}",
        ]
    raise SystemExit(f"more than two prefixes for one controller in {owner}")


def emit_event_fill(event: dict, alias: dict) -> tuple[list[str], list[str], list[str]]:
    """Return (keys, prefixes, fill_lines) for a game event extractor."""
    owner = event["class_name"]
    indent = "            "
    bool_coerce = set(alias.get("bool_coerce", []))
    key_aliases: dict[str, list[str]] = alias.get("key_aliases", {})
    keys: list[str] = []
    prefixes: list[str] = []
    lines: list[str] = []
    props = event["properties"]
    for prop in props:
        if prop["kind"] == "player":
            continue
        field = prop["field"]
        names = [field, *key_aliases.get(field, [])]
        lines.extend(emit_key_group(names, csharp_expr_for_prop(prop, bool_coerce), keys, owner, indent))

    players = [p for p in props if p["kind"] == "player"]
    for prop in players:
        names = player_prefixes_for(prop["field"], players, alias)
        lines.extend(emit_player_fill(f"e.{prop['csharp']}", names, prefixes, owner, indent))
    return keys, prefixes, lines


def emit_targets(event: dict, alias: dict, types: list[str], audience: str) -> list[str]:
    players = player_props(event)
    extra = alias.get("types", {})
    index = {t: i for i, t in enumerate(types)}
    lines: list[str] = ["        {"]
    player_specific = {
        field: ts
        for field, ts in extra.items()
        if field != "_all" and ts
    }
    needs_event = bool(players) and (audience != "all" or bool(player_specific))
    if needs_event:
        lines.append(f"            var e = ({event['class_name']})gameEvent;")

    def add_player(csharp: str, t: str) -> None:
        lines.extend(
            [
                f"            if (need.Types[{index[t]}])",
                "            {",
                f'                buffer.Add((e.{csharp}, "{t}"));',
                "            }",
            ]
        )

    if audience == "all":
        broadcast = list(extra.get("_all", [])) if extra else list(types)
        if not broadcast and not player_specific:
            broadcast = list(types)
        if broadcast:
            cond = " || ".join(f"need.Types[{index[t]}]" for t in broadcast)
            lines.append(f"            if ({cond})")
            lines.append("            {")
            lines.append("                foreach (var entry in Utilities.GetPlayers())")
            lines.append("                {")
            if len(broadcast) == 1:
                lines.append(f'                    buffer.Add((entry, "{broadcast[0]}"));')
            else:
                for t in broadcast:
                    lines.extend(
                        [
                            f"                    if (need.Types[{index[t]}])",
                            "                    {",
                            f'                        buffer.Add((entry, "{t}"));',
                            "                    }",
                        ]
                    )
            lines.append("                }")
            lines.append("            }")
        for prop in players:
            for t in player_specific.get(prop["field"], []):
                add_player(prop["csharp"], t)
    elif extra:
        for prop in players:
            for t in extra.get(prop["field"], []):
                add_player(prop["csharp"], t)
    elif len(players) == 1:
        add_player(players[0]["csharp"], types[0])
    else:
        for prop, t in zip(players, types, strict=True):
            add_player(prop["csharp"], t)

    lines.append("        }")
    return lines


def generate_event_extractor(event: dict, alias: dict) -> str:
    cls = extractor_class_name(event["class_name"])
    types, audience = event_challenge_types(event, alias)
    derived = alias.get("derived")
    keys, prefixes, fill_lines = emit_event_fill(event, alias)
    derived_keys = [k for k, _ in DERIVED_KEYS.get(derived, [])] if derived else []
    if derived and not derived_keys:
        raise SystemExit(f"unknown derived kind {derived} in {event['class_name']}")

    needs_cast = bool(fill_lines)
    if derived:
        fill_lines.extend(
            [
                "            if (need.Derived)",
                "            {",
                f'                global::Challenges.Extractors.DerivedKeys.Apply("{derived}", gameEvent, d);',
                "            }",
            ]
        )

    needs_utilities = audience == "all"
    usings = [
        "using CounterStrikeSharp.API.Core;",
        "using CounterStrikeSharp.API.Modules.Events;",
        "using Challenges.Utils;",
    ]
    if needs_utilities:
        usings.insert(0, "using CounterStrikeSharp.API;")

    body_fill = "\n".join(
        [
            FILL_SIGNATURE,
            "        {",
            *([f"            var e = ({event['class_name']})gameEvent;"] if needs_cast else []),
            *fill_lines,
            "        }",
        ]
    )

    body_targets = "\n".join(
        [
            TARGETS_SIGNATURE,
            *emit_targets(event, alias, types, audience),
        ]
    )

    return "\n".join(
        [
            "// <auto-generated> by tools/generate_events.py - do not edit.",
            "#nullable enable",
            *usings,
            "",
            "namespace Challenges.Extractors",
            "{",
            f"    public sealed class {cls} : IExtractor",
            "    {",
            "        public ExtractorKind Kind => ExtractorKind.GameEvent;",
            f'        public string EventClassName => "{event["class_name"]}";',
            f"        public IReadOnlyList<string> ChallengeTypes {{ get; }} = {csharp_list(types)};",
            f"        public IReadOnlyList<string> Keys {{ get; }} = {csharp_list(keys)};",
            f"        public IReadOnlyList<string> PlayerPrefixes {{ get; }} = {csharp_list(prefixes)};",
            f"        public IReadOnlyList<string> DerivedKeys {{ get; }} = {csharp_list(derived_keys)};",
            "",
            body_fill,
            "",
            body_targets,
            "    }",
            "}",
            "",
        ]
    )


def generate_listener_extractor(listener: dict, alias: dict, snapshot: dict) -> str:
    name = listener["name"]
    types = listener_challenge_types(listener, alias)
    keys, prefixes, _, _, _ = emit_listener_fill(listener, alias, snapshot)
    cls = name  # OnMapStart etc.
    return "\n".join(
        [
            "// <auto-generated> by tools/generate_events.py - do not edit.",
            "#nullable enable",
            "using CounterStrikeSharp.API.Core;",
            "using CounterStrikeSharp.API.Modules.Events;",
            "using Challenges.Utils;",
            "",
            "namespace Challenges.Extractors",
            "{",
            f"    /// <summary>Key layout of the <c>{name}</c> listener; filled by <c>ChallengeEngine.{name}</c>.</summary>",
            f"    public sealed class Listener{cls} : IExtractor",
            "    {",
            "        public ExtractorKind Kind => ExtractorKind.Listener;",
            f'        public string EventClassName => "{name}";',
            f"        public IReadOnlyList<string> ChallengeTypes {{ get; }} = {csharp_list(types)};",
            f"        public IReadOnlyList<string> Keys {{ get; }} = {csharp_list(keys)};",
            f"        public IReadOnlyList<string> PlayerPrefixes {{ get; }} = {csharp_list(prefixes)};",
            "        public IReadOnlyList<string> DerivedKeys { get; } = [];",
            "",
            FILL_SIGNATURE,
            "        {",
            "        }",
            "",
            TARGETS_SIGNATURE,
            "        {",
            "        }",
            "    }",
            "}",
            "",
        ]
    )


def listener_player_params(listener: dict) -> list[dict]:
    return [p for p in listener["parameters"] if p["kind"] in {"player", "pawn", "slot"}]


def emit_listener_fill(
    listener: dict, alias: dict, snapshot: dict
) -> tuple[list[str], list[str], list[str], list[str], list[str]]:
    """Return (keys, prefixes, resolve_lines, target_exprs, fill_lines)."""
    owner = listener["name"]
    indent = "            "
    key_aliases = alias.get("key_aliases", {})
    player_prefixes = alias.get("player_prefixes", {})
    keys: list[str] = []
    prefixes: list[str] = []
    resolve: list[str] = []
    lines: list[str] = []
    target_exprs: list[str] = []

    def group(names: list[str], expr: str) -> None:
        lines.extend(emit_key_group(names, expr, keys, owner, indent))

    for param in listener["parameters"]:
        kind = param["kind"]
        name = param["name"]
        key = name.lower()
        if kind == "skip":
            continue
        if kind == "player":
            prefix = player_prefixes.get("player", "player")
            names = [prefix] if name == prefix else [prefix, name]
            lines.extend(emit_player_fill(name, names, prefixes, owner, indent))
            target_exprs.append(name)
        elif kind == "pawn":
            resolve.append(f"{indent}CCSPlayerController? {name}Controller = {name}.OriginalController?.Value;")
            lines.extend(emit_player_fill(f"{name}Controller", ["player"], prefixes, owner, indent))
            target_exprs.append(f"{name}Controller")
        elif kind == "slot":
            resolve.append(f"{indent}CCSPlayerController? {name}Player = Utilities.GetPlayerFromSlot({name});")
            lines.extend(emit_player_fill(f"{name}Player", ["player"], prefixes, owner, indent))
            target_exprs.append(f"{name}Player")
        elif kind == "string":
            group([key, *key_aliases.get(key, [])], name)
        elif kind == "bool":
            group([key, *key_aliases.get(key, [])], f"EventData.Bool({name})")
        elif kind in {"int", "float"}:
            group([key], f"EventData.Num({name})")
        elif kind == "steamid":
            group([key], f"{name}.SteamId64.ToString()")
        elif kind == "entity":
            group([f"{name}.designername"], f"{name}.DesignerName")
            group([f"{name}.index"], f"EventData.Num({name}.Index)")
        elif kind == "buttons":
            group([key], f"EventData.Num((ulong){name})")
        elif kind in {"damage_info", "damage_result"}:
            prefix, fields = (
                ("damage", snapshot["damage_info_fields"])
                if kind == "damage_info"
                else ("result", snapshot["damage_result_fields"])
            )
            for field in fields:
                fk = field["field"]
                cs = field["csharp"]
                k = field["kind"]
                if k == "bool":
                    expr = f"EventData.Bool({name}.{cs})"
                elif k in {"int", "float"}:
                    expr = f"EventData.Num({name}.{cs})"
                else:
                    expr = f"{name}.{cs}.ToString()"
                group([f"{prefix}.{fk}"], expr)
        elif kind == "tostring":
            group([key], f"{name}.ToString()")
        else:
            raise SystemExit(f"unsupported listener param kind {kind} in {listener['name']}")

    return keys, prefixes, resolve, target_exprs, lines


def generate_listener_method(listener: dict, alias: dict, snapshot: dict) -> str:
    name = listener["name"]
    types = listener_challenge_types(listener, alias)
    params = ", ".join(f"{p['type']} {p['name']}" for p in listener["parameters"])
    ret = listener["return"]
    exit_line = "                return HookResult.Continue;" if ret == "HookResult" else "                return;"
    _, _, resolve, target_exprs, fill_lines = emit_listener_fill(listener, alias, snapshot)

    preamble: list[str] = []
    if name == "OnPlayerButtonsChanged":
        preamble = [
            "            Situations.OnButtonsChanged(player, pressed, released);",
        ]

    begin = [
        f'            if (!BeginListener("{name}", out ExtractorPlan? plan, out GlobalGate closed))',
        "            {",
        exit_line,
        "            }",
        "",
        "            FillDemand need = plan.Demand;",
        *resolve,
        "            List<(CCSPlayerController? Player, string Type)> targets = ListenerTargets();",
    ]

    target_block: list[str] = []
    if target_exprs:
        for i, t in enumerate(types):
            target_block.extend(
                [
                    f"            if (need.Types[{i}])",
                    "            {",
                    f'                targets.Add(({target_exprs[0]}, "{t}"));',
                    "            }",
                ]
            )
    else:
        target_block.append("            foreach (var entry in Utilities.GetPlayers())")
        target_block.append("            {")
        if len(types) == 1:
            target_block.append(f'                targets.Add((entry, "{types[0]}"));')
        else:
            for i, t in enumerate(types):
                target_block.extend(
                    [
                        f"                if (need.Types[{i}])",
                        "                {",
                        f'                    targets.Add((entry, "{t}"));',
                        "                }",
                    ]
                )
        target_block.append("            }")

    lines = [
        f"        public {ret} {name}({params})",
        "        {",
        *preamble,
        *begin,
        *target_block,
        "",
        f'            if (FilterListenerTargets("{name}", targets, closed) is not {{ }} valid)',
        "            {",
        exit_line,
        "            }",
        "",
        "            Dictionary<string, string> d = new(need.Capacity);",
        *fill_lines,
        f'            HandleListener("{name}", d, valid, closed);',
    ]
    if ret == "HookResult":
        lines.append("            return HookResult.Continue;")
    lines.append("        }")
    return "\n".join(lines)


def write_generated_extractors(snapshot: dict, aliases: dict) -> list[str]:
    parts: list[str] = [
        "// <auto-generated> by tools/generate_events.py - do not edit.",
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
        source = generate_listener_extractor(listener, alias, snapshot)
        (gen_dir / f"Listener{listener['name']}.g.cs").write_text(source, encoding="utf-8")
        registry_names.append(f"Listener{listener['name']}")

    return registry_names


def write_registry(names: list[str]) -> None:
    lines = [
        "// <auto-generated> by tools/generate_events.py - do not edit.",
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
        "// <auto-generated> by tools/generate_events.py - do not edit.",
        "#nullable enable",
        "using CounterStrikeSharp.API.Core;",
        "using CounterStrikeSharp.API.Modules.Events;",
        "",
        "namespace Challenges",
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
        "// <auto-generated> by tools/generate_events.py - do not edit.",
        "#nullable enable",
        "using CounterStrikeSharp.API;",
        "using CounterStrikeSharp.API.Core;",
        "using CounterStrikeSharp.API.Modules.Entities;",
        "using CounterStrikeSharp.API.Modules.Utils;",
        "using Challenges.Utils;",
        "",
        "namespace Challenges",
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
    """Return [{suffix, type}, ...] from the PlayerKeys.Suffixes table (runtime source of truth)."""
    found: list[dict] = []
    seen: set[str] = set()
    for suffix, _field, typ in RE_SUFFIX.findall(FILL_DEMAND.read_text(encoding="utf-8")):
        if suffix in seen:
            raise SystemExit(f"duplicate player property suffix {suffix} in {FILL_DEMAND}")
        seen.add(suffix)
        found.append({"suffix": suffix, "type": typ})
    if not found:
        raise SystemExit(f"no player property suffixes in {FILL_DEMAND}")
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

    for key, typ in DERIVED_KEYS.get(alias.get("derived") or "", []):
        add(key, typ)

    situation = alias.get("situation")
    if situation == "grenade_on_death":
        for prefix in ("attacker", "victim", "userid"):
            add(f"{prefix}.grenade", "string")
            add(f"{prefix}.grenade.holding", "string")
            add(f"{prefix}.grenade.thrown", "string")
            add(f"{prefix}.grenade.throwing", "string")
        add("isace", "bool")
    elif situation == "smoke_defuse":
        add("insmoke", "bool")
        add("smoke.distance", "float")
        add("smoke.age", "float")
        for prefix in player_prefixes_for(
            players[0]["field"], players, alias
        ) if players else ["player"]:
            add(f"{prefix}.insmoke", "bool")

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


COMMON_CHALLENGE_TYPES = (
    "player_kill",
    "player_trade",
    "player_hurt_attacker",
    "player_bomb_planted",
    "player_bomb_begindefuse",
    "player_bomb_defused",
    "weapon_fire",
    "round_start",
    "on_player_chat",
    "player_chat",
)

# (group_title, matchers). First match wins. Listeners always go to Listeners.
# Each matcher is a substring of the challenge type (or event name fallback).
# More specific groups first so player_bomb_* lands under Bomb, not Players.
EVENT_DOC_GROUPS: list[tuple[str, tuple[str, ...]]] = [
    ("Bomb", ("bomb_", "defuser_", "enter_bomb", "exit_bomb")),
    ("Weapons", ("weapon_", "item_", "inspect_", "silencer_", "ammo_", "buymenu_", "buytime_", "cart_", "inventory_")),
    ("Grenades", ("grenade_", "flashbang_", "hegrenade_", "molotov_", "decoy_", "smoke", "inferno_", "tagrenade_")),
    ("Hostage", ("hostage_", "enter_rescue", "exit_rescue")),
    ("Vote", ("vote_", "start_vote", "enable_restart_voting", "player_reset_vote")),
    ("Round", ("round_", "warmup_", "announce_phase", "begin_new_match", "cs_win_", "cs_intermission", "cs_match_", "cs_pre_restart", "cs_round_", "game_phase", "game_end", "game_start", "match_end", "start_halftime", "teamplay_round")),
    ("Players", ("player_", "bot_", "vip_", "gg_killed", "other_death", "entity_killed")),
    ("Server", ("server_", "map_", "hostname_", "nextlevel_", "game_newmap", "game_init", "game_message", "demo_", "nav_", "difficulty_", "ugc_", "gc_connected", "user_data_", "store_", "seasoncoin_", "tournament_", "trial_", "achievement_", "instructor_", "gameinstructor_", "gameui_", "sfuievent", "spec_", "switch_team", "team_", "teamchange_", "local_player_", "client_", "clientside_")),
]

COMMON_TYPE_BLURBS = {
    "player_kill": "Player got a kill",
    "player_hurt_attacker": "Player dealt damage",
    "player_bomb_planted": "Player planted the bomb",
    "player_bomb_begindefuse": "Player started defusing",
    "player_bomb_defused": "Player finished a defuse",
    "player_trade": "Mutual kill within a short window",
    "weapon_fire": "Player fired a weapon",
    "round_start": "Round started",
    "on_player_chat": "Player sent a chat message (listener)",
    "player_chat": "Player sent a chat message",
}

# Synthetic challenge types (no CSS event). Merged into docs + catalog after generation.
SYNTHETIC_EVENTS = [
    {
        "doc_name": "PlayerTrade",
        "title": "PlayerTrade (player_trade)",
        "source": "Synthetic task type from situation memory (two `player_death` events).",
        "audience": "players",
        "types": ["player_trade"],
        "prefixes": ["attacker", "victim", "userid"],
        "keys": [
            {"key": "seconds", "type": "float"},
            {"key": "weapon", "type": "string"},
            {"key": "other_weapon", "type": "string"},
            {"key": "same_weapon", "type": "bool"},
            {"key": "headshot", "type": "bool"},
            {"key": "other_headshot", "type": "bool"},
            {"key": "isteamkill", "type": "bool"},
            {"key": "isselfkill", "type": "bool"},
        ],
        "catalog_event": {
            "type": "player_trade",
            "event_class": "PlayerTrade",
            "extractor": "SituationMemory",
            "kind": "synthetic",
        },
    },
]


def format_key_docs(keys: list[dict], prefixes: list[str], suffixes: list[dict] | None = None) -> list[str]:
    lines: list[str] = [
        "| Key | Type |",
        "|-----|------|",
        "| [global event data](../../rules/GlobalEventData.md) | see page |",
    ]
    if prefixes:
        joined = ", ".join(f"`{p}`" for p in prefixes)
        lines.append(
            f"| [player data](../../rules/GlobalPlayerData.md) | prefixes {joined} |"
        )
    standard = {s["suffix"] for s in (suffixes or [])}
    for entry in keys:
        key = entry["key"]
        skip = False
        for p in prefixes:
            prefix = f"{p}."
            if key.startswith(prefix) and key[len(prefix) :] in standard:
                skip = True
                break
        if skip:
            continue
        lines.append(f"| `{key}` | {entry['type']} |")
    return lines


def audience_line(audience: str) -> str:
    if audience == "all":
        return "Who gets credit: **all connected players**."
    return "Who gets credit: the **event player(s)** listed under task types."


def write_event_page(
    path: Path,
    title: str,
    source_line: str,
    audience: str,
    types: list[str],
    keys: list[dict],
    prefixes: list[str],
    suffixes: list[dict] | None = None,
) -> None:
    first_type = types[0] if types else "example"
    lines = [
        f"# {title}",
        "",
        source_line,
        "",
        audience_line(audience),
        "",
        "## Task types",
        "",
        "Put one of these in your task `type:` field:",
        "",
        *[f"- `{t}`" for t in types],
        "",
        "## Minimal YAML",
        "",
        "Save under `blueprints/`. Add rules from the table below — see [Rules](../rules.md).",
        "",
        "```yaml",
        "tasks:",
        "  - id: example",
        f"    type: {first_type}",
        "    amount: 1",
        "    rules: []",
        "```",
        "",
        "## Attributes (rule keys)",
        "",
        "Unknown keys never match — see [Rules](../rules.md).",
        "",
        *format_key_docs(keys, prefixes, suffixes),
        "",
    ]
    path.write_text("\n".join(lines), encoding="utf-8")


def classify_event_group(label: str, *, is_listener: bool) -> str:
    if is_listener or label.startswith("on_"):
        return "Listeners"
    for group, matchers in EVENT_DOC_GROUPS:
        for matcher in matchers:
            if label.startswith(matcher) or matcher in label:
                return group
    return "Other"


def render_index_table(rows: list[tuple[str, str]]) -> list[str]:
    lines = [
        "| Task type | Page |",
        "|-----------|------|",
    ]
    for label, href in rows:
        page = Path(href).stem
        lines.append(f"| `{label}` | [{page}]({href}) |")
    return lines


def write_grouped_index(
    path: Path,
    title: str,
    intro: str,
    source_note: str,
    index_rows: list[tuple[str, str, str]],
    group_order: list[str],
    *,
    common_rows: list[tuple[str, str]] | None = None,
) -> None:
    by_group: dict[str, list[tuple[str, str]]] = {g: [] for g in group_order}
    for label, href, group in index_rows:
        by_group.setdefault(group, []).append((label, href))

    out: list[str] = [
        f"# {title}",
        "",
        intro,
        "",
        source_note,
        "",
    ]

    if common_rows:
        out.extend(
            [
                "## Common types",
                "",
                "| Task type | Typical use |",
                "|-----------|-------------|",
                *[f"| `{t}` | {blurb} |" for t, blurb in common_rows],
                "",
            ]
        )

    out.extend(["## All types", ""])
    for group in group_order:
        rows = by_group.get(group) or []
        if not rows:
            continue
        out.append(f'<details markdown="1"><summary><b>{group}</b> ({len(rows)})</summary>')
        out.append("")
        out.extend(render_index_table(rows))
        out.append("")
        out.append("</details>")
        out.append("")

    path.write_text("\n".join(out), encoding="utf-8")


def write_docs(snapshot: dict, aliases: dict, suffixes: list[dict]) -> None:
    if DOCS_EVENTS.exists():
        shutil.rmtree(DOCS_EVENTS)
    DOCS_EVENTS.mkdir(parents=True)

    # (label, href, group) - one row per task type that points at its page
    event_rows: list[tuple[str, str, str]] = []
    listener_rows: list[tuple[str, str, str]] = []
    emitted_types: set[str] = set()

    for event in snapshot["events"]:
        alias = alias_for(aliases, event["class_name"])
        types, audience = event_challenge_types(event, alias)
        keys = catalog_keys_for_event(event, alias, suffixes)
        players = player_props(event)
        prefixes = ordered_unique(
            [p for prop in players for p in player_prefixes_for(prop["field"], players, alias)]
        )
        doc_name = event["class_name"]
        write_event_page(
            DOCS_EVENTS / f"{doc_name}.md",
            f"{doc_name} ({' / '.join(types)})",
            f"CSS game event `{event['event_name']}` (`{event['class_name']}`).",
            audience,
            types,
            keys,
            prefixes,
            suffixes,
        )
        href = f"events/{doc_name}.md"
        for t in types:
            emitted_types.add(t)
            event_rows.append((t, href, classify_event_group(t, is_listener=False)))

    for synth in SYNTHETIC_EVENTS:
        synth_keys = list(synth["keys"])
        for prefix in synth["prefixes"]:
            for suffix in suffixes:
                synth_keys.append(
                    {"key": f"{prefix}.{suffix['suffix']}", "type": suffix["type"]}
                )
        write_event_page(
            DOCS_EVENTS / f"{synth['doc_name']}.md",
            synth["title"],
            synth["source"],
            synth["audience"],
            synth["types"],
            synth_keys,
            synth["prefixes"],
            suffixes,
        )
        href = f"events/{synth['doc_name']}.md"
        for t in synth["types"]:
            if t in emitted_types:
                continue
            emitted_types.add(t)
            event_rows.append((t, href, classify_event_group(t, is_listener=False)))

    for listener in snapshot["listeners"]:
        alias = alias_for(aliases, listener["name"])
        if not alias and listener.get("listener_name"):
            alias = alias_for(aliases, listener["listener_name"])
        types = listener_challenge_types(listener, alias)
        keys = catalog_keys_for_listener(listener, alias, suffixes, snapshot)
        has_player = any(p["kind"] in {"player", "pawn", "slot"} for p in listener["parameters"])
        prefixes = ["player"] if has_player else []
        doc_name = listener["name"]
        write_event_page(
            DOCS_EVENTS / f"{doc_name}.md",
            f"{doc_name} ({' / '.join(types)})",
            f"CSS listener `{listener.get('listener_name', listener['name'])}`.",
            "players",
            types,
            keys,
            prefixes,
            suffixes,
        )
        href = f"events/{doc_name}.md"
        for t in types:
            emitted_types.add(t)
            listener_rows.append((t, href, "Listeners"))

    event_rows.sort(key=lambda x: (x[0], x[1]))
    listener_rows.sort(key=lambda x: (x[0], x[1]))

    event_types = {label for label, _, _ in event_rows}
    common_rows = [
        (t, COMMON_TYPE_BLURBS[t])
        for t in COMMON_CHALLENGE_TYPES
        if t in event_types and t in COMMON_TYPE_BLURBS
    ]
    listener_common = [
        (t, COMMON_TYPE_BLURBS[t])
        for t in COMMON_CHALLENGE_TYPES
        if t in {label for label, _, _ in listener_rows} and t in COMMON_TYPE_BLURBS
    ]
    ref = snapshot.get("ref", "main")
    commit = snapshot.get("commit") or ""
    commit_note = f" (synced tip `{commit[:12]}`)" if commit else ""
    source_note = f"Source: CounterStrikeSharp `{ref}`{commit_note}."

    event_group_order = [
        "Players",
        "Round",
        "Bomb",
        "Weapons",
        "Grenades",
        "Hostage",
        "Vote",
        "Server",
        "Other",
    ]
    write_grouped_index(
        DOCS_EVENTS_INDEX,
        "Events",
        "Use a **task type** as the task `type:` in your challenge file. "
        "Open an event page for the attributes (rule keys) you can check. "
        "For CounterStrikeSharp listeners, see [Listeners](listeners.md).",
        source_note,
        event_rows,
        event_group_order,
        common_rows=common_rows,
    )
    write_grouped_index(
        DOCS_LISTENERS_INDEX,
        "Listeners",
        "Use a **task type** as the task `type:` in your challenge file. "
        "Listeners are CounterStrikeSharp hooks (not game events). "
        "Open a listener page for the attributes (rule keys) you can check. "
        "For game events, see [Events](events.md).",
        source_note,
        listener_rows,
        ["Listeners"],
        common_rows=listener_common or None,
    )


def write_catalog(snapshot: dict, aliases: dict, suffixes: list[dict]) -> None:
    operators = RE_CASE.findall(RULES.read_text(encoding="utf-8"))
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

    for synth in SYNTHETIC_EVENTS:
        synth_keys = list(synth["keys"])
        for prefix in synth["prefixes"]:
            for suffix in suffixes:
                synth_keys.append(
                    {"key": f"{prefix}.{suffix['suffix']}", "type": suffix["type"]}
                )
        key_sets[synth["doc_name"]] = synth_keys
        events_out.append(dict(synth["catalog_event"]))

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

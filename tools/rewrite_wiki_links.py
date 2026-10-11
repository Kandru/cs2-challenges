#!/usr/bin/env python3
"""Drop .md from internal links so GitHub wiki opens pages, not raw files.

A target such as settings.md is published as
https://raw.githubusercontent.com/wiki/<owner>/<repo>/settings.md.
The same link without the extension opens the rendered wiki page.
External URLs, anchors, and fenced or inline code are left unchanged.
"""
from __future__ import annotations

import re
import sys
from pathlib import Path

LINK_RE = re.compile(r"(?<!!)\[([^\]]*)\]\(([^)\s]+)\)")


def _rewrite_target(target: str) -> str:
    if "://" in target or target.startswith(("mailto:", "#")):
        return target
    for sep in ("#", "?"):
        if sep in target:
            path, rest = target.split(sep, 1)
            if path.endswith(".md"):
                return f"{path[:-3]}{sep}{rest}"
            return target
    if target.endswith(".md"):
        return target[:-3]
    return target


def _rewrite_plain(text: str) -> str:
    def repl(match: re.Match[str]) -> str:
        label, target = match.group(1), match.group(2)
        rewritten = _rewrite_target(target)
        if rewritten == target:
            return match.group(0)
        return f"[{label}]({rewritten})"

    return LINK_RE.sub(repl, text)


def _rewrite_line(line: str) -> str:
    parts: list[str] = []
    index = 0
    while index < len(line):
        tick = line.find("`", index)
        if tick == -1:
            parts.append(_rewrite_plain(line[index:]))
            break
        parts.append(_rewrite_plain(line[index:tick]))
        close = line.find("`", tick + 1)
        if close == -1:
            parts.append(line[tick:])
            break
        parts.append(line[tick : close + 1])
        index = close + 1
    return "".join(parts)


def rewrite_markdown(text: str) -> str:
    lines = text.splitlines(keepends=True)
    out: list[str] = []
    in_fence = False
    for line in lines:
        if line.lstrip().startswith("```"):
            in_fence = not in_fence
            out.append(line)
            continue
        out.append(line if in_fence else _rewrite_line(line))
    return "".join(out)


def rewrite_tree(root: Path) -> int:
    changed = 0
    for path in sorted(root.rglob("*.md")):
        original = path.read_text(encoding="utf-8")
        rewritten = rewrite_markdown(original)
        if rewritten != original:
            path.write_text(rewritten, encoding="utf-8")
            changed += 1
    return changed


def main(argv: list[str]) -> int:
    if len(argv) != 2:
        print(f"usage: {Path(argv[0]).name} WIKI_DIR", file=sys.stderr)
        return 2
    root = Path(argv[1])
    if not root.is_dir():
        print(f"not a directory: {root}", file=sys.stderr)
        return 2
    changed = rewrite_tree(root)
    print(f"Rewrote internal .md links in {changed} file(s).")
    return 0


if __name__ == "__main__":
    raise SystemExit(main(sys.argv))

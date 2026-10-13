#!/usr/bin/env python3
"""Assert every builder help URL resolves in the assembled _site tree."""

from __future__ import annotations

import json
import re
import sys
from pathlib import Path
from urllib.parse import unquote, urlparse

ROOT = Path(__file__).resolve().parents[1]
BUILDER = ROOT / "builder"

# Docs-relative paths used by the builder (app.js helpers + index.html).
PATH_RE = re.compile(r'''["']((?:reference|howto|tutorial)/[^"']*)["']''')
HTML_HREF_RE = re.compile(r'''href=["']\.\./docs/([^"']+)["']''')


def collect_paths() -> list[str]:
    app = (BUILDER / "app.js").read_text(encoding="utf-8")
    html = (BUILDER / "index.html").read_text(encoding="utf-8")
    paths = {m.group(1) for m in PATH_RE.finditer(app)}
    paths.update(m.group(1) for m in HTML_HREF_RE.finditer(html))
    return sorted(p for p in paths if "${" not in p)


def resolve(site: Path, docs_rel: str) -> Path | None:
    path = unquote(urlparse(docs_rel).path).strip("/")
    base = site / "docs"
    for candidate in (base / path / "index.html", base / f"{path}.html", base / path):
        if candidate.is_file():
            return candidate
    return None


def has_anchor(html: str, fragment: str) -> bool:
    if not fragment:
        return True
    frag = re.escape(unquote(fragment))
    return bool(re.search(rf'(?:id|name)=["\']{frag}["\']', html))


def check(site: Path, docs_rel: str) -> str | None:
    target = resolve(site, docs_rel)
    if target is None:
        return f"MISSING page: {docs_rel}"
    fragment = urlparse(docs_rel).fragment
    if fragment and not has_anchor(target.read_text(encoding="utf-8", errors="replace"), fragment):
        return f"MISSING anchor: {docs_rel}"
    return None


def main() -> int:
    site = Path(sys.argv[1] if len(sys.argv) > 1 else ROOT / "_site").resolve()
    if not site.is_dir():
        print(f"error: site root not found: {site}", file=sys.stderr)
        return 1

    to_check = collect_paths()
    if not to_check:
        print("error: no builder docs paths found", file=sys.stderr)
        return 1

    catalog = site / "builder" / "catalog.json"
    if catalog.is_file():
        for action in json.loads(catalog.read_text(encoding="utf-8")).get("action_types", []):
            to_check.append(f"reference/actions/#{action.replace('.', '-')}")
    to_check.append("reference/events/EventPlayerDeath/")

    errors = [err for docs_rel in to_check if (err := check(site, docs_rel))]
    for err in errors:
        print(err, file=sys.stderr)
    if errors:
        print(f"{len(errors)} broken builder docs link(s)", file=sys.stderr)
        return 1
    print(f"{len(to_check)} builder docs links OK")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())

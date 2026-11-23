#!/usr/bin/env python3
"""Render every Mermaid source under src/ to an SVG in this directory using Kroki.

Usage:
    python3 docs/assets/permission/render.py            # render all
    python3 docs/assets/permission/render.py foo.mmd    # render one

Requires network access to https://kroki.io. The request always carries
?html-labels=false so the output uses <text>/<tspan> instead of <foreignObject>
(GitHub does not render the text inside foreignObject).
"""

from __future__ import annotations

import base64
import pathlib
import sys
import urllib.request
import zlib

HERE = pathlib.Path(__file__).resolve().parent
SRC = HERE / "src"
KROKI = "https://kroki.io/mermaid/svg/{data}?html-labels=false"


def encode(source: str) -> str:
    return base64.urlsafe_b64encode(zlib.compress(source.encode("utf-8"), 9)).decode("ascii")


def render(mmd: pathlib.Path) -> pathlib.Path:
    source = mmd.read_text(encoding="utf-8")
    url = KROKI.format(data=encode(source))
    request = urllib.request.Request(url, headers={"User-Agent": "euonia-docs-render/1.0"})
    with urllib.request.urlopen(request, timeout=60) as response:
        svg = response.read().decode("utf-8")

    if "<svg" not in svg:
        raise SystemExit(f"{mmd.name}: Kroki did not return an SVG:\n{svg[:400]}")
    if "<foreignObject" in svg:
        raise SystemExit(f"{mmd.name}: SVG contains <foreignObject>; html-labels=false was not honoured")
    if "<text" not in svg:
        raise SystemExit(f"{mmd.name}: SVG contains no <text> element; labels would not render")

    # Mermaid leaves the canvas transparent and draws text in dark grey, which is unreadable on
    # GitHub's dark theme. Paint an explicit white card behind the diagram (the SVG scales it).
    if 'class="permission-backdrop"' not in svg:
        end = svg.find(">", svg.find("<svg")) + 1
        svg = svg[:end] + '<rect class="permission-backdrop" x="0" y="0" width="100%" height="100%" fill="#ffffff"/>' + svg[end:]

    target = HERE / (mmd.stem + ".svg")
    target.write_text(svg, encoding="utf-8")
    return target


def main(argv: list[str]) -> int:
    if not SRC.is_dir():
        raise SystemExit(f"missing source directory: {SRC}")

    if argv:
        sources = []
        for name in argv:
            candidate = SRC / name
            if not candidate.is_file():
                candidate = SRC / (name + ".mmd")
            if not candidate.is_file():
                raise SystemExit(f"not found: {name}")
            sources.append(candidate)
    else:
        sources = sorted(SRC.glob("*.mmd"))

    if not sources:
        raise SystemExit("no .mmd sources found")

    for mmd in sources:
        target = render(mmd)
        size = target.stat().st_size
        print(f"{mmd.name} -> {target.name} ({size} bytes)")

    print(f"rendered {len(sources)} diagram(s)")
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))

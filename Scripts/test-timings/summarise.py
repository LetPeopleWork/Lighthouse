"""Print the slowest tests across backend (TRX) and frontend (Vitest JSON) results.

Used locally by developers to answer 'what's slow right now?' against a freshly
populated TestResults/ directory.
"""

from __future__ import annotations

import argparse
import json
import sys
from pathlib import Path

from trx_to_csv import (
    discover_integration_class_names,
    discover_integration_method_fqns,
    parse_trx,
)
from vitest_to_csv import parse_vitest

_VITEST_REQUIRED_KEYS = {"testResults", "numTotalTests"}
_TABLE_COLUMNS = (
    ("stack", 9),
    ("duration_ms", 13),
    ("outcome", 8),
    ("category_or_file", 50),
    ("name", 80),
)


def _iter_candidate_files(paths: list[Path]) -> tuple[list[Path], list[Path]]:
    trx: list[Path] = []
    json_files: list[Path] = []
    for path in paths:
        if path.is_file():
            (trx if path.suffix == ".trx" else json_files).append(path)
            continue
        if path.is_dir():
            trx.extend(sorted(path.rglob("*.trx")))
            json_files.extend(sorted(path.rglob("*.json")))
    return trx, json_files


def _is_vitest_report(path: Path) -> bool:
    try:
        payload = json.loads(path.read_text(encoding="utf-8"))
    except (OSError, UnicodeDecodeError, json.JSONDecodeError):
        return False
    return isinstance(payload, dict) and _VITEST_REQUIRED_KEYS.issubset(payload)


def gather(
    paths: list[Path], source_root: Path | None = None
) -> list[dict[str, object]]:
    """Walk paths and produce normalised rows tagged with their stack."""
    trx_files, json_files = _iter_candidate_files(paths)
    integration_classes: set[str] = set()
    integration_methods: set[str] = set()
    if source_root is not None:
        integration_classes = discover_integration_class_names(source_root)
        integration_methods = discover_integration_method_fqns(source_root)

    rows: list[dict[str, object]] = []
    for trx_path in trx_files:
        for parsed in parse_trx(
            trx_path.read_text(encoding="utf-8-sig"),
            integration_class_names=integration_classes,
            integration_method_fqns=integration_methods,
        ):
            rows.append(
                {
                    "stack": "Backend",
                    "name": parsed["fully_qualified_name"],
                    "category_or_file": parsed["category"],
                    "duration_ms": parsed["duration_ms"],
                    "outcome": parsed["outcome"],
                }
            )

    for json_path in json_files:
        if not _is_vitest_report(json_path):
            continue
        for parsed in parse_vitest(json_path.read_text(encoding="utf-8")):
            rows.append(
                {
                    "stack": "Frontend",
                    "name": parsed["test_name"],
                    "category_or_file": parsed["file"],
                    "duration_ms": parsed["duration_ms"],
                    "outcome": parsed["outcome"],
                }
            )
    return rows


def _format_cell(value: object, width: int) -> str:
    text = str(value)
    if len(text) <= width:
        return text.ljust(width)
    return text[: max(width - 1, 1)] + "…"


def render_top_n(rows: list[dict[str, object]], n: int = 20) -> str:
    if not rows:
        return "No timing data found. Pass one or more TestResults/ directories or files."

    header = " ".join(_format_cell(name, width) for name, width in _TABLE_COLUMNS)
    rule = "-" * len(header)

    ordered = sorted(rows, key=lambda r: r["duration_ms"], reverse=True)[:n]
    body_lines = []
    for row in ordered:
        duration = f"{float(row['duration_ms']):.1f}ms"
        cells = [
            _format_cell(row["stack"], _TABLE_COLUMNS[0][1]),
            _format_cell(duration, _TABLE_COLUMNS[1][1]),
            _format_cell(row["outcome"], _TABLE_COLUMNS[2][1]),
            _format_cell(row["category_or_file"], _TABLE_COLUMNS[3][1]),
            _format_cell(row["name"], _TABLE_COLUMNS[4][1]),
        ]
        body_lines.append(" ".join(cells))

    summary = _render_summary(rows)
    return "\n".join([header, rule, *body_lines, "", summary])


def _render_summary(rows: list[dict[str, object]]) -> str:
    backend = [row for row in rows if row["stack"] == "Backend"]
    frontend = [row for row in rows if row["stack"] == "Frontend"]
    backend_total_ms = sum(float(r["duration_ms"]) for r in backend)
    frontend_total_ms = sum(float(r["duration_ms"]) for r in frontend)
    integration = [
        row for row in backend if row["category_or_file"] == "Integration"
    ]
    integration_ms = sum(float(r["duration_ms"]) for r in integration)

    lines = [
        f"Backend  : {len(backend):>5} tests, {backend_total_ms / 1000:>8.2f}s wall-clock "
        f"({len(integration)} integration, {integration_ms / 1000:.2f}s)",
        f"Frontend : {len(frontend):>5} tests, {frontend_total_ms / 1000:>8.2f}s wall-clock",
    ]
    return "\n".join(lines)


def _build_arg_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(
        description="Print the slowest tests across backend (TRX) and frontend (Vitest JSON) results."
    )
    parser.add_argument(
        "paths",
        nargs="+",
        type=Path,
        help="One or more TestResults/ directories or individual .trx / Vitest-JSON files.",
    )
    parser.add_argument(
        "--source-root",
        type=Path,
        default=None,
        help="C# source tree root to classify Integration tests (e.g. Lighthouse.Backend/Lighthouse.Backend.Tests).",
    )
    parser.add_argument(
        "--top",
        type=int,
        default=20,
        help="Number of slowest tests to print (default 20).",
    )
    return parser


def main(argv: list[str] | None = None) -> int:
    args = _build_arg_parser().parse_args(argv)
    rows = gather(args.paths, source_root=args.source_root)
    print(render_top_n(rows, n=args.top))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())

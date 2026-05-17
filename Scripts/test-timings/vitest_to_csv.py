"""Extract per-test timings from a Vitest JSON reporter file into CSV rows."""

from __future__ import annotations

import argparse
import csv
import json
import sys
from pathlib import Path
from typing import Iterable, TextIO

CSV_COLUMNS = ("file", "test_name", "duration_ms", "outcome")
_OUTCOME_MAP = {
    "passed": "Passed",
    "failed": "Failed",
    "skipped": "Skipped",
    "pending": "Skipped",
    "todo": "Skipped",
}


def _normalise_file(path: str, source_root: str | None) -> str:
    if source_root is None:
        return path
    root = str(Path(source_root).resolve())
    resolved = str(Path(path).resolve())
    if resolved.startswith(root + "/"):
        return resolved[len(root) + 1 :]
    return path


def parse_vitest(
    json_content: str, source_root: str | None = None
) -> list[dict[str, object]]:
    """Parse a Vitest JSON reporter document into row dicts."""
    payload = json.loads(json_content)
    rows: list[dict[str, object]] = []
    for file_result in payload.get("testResults", []):
        file_path = _normalise_file(file_result.get("name", ""), source_root)
        for assertion in file_result.get("assertionResults", []):
            duration = assertion.get("duration")
            rows.append(
                {
                    "file": file_path,
                    "test_name": assertion.get("fullName")
                    or assertion.get("title", ""),
                    "duration_ms": float(duration) if duration else 0.0,
                    "outcome": _OUTCOME_MAP.get(
                        assertion.get("status", "").lower(), "Unknown"
                    ),
                }
            )
    return rows


def rows_to_csv(rows: Iterable[dict[str, object]], stream: TextIO) -> None:
    ordered = sorted(rows, key=lambda r: r["duration_ms"], reverse=True)
    writer = csv.writer(stream, lineterminator="\n")
    writer.writerow(CSV_COLUMNS)
    for row in ordered:
        writer.writerow(
            [
                row["file"],
                row["test_name"],
                f"{float(row['duration_ms']):.3f}",
                row["outcome"],
            ]
        )


def _build_arg_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(
        description="Extract per-test timings from a Vitest JSON reporter file into a CSV."
    )
    parser.add_argument(
        "--input",
        type=Path,
        required=True,
        help="Vitest JSON reporter file (--reporter=json --outputFile.json=...).",
    )
    parser.add_argument(
        "--source-root",
        type=str,
        default=None,
        help="Absolute path to make file paths relative against (e.g. Lighthouse.Frontend).",
    )
    parser.add_argument(
        "--output",
        type=Path,
        required=True,
        help="Destination CSV file.",
    )
    return parser


def main(argv: list[str] | None = None) -> int:
    args = _build_arg_parser().parse_args(argv)
    if not args.input.exists():
        print(f"error: Vitest JSON not found at {args.input}", file=sys.stderr)
        return 1
    rows = parse_vitest(
        args.input.read_text(encoding="utf-8"), source_root=args.source_root
    )
    args.output.parent.mkdir(parents=True, exist_ok=True)
    with args.output.open("w", encoding="utf-8", newline="") as stream:
        rows_to_csv(rows, stream)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())

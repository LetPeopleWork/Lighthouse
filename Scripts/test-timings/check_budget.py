"""Fail the verify job when a test file (frontend) or class (backend) runs over its time budget."""

from __future__ import annotations

import argparse
import csv
import json
from dataclasses import dataclass
from decimal import Decimal
from pathlib import Path

import trx_to_csv
import vitest_to_csv

HERE = Path(__file__).resolve().parent
REPOSITORY_ROOT = HERE.parent.parent
DEFAULT_ALLOWLIST = HERE / "test-time-budget.json"
DEFAULT_BACKEND_SOURCE_ROOT = REPOSITORY_ROOT / "Lighthouse.Backend" / "Lighthouse.Backend.Tests"


def _build_arg_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(
        description="Check a test-timings CSV against the per-file / per-class time budget."
    )
    parser.add_argument(
        "--stack",
        choices=("frontend", "backend"),
        required=True,
        help="Which suite the CSV came from: frontend sums per file, backend per class.",
    )
    parser.add_argument(
        "--csv",
        type=Path,
        required=True,
        help="test-timings-frontend.csv or test-timings-backend.csv.",
    )
    parser.add_argument(
        "--allowlist",
        type=Path,
        default=DEFAULT_ALLOWLIST,
        help="Budget and allowlist file (default: test-time-budget.json beside this script).",
    )
    parser.add_argument(
        "--source-root",
        type=Path,
        default=DEFAULT_BACKEND_SOURCE_ROOT,
        help="Backend test source tree an allowlisted class must still be declared in.",
    )
    parser.add_argument(
        "--warn-only",
        action="store_true",
        help="Report a unit over its budget or ceiling as a warning and pass. "
        "A missing or malformed input still fails.",
    )
    return parser


UNIT_COLUMN = {
    "frontend": vitest_to_csv.CSV_COLUMNS[0],
    "backend": trx_to_csv.CSV_COLUMNS[0],
}
DURATION_COLUMN = {
    "frontend": vitest_to_csv.CSV_COLUMNS[2],
    "backend": trx_to_csv.CSV_COLUMNS[2],
}


@dataclass(frozen=True)
class Finding:
    unit: str
    seconds: Decimal
    limit: Decimal


def class_of(fully_qualified_name: str) -> str:
    # Test arguments may themselves hold dots and brackets, so drop them before looking for the method.
    without_arguments = fully_qualified_name.split("(", 1)[0]
    return without_arguments.rsplit(".", 1)[0]


def unit_of(stack: str, name: str) -> str:
    return class_of(name) if stack == "backend" else name


def seconds_per_unit(stack: str, rows: list[dict[str, str]]) -> dict[str, Decimal]:
    totals: dict[str, Decimal] = {}
    for row in rows:
        unit = unit_of(stack, row[UNIT_COLUMN[stack]])
        totals[unit] = totals.get(unit, Decimal(0)) + Decimal(row[DURATION_COLUMN[stack]]) / 1000
    return totals


def judge(stack: str, rows: list[dict[str, str]], budget_seconds: Decimal) -> list[Finding]:
    return [
        Finding(unit, seconds, budget_seconds)
        for unit, seconds in sorted(seconds_per_unit(stack, rows).items())
        if seconds > budget_seconds
    ]


def annotation(finding: Finding, allowlist_name: str) -> str:
    return (
        f"::error::{finding.unit} took {finding.seconds:.1f}s, "
        f"over the {finding.limit}s budget set in {allowlist_name}"
    )


def main(argv: list[str] | None = None) -> int:
    arguments = _build_arg_parser().parse_args(argv)
    with arguments.csv.open(encoding="utf-8", newline="") as stream:
        rows = list(csv.DictReader(stream))
    document = json.loads(arguments.allowlist.read_text(encoding="utf-8"))
    budget_seconds = Decimal(str(document[arguments.stack]["budget_seconds"]))

    findings = judge(arguments.stack, rows, budget_seconds)
    for finding in findings:
        print(annotation(finding, arguments.allowlist.name))
    return 1 if findings else 0


if __name__ == "__main__":
    raise SystemExit(main())

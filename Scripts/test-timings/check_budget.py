"""Fail the verify job when a test file (frontend) or class (backend) runs over its time budget."""

from __future__ import annotations

import argparse
import csv
import json
import re
from dataclasses import dataclass
from decimal import Decimal, InvalidOperation
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
    limit_kind: str


class CannotJudge(Exception):
    """An input the check cannot trust, so it gives no verdict rather than a wrong one."""


SKIPPED_SOURCE_FOLDERS = ("bin", "obj", "StrykerOutput")
DECLARATION = re.compile(r"\bnamespace\s+([\w.]+)|\bclass\s+(\w+)")


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


def judge(
    stack: str,
    rows: list[dict[str, str]],
    budget_seconds: Decimal,
    ceilings: dict[str, Decimal],
) -> list[Finding]:
    findings = []
    for unit, seconds in sorted(seconds_per_unit(stack, rows).items()):
        limit, limit_kind = (
            (ceilings[unit], "ceiling") if unit in ceilings else (budget_seconds, "budget")
        )
        if seconds > limit:
            findings.append(Finding(unit, seconds, limit, limit_kind))
    return findings


def stale_entries(allowlisted: list[str], still_present: set[str]) -> list[str]:
    return [unit for unit in allowlisted if unit not in still_present]


def _is_build_or_mutation_output(folder: str) -> bool:
    return folder in SKIPPED_SOURCE_FOLDERS or folder.startswith(".stryker-tmp")


def declared_classes(source_root: Path) -> set[str]:
    classes = set()
    for directory, folders, files in source_root.walk():
        folders[:] = [folder for folder in folders if not _is_build_or_mutation_output(folder)]
        for file in files:
            if file.endswith(".cs"):
                classes |= _classes_in(_source_of(directory / file))
    return classes


def _source_of(file: Path) -> str:
    try:
        return file.read_text(encoding="utf-8-sig")
    except (OSError, UnicodeDecodeError) as error:
        raise CannotJudge(f"{escaped(str(file))} cannot be read: {_reason(error)}") from error


def _classes_in(source: str) -> set[str]:
    namespace = ""
    classes = set()
    for match in DECLARATION.finditer(source):
        if match[1]:
            namespace = match[1]
        else:
            classes.add(f"{namespace}.{match[2]}" if namespace else match[2])
    return classes


def escaped(text: str) -> str:
    # A raw line break would end the annotation and let the rest of a unit name pose as a workflow command.
    return text.replace("%", "%25").replace("\r", "%0D").replace("\n", "%0A")


def annotation(finding: Finding, allowlist_name: str) -> str:
    return (
        f"::error::{escaped(finding.unit)} took {finding.seconds:.1f}s, "
        f"over the {finding.limit}s {finding.limit_kind} set in {allowlist_name}"
    )


def stale_annotation(stack: str, unit: str, allowlist_name: str) -> str:
    where = "the test run" if stack == "frontend" else "the backend test source"
    return (
        f"::error::{escaped(unit)} is allowlisted in {allowlist_name} "
        f"but is no longer in {where}; remove its entry"
    )


def load_timings(stack: str, path: Path) -> list[dict[str, str]]:
    name = escaped(str(path))
    try:
        with path.open(encoding="utf-8", newline="") as stream:
            reader = csv.DictReader(stream)
            columns = reader.fieldnames
            rows = list(reader)
    except (OSError, UnicodeDecodeError, csv.Error) as error:
        raise CannotJudge(f"{name} cannot be read: {_reason(error)}") from error
    expected_columns = vitest_to_csv.CSV_COLUMNS if stack == "frontend" else trx_to_csv.CSV_COLUMNS
    if columns is None:
        raise CannotJudge(f"{name} is empty")
    if tuple(columns) != expected_columns:
        raise CannotJudge(
            f"{name} does not have the {stack} columns {', '.join(expected_columns)}"
        )
    if not rows:
        # A green run always ran tests, so a timing file without any means it was written wrong.
        raise CannotJudge(f"{name} holds no tests")
    for line, row in enumerate(rows, start=2):
        if _milliseconds(row[DURATION_COLUMN[stack]]) is None:
            raise CannotJudge(f"{name} has an unreadable duration on line {line}")
    return rows


def _milliseconds(text: str | None) -> Decimal | None:
    try:
        value = Decimal(text)
    except (InvalidOperation, TypeError):
        return None
    return value if value.is_finite() and value >= 0 else None


def load_allowlist(stack: str, path: Path) -> tuple[Decimal, dict[str, Decimal]]:
    name = escaped(str(path))
    try:
        document = json.loads(path.read_text(encoding="utf-8"))
    except (OSError, UnicodeDecodeError, ValueError) as error:
        raise CannotJudge(f"{name} cannot be read as JSON: {_reason(error)}") from error
    section = document.get(stack) if isinstance(document, dict) else None
    if not isinstance(section, dict):
        raise CannotJudge(f"{name} has no {stack} section")
    budget_seconds = _positive_seconds(section.get("budget_seconds"))
    if budget_seconds is None:
        raise CannotJudge(f"{name} sets no positive budget_seconds in its {stack} section")
    entries = section.get("allowlist", [])
    if not isinstance(entries, list):
        raise CannotJudge(f"{name} has a {stack} allowlist that is not a list")
    return budget_seconds, dict(_allowlisted(entry, stack, name) for entry in entries)


def _allowlisted(entry: object, stack: str, name: str) -> tuple[str, Decimal]:
    if not isinstance(entry, dict) or not _is_text(entry.get("unit")):
        raise CannotJudge(f"{name} has a {stack} allowlist entry without a unit")
    unit = escaped(entry["unit"])
    if not _is_text(entry.get("reason")):
        raise CannotJudge(f"{unit} is allowlisted in {name} without a reason")
    ceiling_seconds = _positive_seconds(entry.get("ceiling_seconds"))
    if ceiling_seconds is None:
        raise CannotJudge(
            f"{unit} is allowlisted in {name} without a positive number as its ceiling_seconds"
        )
    return entry["unit"], ceiling_seconds


def _is_text(value: object) -> bool:
    return isinstance(value, str) and value.strip() != ""


def _positive_seconds(value: object) -> Decimal | None:
    # JSON true arrives as a Python bool, which would otherwise pass as the number 1.
    if isinstance(value, bool) or not isinstance(value, (int, float)):
        return None
    seconds = Decimal(str(value))
    return seconds if seconds.is_finite() and seconds > 0 else None


def _reason(error: Exception) -> str:
    return escaped(error.strerror if isinstance(error, OSError) and error.strerror else str(error))


def main(argv: list[str] | None = None) -> int:
    arguments = _build_arg_parser().parse_args(argv)
    try:
        return _check(arguments)
    except CannotJudge as refusal:
        print(f"::error::{refusal}")
        return 2


def _check(arguments: argparse.Namespace) -> int:
    rows = load_timings(arguments.stack, arguments.csv)
    budget_seconds, ceilings = load_allowlist(arguments.stack, arguments.allowlist)
    if arguments.stack == "backend" and not arguments.source_root.is_dir():
        raise CannotJudge(
            f"the backend test source folder {escaped(str(arguments.source_root))} does not exist"
        )

    findings = judge(arguments.stack, rows, budget_seconds, ceilings)
    stale = stale_entries(list(ceilings), _still_present(arguments, rows, ceilings))
    for finding in findings:
        print(annotation(finding, arguments.allowlist.name))
    for unit in stale:
        print(stale_annotation(arguments.stack, unit, arguments.allowlist.name))
    return 1 if findings or stale else 0


def _still_present(
    arguments: argparse.Namespace, rows: list[dict[str, str]], ceilings: dict[str, Decimal]
) -> set[str]:
    if arguments.stack == "frontend":
        return set(seconds_per_unit("frontend", rows))
    # Only the test source decides: a backend class may be filtered out of a run and still be alive.
    return declared_classes(arguments.source_root) if ceilings else set()


if __name__ == "__main__":
    raise SystemExit(main())

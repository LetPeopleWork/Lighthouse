"""Report each stack's test tiers, and fail the verify job when a test file (frontend) or class
(backend) runs over its time budget.

A limit is in one of three states. With none written, the check only reports. A provisional
limit, read from fewer than five CI runs, warns on a breach and never fails. An enforced limit,
read from five or more runs, fails the build on a breach.
"""

from __future__ import annotations

import sys

MINIMUM_PYTHON = (3, 12)
CANNOT_JUDGE = 2


def require_python(version_info: tuple) -> None:
    if tuple(version_info[:2]) < MINIMUM_PYTHON:
        found = ".".join(str(part) for part in version_info[:3])
        reason = (
            f"check_budget.py needs Python {'.'.join(map(str, MINIMUM_PYTHON))} or newer, "
            f"but runs on Python {found}"
        )
        print(f"::error::{reason}")
        # The interpreter exits with an integer status silently, so the reason is printed above.
        raise SystemExit(CANNOT_JUDGE)


# Refuse before importing anything an older interpreter might not be able to load.
require_python(sys.version_info)

import argparse
import csv
import json
import os
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


COLUMNS = {
    "frontend": vitest_to_csv.CSV_COLUMNS,
    "backend": trx_to_csv.CSV_COLUMNS,
}
UNIT_COLUMN = {stack: columns[0] for stack, columns in COLUMNS.items()}
DURATION_COLUMN = {stack: columns[2] for stack, columns in COLUMNS.items()}


@dataclass(frozen=True)
class Finding:
    unit: str
    seconds: Decimal
    limit: Decimal
    limit_kind: str


class CannotJudge(Exception):
    """An input the check cannot trust, so it gives no verdict rather than a wrong one."""


# Anchored at line start so a comment that merely mentions a namespace or a class declares nothing.
DECLARATION = re.compile(
    r"^[ \t]*(?:namespace\s+([\w.]+)\s*[;{]"
    r"|(?:\[[^\]\n]*\][ \t]*)*"
    r"(?:(?:public|internal|private|protected|sealed|static|abstract|partial|file|unsafe|new)\s+)*"
    r"class\s+(\w+))",
    re.MULTILINE,
)


def class_of(fully_qualified_name: str) -> str:
    # A fixture's and a method's arguments may hold dots, brackets and quotes, so only a dot
    # outside every bracket and quote separates the class from the method.
    dots = _top_level_dots(fully_qualified_name)
    class_part = fully_qualified_name[: dots[-1]] if dots else fully_qualified_name
    return class_part.split("(", 1)[0]


def _top_level_dots(name: str) -> list[int]:
    dots, depth, quote, escaping = [], 0, "", False
    for position, character in enumerate(name):
        if quote:
            if escaping:
                escaping = False
            elif character == "\\":
                escaping = True
            elif character == quote:
                quote = ""
        elif character in "\"'":
            quote = character
        elif character == "(":
            depth += 1
        elif character == ")":
            depth = max(depth - 1, 0)
        elif character == "." and depth == 0:
            dots.append(position)
    return dots


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


FAST, INTEGRATION = "Fast", "Integration"
COMPONENT_TEST_SUFFIX = ".test.tsx"
PASSED = "Passed"
OUTCOME_COLUMN = {stack: columns[3] for stack, columns in COLUMNS.items()}
BACKEND_CATEGORY_COLUMN = trx_to_csv.CSV_COLUMNS[1]


@dataclass(frozen=True)
class Tier:
    tests: int = 0
    seconds: Decimal = Decimal(0)

    def adding(self, seconds: Decimal) -> Tier:
        return Tier(self.tests + 1, self.seconds + seconds)

    @property
    def mean(self) -> Decimal | None:
        return self.seconds / self.tests if self.tests else None


@dataclass(frozen=True)
class TierCount:
    fast: Tier
    integration: Tier
    left_out: int
    not_run: int


def count_tiers(
    stack: str, rows: list[dict[str, str]], connector_classes: set[str]
) -> TierCount:
    tiers = {FAST: Tier(), INTEGRATION: Tier()}
    left_out = not_run = 0
    for row in rows:
        if stack == "backend" and class_of(row[UNIT_COLUMN[stack]]) in connector_classes:
            left_out += 1
        elif row[OUTCOME_COLUMN[stack]] != PASSED:
            not_run += 1
        else:
            tier = _tier_of(stack, row)
            tiers[tier] = tiers[tier].adding(Decimal(row[DURATION_COLUMN[stack]]) / 1000)
    return TierCount(tiers[FAST], tiers[INTEGRATION], left_out, not_run)


def _tier_of(stack: str, row: dict[str, str]) -> str:
    if stack == "frontend":
        return INTEGRATION if row[UNIT_COLUMN[stack]].endswith(COMPONENT_TEST_SUFFIX) else FAST
    return INTEGRATION if row[BACKEND_CATEGORY_COLUMN] == INTEGRATION else FAST


NOTHING_LABELLED = "nothing labelled yet"
FIGURES_WIDTH = 41


def tier_report(stack: str, count: TierCount) -> str:
    if stack == "frontend":
        return "\n".join(
            [
                "Frontend test time",
                f"{'Tier':<12}{'Tests':>7}{'Seconds':>10}{'Mean s':>9}",
                _tier_line(FAST, count.fast),
                _tier_line(INTEGRATION, count.integration),
                _aside_line("Not run", count.not_run, "did not pass, not counted"),
                "The seconds do not compare across runs: the runner sets them.",
                _shares_line(count),
            ]
        )
    labelled = count.integration.tests > 0
    integration_lines = (
        [
            _tier_line(INTEGRATION, count.integration, "mean not set"),
            f"{'':<{FIGURES_WIDTH}}total not set",
        ]
        if labelled
        else [_aside_line(INTEGRATION, None, NOTHING_LABELLED)]
    )
    return "\n".join(
        [
            "Backend test time",
            f"{'Tier':<12}{'Tests':>7}{'Seconds':>10}{'Mean s':>9}   {'Ceiling':<32}Room",
            _tier_line(FAST, count.fast, "per test not set"),
            *integration_lines,
            _aside_line("Left out", count.left_out, "connector classes, not judged"),
            _aside_line("Not run", count.not_run, "did not pass, not counted"),
            _shares_line(count) if labelled else _shares_text(NOTHING_LABELLED),
        ]
    )


def _tier_line(name: str, tier: Tier, ceiling: str = "") -> str:
    mean = "-" if tier.mean is None else f"{tier.mean:.3f}"
    line = f"{name:<12}{tier.tests:>7,}{tier.seconds:>10,.1f}{mean:>9}"
    return f"{line}   {ceiling}" if ceiling else line


def _aside_line(name: str, tests: int | None, note: str) -> str:
    figure = "" if tests is None else f"{tests:,}"
    return f"{name:<12}{figure:>7}   {note}"


def _shares_line(count: TierCount) -> str:
    counted = count.fast.tests + count.integration.tests
    fast_share = Decimal(count.fast.tests * 100) / counted if counted else Decimal(0)
    integration_share = Decimal(100) - fast_share if counted else Decimal(0)
    return _shares_text(f"{fast_share:.1f} % fast, {integration_share:.1f} % integration")


def _shares_text(shares: str) -> str:
    return f"This stack only, not the pyramid shares: {shares}. No gate."


def stale_entries(allowlisted: list[str], still_present: set[str]) -> list[str]:
    return [unit for unit in allowlisted if unit not in still_present]


WARN_ONLY_VARIABLE = "LIGHTHOUSE_BUDGET_WARN_ONLY"
SUMMARY_VARIABLE = "GITHUB_STEP_SUMMARY"


def warn_only_requested(environment: dict[str, str], flag: bool) -> bool:
    if flag:
        return True
    value = environment.get(WARN_ONLY_VARIABLE, "")
    if value in ("", "false"):
        return False
    if value == "true":
        return True
    raise CannotJudge(
        f"{WARN_ONLY_VARIABLE} is set to {escaped(repr(value))}, "
        "but only true or false (or leaving it unset) are understood"
    )


def summary_file(environment: dict[str, str]) -> Path | None:
    name = environment.get(SUMMARY_VARIABLE, "")
    return Path(name) if name else None


def summary_block(report: str) -> str:
    # The summary renders Markdown, which would fold the report's columns into one paragraph.
    return f"\n```\n{report}\n```\n"


def unneeded_entries(
    stack: str,
    rows: list[dict[str, str]],
    budget_seconds: Decimal,
    ceilings: dict[str, Decimal],
) -> list[str]:
    seconds = seconds_per_unit(stack, rows)
    return [
        unit
        for unit, ceiling in ceilings.items()
        if _is_not_needed(stack, unit, ceiling, budget_seconds, seconds)
    ]


def _is_not_needed(
    stack: str, unit: str, ceiling: Decimal, budget_seconds: Decimal, seconds: dict[str, Decimal]
) -> bool:
    if ceiling == budget_seconds:
        return True
    # Backend class sets vary per run, so only a frontend file's run time says its entry is idle.
    return (
        stack == "frontend"
        and ceiling > budget_seconds
        and unit in seconds
        and seconds[unit] < budget_seconds / 4
    )


def unneeded_annotation(unit: str, allowlist_name: str) -> str:
    return f"::error::{escaped(unit)} is allowlisted in {allowlist_name} but is not needed; remove its entry"


def declared_classes(source_root: Path) -> set[str]:
    classes = set()
    for directory, folders, files in source_root.walk():
        folders[:] = [
            folder for folder in folders if not trx_to_csv.is_build_or_mutation_output(folder)
        ]
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


def breach_annotation(finding: Finding, allowlist_name: str, kind: str) -> str:
    return (
        f"::{kind}::{escaped(finding.unit)} took {finding.seconds:.1f}s, "
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
    expected_columns = COLUMNS[stack]
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
    ceilings: dict[str, Decimal] = {}
    for entry in entries:
        unit, ceiling_seconds = _allowlisted(entry, stack, name)
        if unit in ceilings:
            raise CannotJudge(
                f"{name} allowlists {escaped(unit)} more than once in its {stack} section"
            )
        ceilings[unit] = ceiling_seconds
    return budget_seconds, ceilings


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


def _build_arg_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(
        description="Check a test-timings CSV against the per-file / per-class time budget."
    )
    parser.add_argument(
        "--stack",
        choices=tuple(COLUMNS),
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


def main(argv: list[str] | None = None) -> int:
    arguments = _build_arg_parser().parse_args(argv)
    try:
        return _check(arguments)
    except CannotJudge as refusal:
        print(f"::error::{refusal}")
        return CANNOT_JUDGE


def _check(arguments: argparse.Namespace) -> int:
    stack, allowlist_name = arguments.stack, arguments.allowlist.name
    warn_only = warn_only_requested(os.environ, arguments.warn_only)
    summary = summary_file(os.environ)
    rows = load_timings(stack, arguments.csv)
    budget_seconds, ceilings = load_allowlist(stack, arguments.allowlist)
    if stack == "backend" and not arguments.source_root.is_dir():
        raise CannotJudge(
            f"the backend test source folder {escaped(str(arguments.source_root))} does not exist"
        )
    connector_classes = _connector_classes(stack, arguments.source_root)
    publish_report(tier_report(stack, count_tiers(stack, rows, connector_classes)), summary)

    findings = judge(stack, rows, budget_seconds, ceilings)
    still_present = _still_present(stack, rows, arguments.source_root, ceilings)
    stale = stale_entries(list(ceilings), still_present)
    kind = "warning" if warn_only else "error"
    if os.environ.get(WARN_ONLY_VARIABLE) == "true":
        print(f"::notice::{WARN_ONLY_VARIABLE} is true, so budget enforcement is off")
    for finding in findings:
        print(breach_annotation(finding, allowlist_name, kind))
    for unit in stale:
        print(stale_annotation(stack, unit, allowlist_name))
    unneeded = unneeded_entries(stack, rows, budget_seconds, ceilings)
    for unit in unneeded:
        print(unneeded_annotation(unit, allowlist_name))
    breached = bool(findings) and not warn_only
    return 1 if breached or stale or unneeded else 0


def publish_report(report: str, summary: Path | None) -> None:
    print(report)
    if summary is None:
        return
    try:
        with summary.open("a", encoding="utf-8") as stream:
            stream.write(summary_block(report))
    except OSError as error:
        print(
            f"::warning::the report could not be added to the job summary "
            f"{escaped(str(summary))}: {_reason(error)}"
        )


def _connector_classes(stack: str, source_root: Path) -> set[str]:
    if stack == "frontend":
        return set()
    try:
        return trx_to_csv.discover_connector_class_names(source_root)
    except trx_to_csv.UnreadableSourceError as error:
        raise CannotJudge(
            f"{escaped(str(error.path))} cannot be read, so the classes that talk to a real "
            "tracker cannot be told apart"
        ) from error


def _still_present(
    stack: str, rows: list[dict[str, str]], source_root: Path, ceilings: dict[str, Decimal]
) -> set[str]:
    if stack == "frontend":
        return set(seconds_per_unit(stack, rows))
    # Only the test source decides: a backend class may be filtered out of a run and still be alive.
    return declared_classes(source_root) if ceilings else set()


if __name__ == "__main__":
    raise SystemExit(main())

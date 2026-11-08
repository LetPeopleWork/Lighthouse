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
from decimal import ROUND_UP, Decimal, InvalidOperation
from pathlib import Path

import trx_to_csv
import vitest_to_csv

HERE = Path(__file__).resolve().parent
REPOSITORY_ROOT = HERE.parent.parent
DEFAULT_ALLOWLIST = HERE / "test-time-budget.json"
DEFAULT_BACKEND_SOURCE_ROOT = REPOSITORY_ROOT / "Lighthouse.Backend" / "Lighthouse.Backend.Tests"
DEFAULT_SPEC_ROOT = REPOSITORY_ROOT / "Lighthouse.EndToEndTests" / "tests" / "specs"


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
    provisional: bool = False


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
    largest: Decimal = Decimal(0)

    def adding(self, seconds: Decimal) -> Tier:
        return Tier(self.tests + 1, self.seconds + seconds, max(self.largest, seconds))

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


FAST_TEST_LIMIT, CLASS_CEILING = "fast test limit", "class ceiling"


def judge_fast(
    rows: list[dict[str, str]],
    connector_classes: set[str],
    limit: Limit,
    ceilings: dict[str, Decimal],
) -> list[Finding]:
    """Each backend test counted in the fast tier, one at a time, against the fast test limit."""
    if limit.in_force is None:
        return []
    findings = []
    for row in rows:
        test = row[UNIT_COLUMN["backend"]]
        unit = class_of(test)
        if (
            unit in connector_classes
            or row[OUTCOME_COLUMN["backend"]] != PASSED
            or _tier_of("backend", row) != FAST
        ):
            continue
        seconds = Decimal(row[DURATION_COLUMN["backend"]]) / 1000
        if unit in ceilings:
            if seconds > ceilings[unit]:
                findings.append(Finding(test, seconds, ceilings[unit], CLASS_CEILING))
            continue
        breached = limit.breached_by(seconds)
        if breached:
            kind = _kind(FAST_TEST_LIMIT, breached)
            findings.append(Finding(test, seconds, breached.seconds, kind, not breached.enforced))
    return findings


def _kind(limit_kind: str, entry: LedgerEntry) -> str:
    return limit_kind if entry.enforced else f"provisional {limit_kind}"


RUNS_TO_ENFORCE = 5
LEDGERS_FIELD = "ledgers"
FAST_LEDGER, MEAN_LEDGER, TOTAL_LEDGER = "fast_test_limit", "mean_ceiling", "total_ceiling"
LEDGER_WORDS = {
    FAST_LEDGER: "fast test limit ledger",
    MEAN_LEDGER: "mean ceiling ledger",
    TOTAL_LEDGER: "total ceiling ledger",
}
TESTS_WHEN_SET = "tests_when_set"


@dataclass(frozen=True)
class LedgerEntry:
    seconds: Decimal
    runs: int
    tests_when_set: int | None = None

    @property
    def enforced(self) -> bool:
        return self.runs >= RUNS_TO_ENFORCE


@dataclass(frozen=True)
class Limit:
    """What one ledger says today: its newest enforced entry, and the provisional one after it."""

    enforced: LedgerEntry | None = None
    provisional: LedgerEntry | None = None

    @property
    def in_force(self) -> LedgerEntry | None:
        return self.enforced or self.provisional

    @property
    def enforced_seconds(self) -> Decimal | None:
        return self.enforced.seconds if self.enforced else None

    def breached_by(self, value: Decimal) -> LedgerEntry | None:
        """The entry a value is over: the enforced one first, so a lower provisional one only
        warns between the two and can never stand in for a failure."""
        for entry in (self.enforced, self.provisional):
            if entry is not None and value > entry.seconds:
                return entry
        return None


@dataclass(frozen=True)
class Ledgers:
    fast: Limit = Limit()
    mean: Limit = Limit()
    total: Limit = Limit()


NO_LEDGERS = Ledgers()
MEAN_CEILING, TOTAL_CEILING = "mean ceiling", "total ceiling"


@dataclass(frozen=True)
class TierBreach:
    text: str
    provisional: bool


def judge_integration(
    integration: Tier, ledgers: Ledgers, allowlist_name: str
) -> list[TierBreach]:
    """The backend Integration tier as a whole against its mean and total ceilings."""
    breaches = []
    mean = ledgers.mean.breached_by(integration.mean) if integration.mean is not None else None
    if mean:
        breaches.append(
            TierBreach(
                f"{INTEGRATION} tests averaged {_over(integration.mean)}s each over "
                f"{integration.tests:,} tests, over the {mean.seconds}s "
                f"{_kind(MEAN_CEILING, mean)} set in {allowlist_name}",
                not mean.enforced,
            )
        )
    total = ledgers.total.breached_by(integration.seconds)
    if total:
        breaches.append(
            TierBreach(
                f"{INTEGRATION} tests took {_over(integration.seconds)}s in all over "
                f"{integration.tests:,} tests, over the {total.seconds}s "
                f"{_kind(TOTAL_CEILING, total)} set in {allowlist_name}; "
                f"{integration.tests - total.tests_when_set:+d} tests since it was set",
                not total.enforced,
            )
        )
    return breaches


def provisional_notes(ledgers: Ledgers) -> list[str]:
    named = (
        (FAST_TEST_LIMIT, ledgers.fast),
        (f"{INTEGRATION} {MEAN_CEILING}", ledgers.mean),
        (f"{INTEGRATION} {TOTAL_CEILING}", ledgers.total),
    )
    return [
        f"the {name} of {limit.provisional.seconds}s is provisional, {limit.provisional.runs} "
        f"of {RUNS_TO_ENFORCE} runs, so it warns and does not fail"
        for name, limit in named
        if limit.provisional is not None
    ]


def refuse_an_unlabelled_run(integration: Tier, ledgers: Ledgers, allowlist_name: str) -> None:
    # A run whose tests all read Unit has no tier to hold to these limits, so a pass would be
    # a verdict nobody checked.
    in_force = any(limit.in_force for limit in (ledgers.fast, ledgers.mean, ledgers.total))
    if in_force and integration.tests == 0:
        raise CannotJudge(
            f"{allowlist_name} holds limits that need {INTEGRATION} tests, but this run has none, "
            "so it cannot be judged"
        )


def _over(seconds: Decimal) -> str:
    # Rounded up, so a value a fraction over its ceiling never prints as equal to it.
    return f"{seconds.quantize(Decimal('0.001'), rounding=ROUND_UP)}"
NOTHING_LABELLED = "nothing labelled yet"
FIGURES_WIDTH = 41
CEILING_WIDTH = 31


def tier_report(stack: str, count: TierCount, ledgers: Ledgers = NO_LEDGERS) -> str:
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
    integration = count.integration
    integration_lines = (
        [
            _tier_line(
                INTEGRATION,
                integration,
                _ceiling_cell("mean", "", ledgers.mean, integration.mean),
            ),
            f"{'':<{FIGURES_WIDTH}}"
            + _ceiling_cell("total", "", ledgers.total, integration.seconds),
        ]
        if labelled
        else [_aside_line(INTEGRATION, None, NOTHING_LABELLED)]
    )
    return "\n".join(
        [
            "Backend test time",
            f"{'Tier':<12}{'Tests':>7}{'Seconds':>10}{'Mean s':>9}   {'Ceiling':<32}Room",
            _tier_line(FAST, count.fast, _fast_cell(ledgers.fast, count.fast)),
            *integration_lines,
            _aside_line("Left out", count.left_out, "connector classes, not judged"),
            _aside_line("Not run", count.not_run, "did not pass, not counted"),
            *_since_the_total_was_set(ledgers.total, integration),
            _shares_line(count) if labelled else _shares_text(NOTHING_LABELLED),
        ]
    )


def _fast_cell(limit: Limit, fast: Tier) -> str:
    room = f"largest {fast.largest:.3f} s" if limit.in_force else ""
    return _cell(_limit_text("per test", " s", limit), room)


def _ceiling_cell(label: str, unit: str, limit: Limit, value: Decimal | None) -> str:
    return _cell(_limit_text(label, unit, limit), _room(limit, value))


def _cell(limit_text: str, room: str) -> str:
    return f"{limit_text:<{CEILING_WIDTH}} {room}".rstrip()


def _limit_text(label: str, unit: str, limit: Limit) -> str:
    shown, provisional = limit.in_force, limit.provisional
    if shown is None:
        return f"{label} not set"
    text = f"{label} {shown.seconds:,}{unit}"
    if provisional is None:
        return text
    if limit.enforced is None:
        return f"{text} (provisional, {provisional.runs} of {RUNS_TO_ENFORCE})"
    return f"{text}, provisional {provisional.seconds:,} ({provisional.runs} of {RUNS_TO_ENFORCE})"


def _room(limit: Limit, value: Decimal | None) -> str:
    shown = limit.in_force
    if shown is None or value is None:
        return ""
    return f"{(shown.seconds - value) / shown.seconds * 100:.0f} %"


def _since_the_total_was_set(total: Limit, integration: Tier) -> list[str]:
    shown = total.in_force
    if shown is None:
        return []
    return [f"Since the total was set: {integration.tests - shown.tests_when_set:+d} tests"]


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


PYRAMID_LIMITS = {FAST: "90 %", INTEGRATION: "9.5 %", "E2E": "0.5 %"}
# A Playwright test is declared with a plain `test(` or with a fixture-extended `testWith<Name>(`,
# always at the start of a line; `test.describe(` and `test.skip(` declare none.
E2E_DECLARATION = re.compile(r"^[ \t]*(?:test|testWith[A-Z]\w*)\(", re.MULTILINE)
SPEC_SUFFIX = ".spec.ts"


def pyramid_report(frontend: TierCount, backend: TierCount, e2e_tests: int) -> str:
    tests = {
        FAST: frontend.fast.tests + backend.fast.tests,
        INTEGRATION: frontend.integration.tests + backend.integration.tests,
        "E2E": e2e_tests,
    }
    counted = sum(tests.values())
    unlabelled = (
        ["The backend has nothing labelled yet, so all its tests count as fast."]
        if backend.integration.tests == 0
        else []
    )
    return "\n".join(
        [
            "Test pyramid across both stacks and Playwright",
            f"{'Tier':<12}{'Tests':>7}{'Share':>9}   Upper limit",
            *(_pyramid_line(tier, count, counted) for tier, count in tests.items()),
            _aside_line("Left out", backend.left_out, "connector classes, not counted"),
            _aside_line(
                "Not run", frontend.not_run + backend.not_run, "did not pass, not counted"
            ),
            *unlabelled,
            "E2E counts the test declarations in the Playwright specs, not what ran. No gate.",
        ]
    )


def _pyramid_line(tier: str, tests: int, counted: int) -> str:
    share = Decimal(tests * 100) / counted if counted else Decimal(0)
    return f"{tier:<12}{tests:>7,}{f'{share:.1f} %':>9}   {PYRAMID_LIMITS[tier]}"


def e2e_tests(spec_root: Path) -> int:
    if not spec_root.is_dir():
        raise CannotJudge(
            f"the Playwright spec folder {escaped(str(spec_root))} does not exist"
        )
    return sum(
        len(E2E_DECLARATION.findall(_source_of(directory / file)))
        for directory, _, files in spec_root.walk()
        for file in files
        if file.endswith(SPEC_SUFFIX)
    )


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


def breach_annotation(finding: Finding, allowlist_name: str, kind: str, places: int = 1) -> str:
    return (
        f"::{kind}::{escaped(finding.unit)} took {finding.seconds:.{places}f}s, "
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


def _section(stack: str, path: Path) -> tuple[str, dict]:
    name = escaped(str(path))
    try:
        document = json.loads(path.read_text(encoding="utf-8"))
    except (OSError, UnicodeDecodeError, ValueError) as error:
        raise CannotJudge(f"{name} cannot be read as JSON: {_reason(error)}") from error
    section = document.get(stack) if isinstance(document, dict) else None
    if not isinstance(section, dict):
        raise CannotJudge(f"{name} has no {stack} section")
    return name, section


def load_ledgers(path: Path) -> Ledgers:
    """The backend's three ledgers of limits, refused whole if any of them is broken or rises."""
    name, section = _section("backend", path)
    block = section.get(LEDGERS_FIELD, {})
    if not isinstance(block, dict):
        raise CannotJudge(
            f"{name} has a {LEDGERS_FIELD} block in its backend section that is not an object"
        )
    return Ledgers(
        *(
            _limit_of(block.get(ledger, []), ledger, f"{name} has a {LEDGER_WORDS[ledger]}")
            for ledger in (FAST_LEDGER, MEAN_LEDGER, TOTAL_LEDGER)
        )
    )


def _limit_of(entries: object, ledger: str, where: str) -> Limit:
    if not isinstance(entries, list):
        raise CannotJudge(f"{where} that is not a list")
    read = [
        _ledger_entry(entry, ledger, f"{where} whose entry {number}")
        for number, entry in enumerate(entries, start=1)
    ]
    _refuse_an_early_provisional(read, where)
    if ledger != TOTAL_LEDGER:
        _refuse_a_rise(read, where)
    enforced = [entry for entry in read if entry.enforced]
    return Limit(
        enforced=enforced[-1] if enforced else None,
        provisional=read[-1] if read and not read[-1].enforced else None,
    )


def _refuse_an_early_provisional(read: list[LedgerEntry], where: str) -> None:
    for number, entry in enumerate(read[:-1], start=1):
        if not entry.enforced:
            raise CannotJudge(
                f"{where} whose entry {number} is provisional but not the last; only the last "
                f"entry may have fewer than {RUNS_TO_ENFORCE} run ids"
            )


def _refuse_a_rise(read: list[LedgerEntry], where: str) -> None:
    newest: LedgerEntry | None = None
    for number, entry in enumerate(read, start=1):
        if newest is not None and entry.enforced and entry.seconds >= newest.seconds:
            raise CannotJudge(
                f"{where} whose entry {number}, {entry.seconds}s, is not below the "
                f"{newest.seconds}s before it; this limit only goes down"
            )
        if newest is not None and entry.seconds > newest.seconds:
            raise CannotJudge(
                f"{where} whose provisional entry {number}, {entry.seconds}s, is above the "
                f"enforced {newest.seconds}s before it; this limit only goes down"
            )
        newest = entry if entry.enforced else newest


def _ledger_entry(entry: object, ledger: str, where: str) -> LedgerEntry:
    if not isinstance(entry, dict):
        raise CannotJudge(f"{where} is not an object")
    seconds = _positive_seconds(entry.get("seconds"))
    if seconds is None:
        raise CannotJudge(f"{where} has no positive number as its seconds")
    if not _is_text(entry.get("reason")):
        raise CannotJudge(f"{where} has no reason")
    runs = _distinct_runs(entry.get("runs"), where)
    if ledger != TOTAL_LEDGER:
        return LedgerEntry(seconds, runs)
    return LedgerEntry(seconds, runs, _tests_when_set(entry.get(TESTS_WHEN_SET), where))


def _distinct_runs(runs: object, where: str) -> int:
    if not isinstance(runs, list):
        raise CannotJudge(f"{where} has no list of run ids")
    if not runs:
        raise CannotJudge(f"{where} has no run id")
    seen: set[str] = set()
    for run in runs:
        if not _is_run_id(run):
            raise CannotJudge(f"{where} has a run id that is neither a whole number nor text")
        # The same run written once as a number and once as text is still one run.
        if str(run) in seen:
            raise CannotJudge(f"{where} names run {escaped(str(run))} more than once")
        seen.add(str(run))
    return len(seen)


def _is_run_id(run: object) -> bool:
    # JSON true arrives as a Python bool, which would otherwise pass as the number 1.
    return _is_text(run) or (isinstance(run, int) and not isinstance(run, bool))


def _tests_when_set(tests: object, where: str) -> int:
    if isinstance(tests, bool) or not isinstance(tests, int) or tests <= 0:
        raise CannotJudge(f"{where} has no positive whole number as its {TESTS_WHEN_SET}")
    return tests


def load_allowlist(stack: str, path: Path) -> tuple[Decimal, dict[str, Decimal]]:
    name, section = _section(stack, path)
    budget_seconds = _positive_seconds(section.get("budget_seconds"))
    if budget_seconds is None:
        raise CannotJudge(f"{name} sets no positive budget_seconds in its {stack} section")
    ceilings = _ceilings_of(
        section.get("allowlist", []), name, f"{stack} allowlist", name, f"its {stack} section"
    )
    return budget_seconds, ceilings


FAST_ALLOWLIST_FIELD = "fast_allowlist"
MOST_FAST_ALLOWLIST_ENTRIES = 10


def load_fast_allowlist(path: Path, fast: Limit) -> dict[str, Decimal]:
    """Backend classes whose tests answer to their own ceiling instead of the fast test limit."""
    name, section = _section("backend", path)
    ceilings = _ceilings_of(
        section.get(FAST_ALLOWLIST_FIELD, []),
        name,
        FAST_ALLOWLIST_FIELD,
        f"the {FAST_ALLOWLIST_FIELD} of {name}",
        f"its {FAST_ALLOWLIST_FIELD}",
    )
    if len(ceilings) > MOST_FAST_ALLOWLIST_ENTRIES:
        raise CannotJudge(
            f"{name} lists {len(ceilings)} classes in its {FAST_ALLOWLIST_FIELD}, "
            f"but at most {MOST_FAST_ALLOWLIST_ENTRIES} may be listed"
        )
    # A class may be listed only for being slow in every run the limit was read from, which a
    # limit read from fewer than five runs cannot show.
    if ceilings and fast.enforced is None:
        raise CannotJudge(
            f"{name} lists classes in its {FAST_ALLOWLIST_FIELD}, but its "
            f"{LEDGER_WORDS[FAST_LEDGER]} has no enforced entry to list them against"
        )
    return ceilings


def _ceilings_of(
    entries: object, name: str, list_words: str, where: str, scope: str
) -> dict[str, Decimal]:
    if not isinstance(entries, list):
        raise CannotJudge(f"{name} has a {list_words} that is not a list")
    ceilings: dict[str, Decimal] = {}
    for entry in entries:
        unit, ceiling_seconds = _allowlisted(entry, name, list_words, where)
        if unit in ceilings:
            raise CannotJudge(f"{name} allowlists {escaped(unit)} more than once in {scope}")
        ceilings[unit] = ceiling_seconds
    return ceilings


def _allowlisted(entry: object, name: str, list_words: str, where: str) -> tuple[str, Decimal]:
    if not isinstance(entry, dict) or not _is_text(entry.get("unit")):
        raise CannotJudge(f"{name} has a {list_words} entry without a unit")
    unit = escaped(entry["unit"])
    if not _is_text(entry.get("reason")):
        raise CannotJudge(f"{unit} is allowlisted in {where} without a reason")
    ceiling_seconds = _positive_seconds(entry.get("ceiling_seconds"))
    if ceiling_seconds is None:
        raise CannotJudge(
            f"{unit} is allowlisted in {where} without a positive number as its ceiling_seconds"
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
        help="Which suite the CSV came from: frontend sums per file, backend per class.",
    )
    parser.add_argument(
        "--csv",
        type=Path,
        help="test-timings-frontend.csv or test-timings-backend.csv.",
    )
    parser.add_argument(
        "--pyramid",
        action="store_true",
        help="Only print the fast, integration and E2E shares across both stacks and "
        "Playwright beside their upper limits. Judges nothing and writes nothing.",
    )
    parser.add_argument("--frontend-csv", type=Path, help="With --pyramid: the frontend CSV.")
    parser.add_argument("--backend-csv", type=Path, help="With --pyramid: the backend CSV.")
    parser.add_argument(
        "--spec-root",
        type=Path,
        default=DEFAULT_SPEC_ROOT,
        help="With --pyramid: the Playwright spec folder the E2E tests are counted in.",
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
    parser = _build_arg_parser()
    arguments = parser.parse_args(argv)
    needed = ("frontend_csv", "backend_csv") if arguments.pyramid else ("stack", "csv")
    missing = [name for name in needed if getattr(arguments, name) is None]
    if missing:
        options = ", ".join(f"--{name.replace('_', '-')}" for name in missing)
        parser.error(f"the following arguments are required: {options}")
    try:
        return _pyramid(arguments) if arguments.pyramid else _check(arguments)
    except CannotJudge as refusal:
        print(f"::error::{refusal}")
        return CANNOT_JUDGE


def _check(arguments: argparse.Namespace) -> int:
    stack, allowlist_name = arguments.stack, arguments.allowlist.name
    warn_only = warn_only_requested(os.environ, arguments.warn_only)
    summary = summary_file(os.environ)
    rows = load_timings(stack, arguments.csv)
    budget_seconds, ceilings = load_allowlist(stack, arguments.allowlist)
    ledgers, fast_ceilings = _backend_limits(stack, arguments.allowlist)
    if stack == "backend":
        _require_source_root(arguments.source_root)
    connector_classes = _connector_classes(stack, arguments.source_root)
    count = count_tiers(stack, rows, connector_classes)
    publish_report(tier_report(stack, count, ledgers), summary)
    refuse_an_unlabelled_run(count.integration, ledgers, allowlist_name)

    findings = judge(stack, rows, budget_seconds, ceilings)
    fast_limit = ledgers.fast.enforced_seconds
    fast_findings = judge_fast(rows, connector_classes, ledgers.fast, fast_ceilings)
    tier_breaches = judge_integration(count.integration, ledgers, allowlist_name)
    still_present = _still_present(stack, rows, arguments.source_root, ceilings or fast_ceilings)
    stale = stale_entries([*ceilings, *fast_ceilings], still_present)
    kind = "warning" if warn_only else "error"
    if os.environ.get(WARN_ONLY_VARIABLE) == "true":
        print(f"::notice::{WARN_ONLY_VARIABLE} is true, so budget enforcement is off")
    for finding in findings:
        print(breach_annotation(finding, allowlist_name, kind))
    _print_tier_verdicts(ledgers, fast_findings, tier_breaches, allowlist_name, kind)
    for unit in stale:
        print(stale_annotation(stack, unit, allowlist_name))
    unneeded = unneeded_entries(stack, rows, budget_seconds, ceilings)
    if fast_ceilings:
        unneeded += unneeded_entries(stack, rows, fast_limit, fast_ceilings)
    for unit in unneeded:
        print(unneeded_annotation(unit, allowlist_name))
    failing = [*findings, *fast_findings, *tier_breaches]
    breached = any(not breach.provisional for breach in failing) and not warn_only
    return 1 if breached or stale or unneeded else 0


def _pyramid(arguments: argparse.Namespace) -> int:
    frontend_rows = load_timings("frontend", arguments.frontend_csv)
    backend_rows = load_timings("backend", arguments.backend_csv)
    _require_source_root(arguments.source_root)
    connector_classes = _connector_classes("backend", arguments.source_root)
    e2e = e2e_tests(arguments.spec_root)
    frontend = count_tiers("frontend", frontend_rows, set())
    backend = count_tiers("backend", backend_rows, connector_classes)
    print(pyramid_report(frontend, backend, e2e))
    return 0


def _require_source_root(source_root: Path) -> None:
    if not source_root.is_dir():
        raise CannotJudge(
            f"the backend test source folder {escaped(str(source_root))} does not exist"
        )


def _print_tier_verdicts(
    ledgers: Ledgers,
    fast_findings: list[Finding],
    tier_breaches: list[TierBreach],
    allowlist_name: str,
    kind: str,
) -> None:
    for note in provisional_notes(ledgers):
        print(f"::warning::{note}")
    for finding in fast_findings:
        severity = _severity(finding.provisional, kind)
        print(breach_annotation(finding, allowlist_name, severity, places=3))
    for breach in tier_breaches:
        print(f"::{_severity(breach.provisional, kind)}::{breach.text}")


def _severity(provisional: bool, kind: str) -> str:
    return "warning" if provisional else kind


def _backend_limits(stack: str, allowlist: Path) -> tuple[Ledgers, dict[str, Decimal]]:
    if stack != "backend":
        return NO_LEDGERS, {}
    ledgers = load_ledgers(allowlist)
    return ledgers, load_fast_allowlist(allowlist, ledgers.fast)


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

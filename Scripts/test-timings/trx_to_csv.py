"""Extract per-test timings from a TRX file into a CSV row stream.

NUnit's TRX adapter does not surface ``[Category("Integration")]`` in TRX
attributes, so integration classification is derived by scanning the C#
source tree for class-level ``[Category("Integration")]`` and matching
against the ``className`` recorded in ``<TestMethod>``.
"""

from __future__ import annotations

import argparse
import csv
import re
import sys
import xml.etree.ElementTree as ET
from pathlib import Path
from typing import Iterable, TextIO

TRX_NS = "{http://microsoft.com/schemas/VisualStudio/TeamTest/2010}"
CSV_COLUMNS = ("fully_qualified_name", "category", "duration_ms", "outcome")
INTEGRATION_CATEGORY_AT_CLASS_PATTERN = re.compile(
    r'\[Category\(\s*"Integration"\s*\)\]\s*(?:public\s+|internal\s+|sealed\s+|static\s+|partial\s+)*class\s+([A-Za-z_][A-Za-z0-9_]*)'
)
NAMESPACE_PATTERN = re.compile(
    r"^\s*namespace\s+([A-Za-z_][A-Za-z0-9_.]*)\s*[;{]", re.MULTILINE
)
CLASS_DECLARATION_PATTERN = re.compile(
    r"^\s*(?:public\s+|internal\s+|sealed\s+|static\s+|partial\s+|abstract\s+)*class\s+([A-Za-z_][A-Za-z0-9_]*)"
)
INTEGRATION_ATTRIBUTE_PATTERN = re.compile(
    r'\[Category\(\s*"Integration"\s*\)\]'
)
METHOD_DECLARATION_PATTERN = re.compile(
    r"^\s*(?:public\s+|internal\s+|protected\s+|private\s+|static\s+|async\s+|virtual\s+|override\s+|sealed\s+)*"
    r"(?:Task<[^>]+>|Task|void|[A-Za-z_][A-Za-z0-9_]*)\s+([A-Za-z_][A-Za-z0-9_]*)\s*\("
)


def _duration_to_milliseconds(raw: str) -> float:
    hours_str, minutes_str, seconds_str = raw.split(":")
    seconds = float(seconds_str)
    return ((int(hours_str) * 60 + int(minutes_str)) * 60 + seconds) * 1000.0


def _class_name_lookup(root: ET.Element) -> dict[str, str]:
    lookup: dict[str, str] = {}
    for unit_test in root.iter(f"{TRX_NS}UnitTest"):
        test_id = unit_test.get("id")
        method = unit_test.find(f"{TRX_NS}TestMethod")
        if test_id and method is not None:
            class_name = method.get("className")
            if class_name:
                lookup[test_id] = class_name
    return lookup


def _bare_method_name(test_name: str) -> str:
    paren = test_name.find("(")
    return test_name if paren < 0 else test_name[:paren]


def parse_trx(
    xml_content: str,
    integration_class_names: set[str] | None = None,
    integration_method_fqns: set[str] | None = None,
) -> list[dict[str, object]]:
    """Parse a TRX document into row dicts matching ``CSV_COLUMNS``."""
    integration_classes = integration_class_names or set()
    integration_methods = integration_method_fqns or set()
    root = ET.fromstring(xml_content)
    class_by_id = _class_name_lookup(root)

    rows: list[dict[str, object]] = []
    for result in root.iter(f"{TRX_NS}UnitTestResult"):
        test_id = result.get("testId")
        method_name = result.get("testName", "")
        class_name = class_by_id.get(test_id or "", "")
        fqn = (
            f"{class_name}.{method_name}".lstrip(".")
            if method_name
            else class_name
        )
        method_fqn = f"{class_name}.{_bare_method_name(method_name)}".lstrip(".")
        category = (
            "Integration"
            if class_name in integration_classes
            or method_fqn in integration_methods
            else "Unit"
        )
        rows.append(
            {
                "fully_qualified_name": fqn,
                "category": category,
                "duration_ms": _duration_to_milliseconds(
                    result.get("duration", "00:00:00.0000000")
                ),
                "outcome": result.get("outcome", ""),
            }
        )
    return rows


def discover_integration_class_names(source_root: Path) -> set[str]:
    """Scan a C# source tree for classes carrying class-level ``[Category("Integration")]``."""
    names: set[str] = set()
    for cs_file in source_root.rglob("*.cs"):
        content = _read_cs_file(cs_file)
        if content is None:
            continue
        namespace_match = NAMESPACE_PATTERN.search(content)
        if not namespace_match:
            continue
        namespace = namespace_match.group(1)
        for class_match in INTEGRATION_CATEGORY_AT_CLASS_PATTERN.finditer(content):
            names.add(f"{namespace}.{class_match.group(1)}")
    return names


def discover_integration_method_fqns(source_root: Path) -> set[str]:
    """Scan a C# source tree for methods carrying ``[Category("Integration")]``.

    Returns the bare ``namespace.class.method`` FQNs — parametrized-test name
    suffixes are excluded so callers can match TRX ``testName`` after stripping
    the ``(...)`` argument tail.
    """
    fqns: set[str] = set()
    for cs_file in source_root.rglob("*.cs"):
        content = _read_cs_file(cs_file)
        if content is None:
            continue
        namespace_match = NAMESPACE_PATTERN.search(content)
        if not namespace_match:
            continue
        namespace = namespace_match.group(1)

        current_class: str | None = None
        pending_integration = False
        for line in content.splitlines():
            class_match = CLASS_DECLARATION_PATTERN.match(line)
            if class_match:
                current_class = class_match.group(1)
                pending_integration = False
                continue
            if INTEGRATION_ATTRIBUTE_PATTERN.search(line):
                pending_integration = True
                continue
            if pending_integration and current_class is not None:
                method_match = METHOD_DECLARATION_PATTERN.match(line)
                if method_match:
                    fqns.add(
                        f"{namespace}.{current_class}.{method_match.group(1)}"
                    )
                    pending_integration = False
    return fqns


def _read_cs_file(path: Path) -> str | None:
    try:
        return path.read_text(encoding="utf-8")
    except (OSError, UnicodeDecodeError):
        return None


def rows_to_csv(rows: Iterable[dict[str, object]], stream: TextIO) -> None:
    ordered = sorted(rows, key=lambda r: r["duration_ms"], reverse=True)
    writer = csv.writer(stream, lineterminator="\n")
    writer.writerow(CSV_COLUMNS)
    for row in ordered:
        writer.writerow(
            [
                row["fully_qualified_name"],
                row["category"],
                f"{float(row['duration_ms']):.3f}",
                row["outcome"],
            ]
        )


def _build_arg_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(
        description="Extract per-test timings from one or more TRX files into a CSV."
    )
    parser.add_argument(
        "--trx",
        type=Path,
        required=True,
        action="append",
        help="Path to a .trx file (repeat the flag for multiple).",
    )
    parser.add_argument(
        "--source-root",
        type=Path,
        default=None,
        help="C# source tree root used to classify Integration tests. "
        "Omit to mark every test as Unit.",
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
    integration_classes: set[str] = set()
    integration_methods: set[str] = set()
    if args.source_root is not None:
        integration_classes = discover_integration_class_names(args.source_root)
        integration_methods = discover_integration_method_fqns(args.source_root)
    rows: list[dict[str, object]] = []
    for trx_path in args.trx:
        if not trx_path.exists():
            print(f"warning: TRX not found at {trx_path}", file=sys.stderr)
            continue
        rows.extend(
            parse_trx(
                trx_path.read_text(encoding="utf-8-sig"),
                integration_class_names=integration_classes,
                integration_method_fqns=integration_methods,
            )
        )
    args.output.parent.mkdir(parents=True, exist_ok=True)
    with args.output.open("w", encoding="utf-8", newline="") as stream:
        rows_to_csv(rows, stream)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())

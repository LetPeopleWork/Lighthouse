"""Extract per-test timings from TRX files into a CSV row stream.

A TRX file does not carry NUnit categories, so the ``category`` column is read
from the C# source tree, from each class's declaration, its chain of first bases
and any ``[Category("Integration")]`` attribute. Classes are looked up by the
``className`` in ``<TestMethod>``, less a parametrised fixture's arguments.

- ``Integration``: a hosted, container or live test. Its class reaches
  ``IntegrationFixture`` or carries the attribute, or its method carries it.
- ``Unit``: a test of a declared class that does none of these.
- ``Unplaced``: the tree declares no such class, and a warning names it. A row
  like this says nothing about the test's speed, and the run does not fail.

With no source tree there is nothing to read, and every row is ``Unit``.
"""

from __future__ import annotations

import argparse
import csv
import functools
import re
import sys
import xml.etree.ElementTree as ET
from pathlib import Path
from typing import Iterable, Iterator, TextIO

TRX_NS = "{http://microsoft.com/schemas/VisualStudio/TeamTest/2010}"
CSV_COLUMNS = ("fully_qualified_name", "category", "duration_ms", "outcome")
INTEGRATION_ROOT_CLASS = "IntegrationFixture"
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
COMMENT_OR_LITERAL_PATTERN = re.compile(
    r"//[^\n]*"
    r"|/\*.*?\*/"
    r"|^[ \t]*#[^\n]*"
    r'|\$*("{3,}).*?\1(?!")'
    r'|(?:\$@|@\$|@)"(?:[^"]|"")*"'
    r'|\$?"(?:\\.|[^"\\\n])*"'
    r"|'(?:\\.|[^'\\\n])+'",
    re.DOTALL | re.MULTILINE,
)
CLASS_SCOPE_TOKEN_PATTERN = re.compile(
    r"\bclass\s+(?P<name>[A-Za-z_]\w*)"
    r"\s*(?:<[^<>]*>)?"
    r"\s*(?:\((?:[^()]|\([^()]*\))*\))?"
    r"(?:\s*:\s*(?:global::)?(?P<base>[A-Za-z_][\w.]*))?"
    r"|[{}]"
)
CONNECTOR_CATEGORIES = (
    "JiraIntegration",
    "LinearIntegration",
    "AdoIntegration",
    "ServiceNowIntegration",
    "GithubIntegration",
)
CONNECTOR_ATTRIBUTE_PATTERN = re.compile(
    r'\bCategory\(\s*"(?:' + "|".join(CONNECTOR_CATEGORIES) + r')"\s*\)'
)
ATTRIBUTES_BEFORE_DECLARATION_PATTERN = re.compile(
    r"(?:\[[^\[\]]*\]\s*)*(?:\w+\s+)*$"
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


def _without_arguments(name: str) -> str:
    paren = name.find("(")
    return name if paren < 0 else name[:paren]


def parse_trx(
    xml_content: str,
    integration_class_names: set[str] | None = None,
    integration_method_fqns: set[str] | None = None,
    declared_class_names: set[str] | None = None,
) -> list[dict[str, object]]:
    """Parse a TRX document into row dicts matching ``CSV_COLUMNS``."""
    integration_classes = integration_class_names or set()
    integration_methods = integration_method_fqns or set()
    root = ET.fromstring(xml_content)
    class_by_id = _class_name_lookup(root)

    rows: list[dict[str, object]] = []
    unplaced_classes: set[str] = set()
    for result in root.iter(f"{TRX_NS}UnitTestResult"):
        test_id = result.get("testId")
        method_name = result.get("testName", "")
        class_name = class_by_id.get(test_id or "", "")
        fqn = (
            f"{class_name}.{method_name}".lstrip(".")
            if method_name
            else class_name
        )
        declared_class = _without_arguments(class_name)
        method_fqn = f"{declared_class}.{_without_arguments(method_name)}".lstrip(".")
        if declared_class in integration_classes or method_fqn in integration_methods:
            category = "Integration"
        elif declared_class_names is None or declared_class in declared_class_names:
            category = "Unit"
        else:
            category = "Unplaced"
            unplaced_classes.add(declared_class)
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
    for class_name in sorted(unplaced_classes):
        print(
            f"warning: no declaration of {class_name} in the source tree, "
            "its tests read Unplaced",
            file=sys.stderr,
        )
    return rows


def discover_integration_class_names(source_root: Path) -> set[str]:
    """Scan a C# source tree for classes in the ``Integration`` category.

    A class qualifies through its own ``[Category("Integration")]`` or through
    a chain of first bases that reaches ``IntegrationFixture``. Names read as
    TRX writes them, with ``+`` between a class and the class nested in it.
    """
    names = {
        f"{namespace}.{class_match.group(1)}"
        for namespace, content in _namespaced_sources(source_root)
        for class_match in INTEGRATION_CATEGORY_AT_CLASS_PATTERN.finditer(content)
    }
    return names | _classes_reaching(
        _base_by_class(source_root), {INTEGRATION_ROOT_CLASS}
    )


def discover_connector_class_names(source_root: Path) -> set[str]:
    """Scan a C# source tree for classes in one of the live connector categories.

    A class qualifies through the attribute written on its own declaration, or
    through a chain of first bases reaching such a class, as NUnit inherits a
    category. A mention in a comment does not count. Names read as TRX writes
    them. A source file that cannot be read raises ``UnreadableSourceError``,
    since a class missed there would be counted as if it were offline.
    """
    for cs_file, content in _cs_sources(source_root):
        if content is None:
            raise UnreadableSourceError(cs_file)
    marked = {
        full_name
        for full_name, _, attributes in _class_records(source_root)
        if CONNECTOR_ATTRIBUTE_PATTERN.search(attributes)
    }
    marked_simple_names = {_simple_name(name) for name in marked}
    return marked | _classes_reaching(_base_by_class(source_root), marked_simple_names)


class UnreadableSourceError(Exception):
    def __init__(self, path: Path) -> None:
        super().__init__(f"cannot read the C# source file {path}")
        self.path = path


def discover_declared_class_names(source_root: Path) -> set[str]:
    """Scan a C# source tree for every class, named as TRX writes it."""
    return set(_base_by_class(source_root))


def simple_name_collisions(source_root: Path) -> dict[str, set[str]]:
    """Return the simple names on a chain to the root held by more than one class.

    Bases are resolved by simple name, so a namesake off the chain can pull its
    own subclasses onto it, and put them on the wrong side of the column without
    anything else noticing.
    """
    base_by_class = _base_by_class(source_root)
    names_on_chains = {INTEGRATION_ROOT_CLASS} | {
        _simple_name(name)
        for name in _classes_reaching(base_by_class, {INTEGRATION_ROOT_CLASS})
    }
    classes_by_simple_name: dict[str, set[str]] = {}
    for name in base_by_class:
        if _simple_name(name) in names_on_chains:
            classes_by_simple_name.setdefault(_simple_name(name), set()).add(name)
    return {
        simple_name: classes
        for simple_name, classes in classes_by_simple_name.items()
        if len(classes) > 1
    }


def _base_by_class(source_root: Path) -> dict[str, str | None]:
    base_by_class: dict[str, str | None] = {}
    for full_name, base, _ in _class_records(source_root):
        # Only one part of a partial class needs to name the base.
        base_by_class[full_name] = base or base_by_class.get(full_name)
    return base_by_class


@functools.cache
def _class_records(source_root: Path) -> list[tuple[str, str | None, str]]:
    return [
        (f"{namespace}.{class_name}", base, attributes)
        for namespace, content in _namespaced_sources(source_root)
        for class_name, base, attributes in _class_declarations(content)
    ]


def _class_declarations(content: str) -> Iterator[tuple[str, str | None, str]]:
    """Yield each class's ``Outer+Inner`` name, its first base's simple name and
    the attributes written on its declaration."""
    code = _blanked(content, keep_literals=False)
    enclosing: list[str | None] = []
    opening_class: str | None = None
    preceding_end = 0
    for token in CLASS_SCOPE_TOKEN_PATTERN.finditer(code):
        text = token.group()
        if text == "{":
            enclosing.append(opening_class)
            opening_class = None
        elif text == "}":
            del enclosing[-1:]
        else:
            opening_class = token.group("name")
            nested_name = "+".join(
                [name for name in enclosing if name] + [opening_class]
            )
            base = token.group("base")
            # Attribute arguments are literals, so only comments are blanked here.
            attributes = ATTRIBUTES_BEFORE_DECLARATION_PATTERN.search(
                _blanked(content[preceding_end : token.start()], keep_literals=True)
            )
            yield (
                nested_name,
                base.rsplit(".", 1)[-1] if base else None,
                attributes.group() if attributes else "",
            )
        preceding_end = token.end()


def _classes_reaching(
    base_by_class: dict[str, str | None], root_simple_names: set[str]
) -> set[str]:
    reached_simple_names = set(root_simple_names)
    reached: set[str] = set()
    while True:
        newly_reached = {
            name
            for name, base in base_by_class.items()
            if base in reached_simple_names
        } - reached
        if not newly_reached:
            return reached
        reached |= newly_reached
        reached_simple_names |= {_simple_name(name) for name in newly_reached}


def _simple_name(full_name: str) -> str:
    return re.split(r"[.+]", full_name)[-1]


def discover_integration_method_fqns(source_root: Path) -> set[str]:
    """Scan a C# source tree for methods carrying ``[Category("Integration")]``.

    Returns the bare ``namespace.class.method`` FQNs — parametrized-test name
    suffixes are excluded so callers can match TRX ``testName`` after stripping
    the ``(...)`` argument tail.
    """
    fqns: set[str] = set()
    for namespace, content in _namespaced_sources(source_root):
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


# Every discovery of a run walks the same tree, and the walk is most of its time.
@functools.cache
def _namespaced_sources(source_root: Path) -> list[tuple[str, str]]:
    return [
        block
        for _, content in _cs_sources(source_root)
        if content is not None
        for block in _namespace_blocks(content)
    ]


BUILD_OR_MUTATION_OUTPUT_FOLDERS = ("bin", "obj", "StrykerOutput")


def is_build_or_mutation_output(folder: str) -> bool:
    return folder in BUILD_OR_MUTATION_OUTPUT_FOLDERS or folder.startswith(".stryker-tmp")


@functools.cache
def _cs_sources(source_root: Path) -> list[tuple[Path, str | None]]:
    sources = []
    for directory, folders, files in source_root.walk():
        folders[:] = sorted(folder for folder in folders if not is_build_or_mutation_output(folder))
        sources += [
            (directory / file, _read_cs_file(directory / file))
            for file in sorted(files)
            if file.endswith(".cs")
        ]
    return sources


def _blanked(content: str, keep_literals: bool) -> str:
    """Blank comments, and literals unless kept, to the same length, so a match's
    offsets still hold in the content."""

    def blank(match: re.Match[str]) -> str:
        if keep_literals and match.group()[0] in "\"$@'":
            return match.group()
        return " " * len(match.group())

    return COMMENT_OR_LITERAL_PATTERN.sub(blank, content)


def _namespace_blocks(content: str) -> Iterator[tuple[str, str]]:
    """Yield each namespace of a file with the text from it to the next one."""
    code = _blanked(content, keep_literals=False)
    starts = list(NAMESPACE_PATTERN.finditer(code))
    ends = [following.start() for following in starts[1:]] + [len(content)]
    for start, end in zip(starts, ends):
        yield start.group(1), content[start.start() : end]


def _read_cs_file(path: Path) -> str | None:
    try:
        return path.read_text(encoding="utf-8-sig")
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
        help="C# source tree root the category column is read from. "
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
    declared_classes: set[str] | None = None
    if args.source_root is not None:
        integration_classes = discover_integration_class_names(args.source_root)
        integration_methods = discover_integration_method_fqns(args.source_root)
        declared_classes = discover_declared_class_names(args.source_root)
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
                declared_class_names=declared_classes,
            )
        )
    args.output.parent.mkdir(parents=True, exist_ok=True)
    with args.output.open("w", encoding="utf-8", newline="") as stream:
        rows_to_csv(rows, stream)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())

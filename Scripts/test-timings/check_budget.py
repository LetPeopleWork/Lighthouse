"""Fail the verify job when a test file (frontend) or class (backend) runs over its time budget.

RED scaffold: the command-line surface is final, the check itself is not written yet.
"""

from __future__ import annotations

import argparse
from pathlib import Path

__SCAFFOLD__ = True

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


def main(argv: list[str] | None = None) -> int:
    _build_arg_parser().parse_args(argv)
    raise AssertionError("Not yet implemented -- RED scaffold")


if __name__ == "__main__":
    raise SystemExit(main())

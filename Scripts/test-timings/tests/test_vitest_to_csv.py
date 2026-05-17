import csv
import io
import sys
import unittest
from pathlib import Path

HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE.parent))

from vitest_to_csv import parse_vitest, rows_to_csv

FIXTURES = HERE / "fixtures"
SAMPLE = (FIXTURES / "sample-vitest.json").read_text(encoding="utf-8")


class ParseVitestTests(unittest.TestCase):
    def test_extracts_one_row_per_assertion(self):
        rows = parse_vitest(SAMPLE)
        self.assertEqual(len(rows), 4)

    def test_row_has_expected_columns(self):
        rows = parse_vitest(SAMPLE)
        self.assertEqual(
            set(rows[0].keys()),
            {"file", "test_name", "duration_ms", "outcome"},
        )

    def test_file_path_made_relative_when_root_given(self):
        rows = parse_vitest(SAMPLE, source_root="/abs/repo/Lighthouse.Frontend")
        files = {row["file"] for row in rows}
        self.assertEqual(
            files,
            {
                "src/components/Foo.test.tsx",
                "src/services/Bar.test.ts",
            },
        )

    def test_file_path_passes_through_when_no_root(self):
        rows = parse_vitest(SAMPLE)
        files = {row["file"] for row in rows}
        self.assertIn(
            "/abs/repo/Lighthouse.Frontend/src/components/Foo.test.tsx", files
        )

    def test_test_name_uses_full_name(self):
        rows = parse_vitest(SAMPLE)
        names = {row["test_name"] for row in rows}
        self.assertIn("Bar service when slow takes ages", names)
        self.assertIn("Foo renders the heading", names)

    def test_duration_propagated_as_float(self):
        rows = parse_vitest(SAMPLE)
        by_name = {row["test_name"]: row for row in rows}
        self.assertAlmostEqual(
            by_name["Bar service when slow takes ages"]["duration_ms"],
            1500.0,
            places=3,
        )

    def test_null_duration_treated_as_zero(self):
        rows = parse_vitest(SAMPLE)
        by_name = {row["test_name"]: row for row in rows}
        self.assertEqual(by_name["top-level pending"]["duration_ms"], 0.0)

    def test_outcome_normalised_to_pascal_case(self):
        rows = parse_vitest(SAMPLE)
        outcomes = {row["test_name"]: row["outcome"] for row in rows}
        self.assertEqual(outcomes["Foo renders the heading"], "Passed")
        self.assertEqual(outcomes["Foo handles click"], "Failed")
        self.assertEqual(outcomes["top-level pending"], "Skipped")


class RowsToCsvTests(unittest.TestCase):
    def test_writes_header_in_declared_order(self):
        buf = io.StringIO()
        rows_to_csv(
            [
                {
                    "file": "src/foo.test.ts",
                    "test_name": "X",
                    "duration_ms": 1.0,
                    "outcome": "Passed",
                }
            ],
            buf,
        )
        first_line = buf.getvalue().splitlines()[0]
        self.assertEqual(first_line, "file,test_name,duration_ms,outcome")

    def test_rows_sorted_by_duration_descending(self):
        buf = io.StringIO()
        rows_to_csv(
            [
                {
                    "file": "a",
                    "test_name": "fast",
                    "duration_ms": 1.0,
                    "outcome": "Passed",
                },
                {
                    "file": "b",
                    "test_name": "slow",
                    "duration_ms": 99.0,
                    "outcome": "Passed",
                },
            ],
            buf,
        )
        reader = csv.DictReader(io.StringIO(buf.getvalue()))
        self.assertEqual(
            [row["test_name"] for row in reader], ["slow", "fast"]
        )


if __name__ == "__main__":
    unittest.main()

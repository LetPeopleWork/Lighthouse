import csv
import io
import unittest
from pathlib import Path

import sys

HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE.parent))

from trx_to_csv import (
    parse_trx,
    discover_integration_class_names,
    discover_integration_method_fqns,
    rows_to_csv,
)

FIXTURES = HERE / "fixtures"


class ParseTrxTests(unittest.TestCase):
    def test_extracts_one_row_per_unit_test_result(self):
        rows = parse_trx((FIXTURES / "sample.trx").read_text(encoding="utf-8"))
        self.assertEqual(len(rows), 8)

    def test_row_has_expected_columns(self):
        rows = parse_trx((FIXTURES / "sample.trx").read_text(encoding="utf-8"))
        row = rows[0]
        self.assertEqual(
            set(row.keys()),
            {"fully_qualified_name", "category", "duration_ms", "outcome"},
        )

    def test_fully_qualified_name_combines_class_and_method(self):
        rows = parse_trx((FIXTURES / "sample.trx").read_text(encoding="utf-8"))
        names = {row["fully_qualified_name"] for row in rows}
        self.assertIn(
            "Lighthouse.Backend.Tests.Foo.FastUnitTestClass.FastUnitTest", names
        )
        self.assertIn(
            "Lighthouse.Backend.Tests.Bar.JiraIntegrationTest.SlowIntegrationTest",
            names,
        )

    def test_duration_parsed_to_milliseconds(self):
        rows = parse_trx((FIXTURES / "sample.trx").read_text(encoding="utf-8"))
        by_test = {row["fully_qualified_name"]: row for row in rows}
        slow = by_test[
            "Lighthouse.Backend.Tests.Bar.JiraIntegrationTest.SlowIntegrationTest"
        ]
        self.assertAlmostEqual(slow["duration_ms"], 2500.0, places=3)
        fast = by_test[
            "Lighthouse.Backend.Tests.Foo.FastUnitTestClass.FastUnitTest"
        ]
        self.assertAlmostEqual(fast["duration_ms"], 5.0, places=3)
        sub_ms = by_test[
            "Lighthouse.Backend.Tests.Foo.FastUnitTestClass.FailingTest"
        ]
        self.assertAlmostEqual(sub_ms["duration_ms"], 123.4567, places=3)

    def test_outcome_passed_through(self):
        rows = parse_trx((FIXTURES / "sample.trx").read_text(encoding="utf-8"))
        outcomes = {
            row["fully_qualified_name"]: row["outcome"] for row in rows
        }
        self.assertEqual(
            outcomes[
                "Lighthouse.Backend.Tests.Foo.FastUnitTestClass.FailingTest"
            ],
            "Failed",
        )
        self.assertEqual(
            outcomes[
                "Lighthouse.Backend.Tests.Foo.FastUnitTestClass.FastUnitTest"
            ],
            "Passed",
        )

    def test_category_defaults_to_unit_when_no_classification(self):
        rows = parse_trx((FIXTURES / "sample.trx").read_text(encoding="utf-8"))
        categories = {row["category"] for row in rows}
        self.assertEqual(categories, {"Unit"})

    def test_category_marked_integration_when_class_classified(self):
        rows = parse_trx(
            (FIXTURES / "sample.trx").read_text(encoding="utf-8"),
            integration_class_names={
                "Lighthouse.Backend.Tests.Bar.JiraIntegrationTest"
            },
        )
        by_class = {row["fully_qualified_name"]: row for row in rows}
        self.assertEqual(
            by_class[
                "Lighthouse.Backend.Tests.Bar.JiraIntegrationTest.SlowIntegrationTest"
            ]["category"],
            "Integration",
        )
        self.assertEqual(
            by_class[
                "Lighthouse.Backend.Tests.Foo.FastUnitTestClass.FastUnitTest"
            ]["category"],
            "Unit",
        )

    def test_category_marked_integration_when_method_classified(self):
        rows = parse_trx(
            (FIXTURES / "sample.trx").read_text(encoding="utf-8"),
            integration_method_fqns={
                "Lighthouse.Backend.Tests.Mixed.MixedTest.IntegrationMethod",
                "Lighthouse.Backend.Tests.Mixed.MixedTest.IntegrationParametrized",
            },
        )
        by_name = {row["fully_qualified_name"]: row for row in rows}
        self.assertEqual(
            by_name[
                "Lighthouse.Backend.Tests.Mixed.MixedTest.UnitOnlyMethod"
            ]["category"],
            "Unit",
        )
        self.assertEqual(
            by_name[
                "Lighthouse.Backend.Tests.Mixed.MixedTest.IntegrationMethod"
            ]["category"],
            "Integration",
        )

    def test_method_classification_matches_parametrized_test_names(self):
        rows = parse_trx(
            (FIXTURES / "sample.trx").read_text(encoding="utf-8"),
            integration_method_fqns={
                "Lighthouse.Backend.Tests.Mixed.MixedTest.IntegrationParametrized",
            },
        )
        parametrized = [
            row
            for row in rows
            if row["fully_qualified_name"].startswith(
                'Lighthouse.Backend.Tests.Mixed.MixedTest.IntegrationParametrized('
            )
        ]
        self.assertEqual(len(parametrized), 2)
        for row in parametrized:
            self.assertEqual(row["category"], "Integration")


class DiscoverIntegrationClassNamesTests(unittest.TestCase):
    def test_detects_class_level_integration_attribute(self):
        names = discover_integration_class_names(FIXTURES)
        self.assertIn("Lighthouse.Backend.Tests.Bar.JiraIntegrationTest", names)

    def test_ignores_classes_without_integration_attribute(self):
        names = discover_integration_class_names(FIXTURES)
        self.assertNotIn(
            "Lighthouse.Backend.Tests.Foo.FastUnitTestClass", names
        )

    def test_does_not_treat_method_level_decoration_as_class_level(self):
        names = discover_integration_class_names(FIXTURES)
        self.assertNotIn("Lighthouse.Backend.Tests.Mixed.MixedTest", names)


class DiscoverIntegrationMethodFqnsTests(unittest.TestCase):
    def test_collects_method_decorated_with_integration_attribute(self):
        fqns = discover_integration_method_fqns(FIXTURES)
        self.assertIn(
            "Lighthouse.Backend.Tests.Mixed.MixedTest.IntegrationMethod", fqns
        )
        self.assertIn(
            "Lighthouse.Backend.Tests.Mixed.MixedTest.IntegrationParametrized",
            fqns,
        )

    def test_ignores_methods_without_integration_attribute(self):
        fqns = discover_integration_method_fqns(FIXTURES)
        self.assertNotIn(
            "Lighthouse.Backend.Tests.Mixed.MixedTest.UnitOnlyMethod", fqns
        )


class RowsToCsvTests(unittest.TestCase):
    def test_writes_header_in_declared_order(self):
        buf = io.StringIO()
        rows_to_csv(
            [
                {
                    "fully_qualified_name": "X.Y",
                    "category": "Unit",
                    "duration_ms": 1.5,
                    "outcome": "Passed",
                }
            ],
            buf,
        )
        first_line = buf.getvalue().splitlines()[0]
        self.assertEqual(
            first_line, "fully_qualified_name,category,duration_ms,outcome"
        )

    def test_rows_sorted_by_duration_descending(self):
        buf = io.StringIO()
        rows_to_csv(
            [
                {
                    "fully_qualified_name": "A",
                    "category": "Unit",
                    "duration_ms": 1.0,
                    "outcome": "Passed",
                },
                {
                    "fully_qualified_name": "B",
                    "category": "Unit",
                    "duration_ms": 9.0,
                    "outcome": "Passed",
                },
            ],
            buf,
        )
        reader = csv.DictReader(io.StringIO(buf.getvalue()))
        ordered = [row["fully_qualified_name"] for row in reader]
        self.assertEqual(ordered, ["B", "A"])

    def test_duration_formatted_with_three_decimals(self):
        buf = io.StringIO()
        rows_to_csv(
            [
                {
                    "fully_qualified_name": "X",
                    "category": "Unit",
                    "duration_ms": 1.23456789,
                    "outcome": "Passed",
                }
            ],
            buf,
        )
        reader = csv.DictReader(io.StringIO(buf.getvalue()))
        first = next(reader)
        self.assertEqual(first["duration_ms"], "1.235")


if __name__ == "__main__":
    unittest.main()

import csv
import io
import tempfile
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

TEST_NAMESPACE = "Lighthouse.Backend.Tests.Shapes"
OFF_THE_BASE_CLASS = f"{TEST_NAMESPACE}.OffTheBaseTest"
SHARED_BASES = """
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.Shapes
{
    [Category("Integration")]
    public abstract class IntegrationFixture
    {
    }

    public abstract class IntegrationTestBase : IntegrationFixture
    {
        protected IntegrationTestBase(string hostName = "default")
        {
        }
    }

    public abstract class RegularBase
    {
    }

    public class OffTheBaseTest : RegularBase
    {
    }
}
"""


def in_test_namespace(source):
    return f"namespace {TEST_NAMESPACE}\n{{\n{source}\n}}\n"


def trx_listing(class_names):
    definitions = "".join(
        f'<UnitTest id="t{index}">'
        f'<TestMethod className="{class_name}" name="Runs" /></UnitTest>'
        for index, class_name in enumerate(class_names)
    )
    results = "".join(
        f'<UnitTestResult testId="t{index}" testName="Runs" '
        'duration="00:00:00.0010000" outcome="Passed" />'
        for index in range(len(class_names))
    )
    return (
        '<TestRun xmlns="http://microsoft.com/schemas/VisualStudio/TeamTest/2010">'
        f"<Results>{results}</Results>"
        f"<TestDefinitions>{definitions}</TestDefinitions></TestRun>"
    )


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


class ATestClassOnTheBaseReadsIntegrationTests(unittest.TestCase):
    def assert_reads_integration(self, class_name, *sources):
        categories = self.categories_of(
            [f"{TEST_NAMESPACE}.{class_name}", OFF_THE_BASE_CLASS], sources
        )
        self.assertEqual(
            categories,
            {
                f"{TEST_NAMESPACE}.{class_name}": "Integration",
                OFF_THE_BASE_CLASS: "Unit",
            },
        )

    def categories_of(self, class_names, sources):
        with tempfile.TemporaryDirectory() as directory:
            source_root = Path(directory)
            (source_root / "SharedBases.cs").write_text(
                SHARED_BASES, encoding="utf-8"
            )
            for index, source in enumerate(sources):
                (source_root / f"Shape{index}.cs").write_text(
                    in_test_namespace(source), encoding="utf-8"
                )
            rows = parse_trx(
                trx_listing(class_names),
                integration_class_names=discover_integration_class_names(
                    source_root
                ),
            )
        return {
            str(row["fully_qualified_name"]).removesuffix(".Runs"): row[
                "category"
            ]
            for row in rows
        }

    def test_primary_constructor(self):
        self.assert_reads_integration(
            "PrimaryConstructorTest",
            "public class PrimaryConstructorTest() : IntegrationTestBase { }",
        )

    def test_primary_constructor_with_parameters(self):
        self.assert_reads_integration(
            "PrimaryConstructorParametersTest",
            """
            public class PrimaryConstructorParametersTest(
                string first, int second = (1 + 2)) : IntegrationTestBase
            {
            }
            """,
        )

    def test_base_with_arguments(self):
        self.assert_reads_integration(
            "BaseArgumentsTest",
            'public class BaseArgumentsTest() : IntegrationTestBase("other") { }',
        )

    def test_base_on_the_next_line(self):
        self.assert_reads_integration(
            "NextLineBaseTest",
            """
            public class NextLineBaseTest
                : IntegrationTestBase
            {
            }
            """,
        )

    def test_interface_after_the_base(self):
        self.assert_reads_integration(
            "InterfaceAfterBaseTest",
            "public class InterfaceAfterBaseTest : IntegrationTestBase, "
            "IDisposable { }",
        )

    def test_qualified_base(self):
        self.assert_reads_integration(
            "QualifiedBaseTest",
            "public class QualifiedBaseTest : "
            "Lighthouse.Backend.Tests.Shapes.IntegrationTestBase { }",
        )

    def test_base_named_in_one_part_of_a_partial_class(self):
        self.assert_reads_integration(
            "PartialTest",
            "public partial class PartialTest { }",
            "public partial class PartialTest : IntegrationTestBase { }",
            "public partial class PartialTest { }",
        )

    def test_nested_class(self):
        self.assert_reads_integration(
            "Outer+Inner",
            """
            public class Outer
            {
                private const string ClosingBrace = "}";

                public class Inner : IntegrationTestBase
                {
                }
            }
            """,
        )

    def test_private_nested_class(self):
        self.assert_reads_integration(
            "Outer+Inner",
            """
            public class Outer
            {
                // A closing brace in a comment } closes no scope.
                private sealed class Inner : IntegrationTestBase
                {
                }
            }
            """,
        )

    def test_a_class_after_a_nested_class_is_not_nested_in_it(self):
        self.assert_reads_integration(
            "AfterOuterTest",
            """
            public class Outer
            {
                public class Inner
                {
                }
            }

            public class AfterOuterTest : IntegrationTestBase
            {
            }
            """,
        )

    def test_a_declaration_in_a_comment_or_a_string_counts_for_nothing(self):
        categories = self.categories_of(
            [
                f"{TEST_NAMESPACE}.CommentedOutTest",
                f"{TEST_NAMESPACE}.QuotedTest",
            ],
            [
                """
                // public class CommentedOutTest : IntegrationTestBase { }
                public class Holder
                {
                    private const string Quoted =
                        "public class QuotedTest : IntegrationTestBase { }";
                }
                """
            ],
        )
        self.assertEqual(set(categories.values()), {"Unit"})


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

import contextlib
import csv
import io
import tempfile
import unittest
from pathlib import Path

import sys

HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE.parent))

from trx_to_csv import (
    discover_declared_class_names,
    discover_integration_class_names,
    discover_integration_method_fqns,
    main,
    parse_trx,
    rows_to_csv,
    simple_name_collisions,
)

FIXTURES = HERE / "fixtures"
REAL_SOURCE_ROOT = (
    HERE.parents[1].parent / "Lighthouse.Backend" / "Lighthouse.Backend.Tests"
)

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
    return trx_of_tests([(class_name, "Runs") for class_name in class_names])


def trx_of_tests(tests):
    definitions = "".join(
        f'<UnitTest id="t{index}">'
        f'<TestMethod className="{class_name}" name="{method_name}" /></UnitTest>'
        for index, (class_name, method_name) in enumerate(tests)
    )
    results = "".join(
        f'<UnitTestResult testId="t{index}" testName="{method_name}" '
        'duration="00:00:00.0010000" outcome="Passed" />'
        for index, (_, method_name) in enumerate(tests)
    )
    return (
        '<TestRun xmlns="http://microsoft.com/schemas/VisualStudio/TeamTest/2010">'
        f"<Results>{results}</Results>"
        f"<TestDefinitions>{definitions}</TestDefinitions></TestRun>"
    )


def write_tree(directory, sources):
    source_root = Path(directory)
    for file_name, source in sources.items():
        (source_root / file_name).write_text(source, encoding="utf-8")
    return source_root


def categories_read_from(source_root, tests):
    rows = parse_trx(
        trx_of_tests(tests),
        integration_class_names=discover_integration_class_names(source_root),
        integration_method_fqns=discover_integration_method_fqns(source_root),
        declared_class_names=discover_declared_class_names(source_root),
    )
    return {str(row["fully_qualified_name"]): row["category"] for row in rows}


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


class AParametrisedFixtureTests(unittest.TestCase):
    # These rows are copied from a real hosted run. The fixture's arguments
    # sit on className alone: testName and the method's name carry none.
    NAMESPACE = "Lighthouse.Backend.Tests.API.Integration"
    STRING_ARGUMENT_CLASS = (
        f"{NAMESPACE}.CategoryListsHoldRawStatesSettingsIntegrationTest"
    )
    NUMBER_ARGUMENT_CLASS = (
        f"{NAMESPACE}.FastWithManyFeatures"
        ".Slice01ForecastRoundsRunOnTheirOwnThreadsScenarios"
    )
    STRING_ARGUMENT_METHOD = "SavingAGroupOverToDoStates_ShowsItInToDo"
    NUMBER_ARGUMENT_METHOD = (
        "A_background_round_simulates_on_Lighthouses_own_forecast_threads"
        "_never_on_the_threads_that_answer_pages"
    )
    REAL_TRX = (
        '<TestRun xmlns="http://microsoft.com/schemas/VisualStudio/TeamTest/2010">'
        "<Results>"
        '<UnitTestResult testId="74d4f33f-7011-2800-84a6-20499907d6a5" '
        f'testName="{STRING_ARGUMENT_METHOD}" '
        'duration="00:00:00.1113060" outcome="Passed" />'
        '<UnitTestResult testId="087264ed-864c-d6bb-867c-693424e1de53" '
        f'testName="{NUMBER_ARGUMENT_METHOD}" '
        'duration="00:00:00.3950710" outcome="Passed" />'
        "</Results><TestDefinitions>"
        '<UnitTest id="74d4f33f-7011-2800-84a6-20499907d6a5">'
        f'<TestMethod className="{STRING_ARGUMENT_CLASS}(Portfolio)" '
        f'name="{STRING_ARGUMENT_METHOD}" /></UnitTest>'
        '<UnitTest id="087264ed-864c-d6bb-867c-693424e1de53">'
        f'<TestMethod className="{NUMBER_ARGUMENT_CLASS}(2)" '
        f'name="{NUMBER_ARGUMENT_METHOD}" /></UnitTest>'
        "</TestDefinitions></TestRun>"
    )

    def test_the_name_in_the_row_keeps_the_arguments_as_trx_wrote_them(self):
        names = [row["fully_qualified_name"] for row in parse_trx(self.REAL_TRX)]
        self.assertEqual(
            names,
            [
                f"{self.STRING_ARGUMENT_CLASS}(Portfolio)"
                f".{self.STRING_ARGUMENT_METHOD}",
                f"{self.NUMBER_ARGUMENT_CLASS}(2).{self.NUMBER_ARGUMENT_METHOD}",
            ],
        )

    def test_the_arguments_do_not_hide_a_class_on_the_base(self):
        rows = parse_trx(
            self.REAL_TRX,
            integration_class_names={
                self.STRING_ARGUMENT_CLASS,
                self.NUMBER_ARGUMENT_CLASS,
            },
        )
        self.assertEqual(
            [row["category"] for row in rows], ["Integration", "Integration"]
        )

    def test_the_arguments_do_not_hide_a_method_in_the_category(self):
        rows = parse_trx(
            self.REAL_TRX,
            integration_method_fqns={
                f"{self.NUMBER_ARGUMENT_CLASS}.{self.NUMBER_ARGUMENT_METHOD}"
            },
        )
        self.assertEqual(
            [row["category"] for row in rows], ["Unit", "Integration"]
        )


class NamespacesAndChainsTests(unittest.TestCase):
    def test_each_class_takes_the_namespace_it_sits_in(self):
        header = "// A licence header, as long as they come. " * 5
        source = header + """
namespace Shapes.First
{
    public class FirstOnTheBase : IntegrationFixture
    {
    }
}

namespace Shapes.Second
{
    public class SecondOnTheBase : IntegrationFixture
    {
    }

    public class SecondOffTheBase
    {
    }
}
"""
        with tempfile.TemporaryDirectory() as directory:
            source_root = write_tree(directory, {"TwoNamespaces.cs": source})
            declared = discover_declared_class_names(source_root)
            categories = categories_read_from(
                source_root,
                [
                    ("Shapes.First.FirstOnTheBase", "Runs"),
                    ("Shapes.Second.SecondOnTheBase", "Runs"),
                    ("Shapes.Second.SecondOffTheBase", "Runs"),
                ],
            )
        self.assertEqual(
            categories,
            {
                "Shapes.First.FirstOnTheBase.Runs": "Integration",
                "Shapes.Second.SecondOnTheBase.Runs": "Integration",
                "Shapes.Second.SecondOffTheBase.Runs": "Unit",
            },
        )
        self.assertEqual(
            declared,
            {
                "Shapes.First.FirstOnTheBase",
                "Shapes.Second.SecondOnTheBase",
                "Shapes.Second.SecondOffTheBase",
            },
        )

    def test_a_namespace_inside_a_string_opens_nothing(self):
        source = """
namespace Shapes.Real
{
    public class HoldsSourceTest
    {
        private const string Planted = @"
namespace Shapes.Planted
{
";

        public class AfterTheString : IntegrationFixture
        {
        }
    }
}
"""
        with tempfile.TemporaryDirectory() as directory:
            names = discover_integration_class_names(
                write_tree(directory, {"HoldsSource.cs": source})
            )
        self.assertEqual(names, {"Shapes.Real.HoldsSourceTest+AfterTheString"})

    def test_a_chain_crossing_files_and_namespaces_reaches_the_base(self):
        sources = {
            "Root.cs": "namespace Shapes.Roots;\n"
            "public abstract class Harness : IntegrationFixture { }\n",
            "Middle.cs": "namespace Shapes.Middles;\n"
            "public abstract class Middle : Shapes.Roots.Harness { }\n",
            "Leaf.cs": "namespace Shapes.Leaves;\n"
            "public class Leaf : Middle { }\n",
        }
        with tempfile.TemporaryDirectory() as directory:
            categories = categories_read_from(
                write_tree(directory, sources), [("Shapes.Leaves.Leaf", "Runs")]
            )
        self.assertEqual(categories, {"Shapes.Leaves.Leaf.Runs": "Integration"})


class AClassTheScriptCannotPlaceTests(unittest.TestCase):
    DECLARED = "namespace Shapes;\npublic class Declared { }\n"
    UNDECLARED_CLASS = "Shapes.NoDeclarationAnywhere"

    def run_the_script(self, directory):
        source_root = write_tree(directory, {"Declared.cs": self.DECLARED})
        trx_path = source_root / "run.trx"
        trx_path.write_text(
            trx_of_tests(
                [
                    ("Shapes.Declared", "Runs"),
                    (self.UNDECLARED_CLASS, "First"),
                    (self.UNDECLARED_CLASS, "Second"),
                ]
            ),
            encoding="utf-8",
        )
        output_path = source_root / "out.csv"
        warnings = io.StringIO()
        with contextlib.redirect_stderr(warnings):
            exit_code = main(
                [
                    "--trx",
                    str(trx_path),
                    "--source-root",
                    str(source_root),
                    "--output",
                    str(output_path),
                ]
            )
        with output_path.open(encoding="utf-8", newline="") as stream:
            categories = {
                row["fully_qualified_name"]: row["category"]
                for row in csv.DictReader(stream)
            }
        return exit_code, categories, warnings.getvalue().splitlines()

    def test_its_rows_are_not_counted_as_fast_and_the_run_still_succeeds(self):
        with tempfile.TemporaryDirectory() as directory:
            exit_code, categories, _ = self.run_the_script(directory)
        self.assertEqual(exit_code, 0)
        self.assertEqual(
            categories,
            {
                "Shapes.Declared.Runs": "Unit",
                f"{self.UNDECLARED_CLASS}.First": "Unplaced",
                f"{self.UNDECLARED_CLASS}.Second": "Unplaced",
            },
        )

    def test_it_is_named_in_one_warning(self):
        with tempfile.TemporaryDirectory() as directory:
            _, _, warnings = self.run_the_script(directory)
        self.assertEqual(len(warnings), 1)
        self.assertIn(self.UNDECLARED_CLASS, warnings[0])

    def test_a_parametrised_fixture_is_found_under_its_own_declaration(self):
        with tempfile.TemporaryDirectory() as directory:
            categories = categories_read_from(
                write_tree(directory, {"Declared.cs": self.DECLARED}),
                [("Shapes.Declared(Team)", "Runs")],
            )
        self.assertEqual(categories, {"Shapes.Declared(Team).Runs": "Unit"})


class AMixedClassTests(unittest.TestCase):
    SOURCE = """
namespace Shapes
{
    [Category("JiraIntegration")]
    public class MixedTest
    {
        [Test]
        [Category("Integration")]
        public void Marked()
        {
        }

        [Test]
        public void Unmarked()
        {
        }
    }
}
"""

    def test_only_the_marked_method_reads_integration(self):
        with tempfile.TemporaryDirectory() as directory:
            categories = categories_read_from(
                write_tree(directory, {"Mixed.cs": self.SOURCE}),
                [
                    ("Shapes.MixedTest", "Marked"),
                    ("Shapes.MixedTest", "Unmarked"),
                    ("Shapes.MixedTest(Team)", "Marked"),
                    ("Shapes.MixedTest(Team)", "Unmarked"),
                ],
            )
        self.assertEqual(
            categories,
            {
                "Shapes.MixedTest.Marked": "Integration",
                "Shapes.MixedTest.Unmarked": "Unit",
                "Shapes.MixedTest(Team).Marked": "Integration",
                "Shapes.MixedTest(Team).Unmarked": "Unit",
            },
        )


class SimpleNameCollisionTests(unittest.TestCase):
    def collisions_in(self, sources):
        with tempfile.TemporaryDirectory() as directory:
            return simple_name_collisions(write_tree(directory, sources))

    def test_two_classes_on_a_chain_sharing_a_simple_name_are_reported(self):
        collisions = self.collisions_in(
            {
                "One.cs": "namespace Shapes.One;\n"
                "public abstract class Shared : IntegrationFixture { }\n",
                "Two.cs": "namespace Shapes.Two;\n"
                "public class Outer { public class Shared : IntegrationFixture { } }\n",
            }
        )
        self.assertEqual(
            collisions,
            {"Shared": {"Shapes.One.Shared", "Shapes.Two.Outer+Shared"}},
        )

    def test_the_parts_of_a_partial_class_are_one_class(self):
        self.assertEqual(
            self.collisions_in(
                {
                    "One.cs": "namespace Shapes;\n"
                    "public partial class Split : IntegrationFixture { }\n",
                    "Two.cs": "namespace Shapes;\n"
                    "public partial class Split { }\n",
                }
            ),
            {},
        )

    def test_a_namesake_off_the_chain_is_no_collision(self):
        self.assertEqual(
            self.collisions_in(
                {
                    "One.cs": "namespace Shapes.One;\n"
                    "public class Shared : IntegrationFixture { }\n",
                    "Two.cs": "namespace Shapes.Two;\n"
                    "public class Shared { }\n",
                }
            ),
            {},
        )


class TheRealTreeTests(unittest.TestCase):
    TESTS = "Lighthouse.Backend.Tests"
    ANCHORS = {
        "on IntegrationTestBase": (
            f"{TESTS}.API.Integration.TeamDeletionIntegrationTest",
            "Integration",
        ),
        "through a three-deep abstract chain": (
            f"{TESTS}.API.Integration.TaskManager.Slice04StopARefreshTest",
            "Integration",
        ),
        "self-hosting": (
            f"{TESTS}.API.Integration.SystemInfoWireFormatRegressionTests",
            "Integration",
        ),
        "container": (
            f"{TESTS}.Integration.Containers.SubstrateProbeTests",
            "Integration",
        ),
        "live": (
            f"{TESTS}.Services.Implementation.WorkTrackingConnectors.Jira"
            ".JiraOriginalNamesLiveTest",
            "Integration",
        ),
        "parametrised": (
            f"{TESTS}.API.Integration"
            ".CycleTimeDefinitionOverAMappingIntegrationTest(Team)",
            "Integration",
        ),
        "regular": (f"{TESTS}.Models.AdditionalFieldDefinitionTest", "Unit"),
    }

    def test_the_anchors_read_as_they_must(self):
        categories = categories_read_from(
            REAL_SOURCE_ROOT,
            [(class_name, "Runs") for class_name, _ in self.ANCHORS.values()],
        )
        self.assertEqual(
            {
                anchor: categories[f"{class_name}.Runs"]
                for anchor, (class_name, _) in self.ANCHORS.items()
            },
            {anchor: category for anchor, (_, category) in self.ANCHORS.items()},
        )

    def test_no_two_classes_on_a_chain_share_a_simple_name(self):
        self.assertEqual(simple_name_collisions(REAL_SOURCE_ROOT), {})


if __name__ == "__main__":
    unittest.main()

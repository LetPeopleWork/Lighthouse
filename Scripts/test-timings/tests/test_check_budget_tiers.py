"""How many tests each tier holds, how long they took, and the report each run prints.

Every backend case starts from one steady run and changes only what it names: 100 fast tests
at 0.01 s, 20 integration tests at 0.4 s, 3 tests that did not run, and 4 tests in a class
that talks to a real tracker, one of them 30 s. The check is run as CI runs it, as a process.
"""

import json
import os
import re
import sys
import tempfile
import unittest
from pathlib import Path
from unittest import mock

HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))
sys.path.insert(0, str(HERE.parent))

from test_check_budget import (  # noqa: E402
    BACKEND_COLUMNS,
    FRONTEND_COLUMNS,
    SUMMARY_VARIABLE,
    TESTS_NAMESPACE,
    BudgetCheckTestCase,
    CheckWorkspace,
    allowed,
)

import test_check_budget_time_budgets as time_budgets  # noqa: E402

import check_budget  # noqa: E402

REPOSITORY = HERE.parents[2]
FRONTEND_PACKAGE = REPOSITORY / "Lighthouse.Frontend" / "package.json"

PLANTED_BUDGET_SECONDS = 5000
FAST_CLASS = f"{TESTS_NAMESPACE}.PricingTest"
HOSTED_CLASS = f"{TESTS_NAMESPACE}.PortfolioEndpointTest"
CONNECTOR_CLASS = f"{TESTS_NAMESPACE}.JiraConnectorTest"
CONNECTOR_SOURCE = (
    f"namespace {TESTS_NAMESPACE};\n\n"
    '[Category("JiraIntegration")]\n'
    "public class JiraConnectorTest\n{\n}\n"
)


def row(name, category, seconds, outcome="Passed"):
    return (name, category, f"{seconds * 1000:.3f}", outcome)


def steady_rows(integration_seconds=0.4):
    return (
        [row(f"{FAST_CLASS}.Prices{index}", "Unit", 0.01) for index in range(100)]
        + [
            row(f"{HOSTED_CLASS}.Serves{index}", "Integration", integration_seconds)
            for index in range(20)
        ]
        + [row(f"{FAST_CLASS}.Waits{index}", "Unit", 0, "NotExecuted") for index in range(3)]
        + [row(f"{CONNECTOR_CLASS}.ReadsALiveTracker", "Integration", 30)]
        + [row(f"{CONNECTOR_CLASS}.Reads{index}", "Integration", 0.1) for index in range(3)]
    )


class TierWorkspace(CheckWorkspace):
    """A workspace that writes any category and outcome, and plants the connector class."""

    def steady_run(self, extra_rows=(), integration_seconds=0.4):
        self.plant_connector_class()
        return self.backend_rows([*steady_rows(integration_seconds), *extra_rows])

    def plant_connector_class(self):
        self.write_test_source("JiraConnectorTest.cs", CONNECTOR_SOURCE)

    def backend_rows(self, rows):
        return self._write_csv("test-timings-backend.csv", BACKEND_COLUMNS, rows)

    def frontend_rows(self, rows):
        return self._write_csv("test-timings-frontend.csv", FRONTEND_COLUMNS, rows)

    def planted_budget(self, backend_entries=()):
        return self.budget(
            backend_entries=backend_entries,
            frontend_budget=PLANTED_BUDGET_SECONDS,
            backend_budget=PLANTED_BUDGET_SECONDS,
        )


REPORT_LINE = re.compile(
    r"^(?P<tier>Fast|Integration|Left out|Not run)\s+(?P<tests>[\d,]+)"
    r"(?:\s+(?P<seconds>[\d,]+\.\d)\s+(?P<mean>\d+\.\d{3}))?"
)


class TierReportTestCase(BudgetCheckTestCase):
    def fresh_workspace(self):
        folder = tempfile.TemporaryDirectory()
        self.addCleanup(folder.cleanup)
        return TierWorkspace(Path(folder.name))

    def report_of(self, result, title):
        lines = result.stdout.splitlines()
        self.assertIn(title, lines, msg=self._explain(result))
        start = lines.index(title)
        end = next(
            (
                index
                for index in range(start, len(lines))
                if lines[index].startswith("This stack only")
            ),
            None,
        )
        self.assertIsNotNone(end, msg=self._explain(result))
        return "\n".join(lines[start : end + 1])

    def figures_of(self, report):
        figures = {}
        for line in report.splitlines():
            match = REPORT_LINE.match(line)
            if match:
                figures[match["tier"]] = (
                    int(match["tests"].replace(",", "")),
                    match["seconds"],
                    match["mean"],
                )
        return figures

    def check_steady_backend(self, extra_rows=(), integration_seconds=0.4):
        timings = self.workspace.steady_run(extra_rows, integration_seconds)
        self.workspace.planted_budget()
        return self.workspace.check("backend", timings)


class EachRunReportsItsTiers(TierReportTestCase):
    def test_each_run_shows_how_many_tests_each_tier_has_how_long_they_took_and_how_much_room_is_left(self):
        result = self.check_steady_backend()

        self.assert_passes_quietly(result)
        self.assertEqual(
            self.report_of(result, "Backend test time"),
            "Backend test time\n"
            "Tier          Tests   Seconds   Mean s   Ceiling                         Room\n"
            "Fast            100       1.0    0.010   per test not set\n"
            "Integration      20       8.0    0.400   mean not set\n"
            "                                         total not set\n"
            "Left out          4   connector classes, not judged\n"
            "Not run           3   did not pass, not counted\n"
            "This stack only, not the pyramid shares: 83.3 % fast, 16.7 % integration. No gate.",
        )

    def test_the_frontend_shows_its_two_tiers_and_says_its_seconds_do_not_compare(self):
        timings = self.workspace.frontend_rows(
            [
                ("src/a.test.ts", "adds", "500.000", "Passed"),
                ("src/a.test.ts", "subtracts", "300.000", "Passed"),
                ("src/b.test.tsx", "renders", "1500.000", "Passed"),
                ("src/a.test.ts", "divides", "0.000", "Skipped"),
                ("src/b.test.tsx", "scrolls", "0.000", "Skipped"),
            ]
        )
        self.workspace.planted_budget()

        result = self.workspace.check("frontend", timings)

        self.assert_passes_quietly(result)
        self.assertEqual(
            self.report_of(result, "Frontend test time"),
            "Frontend test time\n"
            "Tier          Tests   Seconds   Mean s\n"
            "Fast              2       0.8    0.400\n"
            "Integration       1       1.5    1.500\n"
            "Not run           2   did not pass, not counted\n"
            "The seconds do not compare across runs: the runner sets them.\n"
            "This stack only, not the pyramid shares: 66.7 % fast, 33.3 % integration. No gate.",
        )


class OnlyPassedTestsOutsideConnectorClassesCount(TierReportTestCase):
    def test_only_tests_that_passed_outside_connector_classes_count(self):
        kinds = {
            "Failed": lambda index: row(f"{HOSTED_CLASS}.Breaks{index}", "Integration", 40, "Failed"),
            "NotExecuted": lambda index: row(f"{FAST_CLASS}.Skips{index}", "Unit", 40, "NotExecuted"),
            "connector": lambda index: row(f"{CONNECTOR_CLASS}.Syncs{index}", "Unit", 40),
        }
        for kind, make in kinds.items():
            for how_many in (0, 1, 7):
                with self.subTest(kind=kind, how_many=how_many):
                    self.workspace = self.fresh_workspace()
                    extra = [make(index) for index in range(how_many)]

                    figures = self.figures_of(
                        self.report_of(self.check_steady_backend(extra), "Backend test time")
                    )

                    self.assertEqual(figures["Fast"], (100, "1.0", "0.010"))
                    self.assertEqual(figures["Integration"], (20, "8.0", "0.400"))
                    self.assertEqual(
                        figures["Left out"][0], 4 + (how_many if kind == "connector" else 0)
                    )
                    self.assertEqual(
                        figures["Not run"][0], 3 + (how_many if kind != "connector" else 0)
                    )
                    self.assert_rows_add_up(figures, rows=127 + how_many)

    def test_a_test_that_did_not_run_in_a_connector_class_is_left_out_once(self):
        extra = [row(f"{CONNECTOR_CLASS}.Skipped", "Integration", 40, "NotExecuted")]

        figures = self.figures_of(
            self.report_of(self.check_steady_backend(extra), "Backend test time")
        )

        self.assertEqual(figures["Left out"][0], 5)
        self.assertEqual(figures["Not run"][0], 3)
        self.assert_rows_add_up(figures, rows=128)

    def assert_rows_add_up(self, figures, rows):
        counted = sum(figures[tier][0] for tier in ("Fast", "Integration", "Left out", "Not run"))
        self.assertEqual(counted, rows)


class WithNoLimitWrittenTheCheckOnlyReports(TierReportTestCase):
    def test_with_no_limit_written_the_check_only_reports(self):
        self.workspace.declare_test_class("PortfolioEndpointTest")
        timings = self.workspace.steady_run(integration_seconds=5)
        reports = []
        for entries in ([allowed(HOSTED_CLASS, 6000)], []):
            self.workspace.planted_budget(backend_entries=entries)

            result = self.workspace.check("backend", timings)

            self.assert_passes_quietly(result)
            reports.append(self.report_of(result, "Backend test time"))

        self.assertIn("Integration      20     100.0    5.000   mean not set", reports[0])
        self.assertIn("total not set", reports[0])
        self.assertEqual(reports[0], reports[1])

    def test_a_run_with_nothing_labelled_says_so(self):
        timings = self.workspace.backend_rows(
            [row(f"{FAST_CLASS}.Prices{index}", "Unit", 0.01) for index in range(5)]
        )
        self.workspace.planted_budget()

        result = self.workspace.check("backend", timings)

        self.assert_passes_quietly(result)
        self.assertNotIn("Traceback", result.stderr)
        report = self.report_of(result, "Backend test time")
        integration = [line for line in report.splitlines() if line.startswith("Integration")]
        self.assertEqual(len(integration), 1, msg=report)
        self.assertIn("nothing labelled yet", integration[0])


def excludes_component_tests(package_json_text):
    """Whether the fast command leaves out the files the report counts as integration tests."""
    fast_command = json.loads(package_json_text)["scripts"]["test:fast"]
    return f'--exclude "**/*{check_budget.COMPONENT_TEST_SUFFIX}"' in fast_command


class TheFrontendTiersFollowTheFastCommand(unittest.TestCase):
    def test_the_frontend_tiers_follow_the_same_rule_as_the_fast_command(self):
        real = FRONTEND_PACKAGE.read_text(encoding="utf-8")
        planted = re.sub(r'\s*--exclude \\"[^"\\]*\\"', "", real)

        self.assertTrue(excludes_component_tests(real))
        self.assertNotEqual(planted, real)
        self.assertFalse(
            excludes_component_tests(planted),
            msg="a fast command that runs component tests must not pass as the fast tier",
        )


class SourceThatCannotBeReadGivesNoVerdict(TierReportTestCase):
    def test_a_source_file_that_cannot_be_read_gives_no_verdict_and_names_the_file(self):
        timings = self.workspace.steady_run()
        self.workspace.planted_budget()
        unreadable = self.workspace.source_root / "Broken.cs"
        unreadable.write_bytes(b"namespace Broken;\n\xff\xfe\xfa public class BrokenTest {}\n")

        result = self.workspace.check("backend", timings)

        self.assert_refuses_to_judge(result, "Broken.cs")
        self.assertNotIn("Backend test time", result.stdout)

    def test_build_output_is_not_source(self):
        build_output = self.workspace.source_root / "obj" / "Debug"
        build_output.mkdir(parents=True)
        (build_output / "Generated.cs").write_text(
            CONNECTOR_SOURCE.replace("JiraConnectorTest", "GeneratedTest"), encoding="utf-8"
        )
        timings = self.workspace.backend_rows(
            [*steady_rows(), row(f"{TESTS_NAMESPACE}.GeneratedTest.Runs", "Unit", 0.01)]
        )
        self.workspace.planted_budget()

        figures = self.figures_of(
            self.report_of(self.workspace.check("backend", timings), "Backend test time")
        )

        self.assertEqual(figures["Left out"][0], 0)
        self.assertEqual(figures["Fast"][0], 101)


FENCE = "```"


def summary_block(report):
    return f"\n{FENCE}\n{report}\n{FENCE}\n"


class TheReportLandsInTheJobSummary(TierReportTestCase):
    def outer_file(self, text):
        folder = tempfile.TemporaryDirectory()
        self.addCleanup(folder.cleanup)
        path = Path(folder.name) / "step-summary.md"
        path.write_text(text, encoding="utf-8")
        return path

    def check_steady_backend_into(self, summary):
        timings = self.workspace.steady_run()
        self.workspace.planted_budget()
        return self.workspace.check("backend", timings, summary=summary)

    def test_the_report_also_lands_in_the_jobs_summary_after_what_is_there(self):
        earlier = "## Backend tests\n\n1,234 passed\n"
        summary = self.outer_file(earlier)

        first = self.check_steady_backend_into(summary)
        second = self.check_steady_backend_into(summary)

        self.assert_passes_quietly(first)
        self.assert_passes_quietly(second)
        report = self.report_of(first, "Backend test time")
        written = summary.read_text(encoding="utf-8")
        self.assertEqual(written, earlier + summary_block(report) + summary_block(report))
        self.assertEqual([line for line in written.splitlines() if line.startswith("::")], [])

    def test_with_no_summary_file_named_the_run_only_prints(self):
        timings = self.workspace.steady_run()
        self.workspace.planted_budget()
        for summary in (None, ""):
            with self.subTest(summary=summary):
                before = self.workspace.snapshot()

                result = self.workspace.check("backend", timings, summary=summary)

                self.assert_passes_quietly(result)
                self.report_of(result, "Backend test time")
                self.assertEqual(self.workspace.snapshot(), before)

    def test_a_summary_that_cannot_be_written_warns_and_changes_no_verdict(self):
        unwritable = self.outer_file("").parent / "a-folder"
        unwritable.mkdir()

        result = self.check_steady_backend_into(unwritable)

        self.assert_warns_and_passes(result, "summary", "a-folder")
        self.assertEqual(len(self.annotations(result, "warning")), 1, msg=self._explain(result))
        self.report_of(result, "Backend test time")

    def test_the_report_comes_before_any_verdict(self):
        summary = self.outer_file("")
        timings = self.workspace.steady_run(integration_seconds=4)
        self.workspace.budget()

        result = self.workspace.check("backend", timings, summary=summary)

        self.assert_fails_naming(result, "PortfolioEndpointTest")
        report = self.report_of(result, "Backend test time")
        lines = result.stdout.splitlines()
        first_error = next(i for i, line in enumerate(lines) if line.startswith("::error::"))
        self.assertTrue(
            all(lines.index(line) < first_error for line in report.splitlines()),
            msg=self._explain(result),
        )
        self.assertEqual(summary.read_text(encoding="utf-8"), summary_block(report))

    def test_the_checks_own_tests_leave_the_real_summary_alone(self):
        real = self.outer_file("## The job's own summary\n")
        timings = self.workspace.steady_run()
        self.workspace.planted_budget()
        other = time_budgets.Workspace(self.outer_file("").parent)
        other_timings = other.backend_timings({f"{FAST_CLASS}.Prices": 0.01})
        other.budget()

        with mock.patch.dict(os.environ, {SUMMARY_VARIABLE: str(real)}):
            results = [
                self.workspace.check("backend", timings),
                other.check("backend", other_timings),
            ]

        for result in results:
            self.assert_passes_quietly(result)
        self.assertEqual(real.read_text(encoding="utf-8"), "## The job's own summary\n")


if __name__ == "__main__":
    unittest.main()

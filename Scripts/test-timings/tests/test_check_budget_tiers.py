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
from decimal import Decimal
from pathlib import Path
from unittest import mock

HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))
sys.path.insert(0, str(HERE.parent))

from test_check_budget import (  # noqa: E402
    ALLOWLIST_NAME,
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


def steady_rows(integration_seconds=0.4, integration_tests=20):
    return (
        [row(f"{FAST_CLASS}.Prices{index}", "Unit", 0.01) for index in range(100)]
        + [
            row(f"{HOSTED_CLASS}.Serves{index}", "Integration", integration_seconds)
            for index in range(integration_tests)
        ]
        + [row(f"{FAST_CLASS}.Waits{index}", "Unit", 0, "NotExecuted") for index in range(3)]
        + [row(f"{CONNECTOR_CLASS}.ReadsALiveTracker", "Integration", 30)]
        + [row(f"{CONNECTOR_CLASS}.Reads{index}", "Integration", 0.1) for index in range(3)]
    )


class TierWorkspace(CheckWorkspace):
    """A workspace that writes any category and outcome, and plants the connector class."""

    def steady_run(self, extra_rows=(), integration_seconds=0.4, integration_tests=20):
        self.plant_connector_class()
        return self.backend_rows(
            [*steady_rows(integration_seconds, integration_tests), *extra_rows]
        )

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
    r"^(?P<tier>Fast|Integration|E2E|Left out|Not run)\s+(?P<tests>[\d,]+)"
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


RUN_IDS = [42372930241, 42372874167, 41904560367, 42401000001, 42401000002, 42401000003]
ENFORCING_RUNS = 5
FAST_LEDGER, MEAN_LEDGER, TOTAL_LEDGER = "fast_test_limit", "mean_ceiling", "total_ceiling"
LEDGER_WORDS = {
    FAST_LEDGER: "fast test limit ledger",
    MEAN_LEDGER: "mean ceiling ledger",
    TOTAL_LEDGER: "total ceiling ledger",
}


def entry(seconds, runs=ENFORCING_RUNS, reason="the largest of five plain runs on main", **extra):
    run_ids = RUN_IDS[:runs] if isinstance(runs, int) else runs
    return {"seconds": seconds, "runs": run_ids, "reason": reason, **extra}


def total_entry(seconds, tests_when_set=20, **extra):
    return entry(seconds, tests_when_set=tests_when_set, **extra)


def planted_ledgers(**ledgers):
    """Fast limit 2 s, mean ceiling 0.5 s, total ceiling 10 s set at 20 tests, all enforced."""
    return {
        FAST_LEDGER: [entry(2)],
        MEAN_LEDGER: [entry(0.5)],
        TOTAL_LEDGER: [total_entry(10)],
        **ledgers,
    }


def ceiling_cell(ceiling, room):
    return f"{ceiling:<31} {room}"


def fast_line(ceiling, room):
    return f"Fast            100       1.0    0.010   {ceiling_cell(ceiling, room)}"


def mean_line(ceiling, room):
    return f"Integration      20       8.0    0.400   {ceiling_cell(ceiling, room)}"


def total_line(ceiling, room):
    return f"{'':<41}{ceiling_cell(ceiling, room)}"


class LedgerTestCase(TierReportTestCase):
    def check_ledgers(self, ledgers, *options):
        timings = self.workspace.steady_run()
        self.write_ledgers(ledgers)
        return self.workspace.check("backend", timings, *options)

    def write_ledgers(self, ledgers, fast_allowlist=None, budget_seconds=PLANTED_BUDGET_SECONDS):
        document = {
            "frontend": {"budget_seconds": PLANTED_BUDGET_SECONDS, "allowlist": []},
            "backend": {
                "budget_seconds": budget_seconds,
                "allowlist": [],
                "ledgers": ledgers,
            },
        }
        if fast_allowlist is not None:
            document["backend"]["fast_allowlist"] = list(fast_allowlist)
        self.workspace.raw_allowlist(json.dumps(document, indent=2))

    def accepted_report(self, ledgers):
        result = self.check_ledgers(ledgers)
        self.assertEqual(result.returncode, 0, msg=self._explain(result))
        self.assertEqual(self.annotations(result, "error"), [], msg=self._explain(result))
        return self.report_of(result, "Backend test time").splitlines()

    def assert_ledger_refused(self, result, ledger, *fragments):
        self.assertEqual(result.returncode, 2, msg=self._explain(result))
        errors = self.annotations(result, "error")
        self.assertEqual(len(errors), 1, msg=self._explain(result))
        for fragment in (LEDGER_WORDS.get(ledger, ledger), ALLOWLIST_NAME, *fragments):
            self.assertIn(fragment, errors[0], msg=self._explain(result))
        self.assertNotIn("Backend test time", result.stdout, msg=self._explain(result))
        self.assertNotIn("Traceback", result.stderr, msg=self._explain(result))



class ALimitIsLoweredByANewEntry(LedgerTestCase):
    def test_a_limit_is_lowered_by_a_new_entry(self):
        result = self.check_ledgers(planted_ledgers())

        self.assert_passes_quietly(result)
        report = self.report_of(result, "Backend test time").splitlines()
        self.assertIn(fast_line("per test 2 s", "largest 0.010 s"), report)
        self.assertIn(mean_line("mean 0.5", "20 %"), report)
        self.assertIn(total_line("total 10", "20 %"), report)
        self.assertIn("Since the total was set: +0 tests", report)
        self.assertLess(
            report.index("Not run           3   did not pass, not counted"),
            report.index("Since the total was set: +0 tests"),
        )

        lowered = self.accepted_report(planted_ledgers(**{MEAN_LEDGER: [entry(0.5), entry(0.45)]}))

        self.assertIn(mean_line("mean 0.45", "11 %"), lowered)


class AMeanOrFastLimitThatRisesIsRefused(LedgerTestCase):
    def test_a_mean_or_fast_limit_that_rises_is_refused(self):
        cases = {
            (MEAN_LEDGER, "above"): [entry(0.5), entry(0.55)],
            (MEAN_LEDGER, "equal"): [entry(0.5), entry(0.5)],
            (FAST_LEDGER, "above"): [entry(2), entry(2.5)],
            (FAST_LEDGER, "equal"): [entry(2), entry(2)],
        }
        for (ledger, how), entries in cases.items():
            with self.subTest(ledger=ledger, how=how):
                self.workspace = self.fresh_workspace()

                result = self.check_ledgers(planted_ledgers(**{ledger: entries}))

                self.assert_ledger_refused(result, ledger, "entry 2", "only goes down")

    def test_the_total_may_rise_with_a_reason(self):
        report = self.accepted_report(
            planted_ledgers(
                **{TOTAL_LEDGER: [total_entry(10), total_entry(13, tests_when_set=26)]}
            )
        )

        self.assertIn(total_line("total 13", "38 %"), report)
        self.assertIn("Since the total was set: -6 tests", report)

        self.workspace = self.fresh_workspace()
        blank_reason = [total_entry(10), total_entry(13, tests_when_set=26, reason="  ")]

        result = self.check_ledgers(planted_ledgers(**{TOTAL_LEDGER: blank_reason}))

        self.assert_ledger_refused(result, TOTAL_LEDGER, "entry 2", "reason")


class AProvisionalEntryCannotSitAboveTheMeanOrFastLimit(LedgerTestCase):
    def test_a_provisional_entry_above_the_mean_or_fast_limit_is_refused(self):
        for ledger, enforced, above in ((MEAN_LEDGER, 0.5, 0.6), (FAST_LEDGER, 2, 2.4)):
            with self.subTest(ledger=ledger):
                self.workspace = self.fresh_workspace()
                entries = [entry(enforced), entry(above, runs=2)]

                result = self.check_ledgers(planted_ledgers(**{ledger: entries}))

                self.assert_ledger_refused(result, ledger, "provisional", "entry 2")

    def test_a_provisional_entry_equal_to_or_below_the_limit_is_accepted_and_shown_beside_it(self):
        cases = [
            (MEAN_LEDGER, [entry(0.5), entry(0.5, runs=2)], mean_line("mean 0.5, provisional 0.5 (2 of 5)", "20 %")),
            (MEAN_LEDGER, [entry(0.5), entry(0.45, runs=2)], mean_line("mean 0.5, provisional 0.45 (2 of 5)", "20 %")),
            (FAST_LEDGER, [entry(2), entry(2, runs=2)], fast_line("per test 2 s, provisional 2 (2 of 5)", "largest 0.010 s")),
            (FAST_LEDGER, [entry(2), entry(1.8, runs=2)], fast_line("per test 2 s, provisional 1.8 (2 of 5)", "largest 0.010 s")),
            (TOTAL_LEDGER, [total_entry(10), total_entry(13, runs=2)], total_line("total 10, provisional 13 (2 of 5)", "20 %")),
        ]
        for ledger, entries, expected in cases:
            with self.subTest(ledger=ledger, entries=entries):
                self.workspace = self.fresh_workspace()

                report = self.accepted_report(planted_ledgers(**{ledger: entries}))

                self.assertIn(expected, report)


class ThereIsOneProvisionalEntryAtMostAndItComesLast(LedgerTestCase):
    def test_there_is_one_provisional_entry_at_most_and_it_comes_last(self):
        cases = {
            "two provisional": [entry(0.5), entry(0.48, runs=2), entry(0.45, runs=3)],
            "provisional before enforced": [entry(0.5, runs=2), entry(0.45)],
        }
        for case, entries in cases.items():
            with self.subTest(case=case):
                self.workspace = self.fresh_workspace()

                result = self.check_ledgers(planted_ledgers(**{MEAN_LEDGER: entries}))

                self.assert_ledger_refused(result, MEAN_LEDGER, "provisional", "last")


class DistinctRunIdsDecideWhatAnEntryIs(LedgerTestCase):
    def test_distinct_run_ids_decide_what_an_entry_is(self):
        cases = {
            "one id": (entry(0.5, runs=1), "mean 0.5 (provisional, 1 of 5)"),
            "four ids": (entry(0.5, runs=4), "mean 0.5 (provisional, 4 of 5)"),
            "five ids": (entry(0.5, runs=5), "mean 0.5"),
            "six ids": (entry(0.5, runs=6), "mean 0.5"),
            "five text ids": (entry(0.5, runs=[str(run) for run in RUN_IDS[:5]]), "mean 0.5"),
        }
        for case, (only_entry, ceiling) in cases.items():
            with self.subTest(case=case):
                self.workspace = self.fresh_workspace()

                report = self.accepted_report(planted_ledgers(**{MEAN_LEDGER: [only_entry]}))

                self.assertIn(mean_line(ceiling, "20 %"), report)

    def test_an_entry_with_no_run_id_or_a_repeated_one_is_refused(self):
        cases = {
            "no id": (entry(0.5, runs=[]), "no run id"),
            "a repeat": (entry(0.5, runs=[*RUN_IDS[:4], RUN_IDS[0]]), "more than once"),
            "a repeat as text": (entry(0.5, runs=[*RUN_IDS[:4], str(RUN_IDS[0])]), "more than once"),
        }
        for case, (only_entry, fragment) in cases.items():
            with self.subTest(case=case):
                self.workspace = self.fresh_workspace()

                result = self.check_ledgers(planted_ledgers(**{MEAN_LEDGER: [only_entry]}))

                self.assert_ledger_refused(result, MEAN_LEDGER, "entry 1", fragment)


class AMalformedEntryIsRefused(LedgerTestCase):
    def test_a_malformed_entry_is_refused_naming_what_is_wrong(self):
        no_reason = entry(0.5)
        del no_reason["reason"]
        no_count = total_entry(10)
        del no_count["tests_when_set"]
        cases = {
            "seconds 0": (MEAN_LEDGER, [entry(0)], "seconds"),
            "seconds -1": (MEAN_LEDGER, [entry(-1)], "seconds"),
            "seconds as text": (MEAN_LEDGER, [entry("0.5")], "seconds"),
            "seconds true": (MEAN_LEDGER, [entry(True)], "seconds"),
            "no reason": (MEAN_LEDGER, [no_reason], "reason"),
            "ledger not a list": (MEAN_LEDGER, entry(0.5), "not a list"),
            "entry not an object": (MEAN_LEDGER, [0.5], "entry 1"),
            "ids not a list": (MEAN_LEDGER, [entry(0.5, runs=str(RUN_IDS[0]))], "run ids"),
            "an id of true": (MEAN_LEDGER, [entry(0.5, runs=[*RUN_IDS[:4], True])], "run id"),
            "an id of blank text": (MEAN_LEDGER, [entry(0.5, runs=[*RUN_IDS[:4], " "])], "run id"),
            "total with no count": (TOTAL_LEDGER, [no_count], "tests_when_set"),
            "total counted at 0": (TOTAL_LEDGER, [total_entry(10, tests_when_set=0)], "tests_when_set"),
        }
        for case, (ledger, entries, fragment) in cases.items():
            with self.subTest(case=case):
                self.workspace = self.fresh_workspace()

                result = self.check_ledgers(planted_ledgers(**{ledger: entries}))

                self.assert_ledger_refused(result, ledger, fragment)

    def test_a_ledgers_block_that_is_not_an_object_is_refused(self):
        result = self.check_ledgers([entry(0.5)])

        self.assert_ledger_refused(result, "ledgers", "not an object")


class TheSwitchDoesNotSoftenABadLedger(LedgerTestCase):
    def test_the_switch_does_not_soften_a_bad_ledger(self):
        rising = planted_ledgers(**{MEAN_LEDGER: [entry(0.5), entry(0.55)]})

        with self.subTest(switch="--warn-only"):
            self.assert_ledger_refused(
                self.check_ledgers(rising, "--warn-only"), MEAN_LEDGER, "only goes down"
            )
        with self.subTest(switch="variable"):
            self.workspace = self.fresh_workspace()
            with mock.patch.dict(os.environ, {check_budget.WARN_ONLY_VARIABLE: "true"}):
                result = self.check_ledgers(rising)
            self.assert_ledger_refused(result, MEAN_LEDGER, "only goes down")


LISTED_CLASS = f"{TESTS_NAMESPACE}.ImportTest"
OTHER_CLASS = f"{TESTS_NAMESPACE}.InvoiceTest"


class FastTestLimitTestCase(LedgerTestCase):
    def check_fast(self, extra_rows=(), fast_allowlist=None, ledgers=None, integration_seconds=0.4):
        timings = self.workspace.steady_run(extra_rows, integration_seconds)
        self.write_ledgers(planted_ledgers() if ledgers is None else ledgers, fast_allowlist)
        return self.workspace.check("backend", timings)

    def named_in(self, result, test):
        return [line for line in result.stdout.splitlines() if line.startswith("::") and test in line]


class ASlowFastTestFailsTheBuildAndIsNamed(FastTestLimitTestCase):
    def test_a_slow_fast_test_fails_the_build_and_is_named(self):
        slow = f"{FAST_CLASS}.PricesAWholeCatalogue"

        result = self.check_fast([row(slow, "Unit", 3.4)])

        self.assert_fails_naming(
            result, slow, "took 3.400s", "over the 2s fast test limit", f"set in {ALLOWLIST_NAME}"
        )
        self.assertEqual(len(self.annotations(result, "error")), 1, msg=self._explain(result))

    def test_a_fast_test_is_judged_at_the_edge_of_the_limit(self):
        at_the_limit = f"{FAST_CLASS}.PricesAtTheLimit"
        just_over = f"{FAST_CLASS}.PricesJustOver"

        result = self.check_fast([row(at_the_limit, "Unit", 2), row(just_over, "Unit", 2.001)])

        self.assert_fails_naming(result, just_over, "2.001s", "fast test limit")
        self.assertEqual(len(self.annotations(result, "error")), 1, msg=self._explain(result))
        self.assertEqual(self.named_in(result, at_the_limit), [], msg=self._explain(result))

    def test_only_fast_tests_answer_to_this_limit(self):
        result = self.check_fast(
            [
                row(f"{HOSTED_CLASS}.ServesTheWholePortfolio", "Integration", 3),
                row(f"{CONNECTOR_CLASS}.SyncsEveryProject", "Unit", 30),
                row(f"{FAST_CLASS}.WaitsForever", "Unit", 30, "NotExecuted"),
            ],
            integration_seconds=0.2,
        )

        self.assert_passes_quietly(result)

    def test_every_slow_test_is_named_not_only_the_first(self):
        slow = {
            f"{FAST_CLASS}.PricesSlowly": 3,
            f"{FAST_CLASS}.DiscountsSlowly": 4,
            f"{OTHER_CLASS}.TotalsSlowly": 5,
        }

        result = self.check_fast([row(name, "Unit", seconds) for name, seconds in slow.items()])

        self.assertEqual(result.returncode, 1, msg=self._explain(result))
        self.assertEqual(len(self.annotations(result, "error")), 3, msg=self._explain(result))
        for name, seconds in slow.items():
            self.assert_fails_naming(result, name, f"took {seconds}.000s", "fast test limit")


class AListedClassAnswersToItsOwnCeiling(FastTestLimitTestCase):
    def setUp(self):
        super().setUp()
        self.workspace.declare_test_class("ImportTest")

    def test_a_listed_class_answers_to_its_own_ceiling(self):
        under = f"{LISTED_CLASS}.ImportsABoard"
        over = f'{LISTED_CLASS}("Jira.Cloud").ImportsEveryBoard'

        result = self.check_fast(
            [row(under, "Unit", 3), row(over, "Unit", 6)],
            fast_allowlist=[allowed(LISTED_CLASS, 5)],
        )

        self.assert_fails_naming(result, over, "took 6.000s", "over the 5s class ceiling")
        self.assertEqual(len(self.annotations(result, "error")), 1, msg=self._explain(result))
        self.assertEqual(self.named_in(result, under), [], msg=self._explain(result))

    def test_a_listed_class_that_is_gone_fails_whatever_the_switch_says(self):
        gone = f"{TESTS_NAMESPACE}.RetiredImportTest"
        with mock.patch.dict(os.environ, {check_budget.WARN_ONLY_VARIABLE: "true"}):
            result = self.check_fast(fast_allowlist=[allowed(gone, 5)])

        self.assert_fails_naming(result, gone, ALLOWLIST_NAME, "no longer in the backend test source")

    def test_a_listed_class_that_no_longer_earns_its_place_fails_and_a_stricter_one_stays(self):
        with self.subTest(ceiling="equal to the limit"):
            result = self.check_fast(fast_allowlist=[allowed(LISTED_CLASS, 2)])

            self.assert_fails_naming(result, LISTED_CLASS, ALLOWLIST_NAME, "not needed")

        with self.subTest(ceiling="below the limit"):
            stricter = f"{LISTED_CLASS}.ImportsTwoBoards"

            result = self.check_fast(
                [row(stricter, "Unit", 1.8)], fast_allowlist=[allowed(LISTED_CLASS, 1.5)]
            )

            self.assert_fails_naming(result, stricter, "took 1.800s", "over the 1.5s class ceiling")
            self.assertEqual(len(self.annotations(result, "error")), 1, msg=self._explain(result))


class TheFastAllowlistStaysShortAndComplete(FastTestLimitTestCase):
    def listed_classes(self, how_many):
        names = [f"Import{index}Test" for index in range(how_many)]
        for name in names:
            self.workspace.declare_test_class(name)
        return [allowed(f"{TESTS_NAMESPACE}.{name}", 3) for name in names]

    def test_ten_listed_classes_are_accepted(self):
        result = self.check_fast(fast_allowlist=self.listed_classes(10))

        self.assert_passes_quietly(result)

    def test_a_list_that_is_too_long_or_incomplete_is_refused(self):
        no_reason = allowed(LISTED_CLASS, 3)
        del no_reason["reason"]
        no_ceiling = allowed(LISTED_CLASS, 3)
        del no_ceiling["ceiling_seconds"]
        cases = {
            "eleven entries": (lambda: self.listed_classes(11), None, "at most 10"),
            "no reason": (lambda: [no_reason], None, "reason"),
            "no ceiling": (lambda: [no_ceiling], None, "ceiling"),
            "a class listed twice": (
                lambda: [allowed(LISTED_CLASS, 3), allowed(LISTED_CLASS, 4)],
                None,
                "more than once",
            ),
            "fast ledger empty": (
                lambda: [allowed(LISTED_CLASS, 3)],
                planted_ledgers(**{FAST_LEDGER: []}),
                "no enforced entry",
            ),
            "fast ledger provisional only": (
                lambda: [allowed(LISTED_CLASS, 3)],
                planted_ledgers(**{FAST_LEDGER: [entry(2, runs=2)]}),
                "no enforced entry",
            ),
        }
        for case, (entries, ledgers, fragment) in cases.items():
            with self.subTest(case=case):
                self.workspace = self.fresh_workspace()
                self.workspace.declare_test_class("ImportTest")

                result = self.check_fast(fast_allowlist=entries(), ledgers=ledgers)

                self.assert_refuses_to_judge(result, ALLOWLIST_NAME, "fast_allowlist", fragment)
                self.assertEqual(len(self.annotations(result, "error")), 1, msg=self._explain(result))


GONE_CLASS = f"{TESTS_NAMESPACE}.RetiredImportTest"


class CeilingTestCase(LedgerTestCase):
    def check_integration(self, seconds, tests, extra_rows=(), ledgers=None, **options):
        timings = self.workspace.steady_run(extra_rows, seconds, tests)
        self.write_ledgers(planted_ledgers() if ledgers is None else ledgers, **options)
        return self.workspace.check("backend", timings)

    def assert_breaches_alone(self, result, ceiling, *fragments):
        self.assert_fails_naming(result, f"{ceiling} set in {ALLOWLIST_NAME}", *fragments)
        self.assertEqual(len(self.annotations(result, "error")), 1, msg=self._explain(result))


class ASlowerIntegrationTierFailsTheBuild(CeilingTestCase):
    def test_a_slower_integration_tier_fails_the_build_though_no_test_is_over_any_other_limit(self):
        result = self.check_integration(0.6, 16)

        self.assert_breaches_alone(
            result,
            "mean ceiling",
            "Integration tests averaged 0.600s each over 16 tests",
            "over the 0.5s mean ceiling",
        )

    def test_more_tests_at_the_same_mean_pass_until_the_total_is_reached(self):
        for tests in (20, 25):
            with self.subTest(tests=tests):
                self.workspace = self.fresh_workspace()

                self.assert_passes_quietly(self.check_integration(0.4, tests))

        self.workspace = self.fresh_workspace()

        result = self.check_integration(0.4, 26)

        self.assert_breaches_alone(
            result,
            "total ceiling",
            "Integration tests took 10.400s in all over 26 tests",
            "over the 10s total ceiling",
            "; +6 tests since it was set",
        )


class EachCeilingIsJudgedAtItsEdge(CeilingTestCase):
    def test_a_mean_of_exactly_the_ceiling_passes_and_one_millisecond_more_breaches_it_alone(self):
        self.assert_passes_quietly(self.check_integration(0.5, 10))

        self.workspace = self.fresh_workspace()
        one_more = row(f"{HOSTED_CLASS}.ServesOneMillisecondLonger", "Integration", 0.501)

        result = self.check_integration(0.5, 9, [one_more])

        self.assert_breaches_alone(
            result, "mean ceiling", "averaged 0.501s each over 10 tests", "over the 0.5s"
        )

    def test_a_total_of_exactly_the_ceiling_passes_and_one_millisecond_more_breaches_it_alone(self):
        self.assert_passes_quietly(self.check_integration(0.4, 25))

        self.workspace = self.fresh_workspace()
        one_more = row(f"{HOSTED_CLASS}.ServesOneMillisecondLonger", "Integration", 0.401)

        result = self.check_integration(0.4, 24, [one_more])

        self.assert_breaches_alone(
            result, "total ceiling", "took 10.001s in all over 25 tests", "over the 10s"
        )


class AReviewedRaiseGivesTheTotalRoomAgain(CeilingTestCase):
    def raised(self, runs):
        return planted_ledgers(
            **{TOTAL_LEDGER: [total_entry(10), total_entry(13, tests_when_set=26, runs=runs)]}
        )

    def test_a_reviewed_raise_gives_the_total_room_again(self):
        result = self.check_integration(0.4, 26, ledgers=self.raised(ENFORCING_RUNS))

        self.assert_passes_quietly(result)
        self.assertIn(
            "Since the total was set: +0 tests",
            self.report_of(result, "Backend test time").splitlines(),
        )

    def test_a_raise_on_too_few_runs_lifts_nothing(self):
        result = self.check_integration(0.4, 26, ledgers=self.raised(4))

        self.assert_breaches_alone(result, "total ceiling", "over the 10s total ceiling")
        self.assertIn(
            total_line("total 10, provisional 13 (4 of 5)", "-4 %"),
            self.report_of(result, "Backend test time").splitlines(),
        )


class APushThatRunsTheLiveSuitesDoesNotMoveTheCeilings(CeilingTestCase):
    def test_a_push_that_runs_the_live_suites_does_not_move_the_ceilings(self):
        live = [
            row(f"{CONNECTOR_CLASS}.SyncsLiveProject{index}", "Integration", 30)
            for index in range(40)
        ]

        result = self.check_integration(0.4, 20, live)

        self.assert_passes_quietly(result)
        figures = self.figures_of(self.report_of(result, "Backend test time"))
        self.assertEqual(figures["Integration"], (20, "8.0", "0.400"))


class TheSwitchSoftensEveryNewBreachAndNothingElse(CeilingTestCase):
    def check_three_breaches(self, *, switch, fast_allowlist=None):
        self.workspace = self.fresh_workspace()
        slow = row(f"{FAST_CLASS}.PricesAWholeCatalogue", "Unit", 3.4)
        environment = {check_budget.WARN_ONLY_VARIABLE: "true"} if switch else {}
        with mock.patch.dict(os.environ, environment):
            return self.check_integration(0.6, 21, [slow], fast_allowlist=fast_allowlist)

    def test_the_switch_turns_the_fast_mean_and_total_breaches_into_warnings(self):
        softened = self.check_three_breaches(switch=True)
        judged = self.check_three_breaches(switch=False)

        self.assertEqual(softened.returncode, 0, msg=self._explain(softened))
        self.assertEqual(self.annotations(softened, "error"), [], msg=self._explain(softened))
        warnings = self.annotations(softened, "warning")
        for limit in ("fast test limit", "mean ceiling", "total ceiling"):
            with self.subTest(limit=limit):
                named = [warning for warning in warnings if limit in warning]
                self.assertEqual(len(named), 1, msg=self._explain(softened))
        self.assertEqual(len(warnings), 3, msg=self._explain(softened))
        self.assertEqual(judged.returncode, 1, msg=self._explain(judged))
        self.assertEqual(len(self.annotations(judged, "error")), 3, msg=self._explain(judged))
        self.assertEqual(
            self.report_of(softened, "Backend test time"),
            self.report_of(judged, "Backend test time"),
        )

    def test_a_stale_list_entry_in_the_same_run_still_fails(self):
        result = self.check_three_breaches(switch=True, fast_allowlist=[allowed(GONE_CLASS, 5)])

        self.assert_fails_naming(result, GONE_CLASS, "no longer in the backend test source")
        self.assertEqual(len(self.annotations(result, "warning")), 3, msg=self._explain(result))


class TheOlderClassBudgetStillJudgesBesideTheNewLimits(CeilingTestCase):
    def test_a_class_over_the_class_budget_fails_with_its_message_unchanged(self):
        result = self.check_integration(0.4, 20, budget_seconds=5)

        self.assertEqual(result.returncode, 1, msg=self._explain(result))
        self.assertIn(
            f"::error::{HOSTED_CLASS} took 8.0s, over the 5s budget set in {ALLOWLIST_NAME}",
            self.annotations(result, "error"),
            msg=self._explain(result),
        )
        self.assertNotIn("ceiling", result.stdout.split("No gate.")[-1], msg=self._explain(result))

    def test_a_breach_with_a_summary_that_cannot_be_written_fails_and_warns_once(self):
        timings = self.workspace.steady_run(integration_seconds=0.6, integration_tests=16)
        self.write_ledgers(planted_ledgers())
        unwritable = self.workspace.root / "a-folder"
        unwritable.mkdir()

        result = self.workspace.check("backend", timings, summary=unwritable)

        self.assert_breaches_alone(result, "mean ceiling", "averaged 0.600s")
        warnings = self.annotations(result, "warning")
        self.assertEqual(len(warnings), 1, msg=self._explain(result))
        self.assertIn("summary", warnings[0], msg=self._explain(result))


PROVISIONAL_RUNS = 2
NOTHING_TO_JUDGE = (
    f"::error::{ALLOWLIST_NAME} holds limits that need Integration tests, "
    "but this run has none, so it cannot be judged"
)


def progress_line(limit, seconds, runs=PROVISIONAL_RUNS):
    return (
        f"::warning::the {limit} of {seconds}s is provisional, {runs} of {ENFORCING_RUNS} runs, "
        "so it warns and does not fail"
    )


def only(ledger, entries):
    return {FAST_LEDGER: [], MEAN_LEDGER: [], TOTAL_LEDGER: [], ledger: entries}


class AProvisionalLimitWarnsAndNeverFails(CeilingTestCase):
    """Each limit read from two runs; the first case of each is a run over it."""

    SLOW_UNIT = row(f"{FAST_CLASS}.PricesAWholeCatalogue", "Unit", 3.4)
    CASES = (
        (
            FAST_LEDGER,
            "fast test limit",
            entry(2, runs=PROVISIONAL_RUNS),
            dict(seconds=0.4, tests=20, extra_rows=[SLOW_UNIT]),
            f"{FAST_CLASS}.PricesAWholeCatalogue took 3.400s, over the 2s provisional fast test "
            f"limit set in {ALLOWLIST_NAME}",
        ),
        (
            MEAN_LEDGER,
            "Integration mean ceiling",
            entry(0.5, runs=PROVISIONAL_RUNS),
            dict(seconds=0.6, tests=16),
            "Integration tests averaged 0.600s each over 16 tests, over the 0.5s provisional mean "
            f"ceiling set in {ALLOWLIST_NAME}",
        ),
        (
            TOTAL_LEDGER,
            "Integration total ceiling",
            total_entry(10, runs=PROVISIONAL_RUNS),
            dict(seconds=0.4, tests=26),
            "Integration tests took 10.400s in all over 26 tests, over the 10s provisional total "
            f"ceiling set in {ALLOWLIST_NAME}; +6 tests since it was set",
        ),
    )

    def test_a_breached_provisional_limit_warns_on_its_own_number_and_passes(self):
        for ledger, limit, provisional, run, breach in self.CASES:
            with self.subTest(limit=limit):
                self.workspace = self.fresh_workspace()

                result = self.check_integration(**run, ledgers=only(ledger, [provisional]))

                self.assertEqual(result.returncode, 0, msg=self._explain(result))
                self.assertEqual(self.annotations(result, "error"), [], msg=self._explain(result))
                self.assertCountEqual(
                    self.annotations(result, "warning"),
                    [f"::warning::{breach}", progress_line(limit, provisional["seconds"])],
                    msg=self._explain(result),
                )

    def test_a_provisional_limit_says_how_far_along_it_is_on_every_run(self):
        ledgers = {ledger: [provisional] for ledger, _, provisional, _, _ in self.CASES}

        result = self.check_integration(0.4, 20, ledgers=ledgers)

        self.assertEqual(result.returncode, 0, msg=self._explain(result))
        self.assertEqual(self.annotations(result, "error"), [], msg=self._explain(result))
        self.assertCountEqual(
            self.annotations(result, "warning"),
            [progress_line(limit, provisional["seconds"]) for _, limit, provisional, _, _ in self.CASES],
            msg=self._explain(result),
        )
        report = self.report_of(result, "Backend test time").splitlines()
        for line in (
            fast_line("per test 2 s (provisional, 2 of 5)", "largest 0.010 s"),
            mean_line("mean 0.5 (provisional, 2 of 5)", "20 %"),
            total_line("total 10 (provisional, 2 of 5)", "20 %"),
        ):
            self.assertIn(line, report, msg=self._explain(result))


class ANewerShortEntryCannotSwitchOffAnEnforcedOne(CeilingTestCase):
    def ledgers(self):
        return planted_ledgers(**{MEAN_LEDGER: [entry(0.5), entry(0.45, runs=PROVISIONAL_RUNS)]})

    def test_over_the_enforced_number_fails_once_and_between_the_two_only_warns(self):
        over_both = self.check_integration(0.6, 16, ledgers=self.ledgers())

        self.assert_breaches_alone(
            over_both, "mean ceiling", "averaged 0.600s", "over the 0.5s mean ceiling"
        )
        self.assertEqual(
            self.annotations(over_both, "warning"),
            [progress_line("Integration mean ceiling", 0.45)],
            msg=self._explain(over_both),
        )

        self.workspace = self.fresh_workspace()

        between = self.check_integration(0.47, 16, ledgers=self.ledgers())

        self.assertEqual(between.returncode, 0, msg=self._explain(between))
        self.assertEqual(self.annotations(between, "error"), [], msg=self._explain(between))
        self.assertCountEqual(
            self.annotations(between, "warning"),
            [
                "::warning::Integration tests averaged 0.470s each over 16 tests, over the 0.45s "
                f"provisional mean ceiling set in {ALLOWLIST_NAME}",
                progress_line("Integration mean ceiling", 0.45),
            ],
            msg=self._explain(between),
        )


class ALimitInForceWithNoIntegrationTestsGivesNoVerdict(LedgerTestCase):
    SLOW_UNIT = row(f"{FAST_CLASS}.PricesAWholeCatalogue", "Unit", 3.4)

    def unit_rows_only(self, extra_rows):
        return self.workspace.backend_rows(
            [row(f"{FAST_CLASS}.Prices{index}", "Unit", 0.01) for index in range(100)]
            + list(extra_rows)
        )

    def test_a_limit_in_force_with_no_integration_tests_gives_no_verdict(self):
        switch_on = {check_budget.WARN_ONLY_VARIABLE: "true"}
        cases = {
            "mean, enforced": (only(MEAN_LEDGER, [entry(0.5)]), (), {}),
            "mean, provisional": (only(MEAN_LEDGER, [entry(0.5, runs=PROVISIONAL_RUNS)]), (), {}),
            "fast limit alone": (only(FAST_LEDGER, [entry(2)]), (), {}),
            "total alone": (only(TOTAL_LEDGER, [total_entry(10)]), (), {}),
            "a test over the fast limit": (only(FAST_LEDGER, [entry(2)]), [self.SLOW_UNIT], {}),
            "the switch on": (planted_ledgers(), [self.SLOW_UNIT], switch_on),
        }
        for case, (ledgers, extra_rows, environment) in cases.items():
            with self.subTest(case=case):
                self.workspace = self.fresh_workspace()
                timings = self.unit_rows_only(extra_rows)
                self.write_ledgers(ledgers)

                with mock.patch.dict(os.environ, environment):
                    result = self.workspace.check("backend", timings)

                self.assertEqual(result.returncode, 2, msg=self._explain(result))
                self.assertEqual(
                    self.annotations(result, "error"), [NOTHING_TO_JUDGE], msg=self._explain(result)
                )
                self.assertNotIn("PricesAWholeCatalogue", result.stdout, msg=self._explain(result))
                lines = result.stdout.splitlines()
                self.assertLess(
                    lines.index("Backend test time"),
                    lines.index(NOTHING_TO_JUDGE),
                    msg=self._explain(result),
                )


PLANTED_NAMESPACE = "Lighthouse.Backend.Tests.Planted"


class ShippedWorkspace(TierWorkspace, time_budgets.Workspace):
    """Writes rows of any category and checks them against the shipped file and test source."""


def shipped_ledger_values():
    backend = json.loads(check_budget.DEFAULT_ALLOWLIST.read_text(encoding="utf-8"))["backend"]
    ledgers = backend.get("ledgers", {})
    seconds = [item["seconds"] for entries in ledgers.values() for item in entries]
    ceilings = [item["ceiling_seconds"] for item in backend.get("fast_allowlist", [])]
    return [str(Decimal(str(value))) for value in seconds + ceilings]


class TheShippedFileObeysTheLedgerRulesWhateverItsNumbers(unittest.TestCase):
    def test_the_reader_accepts_the_shipped_file(self):
        ledgers = check_budget.load_ledgers(check_budget.DEFAULT_ALLOWLIST)

        check_budget.load_fast_allowlist(check_budget.DEFAULT_ALLOWLIST, ledgers.fast)

    def test_no_test_pins_a_number_of_the_shipped_ledgers_and_an_empty_ledger_has_none(self):
        sources = {path.name: path.read_text(encoding="utf-8") for path in HERE.glob("*.py")}
        for value in shipped_ledger_values():
            with self.subTest(value=value):
                self.assertEqual(
                    [name for name, text in sources.items() if value in text],
                    [],
                    msg=f"{value}, from the shipped ledgers, is written in a test",
                )


class ABreachPlantedFromTheShippedLimitsFails(TierReportTestCase):
    """Each shipped limit with an enforced entry, read from the file, and a run just over it."""

    def fresh_workspace(self):
        folder = tempfile.TemporaryDirectory()
        self.addCleanup(folder.cleanup)
        return ShippedWorkspace(Path(folder.name))

    def setUp(self):
        super().setUp()
        self.ledgers = check_budget.load_ledgers(check_budget.DEFAULT_ALLOWLIST)
        self.budget, _ = check_budget.load_allowlist("backend", check_budget.DEFAULT_ALLOWLIST)

    def hosted(self, seconds, tests=1):
        # Spread over classes so no planted class runs over the class budget as well.
        per_class = max(1, int(self.budget / max(seconds, Decimal("0.001"))) - 1)
        return [
            row(f"{PLANTED_NAMESPACE}.Hosted{index // per_class}Test.Serves{index}", "Integration", seconds)
            for index in range(tests)
        ]

    def check_shipped(self, rows):
        return self.workspace.check_shipped("backend", self.workspace.backend_rows(rows))

    def over_the_fast_limit(self, limit):
        test = f"{PLANTED_NAMESPACE}.PricingTest.PricesAWholeCatalogue"
        rows = [row(test, "Unit", limit + Decimal("0.001")), *self.hosted(Decimal(0))]
        return self.check_shipped(rows), (test, "fast test limit")

    def over_the_mean(self, limit):
        return self.check_shipped(self.hosted(limit + Decimal("0.001"), 2)), ("mean ceiling",)

    def over_the_total(self, limit):
        mean = self.ledgers.mean.enforced
        seconds = min(mean.seconds, limit) if mean else limit
        tests = int(limit / seconds) + 1
        return self.check_shipped(self.hosted(seconds, tests)), ("total ceiling",)

    def over_the_class_budget(self):
        test = f"{PLANTED_NAMESPACE}.SlowTest.Runs"
        rows = [row(test, "Unit", self.budget + 1), *self.hosted(Decimal(0))]
        return self.check_shipped(rows), (f"{PLANTED_NAMESPACE}.SlowTest", "budget")

    def test_each_enforced_shipped_limit_fails_a_run_just_over_it_and_an_empty_ledger_leaves_it_to_the_class_budget(self):
        planted = {
            FAST_LEDGER: (self.ledgers.fast, self.over_the_fast_limit),
            MEAN_LEDGER: (self.ledgers.mean, self.over_the_mean),
            TOTAL_LEDGER: (self.ledgers.total, self.over_the_total),
        }
        for ledger, (limit, plant) in planted.items():
            with self.subTest(ledger=ledger, empty=limit.enforced is None):
                self.workspace = self.fresh_workspace()

                result, named = (
                    plant(limit.enforced.seconds) if limit.enforced else self.over_the_class_budget()
                )

                self.assert_fails_naming(result, *named)


if __name__ == "__main__":
    unittest.main()

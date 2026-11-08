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

    def write_ledgers(self, ledgers):
        document = {
            "frontend": {"budget_seconds": PLANTED_BUDGET_SECONDS, "allowlist": []},
            "backend": {
                "budget_seconds": PLANTED_BUDGET_SECONDS,
                "allowlist": [],
                "ledgers": ledgers,
            },
        }
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


if __name__ == "__main__":
    unittest.main()

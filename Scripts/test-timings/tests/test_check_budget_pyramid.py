"""The three shares of the test pyramid across both stacks and Playwright, printed beside their limits.

The pyramid mode reads the two timing CSVs of one commit and the Playwright specs. It reports and
never judges, and it writes nothing. It is run as a person runs it, as a process.
"""

import subprocess
import sys
import tempfile
import unittest
from pathlib import Path

HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))
sys.path.insert(0, str(HERE.parent))

from test_check_budget import CHECK_BUDGET, check_environment  # noqa: E402
from test_check_budget_tiers import (  # noqa: E402
    TierReportTestCase,
    TierWorkspace,
    row,
)

PYRAMID_TITLE = "Test pyramid across both stacks and Playwright"
HEADER = "Tier          Tests    Share   Upper limit"
NO_GATE = "E2E counts the test declarations in the Playwright specs, not what ran. No gate."
TWO_TESTS_SPEC = (
    'import { test } from "../fixtures/LighthouseFixture";\n\n'
    'test("shows the overview", async ({ page }) => {});\n'
    'testWithDemo("opens a team", async ({ page }) => {});\n'
)


def frontend_row(file, name, seconds, outcome="Passed"):
    return (file, name, f"{seconds * 1000:.3f}", outcome)


class PyramidWorkspace(TierWorkspace):
    """A workspace that also holds a Playwright spec folder, and runs the pyramid mode."""

    def __init__(self, root: Path):
        super().__init__(root)
        self.spec_root = root / "specs"
        self.spec_root.mkdir()

    def write_spec(self, relative_path, source):
        path = self.spec_root / relative_path
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_text(source, encoding="utf-8")

    def frontend_of(self, fast_tests, component_tests, skipped=0):
        return self.frontend_rows(
            [frontend_row("src/a.test.ts", f"adds {index}", 0.01) for index in range(fast_tests)]
            + [
                frontend_row("src/b.test.tsx", f"renders {index}", 0.2)
                for index in range(component_tests)
            ]
            + [frontend_row("src/a.test.ts", f"waits {index}", 0, "Skipped") for index in range(skipped)]
        )

    def pyramid_arguments(self, frontend, backend):
        return [
            "--frontend-csv",
            str(frontend),
            "--backend-csv",
            str(backend),
            "--source-root",
            str(self.source_root),
            "--spec-root",
            str(self.spec_root),
        ]

    def pyramid(self, *arguments, summary=None):
        return subprocess.run(
            [sys.executable, str(CHECK_BUDGET), "--pyramid", *arguments],
            capture_output=True,
            text=True,
            cwd=self.root,
            env=check_environment(summary),
            timeout=60,
        )


class PyramidTestCase(TierReportTestCase):
    def fresh_workspace(self):
        folder = tempfile.TemporaryDirectory()
        self.addCleanup(folder.cleanup)
        return PyramidWorkspace(Path(folder.name))

    def pyramid_of(self, result):
        lines = result.stdout.splitlines()
        self.assertIn(PYRAMID_TITLE, lines, msg=self._explain(result))
        start = lines.index(PYRAMID_TITLE)
        self.assertIn(NO_GATE, lines[start:], msg=self._explain(result))
        return "\n".join(lines[start : lines.index(NO_GATE, start) + 1])

    def e2e_of(self, report):
        return self.figures_of(report)["E2E"][0]

    def steady_pyramid(self):
        frontend = self.workspace.frontend_of(fast_tests=2, component_tests=1)
        backend = self.workspace.steady_run()
        self.workspace.write_spec("overview.spec.ts", TWO_TESTS_SPEC)
        return frontend, backend


class TheThreeSharesArePrintedBesideTheirLimits(PyramidTestCase):
    def test_the_three_shares_across_both_stacks_and_playwright_are_printed_beside_their_limits(self):
        frontend, backend = self.steady_pyramid()

        result = self.workspace.pyramid(*self.workspace.pyramid_arguments(frontend, backend))

        self.assert_passes_quietly(result)
        self.assertEqual(
            self.pyramid_of(result),
            f"{PYRAMID_TITLE}\n"
            f"{HEADER}\n"
            "Fast            102   81.6 %   90 %\n"
            "Integration      21   16.8 %   9.5 %\n"
            "E2E               2    1.6 %   0.5 %\n"
            "Left out          4   connector classes, not counted\n"
            "Not run           3   did not pass, not counted\n"
            f"{NO_GATE}",
        )


class TheSharesNeverJudge(PyramidTestCase):
    def test_shares_far_over_their_limits_still_pass(self):
        frontend = self.workspace.frontend_of(fast_tests=58, component_tests=0)
        backend = self.workspace.backend_rows(
            [row(f"Tests.HostedTest.Serves{index}", "Integration", 0.4) for index in range(40)]
        )
        self.workspace.write_spec("overview.spec.ts", TWO_TESTS_SPEC)

        result = self.workspace.pyramid(*self.workspace.pyramid_arguments(frontend, backend))

        self.assert_passes_quietly(result)
        report = self.pyramid_of(result)
        self.assertIn("Integration      40   40.0 %   9.5 %", report)
        self.assertIn("E2E               2    2.0 %   0.5 %", report)

    def test_a_backend_with_nothing_labelled_says_so(self):
        frontend = self.workspace.frontend_of(fast_tests=3, component_tests=1)
        backend = self.workspace.backend_rows(
            [row(f"Tests.PricingTest.Prices{index}", "Unit", 0.01) for index in range(5)]
        )
        self.workspace.write_spec("overview.spec.ts", TWO_TESTS_SPEC)

        result = self.workspace.pyramid(*self.workspace.pyramid_arguments(frontend, backend))

        self.assert_passes_quietly(result)
        self.assertIn("nothing labelled yet", self.pyramid_of(result))


class AShareCountsWhatTheRunsReportCounts(PyramidTestCase):
    def test_the_tier_counts_equal_those_of_the_same_csvs_through_each_stacks_check(self):
        frontend = self.workspace.frontend_of(fast_tests=7, component_tests=4, skipped=2)
        backend = self.workspace.steady_run()
        self.workspace.write_spec("overview.spec.ts", TWO_TESTS_SPEC)
        self.workspace.planted_budget()

        frontend_figures = self.figures_of(
            self.report_of(self.workspace.check("frontend", frontend), "Frontend test time")
        )
        backend_figures = self.figures_of(
            self.report_of(self.workspace.check("backend", backend), "Backend test time")
        )
        pyramid = self.workspace.pyramid(*self.workspace.pyramid_arguments(frontend, backend))
        pyramid_figures = self.figures_of(self.pyramid_of(pyramid))

        for tier in ("Fast", "Integration", "Not run"):
            with self.subTest(tier=tier):
                self.assertEqual(
                    pyramid_figures[tier][0],
                    frontend_figures[tier][0] + backend_figures[tier][0],
                )
        self.assertEqual(pyramid_figures["Left out"][0], backend_figures["Left out"][0])
        self.assertEqual(pyramid_figures["Left out"][0], 4)
        self.assertEqual(pyramid_figures["Not run"][0], 5)


class OnlyRealTestDeclarationsCountAsE2ETests(PyramidTestCase):
    def test_only_line_start_test_calls_in_spec_files_count(self):
        self.workspace.write_spec(
            "teams/teams.spec.ts",
            'test("lists the teams", async () => {});\n'
            'testWithDemo("opens a team", async () => {});\n'
            "for (const name of names) {\n"
            '\ttest(`shows ${name}`, async () => {});\n'
            "}\n"
            'test.describe("a group", () => {});\n'
            'test.skip("not yet", async () => {});\n'
            "test.beforeEach(async () => {});\n"
            'testUserRow.getByRole("button");\n'
            '// test("commented out", async () => {});\n',
        )
        self.workspace.write_spec("teams/teams.feature", 'test("in a feature file", () => {});\n')
        self.workspace.write_spec("helpers/steps.ts", 'test("in a helper", async () => {});\n')
        frontend = self.workspace.frontend_of(fast_tests=1, component_tests=0)
        backend = self.workspace.steady_run()

        result = self.workspace.pyramid(*self.workspace.pyramid_arguments(frontend, backend))

        self.assert_passes_quietly(result)
        self.assertEqual(self.e2e_of(self.pyramid_of(result)), 3)

    def test_the_real_spec_folder_holds_some_tests(self):
        frontend = self.workspace.frontend_of(fast_tests=1, component_tests=0)
        backend = self.workspace.steady_run()

        result = self.workspace.pyramid(
            "--frontend-csv",
            str(frontend),
            "--backend-csv",
            str(backend),
            "--source-root",
            str(self.workspace.source_root),
        )

        self.assert_passes_quietly(result)
        self.assertGreater(self.e2e_of(self.pyramid_of(result)), 0)


class InputsThatCannotBeReadGiveNoShares(PyramidTestCase):
    def test_an_input_that_cannot_be_read_exits_2_naming_it(self):
        frontend, backend = self.steady_pyramid()
        wrong_columns = self.workspace.raw_timings("wrong.csv", "a,b\n1,2\n")
        missing = self.workspace.root / "nowhere"
        cases = {
            "missing CSV": (["--frontend-csv", str(missing / "absent-frontend.csv")], ("absent-frontend.csv",)),
            "wrong columns": (["--backend-csv", str(wrong_columns)], ("wrong.csv", "columns")),
            "missing spec folder": (["--spec-root", str(missing / "absent-specs")], ("absent-specs",)),
            "missing source folder": (["--source-root", str(missing / "absent-source")], ("absent-source",)),
        }
        for case, (override, named) in cases.items():
            with self.subTest(case=case):
                arguments = self.workspace.pyramid_arguments(frontend, backend) + override

                result = self.workspace.pyramid(*arguments)

                self.assert_refuses_to_judge(result, *named)
                self.assertNotIn(PYRAMID_TITLE, result.stdout, msg=self._explain(result))

    def test_a_csv_left_out_exits_2_naming_its_option(self):
        frontend, backend = self.steady_pyramid()
        arguments = self.workspace.pyramid_arguments(frontend, backend)
        for option in ("--frontend-csv", "--backend-csv"):
            with self.subTest(option=option):
                at = arguments.index(option)
                without = arguments[:at] + arguments[at + 2 :]

                result = self.workspace.pyramid(*without)

                self.assertEqual(result.returncode, 2, msg=self._explain(result))
                self.assertIn(option, result.stderr, msg=self._explain(result))
                self.assertNotIn("Traceback", result.stderr, msg=self._explain(result))


class TheCommandWritesNothing(PyramidTestCase):
    def test_the_command_writes_nothing_even_with_a_job_summary_named(self):
        frontend, backend = self.steady_pyramid()
        folder = tempfile.TemporaryDirectory()
        self.addCleanup(folder.cleanup)
        summary = Path(folder.name) / "step-summary.md"
        summary.write_text("## The job's own summary\n", encoding="utf-8")
        before = self.workspace.snapshot()

        result = self.workspace.pyramid(
            *self.workspace.pyramid_arguments(frontend, backend), summary=summary
        )

        self.assert_passes_quietly(result)
        self.pyramid_of(result)
        self.assertEqual(self.workspace.snapshot(), before)
        self.assertEqual(summary.read_text(encoding="utf-8"), "## The job's own summary\n")


if __name__ == "__main__":
    unittest.main()

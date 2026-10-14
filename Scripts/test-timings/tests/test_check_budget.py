import csv
import json
import subprocess
import sys
import tempfile
import unittest
from pathlib import Path

HERE = Path(__file__).resolve().parent
SCRIPTS = HERE.parent
sys.path.insert(0, str(SCRIPTS))

from trx_to_csv import CSV_COLUMNS as BACKEND_COLUMNS  # noqa: E402
from vitest_to_csv import CSV_COLUMNS as FRONTEND_COLUMNS  # noqa: E402

CHECK_BUDGET = SCRIPTS / "check_budget.py"
VITEST_TO_CSV = SCRIPTS / "vitest_to_csv.py"
TRX_TO_CSV = SCRIPTS / "trx_to_csv.py"

PENDING = unittest.skip("pending — DELIVER slice 06")

BUDGET_SECONDS = 60
ALLOWLIST_NAME = "test-time-budget.json"
TESTS_NAMESPACE = "Lighthouse.Backend.Tests.Services"
TRX_NS = "http://microsoft.com/schemas/VisualStudio/TeamTest/2010"


def allowed(unit, ceiling_seconds, reason="starts a real Postgres container"):
    return {"unit": unit, "reason": reason, "ceiling_seconds": ceiling_seconds}


class CheckWorkspace:
    """A throwaway folder holding the files the check reads, laid out as CI lays them out."""

    def __init__(self, root: Path):
        self.root = root
        self.allowlist = root / ALLOWLIST_NAME
        self.source_root = root / "Lighthouse.Backend.Tests"
        self.source_root.mkdir()

    def frontend_timings(self, seconds_per_test_by_file):
        rows = [
            (file, f"{file} test {index}", f"{seconds * 1000:.3f}", "Passed")
            for file, durations in seconds_per_test_by_file.items()
            for index, seconds in enumerate(durations)
        ]
        return self._write_csv("test-timings-frontend.csv", FRONTEND_COLUMNS, rows)

    def backend_timings(self, seconds_by_test):
        rows = [
            (name, "Unit", f"{seconds * 1000:.3f}", "Passed")
            for name, seconds in seconds_by_test.items()
        ]
        return self._write_csv("test-timings-backend.csv", BACKEND_COLUMNS, rows)

    def raw_timings(self, name, content):
        path = self.root / name
        path.write_text(content, encoding="utf-8")
        return path

    def budget(
        self,
        frontend_entries=(),
        backend_entries=(),
        frontend_budget=BUDGET_SECONDS,
        backend_budget=BUDGET_SECONDS,
    ):
        document = {
            "frontend": {
                "budget_seconds": frontend_budget,
                "allowlist": list(frontend_entries),
            },
            "backend": {
                "budget_seconds": backend_budget,
                "allowlist": list(backend_entries),
            },
        }
        return self.raw_allowlist(json.dumps(document, indent=2))

    def raw_allowlist(self, content):
        self.allowlist.write_text(content, encoding="utf-8")
        return self.allowlist

    def declare_test_class(
        self, class_name, folder="Services", namespace=TESTS_NAMESPACE, file_name=None
    ):
        directory = self.source_root / folder
        directory.mkdir(parents=True, exist_ok=True)
        (directory / (file_name or f"{class_name}.cs")).write_text(
            f"namespace {namespace};\n\npublic class {class_name}\n{{\n}}\n",
            encoding="utf-8",
        )

    def check(self, stack, timings, *options):
        command = [
            sys.executable,
            str(CHECK_BUDGET),
            "--stack",
            stack,
            "--csv",
            str(timings),
            "--allowlist",
            str(self.allowlist),
            *options,
        ]
        if stack == "backend":
            command += ["--source-root", str(self.source_root)]
        return subprocess.run(
            command, capture_output=True, text=True, cwd=self.root, timeout=60
        )

    def convert_vitest_report(self, report):
        frontend_root = self.root / "Lighthouse.Frontend"
        report_path = self.root / "test-results.json"
        report_path.write_text(json.dumps(report), encoding="utf-8")
        output = self.root / "test-timings-frontend.csv"
        subprocess.run(
            [
                sys.executable,
                str(VITEST_TO_CSV),
                "--input",
                str(report_path),
                "--source-root",
                str(frontend_root),
                "--output",
                str(output),
            ],
            check=True,
            timeout=60,
        )
        return output

    def convert_trx(self, trx_content):
        trx_path = self.root / "backend.trx"
        trx_path.write_text(trx_content, encoding="utf-8")
        output = self.root / "test-timings-backend.csv"
        subprocess.run(
            [
                sys.executable,
                str(TRX_TO_CSV),
                "--trx",
                str(trx_path),
                "--output",
                str(output),
            ],
            check=True,
            timeout=60,
        )
        return output

    def snapshot(self):
        return {
            str(path.relative_to(self.root)): path.read_bytes()
            for path in sorted(self.root.rglob("*"))
            if path.is_file()
        }

    def _write_csv(self, name, columns, rows):
        path = self.root / name
        with path.open("w", encoding="utf-8", newline="") as stream:
            writer = csv.writer(stream, lineterminator="\n")
            writer.writerow(columns)
            writer.writerows(rows)
        return path


class BudgetCheckTestCase(unittest.TestCase):
    def setUp(self):
        folder = tempfile.TemporaryDirectory()
        self.addCleanup(folder.cleanup)
        self.workspace = CheckWorkspace(Path(folder.name))

    def fresh_workspace(self):
        folder = tempfile.TemporaryDirectory()
        self.addCleanup(folder.cleanup)
        return CheckWorkspace(Path(folder.name))

    def annotations(self, result, kind):
        prefix = f"::{kind}::"
        return [line for line in result.stdout.splitlines() if line.startswith(prefix)]

    def assert_passes_quietly(self, result):
        self.assertEqual(result.returncode, 0, msg=self._explain(result))
        self.assertEqual(self.annotations(result, "error"), [], msg=self._explain(result))
        self.assertEqual(self.annotations(result, "warning"), [], msg=self._explain(result))

    def assert_fails_naming(self, result, *fragments):
        self._assert_one_annotation_names("error", result, fragments)
        self.assertEqual(result.returncode, 1, msg=self._explain(result))
        self.assertNotIn("Traceback", result.stderr, msg=self._explain(result))

    def assert_warns_and_passes(self, result, *fragments):
        self.assertEqual(result.returncode, 0, msg=self._explain(result))
        self.assertEqual(self.annotations(result, "error"), [], msg=self._explain(result))
        self._assert_one_annotation_names("warning", result, fragments)

    def assert_refuses_to_judge(self, result, *fragments):
        self._assert_one_annotation_names("error", result, fragments)
        self.assertEqual(result.returncode, 2, msg=self._explain(result))
        self.assertNotIn("Traceback", result.stderr, msg=self._explain(result))

    def _assert_one_annotation_names(self, kind, result, fragments):
        matching = [
            line
            for line in self.annotations(result, kind)
            if all(fragment in line for fragment in fragments)
        ]
        self.assertTrue(
            matching,
            msg=f"no ::{kind}:: line names all of {fragments}\n{self._explain(result)}",
        )

    @staticmethod
    def _explain(result):
        return f"exit {result.returncode}\nstdout:\n{result.stdout}\nstderr:\n{result.stderr}"


class AFrontendFileOverTheBudgetFailsTheBuild(BudgetCheckTestCase):
    def test_a_run_with_every_file_under_the_budget_passes_without_annotations(self):
        timings = self.workspace.frontend_timings(
            {"src/pages/Overview.test.tsx": [12.0, 8.5], "src/utils/dates.test.ts": [0.2]}
        )
        self.workspace.budget()

        self.assert_passes_quietly(self.workspace.check("frontend", timings))

    def test_a_file_exactly_at_the_budget_passes(self):
        timings = self.workspace.frontend_timings({"src/pages/Overview.test.tsx": [30.0, 30.0]})
        self.workspace.budget()

        self.assert_passes_quietly(self.workspace.check("frontend", timings))

    def test_a_file_over_the_budget_fails_naming_the_file_its_time_the_budget_and_the_allowlist(self):
        timings = self.workspace.frontend_timings(
            {"src/pages/RefinementView.paging.test.tsx": [40.0, 35.0]}
        )
        self.workspace.budget()

        result = self.workspace.check("frontend", timings)

        self.assert_fails_naming(
            result, "src/pages/RefinementView.paging.test.tsx", "75.0", "60", ALLOWLIST_NAME
        )

    def test_a_file_a_millisecond_over_the_budget_fails(self):
        timings = self.workspace.frontend_timings({"src/pages/Overview.test.tsx": [60.001]})
        self.workspace.budget()

        self.assert_fails_naming(
            self.workspace.check("frontend", timings), "src/pages/Overview.test.tsx"
        )

    def test_each_stack_is_held_to_the_budget_its_own_section_sets(self):
        cases = (
            ("frontend", 40.0, "fails"),
            ("frontend", 25.0, "passes"),
            ("backend", 40.0, "fails"),
            ("backend", 25.0, "passes"),
        )
        for stack, seconds, expected in cases:
            with self.subTest(stack=stack, seconds=seconds, expected=expected):
                workspace = self.fresh_workspace()
                if stack == "frontend":
                    unit = "src/pages/Overview.test.tsx"
                    timings = workspace.frontend_timings({unit: [seconds]})
                    workspace.budget(frontend_budget=30, backend_budget=90)
                else:
                    unit = f"{TESTS_NAMESPACE}.CacheTest"
                    timings = workspace.backend_timings({f"{unit}.Reads": seconds})
                    workspace.budget(frontend_budget=90, backend_budget=30)
                result = workspace.check(stack, timings)
                if expected == "fails":
                    self.assert_fails_naming(result, unit, "40.0", "30")
                else:
                    self.assert_passes_quietly(result)

    def test_a_file_is_judged_on_the_sum_of_its_tests_not_on_its_slowest_test(self):
        timings = self.workspace.frontend_timings(
            {"src/components/WorkItemsDialog.contexts.test.tsx": [25.0, 25.0, 25.0]}
        )
        self.workspace.budget()

        self.assert_fails_naming(
            self.workspace.check("frontend", timings),
            "src/components/WorkItemsDialog.contexts.test.tsx",
            "75.0",
        )

    def test_the_verdict_does_not_depend_on_how_a_files_time_is_split_across_its_tests(self):
        within = ([60.0], [30.0, 30.0], [20.0, 20.0, 20.0], [59.999, 0.001])
        over = ([61.0], [30.5, 30.5], [20.0, 20.0, 21.0], [0.001] * 10 + [60.0])
        for split in within:
            with self.subTest(split=split, expected="passes"):
                workspace = self.fresh_workspace()
                timings = workspace.frontend_timings({"src/pages/Split.test.tsx": split})
                workspace.budget()
                self.assert_passes_quietly(workspace.check("frontend", timings))
        for split in over:
            with self.subTest(split=split, expected="fails"):
                workspace = self.fresh_workspace()
                timings = workspace.frontend_timings({"src/pages/Split.test.tsx": split})
                workspace.budget()
                self.assert_fails_naming(
                    workspace.check("frontend", timings), "src/pages/Split.test.tsx"
                )

    def test_only_the_file_over_the_budget_is_named_when_others_are_under_it(self):
        timings = self.workspace.frontend_timings(
            {
                "src/pages/Fast.test.tsx": [10.0],
                "src/pages/Slow.test.tsx": [75.0],
                "src/pages/Close.test.tsx": [59.0],
            }
        )
        self.workspace.budget()

        result = self.workspace.check("frontend", timings)

        self.assert_fails_naming(result, "src/pages/Slow.test.tsx")
        self.assertEqual(len(self.annotations(result, "error")), 1, msg=self._explain(result))

    def test_a_file_name_containing_a_percent_sign_and_a_line_break_is_reported_on_one_escaped_line(self):
        timings = self.workspace.frontend_timings({"src/pages/100%\nDone.test.tsx": [75.0]})
        self.workspace.budget()

        result = self.workspace.check("frontend", timings)

        self.assert_fails_naming(result, "src/pages/100%25%0ADone.test.tsx", "75.0")
        self.assertEqual(len(self.annotations(result, "error")), 1, msg=self._explain(result))
        self.assertFalse(
            any(line.startswith("Done.test.tsx") for line in result.stdout.splitlines()),
            msg=self._explain(result),
        )

    def test_judging_a_run_leaves_every_file_in_the_folder_as_it_was(self):
        self.workspace.declare_test_class("CacheTest")
        timings = self.workspace.frontend_timings({"src/pages/Slow.test.tsx": [75.0]})
        self.workspace.budget()
        before = self.workspace.snapshot()

        result = self.workspace.check("frontend", timings)

        self.assert_fails_naming(result, "src/pages/Slow.test.tsx")
        self.assertEqual(self.workspace.snapshot(), before)


class AnAllowlistedUnitIsHeldToItsOwnCeiling(BudgetCheckTestCase):
    def test_an_allowlisted_file_under_its_own_ceiling_passes_although_it_is_over_the_budget(self):
        timings = self.workspace.frontend_timings({"src/pages/Heavy.test.tsx": [80.0]})
        self.workspace.budget(frontend_entries=[allowed("src/pages/Heavy.test.tsx", 90)])

        self.assert_passes_quietly(self.workspace.check("frontend", timings))

    def test_an_allowlisted_file_exactly_at_its_ceiling_passes(self):
        timings = self.workspace.frontend_timings({"src/pages/Heavy.test.tsx": [45.0, 45.0]})
        self.workspace.budget(frontend_entries=[allowed("src/pages/Heavy.test.tsx", 90)])

        self.assert_passes_quietly(self.workspace.check("frontend", timings))

    def test_an_allowlisted_file_over_its_own_ceiling_fails_naming_the_file_its_time_and_its_ceiling(self):
        timings = self.workspace.frontend_timings({"src/pages/Heavy.test.tsx": [95.0]})
        self.workspace.budget(frontend_entries=[allowed("src/pages/Heavy.test.tsx", 90)])

        self.assert_fails_naming(
            self.workspace.check("frontend", timings),
            "src/pages/Heavy.test.tsx",
            "95.0",
            "90",
            ALLOWLIST_NAME,
        )

    def test_a_frontend_allowlist_entry_for_a_file_no_longer_in_the_run_fails(self):
        timings = self.workspace.frontend_timings({"src/pages/Overview.test.tsx": [1.0]})
        self.workspace.budget(frontend_entries=[allowed("src/pages/Removed.test.tsx", 90)])

        self.assert_fails_naming(
            self.workspace.check("frontend", timings), "src/pages/Removed.test.tsx", ALLOWLIST_NAME
        )

    def test_a_backend_allowlist_entry_for_a_class_still_in_the_test_source_passes_when_the_class_did_not_run(self):
        self.workspace.declare_test_class("JiraConnectorTest")
        timings = self.workspace.backend_timings({f"{TESTS_NAMESPACE}.CacheTest.Reads": 1.0})
        self.workspace.budget(
            backend_entries=[allowed(f"{TESTS_NAMESPACE}.JiraConnectorTest", 120, "talks to a live Jira")]
        )

        self.assert_passes_quietly(self.workspace.check("backend", timings))

    def test_an_allowlisted_backend_class_is_held_to_its_own_ceiling(self):
        for seconds, expected in ((90.0, "passes"), (95.0, "fails")):
            with self.subTest(seconds=seconds, expected=expected):
                workspace = self.fresh_workspace()
                workspace.declare_test_class("DataProtectionKeyRingTest")
                timings = workspace.backend_timings(
                    {f"{TESTS_NAMESPACE}.DataProtectionKeyRingTest.SharesKeysThroughRedis": seconds}
                )
                workspace.budget(
                    backend_entries=[allowed(f"{TESTS_NAMESPACE}.DataProtectionKeyRingTest", 90)]
                )
                result = workspace.check("backend", timings)
                if expected == "fails":
                    self.assert_fails_naming(
                        result, f"{TESTS_NAMESPACE}.DataProtectionKeyRingTest", "95.0", "90"
                    )
                else:
                    self.assert_passes_quietly(result)

    def test_a_backend_class_counts_as_in_the_source_whatever_the_file_it_is_declared_in_is_called(self):
        self.workspace.declare_test_class("CacheTest", file_name="CacheScenarios.cs")
        timings = self.workspace.backend_timings({f"{TESTS_NAMESPACE}.OtherTest.Reads": 1.0})
        self.workspace.budget(backend_entries=[allowed(f"{TESTS_NAMESPACE}.CacheTest", 120)])

        self.assert_passes_quietly(self.workspace.check("backend", timings))

    def test_a_class_of_the_same_name_in_another_namespace_does_not_keep_an_entry_alive(self):
        self.workspace.declare_test_class("CacheTest", namespace="Lighthouse.Backend.Tests.Other")
        timings = self.workspace.backend_timings({f"{TESTS_NAMESPACE}.OtherTest.Reads": 1.0})
        self.workspace.budget(backend_entries=[allowed(f"{TESTS_NAMESPACE}.CacheTest", 120)])

        self.assert_fails_naming(
            self.workspace.check("backend", timings), f"{TESTS_NAMESPACE}.CacheTest"
        )

    def test_a_backend_allowlist_entry_for_a_class_no_longer_in_the_test_source_fails(self):
        timings = self.workspace.backend_timings({f"{TESTS_NAMESPACE}.CacheTest.Reads": 1.0})
        self.workspace.budget(
            backend_entries=[allowed(f"{TESTS_NAMESPACE}.DeletedFixtureTest", 120)]
        )

        self.assert_fails_naming(
            self.workspace.check("backend", timings),
            f"{TESTS_NAMESPACE}.DeletedFixtureTest",
            ALLOWLIST_NAME,
        )

    def test_a_class_left_only_in_build_or_mutation_output_does_not_count_as_still_in_the_source(self):
        for leftover in ("bin/Debug", "obj", ".stryker-tmp4711/Services", "StrykerOutput/2026-10-14"):
            with self.subTest(leftover=leftover):
                workspace = self.fresh_workspace()
                workspace.declare_test_class("DeletedFixtureTest", folder=leftover)
                timings = workspace.backend_timings({f"{TESTS_NAMESPACE}.CacheTest.Reads": 1.0})
                workspace.budget(
                    backend_entries=[allowed(f"{TESTS_NAMESPACE}.DeletedFixtureTest", 120)]
                )

                self.assert_fails_naming(
                    workspace.check("backend", timings), f"{TESTS_NAMESPACE}.DeletedFixtureTest"
                )


class AnAllowlistThatCannotBeTrustedIsRefused(BudgetCheckTestCase):
    def test_a_backend_test_source_folder_that_does_not_exist_fails_closed(self):
        timings = self.workspace.backend_timings({f"{TESTS_NAMESPACE}.CacheTest.Reads": 1.0})
        self.workspace.budget(backend_entries=[allowed(f"{TESTS_NAMESPACE}.CacheTest", 120)])
        self.workspace.source_root.rmdir()

        self.assert_refuses_to_judge(
            self.workspace.check("backend", timings), str(self.workspace.source_root)
        )

    def test_an_allowlist_entry_without_a_reason_is_rejected(self):
        for reason in (None, "", "   "):
            with self.subTest(reason=reason):
                workspace = self.fresh_workspace()
                timings = workspace.frontend_timings({"src/pages/Heavy.test.tsx": [80.0]})
                entry = {"unit": "src/pages/Heavy.test.tsx", "ceiling_seconds": 90}
                if reason is not None:
                    entry["reason"] = reason
                workspace.budget(frontend_entries=[entry])

                self.assert_refuses_to_judge(
                    workspace.check("frontend", timings), "src/pages/Heavy.test.tsx", "reason"
                )

    def test_an_allowlist_entry_without_a_usable_ceiling_is_rejected(self):
        for ceiling in (None, "lots", -5, 0, True):
            with self.subTest(ceiling=ceiling):
                workspace = self.fresh_workspace()
                timings = workspace.frontend_timings({"src/pages/Heavy.test.tsx": [80.0]})
                entry = {"unit": "src/pages/Heavy.test.tsx", "reason": "renders every chart"}
                if ceiling is not None:
                    entry["ceiling_seconds"] = ceiling
                workspace.budget(frontend_entries=[entry])

                self.assert_refuses_to_judge(
                    workspace.check("frontend", timings), "src/pages/Heavy.test.tsx", "ceiling"
                )

    def test_an_allowlist_that_is_not_valid_json_fails_closed(self):
        timings = self.workspace.frontend_timings({"src/pages/Overview.test.tsx": [1.0]})
        self.workspace.raw_allowlist('{"frontend": {"budget_seconds": 60,')

        self.assert_refuses_to_judge(self.workspace.check("frontend", timings), ALLOWLIST_NAME)

    def test_an_allowlist_without_a_section_for_the_stack_fails_closed(self):
        timings = self.workspace.frontend_timings({"src/pages/Overview.test.tsx": [1.0]})
        self.workspace.raw_allowlist(
            json.dumps({"backend": {"budget_seconds": 60, "allowlist": []}})
        )

        self.assert_refuses_to_judge(
            self.workspace.check("frontend", timings), ALLOWLIST_NAME, "frontend"
        )

    def test_an_allowlist_section_without_a_budget_fails_closed(self):
        timings = self.workspace.frontend_timings({"src/pages/Overview.test.tsx": [1.0]})
        self.workspace.raw_allowlist(json.dumps({"frontend": {"allowlist": []}}))

        self.assert_refuses_to_judge(
            self.workspace.check("frontend", timings), ALLOWLIST_NAME, "budget"
        )

    def test_a_missing_allowlist_file_fails_closed(self):
        timings = self.workspace.frontend_timings({"src/pages/Overview.test.tsx": [1.0]})

        self.assert_refuses_to_judge(self.workspace.check("frontend", timings), ALLOWLIST_NAME)


class ATimingFileThatCannotBeReadFailsClosed(BudgetCheckTestCase):
    def test_a_missing_timing_file_fails_closed_naming_the_file(self):
        self.workspace.budget()

        result = self.workspace.check("frontend", self.workspace.root / "test-timings-frontend.csv")

        self.assert_refuses_to_judge(result, "test-timings-frontend.csv")

    def test_an_empty_timing_file_fails_closed(self):
        timings = self.workspace.raw_timings("test-timings-frontend.csv", "")
        self.workspace.budget()

        self.assert_refuses_to_judge(
            self.workspace.check("frontend", timings), "test-timings-frontend.csv"
        )

    def test_a_timing_file_with_a_header_and_no_tests_fails_closed(self):
        timings = self.workspace.frontend_timings({})
        self.workspace.budget()

        self.assert_refuses_to_judge(
            self.workspace.check("frontend", timings), "test-timings-frontend.csv"
        )

    def test_a_timing_file_with_the_other_stacks_columns_fails_closed(self):
        timings = self.workspace.backend_timings({f"{TESTS_NAMESPACE}.CacheTest.Reads": 1.0})
        self.workspace.budget()

        self.assert_refuses_to_judge(
            self.workspace.check("frontend", timings), "test-timings-backend.csv"
        )

    def test_a_timing_file_with_an_unreadable_duration_fails_closed(self):
        header = ",".join(FRONTEND_COLUMNS)
        timings = self.workspace.raw_timings(
            "test-timings-frontend.csv",
            f"{header}\nsrc/pages/Overview.test.tsx,renders,not-a-number,Passed\n",
        )
        self.workspace.budget()

        self.assert_refuses_to_judge(
            self.workspace.check("frontend", timings), "test-timings-frontend.csv"
        )


class WarnOnlySoftensABreachButNothingElse(BudgetCheckTestCase):
    @PENDING
    def test_warn_only_reports_a_class_over_the_budget_as_a_warning_and_passes(self):
        timings = self.workspace.backend_timings(
            {f"{TESTS_NAMESPACE}.CacheTest.ConcurrentReadersAndWriters": 61.0}
        )
        self.workspace.budget()

        self.assert_warns_and_passes(
            self.workspace.check("backend", timings, "--warn-only"),
            f"{TESTS_NAMESPACE}.CacheTest",
            "61.0",
            "60",
            ALLOWLIST_NAME,
        )

    @PENDING
    def test_warn_only_reports_an_allowlisted_class_over_its_ceiling_as_a_warning_and_passes(self):
        self.workspace.declare_test_class("DataProtectionKeyRingTest")
        timings = self.workspace.backend_timings(
            {f"{TESTS_NAMESPACE}.DataProtectionKeyRingTest.SharesKeysThroughRedis": 50.0}
        )
        self.workspace.budget(
            backend_entries=[allowed(f"{TESTS_NAMESPACE}.DataProtectionKeyRingTest", 45)]
        )

        self.assert_warns_and_passes(
            self.workspace.check("backend", timings, "--warn-only"),
            f"{TESTS_NAMESPACE}.DataProtectionKeyRingTest",
            "50.0",
            "45",
        )

    @PENDING
    def test_warn_only_still_fails_on_a_missing_timing_file(self):
        for stack in ("frontend", "backend"):
            with self.subTest(stack=stack):
                workspace = self.fresh_workspace()
                workspace.budget()
                missing = workspace.root / f"test-timings-{stack}.csv"

                result = workspace.check(stack, missing, "--warn-only")

                self.assert_refuses_to_judge(result, f"test-timings-{stack}.csv")

    @PENDING
    def test_warn_only_still_fails_on_an_allowlist_entry_for_a_class_no_longer_in_the_source(self):
        timings = self.workspace.backend_timings({f"{TESTS_NAMESPACE}.CacheTest.Reads": 1.0})
        self.workspace.budget(
            backend_entries=[allowed(f"{TESTS_NAMESPACE}.DeletedFixtureTest", 120)]
        )

        self.assert_fails_naming(
            self.workspace.check("backend", timings, "--warn-only"),
            f"{TESTS_NAMESPACE}.DeletedFixtureTest",
            ALLOWLIST_NAME,
        )

    @PENDING
    def test_warn_only_still_fails_on_a_malformed_timing_file(self):
        timings = self.workspace.raw_timings("test-timings-backend.csv", "this is not a timing file\n")
        self.workspace.budget()

        self.assert_refuses_to_judge(
            self.workspace.check("backend", timings, "--warn-only"), "test-timings-backend.csv"
        )

    @PENDING
    def test_warn_only_still_fails_on_a_malformed_allowlist(self):
        timings = self.workspace.backend_timings({f"{TESTS_NAMESPACE}.CacheTest.Reads": 1.0})
        self.workspace.raw_allowlist("not json at all")

        self.assert_refuses_to_judge(
            self.workspace.check("backend", timings, "--warn-only"), ALLOWLIST_NAME
        )


class ABackendClassIsJudgedOnTheSumOfItsTests(BudgetCheckTestCase):
    def test_backend_tests_are_summed_per_class(self):
        timings = self.workspace.backend_timings(
            {
                f"{TESTS_NAMESPACE}.BlockedCountSnapshotMigrationTests.MigratesOnSqlite": 30.0,
                f"{TESTS_NAMESPACE}.BlockedCountSnapshotMigrationTests.MigratesOnPostgres": 40.0,
                f"{TESTS_NAMESPACE}.CacheTest.Reads": 59.0,
            }
        )
        self.workspace.budget()

        result = self.workspace.check("backend", timings)

        self.assert_fails_naming(
            result, f"{TESTS_NAMESPACE}.BlockedCountSnapshotMigrationTests", "70.0"
        )
        self.assertEqual(len(self.annotations(result, "error")), 1, msg=self._explain(result))

    def test_a_class_is_named_without_its_method_or_arguments_even_when_the_arguments_contain_dots(self):
        embed_tests = "Lighthouse.Backend.Tests.API.S8_EmbedEntryPointTests"
        timings = self.workspace.backend_timings(
            {
                f'{embed_tests}.ReturnPathPointsOffHost("https://evil.example/steal")': 40.0,
                f'{embed_tests}.ReturnPathPointsOffHost("//evil.example")': 15.0,
                f'{embed_tests}.ReturnPathPointsOffHost("a(b.c")': 5.0,
                f'{embed_tests}.ReturnPathPointsOffHost("x).y")': 5.0,
            }
        )
        self.workspace.budget()

        result = self.workspace.check("backend", timings)

        self.assert_fails_naming(result, embed_tests, "65.0")
        self.assertNotIn("evil.example", "\n".join(self.annotations(result, "error")))
        self.assertNotIn("ReturnPathPointsOffHost", "\n".join(self.annotations(result, "error")))


class TheCheckJudgesWhatTheConvertersWrite(BudgetCheckTestCase):
    def test_a_frontend_timing_file_written_by_the_vitest_converter_is_judged_end_to_end(self):
        frontend_root = self.workspace.root / "Lighthouse.Frontend"
        report = {
            "testResults": [
                {
                    "name": str(frontend_root / "src/pages/Slow.test.tsx"),
                    "assertionResults": [
                        {"fullName": "Slow pages one page", "status": "passed", "duration": 40000.0},
                        {"fullName": "Slow pages the next page", "status": "passed", "duration": 35000.0},
                    ],
                },
                {
                    "name": str(frontend_root / "src/pages/Fast.test.tsx"),
                    "assertionResults": [
                        {"fullName": "Fast renders", "status": "passed", "duration": 120.0},
                        {"fullName": "Fast is pending", "status": "skipped", "duration": None},
                    ],
                },
            ]
        }
        timings = self.workspace.convert_vitest_report(report)
        self.workspace.budget()

        result = self.workspace.check("frontend", timings)

        self.assert_fails_naming(result, "src/pages/Slow.test.tsx", "75.0", "60", ALLOWLIST_NAME)
        self.assertNotIn("Fast.test.tsx", "\n".join(self.annotations(result, "error")))

    def test_a_backend_timing_file_written_by_the_trx_converter_is_judged_per_class(self):
        trx = f"""<?xml version="1.0" encoding="utf-8"?>
<TestRun id="run" name="run" xmlns="{TRX_NS}">
  <Results>
    <UnitTestResult executionId="e1" testId="t1" testName="ReturnPathPointsOffHost(&quot;https://evil.example/steal&quot;)" duration="00:00:50.0000000" outcome="Passed" />
    <UnitTestResult executionId="e2" testId="t2" testName="ReturnPathPointsOffHost(&quot;/home&quot;)" duration="00:00:20.0000000" outcome="Passed" />
    <UnitTestResult executionId="e3" testId="t3" testName="Reads" duration="00:00:01.0000000" outcome="Passed" />
  </Results>
  <TestDefinitions>
    <UnitTest name="ReturnPathPointsOffHost" id="t1">
      <TestMethod className="Lighthouse.Backend.Tests.API.S8_EmbedEntryPointTests" name="ReturnPathPointsOffHost" />
    </UnitTest>
    <UnitTest name="ReturnPathPointsOffHost" id="t2">
      <TestMethod className="Lighthouse.Backend.Tests.API.S8_EmbedEntryPointTests" name="ReturnPathPointsOffHost" />
    </UnitTest>
    <UnitTest name="Reads" id="t3">
      <TestMethod className="{TESTS_NAMESPACE}.CacheTest" name="Reads" />
    </UnitTest>
  </TestDefinitions>
</TestRun>
"""
        timings = self.workspace.convert_trx(trx)
        self.workspace.budget()

        result = self.workspace.check("backend", timings)

        self.assert_fails_naming(
            result, "Lighthouse.Backend.Tests.API.S8_EmbedEntryPointTests", "70.0"
        )
        self.assertEqual(len(self.annotations(result, "error")), 1, msg=self._explain(result))


if __name__ == "__main__":
    unittest.main()

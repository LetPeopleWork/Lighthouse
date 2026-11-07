import contextlib
import io
import json
import shutil
import sys
import unittest
from pathlib import Path

HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE.parent))

from summarise import _render_summary, gather, render_top_n  # noqa: E402

FIXTURES = HERE / "fixtures"


class GatherTests(unittest.TestCase):
    def setUp(self):
        self.tmp = HERE / "_tmp_summarise"
        if self.tmp.exists():
            shutil.rmtree(self.tmp)
        self.tmp.mkdir()
        shutil.copy(FIXTURES / "sample.trx", self.tmp / "sample.trx")
        shutil.copy(
            FIXTURES / "sample-vitest.json",
            self.tmp / "vitest-results.json",
        )

    def tearDown(self):
        shutil.rmtree(self.tmp, ignore_errors=True)

    def test_picks_up_trx_files(self):
        rows = gather([self.tmp])
        backend = [row for row in rows if row["stack"] == "Backend"]
        self.assertGreater(len(backend), 0)

    def test_picks_up_vitest_json_files(self):
        rows = gather([self.tmp])
        frontend = [row for row in rows if row["stack"] == "Frontend"]
        self.assertGreater(len(frontend), 0)

    def test_ignores_unrelated_json_files(self):
        without = gather([self.tmp])
        (self.tmp / "package.json").write_text(
            json.dumps({"name": "not-a-test-result"})
        )
        self.assertEqual(len(gather([self.tmp])), len(without))

    def test_a_class_with_no_declaration_does_not_read_as_a_fast_test(self):
        source_root = self.tmp / "empty-source-tree"
        source_root.mkdir()
        with contextlib.redirect_stderr(io.StringIO()):
            rows = gather([self.tmp / "sample.trx"], source_root=source_root)
        self.assertEqual({row["category_or_file"] for row in rows}, {"Unplaced"})


class RenderTopNTests(unittest.TestCase):
    def test_renders_a_table_sorted_by_duration_descending(self):
        rows = [
            {
                "stack": "Backend",
                "name": "Slow.Test",
                "category_or_file": "Integration",
                "duration_ms": 9999.0,
                "outcome": "Passed",
            },
            {
                "stack": "Frontend",
                "name": "fast spec",
                "category_or_file": "src/foo.test.tsx",
                "duration_ms": 12.0,
                "outcome": "Passed",
            },
        ]
        output = render_top_n(rows, n=5)
        first_row_line = [
            line
            for line in output.splitlines()
            if "Slow.Test" in line or "fast spec" in line
        ]
        self.assertGreater(len(first_row_line), 0)
        self.assertIn("Slow.Test", first_row_line[0])

    def test_truncates_to_n_data_rows(self):
        rows = [
            {
                "stack": "Backend",
                "name": f"T{i}",
                "category_or_file": "Unit",
                "duration_ms": float(i),
                "outcome": "Passed",
            }
            for i in range(30)
        ]
        output = render_top_n(rows, n=5)
        body_lines = [
            line
            for line in output.splitlines()
            if " Passed " in line and "ms" in line
        ]
        self.assertEqual(len(body_lines), 5)

    def test_renders_empty_message_when_no_rows(self):
        output = render_top_n([], n=20)
        self.assertIn("No timing data found", output)


class SummaryProfileTests(unittest.TestCase):
    def setUp(self):
        self.tmp = HERE / "_tmp_summarise_profile"
        if self.tmp.exists():
            shutil.rmtree(self.tmp)
        self.tmp.mkdir()
        shutil.copy(FIXTURES / "sample.trx", self.tmp / "sample.trx")
        shutil.copy(FIXTURES / "sample-vitest.json", self.tmp / "vitest-results.json")
        self.summary = _render_summary(gather([self.tmp])).splitlines()

    def tearDown(self):
        shutil.rmtree(self.tmp, ignore_errors=True)

    def test_keeps_the_existing_stack_lines_first(self):
        self.assertTrue(self.summary[0].startswith("Backend  :"))
        self.assertTrue(self.summary[1].startswith("Frontend :"))

    def test_reports_the_share_of_time_held_by_the_slowest_files(self):
        self.assertIn(
            "Slowest 10 frontend files : 100% of summed time (1.7s)", self.summary
        )
        self.assertIn(
            "Slowest 20 frontend files : 100% of summed time (1.7s)", self.summary
        )
        self.assertIn(
            "Slowest 50 frontend files : 100% of summed time (1.7s)", self.summary
        )

    def test_says_what_the_integration_column_counts(self):
        rows = [
            {
                "stack": "Backend",
                "name": "Hosted.Test",
                "category_or_file": "Integration",
                "duration_ms": 2000.0,
                "outcome": "Passed",
            },
            {
                "stack": "Backend",
                "name": "Fast.Test",
                "category_or_file": "Unit",
                "duration_ms": 1000.0,
                "outcome": "Passed",
            },
        ]
        self.assertIn(
            "(1 hosted, container or live, 2.00s)",
            _render_summary(rows).splitlines()[0],
        )

    def test_keeps_backend_categories_out_of_the_file_shares(self):
        rows = [
            {
                "stack": "Backend",
                "name": "Big.Backend.Test",
                "category_or_file": "Integration",
                "duration_ms": 90000.0,
                "outcome": "Passed",
            },
            {
                "stack": "Frontend",
                "name": "only frontend",
                "category_or_file": "src/a.test.tsx",
                "duration_ms": 1000.0,
                "outcome": "Passed",
            },
        ]
        summary = _render_summary(rows).splitlines()
        self.assertIn(
            "Slowest 10 frontend files : 100% of summed time (1.0s)", summary
        )

    def test_reports_counts_and_seconds_of_tests_over_each_threshold(self):
        self.assertIn("Tests > 0.1s : {:>5} ({:.1f}s)".format(7, 6.5), self.summary)
        self.assertIn("Tests > 0.5s : {:>5} ({:.1f}s)".format(4, 5.6), self.summary)
        self.assertIn("Tests > 1s   : {:>5} ({:.1f}s)".format(2, 4.0), self.summary)
        self.assertIn("Tests > 2s   : {:>5} ({:.1f}s)".format(1, 2.5), self.summary)
        self.assertIn("Tests > 5s   : {:>5} ({:.1f}s)".format(0, 0.0), self.summary)


if __name__ == "__main__":
    unittest.main()

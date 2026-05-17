import io
import json
import shutil
import sys
import unittest
from pathlib import Path

HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE.parent))

from summarise import gather, render_top_n  # noqa: E402

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
        (self.tmp / "package.json").write_text(
            json.dumps({"name": "not-a-test-result"})
        )
        rows = gather([self.tmp])
        frontend = [row for row in rows if row["stack"] == "Frontend"]
        for row in frontend:
            self.assertNotEqual(row["name"], "")


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


if __name__ == "__main__":
    unittest.main()

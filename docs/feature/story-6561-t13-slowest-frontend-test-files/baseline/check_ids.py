"""Compares the test ids of an after-run with the union of the baseline ids over every touched file.

usage (from the repository root):
  check_ids.py --before <sha> --list-files
      prints the test files an after-run must contain: every file with a baseline id list, every test file changed
      since <sha> and every new test file not tracked yet (paths relative to Lighthouse.Frontend, one per line)
  check_ids.py --before <sha> --moved <moved-assertions.md> --report <test-results.json> --started <epoch> \
      [--expected-tests N]
      fails (exit 1) when
        - the report is older than the run, or its count differs from the run's own summary (exit 2),
        - a touched file is missing from the report (an after-run that left out a new file),
        - a test in the report did not pass (skipped, todo, failed),
        - a baseline id is missing from the report and has no row in the "Moved assertions" table of the moved file,
        - an id in the report is not in the baseline and is not listed under "Added ids" of the moved file.

This pins test NAMES, not assertions. An assertion lost inside a test that survives under the same name is not seen
here; the removed-`expect(` rows checked by check_behaviour_free.py are the control for that.
"""
import argparse
import glob
import json
import os
import subprocess
import sys

FRONTEND = "Lighthouse.Frontend"
SRC = f"{FRONTEND}/src"
TESTS = [f":(glob){SRC}/**/*.test.ts", f":(glob){SRC}/**/*.test.tsx"]
HERE = os.path.dirname(os.path.abspath(__file__))
BASELINE_DIRS = [HERE, os.path.join(HERE, "outside-seven")]


def git(*args):
    done = subprocess.run(["git", *args], capture_output=True, text=True, encoding="utf-8")
    if done.returncode != 0:
        sys.exit(f"git {' '.join(args)} failed: {done.stderr.strip()}")
    return done.stdout


def baseline_ids():
    ids = set()
    suites = set()
    for directory in BASELINE_DIRS:
        for path in glob.glob(os.path.join(directory, "*.ids.txt")):
            with open(path, encoding="utf-8") as handle:
                for line in handle:
                    line = line.rstrip("\n")
                    if line:
                        ids.add(line)
                        suites.add(line.split(" :: ")[0])
    return ids, suites


def touched_files(before):
    changed = git("diff", "--no-renames", "--name-only", "--diff-filter=ACMR", before, "--", *TESTS).splitlines()
    untracked = git("ls-files", "-o", "--exclude-standard", "--", *TESTS).splitlines()
    return sorted({f[len(FRONTEND) + 1:] for f in changed + untracked if f})


def rows_and_added(moved_path):
    old_ids, added = set(), set()
    section = ""
    with open(moved_path, encoding="utf-8") as handle:
        for line in handle:
            if line.startswith("## "):
                section = line[3:].strip().lower()
            elif section.startswith("moved assertions") and line.lstrip().startswith("|"):
                first = line.strip().strip("|").split("|")[0].strip().strip("`")
                if " :: " in first and not first.startswith("Old id"):
                    old_ids.add(first)
            elif section.startswith("added ids"):
                text = line.strip().lstrip("-").strip().strip("`")
                if " :: " in text:
                    added.add(text)
    return old_ids, added


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--before", required=True)
    parser.add_argument("--list-files", action="store_true")
    parser.add_argument("--moved")
    parser.add_argument("--report")
    parser.add_argument("--started", type=float)
    parser.add_argument("--expected-tests", type=int)
    args = parser.parse_args()

    before_ids, baseline_suites = baseline_ids()
    touched = touched_files(args.before)
    must_run = sorted(f for f in set(touched) | baseline_suites if os.path.exists(os.path.join(FRONTEND, f)))

    if args.list_files:
        print("\n".join(must_run))
        return 0
    for needed in ("moved", "report", "started"):
        if getattr(args, needed) is None:
            sys.exit(f"--{needed} is required")

    with open(args.report, encoding="utf-8") as handle:
        report = json.load(handle)
    if os.path.getmtime(args.report) < args.started or report.get("startTime", 0) / 1000 < args.started - 1:
        print(f"REFUSED: {args.report} is older than the run")
        return 2
    counted = sum(len(s["assertionResults"]) for s in report["testResults"])
    if counted != report["numTotalTests"] or (args.expected_tests is not None and counted != args.expected_tests):
        print(f"REFUSED: report has {counted} tests, report says {report['numTotalTests']}, "
              f"run printed {args.expected_tests}")
        return 2

    failures = []
    after_ids, not_passed, suites_run = set(), [], set()
    for suite in report["testResults"]:
        relative = os.path.relpath(suite["name"], os.path.abspath(FRONTEND))
        suites_run.add(relative)
        for test in suite["assertionResults"]:
            identifier = f"{relative} :: {test['fullName']}"
            after_ids.add(identifier)
            if test["status"] != "passed":
                not_passed.append(f"{test['status']}: {identifier}")

    for path in must_run:
        if path not in suites_run:
            failures.append(f"touched or baseline file not in the after-run: {path}")
    for line in not_passed:
        failures.append(f"did not pass ({line})")

    moved_old, moved_added = rows_and_added(args.moved)
    missing = sorted(before_ids - after_ids)
    new = sorted(after_ids - before_ids)
    for identifier in missing:
        if identifier not in moved_old:
            failures.append(f"baseline id gone and no row: {identifier}")
    for identifier in new:
        if identifier not in moved_added:
            failures.append(f"new id not listed under 'Added ids': {identifier}")
    for identifier in sorted(moved_old - set(missing)):
        print(f"note: a row names an id that is still present: {identifier}")

    print(f"baseline ids {len(before_ids)}, after-run ids {len(after_ids)}, missing {len(missing)}, new {len(new)}, "
          f"files that must be in the run {len(must_run)}")
    if failures:
        print("FAIL:")
        for failure in failures:
            print(f"  {failure}")
        return 1
    print("OK: every missing id has a row, every new id is listed, every touched file ran and passed")
    return 0


if __name__ == "__main__":
    sys.exit(main())

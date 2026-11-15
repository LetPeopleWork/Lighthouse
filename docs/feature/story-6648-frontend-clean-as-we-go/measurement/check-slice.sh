#!/usr/bin/env bash
# Usage (from the repository root): check-slice.sh <slice id>
#
# Checks that the rewrite of a slice lost no rule and changed no behaviour. Reads slice-<id>-moved-assertions.md:
#  - the test ids of the page file at the before commit are exactly the ids of its rows;
#  - one run of the page file and the direct test files passes, and the page file holds exactly the kept rows;
#  - every removed row names tests that exist in that run and passed, and the direct tests are exactly the
#    "Added ids" list;
#  - the page file and the direct test files carry no skip, only, todo, retry or timeout, and the lines added to the
#    page file carry no weaker matcher;
#  - no production file, no shared test kit, no time-budget file and no end-to-end file differs from the before
#    commit, except the files the slice names under "Production allow-list".
# Last line printed is the gate line: exit 0 only when no assertion failed and no setup failed.
set -u

slice="${1:?usage: check-slice.sh <slice id>}"
here="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
root="$(git -C "$here" rev-parse --show-toplevel)"
cd "$root" || exit 3
data="$here/slice-$slice-moved-assertions.md"
frontend="Lighthouse.Frontend"

gate() { echo "gate: slice=$slice observed=$1 assertions_failed=$2 setup_failed=$3 verdict=$4"; }
[ -f "$data" ] || { echo "no data file $data"; gate 0 0 1 setup-failed; exit 3; }

work="$(mktemp -d)"
trap 'rm -rf "$work"' EXIT

field() { sed -n "s/^- $1: *//p" "$data" | head -1 | sed 's/[[:space:]]*$//'; }
before_sha="$(field "Before commit")"
page="$(field "Page test file")"
direct_list="$(field "Direct test files")"
allow="$(field "Production allow-list")"

started="$(date +%s)"
report="$work/report.json"
rm -f "$frontend/test-results.json"
# shellcheck disable=SC2086
pnpm --dir "$frontend" exec cross-env TZ=Europe/Zurich vitest run \
	"$page" $direct_list >"$work/run.log" 2>&1
mv -f "$frontend/test-results.json" "$report" 2>/dev/null

python3 -I - "$data" "$report" "$started" "$before_sha" "$page" "$direct_list" "$allow" "$frontend" "$work/result" <<'PY'
import json, os, re, subprocess, sys

data, report_path, started, before_sha, page, direct_list, allow, frontend = sys.argv[1:9]
failures = []
setup = []


def git(*args):
    done = subprocess.run(["git", *args], capture_output=True, text=True, encoding="utf-8")
    if done.returncode != 0:
        setup.append(f"git {' '.join(args)}: {done.stderr.strip()}")
        return ""
    return done.stdout


rows, added, section = [], [], ""
for line in open(data, encoding="utf-8"):
    if line.startswith("## "):
        section = line[3:].strip().lower()
    elif section == "moved assertions" and line.startswith("| src/"):
        cells = [c.strip() for c in line.strip().strip("|").split("|")]
        rows.append((cells[0], cells[1], [n.strip() for n in cells[3].split(";;") if n.strip()]))
    elif section == "added ids" and line.startswith("- "):
        added.append(line[2:].strip())

# The ids of the page file at the before commit, read from its source.
source = git("show", f"{before_sha}:{frontend}/{page}")
before_ids = set()
for describe in re.finditer(r'^describe\("([^"]+)", \(\) => \{$(.*?)^\}\);$', source, re.S | re.M):
    for test in re.finditer(r"^\tit\(`([^`]+)`", describe.group(2), re.M):
        before_ids.add(f"{page} :: {describe.group(1)} {test.group(1)}")
row_ids = {r[0] for r in rows}
for gone in sorted(before_ids - row_ids):
    failures.append(f"test of the before commit with no row: {gone}")
for phantom in sorted(row_ids - before_ids):
    failures.append(f"row for a test the before commit does not have: {phantom}")

observed = 0
if not os.path.exists(report_path) or os.path.getmtime(report_path) < float(started) - 1:
    setup.append("the vitest run left no fresh JSON report")
else:
    report = json.load(open(report_path))
    status = {}
    for suite in report["testResults"]:
        relative = os.path.relpath(suite["name"], os.path.abspath(frontend))
        for test in suite["assertionResults"]:
            status[f"{relative} :: {test['fullName']}"] = test["status"]
    observed = len(status)
    for identifier, state in sorted(status.items()):
        if state != "passed":
            failures.append(f"did not pass ({state}): {identifier}")
    page_ids = {i for i in status if i.startswith(page + " :: ")}
    kept = {r[0] for r in rows if r[1] == "kept"}
    for extra in sorted(page_ids - kept):
        failures.append(f"page test that is not a kept row: {extra}")
    for lost in sorted(kept - page_ids):
        failures.append(f"kept row whose test is not in the page file: {lost}")
    for old, disposition, news in rows:
        if disposition == "removed":
            if not news:
                failures.append(f"removed row with no new test: {old}")
            for new in news:
                if new not in status:
                    failures.append(f"removed row names a test that does not exist: {new}")
    direct_ids = {i for i in status if not i.startswith(page + " :: ")}
    for unlisted in sorted(direct_ids - set(added)):
        failures.append(f"direct test not listed under Added ids: {unlisted}")
    for missing in sorted(set(added) - direct_ids):
        failures.append(f"Added ids names a test the run does not have: {missing}")

# No skip, only, todo, retry or timeout anywhere in the slice's test files; no weaker matcher added to the page file.
BANNED = re.compile(r"\.skip\b|\.only\b|\.todo\b|\bskipIf\b|\brunIf\b|it\.fails|\bxit\(|\bxdescribe\(|\{ *timeout *:|testTimeout|\bretry *:\s*[1-9]|setConfig|, *[0-9]{4,} *\)")
WEAKER = re.compile(r"toMatchObject|objectContaining|toBeGreaterThan|toBeLessThan|toContain\(|toMatch\(|toBeTruthy|toBeDefined")
for path in [page, *direct_list.split()]:
    for number, text in enumerate(open(os.path.join(frontend, path), encoding="utf-8"), 1):
        if BANNED.search(text):
            failures.append(f"{path}:{number}: skip, only, todo, retry or timeout: {text.strip()}")
diff = git("diff", "--no-renames", "-U0", before_sha, "--", f"{frontend}/{page}")
for text in diff.splitlines():
    if text.startswith("+") and not text.startswith("+++") and WEAKER.search(text):
        failures.append(f"weaker matcher added to the page file: {text[1:].strip()}")

# Nothing beyond the tests changes.
SRC = f"{frontend}/src"
allowed = set() if allow in ("", "none") else {f"{frontend}/{p}" for p in allow.split()}
changed = git("diff", "--no-renames", "--name-only", before_sha, "--", SRC).splitlines()
changed += git("ls-files", "-o", "--exclude-standard", "--", SRC).splitlines()
FROZEN = {
    f"{SRC}/tests/BulkReportsTestKit.tsx",
    f"{SRC}/tests/RefinementTabTestKit.tsx",
    f"{SRC}/tests/timeBudget/slowTests.ts",
    f"{frontend}/test-time-budget.json",
}
for path in sorted(set(changed)):
    is_test = re.search(r"\.test\.tsx?$", path)
    if path in FROZEN or (not is_test and f"{SRC}/tests/" not in path and path not in allowed):
        failures.append(f"file changed that the slice may not change: {path}")
    elif f"{SRC}/tests/" in path and not is_test and path not in FROZEN:
        failures.append(f"shared test support changed: {path}")
for path in git("diff", "--no-renames", "--name-only", before_sha, "--", "Lighthouse.EndToEndTests").splitlines():
    failures.append(f"end-to-end file changed: {path}")

for message in setup:
    print(f"setup: {message}")
for message in failures:
    print(f"fail: {message}")
open(sys.argv[9], "w").write(f"{observed} {len(failures)} {len(setup)}\n")
PY
read -r observed failed setup_failed <"$work/result" || { gate 0 0 1 setup-failed; exit 3; }
if [ "$setup_failed" -gt 0 ]; then gate "$observed" "$failed" 1 setup-failed; exit 3; fi
if [ "$failed" -gt 0 ]; then gate "$observed" "$failed" 0 failed; exit 1; fi
gate "$observed" 0 0 pass

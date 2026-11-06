#!/usr/bin/env bash
# Usage (from the repository root):
#   run-touched-files.sh <before-commit> <slice-X-moved-assertions.md>
# Runs EVERY file an after-run needs (baseline files, changed and new test files), with the report deleted first so a
# stale one cannot be read, then checks the ids against the baseline. The exit status is the id check's. Vitest's own
# status is printed beside it: it also goes non-zero when the per-test time guard names a slow test, and on a loaded
# machine that is a timing reading, to be judged by the slice's own measurement rather than by this script.
set -u
here="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
before="${1:?before-commit required}"
moved="${2:?moved-assertions file required}"
files=$(python3 -I "$here/check_ids.py" --before "$before" --list-files) || exit 1
rm -f Lighthouse.Frontend/test-results.json
started=$(date +%s)
log=$(mktemp)
(cd Lighthouse.Frontend && LIGHTHOUSE_TEST_WORKERS=2 pnpm exec cross-env TZ=Europe/Zurich vitest run $files) | tee "$log"
vitest_status=${PIPESTATUS[0]}
count=$(sed 's/\x1b\[[0-9;]*m//g' "$log" | grep -E '^ *Tests ' | grep -oE '\([0-9]+\)' | tr -d '()' | tail -1)
rm -f "$log"
echo "vitest exit status: $vitest_status; tests printed by the run: ${count:-unknown}"
python3 -I "$here/check_ids.py" --before "$before" --moved "$moved" \
  --report Lighthouse.Frontend/test-results.json --started "$started" ${count:+--expected-tests "$count"}

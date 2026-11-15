#!/usr/bin/env bash
# Usage (from the repository root): deliberate-breaks.sh <slice id>
#
# Proves that each moved rule can fail. For every line of slice-<id>-breaks.tsv (production file, sed expression,
# test file, test name pattern, what is broken) it applies the sed expression to the production file, runs the named
# tests, expects at least one of them to fail, and puts the file back with git checkout. It refuses to start when a
# production file named in the list already differs from the index, and ends by checking that none of them differs.
# Last line printed is the gate line: observed = break lines run, assertions_failed = breaks whose tests stayed
# green, setup_failed = lines that could not be applied or run. Exit 0 only when every break went red.
set -u

slice="${1:?usage: deliberate-breaks.sh <slice id>}"
here="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
root="$(git -C "$here" rev-parse --show-toplevel)"
cd "$root" || exit 3
list="$here/slice-$slice-breaks.tsv"
frontend="Lighthouse.Frontend"

observed=0
survived=0
setup_failed=0
gate() { echo "gate: slice=$slice observed=$observed assertions_failed=$survived setup_failed=$setup_failed went_red=$((observed - survived - setup_failed)) verdict=$1"; }

[ -f "$list" ] || { echo "no break list $list"; setup_failed=1; gate setup-failed; exit 3; }

work="$(mktemp -d)"
touched=()
restore() {
	for file in "${touched[@]}"; do
		git checkout -- "$file" 2>/dev/null
	done
	rm -rf "$work"
}
trap restore EXIT
trap 'exit 130' INT TERM

while IFS=$'\t' read -r file expression tests pattern what; do
	case "$file" in '' | '#'*) continue ;; esac
	if ! git diff --quiet -- "$file" || ! git diff --cached --quiet -- "$file"; then
		echo "refused: $file already differs from the index"
		setup_failed=1
		gate setup-failed
		exit 3
	fi
done <"$list"

while IFS=$'\t' read -r file expression tests pattern what; do
	case "$file" in '' | '#'*) continue ;; esac
	observed=$((observed + 1))
	touched+=("$file")
	sed -i -e "$expression" "$file"
	if git diff --quiet -- "$file"; then
		echo "break $observed ($what): the sed expression changed nothing in $file"
		setup_failed=$((setup_failed + 1))
		continue
	fi
	out="$work/break-$observed.json"
	rm -f "$frontend/test-results.json"
	started="$(date +%s)"
	pnpm --dir "$frontend" exec cross-env TZ=Europe/Zurich vitest run \
		-t "$pattern" "$tests" >"$work/break-$observed.log" 2>&1 </dev/null
	mv -f "$frontend/test-results.json" "$out" 2>/dev/null
	git checkout -- "$file"
	result="$(python3 -I -c '
import json, os, sys
path, started = sys.argv[1], float(sys.argv[2])
if not os.path.exists(path) or os.path.getmtime(path) < started - 1:
    print("none 0 0"); sys.exit(0)
report = json.load(open(path))
ran = failed = 0
for suite in report["testResults"]:
    for test in suite["assertionResults"]:
        if test["status"] != "skipped":
            ran += 1
        failed += test["status"] == "failed"
print("ok", ran, failed)
' "$out" "$started")"
	read -r state ran failed <<<"$result"
	if [ "$state" != "ok" ] || [ "$ran" -eq 0 ]; then
		echo "break $observed ($what): no test of '$pattern' ran in $tests"
		setup_failed=$((setup_failed + 1))
	elif [ "$failed" -eq 0 ]; then
		echo "break $observed ($what): $ran test(s) stayed green - the rule is not proven"
		survived=$((survived + 1))
	else
		echo "break $observed ($what): $failed of $ran test(s) went red"
	fi
done <"$list"

dirty=0
for file in "${touched[@]}"; do
	git diff --exit-code --quiet -- "$file" || { echo "NOT RESTORED: $file"; dirty=1; }
done
if [ "$dirty" -eq 1 ]; then
	setup_failed=$((setup_failed + 1))
	gate not-restored
	exit 3
fi
if [ "$setup_failed" -gt 0 ]; then gate setup-failed; exit 3; fi
if [ "$survived" -gt 0 ]; then gate break-survived; exit 1; fi
gate pass
exit 0

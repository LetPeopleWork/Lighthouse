#!/usr/bin/env bash
# Usage (from the repository root): measure-slice.sh <slice id>, for example S1.
#
# Judges how much faster a rewritten page test file is, per test, than the same file at the before commit.
# What belongs to the slice (before commit, page test file, direct test files, the two ratio limits) is read from
# slice-<id>-moved-assertions.md, section "Measure". This script has no slice-specific fact in it.
#
# Each pair runs the before-commit copy of the page file (an untracked copy next to the real file, deleted on exit),
# the rewritten page file, and the direct test files, one vitest run each, in an order that alternates from pair to
# pair. A pair counts only when every 1-minute load-average sample taken before and after each of its runs is at or
# under half the cores and every test passed. The script waits for a quiet machine up to a bounded time and then
# stops: fewer than 3 valid pairs is printed as unmeasured, never as a pass. The ratio is the median, over valid pairs,
# of the summed per-test seconds (assertionResults duration of the JSON report) after over before; the file-alone
# ratio uses the page file only, the combined ratio adds the direct test files.
#
# Optional environment, for diagnosis only: MEASURE_PAIRS (default 5), MEASURE_WAIT_SECONDS (default 900),
# MEASURE_MAX_ATTEMPTS (default 12). The load limit and the 3-pair minimum cannot be changed.
#
# Last line printed is the gate line. Exit 0 only on a pass, 1 on a failed assertion (a test or a ratio),
# 2 when unmeasured, 3 when the setup failed.
set -u

slice="${1:?usage: measure-slice.sh <slice id>}"
here="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
root="$(git -C "$here" rev-parse --show-toplevel)"
cd "$root" || exit 3
data="$here/slice-$slice-moved-assertions.md"
frontend="Lighthouse.Frontend"

pairs="${MEASURE_PAIRS:-5}"
wait_seconds="${MEASURE_WAIT_SECONDS:-900}"
max_attempts="${MEASURE_MAX_ATTEMPTS:-12}"
required_pairs=3

gate() {
	echo "gate: slice=$slice observed=$1 assertions_failed=$2 setup_failed=$3 file_alone_ratio=$4 combined_ratio=$5 valid_pairs=$6 required_pairs=$required_pairs limit_file_alone=${limit_alone:-?} limit_combined=${limit_combined:-?} verdict=$7"
}
setup_fail() {
	echo "setup failed: $1"
	gate 0 0 1 n/a n/a 0 "setup-failed"
	exit 3
}

[ -f "$data" ] || setup_fail "no data file $data"
field() { sed -n "s/^- $1: *//p" "$data" | head -1 | sed 's/[[:space:]]*$//'; }
before_sha="$(field "Before commit")"
page="$(field "Page test file")"
direct_list="$(field "Direct test files")"
limit_alone="$(field "File-alone ratio at most")"
limit_combined="$(field "Combined ratio at most")"
before_subject="$(field "Before commit subject")"
if [ -n "$before_subject" ]; then
	matches="$(git log --format='%H%x09%s' --fixed-strings --grep="$before_subject" HEAD | awk -F'\t' -v s="$before_subject" '$2 == s { print $1 }')"
	[ "$(printf '%s\n' "$matches" | grep -c .)" -eq 1 ] ||
		setup_fail "the subject '$before_subject' names $(printf '%s\n' "$matches" | grep -c .) commits on HEAD's history, one is required"
	before_sha="$matches"
fi
[ -n "$before_sha" ] && [ -n "$page" ] && [ -n "$limit_alone" ] && [ -n "$limit_combined" ] ||
	setup_fail "the Measure section of $data lacks a before commit, a page file or a ratio limit"
[ -f "$frontend/$page" ] || setup_fail "page test file $page does not exist"

direct_files=()
for file in $direct_list; do
	[ -f "$frontend/$file" ] || setup_fail "direct test file $file does not exist"
	direct_files+=("$file")
done

work="$(mktemp -d)"
before_copy="${page%.test.tsx}.measuredBefore.test.tsx"
cleanup() {
	rm -f "$frontend/$before_copy"
	rm -rf "$work"
}
trap cleanup EXIT
trap 'exit 130' INT TERM

git show "$before_sha:$frontend/$page" >"$frontend/$before_copy" 2>/dev/null ||
	setup_fail "cannot read $page at $before_sha"

cores="$(nproc)"
limit_load="$(awk -v c="$cores" 'BEGIN { printf "%.2f", c / 2 }')"
load1() { cut -d' ' -f1 /proc/loadavg; }
quiet() { awk -v l="$1" -v m="$limit_load" 'BEGIN { exit !(l <= m) }'; }

# json_stat <report> <not older than epoch>  ->  "ok <tests> <failed> <seconds>" or "missing" or "stale"
json_stat() {
	python3 -I -c '
import json, os, sys
path, started = sys.argv[1], float(sys.argv[2])
if not os.path.exists(path):
    print("missing"); sys.exit(0)
if os.path.getmtime(path) < started - 1:
    print("stale"); sys.exit(0)
report = json.load(open(path))
tests = failed = 0
seconds = 0.0
for suite in report["testResults"]:
    for test in suite["assertionResults"]:
        tests += 1
        failed += test["status"] != "passed"
        seconds += (test.get("duration") or 0) / 1000
print("ok", tests, failed, round(seconds, 4))
' "$1" "$2"
}

run_stat=""
run_vitest() { # <report name> <files...>; sets run_stat
	local out="$work/$1.json"
	shift
	rm -f "$out" "$frontend/test-results.json"
	local started
	started="$(date +%s)"
	pnpm --dir "$frontend" exec cross-env TZ=Europe/Zurich vitest run \
		--no-file-parallelism "$@" >"$out.log" 2>&1
	mv -f "$frontend/test-results.json" "$out" 2>/dev/null
	run_stat="$(json_stat "$out" "$started")"
}

readings=()
deadline=$(($(date +%s) + wait_seconds))
waited=0
wait_for_quiet() {
	while true; do
		local now
		now="$(load1)"
		readings+=("$now")
		quiet "$now" && return 0
		[ "$(date +%s)" -ge "$deadline" ] && return 1
		sleep 15
		waited=$((waited + 15))
	done
}

valid=0
attempts=0
failed_tests=0
setup_failed=0
observed=0
: >"$work/valid.tsv"
: >"$work/loaded.tsv"

# measure_side <name> <files...>: prints "ok <tests> <seconds>" or a reason, sets side_quiet to 1 or 0
side_quiet=1
measure_side() {
	local name="$1"
	shift
	local before after
	before="$(load1)"
	readings+=("$before")
	run_vitest "$name" "$@"
	after="$(load1)"
	readings+=("$after")
	quiet "$before" || side_quiet=0
	quiet "$after" || side_quiet=0
}

while [ "$valid" -lt "$pairs" ] && [ "$attempts" -lt "$max_attempts" ]; do
	wait_for_quiet || break
	attempts=$((attempts + 1))
	side_quiet=1
	loaded_abort=0
	declare -A stat=()
	if [ $((attempts % 2)) -eq 1 ]; then order=(before page direct); else order=(page direct before); fi
	for side in "${order[@]}"; do
		case "$side" in
		before) measure_side before "$before_copy" ;;
		page) measure_side page "$page" ;;
		direct)
			if [ "${#direct_files[@]}" -gt 0 ]; then
				measure_side direct "${direct_files[@]}"
			else
				run_stat="ok 0 0 0"
			fi
			;;
		esac
		stat[$side]="$run_stat"
		read -r state tests failed secs <<<"$run_stat"
		if { [ "$state" != "ok" ] || [ "$failed" -gt 0 ]; } && [ "$side_quiet" -eq 0 ]; then
			echo "pair $attempts: $side run failed while the machine was loaded; the pair is discarded"
			loaded_abort=1
			break
		fi
		if [ "$state" != "ok" ]; then
			echo "pair $attempts: $side run left $state report (see $work/$side.json.log)"
			setup_failed=1
			break
		fi
		if [ "$failed" -gt 0 ]; then
			echo "pair $attempts: $side run had $failed failed test(s)"
			if [ "$side" = before ]; then setup_failed=1; else failed_tests=$((failed_tests + failed)); fi
			break
		fi
	done
	[ "$loaded_abort" -eq 1 ] && continue
	[ "$setup_failed" -eq 1 ] && break
	[ "$failed_tests" -gt 0 ] && break
	read -r _ before_tests _ before_secs <<<"${stat[before]}"
	read -r _ page_tests _ page_secs <<<"${stat[page]}"
	read -r _ direct_tests _ direct_secs <<<"${stat[direct]}"
	if [ "$side_quiet" -eq 1 ]; then
		valid=$((valid + 1))
		observed=$((page_tests + direct_tests))
		echo "$before_secs $page_secs $direct_secs" >>"$work/valid.tsv"
	else
		echo "$before_secs $page_secs $direct_secs" >>"$work/loaded.tsv"
	fi
	echo "pair $attempts: before ${before_secs}s page ${page_secs}s direct ${direct_secs}s, load limit $limit_load, valid=$side_quiet"
done

readings_summary="$(printf '%s\n' "${readings[@]}" | sort -n | awk 'NR==1{min=$1} {max=$1; n++} END{printf "min=%s max=%s samples=%d", min, max, n}')"

if [ "$setup_failed" -eq 1 ]; then
	gate "$observed" 0 1 n/a n/a "$valid" "setup-failed"
	exit 3
fi
if [ "$failed_tests" -gt 0 ]; then
	gate "$observed" "$failed_tests" 0 n/a n/a "$valid" "tests-failed"
	exit 1
fi

# median ratios over a pairs file; prints "<file alone> <combined>" or "none"
ratios_of() {
	python3 -I -c '
import statistics, sys
rows = [tuple(map(float, line.split())) for line in open(sys.argv[1]) if line.strip()]
if not rows or any(before <= 0 for before, _, _ in rows):
    print("none"); sys.exit(0)
alone = statistics.median(page / before for before, page, _ in rows)
combined = statistics.median((page + direct) / before for before, page, direct in rows)
print(f"{alone:.3f} {combined:.3f}")
' "$1"
}

if [ "$valid" -lt "$required_pairs" ]; then
	indicative="$(ratios_of "$work/loaded.tsv")"
	echo "unmeasured, machine loaded: $valid valid pair(s) of $required_pairs required after $attempts attempt(s) and ${waited}s of waiting;"
	echo "  load readings (1 minute) $readings_summary, limit $limit_load on $cores cores; not judged: pairs that ended loaded read $indicative"
	gate "$observed" 0 0 n/a n/a "$valid" "unmeasured"
	exit 2
fi

read -r alone combined <<<"$(ratios_of "$work/valid.tsv")"
if [ "$alone" = "none" ]; then
	gate "$observed" 0 1 n/a n/a "$valid" "setup-failed"
	exit 3
fi
over="$(awk -v a="$alone" -v la="$limit_alone" -v c="$combined" -v lc="$limit_combined" 'BEGIN { print (a > la || c > lc) ? 1 : 0 }')"
if [ "$over" -eq 1 ]; then
	gate "$observed" 1 0 "$alone" "$combined" "$valid" "time-ratio-exceeded"
	exit 1
fi
gate "$observed" 0 0 "$alone" "$combined" "$valid" "pass"
exit 0

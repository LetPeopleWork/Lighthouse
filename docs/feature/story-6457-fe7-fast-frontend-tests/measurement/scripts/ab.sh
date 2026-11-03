#!/usr/bin/env bash
# Usage: ab.sh <out-dir> <pairs> <baselineFE> <candidateFE> [extra vitest args for the candidate arm...]
# Alternates baseline (A) and candidate (B) full runs, A B A B ..., for <pairs> pairs.
# <baselineFE> and <candidateFE> are the Lighthouse.Frontend folders of the two worktrees.
# Each run: LIGHTHOUSE_TEST_WORKERS=6 (env WORKERS_A / WORKERS_B override it per arm), TZ=Europe/Zurich, LIGHTHOUSE_TIME_BUDGET=off (wall time is
# measured here, not judged), `pnpm exec vitest run --reporter=default --reporter=json`, sampled by measure.sh.
# Per run it saves <out-dir>/<label>.json.gz and <label>.log, and appends to <out-dir>/runs.txt the
# CSV line (label,exit,wall_s,peak_rss_mb,min_mem_available_mb,max_threads) and Vitest's Duration line.
# SHUFFLE=<seed> adds --sequence.shuffle --sequence.seed=<seed> to the candidate arm only.
# Run it on a quiet machine. Summarise with ab_report.py <out-dir>.
set -u
[ $# -ge 4 ] || { sed -n '2,10p' "$0"; exit 2; }
out=$(realpath -m "$1"); pairs=$2; base=$3; cand=$4; shift 4
here=$(dirname "$(realpath "$0")")
mkdir -p "$out"; export OUT_DIR="$out"

run_one() { # label frontendDir workers extra-args...
  local label=$1 dir=$2 workers=$3; shift 3
  rm -f "$dir/test-results.json"
  local csv
  csv=$(env LIGHTHOUSE_TEST_WORKERS=$workers TZ=Europe/Zurich LIGHTHOUSE_TIME_BUDGET=off \
    "$here/measure.sh" "$label" "$dir" pnpm exec vitest run --reporter=default --reporter=json "$@")
  echo "$csv" >>"$out/runs.txt"
  sed 's/\x1b\[[0-9;]*m//g' "$out/$label.log" | grep -aE '^\s+Duration\s+[0-9.]+s' | tail -1 | sed "s/^/$label: /" >>"$out/runs.txt"
  [ -f "$dir/test-results.json" ] && gzip -c "$dir/test-results.json" >"$out/$label.json.gz"
  echo "$csv"
}

extra=()
[ -n "${SHUFFLE:-}" ] && extra+=(--sequence.shuffle "--sequence.seed=${SHUFFLE}")
for i in $(seq 1 "$pairs"); do
  run_one "A-$i" "$base" "${WORKERS_A:-6}"
  run_one "B-$i" "$cand" "${WORKERS_B:-6}" "${extra[@]}" "$@"
done

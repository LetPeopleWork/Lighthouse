#!/usr/bin/env bash
# Full (explicit) against half the cores, both stacks, three alternating rounds.
# Usage: OUT_DIR=<dir> run-all.sh <repo-root>
set -u
repo=$1
here=$(cd "$(dirname "$0")" && pwd)
out=${OUT_DIR:?set OUT_DIR}
mkdir -p "$out"
out=$(cd "$out" && pwd)
results="$out/results.csv"
echo "label,exit,wall_s,peak_tree_rss_mb,min_mem_available_mb,max_threads" >"$results"

cores=$(nproc)
fe_full=$((cores - 1))
be_full=$cores
half=$((cores / 2)); [ "$half" -ge 1 ] || half=1
[ "$fe_full" -ge 1 ] || fe_full=1

filter='TestCategory!=Integration&TestCategory!=JiraIntegration&TestCategory!=LinearIntegration&TestCategory!=AdoIntegration&TestCategory!=ServiceNowIntegration'

(cd "$repo/Lighthouse.Backend" && dotnet build -v q >"$out/build.log" 2>&1) || { echo "build failed" >>"$results"; exit 1; }

fe() { # $1 label, $2 worker count
  LIGHTHOUSE_TEST_WORKERS=$2 OUT_DIR=$out bash "$here/measure.sh" "$1" "$repo/Lighthouse.Frontend" pnpm test >>"$results"
}
be() { # $1 label, $2 worker count
  (cd "$repo/Lighthouse.Backend" && dotnet build-server shutdown >/dev/null 2>&1)
  LIGHTHOUSE_TEST_WORKERS=$2 OUT_DIR=$out bash "$here/measure.sh" "$1" "$repo/Lighthouse.Backend" dotnet test --no-build --filter "$filter" >>"$results"
}

for round in 1 2 3; do
  if [ $((round % 2)) -eq 1 ]; then
    fe "fe-full-r$round" "$fe_full"; fe "fe-half-r$round" "$half"
    be "be-full-r$round" "$be_full"; be "be-half-r$round" "$half"
  else
    fe "fe-half-r$round" "$half"; fe "fe-full-r$round" "$fe_full"
    be "be-half-r$round" "$half"; be "be-full-r$round" "$be_full"
  fi
done
echo done >>"$results"

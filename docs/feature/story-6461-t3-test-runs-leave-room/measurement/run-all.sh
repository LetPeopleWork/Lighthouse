#!/usr/bin/env bash
# Full (tool default) against half the cores, both stacks, three alternating rounds.
# Usage: OUT_DIR=<dir> run-all.sh <repo-root>
set -u
repo=$1
here=$(cd "$(dirname "$0")" && pwd)
out=${OUT_DIR:?set OUT_DIR}
mkdir -p "$out"
results="$out/results.csv"
echo "label,exit,wall_s,peak_tree_rss_mb,min_mem_available_mb,max_threads" >"$results"

filter='TestCategory!=Integration&TestCategory!=JiraIntegration&TestCategory!=LinearIntegration&TestCategory!=AdoIntegration&TestCategory!=ServiceNowIntegration'

(cd "$repo/Lighthouse.Backend" && dotnet build -v q >"$out/build.log" 2>&1) || { echo "build failed" >>"$results"; exit 1; }

fe() { # $1 label, rest: extra vitest args
  local label=$1; shift
  OUT_DIR=$out bash "$here/measure.sh" "$label" "$repo/Lighthouse.Frontend" pnpm test "$@" >>"$results"
}
be() { # $1 label, rest: extra runsettings args
  local label=$1; shift
  (cd "$repo/Lighthouse.Backend" && dotnet build-server shutdown >/dev/null 2>&1)
  if [ $# -gt 0 ]; then
    OUT_DIR=$out bash "$here/measure.sh" "$label" "$repo/Lighthouse.Backend" dotnet test --no-build --filter "$filter" -- "$@" >>"$results"
  else
    OUT_DIR=$out bash "$here/measure.sh" "$label" "$repo/Lighthouse.Backend" dotnet test --no-build --filter "$filter" >>"$results"
  fi
}

for round in 1 2 3; do
  if [ $((round % 2)) -eq 1 ]; then
    fe "fe-full-r$round"; fe "fe-half-r$round" --maxWorkers=6
    be "be-full-r$round"; be "be-half-r$round" NUnit.NumberOfTestWorkers=6
  else
    fe "fe-half-r$round" --maxWorkers=6; fe "fe-full-r$round"
    be "be-half-r$round" NUnit.NumberOfTestWorkers=6; be "be-full-r$round"
  fi
done
echo done >>"$results"

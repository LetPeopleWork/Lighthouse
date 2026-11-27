#!/usr/bin/env bash
# Rebuilds each scratch backend without its stale deps.json (the known missing-migrations-assembly
# fault), then saves its OpenAPI document.
S=/tmp/claude-1000/-storage-repos-Lighthouse/400a30ce-7392-4b8c-a158-bd07eae5e2d0/scratchpad
P=/storage/repos/Lighthouse/.claude/worktrees/glowing-crafting-globe/docs/feature/api-shape-evaluation/prototype
port=5433
for pair in wt-fix:fixed-annotations wt-after:bug6254-after wt-before:bug6254-before; do
  wt=${pair%%:*}; name=${pair##*:}
  find "$S/$wt/Lighthouse.Backend/Lighthouse.Backend/bin" -name Lighthouse.deps.json -delete
  (cd "$S/$wt/Lighthouse.Backend" && dotnet build Lighthouse.Backend -v q -nologo > "$S/rebuild-$wt.log" 2>&1)
  echo "$wt rebuild exit $?"
  bash "$P/scripts/run-and-fetch-doc.sh" "$S/$wt" "$port" "$P/openapi/$name.json" 2>&1 | tail -5
  port=$((port + 1))
done

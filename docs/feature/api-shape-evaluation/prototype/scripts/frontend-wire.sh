#!/usr/bin/env bash
# TEMPORARY: copies one generated output into Lighthouse.Frontend/src/generated-prototype (without the
# MSW mocks, which need msw + faker the frontend does not have) and runs tsc -b. The folder is removed
# by frontend-unwire.sh before the prototype ends.
# usage: frontend-wire.sh <out-subdir> <log>
gen=/tmp/claude-1000/-storage-repos-Lighthouse/400a30ce-7392-4b8c-a158-bd07eae5e2d0/scratchpad/gen/$1
fe=/storage/repos/Lighthouse/.claude/worktrees/glowing-crafting-globe/Lighthouse.Frontend
g=$fe/src/generated-prototype
rm -rf "$g/zod" "$g/model" "$g/client"
cp -r "$gen/zod" "$gen/model" "$g/"
mkdir -p "$g/client"
mkdir -p "$g/client/team-metrics" "$g/client/deliveries"; cp "$gen/client/team-metrics/team-metrics.ts" "$g/client/team-metrics/"; cp "$gen/client/deliveries/deliveries.ts" "$g/client/deliveries/"
cd "$fe" || exit 1
timeout 110 pnpm exec tsc -b > "$2" 2>&1
echo "tsc exit $?"
grep "error TS" -A3 "$2" | cut -c1-500

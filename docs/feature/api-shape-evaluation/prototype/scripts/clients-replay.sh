#!/usr/bin/env bash
# Bug #6254 replay on a SCRATCH COPY of lighthouse-clients (git archive of 4e76513^, the commit before
# the clients' fix). Generates the clients' types from one OpenAPI document into
# packages/client/src/generated and runs the clients' own typecheck.
# usage: clients-replay.sh <doc-name> <log>
S=/tmp/claude-1000/-storage-repos-Lighthouse/400a30ce-7392-4b8c-a158-bd07eae5e2d0/scratchpad
P=/storage/repos/Lighthouse/.claude/worktrees/glowing-crafting-globe/docs/feature/api-shape-evaluation/prototype
C=$S/clients-before
rm -rf "$C/packages/client/src/generated"
(cd "$S/gen" && INPUT="$P/openapi/$1.json" OUT="$C/packages/client/src/generated" npx orval --config "$P/orval.clients.config.mjs" 2>&1 | tail -1)
cd "$C" || exit 1
pnpm exec tsc -b tsconfig.json --pretty false > "$2" 2>&1
echo "typecheck exit $?" >> "$2"
cat "$2" | cut -c1-600

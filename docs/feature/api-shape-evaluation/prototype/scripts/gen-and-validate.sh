#!/usr/bin/env bash
# Generates from one saved document and validates the live answers of the running instance against it.
# usage: gen-and-validate.sh <doc-name-without-.json> <out-subdir>
S=/tmp/claude-1000/-storage-repos-Lighthouse/400a30ce-7392-4b8c-a158-bd07eae5e2d0/scratchpad
P=/storage/repos/Lighthouse/.claude/worktrees/glowing-crafting-globe/docs/feature/api-shape-evaluation/prototype
doc="$1"; out="$S/gen/$2"
cd "$S/gen" || exit 1
rm -rf "$out"
bash "$P/scripts/generate.sh" "$P/openapi/$doc.json" "$out" 2>&1 | tail -1
npx esbuild "$out/zod/index.zod.ts" --bundle --format=esm --platform=node --outfile="$S/gen/zod-bundle-$doc.mjs" --log-level=error
node "$P/scripts/validate-live.mjs" "$P/openapi/$doc.json" "$S/gen/zod-bundle-$doc.mjs" http://localhost:5431 "$S/answers/$doc" "$P/results/validate-$doc.json" > "$P/results/validate-$doc.log" 2>&1
grep -v "zod=pass T/.* $" "$P/results/validate-$doc.log"
python3 -I "$P/scripts/doc_stats.py" "$P/openapi/$doc.json" | grep -E "properties:|polymorphic"

#!/usr/bin/env bash
# Sizes of the generated code (lines/bytes per part) and the minified+gzipped weight of the generated
# zod schemas for the Team metrics tag (zod itself excluded: the frontend already ships it).
gen=/tmp/claude-1000/-storage-repos-Lighthouse/400a30ce-7392-4b8c-a158-bd07eae5e2d0/scratchpad/gen
out=$gen/$1
for part in zod model client; do
  files=$(find "$out/$part" -name '*.ts' -not -name '*.msw.ts' | wc -l)
  lines=$(find "$out/$part" -name '*.ts' -not -name '*.msw.ts' -exec cat {} + | wc -l)
  bytes=$(find "$out/$part" -name '*.ts' -not -name '*.msw.ts' -exec cat {} + | wc -c)
  echo "$part: $files files, $lines lines, $bytes bytes"
done
mswlines=$(find "$out/client" -name '*.msw.ts' -exec cat {} + | wc -l)
mswbytes=$(find "$out/client" -name '*.msw.ts' -exec cat {} + | wc -c)
echo "msw mocks: $mswlines lines, $mswbytes bytes"
echo "team-metrics zod: $(wc -l < "$out/zod/team-metrics/team-metrics.zod.ts") lines; deliveries zod: $(wc -l < "$out/zod/deliveries/deliveries.zod.ts") lines"
cd "$gen" || exit 1
npx esbuild "$out/zod/team-metrics/team-metrics.zod.ts" --bundle --minify --format=esm --external:zod --outfile="$gen/tm-min.js" --log-level=error
echo "team-metrics zod minified: $(wc -c < "$gen/tm-min.js") bytes, gzip: $(gzip -9c "$gen/tm-min.js" | wc -c) bytes"

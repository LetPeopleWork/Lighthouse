#!/usr/bin/env bash
# Counts Biome findings on the generated folder, split by kind and by sub-folder.
# usage: biome-breakdown.sh <frontend-dir> <generated-subdir>
cd "$1" || exit 1
g="$2"
for part in zod model client; do
  [ -d "$g/$part" ] || continue
  echo "== $part: format"
  pnpm exec biome format "$g/$part" --max-diagnostics=0 2>&1 | grep -E "^Found|^Checked"
  echo "== $part: lint"
  pnpm exec biome lint "$g/$part" --max-diagnostics=0 2>&1 | grep -E "^Found|^Checked"
done
echo "== lint rules, whole folder"
pnpm exec biome lint "$g" --max-diagnostics=3000 2>&1 | grep -oE "lint/[a-zA-Z]+/[a-zA-Z]+" | sort | uniq -c | sort -rn
echo "== after biome check --write (what prebuild does), remaining"
pnpm exec biome check --write "$g" --max-diagnostics=0 2>&1 | grep -E "^Found|^Checked|Fixed"
pnpm exec biome lint "$g" --max-diagnostics=3000 2>&1 | grep -oE "lint/[a-zA-Z]+/[a-zA-Z]+" | sort | uniq -c | sort -rn

#!/usr/bin/env bash
# Usage: check-behaviour-free.sh <before-commit>   (run from the repository root)
# Prints findings and exits 1 if the diff since <before-commit> weakens a test or touches
# production code beyond the two files the design allows.
set -u
before="${1:?before-commit required}"
fe=Lighthouse.Frontend/src
tests=(":(glob)$fe/**/*.test.ts" ":(glob)$fe/**/*.test.tsx" "$fe/tests")
status=0

added=$(git diff -U0 "$before" -- "${tests[@]}" | grep -E '^\+' | grep -vE '^\+\+\+')
removed=$(git diff -U0 "$before" -- "${tests[@]}" | grep -E '^-' | grep -vE '^---')

weaker='\.skip\b|\.only\b|\.todo\b|it\.fails|\bxit\(|\bxdescribe\(|toMatchObject|objectContaining|toBeGreaterThan|toBeLessThan|toContain\(|toMatch\(|\{ *timeout *:|testTimeout|setConfig|, *[0-9]{4,} *\)'
hits=$(printf '%s\n' "$added" | grep -E "$weaker" || true)
if [ -n "$hits" ]; then echo "ADDED skip/only/todo, looser matcher or longer timeout:"; echo "$hits"; status=1; fi

dropped_not=$(printf '%s\n' "$removed" | grep -cE '\.not\.' || true)
added_not=$(printf '%s\n' "$added" | grep -cE '\.not\.' || true)
echo "removed 'not.' lines: $dropped_not, added: $added_not (every removed one needs a moved-assertions row)"

echo "removed expect( lines per file (each needs a row):"
for f in $(git diff --name-only "$before" -- "${tests[@]}"); do
  n=$(git diff -U0 "$before" -- "$f" | grep -E '^-' | grep -vE '^---' | grep -oE '\bexpect\(' | wc -l)
  if [ "$n" -gt 0 ]; then echo "  $n  $f"; fi
done

allowed='Lighthouse.Frontend/src/pages/Reports/(printFailures|useReportsPdfExport)\.ts$'
prod=$(git diff --name-only "$before" -- "$fe" | grep -vE '\.test\.tsx?$|/tests/' | grep -vE "$allowed" || true)
if [ -n "$prod" ]; then echo "PRODUCTION FILES CHANGED beyond the allowed two:"; echo "$prod"; status=1; fi

e2e=$(git diff --name-only "$before" -- Lighthouse.EndToEndTests ':(glob)**/*.screenshot*' || true)
if [ -n "$e2e" ]; then echo "E2E or screenshot files changed:"; echo "$e2e"; status=1; fi

exit $status

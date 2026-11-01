#!/bin/bash
# usage: behaviour-preserved.sh <base-ref>
# Run from anywhere inside the repository, after `dotnet build` of the backend tests (Debug, no changes pending).
# Compares the working tree with <base-ref> (the commit before the slice) and prints what a reviewer must look at.
# Exit code 1 if any check finds something.
base="${1:?usage: behaviour-preserved.sh <base-ref>}"
cd "$(git rev-parse --show-toplevel)" || exit 2
root=Lighthouse.Backend/Lighthouse.Backend.Tests
here=docs/feature/story-6460-t2-backend-host-per-fixture/measurement
fail=0

echo "== 1. test ids: same names as before? (sorted, duplicates kept; the list holds names, not classes)"
before="$here/test-ids-before.2026-11-01.txt.gz"
(cd Lighthouse.Backend && dotnet test --no-build --list-tests) | tail -n +5 | sed 's/^    //' | sort > /tmp/test-ids-after.txt
if zcat "$before" | diff - /tmp/test-ids-after.txt > /tmp/test-ids.diff; then
  echo "same $(wc -l < /tmp/test-ids-after.txt) names"
else
  echo "DIFFERENT (lines starting with < are gone, > are new; the guard's own new tests are expected under >):"
  cat /tmp/test-ids.diff
  fail=1
fi

echo "== 2. added [Ignore, [Explicit, Assert.Ignore or NonParallelizable (must print nothing)"
if git diff -U0 "$base" -- "$root" | grep -nE '^\+[^+].*(\[Ignore|\[Explicit|Assert\.Ignore|NonParallelizable)'; then
  fail=1
fi

echo "== 3. assertion lines per file, before and after (a drop needs a reviewer's eye; a removed and re-added assertion looks like no drop)"
deleted=$(git diff --name-only --diff-filter=D "$base" -- "$root" | grep '\.cs$')
if [ -n "$deleted" ]; then
  echo "$deleted" | sed 's/^/DELETED /'
  fail=1
fi
for f in $(git diff --name-only --diff-filter=M "$base" -- "$root" | grep '\.cs$'); do
  b=$(git show "$base:$f" | grep -c 'Assert\.')
  a=$(grep -c 'Assert\.' "$f")
  if [ "$a" -lt "$b" ]; then
    echo "DROP $f $b -> $a"
    fail=1
  fi
done

[ "$fail" -eq 0 ] && echo "nothing to look at"
exit "$fail"

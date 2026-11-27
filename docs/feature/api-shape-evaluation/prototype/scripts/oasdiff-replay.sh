#!/usr/bin/env bash
# The CI check a committed document would run: oasdiff breaking, failing on ERR-level changes.
# usage: oasdiff-replay.sh <base.json> <revision.json> <out.txt>
oasdiff=/tmp/claude-1000/-storage-repos-Lighthouse/400a30ce-7392-4b8c-a158-bd07eae5e2d0/scratchpad/bin/oasdiff
"$oasdiff" breaking "$1" "$2" --fail-on ERR > "$3" 2>&1
code=$?
echo "oasdiff exit code: $code" >> "$3"
cat "$3"

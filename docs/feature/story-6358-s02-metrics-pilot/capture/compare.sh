#!/usr/bin/env bash
# Compare two capture directories written by capture-metrics.sh. Exits 0 when every captured
# response (requests/) and every snapshot table dump (db/) is byte-identical, 1 on any
# difference (including a request present in one capture only), 2 on bad usage.
#
# Usage: compare.sh <base-capture-dir> <candidate-capture-dir>

set -euo pipefail

if [[ $# -ne 2 ]]; then
    echo "usage: $0 <base-capture-dir> <candidate-capture-dir>" >&2
    exit 2
fi

BASE=$1
CANDIDATE=$2

for dir in "$BASE" "$CANDIDATE"; do
    if [[ ! -d "$dir/requests" || ! -d "$dir/db" || ! -f "$dir/_manifest.txt" ]]; then
        echo "$dir is not a capture directory (needs requests/, db/ and _manifest.txt)" >&2
        exit 2
    fi
done

manifest_value() { sed -n "s/^$2: //p" "$1/_manifest.txt"; }

echo "base:      $(manifest_value "$BASE" revision) captured $(manifest_value "$BASE" captured_at_utc)"
echo "candidate: $(manifest_value "$CANDIDATE" revision) captured $(manifest_value "$CANDIDATE" captured_at_utc)"

# Answers that depend on today are only comparable when both captures ran on the same day.
if [[ "$(manifest_value "$BASE" today_utc)" != "$(manifest_value "$CANDIDATE" today_utc)" ]]; then
    echo "the captures ran on different days ($(manifest_value "$BASE" today_utc) vs $(manifest_value "$CANDIDATE" today_utc)): capture both back to back and compare again" >&2
    exit 1
fi

if ! diff <(sed -n '/^database_source:/,$p' "$BASE/_manifest.txt") \
          <(sed -n '/^database_source:/,$p' "$CANDIDATE/_manifest.txt") > /dev/null; then
    echo "the captures started from different database files: compare only captures of the same backup" >&2
    exit 1
fi

status=0
for part in requests db; do
    if ! diff -r -u "$BASE/$part" "$CANDIDATE/$part"; then
        status=1
    fi
done

files=$(find "$BASE/requests" -type f | wc -l)
if [[ $status -eq 0 ]]; then
    echo "IDENTICAL: $files responses and $(find "$BASE/db" -type f | wc -l) snapshot tables"
else
    echo "DIFFERENT: see the diff above" >&2
fi
exit $status

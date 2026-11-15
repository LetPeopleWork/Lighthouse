#!/usr/bin/env bash
# Targeted S2/S3 explorations: negated predicates give concrete traces.
cd "$(dirname "$0")"
STEPS=12
SAMPLES=50000
SEED=6625
run() {
  name="$1"; shift
  argv=(quint run baseline.qnt --max-steps "$STEPS" --max-samples "$SAMPLES" --seed "$SEED" \
        --out-itf "traces/$name.itf.json" --verbosity 1 "$@")
  {
    echo "ARGV: ${argv[*]}"
    echo "VERSION: $(quint --version)"
    "${argv[@]}" > "runs/$name.out" 2>&1
    echo "EXIT: $?"
  } > "runs/$name.meta"
  if [ -f "traces/$name.itf.json" ]; then
    python3 -I render.py "traces/$name.itf.json" > "runs/$name.rendered" 2>&1
  fi
}
run S2a-migrate-30-45-edit-open-bump --invariant 'not(event.kind == "migrate" and edit.open and edit.id == 1 and edit.draftNowLen == 45 and reports.get(1).len == 30 and migrationBumpsVersion)'
run S2b-migrate-30-45-edit-open-nobump --invariant 'not(event.kind == "migrate" and edit.open and edit.id == 1 and edit.draftNowLen == 45 and reports.get(1).len == 30 and not(migrationBumpsVersion))'
run S2c-oldpage-save-after-migration-nobump --invariant 'not(migrated and event.kind == "save-edit" and event.id == 1 and event.nowLen == 45 and not(migrationBumpsVersion))'
run S2d-newpage-stale-refusal --invariant 'not(event.kind == "save-edit" and event.nowLen == -1 and event.result == "refused: report changed since the Edit was opened")'
run S2e-overlap-appears-from-now-length-change --invariant 'not(reports.keys().exists(k => reports.get(k).nowLen != reports.get(k).len and reports.get(k).thenEnd < today - reports.get(k).nowLen + 1 and reports.get(k).thenEnd >= today - reports.get(k).len + 1))'
run S2f-api-now-equal-accepted --invariant 'not(event.kind == "api-create" and event.nowLen == event.len and event.result == "ok")'
run S3a-perday-same-total-by-1-then-positive --invariant 'not(reports.keys().exists(k => reports.get(k).thenTotal > 0 and reports.get(k).nowTotal >= 0 and iabs(reports.get(k).nowTotal - reports.get(k).thenTotal) == 1 and perDayTenths(reports.get(k).nowTotal, reports.get(k).len) == perDayTenths(reports.get(k).thenTotal, reports.get(k).len)))'
run S3b-both-zero --invariant 'not(reports.keys().exists(k => reports.get(k).thenTotal == 0 and reports.get(k).nowTotal == 0))'
run S3c-drop-to-zero --invariant 'not(reports.keys().exists(k => reports.get(k).thenTotal > 0 and reports.get(k).nowTotal == 0))'
run S3d-then-not-captured --invariant 'not(reports.keys().exists(k => reports.get(k).thenTotal == -1 and reports.get(k).nowTotal >= 0))'

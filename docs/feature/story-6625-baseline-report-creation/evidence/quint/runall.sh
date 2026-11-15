#!/usr/bin/env bash
# Runs every exploration; writes argv, exit code, stdout/stderr and rendered trace per run into runs/.
cd "$(dirname "$0")"
mkdir -p runs traces
STEPS=12
SAMPLES=20000
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
# invariants that the rules should guarantee
for inv in invNowEqThenAfterMigration invOverlapIffShareDay invOverlapUntilIsLastSharedDay \
           invChangeIffBothCaptured invPctOnlyWhenThenPositive invApiAcceptOnlyEqualNow \
           invSavedThenAtLeast14 invThenNotAfterToday; do
  run "$inv" --invariant "$inv"
done
# probes expected to break (questions, not rules)
for inv in invThenInsideKeptHistory invShownNowEqStoredNow invEditSaveNotRefused; do
  run "$inv" --invariant "$inv"
done
# witnesses: negate to obtain a concrete trace
for w in wPerDaySameTotalBy1 wThenWas0 wEqualTotals wNotCaptured wOverlapThenEnded wDefaultRefused \
         wEndsTodayOk wApiNowLenRefused wApiAfterToday wOldEditRefusedAfterMigration; do
  run "$w" --invariant "not($w)"
done
run S1-before-cutoff --invariant 'not(event.result == "refused: Then starts before kept history")'
run S1-too-short --invariant 'not(event.result == "refused: Then shorter than 14 days")'
run S2-oldpage-save-after-migration --invariant 'not(migrated and event.kind == "save-edit" and event.nowLen == 45)'
run S2-newpage-stale-after-migration --invariant 'not(migrated and event.kind == "save-edit" and event.nowLen == -1 and event.result != "ok")'
run S2-old-30-45-shown --invariant 'not(not(migrated) and reports.keys().exists(k => reports.get(k).len == 30 and reports.get(k).nowLen == 45) and today > 1000)'
# witness counts over plain random runs (no invariant)
run counts --witnesses wPerDaySameTotalBy1 wThenWas0 wEqualTotals wNotCaptured wOverlapThenEnded \
    wDefaultRefused wEndsTodayOk wApiNowLenRefused wApiAfterToday wOldEditRefusedAfterMigration

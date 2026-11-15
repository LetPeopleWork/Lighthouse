#!/usr/bin/env bash
# Runs design.qnt: typecheck, each invariant, each witness, and each behavioural mutant against the invariant it
# must break. Mutant copies are built outside the repository in $MUTDIR. One receipt per run in runs/design-*.
set -u
cd "$(dirname "$0")"
MUTDIR="${MUTDIR:-$(mktemp -d)}"
mkdir -p runs
STEPS=10
SAMPLES=20000
SEED=6625
SHA=$(sha256sum design.qnt | cut -d' ' -f1)
receipt() {
  name="$1"; shift
  out="runs/design-$name"
  {
    echo "ARGV: $*"
    echo "VERSION: $(quint --version)"
    echo "MODEL_SHA256: $SHA"
    "$@" > "$out.out" 2>&1
    echo "EXIT: $?"
  } > "$out.meta"
}
receipt typecheck quint typecheck design.qnt
for inv in invShownNowEqThen invNoStoredNowOnCreate invAcceptOnlyEqualOrAbsentNow invStaleOnlyAfterRealChange \
           invEditNeverRefusedForHistory invCreatedNowInsideKeptHistory invTotalAndPerDayAgree; do
  receipt "$inv" quint run design.qnt --max-steps "$STEPS" --max-samples "$SAMPLES" --seed "$SEED" --invariant "$inv"
done
for w in wOldPageNowRefused wLegacyOverHistoryEdited; do
  receipt "$w" quint run design.qnt --max-steps "$STEPS" --max-samples "$SAMPLES" --seed "$SEED" --invariant "$w"
done
while read -r mutant inv; do
  sed "s/pure val MUTANT = \"none\"/pure val MUTANT = \"$mutant\"/" design.qnt > "$MUTDIR/design.qnt"
  receipt "mutant-$mutant" quint run "$MUTDIR/design.qnt" --max-steps "$STEPS" --max-samples "$SAMPLES" --seed "$SEED" --invariant "$inv"
done <<'EOF'
read-stored invShownNowEqThen
store-now invNoStoredNowOnCreate
skip-now-check invAcceptOnlyEqualOrAbsentNow
bump invStaleOnlyAfterRealChange
edit-checks-history invEditNeverRefusedForHistory
skip-history invCreatedNowInsideKeptHistory
unequal-lengths invTotalAndPerDayAgree
EOF
for f in runs/design-*.meta; do echo "$f $(grep EXIT "$f")"; done

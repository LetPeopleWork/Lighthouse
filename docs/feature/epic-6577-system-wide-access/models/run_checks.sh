#!/usr/bin/env bash
# Runs every check of the model and writes one receipt per run under receipts/.
cd "$(dirname "$0")"
M=system_wide_access_writes.qnt
rec() { name=$1; shift; out=receipts/$name.txt
  { echo "argv: $*"; echo "quint: $(quint --version)"; echo "model sha256: $(sha256sum $M | cut -d' ' -f1)"; [ -n "$MUT" ] && echo "mutant sha256: $(sha256sum $MUT | cut -d' ' -f1)"; } > $out
  timeout 180 "$@" > receipts/$name.stdout 2> receipts/$name.stderr; code=$?
  echo "exit: $code" >> $out; echo "$name exit=$code"; }
SEED=0x2a6577
for p in noLostSwitchWrite removalLeavesNoTwin twinsBounded; do
  MUT= rec run_$p quint run $M --invariant=$p --max-steps=15 --max-samples=20000 --seed=$SEED
done
MUT=mutants/mutant_put_ignores_token.qnt rec mutant_put_ignores_token quint run mutants/mutant_put_ignores_token.qnt --invariant=noLostSwitchWrite --max-steps=15 --max-samples=20000 --seed=$SEED
MUT=mutants/mutant_remove_deletes_one_row.qnt rec mutant_remove_deletes_one_row quint run mutants/mutant_remove_deletes_one_row.qnt --invariant=removalLeavesNoTwin --max-steps=15 --max-samples=20000 --seed=$SEED
for p in noLostSwitchWrite removalLeavesNoTwin twinsBounded; do
  MUT= rec verify_tlc_$p quint verify $M --backend=tlc --invariant=$p
done

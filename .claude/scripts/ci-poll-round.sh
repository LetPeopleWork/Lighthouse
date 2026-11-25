#!/usr/bin/env bash
# One polling round for a CI run, short enough to survive lean-ctx, which kills any shell after 120 s.
# Usage: .claude/scripts/ci-poll-round.sh <run-id>
# Prints exactly one line and exits:
#   failed=N pending=M      an answer: a job failed (N > 0), or nothing but release gates is left (M = 0)
#   still-running pending=M no answer within the round; start the next round
set -uo pipefail
id="$1"
p="?"
for _ in 1 2 3 4; do
  s=$(gh run view "$id" -R LetPeopleWork/Lighthouse --json jobs -q '[.jobs[] | select(.conclusion=="failure" or .conclusion=="timed_out" or .conclusion=="cancelled" or .conclusion=="action_required")] | length') || { sleep 20; continue; }
  p=$(gh run view "$id" -R LetPeopleWork/Lighthouse --json jobs -q '[.jobs[] | select(.status=="in_progress" or .status=="queued" or .status=="pending")] | length') || { sleep 20; continue; }
  if [ "$s" -gt 0 ] || [ "$p" -eq 0 ]; then
    echo "failed=$s pending=$p"
    exit 0
  fi
  sleep 20
done
echo "still-running pending=$p"

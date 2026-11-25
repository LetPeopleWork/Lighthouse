#!/usr/bin/env bash
# One waiting round for a long local job, short enough to survive lean-ctx, which kills any shell after 120 s.
# The job must write a line matching <pattern> to <log> when it ends (the gate commands append "exit=N").
# Usage: .claude/scripts/wait-round.sh <log> [pattern]   (pattern defaults to ^exit=)
# Prints exactly one line and exits:
#   done: <matching line>   the job has ended
#   still-waiting           no end within the round; start the next round
set -uo pipefail
log="$1"
pattern="${2:-^exit=}"
for _ in $(seq 1 18); do
  if [ -f "$log" ] && line=$(grep -E -m1 -- "$pattern" "$log"); then
    echo "done: $line"
    exit 0
  fi
  sleep 5
done
echo "still-waiting"

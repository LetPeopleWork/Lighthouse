#!/bin/sh
# Assembles frontend-audit.md from parts/ and the scripts' outputs. The verdict block (parts/09-verdict.md) is always last.
# usage: sh build_record.sh   (from anywhere; run the scripts' outputs first, as the record says)
cd "$(git rev-parse --show-toplevel)/docs/feature/story-6538-t11-test-writing-guidance/audit" || exit 1
fence() { printf '```\n'; cat "$1"; printf '```\n'; }
{
  cat parts/01-population.md
  cat parts/02-rows.md
  cat tally-output.md
  cat parts/03-agreement.md
  cat parts/04-estimate-head.md
  fence bootstrap-output.txt
  cat parts/04b-estimate-tail.md
  printf '\n```\n'; sh rule_diff.sh; printf '```\n'
  cat parts/05-seconds-head.md
  fence timings-output.txt
  cat parts/05b-seconds-tail.md
  cat parts/06-roadmap-search.md
  cat parts/09-verdict.md
} > frontend-audit.md
tail -n 3 frontend-audit.md

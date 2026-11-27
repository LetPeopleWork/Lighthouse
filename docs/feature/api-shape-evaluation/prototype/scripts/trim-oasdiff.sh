#!/usr/bin/env bash
# Replaces the 1.2 MB oasdiff output of the one-off annotation change by a summary.
cd /storage/repos/Lighthouse/.claude/worktrees/glowing-crafting-globe/docs/feature/api-shape-evaluation/prototype/results || exit 1
{
  head -1 oasdiff-current-to-fixed.txt
  echo "(full output was 1.2 MB; counts per rule, then the first 60 lines)"
  grep -oE "^(error|warning)\s+\[[a-z-]+\]" oasdiff-current-to-fixed.txt | sort | uniq -c | sort -rn
  head -60 oasdiff-current-to-fixed.txt
  tail -1 oasdiff-current-to-fixed.txt
} > oasdiff-current-to-fixed.summary.txt
rm oasdiff-current-to-fixed.txt
wc -l oasdiff-current-to-fixed.summary.txt

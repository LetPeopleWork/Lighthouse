#!/bin/sh
# Compares the verdict rule's text with the DESIGN commit and the band wording with the DISTILL commit; prints nothing when equal.
cd "$(git rev-parse --show-toplevel)" || exit 1
F=docs/feature/story-6538-t11-test-writing-guidance/feature-delta.md
rule() { awk '/^- \*\*Statistic\.\*\*/{p=1} /^\*\*Which model does what/{p=0} p' ; }
band() { awk '/^\| Band \| Wording sent to the maintainer \|/{p=1} /^A "low confidence" mark/{p=0} p' ; }
echo "--- verdict rule text: current vs 3ad54cfa0 (empty = same)"
git show 3ad54cfa0:$F | rule > /tmp/rule_a.txt; rule < $F > /tmp/rule_b.txt; wc -l < /tmp/rule_a.txt; diff /tmp/rule_a.txt /tmp/rule_b.txt
echo "--- band wording: current vs f249408df (empty = same)"
git show f249408df:$F | band > /tmp/band_a.txt; band < $F > /tmp/band_b.txt; wc -l < /tmp/band_a.txt; diff /tmp/band_a.txt /tmp/band_b.txt
echo "--- end"

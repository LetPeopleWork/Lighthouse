"""Frontend audit: validates labels.tsv against the listing and prints the per-file rows and sums.

usage: python3 -I tally.py <picks.json> <labels.tsv> [<second-rater.tsv>]
"""
import json
import sys
from collections import Counter

picks = json.load(open(sys.argv[1]))
rows = {}
for line in open(sys.argv[2]):
    if line.startswith("#") or not line.strip():
        continue
    n, labels, e, call, note = line.rstrip("\n").split("\t")
    rows[int(n)] = (labels, e, call, note)
tot = Counter()
bad = 0
print("| # | File | Tests | Logic | UI | Mixed | E | Call | Note |")
print("|---|---|---|---|---|---|---|---|---|")
for n, p in enumerate(picks, 1):
    labels, e, call, note = rows[n]
    c = Counter(labels)
    if len(labels) != len(p["tests"]):
        bad += 1
        print("LENGTH MISMATCH", n, p["file"], len(labels), len(p["tests"]))
    tot.update(c)
    short = p["file"]
    print(f"| {n} | {short} | {len(labels)} | {c['L']} | {c['U']} | {c['M']} | {e} | {call} | {note} |")
print(f"| | Sum | {sum(tot.values())} | {tot['L']} | {tot['U']} | {tot['M']} | | |")
print("files:", len(rows), "mismatches:", bad)

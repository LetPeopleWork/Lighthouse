"""Compares redo_by_hand.txt with picks.json: prints MATCH or the differences.

usage: python3 -I cmp_hand.py <redo_by_hand.txt> <picks.json>
"""
import json, sys
picks = json.load(open(sys.argv[2]))
want = {}
for p in picks:
    want[(p["group"], p["i"])] = (p["pos"], p["file"])
got = []
group = None
for l in open(sys.argv[1]):
    if l.startswith("pages/Settings:"): group = "pages/Settings"
    elif l.startswith("components/Common without"): group = "components/Common (without Charts)"
    else:
        i, pos, f = l.split()
        got.append(((group, int(i)), (int(pos[1:]), f)))
bad = [g for g in got if want[g[0]] != g[1]]
print("compared", len(got), "picks;", "MATCH" if not bad else bad)

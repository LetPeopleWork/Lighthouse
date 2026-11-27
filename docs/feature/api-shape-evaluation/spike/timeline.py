# Spike probe: prints the API call timeline of one measured open.
# Usage: python3 -I timeline.py results.json <page> <profile> [run]
import json
import sys

file, page, profile = sys.argv[1:4]
run = int(sys.argv[4]) if len(sys.argv) > 4 else 0
r = [x for x in json.load(open(file)) if x["page"] == page and x["profile"] == profile and x["run"] == run][0]
print(f"{'start':>6} {'end':>6} {'ms':>6} {'bytes':>8}  url")
for c in sorted(r["calls"], key=lambda c: c["start"]):
    print(f"{c['start']:6} {c['end']:6} {c['end'] - c['start']:6} {c['bytes']:8}  {c['url'][:90]}")

"""Frontend audit: checks that picks.json lists each file's tests in source-line order (labels are written in that order).

usage: python3 -I check_order.py <vitest-list.json> <picks.json>
"""
import json
import sys

listing = json.load(open(sys.argv[1]))
picks = json.load(open(sys.argv[2]))
by_file = {}
for t in listing:
    f = t["file"].split("/Lighthouse.Frontend/src/")[1]
    by_file.setdefault(f, []).append((t["location"]["line"], t["name"]))
bad = 0
for p in picks:
    in_listing_order = [n for _, n in by_file[p["file"]]]
    by_line = [n for _, n in sorted(by_file[p["file"]], key=lambda x: x[0])]
    if in_listing_order != by_line or in_listing_order != p["tests"]:
        bad += 1
        print("ORDER DIFFERS", p["file"])
print("files checked:", len(picks), "order differences:", bad)

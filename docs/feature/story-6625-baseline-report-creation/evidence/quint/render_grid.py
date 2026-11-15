"""Deterministic renderer for the S1 grid printed by `quint repl` (runs/s1grid.out).
Usage: python3 -I render_grid.py runs/s1grid.out"""
import re
import sys
from datetime import date, timedelta

BASE = date(2026, 11, 15)


def d(n):
    return (BASE + timedelta(days=n - 1000)).isoformat()


text = open(sys.argv[1], encoding="utf-8").read()
rows = []
for block in re.findall(r"\{([^{}]*)\}", text):
    f = dict(re.findall(r"(\w+): (\"[^\"]*\"|-?\d+|true|false)", block))
    if "L" in f:
        rows.append({k: val.strip('"') for k, val in f.items()})
rows.sort(key=lambda r: (int(r["L"]), int(r["sinceThenEnded"]), int(r["cutoffMinusThenStart"])))
for r in rows:
    since = int(r["sinceThenEnded"])
    when = ("ending tomorrow" if since == -1 else "ending today" if since == 0
            else f"ending {since} days ago" + (" (back-to-back default)" if r["isDefault"] == "true" else ""))
    c = int(r["cutoffMinusThenStart"])
    cut = {-1: "kept history starts 1 day before Then", 0: "kept history starts on Then's first day",
           1: "kept history starts 1 day after Then's first day"}[c]
    line = (f"L={r['L']}, Then {when}, {cut}: Then {d(int(r['thenStart']))}..{d(int(r['thenEnd']))} -> {r['result']}; "
            f"Now {d(int(r['nowStart']))}..{d(int(r['nowEnd']))}")
    if r["overlaps"] == "true":
        line += f"; 'Now overlaps Then until {d(int(r['overlapUntil']))}' ({r['sharedDays']} shared days)"
    print(line)
m = re.search(r"\n(Set\(\"[^)]*\))\s*$", text)
print("tooShort (L=7 and L=13):", m.group(1) if m else "not found")

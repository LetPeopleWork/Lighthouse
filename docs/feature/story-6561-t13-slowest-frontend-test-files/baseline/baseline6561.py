"""Reads a Vitest JSON report of the seven Story 6561 files and writes, per file, the sorted
test-id list and the count of `expect(` calls, plus a summary table.

usage: baseline6561.py <report.json> <frontend-root> <out-dir>
"""
import json
import os
import re
import sys

report_path, frontend_root, out_dir = sys.argv[1:4]
os.makedirs(out_dir, exist_ok=True)

with open(report_path, encoding="utf-8") as handle:
    report = json.load(handle)

rows = []
for suite in report["testResults"]:
    absolute = suite["name"]
    relative = os.path.relpath(absolute, frontend_root)
    ids = sorted(f"{relative} :: {t['fullName']}" for t in suite["assertionResults"])
    states = sorted({t["status"] for t in suite["assertionResults"]})
    with open(absolute, encoding="utf-8") as source:
        expects = len(re.findall(r"\bexpect\(", source.read()))
    stem = os.path.basename(relative)
    with open(os.path.join(out_dir, stem + ".ids.txt"), "w", encoding="utf-8") as out:
        out.write("\n".join(ids) + "\n")
    with open(os.path.join(out_dir, stem + ".expects.txt"), "w", encoding="utf-8") as out:
        out.write(f"{expects}\n")
    rows.append((relative, len(ids), expects, ",".join(states)))

rows.sort()
with open(os.path.join(out_dir, "summary.md"), "w", encoding="utf-8") as out:
    out.write("| File | Tests | `expect(` calls | States |\n|---|---|---|---|\n")
    for relative, tests, expects, states in rows:
        out.write(f"| `{relative}` | {tests} | {expects} | {states} |\n")
    out.write(f"| total | {sum(r[1] for r in rows)} | {sum(r[2] for r in rows)} | |\n")
print(open(os.path.join(out_dir, "summary.md"), encoding="utf-8").read())

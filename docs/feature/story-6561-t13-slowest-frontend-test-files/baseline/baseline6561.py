"""Reads a Vitest JSON report of the Story 6561 files and writes, per file, the sorted test-id list and the
count of `expect(` calls, plus a summary table.

usage: baseline6561.py --started <epoch-seconds> [--expected-tests N] <report.json> <frontend-root> <out-dir>

The project's JSON reporter writes `Lighthouse.Frontend/test-results.json` on every run, whatever `--outputFile` says,
and a run that crashes before the reporter finishes leaves the previous file in place. So the script refuses to read a
report that is older than the run (`--started`, taken before the run began) or whose test count differs from the run's
own summary line (`--expected-tests`, the N of "Tests  N passed (N)"), and exits 2 without writing anything.
"""
import argparse
import json
import os
import re
import sys

parser = argparse.ArgumentParser()
parser.add_argument("report")
parser.add_argument("frontend_root")
parser.add_argument("out_dir")
parser.add_argument("--started", type=float, required=True)
parser.add_argument("--expected-tests", type=int)
args = parser.parse_args()


def refuse(reason):
    sys.exit(f"REFUSED, nothing written: {reason}")


with open(args.report, encoding="utf-8") as handle:
    report = json.load(handle)

if os.path.getmtime(args.report) < args.started:
    refuse(f"{args.report} is older than the run (written before epoch {args.started:.0f})")
if report.get("startTime", 0) / 1000 < args.started - 1:
    refuse("the report's own startTime is before the run started")
counted = sum(len(s["assertionResults"]) for s in report["testResults"])
if counted != report["numTotalTests"]:
    refuse(f"report lists {counted} tests but says it ran {report['numTotalTests']}")
if args.expected_tests is not None and counted != args.expected_tests:
    refuse(f"report has {counted} tests, the run printed {args.expected_tests}")

os.makedirs(args.out_dir, exist_ok=True)
rows = []
for suite in report["testResults"]:
    absolute = suite["name"]
    relative = os.path.relpath(absolute, args.frontend_root)
    ids = sorted(f"{relative} :: {t['fullName']}" for t in suite["assertionResults"])
    states = sorted({t["status"] for t in suite["assertionResults"]})
    with open(absolute, encoding="utf-8") as source:
        expects = len(re.findall(r"\bexpect\(", source.read()))
    stem = os.path.basename(relative)
    with open(os.path.join(args.out_dir, stem + ".ids.txt"), "w", encoding="utf-8") as out:
        out.write("\n".join(ids) + "\n")
    with open(os.path.join(args.out_dir, stem + ".expects.txt"), "w", encoding="utf-8") as out:
        out.write(f"{expects}\n")
    rows.append((relative, len(ids), expects, ",".join(states)))

rows.sort()
with open(os.path.join(args.out_dir, "summary.md"), "w", encoding="utf-8") as out:
    out.write("| File | Tests | `expect(` calls | States |\n|---|---|---|---|\n")
    for relative, tests, expects, states in rows:
        out.write(f"| `{relative}` | {tests} | {expects} | {states} |\n")
    out.write(f"| total | {sum(r[1] for r in rows)} | {sum(r[2] for r in rows)} | |\n")
print(open(os.path.join(args.out_dir, "summary.md"), encoding="utf-8").read())

"""List StrykerJS survivors with file, line and replacement, so each can be judged by hand."""

import json
import sys

report = json.load(open(sys.argv[1]))

for path, info in report["files"].items():
    mutants = info.get("mutants", [])
    survivors = [m for m in mutants if m["status"] == "Survived"]
    uncovered = [m for m in mutants if m["status"] == "NoCoverage"]

    if not survivors and not uncovered:
        continue

    print(f"\n{path}")

    for mutant in sorted(survivors, key=lambda m: m["location"]["start"]["line"]):
        line = mutant["location"]["start"]["line"]
        replacement = (mutant.get("replacement") or "").replace("\n", " ")[:90]
        print(f"  SURVIVED  L{line:<5} {mutant['mutatorName']:<22} -> {replacement}")

    for mutant in sorted(uncovered, key=lambda m: m["location"]["start"]["line"]):
        line = mutant["location"]["start"]["line"]
        replacement = (mutant.get("replacement") or "").replace("\n", " ")[:90]
        print(f"  NO COVER  L{line:<5} {mutant['mutatorName']:<22} -> {replacement}")

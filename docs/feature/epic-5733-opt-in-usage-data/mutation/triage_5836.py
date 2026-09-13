"""Group a Stryker.NET report by file and list survivors. Scope check first, score second."""

import collections
import json
import sys

report = json.load(open(sys.argv[1]))

per_file = collections.Counter()
survivors = []

for path, info in report["files"].items():
    name = path.split("/")[-1]
    tested = [
        m for m in info["mutants"]
        if m["status"] not in ("Ignored", "NoCoverage", "CompileError")
    ]

    if tested:
        per_file[name] = len(tested)

    for mutant in tested:
        if mutant["status"] == "Survived":
            survivors.append((
                name,
                mutant["location"]["start"]["line"],
                mutant["mutatorName"],
                (mutant.get("replacement") or "").replace("\n", " ")[:70],
            ))

print("TESTED MUTANTS PER FILE:")
for name, count in per_file.most_common():
    print(f"  {count:4d}  {name}")

print(f"\nSURVIVORS ({len(survivors)}):")
for name, line, mutator, replacement in sorted(survivors):
    print(f"  {name}:{line:<5} {mutator:<22} -> {replacement}")

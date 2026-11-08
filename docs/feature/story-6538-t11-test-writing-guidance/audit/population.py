"""Frontend audit: population, quotas and picks.

usage: python3 -I population.py <git-ls-files.txt> <vitest-list.json> <out-dir>
Reads the git file list and the Vitest listing, compares them, recomputes quotas and picks.
Writes picks.json and prints the record's tables.
"""
import json
import math
import sys
from collections import defaultdict

PREFIX = "Lighthouse.Frontend/src/"
GROUPS = [
    ("components/Common/Charts", 5),
    ("components/Common (without Charts)", 15),
    ("pages/Common/MetricsView", 4),
    ("pages/Portfolios", 7),
    ("pages/Common/Reports + pages/Reports", 4),
    ("pages/Settings", 3),
    ("pages/Teams", 6),
    ("the rest", 6),
]


def group_of(path):
    if path.startswith("components/Common/Charts/"):
        return GROUPS[0][0]
    if path.startswith("components/Common/"):
        return GROUPS[1][0]
    if path.startswith("pages/Common/MetricsView/"):
        return GROUPS[2][0]
    if path.startswith("pages/Portfolios/"):
        return GROUPS[3][0]
    if path.startswith("pages/Common/Reports/") or path.startswith("pages/Reports/"):
        return GROUPS[4][0]
    if path.startswith("pages/Settings/"):
        return GROUPS[5][0]
    if path.startswith("pages/Teams/"):
        return GROUPS[6][0]
    return GROUPS[7][0]


def main():
    git_list, vitest_json, out_dir = sys.argv[1:4]
    git_files = sorted(
        l.strip()[len(PREFIX):] for l in open(git_list) if l.strip().endswith(".test.tsx")
    )
    listing = json.load(open(vitest_json))
    tests = defaultdict(list)
    for t in listing:
        f = t["file"].split("/Lighthouse.Frontend/src/")[1]
        tests[f].append(t["name"])
    v_tsx = sorted(f for f in tests if f.endswith(".test.tsx"))
    print("git ls-files .test.tsx:", len(git_files))
    print("vitest listing files .test.tsx:", len(v_tsx))
    print("vitest listing files total:", len(tests), " tests total:", sum(len(v) for v in tests.values()))
    print("tsx tests in listing:", sum(len(tests[f]) for f in v_tsx))
    only_git = sorted(set(git_files) - set(v_tsx))
    only_vitest = sorted(set(v_tsx) - set(git_files))
    print("in git ls-files, not in listing:", only_git)
    print("in listing, not in git ls-files:", only_vitest)

    total = len(git_files)
    by_group = defaultdict(list)
    for f in git_files:
        by_group[group_of(f)].append(f)
    quotas = {}
    for g, _ in GROUPS:
        n = len(by_group[g])
        quotas[g] = int(math.floor(50 * n / total + 0.5))
    diff = 50 - sum(quotas.values())
    biggest = max(by_group, key=lambda g: len(by_group[g]))
    quotas[biggest] += diff
    print("\n| Group | Files | Tests | 50*n/N | Quota | design quota |")
    design = dict(GROUPS)
    for g, _ in GROUPS:
        n = len(by_group[g])
        tcount = sum(len(tests[f]) for f in by_group[g])
        print(f"| {g} | {n} | {tcount} | {50*n/total:.2f} | {quotas[g]} | {design[g]} |")
    print(f"| Total | {total} | {sum(len(tests[f]) for f in git_files)} | | {sum(quotas.values())} | 50 |")
    print("rounding correction to largest group:", diff, biggest)

    picks = []
    print("\npicks (group, i, position, file, tests):")
    for g, _ in GROUPS:
        files = by_group[g]
        q = quotas[g]
        pos = [math.ceil(i * len(files) / q) for i in range(1, q + 1)]
        assert pos[-1] == len(files), g
        print(f"{g}: files={len(files)} quota={q} positions={pos}")
        for i, p in enumerate(pos, 1):
            f = files[p - 1]
            picks.append({"group": g, "i": i, "pos": p, "file": f, "tests": tests[f]})
            print(f"  {i:>2} @{p:>3} {f} ({len(tests[f])})")
    print("sample files:", len(picks), " sample tests:", sum(len(p["tests"]) for p in picks))
    json.dump(picks, open(out_dir + "/picks.json", "w"), indent=1)
    json.dump({g: by_group[g] for g, _ in GROUPS}, open(out_dir + "/groups.json", "w"), indent=1)


main()

"""Per-file time split of a Vitest run.

Usage: python3 -I split.py <per-file-split.json> [top-n]        (output of perFileSplitReporter.mjs)
       python3 -I split.py <test-results.json[.gz]>              (stock JSON reporter: wall span per file only)

The stock JSON reporter has only startTime/endTime per file (their span includes import, environment,
setup and the tests together, so it cannot be split). The split reporter adds, per file, the
environment, prepare, collect (import plus suite bodies), setup and tests/hooks milliseconds.
Prints the summed split across all files and the top-n files (default 10) by total.
"""
import gzip
import json
import sys


def load(path):
    opener = gzip.open if path.endswith(".gz") else open
    with opener(path, "rt") as fh:
        return json.load(fh)


def main(path, top):
    data = load(path)
    if isinstance(data, dict):  # stock JSON reporter
        rows = sorted(((r["endTime"] - r["startTime"], r["name"]) for r in data["testResults"]), reverse=True)
        print(f"stock JSON: {len(rows)} files, summed span {sum(r[0] for r in rows) / 1000:.0f} s (import + environment + setup + tests together)")
        for ms, name in rows[:top]:
            print(f"{ms / 1000:8.1f} s  {name}")
        return
    keys = ["environmentSetupMs", "prepareMs", "collectMs", "setupMs", "testsMs"]
    total = {k: sum(r[k] for r in data) for k in keys}
    grand = sum(total.values()) or 1
    print(f"{len(data)} files; summed worker time {grand / 1000:.0f} s")
    for k in keys:
        print(f"  {k:20s} {total[k] / 1000:8.1f} s  {total[k] / grand * 100:5.1f} %")
    print("top files by total:")
    for r in sorted(data, key=lambda r: -sum(r[k] for k in keys))[:top]:
        parts = " ".join(f"{k[:-2]} {r[k]:.0f}" for k in keys)
        print(f"  {sum(r[k] for k in keys) / 1000:6.1f} s  {r['file']}  ({parts} ms)")


if __name__ == "__main__":
    if len(sys.argv) < 2:
        sys.exit(__doc__)
    main(sys.argv[1], int(sys.argv[2]) if len(sys.argv) > 2 else 10)

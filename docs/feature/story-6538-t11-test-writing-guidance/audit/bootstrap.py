"""Frontend audit: Low / Middle / High shares with a percentile bootstrap, and the band.

usage: python3 -I bootstrap.py <picks.json> <labels.tsv> <N>
Statistic (test-count weighted, pooled over the sample):
  Low    = logic tests in E0 files / all tests
  Middle = logic tests in E0 and E1 files / all tests
  High   = logic plus mixed tests in files of every class / all tests
Interval: files resampled with replacement within their group, 10,000 resamples, seed 6538,
2.5th and 97.5th percentiles (linear interpolation between order statistics).
Band (lower ends only): 1 Low>=N; 2 Middle>=N>Low; 3 High>=N>Middle; 4 High<N.
"""
import json
import random
import sys
from collections import Counter, defaultdict

RESAMPLES = 10_000
SEED = 6538


def percentile(sorted_values, q):
    pos = q * (len(sorted_values) - 1)
    lo = int(pos)
    hi = min(lo + 1, len(sorted_values) - 1)
    return sorted_values[lo] + (sorted_values[hi] - sorted_values[lo]) * (pos - lo)


def load(picks_path, labels_path):
    picks = json.load(open(picks_path))
    labels = {}
    for line in open(labels_path):
        if line.startswith("#") or not line.strip():
            continue
        n, s, e, call, note = line.rstrip("\n").split("\t")
        labels[int(n)] = (s, e)
    files = []
    for n, p in enumerate(picks, 1):
        s, e = labels[n]
        c = Counter(s)
        files.append({"group": p["group"], "file": p["file"], "tests": len(s), "L": c["L"], "M": c["M"], "E": e})
    return files


def stats(files):
    total = sum(f["tests"] for f in files)
    low = sum(f["L"] for f in files if f["E"] == "E0")
    mid = sum(f["L"] for f in files if f["E"] in ("E0", "E1"))
    high = sum(f["L"] + f["M"] for f in files)
    return low / total, mid / total, high / total


def main():
    picks_path, labels_path, n_used = sys.argv[1], sys.argv[2], float(sys.argv[3])
    files = load(picks_path, labels_path)
    for f in files:
        if f["E"] == "-" and (f["L"] or f["M"]):
            print("CLASS '-' HOLDS LOGIC OR MIXED:", f["file"])
    by_group = defaultdict(list)
    for f in files:
        by_group[f["group"]].append(f)
    point = stats(files)
    rng = random.Random(SEED)
    draws = ([], [], [])
    for _ in range(RESAMPLES):
        sample = []
        for g in sorted(by_group):
            members = by_group[g]
            sample.extend(rng.choice(members) for _ in members)
        for series, value in zip(draws, stats(sample)):
            series.append(value)
    print(f"files {len(files)}, tests {sum(f['tests'] for f in files)}")
    print(f"method: percentile bootstrap, files resampled within group, {RESAMPLES} resamples, seed {SEED}, 2.5th and 97.5th percentiles, test-count weighted pooled ratio")
    print(f"N used: {n_used:.4f}")
    print("| Share | Point | Lower end (2.5th) | Upper end (97.5th) |")
    print("|---|---|---|---|")
    lowers = []
    for name, p, series in zip(("Low", "Middle", "High"), point, draws):
        series.sort()
        lo, hi = percentile(series, 0.025), percentile(series, 0.975)
        lowers.append(lo)
        print(f"| {name} | {p*100:.1f} % | {lo*100:.1f} % | {hi*100:.1f} % |")
    low_lo, mid_lo, high_lo = lowers
    if low_lo >= n_used:
        band = 1
    elif mid_lo >= n_used:
        band = 2
    elif high_lo >= n_used:
        band = 3
    else:
        band = 4
    print(f"band {band} (Low lower {low_lo*100:.1f} %, Middle lower {mid_lo*100:.1f} %, High lower {high_lo*100:.1f} %, against N {n_used*100:.1f} %)")


main()

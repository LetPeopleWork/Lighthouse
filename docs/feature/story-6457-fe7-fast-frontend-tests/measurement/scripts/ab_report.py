"""Summarise an ab.sh output folder against the A/B acceptance rule.

Usage: python3 -I ab_report.py <out-dir>

Reads <out-dir>/runs.txt (CSV lines from measure.sh) and the saved <label>.json.gz files, with labels
A-<n> (baseline) and B-<n> (candidate). Prints median wall and min-max per arm, peak memory per arm,
and the three checkable verdict items:
  1. median wall of the candidate at least 10 % lower, ranges not overlapping;
  2. sorted "file :: full test name" lists identical across all runs, and no failed test;
  3. median peak memory not more than 10 % above the baseline.
The other items (3 consecutive green runs, shuffled runs, CI arm) are judged by the lead.
"""
import gzip
import json
import os
import re
import statistics
import sys


def read_csv(path):
    rows = {}
    with open(path) as fh:
        for line in fh:
            parts = line.strip().split(",")
            if len(parts) == 6 and re.fullmatch(r"[AB]-\d+", parts[0]):
                rows[parts[0]] = {
                    "exit": int(parts[1]), "wall": float(parts[2]), "rss": float(parts[3]),
                    "avail": float(parts[4]), "threads": int(parts[5]),
                }
    return rows


def test_ids(path):
    """Return (sorted ids, failed count) from a saved Vitest JSON."""
    with gzip.open(path, "rt") as fh:
        data = json.load(fh)
    ids, failed = [], 0
    for result in data["testResults"]:
        name = result["name"]
        marker = "Lighthouse.Frontend/"
        file = name.split(marker, 1)[1] if marker in name else name
        for test in result["assertionResults"]:
            ids.append(file + " :: " + test["fullName"])
            if test["status"] == "failed":
                failed += 1
    return sorted(ids), failed


def arm(rows, prefix):
    return {k: v for k, v in rows.items() if k.startswith(prefix)}


def summary(runs, key):
    values = [r[key] for r in runs.values()]
    return statistics.median(values), min(values), max(values)


def main(out):
    rows = read_csv(os.path.join(out, "runs.txt"))
    a, b = arm(rows, "A-"), arm(rows, "B-")
    if not a or not b:
        sys.exit("need runs for both arms")
    print(f"pairs: baseline {len(a)}, candidate {len(b)}; non-zero exits: "
          f"{[k for k, v in rows.items() if v['exit'] != 0] or 'none'}")
    am, alo, ahi = summary(a, "wall")
    bm, blo, bhi = summary(b, "wall")
    print(f"wall s   baseline median {am:.0f} (min-max {alo:.0f}-{ahi:.0f}); candidate median {bm:.0f} (min-max {blo:.0f}-{bhi:.0f})")
    change = (bm - am) / am * 100
    print(f"1. median change {change:+.1f} %, ranges {'do not overlap' if bhi < alo else 'overlap'} -> "
          f"{'PASS' if change <= -10 and bhi < alo else 'FAIL'}")

    reference, ref_label, bad, failed_total = None, "", [], 0
    for label in sorted(rows):
        path = os.path.join(out, label + ".json.gz")
        if not os.path.exists(path):
            bad.append(label + " (no JSON)")
            continue
        ids, failed = test_ids(path)
        failed_total += failed
        if reference is None:
            reference, ref_label = ids, label
        elif ids != reference:
            bad.append(f"{label} differs from {ref_label} ({len(ids)} vs {len(reference)} ids)")
    print(f"2. test ids: {len(reference or [])} per run, failed tests {failed_total}, mismatches {bad or 'none'} -> "
          f"{'PASS' if not bad and failed_total == 0 else 'FAIL'}")

    am_, alo_, ahi_ = summary(a, "rss")
    bm_, blo_, bhi_ = summary(b, "rss")
    mem = (bm_ - am_) / am_ * 100
    print(f"   peak RSS MB baseline median {am_:.0f} ({alo_:.0f}-{ahi_:.0f}); candidate median {bm_:.0f} ({blo_:.0f}-{bhi_:.0f})")
    print(f"3. memory {mem:+.1f} % -> {'PASS' if mem <= 10 else 'FAIL'}")


if __name__ == "__main__":
    if len(sys.argv) != 2:
        sys.exit(__doc__)
    main(sys.argv[1])

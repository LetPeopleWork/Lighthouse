"""Frontend audit: seconds from the test-timings-frontend CSV of a green main run.

usage: python3 -I timings.py <test-timings-frontend.csv> <picks.json> <labels.tsv>
Prints the ten files with the most seconds, and the seconds of the sample's tests by label.
A test is matched to the listing by its describe path joined with single spaces (the CSV's form).
"""
import csv
import json
import sys
from collections import defaultdict

csv_path, picks_path, labels_path = sys.argv[1:4]
rows = list(csv.DictReader(open(csv_path)))
by_file = defaultdict(float)
count = defaultdict(int)
by_name = {}
for r in rows:
    ms = float(r["duration_ms"])
    by_file[r["file"]] += ms
    count[r["file"]] += 1
    by_name[(r["file"], r["test_name"])] = ms
tsx = {f: s for f, s in by_file.items() if f.endswith(".test.tsx")}
print("rows:", len(rows), " files:", len(by_file), " tsx files:", len(tsx))
print("total seconds, all tests: %.1f; .test.tsx tests: %.1f" % (sum(by_file.values()) / 1000, sum(tsx.values()) / 1000))
print("\nten files with the most seconds (sum of their tests' durations):")
print("| Rank | File | Tests | Seconds |")
print("|---|---|---|---|")
for i, (f, ms) in enumerate(sorted(by_file.items(), key=lambda kv: -kv[1])[:10], 1):
    print(f"| {i} | {f} | {count[f]} | {ms/1000:.2f} |")

picks = json.load(open(picks_path))
labels = {}
for line in open(labels_path):
    if line.startswith("#") or not line.strip():
        continue
    n, s, e, call, note = line.rstrip("\n").split("\t")
    labels[int(n)] = (s, e)
sec = defaultdict(float)
matched = defaultdict(int)
unmatched = defaultdict(int)
top10 = {f for f, _ in sorted(by_file.items(), key=lambda kv: -kv[1])[:10]}
chosen_by_rule = [p["file"] for p in picks if "src/" + p["file"] in top10]
for n, p in enumerate(picks, 1):
    s, e = labels[n]
    for lab, name in zip(s, p["tests"]):
        key = ("src/" + p["file"], name.replace(" > ", " "))
        if key in by_name:
            sec[(lab, e)] += by_name[key]
            matched[lab] += 1
        else:
            unmatched[lab] += 1
total_sample = sum(sec.values())
print("\nsample seconds matched by name: %d of %d tests (unmatched: parametrised or renamed titles, %d)" % (
    sum(matched.values()), sum(matched.values()) + sum(unmatched.values()), sum(unmatched.values())))
print("sample seconds, matched tests: %.2f" % (total_sample / 1000))
for lab, word in (("L", "logic"), ("M", "mixed"), ("U", "UI")):
    print("  %s: %.2f s over %d matched tests" % (word, sum(v for (l, e), v in sec.items() if l == lab) / 1000, matched[lab]))
low = sum(v for (l, e), v in sec.items() if l == "L" and e == "E0")
mid = sum(v for (l, e), v in sec.items() if l == "L" and e in ("E0", "E1"))
high = sum(v for (l, e), v in sec.items() if l in ("L", "M"))
print("seconds in the Low tests (logic in E0 files): %.2f s (%.1f %% of the sample's matched seconds)" % (low / 1000, 100 * low / total_sample))
print("seconds in the Middle tests (logic in E0 and E1 files): %.2f s (%.1f %%)" % (mid / 1000, 100 * mid / total_sample))
print("seconds in the High tests (logic plus mixed): %.2f s (%.1f %%)" % (high / 1000, 100 * high / total_sample))
print("top-ten files that the pick rule also chose:", chosen_by_rule or "none")

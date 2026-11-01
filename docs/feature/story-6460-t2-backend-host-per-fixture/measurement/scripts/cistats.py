"""Per-class median of summed test seconds across CI runs.

usage: cistats.py <input-dir> [<output-file>]

<input-dir> holds one folder per CI run, each with a test-timings-backend.csv (as downloaded from the
test-timings-backend artifact: gh run download <run-id> -n test-timings-backend -D <input-dir>/ci<run-id>).

The median for a class is taken over the runs in which the class is present. A class absent from a run
(a filter that left it out, a rename) is not counted as 0 seconds, because that would drag the median
down. The output has one line per class: median seconds, runs the class was present in, largest test
count seen in a run, class name. Compare "present in" with the number of runs before trusting a median.
"""
import csv
import glob
import os
import statistics
import collections
import sys

if len(sys.argv) < 2:
    sys.exit(__doc__)

input_dir = sys.argv[1]
output_file = sys.argv[2] if len(sys.argv) > 2 else os.path.join(input_dir, "ci_class_median.tsv")

files = sorted(glob.glob(os.path.join(input_dir, "ci*", "test-timings-backend.csv")))
per_run = []
for path in files:
    seconds = collections.Counter()
    count = collections.Counter()
    with open(path, newline="") as handle:
        for row in csv.DictReader(handle):
            name = row["fully_qualified_name"].split("(")[0]
            cls = name.rsplit(".", 1)[0]
            seconds[cls] += float(row["duration_ms"]) / 1000
            count[cls] += 1
    per_run.append((seconds, count))

print("runs", len(per_run))
classes = set().union(*[set(seconds) for seconds, _ in per_run]) if per_run else set()
median = {c: statistics.median([s[c] for s, _ in per_run if c in s]) for c in classes}
present = {c: sum(1 for s, _ in per_run if c in s) for c in classes}
tests = {c: max(n.get(c, 0) for _, n in per_run) for c in classes}
totals = [sum(s.values()) for s, _ in per_run]
if totals:
    print("summed test seconds per run", [round(t) for t in totals], "median", round(statistics.median(totals)))

ranked = sorted(median.items(), key=lambda item: -item[1])
with open(output_file, "w") as out:
    for c, value in ranked:
        out.write(f"{c}\t{value:.1f}\t{present[c]}\t{tests[c]}\n")

for c, value in ranked[:25]:
    print(f"{value:8.1f}  present {present[c]}/{len(per_run)}  {tests[c]:4d}  {c}")

import csv, glob, statistics, collections, sys
sp = sys.argv[1]
files = glob.glob(sp + "/ci*/test-timings-backend.csv")
per_run = []
for f in files:
    d = collections.Counter()
    n = collections.Counter()
    for r in csv.DictReader(open(f)):
        name = r["fully_qualified_name"].split("(")[0]
        cls = name.rsplit(".", 1)[0]
        d[cls] += float(r["duration_ms"]) / 1000
        n[cls] += 1
    per_run.append((d, n))
print("runs", len(per_run))
classes = set().union(*[set(d) for d, _ in per_run])
med = {c: statistics.median([d.get(c, 0) for d, _ in per_run]) for c in classes}
cnt = {c: max(n.get(c, 0) for _, n in per_run) for c in classes}
tot = [sum(d.values()) for d, _ in per_run]
print("summed test seconds per run", [round(t) for t in tot], "median", round(statistics.median(tot)))
with open(sp + "/ci_class_median.tsv", "w") as o:
    for c, v in sorted(med.items(), key=lambda kv: -kv[1]):
        o.write(f"{c}\t{v:.1f}\t{cnt[c]}\n")
for c, v in sorted(med.items(), key=lambda kv: -kv[1])[:25]:
    print(f"{v:8.1f}  {cnt[c]:4d}  {c}")

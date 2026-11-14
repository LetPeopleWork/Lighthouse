import csv, os, sys, statistics, collections
repo = '/storage/repos/Lighthouse/.claude/worktrees/indexed-wondering-panda'
sys.path.insert(0, os.path.join(repo, 'Scripts/test-timings'))
from check_budget import class_of  # noqa: E402
base = sys.argv[1]
runs = sys.argv[2].split(',')
newest = None
fam = collections.defaultdict(list)
cls_rows = collections.Counter()
tiers = []
for i, rid in enumerate(runs):
    rows = [r for r in csv.DictReader(open(os.path.join(base, rid, 'test-timings-backend.csv'))) if r['category'] == 'Integration' and r['outcome'] == 'Passed']
    tiers.append(sum(float(r['duration_ms']) for r in rows) / 1000)
    if i == 0:
        newest = set(class_of(r['fully_qualified_name']) for r in rows)
        for r in rows:
            cls_rows[class_of(r['fully_qualified_name'])] += 1
    t = collections.Counter()
    for r in rows:
        c = class_of(r['fully_qualified_name'])
        if c in newest:
            t['.'.join(c.split('.')[:-1])] += float(r['duration_ms']) / 1000
    for k, v in t.items():
        fam[k].append(v)
med = statistics.median(tiers)
print(f'tier sums {[round(x,1) for x in tiers]} median {med:.1f} spread {max(tiers)/min(tiers):.2f} 1%-line {med/100:.1f}s')
nrows = collections.Counter()
ncls = collections.Counter()
for c, n in cls_rows.items():
    k = '.'.join(c.split('.')[:-1]); nrows[k] += n; ncls[k] += 1
for m, k in sorted(((statistics.median(v), k) for k, v in fam.items()), reverse=True)[:20]:
    print(f'{m:7.1f} s  {100*m/med:4.1f}%  classes {ncls[k]:3d} rows {nrows[k]:4d}  {k.replace("Lighthouse.Backend.Tests.", "")}')

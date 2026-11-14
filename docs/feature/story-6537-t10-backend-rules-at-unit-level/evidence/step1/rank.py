"""Provisional T10 ranking: eligible plain main runs, per-class median of summed Integration seconds."""
import csv, os, sys, statistics, collections
repo = '/storage/repos/Lighthouse/.claude/worktrees/indexed-wondering-panda'
sys.path.insert(0, os.path.join(repo, 'Scripts/test-timings'))
from check_budget import class_of  # noqa: E402

base = sys.argv[1]
take = int(sys.argv[2]) if len(sys.argv) > 2 else 10
order = [l.split() for l in open(os.path.join(base, 'list.txt'))]  # newest first
FORCE_FULL_MARK = 'WorkTrackingConnectors.Jira.JiraWorkTrackingConnectorTest.'
eligible, table = [], []
for rid, sha, at in order:
    p = os.path.join(base, rid, 'test-timings-backend.csv')
    if not os.path.exists(p):
        continue
    rows = list(csv.DictReader(open(p)))
    failed = sum(1 for r in rows if r['outcome'] == 'Failed')
    full = any(FORCE_FULL_MARK in r['fully_qualified_name'] for r in rows)
    integ = [r for r in rows if r['category'] == 'Integration' and r['outcome'] == 'Passed']
    s = sum(float(r['duration_ms']) for r in integ) / 1000
    ok = failed == 0 and not full
    table.append((rid, sha, at, len(rows), failed, full, len(integ), s, ok))
    if ok:
        eligible.append((rid, sha, integ))
print('run | commit | at | rows | Failed | force-full | Integration n | sum s | mean s | eligible')
for rid, sha, at, n, f, full, ni, s, ok in table:
    print(f'{rid} | {sha} | {at} | {n} | {f} | {full} | {ni} | {s:.1f} | {s/max(ni,1):.3f} | {ok}')
use = eligible[:take]
print('\nranking runs:', ', '.join(r[1] for r in use))
newest = set(class_of(r['fully_qualified_name']) for r in use[0][2])
per = collections.defaultdict(list)
counts = {}
for rid, sha, integ in use:
    tot = collections.Counter()
    cnt = collections.Counter()
    for r in integ:
        c = class_of(r['fully_qualified_name'])
        tot[c] += float(r['duration_ms']) / 1000
        cnt[c] += 1
    for c in newest:
        if c in tot:
            per[c].append(tot[c])
    if rid == use[0][0]:
        counts = cnt
rank = sorted(((statistics.median(v), c, v) for c, v in per.items()), reverse=True)
total = sum(m for m, _, _ in rank)
print(f'\nsum of class medians {total:.1f} s over {len(rank)} classes')
print('rank | median s | rows | spread | class')
for i, (m, c, v) in enumerate(rank[:30], 1):
    print(f'{i} | {m:.1f} | {counts.get(c)} | {max(v)/min(v):.2f} | {c.replace("Lighthouse.Backend.Tests.", "")}')

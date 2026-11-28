"""T10 step 6 reading: before set (rebuilt, id method) against the first five eligible runs after 15d0e986f."""
import csv, json, os, statistics, sys

BASE = '/tmp/claude-1000/-storage-repos-Lighthouse/866d7411-3e78-408d-937c-b6dc9a6976a9/scratchpad/s6'
sys.path.insert(0, '/storage/repos/Lighthouse/.claude/worktrees/snoopy-wondering-cray/Scripts/test-timings')
from check_budget import class_of  # noqa: E402

BEFORE = ['43391361941', '43389215087', '43383069573', '43311419564', '43308085806']
AFTER = ['43636783145', '43693962328', '43696235093', '43708383920', '43830450020']
FAMILIES = ('.PercentilesOverTime.', '.ConfigureOncePerConnection.')
FORCE_FULL_MARK = 'JiraWorkTrackingConnectorTest.'


def load(rid):
    rows = list(csv.DictReader(open(os.path.join(BASE, rid, 'test-timings-backend.csv'))))
    sha = json.load(open(os.path.join(BASE, rid, 'jobs.json')))['headSha'][:9]
    return sha, rows


def integ(rows):
    return {r['fully_qualified_name']: float(r['duration_ms']) / 1000
            for r in rows if r['category'] == 'Integration' and r['outcome'] == 'Passed'}


def fam(name):
    return any(f in name for f in FAMILIES)


data = {rid: load(rid) for rid in BEFORE + AFTER}
before_ids = set(integ(data[BEFORE[0]][1]))
print('run | set | commit | rows | Failed | force-full | Integration n | sum s | mean s | id-method n | id-method s | families n | families s')
summary = {}
for rid in BEFORE + AFTER:
    sha, rows = data[rid]
    i = integ(rows)
    failed = sum(1 for r in rows if r['outcome'] == 'Failed')
    full = any(FORCE_FULL_MARK in r['fully_qualified_name'] for r in rows)
    kept = {k: v for k, v in i.items() if k in before_ids}
    f = {k: v for k, v in i.items() if fam(k)}
    summary[rid] = dict(n=len(i), s=sum(i.values()), kn=len(kept), ks=sum(kept.values()), fn=len(f), fs=sum(f.values()),
                        mean=sum(i.values()) / len(i))
    side = 'before' if rid in BEFORE else 'after'
    print(f"{rid} | {side} | {sha} | {len(rows)} | {failed} | {full} | {len(i)} | {summary[rid]['s']:.1f} | "
          f"{summary[rid]['mean']:.3f} | {len(kept)} | {summary[rid]['ks']:.1f} | {len(f)} | {summary[rid]['fs']:.1f}")


def med(ids, key):
    return statistics.median(summary[r][key] for r in ids)


def spread(ids, key):
    v = [summary[r][key] for r in ids]
    return max(v) / min(v), max(v) - min(v)

print()
for key, label in (('s', 'tier, raw'), ('ks', 'tier, id method (before ids only)'), ('fs', 'moved families')):
    b, a = med(BEFORE, key), med(AFTER, key)
    print(f'{label}: before median {b:.1f} s (spread {spread(BEFORE, key)[0]:.2f}, range {spread(BEFORE, key)[1]:.1f} s), '
          f'after median {a:.1f} s (spread {spread(AFTER, key)[0]:.2f}), fall {b - a:.1f} s = {(b - a) / med(BEFORE, "s") * 100:.1f} % of the before tier')
print(f"moved families rows: before {[summary[r]['fn'] for r in BEFORE]}, after {[summary[r]['fn'] for r in AFTER]}")
print(f"target P = 5.0 % of before tier median = {0.05 * med(BEFORE, 's'):.1f} s")
print(f"means: before {[round(summary[r]['mean'], 3) for r in BEFORE]}, after {[round(summary[r]['mean'], 3) for r in AFTER]}")
print(f"after sums {[round(summary[r]['s'], 1) for r in AFTER]}, counts {[summary[r]['n'] for r in AFTER]}")
# Unit rows that the moves added: Unit-category rows in the after newest that did not exist before, under the same families' new homes
bu = {r['fully_qualified_name'] for r in data[BEFORE[0]][1] if r['category'] != 'Integration'}
for rid in AFTER[:1]:
    new_other = [r for r in data[rid][1] if r['category'] != 'Integration' and r['fully_qualified_name'] not in bu]
    print(f'non-Integration rows new in {rid}: {len(new_other)}, {sum(float(r["duration_ms"]) for r in new_other) / 1000:.1f} s')

print('\nid control, per after run: before ids missing (moved families / other), after ids new, common-id sums')
bsets = {r: integ(data[r][1]) for r in BEFORE}
for rid in AFTER:
    a = integ(data[rid][1])
    missing = before_ids - set(a)
    mf = [k for k in missing if fam(k)]
    other = sorted(k for k in missing if not fam(k))
    new = set(a) - before_ids
    print(f'{rid}: missing {len(missing)} (families {len(mf)}, other {len(other)}), new {len(new)} '
          f'({sum(a[k] for k in new):.1f} s, families {sum(1 for k in new if fam(k))})')
    for k in other[:8]:
        print('   other missing:', k[:150])
common = set.intersection(*(set(integ(data[r][1])) for r in AFTER + BEFORE))
print(f'\ncommon ids over all ten runs: {len(common)}')
cb = statistics.median(sum(bsets[r][k] for k in common) for r in BEFORE)
ca = statistics.median(sum(integ(data[r][1])[k] for k in common) for r in AFTER)
print(f'common-id control: before median {cb:.1f} s, after median {ca:.1f} s, change {ca - cb:+.1f} s ({(ca - cb) / cb * 100:+.1f} %)')
moved = {k for k in before_ids if fam(k)} - set.union(*(set(integ(data[r][1])) for r in AFTER))
mb = [sum(bsets[r][k] for k in moved) for r in BEFORE]
print(f'moved ids: {len(moved)}; their seconds per before run {[round(x, 1) for x in mb]}, median {statistics.median(mb):.1f}, range {max(mb) - min(mb):.1f}')

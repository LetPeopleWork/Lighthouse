"""Per-class seconds of the moved classes still on the allowlist, over the after set and the before set."""
import csv, os, sys

BASE = '/tmp/claude-1000/-storage-repos-Lighthouse/866d7411-3e78-408d-937c-b6dc9a6976a9/scratchpad/s6'
sys.path.insert(0, '/storage/repos/Lighthouse/.claude/worktrees/snoopy-wondering-cray/Scripts/test-timings')
from check_budget import class_of  # noqa: E402

AFTER = ['43636783145', '43693962328', '43696235093', '43708383920', '43830450020']
BEFORE = ['43391361941', '43389215087', '43383069573', '43311419564', '43308085806']
CLASSES = ['Slice07WhereTheLimitsActuallyMovedTest', 'Slice05ReconstructCycleTimeHistoryTest',
           'Slice06EveryPercentileTabSpansTheSamePeriodTest']
for label, runs in (('before', BEFORE), ('after', AFTER)):
    for c in CLASSES:
        per = []
        for rid in runs:
            rows = csv.DictReader(open(os.path.join(BASE, rid, 'test-timings-backend.csv')))
            per.append(sum(float(r['duration_ms']) for r in rows
                           if r['outcome'] == 'Passed' and class_of(r['fully_qualified_name']).endswith('.' + c)) / 1000)
        print(f'{label} {c}: {[round(x, 1) for x in per]}, largest x 2.1 = {max(per) * 2.1:.1f} s')

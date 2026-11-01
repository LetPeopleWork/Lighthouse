import re, sys, glob, os, collections, csv, statistics
root = "/storage/repos/Lighthouse/Lighthouse.Backend/Lighthouse.Backend.Tests"
sp = sys.argv[1]
cat = collections.defaultdict(set)
for l in open(sp + "/s/out.txt"):
    m = re.match(r"(\S+\.cs) (\{.*\})", l)
    if m:
        for (k, w) in eval(m.group(2)):
            cat[m.group(1)].add(k)
cls_file = {}
parents = collections.defaultdict(set)
for p in glob.glob(root + "/**/*.cs", recursive=True):
    if "/obj/" in p or "/bin/" in p:
        continue
    t = open(p, encoding="utf-8-sig").read()
    rel = os.path.relpath(p, root)
    for m in re.finditer(r"\bclass\s+(\w+)(?:<[^>]*>)?\s*(?::\s*([^\n{]+))?", t):
        cls_file.setdefault(m.group(1), rel)
        if m.group(2):
            for b in re.findall(r"\w+", m.group(2)):
                parents[m.group(1)].add(b)
children = collections.defaultdict(set)
for c, ps in parents.items():
    for b in ps:
        children[b].add(c)
def desc(c, seen=None):
    seen = seen if seen is not None else set()
    for ch in children.get(c, ()):
        if ch not in seen:
            seen.add(ch)
            desc(ch, seen)
    return seen
local = {r["fixture"]: r for r in csv.DictReader(open("/tmp/fixture-setup-timings.csv"))}
per = collections.defaultdict(lambda: collections.defaultdict(list))
for f in glob.glob(sp + "/ci*/test-timings-backend.csv"):
    for r in csv.DictReader(open(f)):
        name = r["fully_qualified_name"]
        cls = name.split("(")[0].rsplit(".", 1)[0].rsplit(".", 1)[-1].rsplit("+", 1)[-1]
        per[cls][name].append(float(r["duration_ms"]))
stat = {}
for cls, tests in per.items():
    meds = sorted(statistics.median(v) for v in tests.values())
    stat[cls] = (len(meds), sum(meds) / 1000, meds[0], statistics.median(meds))
out = []
for f, ks in cat.items():
    if not (ks & {"SetUp", "test-body"}):
        continue
    if f.startswith("Integration/Containers/"):
        continue
    classes = {c for c, ff in cls_file.items() if ff == f}
    allc = set(classes)
    for c in classes:
        allc |= desc(c)
    allc = {c for c in allc if c in stat}
    instr = [c for c in allc if c in local]
    n = sum(stat[c][0] for c in allc)
    s = sum(stat[c][1] for c in allc)
    # overhead proxy: n * min (CI)
    ov = sum(stat[c][0] * stat[c][2] for c in allc) / 1000
    ovm = sum(stat[c][0] * min(stat[c][2] * 1.0, stat[c][3]) for c in allc) / 1000
    lset = sum(float(local[c]["setup_total_ms"]) for c in instr) / 1000
    out.append((ov, n, s, lset, len(instr), len(allc), f))
out.sort(reverse=True)
print("overhead_proxy_ci_s  tests  ci_sum_s  local_setup_s  instr/classes  file")
for r in out:
    print(f"{r[0]:8.1f} {r[1]:5d} {r[2]:8.1f} {r[3]:8.1f} {r[4]:3d}/{r[5]:3d} {r[6]}")
print("TOTAL files", len(out), "tests", sum(r[1] for r in out), "ci_sum", round(sum(r[2] for r in out)), "overhead proxy", round(sum(r[0] for r in out)))
un = [r for r in out if r[4] == 0]
print("UNINSTRUMENTED files", len(un), "tests", sum(r[1] for r in un), "ci_sum", round(sum(r[2] for r in un)), "overhead proxy", round(sum(r[0] for r in un)))

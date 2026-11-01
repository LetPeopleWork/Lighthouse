import re, collections, sys
cat = collections.defaultdict(set)
for l in open(sys.argv[1]):
    m = re.match(r"(\S+\.cs) (\{.*\})", l)
    if not m:
        continue
    d = eval(m.group(2))
    for (k, w) in d:
        cat[m.group(1)].add(k)
allf = set(cat)
print("total files", len(allf))
per_test = {f for f, s in cat.items() if s & {'SetUp', 'test-body'}}
helper_only = {f for f, s in cat.items() if 'helper' in s and f not in per_test}
rest = allf - per_test - helper_only
print("SetUp or test-body:", len(per_test))
print("helper only (no SetUp/test-body):", len(helper_only))
print("rest (OneTimeSetUp/ctor/class-level/unknown):", len(rest))
for f in sorted(helper_only):
    print("HELPER", f)
for f in sorted(rest):
    if cat[f] - {'OneTimeSetUp'}:
        print("REST-NONONETIME", f, cat[f])

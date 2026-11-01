import re, collections, sys
files = collections.defaultdict(lambda: collections.defaultdict(int))
for l in open(sys.argv[1]):
    m = re.match(r"(\S+\.cs) (\{.*\})", l)
    if not m:
        continue
    for (k, w), n in eval(m.group(2)).items():
        files[m.group(1)][(k, w)] += n
for what in ("new-factory", "derived"):
    c = collections.Counter()
    for f, d in files.items():
        ks = {k for (k, w) in d if w == what}
        for k in ks:
            c[k] += 1
    print(what, "files per kind (a file can count in several):", dict(c))
    pt = [f for f, d in files.items() if any(w == what and k in ("SetUp", "test-body") for (k, w) in d)]
    print(what, "files with it in SetUp or a test body:", len(pt))
    ot = [f for f, d in files.items() if any(w == what and k == "OneTimeSetUp" for (k, w) in d)]
    print(what, "files with it in OneTimeSetUp:", len(ot))
    print(what, "occurrences:", sum(n for d in files.values() for (k, w), n in d.items() if w == what))

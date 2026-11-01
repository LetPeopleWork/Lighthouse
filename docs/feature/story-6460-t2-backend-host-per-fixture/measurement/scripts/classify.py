"""Classify each file that starts a host by where it starts it (rough heuristic, used before the guard existed).

usage: classify.py <tests-root>

<tests-root> is the folder of the backend test project (Lighthouse.Backend/Lighthouse.Backend.Tests).
The guard (TestHostPerFixtureGuardTest) now owns the list; this stays for the record.
"""
import re, subprocess, sys, collections
if len(sys.argv) < 2:
    sys.exit(__doc__)
root = sys.argv[1]
files = subprocess.check_output(["git", "-C", root, "grep", "-lE", r"new TestWebApplicationFactory|WithWebHostBuilder\(|WithTestAuthentication\(", "--", "*.cs"], text=True).split()
host_re = re.compile(r"new TestWebApplicationFactory|WithWebHostBuilder\(|WithTestAuthentication\(")
ATTR = re.compile(r"^\s*\[(\w+)")
member_re = re.compile(r"^(\s*)(public|private|protected|internal|static|async|override|virtual|\w[\w<>\[\], ?]*)\s.*\)\s*$|^(\s*)(public|private|protected|internal)\s.*\(")
def kind_of(lines, i):
    ind = len(lines[i]) - len(lines[i].lstrip())
    # walk back to the enclosing member declaration: nearest earlier line with smaller indent that has '(' or is a field/prop, followed by attrs above
    j = i
    while j >= 0:
        l = lines[j]
        if l.strip() and (len(l) - len(l.lstrip())) < ind and not l.strip().startswith(("{", "}", "//", "///", "[")):
            # candidate declaration
            s = l.strip()
            if re.match(r"(public|private|protected|internal|static|async|override|virtual|protected internal)\b", s) or re.match(r"[A-Za-z_][\w<>\[\],? ]* \w+\(", s):
                break
        j -= 1
    if j < 0: return "unknown"
    decl = lines[j].strip()
    # class-level?
    if re.search(r"\b(class|record|struct|namespace)\b", decl): return "class-level"
    if "(" not in decl: return "field/prop"
    # attributes above
    attrs = []
    k = j - 1
    while k >= 0 and (lines[k].strip().startswith("[") or lines[k].strip().startswith("//") or lines[k].strip() == ""):
        m = ATTR.match(lines[k])
        if m: attrs.append(m.group(1))
        if lines[k].strip() == "": break
        k -= 1
    a = set(attrs)
    # constructor? name equals class: crude - decl without return type
    if "OneTimeSetUp" in a: return "OneTimeSetUp"
    if "SetUp" in a: return "SetUp"
    if a & {"Test", "TestCase", "TestCaseSource", "Theory", "Property"}: return "test-body"
    if "OneTimeTearDown" in a or "TearDown" in a: return "teardown"
    if re.match(r"(public|protected|private|internal)\s+[A-Z]\w*\(", decl): return "ctor"
    return "helper"
res = collections.defaultdict(lambda: collections.Counter())
perfile = collections.defaultdict(set)
for f in files:
    p = root + "/" + f
    lines = open(p, encoding="utf-8-sig").read().split("\n")
    for i, l in enumerate(lines):
        if host_re.search(l) and not l.strip().startswith("//"):
            k = kind_of(lines, i)
            what = "new-factory" if "new TestWebApplicationFactory" in l else "derived"
            res[f][(k, what)] += 1
            perfile[(k)].add(f)
for k, v in sorted(perfile.items()): print(k, len(v))
print()
for f in sorted(res):
    print(f, dict(res[f]))

"""Frontend audit: agreement between the two raters on the second rater's files.

usage: python3 -I agreement.py <labels.tsv> <rater2-labels.tsv>
Prints per-file agreement, the unrounded share, Cohen's kappa and the disagreeing tests (file, test number, labels).
"""
import sys
from collections import Counter

def read(p):
    d = {}
    for l in open(p):
        if l.startswith("#") or not l.strip():
            continue
        f = l.rstrip("\n").split("\t")
        d[int(f[0])] = f[1]
    return d

a, b = read(sys.argv[1]), read(sys.argv[2])
same = total = 0
pairs = Counter()
dis = []
for n in sorted(b):
    if len(a[n]) != len(b[n]):
        print("LENGTH", n, len(a[n]), len(b[n])); continue
    s = sum(x == y for x, y in zip(a[n], b[n]))
    print(f"pick {n}: {s}/{len(a[n])}")
    same += s; total += len(a[n])
    for i, (x, y) in enumerate(zip(a[n], b[n]), 1):
        pairs[(x, y)] += 1
        if x != y: dis.append((n, i, x, y))
share = same / total
ca = Counter(); cb = Counter()
for (x, y), c in pairs.items():
    ca[x] += c; cb[y] += c
pe = sum(ca[k] * cb[k] for k in "LUM") / total ** 2
print(f"agreement {same}/{total} = {share:.6f}; kappa {(share - pe) / (1 - pe):.4f}")
print("confusion (first, second):", dict(pairs))
print("disagreements:", len(dis))
for d in dis: print(d)

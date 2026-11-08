"""Frontend audit: writes labels.tsv from labels-round1.tsv with the rows of labels_round2.tsv replacing the label strings
(and the E class, when a third column is given).

usage: python3 -I apply_round2.py <labels-round1.tsv> <labels_round2.tsv> <labels.tsv>
"""
import sys
new = {}
for l in open(sys.argv[2]):
    f = l.rstrip("\n").split("\t")
    new[int(f[0])] = f[1:]
out = []
for l in open(sys.argv[1]):
    if l.startswith("#") or not l.strip():
        out.append(l); continue
    f = l.rstrip("\n").split("\t")
    if int(f[0]) in new:
        r = new[int(f[0])]
        f[1] = r[0]
        if len(r) > 1:
            f[2] = r[1]
    out.append("\t".join(f) + "\n")
open(sys.argv[3], "w").writelines(out)

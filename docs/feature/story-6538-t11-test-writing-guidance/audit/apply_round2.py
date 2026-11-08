"""Frontend audit: writes labels.tsv from labels-round1.tsv with the rows of labels_round2.tsv replacing the label strings.

usage: python3 -I apply_round2.py <labels-round1.tsv> <labels_round2.tsv> <labels.tsv>
"""
import sys
new = {}
for l in open(sys.argv[2]):
    n, s = l.rstrip("\n").split("\t")
    new[int(n)] = s
out = []
for l in open(sys.argv[1]):
    if l.startswith("#") or not l.strip():
        out.append(l); continue
    f = l.rstrip("\n").split("\t")
    if int(f[0]) in new:
        f[1] = new[int(f[0])]
    out.append("\t".join(f) + "\n")
open(sys.argv[3], "w").writelines(out)

"""Frontend audit: lists the tests the two raters labelled differently, with their titles.

usage: python3 -I disagreements.py <picks.json> <first.tsv> <second.tsv>
"""
import json, sys
picks = json.load(open(sys.argv[1]))
def read(p):
    return {int(l.split("\t")[0]): l.split("\t")[1] for l in open(p) if l.strip() and not l.startswith("#")}
a, b = read(sys.argv[2]), read(sys.argv[3])
for n in sorted(b):
    for i, (x, y) in enumerate(zip(a[n], b[n])):
        if x != y:
            t = picks[n - 1]["tests"][i].split(" > ")[-1]
            print(f"- pick {n}, test {i + 1}: first {x}, second {y} | {picks[n-1]['file'].split('/')[-1]} | {t}")

#!/usr/bin/env python3
# Usage: summarize.py <file.cpuprofile> ; prints self time by area, top functions, and self time by Lighthouse source file.
import json, sys, re, collections
p = json.load(open(sys.argv[1]))
nodes = {n["id"]: n for n in p["nodes"]}
self_us = collections.Counter()
dts = p["timeDeltas"]
for sid, dt in zip(p["samples"], dts):
    self_us[sid] += dt
total = sum(self_us.values())
def area(url, fn):
    m = re.search(r"node_modules/(?:\.pnpm/[^/]+/node_modules/)?((?:@[^/]+/)?[^/]+)", url)
    if m: return "pkg " + m.group(1)
    if "/Lighthouse.Frontend/src/" in url: return "lighthouse src"
    if url.startswith("node:"): return "node core"
    if not url: return "(native/vm)"
    return "other"
areas = collections.Counter(); funcs = collections.Counter(); src = collections.Counter()
for i, us in self_us.items():
    cf = nodes[i]["callFrame"]; a = area(cf["url"], cf["functionName"])
    areas[a] += us; funcs[(cf["functionName"] or "(anon)", a)] += us
    if a == "lighthouse src":
        src[cf["url"].split("Lighthouse.Frontend/")[-1]] += us
print(f"sampled {total/1e6:.1f} s\nself time by area:")
for k, v in areas.most_common(16): print(f"{100*v/total:7.1f} %  {k}")
print("top functions by self time:")
for (f, a), v in funcs.most_common(12): print(f"{100*v/total:7.1f} %  {f}  {a}")
print("self time by Lighthouse source file:")
for k, v in src.most_common(8): print(f"{100*v/total:7.1f} %  {k}")

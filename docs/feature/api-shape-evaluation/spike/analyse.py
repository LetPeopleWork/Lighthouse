# Spike probe: summarises results.json from measure-calls.mjs into markdown tables.
# Usage: python3 -I analyse.py results.json > summary.md
import collections
import json
import statistics
import sys

runs = json.load(open(sys.argv[1]))

# Calls every page makes, whatever it shows: the app shell (navigation, version, RBAC, licence, terminology...).
by_page = collections.defaultdict(list)
for r in runs:
    by_page[r["page"]].append(r)

fast_endpoints = {
    page: set(c["endpoint"] for r in rs if r["profile"] == "fast" for c in r["calls"])
    for page, rs in by_page.items()
}
shell = set.intersection(*fast_endpoints.values())


def med(values):
    return statistics.median(values) if values else 0


print("## Per page (median of runs)\n")
print("| Page | Requests | Distinct endpoints | Repeated calls | Shell calls | Page calls | Chain depth (slow 4G) | Data done fast (ms) | Data done slow 4G (ms) | Data done slow 3G (ms) | JS boot slow 4G (ms) | API KB |")
print("|---|---|---|---|---|---|---|---|---|---|---|---|")
for page, rs in by_page.items():
    f = [r for r in rs if r["profile"] == "fast"]
    s4 = [r for r in rs if r["profile"] == "slow4g"]
    s3 = [r for r in rs if r["profile"] == "slow3g"]
    shell_calls = med([sum(1 for c in r["calls"] if c["endpoint"] in shell) for r in f])
    print(
        f"| {page} | {med([r['requests'] for r in f])} | {med([r['distinctEndpoints'] for r in f])} | "
        f"{med([r['duplicates'] for r in f])} | {shell_calls} | {med([r['requests'] for r in f]) - shell_calls} | "
        f"{med([r['waterfallDepth'] for r in s4])} | {med([r['lastApiEnd'] for r in f]):.0f} | "
        f"{med([r['lastApiEnd'] for r in s4]):.0f} | {med([r['lastApiEnd'] for r in s3]):.0f} | "
        f"{med([r['firstApiStart'] for r in s4]):.0f} | {med([r['apiBytes'] for r in f]) / 1024:.0f} |"
    )

print("\n## App-shell endpoints (called on every page)\n")
for e in sorted(shell):
    print(f"- `{e}`")

print("\n## Repeated calls (same URL more than once in one load, fast profile, first run)\n")
rep = collections.Counter()
for page, rs in by_page.items():
    f = [r for r in rs if r["profile"] == "fast"][0]
    c = collections.Counter(x["endpoint"] for x in f["calls"])
    for e, n in c.items():
        if n > 1:
            rep[e] += n - 1
print("| Endpoint | Extra calls summed over all pages |")
print("|---|---|")
for e, n in rep.most_common():
    print(f"| `{e}` | {n} |")

print("\n## Longest chain per page (slow 4G, first run)\n")
for page, rs in by_page.items():
    s4 = [r for r in rs if r["profile"] == "slow4g"][0]
    print(f"- **{page}** ({s4['waterfallDepth']}): " + " → ".join(f"`{e}`" for e in s4["waterfallChain"]))

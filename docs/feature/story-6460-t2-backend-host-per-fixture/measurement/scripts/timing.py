import csv, sys
rows = list(csv.DictReader(open(sys.argv[1])))
for r in rows:
    for k in r:
        if k != "fixture":
            r[k] = float(r[k])
tot = sum(r["setup_total_ms"] for r in rows) / 1000
tdn = sum(r["teardown_total_ms"] for r in rows) / 1000
ots = sum(r["one_time_setup_ms"] for r in rows) / 1000
print("fixtures", len(rows), "tests(setup_count)", int(sum(r["setup_count"] for r in rows)))
print(f"SetUp total {tot:.1f}s teardown {tdn:.1f}s onetime-setup {ots:.1f}s")
# groups
def grp(r):
    if r["one_time_setup_ms"] > 0:
        return "B harness host per fixture (DB per test)"
    if r["setup_mean_ms"] >= 250:
        return "A host per test (mean >= 250 ms)"
    return "C IntegrationTestBase-style (host per fixture via ctor, DB per test; or light host)"
g = {}
for r in rows:
    k = grp(r)
    d = g.setdefault(k, [0, 0, 0.0, 0.0])
    d[0] += 1
    d[1] += r["setup_count"]
    d[2] += r["setup_total_ms"] / 1000
    d[3] += r["teardown_total_ms"] / 1000
for k, d in sorted(g.items()):
    print(f"{k}: fixtures {d[0]}, tests {int(d[1])}, setup {d[2]:.1f}s (mean {d[2]/d[1]*1000:.0f} ms), teardown {d[3]:.1f}s")
print()
print("top 10 by setup_total_ms")
for r in sorted(rows, key=lambda r: -r["setup_total_ms"])[:10]:
    print(f"{r['fixture']:55s} n={int(r['setup_count']):3d} setup={r['setup_total_ms']/1000:6.1f}s mean={r['setup_mean_ms']:6.0f}ms td={r['teardown_total_ms']/1000:5.1f}s onetime={r['one_time_setup_ms']:5.0f}ms {grp(r)[0]}")

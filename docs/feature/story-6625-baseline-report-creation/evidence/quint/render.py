"""Deterministic renderer: ITF trace of baseline.qnt -> plain-English lines by fixed templates.
Usage: python3 -I render.py trace.itf.json
Day 1000 = 2026-11-15. Display arithmetic mirrors baseline.qnt (integer, half-up)."""
import json
import sys
from datetime import date, timedelta

BASE = date(2026, 11, 15)


def v(x):
    if isinstance(x, dict) and "#bigint" in x:
        return int(x["#bigint"])
    if isinstance(x, dict) and "#map" in x:
        return {v(k): v(val) for k, val in x["#map"]}
    if isinstance(x, dict):
        return {k: v(val) for k, val in x.items() if k != "#meta"}
    return x


def d(n):
    return (BASE + timedelta(days=n - 1000)).isoformat()


def per_day(t, length):
    tenths = (20 * t + length) // (2 * length)
    return f"{tenths // 10}.{tenths % 10}"


def total_line(r):
    tt, nt = r["thenTotal"], r["nowTotal"]
    if tt < 0 or nt < 0:
        which = " and ".join(w for w, x in (("Then", tt), ("Now", nt)) if x < 0)
        return f"Throughput not captured for {which}; no change shown"
    diff = nt - tt
    arrow = "▲" if diff > 0 else ("▼" if diff < 0 else "=")
    head = (f"Total Then {tt} ({per_day(tt, r['len'])}/day), Now {nt} ({per_day(nt, r['len'])}/day): "
            f"{arrow} {abs(diff)}")
    if tt == 0:
        return head + " (Then was 0)"
    m = (200 * abs(diff) + tt) // (2 * tt)
    return head + f" ({'-' if diff < 0 else '+'}{m}%)"


def screen(rid, r, today):
    length = r["len"]
    ts, te = r["thenEnd"] - length + 1, r["thenEnd"]
    ns = today - length + 1
    s = (f"  report {rid}: Then {d(ts)}..{d(te)} ({length} days), "
         f"Now {d(ns)}..{d(today)} ({length} days), stored Now length {r['nowLen']}, version {r['version']}")
    if te >= ns:
        s += f"; shows 'Now overlaps Then until {d(te)}'"
    if r["thenTotal"] != -1 or r["nowTotal"] != -1:
        s += f"; {total_line(r)}"
    return s


def event_line(e, st):
    k = e["kind"]
    nl = "no Now length" if e["nowLen"] == -1 else f"Now length {e['nowLen']}"
    if k == "init":
        return (f"Start: today {d(st['today'])}, kept history starts {d(st['cutoff'])}; report 1 saved before "
                f"this Story with Then ending {d(e['thenEnd'])}, Then length {e['len']}, Now length {e['nowLen']}; "
                f"migration {'will' if st['migrationBumpsVersion'] else 'will not'} change the report version")
    if k in ("ui-create-default", "ui-create-ends-today"):
        how = "back to back (default)" if k.endswith("default") else "ending today"
        return (f"User creates a report in the UI, Then {how}: length {e['len']}, Then ending {d(e['thenEnd'])} "
                f"-> {e['result']}")
    if k == "api-create":
        return (f"API create request: Then ending {d(e['thenEnd'])}, length {e['len']}, {nl} -> {e['result']}")
    if k in ("open-edit", "open-edit-old-page"):
        page = "an old browser page that still sends Now length" if k.endswith("old-page") else "the new page"
        return f"User opens Edit for report {e['id']} in {page} ({nl})"
    if k == "save-edit":
        return (f"User saves the Edit of report {e['id']}: Then ending {d(e['thenEnd'])}, length {e['len']}, "
                f"{nl} -> {e['result']}")
    if k == "migrate":
        return f"Migration runs once: every saved report's Now length set to its Then length ({e['result']})"
    if k == "advance":
        return f"{e['len']} day(s) pass; today is now {d(st['today'])}"
    if k == "retention":
        return f"Kept history start moves {e['len']} days later, to {d(st['cutoff'])}"
    if k == "totals":
        tt = "not captured" if e["thenEnd"] == -1 else str(e["thenEnd"])
        nt = "not captured" if e["len"] == -1 else str(e["len"])
        return f"Throughput arrives for report {e['id']}: Then total {tt}, Now total {nt}"
    return f"Unknown event {k}"


def main(path):
    data = json.load(open(path, encoding="utf-8"))
    for i, raw in enumerate(data["states"]):
        st = v(raw)
        print(f"[{i}] {event_line(st['event'], st)}")
        if st["edit"]["open"]:
            print(f"  (Edit open for report {st['edit']['id']}, based on version {st['edit']['baseVersion']})")
        for rid in sorted(st["reports"]):
            print(screen(rid, st["reports"][rid], st["today"]))


if __name__ == "__main__":
    main(sys.argv[1])

# Over-time charts fill in the days they missed — Story 6053

Delivered 2026-09-22 → 2026-09-24 in five slices. Free, Preview, **off by default**. Pushed to `main`
2026-09-24; ships in the release cut from run 36018189797.

Percentiles Over Time and PBC Over Time only ever showed the days Lighthouse happened to record. A Team
added last week, or an instance that does not run every day, got a short line or one full of holes — even
though the Work Items needed to work those days out were already stored. This story lets the charts work
out the missing days from that stored history, in the background, when someone opens them.

## What shipped

| Slice | What it changed |
| --- | --- |
| 01 — the trend my data already supports | Opening either chart hands the gap to a background filler that works out each missing day with the recorder's own code, as of that day. Bounded by the owner's first finished item (the floor), the last sync (the ceiling), 90 worked-out days and 10 s per pass, oldest first. Two fills of one day leave one point. Database maintenance and the fill stand out of each other's way in both directions. The forward recorder stopped writing all-zero rows. |
| 02 — every tab spans the same period | Every Cycle Time look-back (30/60/90) fills over its own window; Work Item Age fills at the age items had reached on that day; the portfolio's delivery age tab too. |
| 03 — where the limits actually moved | Process Behaviour limits fill for every behaviour the scope reports, judged as of the day asked rather than as of today. A day with no usable baseline reports none. |
| 04 — say the true thing when empty | An empty period stays empty, nothing is invented to fill it, and both widgets say one sentence that is true whether the fill is on or off. |
| 05 — opt-in switch | The fill sits behind the instance-wide optional feature `OverTimeHistoryFill` — seeded off, Preview, free, toggled by a System Admin with no restart. Switching is logged. The frontend never reads the switch. |

Post-review fixes (all `fix(metrics)` on 2026-09-24): a queue-refused ask can be asked again; the fill never
reaches past the instance's today; an empty pass leaves the metrics cache alone; the shutdown drain honours
the host's stop; a failing day is logged once per pass; a restore turned away by the fill is let in next try;
**a past day is filled only from what retention still keeps** (last sync − `DoneItemsCutoffDays`, per family
window and pinned stretch).

No migration, no API contract change. Architecture: ADR-207 *(read-triggered reconstruction on its own
filler, with its 2026-09-24 amendment for the switch)* and ADR-208 *(a past day is computed by the recorder's
own code)*.

## Decisions worth keeping

- **Read-triggered, not scheduled.** Only a chart someone opens gets filled; nothing sweeps every owner.
- **Fill-if-absent, never rewrite.** An observed day always wins; a filled day is indistinguishable from a
  recorded one afterwards. Switching off stops further filling but **does not roll back data** — the docs
  tell an operator to take a backup first.
- **A filled day is worked out against today's configuration.** State mappings, Cycle Time definitions,
  blocked rules and deleted or re-parented Work Items may make it differ from what would have been recorded.
  Documented, not solved.
- **Opt-in for the first release.** #6083 flips the default on after first positive feedback (and must decide
  how to treat a *seeded* off, which nothing records); #6084 removes the switch.
- **Clients: copy, not contract.** Lighthouse-Clients' "never backfills" copy was rewritten to hold in both
  positions (lighthouse-clients `1ceb8e6`, patch changeset); no version gate.

## Lessons

- **Acceptance cover that cannot fail is the expensive failure.** 03-04 took two acceptance-designer passes
  before any production code, because scenarios seeded the product's default retention window, wide enough
  that the bug under test could not show. Arm the Given so the wrong answer is reachable.
- **A gate that sees itself.** After the maintenance gate learned about the filler, a filler reading
  `IsBlocked` would see its own pass and abandon every day with all tests green. The filler reads
  `IsMaintenanceOperationActive`; do not "simplify" it.
- **A budget-abandoned day must not be memoised** as worked out, or it becomes permanently unfillable.
- **The all-zero KPI was worded wrong.** Seven pre-existing all-zero rows (a deleted owner, pre-story
  recorder) remain because reconstruction never rewrites; none of the 428 added rows is all-zero. The KPI
  should have said "no *new* all-zero row".

## Quality

Backend 7298 / 0 on the standard filter at the last full run; frontend 5694. Mutation: backend
**89.82 %**, frontend **100 %** — [mutation-results.md](story-6053-reconstruct-over-time-history/mutation-results.md).

## Still open

- Fidelity across a **configuration change** has no probe; the mechanism is proven byte-equal only with the
  clock moved. Stated in the user docs as a known limit.
- Live dogfood of every process-behaviour family at both scopes (maintainer check).
- Delivery charts and Blocked Items Over Time still carry their own "builds forward from today" copy — not
  this story's charts, still true there.
- #6083 (default on) and #6084 (remove the switch), unparented in ADO.

Workspace: `docs/feature/story-6053-reconstruct-over-time-history/`.

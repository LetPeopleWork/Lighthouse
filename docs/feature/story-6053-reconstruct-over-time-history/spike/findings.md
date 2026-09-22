# SPIKE-01 findings — reconstruction fidelity, cost, absence

**Date**: 2026-09-22 | **Story**: 6053 | **Slice**: 01 (pre-slice probe) | **Status**: complete

## How it was run

A throwaway NUnit fixture (`Spike6053ReconstructionFidelityTest`, `[Explicit]`, deleted after this
record) constructed the **real** `TeamMetricsService` over a scratch copy of
`Lighthouse.Backend/Lighthouse.Backend/DB_Backup/LighthouseAppContext.db`, with a `FakeLighthouseClock`
pinned to 2026-09-22 UTC. Read-only: nothing was written to any snapshot table, and the scratch copy
(and its `-wal`/`-shm`) was deleted on teardown.

Reconstruction was simulated exactly as D1 describes it — the same service call the recorder makes,
with the window shifted:

```csharp
subject.GetCycleTimePercentilesForTeam(team, D.AddDays(-30), D)   // vs the stored row for day D
```

**Fixture**: Team 1 "New Team", `ThroughputHistory=90`, `DoneItemsCutoffDays=365`, 621 work items
(615 closed) spanning 2025-09-22 → 2026-09-21, 4 recorded CT-30 snapshot days.

### Data provenance — real Azure DevOps, not CSV or demo

Checked rather than assumed, because a demo-synthesized fixture would make the fidelity result close to
worthless:

```
connection 1: dev.azure.com/letpeoplework    WorkTrackingSystem = 0 (AzureDevOps)
  Team 1      "New Team"       -> connection 1
  Portfolio 1 "New Portfolio"  -> connection 1

SynthesizeStateJourneyForDemo : absent from every connection's options
WorkItemStateTransitions      : 1945 rows (a real journey, not a synthesized one)
```

It is the **Lighthouse project's own ADO board**: work item reference ids include 6044, 6045, 6047,
6048, 6049, 6052, 6053, 6054, 6055 and 6067 — story 6053 itself is a row in the data it was measured
against.

**But the same check exposes why the fidelity result discriminates weakly, and it is a sharper limit
than "low-cardinality values" suggests on its own.** This is a solo maintainer's board: items are
routinely created and closed on the same day (`6049` started and closed 2026-09-21; four of the eight
most recent closures are same-day). Cycle times are therefore 1 or 2 days almost always, and the
percentile distribution is nearly degenerate. The narrowness is a genuine property of this team's flow,
not an artifact of fake data — which means it cannot be fixed by finding a better dump of this
instance. **A team with cycle times spread across 1–40 days would be a materially stronger fidelity
test, and no such team exists in this database.**

**No non-ADO coverage.** Connection 2 (`letpeoplework.atlassian.net`, Jira) exists but has no team or
portfolio attached, so nothing here says anything about Jira, Linear or ServiceNow owners.

**Mocked seams** (and why they are unlikely to distort the result): `IForecastFilterRuleService`
returns no rule set and `IBlackoutPeriodService` returns no blackout days — neither is consulted on
the cycle-time percentile path, which is `GetWorkItemsClosedInDateRange` → `CycleTime(Clock.Zone)` →
`BuildPercentiles`. `IRepository<Feature>` is mocked because only Feature-WIP reads it.

---

## (1) Fidelity — **4/4 days reproduce exactly**

| Day | Recorded P50/P70/P85/P95 | Reconstructed | Match |
|---|---|---|---|
| 2026-09-05 | 1, 1, 1, 2 | 1, 1, 1, 2 | yes |
| 2026-09-19 | 1, 1, 2, 2 | 1, 1, 2, 2 | yes |
| 2026-09-20 | 1, 1, 2, 2 | 1, 1, 2, 2 | yes |
| 2026-09-21 | 1, 1, 2, 2 | 1, 1, 2, 2 | yes |

**Verdict: D6 is CONFIRMED — but on weaker evidence than the 4/4 suggests, and the confirmation is
narrower than the decision.**

Three limits on what this actually proves, stated because the headline number invites over-reading:

1. **Near-degenerate distribution.** Every percentile is 1 or 2, because this is a solo maintainer's
   board on which items are routinely opened and closed the same day (see Data provenance). A
   reconstruction that was subtly wrong but returned "about the typical value" would also score 4/4
   here. The data is real; its spread is not wide enough to discriminate.
2. **The four days are nearly identical** — three of them have the same tuple. Effectively two distinct
   observations, not four.
3. **Config drift is not exercised at all.** This is the important one. D6's named residual risk is that
   reconstruction runs against *today's* configuration and item set, so a changed state mapping, an
   edited cycle-time definition, or a deleted or re-parented item makes a reconstructed day differ from
   what would have been written then. Nothing changed on this instance in the 17 days covered, so the
   probe could not see that class of error and says nothing about it.

**What is genuinely established**: the reconstruction *mechanism* is sound — a shifted-window call on
the existing service reproduces the recorder's output byte-for-byte when the inputs have not changed.
That was the premise D1 rests on, and it holds.

**What is not established**: that reconstruction is faithful *across* a configuration change. D6's
residual risk stands unmeasured. Recommend DESIGN treat it as open rather than closed by this spike —
the cheap mitigation is a note in the docs, not a flag, since D6 rejected the flag.

---

## (2) Cost — **~15 ms per day per owner, percentiles only**

```
30 days x 4 percentile series (CT-30/60/90 + WIA), cold cache each day:
  median 14.8 ms/day   min 9.7   max 59.4
  30 days:    527 ms
  90 days:  1,581 ms   (extrapolated)
 365 days:  6,411 ms   (extrapolated)
```

PBC adds five more families per day on the team path (six on portfolio), so a full reconstruction is
roughly 3–4× these figures: on the order of **5–6 s for 90 days**, **20–25 s for 365 days**, per owner.

**Scaling caveat — do not read these as absolute budgets.** 621 work items on SQLite on a developer
machine. Cost is driven by the per-day scan of closed items, so an instance with 50k items will be
substantially slower and the ratio is not something this probe can establish.

**Implications:**

- **D3 (background) stays right**, but the justification shifts. At ~1.6 s for 90 days of percentiles
  the inline option was not the catastrophe it looked like — the real argument for background is the
  365-day PBC case (~25 s here, and much worse on a large instance), not the 90-day one.
- **D4's cap**: 90 days is the natural value. It matches the portfolio dashboard default, costs a few
  seconds on this fixture, and covers the review periods both journeys describe. **Recommended to
  DESIGN, not locked here** — D4 deliberately left the number to DESIGN, and the scaling caveat above
  means a large-instance measurement should precede locking it.

---

## (3) Absence — **1 zero-day in 365 (0.3%)**

Probing all 365 days back at CT-30, exactly **one** day would have written
`P50=P70=P85=P95=0` without the D7 gate.

That day sits at the **data floor**: 365 days back is 2025-09-22, and the earliest closed item is
2025-09-22 17:23, so the window catches nothing. D4's floor-termination and D7's absence gate turn out
to address the same day from two directions — they are complementary, not redundant, and the finding
supports keeping both.

**Verdict: D7 is confirmed as necessary but is cheap insurance on this fixture, not a major correctness
save.** The reason to keep it is the case this fixture cannot show: Team 1 closes ~1.7 items/day, so
almost every 30-day window has data. A **low-throughput team** — one that closes nothing for a month,
which is ordinary for a small team or over a holiday period — would hit the zero-write path repeatedly
and paint exactly the false floor D7 exists to prevent. The gate should be judged against that team,
not this one.

---

## Decisions affected

| Decision | Before | After |
|---|---|---|
| **D6** no reconstructed/recorded distinction | contingent on this spike | **confirmed** for the mechanism; the config-drift residual risk is **unmeasured and stays open** |
| **D4** depth cap | value left to DESIGN | **90 days recommended**, with a large-instance measurement advised before locking |
| **D3** non-blocking | assumed necessary | **still right**, but justified by the 365-day + PBC case rather than the 90-day one |
| **D7** absence gate | assumed mandatory | **confirmed necessary**; fires once here, would fire often on a low-throughput team |
| **D1** reconstruct not synthesize | premise | **premise holds** — exact reproduction on every day tested |

## Not measured (carried into the slices)

- WIA reconstruction fidelity against a recorded WIA day — slice 02 AC2.
- PBC reconstruction, and the today-anchored `BaselineValidationService` hazard — slice 03.
- Portfolio scope.
- Cost on a large instance (tens of thousands of items).
- Fidelity across a configuration change — the open half of D6.
- **Fidelity on a wide cycle-time distribution.** The only owner available closes most items the same
  day, so the sharpest version of the fidelity test could not be run. A team spread across 1-40 days
  would be a materially better probe; if one becomes available, re-run this before relying on D6 for
  anything beyond the mechanism.
- **Any non-ADO connector.** The Jira connection in this database has no owner attached, and there is
  no Linear or ServiceNow data at all. Reconstruction reads stored work items rather than the
  connector, so connector-independence is plausible by construction — but it is unmeasured.

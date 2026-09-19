# ADR-194: SLE Risk Is Reported as a Number per Item, Never as a Background Ladder on the Aging Chart

**Status**: Accepted (2026-09-19 — Morgan, DESIGN wave, interaction mode PROPOSE). Implemented by
`epic-4127-sle-risk-corrections` slice 01 (ADO User Story #6034), which deletes the ladder.

**Feature**: `epic-4127-sle-risk-corrections` — ADO Epic #4127, "Show SLE Probability for In Progress
Items", round 2

**Decider**: Morgan (Solution Architect)

**Amends**: [ADR-192](./adr-192-sle-risk-as-a-pure-conditional-over-the-cycle-time-population.md), whose
Context names chart background zones as one of four planned surfaces. That ADR's decision — the risk is
a pure conditional over the closed cycle-time population, computed once, in one place — is unchanged and
is not in question here. This ADR decides only what may *render* it.

## Context

ADR-192 established the arithmetic:

```
risk(a) = count(T > R AND T >= a) / count(T >= a)
```

over the cycle times `T` of the work a team finished in a window, where `a` is an open item's age and
`R` the team's published target. It planned four surfaces for that number. Three of them show it **per
item** — a column in the work item dialog, an at-risk line on the In Progress card, and a field written
into the user's own tracker. The fourth showed it **per age**: a ladder of full-width horizontal bands
painted behind the Work Item Aging chart's dots, at the ages where the risk first reaches 25, 50, 75 and
100 percent. The ladder shipped to `main` and was used on a dev instance. It has never been in a
release — `v26.9.9.9` is the newest tag and every commit implementing it is later — so nothing here is a
deprecation.

Using it produced three reports. One of them turned out to be two views of a third, and the remaining
two are the subject of this decision.

**The ladder's lowest band is its first threshold, so the region below it cannot be painted.**
`ZoneLevels` is `[25, 50, 75, 100]`; there is no band for "under 25". That region is left blank — and on
this same chart a blank region already means something else. The pace-percentile background, which the
chart has carried since `aging-pace-percentiles`, leaves a region unpainted when there is too little
history to place a band there, and `docs/metrics/flow-metrics.md` documents that meaning in as many
words. So the chart drew a calm answer and an unknowable one identically, and a reader had no way to
tell which one they were looking at.

**The evidence runs out from the top, so the bands that go missing are the ones worth looking at.**
`count(T >= a)` is monotonically non-increasing in `a`: the older the age, the fewer finished items ran
at least that long. `SleRiskCalculator.For` refuses to answer below `MinimumComparableItems`, and
`Zones()` walks ages upward and stops at the first refusal, so the ladder ends where the history ends.
Everything above that is blank — for the opposite reason to the blankness at the bottom, and drawn
identically to it. `docs/evolution/epic-4127-sle-risk/OUT-4127-risk-stability.md` measured the same
thinning as 25-point overnight swings in the high-age region.

**And the better a team keeps its promise, the more of its chart stays empty.** `risk(1)` is just the
team's overall breach rate. A team holding an 85%-on-time SLE breaches about 15% of the time, which is
below the ladder's first level, so nothing is painted at all until the conditional climbs past 25. A
team that misses constantly gets a chart painted from the axis up. The background is at its brightest
exactly where it is least needed, and silent on the teams using it to stay ahead.

**On a thin history it draws nothing whatsoever.** With fewer than `MinimumComparableItems` finished
items, `For(1, …)` already returns no answer, `Zones()` breaks on the first iteration, and the chart
paints an empty background in a mode the user explicitly selected, with nothing on screen saying why.

One correction to the record, made here because this is where the reasoning will be read: the DISCUSS
decision that closed this argument (D18 in `docs/feature/epic-4127-sle-risk-corrections/feature-delta.md`)
described the region below the first crossing as the one where the evidence is thinnest. It is the
opposite — that region is the best-evidenced part of the chart. The verdict does not change, but the
fault moves, and it moves to the place a repair cannot reach.

## Decision

**The SLE Risk is reported as a number attached to an item. No surface renders it as a region, a band,
a ladder or any other geometry over the age axis.**

Concretely:

1. `SleRiskCalculator.Zones`, `WithUpperEdges`, `UpperEdgeOf`, `ZoneLevels`, `CertainRisk` and the
   `SleRiskZone` record are deleted. `SleRiskCalculator.For` — the rule ADR-192 is about — is untouched.
2. `GET /api/{version}/teams/{teamId}/metrics/sleRisk/zones`, `SleRiskZoneDto`,
   `ITeamMetricsService.GetSleRiskZonesForTeam` and its cache entry are deleted.
   `GET .../metrics/sleRisk` is untouched.
3. The Work Item Aging chart's background control offers two modes — Off and Pace percentiles. The
   chart's background therefore carries one vocabulary again: a painted region is a pace band, an
   unpainted region is a state with no pace history. One meaning per colour, one meaning per gap.
4. The SLE reference line stays. With the ladder gone it is once again the only deadline the chart
   asserts.
5. `sleRiskColorFor` and everything else in `utils/charts/sleRisk.ts` stays, unchanged. The column and
   the at-risk line still colour a risk through it, including its calmest colour for a risk below 25,
   which is reachable and correct at both of those call sites. That colour was unreachable **in zone
   mode only**, because the ladder emitted no band that low — the palette was never at fault.

The constraint this ADR carries forward is point 3's premise, not point 1's deletion: **a threshold
ladder laid over an empirical conditional has no legible geometry on a real history, and the aging
chart's unpainted region is already spoken for.** A future proposal to paint risk behind those dots has
to answer both.

## Alternatives Considered

**Option A — emit a band for 0-to-25, so the calm region is painted rather than blank.**
The smallest possible repair, one entry in `ZoneLevels`, and it removes the ambiguity that produced two
of the three reports.
- **Rejected, but it is the strongest alternative and it was rejected on reach rather than on cost.** It
  repairs the bottom of the chart and leaves the top exactly as it was: the bands that vanish as the
  evidence thins still vanish, still into the same blankness, and that blankness now means "unknowable"
  in one place and nothing-in-particular in another. It does nothing at all for the thin-history case,
  where the ladder is empty from age 1 and the repair adds a band that is never reached. Two of the four
  faults survive it, including the one a coach meets on a small team.

**Option B — keep the ladder but give "unknowable" its own visual, so calm and unknown stop looking
alike.** Hatch or stipple the region above the last placeable band; the ambiguity then dissolves without
inventing a band, and the geometry becomes readable.
- **Rejected.** It is the most honest of the repairs and it costs the most. It adds a third background
  vocabulary to a chart that already asks a reader to hold per-state pace colours and a reference line,
  and — because the evidence thins fastest exactly where a coach looks — it would ship a chart whose
  upper region is permanently hatched on most real histories. A reader would learn a new visual language
  in order to be told, most of the time, that the chart cannot answer. The per-item column answers the
  same question for the same items with no new language at all.

**Option C — keep the ladder and gate it on a minimum evidence depth, so it is offered only where it can
be drawn properly.** Show the mode only for teams whose history places at least the first two bands.
- **Rejected.** The gate is the same mechanism as the defect. `Zones()` already stops at the first
  refusal from `For`, so "not enough evidence" is what makes the ladder short; gating on it makes the
  mode disappear for exactly the teams the earlier reports came from, and turns a chart that draws
  nothing into a control that is not there. It also couples a rendering decision to
  `MinimumComparableItems`, which `epic-4127-sle-risk-corrections` slice 02 deletes — so the gate would
  be rewritten by the next story in the same Epic.

**Option D — change nothing, ship the ladder, and let usage decide.**
It is never-released code with no user depending on it; the cheapest action is inaction, and a real
release would produce real feedback.
- **Rejected.** The feedback already exists: the ladder was used on a dev instance and produced three
  reports before any user saw it, and `OUT-4127-risk-stability` measured the volatility independently.
  Shipping it would spend a release note on a surface this Epic's own evidence says is unreadable, and
  would convert a clean deletion into a deprecation with a migration and a note explaining a feature
  being taken away. Deleting before release is the only moment this is free.

## Consequences

**Positive**
- The aging chart's background has one vocabulary. An unpainted region has exactly one meaning, which
  is the condition for using a background at all.
- The SLE reference line is the only deadline the chart asserts, so the chart cannot contradict itself
  about when an item is late.
- `SleRiskCalculator` shrinks to the one pure function ADR-192 decided, which is what lets slice 02 make
  `For` return a number past the target without rewriting a ladder built on its nulls.
- One fewer route, one fewer DTO, one fewer cache key, one fewer fetch on the Flow Metrics view.
- Nothing was released, so there is no deprecation, no migration, no redirect and no release note about
  a feature being withdrawn.

**Negative / accepted**
- The chart no longer shows *any* risk information; a coach who wants it opens the dialog. That is a
  real loss of at-a-glance reach and it is accepted on the grounds that the glance was not legible.
- A browser that stored `risk` as its background preference holds a value nothing reads. It resolves to
  Off through a branch that names it as retired rather than through an unrecognised-value default, so
  the next author who reaches for the word `risk` has to delete a line that says it is taken. The
  population is dev and dogfood browsers only.
- This ADR forecloses a design space rather than opening one. If a future feature genuinely needs risk
  rendered over the age axis, it supersedes this ADR — which is the intended cost, because it forces the
  four faults above to be answered rather than rediscovered.

### Deployment assumptions

Stated because a reader consulting this ADR years from now would otherwise have to guess whether they
were considered.

- **Rolling the code version back is the only recovery path, and it is sufficient.** The deletion
  produces no database change, so there is no migration to reverse and no schema state that a mixed
  fleet could disagree about.
- **A mixed-version fleet is harmless.** The removed read is a cache-backed projection over stored work;
  an older replica still serving the route recomputes it from the same rows, and a newer one simply has
  no such route. Neither direction leaves a stale or contradictory read, because nothing about the
  zones was ever persisted.
- **A downgrade restores the mode rather than corrupting anything, and that is deliberate.** The
  storage key keeps its name and its string values, and a browser holding `risk` is not rewritten when
  the new code reads it. So a rollback finds that preference intact and resumes painting the ladder —
  the state a user left, not a broken one. Rewriting the value on read would have made the rollback
  lossy, which is the second reason not to do it.
- **No cache invalidation, no warm-up and no operator action of any kind is owed at deploy time.**

## Architectural Enforcement

| Rule | Mechanism |
|---|---|
| No zone geometry exists in either stack | `grep -rnE "SleRiskZone\|sleRiskZone\|computeSleRiskZoneRects\|[Gg]etSleRiskZones\|countSleRiskZones\|showSleRisk\|SleRiskCalculator\.Zones\|\bZones_[A-Z]"` over `Lighthouse.Backend`, `Lighthouse.Frontend/src` and `Lighthouse.EndToEndTests` returns nothing. A **mandatory review gate run before the first push**, not a CI job: the last term matches a test-method naming convention, so a hit is read by a person rather than failing a build |
| The zones route is gone and its sibling is not | Acceptance test asserting 404 on `teams/{id}/metrics/sleRisk/zones` and 200 on `teams/{id}/metrics/sleRisk`, same team, same run. The removed path is a sub-path of a surviving route, so the two halves are separate claims |
| The background control offers two modes, and none when there is nothing to paint | Vitest asserting the option list for a team with per-state history, and asserting no control renders for a team without it |
| A retired stored preference paints nothing and raises nothing | Vitest seeding the literal `risk`, asserted separately from the unrecognised-value case so neither test covers for the other |
| The one legacy preference translation survives | Vitest seeding `true` and asserting the pace mode — the pre-existing test, unmodified |
| A risk is coloured identically wherever it still appears | The pre-existing `WorkItemsDialog` and In Progress suites, unmodified by the deletion's commits |
| No risk arithmetic enters TypeScript | ADR-192's rule, unchanged and still enforced by the same review gate |

## Cross-feature impact

- **ADR-192**: amended by a dated Status note correcting its four-surfaces sentence to three. Its
  Decision, its Alternatives and its *Architectural Enforcement* table are untouched — in particular the
  row `Beyond history is null, never 0, 100 or an omitted entry`, which `epic-4127-sle-risk-corrections`
  slice 02 (ADO Story #6037) reverses with its own argument.
- **ADR-020** (per-state bands chart rendering) and **ADR-188** (one pace-band ladder read by both the
  chart's geometry and the dialog's cell): unchanged, and now the only background the chart draws. ADR-188
  is the contrast worth naming — a pace band is a comparison against the *same state's* own history, so
  its geometry is per-column and its unpainted region has one cause. A risk ladder is end-to-end, so its
  geometry is full-width and its unpainted region has two.
- **ADR-065** (`workItemAgePercentiles`): unchanged. The route template this deletion follows in reverse.
- **`quiet-jira-writeback`**: unaffected. The write-back field reads the per-item number and never read a
  zone.

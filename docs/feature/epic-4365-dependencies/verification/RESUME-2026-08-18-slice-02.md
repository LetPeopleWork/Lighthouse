# Slice 02 — where it stands, and what the next session should not re-derive

Written 2026-08-18, 21:15 local, at a hard stop. Slice 01 is shipped, pushed and closed
(ADO Story #5782 `Closed`); Story #5783 is `Active` and is this slice.

## Done and committed

| commit | step | what |
| --- | --- | --- |
| `b01420602` | — | slice-02 roadmap (6 phases, 17 steps), slice-01 roadmap archived beside it, both briefs corrected |
| `6cee4e5ba` | 01-01 | the verdict vocabulary: three reasons, closed; positioned-below carried separately; inert policy input |
| `8cb498074` | 01-02 | `DependencyCycleDetector` — iterative Tarjan SCC, self-loop of one, disjoint loops, 20k-hop chain |

Backend suite after 01-02: **5727 passed, 0 failed, 1 skipped** (the skip is the reflection test that
step 01-03 un-ignores).

## Remaining, in dependency order

`01-03` (the one policy + the single `Program.cs` registration) → `02-01` route → `02-02`/`02-03` →
`03-01` payload warnings → `03-02`/`03-03`/`03-04` → `04-01` dialog → `04-02` warnings column →
`04-03` terminology guard → `05-01` architecture rule → `05-02` operator log → `06-01` measurement →
`06-02` dogfood, screenshots, gates. Then refactor, review, mutation testing, push.

## Five things already established — do not re-derive them

**1. Azure DevOps refuses to store a dependency cycle, transitively.** `TF201035`, on the closing link,
for a two-hop loop and a three-hop one alike. So the loop scenario runs on fixtures, and the detection
written in this slice is the only guard on the paths that follow: Jira's `blocks` links carry no such
guard, and the Portfolio dependency field is free text a user types. Real ADO links seeded instead:
`#5510` waits on `#5511`; `#5511` waits on `#5512` and on `#5733`, which has no child Work Items at all
and is therefore a Feature whose delivery genuinely cannot be forecast.

**2. Four warnings, three reasons.** Positioned-below is not a not-honoured reason — the order stays the
user's and the dependency would still be acted on. It rides on the verdict beside the reason, from the
same call, which is what keeps one decision site. "Nothing wrong with it" means no reason AND not
positioned below.

**3. A stateless stub does not compile in this repo.** `S2325` fires as a build error, so the
skeleton-plus-ignored-test RED the roadmap prescribes is unavailable for a service with no state. Two
consequences: `DependencyCycleDetector` takes its features in the CONSTRUCTOR (`new
DependencyCycleDetector(facts).Detect()`, one per evaluation, nothing shared between walks), and RED for
such types is taken by observed mutation of the finished implementation instead. Two of the mutants tried
against the detector cannot be compiled at all — equivalent mutants, which will move Stryker's
denominator at the slice boundary.

**4. The shipped `Depends On` count is not RBAC-filtered.** `FeaturesController.cs:241` counts against
every reference id the instance holds, and an existing test pins that. So the redaction criterion only
holds if unreadable blockers render as withheld rows — dropping them would make the count and the list
disagree with nothing on screen to explain it.

**5. The terminology guard does not reach this slice's UI strings.** The shipped ArchUnit scan covers the
backend `Dependencies` namespaces plus exactly one frontend file, `columns.tsx`. `WarningsIndicator.tsx`
and the new dialog sit outside it, so the no-"blocked" rule would ship guarded on the backend and
unguarded in the UI. Step 04-03 widens it, and widening will surface a real conflict with epic #5074's
shipped `blocked` concept in the same grid — scope it by region rather than carving out an exception.

## Owed, and small

- `DependencyRefreshReporter` has no row in the architecture's Component Decomposition table. The two
  operator log events were added at the DISTILL review gate after DESIGN closed, and the honour policy is
  now rule-enforced pure, so the aggregation cannot live there. Write the row before the reporter ships.
- ADR-158 is stale in two places, both deliberate and both recorded in the slice brief: its fourth reason
  (`NotLicensed`) moved to Epic #5792 with the split, and the policy input carries a `bool CanBeForecast`
  per Feature rather than a predicate, so the input cannot do work.
- `HasPremiumLicence` is declared on the input and must stay unread in this epic, but no shipped guard
  catches a read of it — none of the four scanned names matches. Step 05-01 must name the property, and
  must forbid the read while permitting the declaration.

## Environment

A stale second Data Protection key ring (`Lighthouse.Backend/Lighthouse.Backend/keys/`, dated 08-16,
alongside today's `data-protection-keys/`) was aborting 9 `WebApplicationFactory` integration tests with
`FATAL: Two key rings were found` before any of this work started. Moved to `~/lighthouse-stale-keyring-2026-08-16`, not
deleted. If the `:5169` dev instance turns out to have been keyed to it, restore it from there — and note
that the dogfood step in `06-02` needs that instance readable.

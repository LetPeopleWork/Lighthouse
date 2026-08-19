# Slice 02 — What exactly, and what Lighthouse cannot act on (free)

**Feature**: epic-4365-dependencies · **ADO**: Epic #4365 · **Stories**: US-02, US-03 ·
**Estimate**: ~5h
**Reference class**: the existing work-items dialog opened from a Feature row, plus
`WarningsIndicator.tsx`, which is additive by construction — it already composes two warning kinds.

## Goal

The count from slice 01 becomes actionable: click it and see which Features, in what state, and read a
warning on every dependency Lighthouse will not be able to honour.

## IN scope

- `GET /api/latest/features/{id}/dependencies` — free, read, RBAC-filtered result set.
- A dialog off the Depends On cell listing each Feature waited on: name, state, its Portfolios, and a
  link to the tracker record.
- **The honour-ability verdict per edge**, computed and exposed here even though nothing consumes it
  for forecasting yet: `outside this Portfolio` (D6), `part of a loop` (D7), `cannot be forecast` (D8).
  This is where cycle detection actually lands — one slice before the forecast needs it, which is the
  cheapest place to get it wrong.
- Three new warnings in `WarningsIndicator`: cross-Portfolio, blocker-ranked-below (read from
  `IFeatureOrdering`, never written), and loop-member. Existing warnings unchanged (AC-3.5).
- A dependency the forecast *would* honour produces **no** warning (AC-3.4) — the presence of a
  dependency is not a problem, only an unhonourable one is.
- A Feature the user cannot read renders as a redacted row with the reason, never omitted (AC-2.5).
- **The ArchUnitNET rule enforcing KPI-5 is written here, not in Epic #5792** (maintainer,
  2026-08-17, overriding the DESIGN wave's OQ-4 answer). KPI-5 claims exactly one place decides
  whether a dependency is honoured. That decision is written in this slice and merely consulted by
  #5792 — so if its enforcement ships with #5792, this epic goes to production with the invariant
  guarded by nothing but a grep, and the split existed precisely so this epic could ship alone. The
  rule asserts **at most one** implementation of `IDependencyHonourPolicy`, and that only that
  implementation may depend on `DependencyCycleDetector`. #5792 tightens `at most` to `exactly` when
  it adds the second consumer.
- **The policy is a pure lookup, and a rule says so.** ADR-158 notes that the two consumers inject
  different predicates; nothing yet forbids one of those predicates from logging, caching, or writing
  state, and a predicate with side effects makes the verdict differ between the read path and the
  forecast path — the exact failure KPI-5 exists to prevent. Extend the same ArchUnitNET rule: the
  honour policy may not depend on any service that writes state.
- **Two operator log events, sized against the noise already in the log.** A detected loop emits one
  `WARN` naming its members — rare, genuinely wrong, and the operator wants it. Unforecastable
  blockers emit **one aggregated line per refresh carrying a count**, not one line per edge. Both
  verdicts are already visible to users in the warnings column; these exist so a support conversation
  can be had from a log rather than a screenshot. Per-edge `INFO` was rejected: `ForecastService`
  already floods the log on every team update, and per-Feature-per-refresh lines would bury the
  `TeamUpdater: Update completed` summary that operators actually read.

## OUT of scope

- Adding or removing anything. Lighthouse never authors an edge (D4), so no write path exists in this
  epic at all — this slice is read-only because the whole feature is.
- Any forecast change. The honour-ability verdict is computed and displayed but drives nothing yet.
- The premium hint — there is no premium behaviour to hint at until Epic #5792 ships its first slice.

## Learning hypothesis

**Disproves** "the honour-ability verdict is a pure function of stored edges plus Portfolio membership
plus rank" **if** computing it per row on a Portfolio's Feature list needs a query per Feature, or if
loop detection over the dogfood edge set is not instant. Both the many-to-many Portfolio join and the
transitive loop walk are the kind of thing that reads as free at 94 Features and is not at 10,000.

If it fails, the verdict must be precomputed at ingestion and stored on the edge. The threshold and
the fallback design are both fixed in slice 01's brief before any storage ships — no more than 200 ms
added to the `/features` read on `:5169`, and no per-Feature query at any list size — so a failure
here is a planned branch into a design already written down, not a rewrite of shipped storage. Record
the measured number in the verdict below either way; a hypothesis that passes without a number is
not a measurement.

**Confirms**, if it holds, that Epic #5792's slices can ask "is this edge honoured?" cheaply, inside
the forecast run, without a cache.

## Acceptance criteria

AC-2.1 … AC-2.5 and AC-3.1 … AC-3.6 verbatim from `feature-delta.md`. The three that carry the slice:

- A cross-Portfolio dependency warns and names the Feature it points at (AC-3.1).
- A dependency loop warns on every member and names the others (AC-3.3).
- A healthy dependency produces no warning at all (AC-3.4).

## Dependencies

Slice 01's stored edges and `dependsOnCount`. `IFeatureOrdering` for the ranked-below comparison —
read only. The loop and throughput-less-blocker shapes are created as **real Predecessor links in the
dogfood ADO project**; if they are not in place when this slice runs, its loop AC falls back to
fixtures and the real-data confirmation moves to Epic #5792 slice 01's dogfood moment. Creating them
first is cheap and strictly better — say which happened in the verdict rather than leaving it implied.

## Dogfood moment

Same day: open `/features` on `:5169` and screenshot the dialog and each warning kind against real ADO
data. Cross-Portfolio should appear naturally; the loop appears only if the deliberate ADO links were
created first.

## Commit gate

Normal — the approval gate is Epic #5792's only (maintainer, 2026-08-16).

## Learning hypothesis verdict

**HOLDS, on the half that settles the question. Measured 2026-08-19, step 06-01.**

The verdict is a pure function of what the read already loaded, and the read pays for it once rather
than per Feature. The Features view issues **11 database commands over 20 Features and 11 over 200** —
identical, asserted rather than eyeballed
(`Reading_the_features_view_costs_the_same_however_many_features_there_are`). A count that grows with
the list is the failure OQ-6 exists to catch, and it is the half that generalises: a wall-clock reading
on one machine says nothing about a Portfolio ten times larger, while a flat command count does.

Wall clock alongside it, same run, in-process against SQLite: 257 ms for the 20-Feature read (first
read of the run, so it carries host warm-up) and **67 ms for the 200-Feature read** returning 249 KB of
payload. Nothing is stored as a result: the whole stored edge set is read before and after and compared
(`Working_out_the_loop_stores_nothing`).

**Owed, and deliberately not faked: the ≤200 ms delta on the `:5169` restored backup has NOT been
measured.** That instance was not running during this session, and starting a dev run against it
creates a second Data Protection key ring that poisons the backend suite and locks `Lighthouse.dll` —
with mutation testing still to come, that trade was not worth taking. It belongs to the dogfood step
(06-02), which needs that instance anyway. The structural half above is what the design actually leans
on; the wall-clock figure is corroboration, and its absence is recorded here rather than implied.

**No branch to ingestion is taken.** The fallback — a precomputed verdict stored on the edge — stays
unbuilt and unneeded.

## Dogfood, 2026-08-19 — what ran on real data and what ran on fixtures

**Ran on real Azure DevOps data.** The walking skeleton (`FeatureDependencies.spec.ts`) drives a real
Portfolio over the real `letpeoplework` board — Epics `#4365`, `#5698`, `#5792`, linked by Predecessor
links a person drew — and now opens the dialog as well as reading the count. It passed locally against a
running Lighthouse: `#5792` shows **2**, `#4365` shows **empty** (the direction guard still holds), and
the dialog lists both Features by name with a link into the tracker. Both entries carried
*cannot be forecast*, correctly: that instance has no Team throughput behind those Epics.

**Ran on fixtures, and this is settled rather than a shortcut.** The loop (AC-3.3) cannot be dogfooded on
Azure DevOps at any hop count — `TF201035`, transitively — so it runs on fixtures here, and its real-data
confirmation belongs to slice 03, where Jira's `blocks` links carry no such guard. Cross-Portfolio
(AC-3.1) also ran on fixtures: the acceptance harness seeds two Portfolios and refreshes them separately,
which is the shape the verdict actually turns on.

**Confirmed on `:5169` against the real board, 2026-08-19.** A refresh of the `Lighthouse` Portfolio
picked the new Predecessor links up: `#5510 Sizing Poker` reads **1**, `#5511 Task Manager` reads **2**,
and `#5512` and `#5733` read **empty** — the direction guard still holds as the edge set grows. Three
Features carry a warning, all of them the ordering one: `#5792`, `#5511` and `#1812` each wait on
something placed below them.

**The prerequisite table was wrong about `#5733`, and this is worth not re-deriving.** It was created as
"a Feature whose delivery genuinely cannot be forecast" because it has no child Work Items at all. It
does not produce the cannot-be-forecast warning, and should not: a Feature with no children is given the
Portfolio's **default Feature size**, so `#5733` carries 9 remaining items against a Team that does have
measured delivery, and it forecasts perfectly well (`isUsingDefaultFeatureSize: true`, four forecasts,
`teamsWithoutForecast: []`). Nothing is broken here — `Feature.CanBeForecast` is being read exactly as
it is written. What that warning actually needs is a Feature whose **Team** has no measured delivery, or
one whose forecast ran with zero trials. So AC-2.3's verdict half runs on fixtures for now too, and a
real-data example has to be arranged deliberately rather than found.

**Owed, not done** — needs a person at the running instance:

- The screenshots of the dialog and of each warning kind, and the ≤200 ms figure recorded above.
- A real Feature behind a Team with no measured delivery, if the cannot-be-forecast warning is to be
  confirmed on real data rather than on fixtures.

**No `lighthouse-clients` version-gate entry is owed.** `FEATURE_REQUIRES_SERVER_NEWER_THAN` guards
routes a client calls, before the call; the new `/features/{id}/dependencies` route earns an entry only
once a client chooses to expose it, and none does. The additive payload fields owe nothing at all — a
Feature payload decodes as an untyped array on the client.

## Prerequisite state, measured 2026-08-18 (before this slice ran)

**Azure DevOps refuses to store a dependency cycle, and the guard is transitive.** Adding a Predecessor
link that would close a loop fails with `TF201035: … would result in a circular relationship` — both for
a two-item loop (`#4365` waits on `#5792`, which already waits on `#4365`) and for a three-hop one
(`#5510 → #5511 → #5512 → #5510`, rejected on the closing link). So AC-3.3's loop **cannot** be
dogfooded on ADO at any hop count, and this slice's loop scenario runs on fixtures.

That does not make cycle detection speculative. Three real sources remain: Jira's `blocks` links carry
no such guard (slice 03), the per-Portfolio dependency field is free text a user can point back at the
Feature itself (slice 04, which is why the dedup key keeps a self-reference), and a reference that
resolves across two Portfolios can close a loop that neither tracker sees whole. Detection ships here as
planned; only its dogfood evidence moves to slice 03.

Real ADO links created for this slice instead:

| Feature | waits on | why it is here |
| --- | --- | --- |
| `#5510 Sizing Poker` | `#5511 Task Manager` | a two-hop chain, so the dialog shows a blocker that is itself waiting |
| `#5511 Task Manager` | `#5512 Gitlab Integration`, `#5733 Opt-In Telemtry` | two blockers on one Feature, and `#5733` has no child Work Items at all — a genuinely unforecastable blocker on real data — the verdict half of AC-2.3, not AC-3.6, which is the terminology criterion |

`#5512` and `#5733` carry only the mirrored Successor link and must still read empty, which keeps
slice 01's direction guard under test as the edge set grows.

Cross-Portfolio (AC-3.1) needs a blocker outside the Feature's own Portfolio; the `:5169` instance has
one Portfolio today, so confirm during the dogfood step whether a second Portfolio is worth creating
there or whether AC-3.1 also runs on fixtures. Say which in the verdict.

## Contract deviations found while cutting the types (2026-08-18, step 01-01)

Three places where the code that shipped departs from what an upstream document says, each deliberate:

- **The reason set is closed at three values, not ADR-158's four.** `NotLicensed` is absent because the
  licence half moved to Epic #5792 with the split, and nothing in this epic may ask a licence question.
  The ADR text is stale on that point; #5792 adds the fourth value when it turns the flag on.
- **The policy's input carries a `bool CanBeForecast` per Feature, not ADR-158's "predicate naming which
  Features can be simulated".** Same information, materialised by the caller. It keeps the input a
  genuinely inert projection — a `Func<>` in the contract would have been the one thing in the input able
  to do work — and leaves the two consumers' one legitimate difference (last completed run versus live
  run coverage) on their side of the call.
- **`HasPremiumLicence` is declared but must never be read here, and nothing yet stops that.** The
  shipped guard scans for `CanUsePremiumFeatures`, `LicenseGuard`, `ILicenseService` and
  `useLicenseRestrictions` — none of which is the new property's name. The architecture rule must name it,
  and must forbid the *read* while permitting the declaration, since the declaration site is the one place
  it is legitimately mentioned.

Also owed: `DependencyRefreshReporter` has no row in the Component Decomposition table. The two operator
log events were added at the DISTILL review gate after DESIGN closed, and the honour policy is now
rule-enforced pure, so the aggregation cannot live there. Write the row before the reporter ships.

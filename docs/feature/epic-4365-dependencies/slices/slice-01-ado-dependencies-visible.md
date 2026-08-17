# Slice 01 — A Feature says what it is waiting on (ADO, read-only, free)

**Feature**: epic-4365-dependencies · **ADO**: Epic #4365 · **Stories**: US-01 · **Estimate**: ~6h
**Reference class**: epic-5375 slice 01 — the same grid, the same column factory, the same additive
`FeatureDto` field, one slice earlier in its own epic.

## Goal

A product owner opens the Features view and sees, for the first time, that this Feature is waiting on
two others — without opening Azure DevOps.

## IN scope

- A persisted **list of references** on the Feature — strings in `ReferenceId` space, resolved when
  read, not a resolved foreign key (D5). Its own collection, **never** a synced scalar. Additive
  migration generated with the `CreateMigration` script, all providers, expand-only.
- ADO ingestion: read `System.LinkTypes.Dependency-Reverse` relations during the portfolio Feature
  fetch and take the **trailing segment of the relation URL**, because ADO's `ReferenceId` is
  `$"{workItem.Id}"` (`:870`). `WorkItemExpand.Relations` is already requested on the parent path
  (`AzureDevOpsWorkTrackingConnector.cs:1043`) and `WorkItemExtensions.cs:25-27` already walks
  `workItem.Relations` — extend that, do not add a second fetch shape.
- Reconcile on every sync: stored edges for a Feature are replaced wholesale by what the tracker now
  says, so a link removed in ADO disappears here (AC-1.5). Because Lighthouse never authors an edge
  (D4), there is nothing to preserve and the reconcile stays a replace for the whole epic.
- **The relations early return must test BOTH overrides.** `GetParentReferenceForWorkItems`
  (`:1012-1018`) returns early when the *parent* override is set, and it is the only place
  `WorkItemExpand.Relations` is requested — the fetch this slice rides. Left as-is, a Portfolio with a
  parent override reports zero dependencies forever, silently. Skip only when both overrides are set.
  Asserted by AC-1.9, at both call sites.

  **Settled (maintainer, 2026-08-17): the caller passes an explicit flag.** The method takes the base
  owner type, the dependency override lives on `Portfolio` alone, and the method is called for a Team
  (`:87`) as well as a Portfolio (`:609`). So the parameter list grows one `bool`, computed where the
  answer is actually known:

  ```csharp
  private async Task<Dictionary<string, string>> GetParentReferenceForWorkItems(
      IEnumerable<AdoWorkItem> adoWorkItems,
      WorkTrackingSystemOptionsOwner workTrackingSystemOptionOwner,
      bool dependenciesComeFromRelations)
  {
      // Relations carry both the parent link and the dependency links. A parent override on its own
      // is no longer reason enough to skip the fetch, or every Portfolio using one would silently
      // report zero dependencies.
      if (workTrackingSystemOptionOwner.ParentOverrideAdditionalFieldDefinitionId.HasValue
          && !dependenciesComeFromRelations)
      {
          return ...;
      }
  ```

  Team path (`:87`) passes `false` — no Feature dependencies are read there, so its behaviour stays
  byte-identical to today, which is the second half of AC-1.9. Portfolio path (`:609`) passes
  `portfolio.DependencyOverrideAdditionalFieldDefinitionId is null`.

  Two alternatives were weighed and rejected: splitting the method per owner type duplicates the
  relation-fetch body, and downcasting to `Portfolio` inside a base-typed method reproduces the very
  shape that caused this trap — one shared method quietly behaving differently per subtype.

- **`Feature.DependsOnReferences` is immutable from outside the reconciler.** Exposed as
  `IReadOnlyCollection<FeatureDependencyReference>`, the reference type owned by EF with no public
  setters, plus an ArchUnitNET rule forbidding mutation anywhere but `DependencyReconciler`. Both,
  not either: the type stops the accident, the rule stops someone widening it back later. Without
  them the reconcile-wholesale contract is a guarantee nothing enforces.

- **Reconcile dedupes on `(FeatureId, ReferenceId)`.** A tracker can return the same link twice, and
  the same target reached by two link kinds is still one dependency. The key matters in one specific
  way: a self-reference — a Feature listing itself — must **survive** dedup, because slice 02's loop
  warning has to be able to name it. Anything coarser drops it and a one-Feature loop becomes
  invisible.
- Resolution happens at read: a reference naming a Feature Lighthouse does not (yet) hold contributes
  nothing to the count and no error (AC-1.4). The relation URL carries an id and not a type, so a
  Predecessor pointing at a Bug or a Task is exactly this case rather than an exception — and a
  reference to a Feature that simply has not been imported yet heals on the next read, which is the
  whole reason D5 stores strings rather than foreign keys. A relation URL that is malformed and
  cannot be parsed at all is the same case: skipped, no error, no partial row — identical to an
  unresolvable reference, so a broken payload can never half-write an edge.
- Additive `dependsOnCount` on `FeatureDto`, alongside `Position` / `CanMove` / `BlockingPortfolios`.
- One new column factory in `FeatureListDataGrid/columns.tsx`, used by **both** `FeaturesView.tsx` and
  the Portfolio detail list — written once (AC-1.2).
- Header text through `getTerm`. The word is "Depends On", never "blocked" (D10).

## OUT of scope

- Anything that says *which* Features it waits on. That is slice 02, and a count alone is a coherent
  product: it tells the user a dependency exists, which today nothing does.
- Every warning. Cross-Portfolio, ordered-below and loop warnings are slice 02.
- Any forecast change whatsoever. The forecast half is Epic #5792 (Dependency-Aware Forecasting), a
  separate premium epic that starts once this one has shipped.
- The per-Portfolio dependency-field override (slice 04). This slice reads the standard ADO link only.
- Jira, Linear, ServiceNow, CSV (D13).
- Cycle detection — nothing consumes the edges yet, so a cycle is inert here.

## Learning hypothesis

**Disproves** "reading ADO relations is affordable inside the sync" **if** the full portfolio refresh
on the dev instance exceeds 110% of its pre-slice baseline. The relation payload gives an id and no
title, so naming the target may cost a follow-up request per relation — the classic N+1. Epic #5687
took this exact path from 468,856 ms to 2,087 ms, and this is the shape that gives it back.

If it fails, the ingestion must move off the synchronous refresh path — a separate pass, or a second
phase keyed like `GetFeaturesForProject(portfolio, referenceIds)` — and slices 02-04, plus every
slice of Epic #5792, inherit a different ingestion shape. Cheaper to discover in slice 01 than in
slice 03.

**Confirms**, if it holds, that every later slice can assume dependency data is present and current
after a normal refresh, with no separate trigger.

## OQ-6 — the threshold and the fallback, decided before this slice stores anything

OQ-6 asks what the honour-ability verdict costs to compute on the read path at instance scale. The
measurement belongs to slice 02, where the policy first exists. The **decision** belongs here, because
if the measurement fails the verdict has to move to ingestion — and that changes the storage shape
this slice is about to commit. Discovering that after slice 01 has shipped means migrating stored data
rather than choosing a design.

- **Threshold**: computing the verdict for every row of a Portfolio's Feature list adds no more than
  200 ms to the `/features` read on the `:5169` restored backup, and issues no query per Feature —
  the Portfolio-membership join and the loop walk each run once per request, not once per row. A
  per-row query is a failure even if the wall-clock number looks acceptable at 94 Features, because
  it is the shape that stops being acceptable at 10,000.
- **Fallback if it fails**: the verdict is precomputed during reconcile and stored on the edge. That
  makes `FeatureDependencyReference` carry verdict fields, makes the reconcile the only writer of
  them, and makes a Portfolio-membership change a reason to recompute. It stays one decision in one
  place, so KPI-5 survives the change — what moves is *when* the decision runs, not how many there
  are.
- **What this slice must therefore not do**: assume the reference row is final. Keep
  `FeatureDependencyReference` free of read-model fields so adding verdict columns later is an
  additive, expand-only migration rather than a reshape.

## Verify the premise first (30 min, before the migration)

On `:5169`, dump the ADO relation payload for one Portfolio's Features and count how many are
`Dependency-Reverse`, how many point at Features Lighthouse stores, and whether the target title is
reachable without a second call. If nothing in the dogfood data has a Predecessor link, the slice
still ships, but its dogfood moment needs a link created in ADO first — find that out now.

## Acceptance criteria

AC-1.1 … AC-1.8 verbatim from `feature-delta.md`. The three that carry the slice:

- Two ADO Predecessor links to Features in the same Portfolio render `2` (AC-1.1).
- A link removed in ADO drops the count on the next refresh — reconciled, not accumulated (AC-1.5).
- Full refresh within 110% of the pre-slice baseline (AC-1.8, KPI-3).

AC-1.10 runs here and at every later slice. It is a backend NUnit test, not an E2E: build a fixture
portfolio, run the forecast twice under a pinned seed — once with dependency edges present, once with
the same fixture stripped of them — and assert the percentile arrays are equal exactly, not within a
tolerance. Approximate equality would let a real drift hide under rounding. The seed is a constant in
the test, so the run is reproducible on any machine and in CI without touching `:5169`.

## Dependencies

Epic #5375's `/features` view and shared `FeatureListDataGrid` — both in `main`. `:5169` restored from
a real backup. `CreateMigration`.

**The KPI-3 baseline is a number, not an intention.** Capture it before the first commit of this slice
and write it into this brief in this shape, so KPI-3 stays checkable months later without
reconstructing anything from git history:

> Pre-slice-01 baseline (YYYY-MM-DD): full ADO portfolio refresh on `:5169`, N Features, M Teams,
> T seconds, measured from the `TeamUpdater: Update completed` summary line.

Every later slice records its own refresh timing against that line. A baseline that exists only as a
promise makes "≤110% of baseline" unfalsifiable, which is the same as having no KPI at all.

## Dogfood moment

Same day: refresh `:5169`, open `/features`, screenshot the column against real ADO data. Record the
refresh timing next to the baseline in this brief.

## Commit gate

Normal. The maintainer's approval gate applies to Epic #5792 only (maintainer, 2026-08-16) — this
epic touches no forecasting code and AC-1.10 asserts it. Commit per focused step; push once CI is
green.

## Learning hypothesis verdict

_Not yet run._

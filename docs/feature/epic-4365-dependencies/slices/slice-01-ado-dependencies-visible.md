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

## Verify the premise first — done 2026-08-18, before the migration

The relation payload for all 82 Features of the ADO Portfolio was dumped and counted, then dumped
again after the maintainer added two more links on 2026-08-18. **11 dependency relations exist over 7
Features** — 5 Predecessor and 6 Successor. What the column must read:

| Feature | Carries | Depends On reads |
|---|---|---|
| #1812 Flexible Feature-to-Milestone Assignment | Predecessor → #3533, #3534; 3 Successors | **1** |
| #3532 Modern Data Grid Implementation | Predecessor → #1812 | **1** |
| #5565 Sync Delivery Dates with the Work Tracking System | Predecessor → #5698 | **1** |
| #5792 Dependency-Aware Forecasting | Predecessor → #4365 | **1** |
| #3533, #4365, #5698 | Successor only | **empty** |
| the other 75 | nothing | **empty** |

Three things this data buys:

- **An unresolvable target, free.** #3534 is not a Feature this Portfolio holds, so #1812 must render
  1 and not 2, and must not error — AC-1.4 against real data rather than a fixture.
- **A direction guard.** #4365 and #5698 carry a dependency relation and must still read empty,
  because this slice reads only `Dependency-Reverse` (D14). A crafter who walks every relation whose
  name starts with `Dependency` turns those two into `1` and silently doubles counts across the
  instance. The pre-2026-08-18 data could not catch this: #1812 held both directions at once, so a
  both-directions bug still produced a plausible number there.
- **A screenshot that explains itself.** #5792 waits on #4365 — the epic split, rendered as the thing
  the epic ships.

**The relation carries an id and no title.** The payload is `{rel, url, attributes.name}` and the
target id is the trailing URL segment — exactly what D5 assumed. Naming a target is a local lookup
against Features already held, so nothing here costs a second request, and the N+1 the learning
hypothesis was written against does not arise for a count.

Four rows carry a number and 78 are empty. Enough to prove the loop closes, and not enough for the
loop and no-throughput shapes scenarios #19/#21 want, which still need their own links before slice 02.

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

> Pre-slice-01 baseline (2026-08-18): full ADO portfolio refresh on `:5169`, 82 Features, 1 Team
> (396 Work Items), **9.82 s** for the Portfolio and **21.86 s** for the Team, measured from the
> `Update completed` summary lines with `mode=Full`.

Two consecutive runs, `DeltaSync` turned off so the number is reproducible rather than a function of
what happened to have changed since the last cycle:

| Run | `PortfolioUpdater` 'Lighthouse' | `TeamUpdater` 'Lighthouse Dev Team' |
|-----|--------------------------------|-------------------------------------|
| 1 | 9868 ms (scanned 82, fetched 82) | 22025 ms (scanned 396, fetched 396) |
| 2 | 9771 ms (scanned 82, fetched 82) | 21684 ms (scanned 396, fetched 396) |
| mean | **9820 ms** | **21855 ms** |

So AC-1.8 passes while the portfolio refresh stays at or under **10802 ms**. The delta path, which is
what the instance actually runs day to day, measured 7257 ms (fetched 27) for the Portfolio and
1520-3508 ms (fetched 16-25) for the Team on the same afternoon — recorded for context, not as the
gate, because its number moves with whatever the tracker happened to change.

Every later slice records its own refresh timing against that line. A baseline that exists only as a
promise makes "≤110% of baseline" unfalsifiable, which is the same as having no KPI at all.

## Dogfood moment — done 2026-08-18, and KPI-3 passes

`:5169` was rebuilt from this slice's code (frontend into `wwwroot`, current backend), `DeltaSync`
turned off, and the ADO Portfolio refreshed three times.

| Run | `PortfolioUpdater` 'Lighthouse', `mode=Full` | vs the 9820 ms baseline |
|---|---|---|
| 1 — first refresh after process start | 12409 ms | 126% |
| 2 — warm | 9204 ms | 94% |
| 3 — warm | 9337 ms | 95% |
| **warm mean** | **9270 ms** | **94%** |

**AC-1.8 passes: 9270 ms against a 10802 ms ceiling.** Reading dependencies costs the refresh nothing
measurable, which is what the zero-extra-request structure predicted — the count comes out of a
response the refresh already fetched, so there was never a request for it to add.

**The honest caveat about run 1.** The baseline was measured on a warm process, so warm-to-warm is the
comparison that means anything. There is no cold-start baseline to compare 12409 ms against, so that
number is *not* evidence of a regression — it is evidence that a cold start costs roughly three
seconds of JIT and connection setup, which was equally true before this slice. Recorded rather than
dropped, because dropping the inconvenient run is how a budget stops being falsifiable.

### What the column shows on real data

Read from the live `/features` payload after the refresh, and confirmed in a browser:

| Feature | Reads | Why |
|---|---|---|
| #5792 Dependency-Aware Forecasting | **2** | waits on #4365 and #5698, both held |
| #5565 Sync Delivery Dates | 1 | waits on #5698 |
| #1812 Flexible Feature-to-Milestone Assignment | **1** | waits on #3533 (held) and #3534 (**not** held) |
| #3532 Modern Data Grid Implementation | 1 | waits on #1812 |
| #4365, #5698, #3533 | empty | they carry Successor links only |
| the other 90 | empty | no dependency links |

Two of those rows are the ones worth having:

- **#1812 is AC-1.4 confirmed against real data rather than a fixture.** It has two Predecessors and
  renders 1, because #3534 is not a Feature this Portfolio holds. The unresolvable reference is passed
  over silently and the good one beside it survives, which is the whole point of resolving at read.
- **#4365, #5698 and #3533 are the direction guard.** Each carries a dependency relation and must still
  read empty, because only `Dependency-Reverse` is read. A crafter walking every relation whose name
  starts with `Dependency` would turn these into 1 and silently inflate counts across the instance.

The browser screenshot was taken the same day against this data, with the "Dependency-Aware
Forecasting" cell reading `2` off the rendered page.

## Commit gate

Normal. The maintainer's approval gate applies to Epic #5792 only (maintainer, 2026-08-16) — this
epic touches no forecasting code and AC-1.10 asserts it. Commit per focused step; push once CI is
green.

## Learning hypothesis verdict

**CONFIRMED, 2026-08-18, at the close of phase 02** — reading ADO relations is affordable inside the
sync, and every later slice may assume dependency data is present and current after a normal refresh
with no separate trigger.

The guarantee holds **by construction rather than by tuning**, which is the stronger outcome.
`GetParentReferencesFromRelationFields` makes one `GetWorkItemsInChunks(…, WorkItemExpand.Relations, …)`
call and reads the parent references and the dependency references out of that single response; the
mapping then resolves each reference against Features already in hand. There is no code path on which
a dependency causes a request, so there is nothing to regress into an N+1 later.

Pinned by `GetFeaturesForProject_ReadingTheDependenciesCostsTheRefreshNoRequestOfItsOwn`, which asserts
three things rather than one:

1. the Feature really came back carrying its dependency — without this the two cost assertions are
   satisfied trivially by a refresh that read nothing;
2. exactly **one** payload read requested `WorkItemExpand.Relations` — the no-second-fetch half;
3. no payload read names the blocker's id — the no-follow-up-per-target half.

The third assertion is not redundant. A follow-up read to resolve a target's title would be a `Links`
read, not a `Relations` one, and would sail straight past a relations-only count. The N+1 this slice
was written to fear is exactly that shape, so it is named directly.

Proven falsifiable: the connector was temporarily perturbed to fetch relations twice and then to fetch
each target, and both cost assertions went red (`Expected: 1 / But was: 2`, and
`Expected: no item equal to 1801 / But was: < 42, 42, 42, 1801 >`) while the first assertion stayed
green — the perturbed code still produced the dependency, it just paid for it. The perturbation was
reverted and is not committed.

So the escalation path the hypothesis named — moving ingestion off the synchronous refresh onto a
separate pass or a second phase keyed like `GetFeaturesForProject(portfolio, referenceIds)` — is **not
needed**, and slices 02 through 04 keep the ingestion shape they were designed against.

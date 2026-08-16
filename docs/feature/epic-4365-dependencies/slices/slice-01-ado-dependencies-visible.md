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
  Note the method takes the base owner type while the dependency override lives on `Portfolio`, and it
  is called for a Team (`:87`) as well as a Portfolio (`:609`) — the Team path keeps today's behaviour
  unchanged. Asserted by AC-1.9.
- Resolution happens at read: a reference naming a Feature Lighthouse does not (yet) hold contributes
  nothing to the count and no error (AC-1.4). The relation URL carries an id and not a type, so a
  Predecessor pointing at a Bug or a Task is exactly this case rather than an exception — and a
  reference to a Feature that simply has not been imported yet heals on the next read, which is the
  whole reason D5 stores strings rather than foreign keys.
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

## Dependencies

Epic #5375's `/features` view and shared `FeatureListDataGrid` — both in `main`. `:5169` restored from
a real backup. The pre-slice timing baseline, captured **before** the first commit. `CreateMigration`.

## Dogfood moment

Same day: refresh `:5169`, open `/features`, screenshot the column against real ADO data. Record the
refresh timing next to the baseline in this brief.

## Commit gate

Normal. The maintainer's approval gate applies to Epic #5792 only (maintainer, 2026-08-16) — this
epic touches no forecasting code and AC-1.10 asserts it. Commit per focused step; push once CI is
green.

## Learning hypothesis verdict

_Not yet run._

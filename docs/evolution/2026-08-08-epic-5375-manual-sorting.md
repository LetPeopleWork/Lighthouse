# Who decides the order your Features are forecast in — Epic 5375

Shipped 2026-08-06 → 2026-08-08 in three slices, plus a fourth that was designed and then dropped on
purpose. Reported by Lorenzo. Premium.

The epic reads like a display feature and is not one. `ForecastService.GetSimulationResultsOfFeatureToUpdate`
draws each simulated day's throughput from the first `FeatureWIP` remaining Features **in `Order`
sequence**, so the order is an input to every forecasted date. Before this epic, that input belonged
entirely to whatever the work tracking system happened to rank — and on ServiceNow it was the record
*number*, which is not a rank at all.

## What shipped

| Slice | ADO | What it added |
| --- | --- | --- |
| 01 | 5688 | A `/features` view — third top-level nav entry — listing every Feature across all Portfolios with a `#` position column. Free, read-only. |
| 02 | 5689 | `ManualRank` plus an instance-wide switch that hands ordering ownership to Lighthouse. Premium. |
| 03 | 5690 | Move to Top / Up / Down / Bottom on any Feature you own, on all three Feature lists. Premium. |
| 04 | 5691 | "Move above Feature X" — **Removed, not deferred.** |

ADRs **132-136**. Journey `docs/product/journeys/epic-5375-manual-sorting.yaml`, five new jobs in
`jobs.yaml`.

## The load-bearing decision: the order has no aggregate root

`ManualRank` is a scalar on `Feature`; the sequence is derived at read time; Settings owns the ordering
*policy*, not the data. That was only available because contiguity was relaxed from an invariant to a
post-condition — the contract is a total order (`ManualRank` ASC, **nulls last**, tie-break on `Id`), so
gaps, duplicates and nulls are all legal (ADR-132).

Concurrency then falls out of the **command shape** rather than a lock: the move is expressed in
identities (`before`/`after` a Feature id), never in positions, so it needs no optimistic-concurrency
token. `PATCH /features/{id}/rank` carries exactly one of `beforeFeatureId` / `afterFeatureId`, and
`beforeFeatureId: null` means the bottom.

That command shape is also why slice 04 is cheap to bring back: "move above Feature X" **is** the
endpoint that already shipped, with X named. Only the picker UI and the Done exclusion remain.

## Three things worth not re-deriving

**The rank service renumbers the whole sequence 1..N, not the moved block.** Over a ragged set a partial
renumber is unsound — a prefix row can legitimately hold rank 900 — and whole-sequence renumbering is
what makes Move to Bottom actually mean the bottom. Places are written with `ExecuteUpdateAsync` per
changed row and **no `Feature` is ever loaded**: loading them re-inserts the `PortfolioTeam` join rows,
the same shape as `5f055dc30`, which surfaced as `SQLite Error 19` the moment a fixture gave the
Portfolio a Team.

**A shared Feature you fully own moves silently.** No warning, though it re-sequences the other
Portfolio's delivery. Reviewed and accepted as-is. The refused case explains itself in a full sentence;
the permitted case says nothing. Deliberate.

**The dogfood instance cannot produce a shared Feature, and not by accident.** Its three Portfolios sit
on three different connections over disjoint `ReferenceId` namespaces, and Features are matched by
`ReferenceId` alone (`WorkItemService.cs:518`) with no connection scope. 97 Features, 93 join rows,
4 orphans, **zero** in two Portfolios. So D11 ships proven by integration test and demo scenario 15
alone. To dogfood it at all, load scenario 15 first.

## Two DISCUSS premises that were simply wrong

1. "`GET /features` is the first endpoint whose result set is RBAC-filtered" — **false**.
   `FeaturesController.GetFeaturesByPredicate` already filtered at `:100`, and that filter **admits
   orphans**, so orphan Features were already visible before this epic.
2. "`OptionalFeature` is framed as preview capability" — **false**; `IsPremium` and `IsPreview` are
   separate flags. `AppSetting` still won, on better grounds: `OptionalFeature`'s premium path is a
   silent no-op where AC-2.5 needs a 403 (ADR-134).

## Two traps the DISCUSS text would have shipped

- `feature.Portfolios.All(canWrite)` is **vacuously true for a Feature in no Portfolio**, and the premise
  check found four orphans. The rule has to be `Portfolios.Any() && Portfolios.All(canWrite)` (ADR-136).
  The frontend equivalent `projects.every(...)` fails open **twice** — `projects` is already
  read-filtered *and* `every` is vacuous on the empty array.
- `Position` is a **global** ordinal over a **filtered** result set, so it must be computed before
  filtering, over a narrow whole-table projection. A window function is structurally unavailable:
  `FeatureComparer`'s int → double-inverted → string ladder has no `ORDER BY` equivalent (ADR-135).
  Costs one extra round trip per request.

## What the mutation runs found

| Slice | Backend | Frontend |
| --- | --- | --- |
| 01 | 80.39 % | 80.00 % |
| 02 | 85.42 % | 84.78 % |
| 03 | 82.76 % | 85.55 % |

Every stack failed its first run and was fixed by writing tests, never by narrowing scope. Two findings
generalise:

- **Slice 03's frontend opened at 65.90 % for a structural reason: nothing rendered the menu through the
  grid.** `FeatureMoveMenu.test.tsx` renders the component directly, and `FeatureListDataGrid.test.tsx`
  mocked the gate as `policy-off`, which makes the menu render `null` — so the column factory, the
  neighbour resolution and the move call were exercised by nothing at all. That is exactly the wiring
  D10's "one change, both surfaces" rests on.
- **A Stryker.NET `test-case-filter` must name every changed file's tests**, not just the headline ones.
  All 13 `ManualRankComparer` mutants reported `NoCoverage` *after* its test existed, purely because the
  filter did not match the test class. A filtered-out test looks identical to a missing one.

## The reading that will keep coming back

**"I moved it and nothing happened" is `FeatureWIP`, not the sort.** The Lighthouse dev instance runs
`FeatureWIP = 3`, so the top three are worked in parallel and a small Feature finishes first wherever it
sits inside that window — three Features can even share identical percentile dates. The order only bites
at the window boundary. This is now written down on the Features page, because it reads as a bug to
everyone who meets it first.

## Slice 04 was dropped on a judgement call, not on evidence

K7 — the run-based reading rule that would have justified the picker — was **never read**. Slice 03
shipped and was verified the same day, so there is no fortnight of Move-to-Top-versus-Move-Up counts.
The call was "keep it simple for now", which means the question is still open rather than answered.
Rationale is on ADO 5691 and in the workspace's `feature-delta.md`.

## Finalization

Docs were deliberately written once, at the end, rather than per slice: slice 01's navigation reorder
staled every full-page screenshot, and regenerating three times for one epic is waste. That deferral was
tracked and paid on 2026-08-08.

Paying it turned up two things worth keeping:

- **`docs/portfolios/detail.md` stated outright that you cannot reorder Features in Lighthouse.** True
  when written, false since slice 03. Prose that describes an absent capability does not announce itself
  when the capability arrives.
- **Two committed screenshots had no test able to regenerate them** — `features/overview.png`, referenced
  from both the Teams and Portfolios pages, and the Features page had no image at all. Both now have
  `@screenshot` tests.

The screenshot run itself cost a wasted 20 minutes and is recorded separately: cleaning the database
wipes the premium licence, and six `Screenshots.spec.ts` tests drive premium-gated controls. Because the
regeneration procedure deletes the PNGs first — the 0.5 % pixel guard otherwise preserves the stale
image — a failing test leaves its screenshot *deleted*, not stale.

## Gates

- CI green on `main` @ `5fdb44a16` (run 31205374311), every job.
- Backend and frontend builds at zero warnings; full suites green.
- `FeatureOrderingSingleSourceArchUnitTest` keeps the ordering selection point single — verified by
  introducing the violation and watching it go red.
- ADO 5688 / 5689 / 5690 Closed, 5691 Removed.

## Still open

- **Slice 04** — recoverable at the cost of a picker; the endpoint is already the right shape.
- **The four `@auth` screenshots** (`settings/rbac*.png`, `settings/apikeys*.png`) still carry the old
  navigation; they need the Keycloak stack to regenerate.
- **ServiceNow instances** forecast in ticket-number order until someone turns this switch on. That is
  now the workaround, and it is worth saying out loud to those users.

# Mutation testing — parent-from-issue-links

## Slice 01 — Stryker.NET deliberately not run

**Decision** (user, 2026-09-19): skip Stryker.NET for `JiraWorkTrackingConnector.cs` and do the
analysis by hand instead.

**Why.** Stryker runs the test suite once per mutant. Slice 01's coverage includes
`JiraLinkTypeDogfoodTest`, a `JiraIntegration`-categorised test that talks to the real
`letpeoplework.atlassian.net` over the network. Every mutant would drag real HTTP calls with it —
minutes per mutant, against a shared instance whose credential CI also uses. Excluding the live
categories is possible but leaves Stryker measuring a subset that no longer matches what the slice
actually relies on.

This is an explicit deviation from the feature's Definition of Done item 5 ("mutation testing run on
both stacks, kill rate ≥ 80%, recorded here"), not a silent skip. The frontend has no slice-01 change,
so no StrykerJS run is owed yet; slice 03 adds the first frontend change and the decision should be
re-taken there, where no live connector is involved.

## What was done instead

A manual mutation analysis over the slice-01 production diff, enumerating operators against
`TheVerdictOnReferencesThatResolvedToNothing`, `ResolveWhatTheFieldListMissedAgainstLinkTypes`,
`GetIssueLinkTypes`, `TheLinkTypesIn` and the three records, then matching each mutant to a killing
test. Closed in step 01-07 (`7ccbaddf3`).

**Killed by existing tests** — recorded because a kill is the evidence a test is load-bearing:

| Mutation | Killed by |
|---|---|
| Remove the early return that skips the link-type call | `No_unresolved_reference_means_no_link_type_call_at_all` |
| `answeringTypes.Count == 1` → `>= 1` (first-one-wins) | `A_label_two_different_types_answer_to_resolves_to_neither` |
| Drop `Name`, `Inward` or `Outward` from `AnswersTo` | the three-row which-label outline, one row each |
| `OrdinalIgnoreCase` → `Ordinal` on the directional labels | `Case_is_ignored_on_the_directional_labels_too` |
| Swap the empty-list and listing branches | the two empty-list scenarios |
| Reorder credential check against the reference verdict | `The_credential_is_checked_before_the_instance_is_asked_what_it_defines` |

**Survivors, all closed in 01-07:** one behavioural defect (a reply Lighthouse could not parse was
reported as a credential problem) plus four unasserted guarantees (the refusal `Code`, `FieldName` on
the two credential-suspecting branches, the refusal status in the message text, and the two defensive
guards in matching and parsing).

## Two things learned that outlast this slice

**The analyzer gate kills part of the mutation space for free.** Two mutants in the hand-built survivor
list could not be written at all: deleting the status interpolation leaves an unused local (`S1481`),
and `Length: > 0` → `>= 0` trips `S3981`. With `TreatWarningsAsErrors` those are compile errors, not
survivors. A hand-built survivor list should be checked for compilability before any entry is treated
as a gap.

**Two guard mutations can mask each other.** Removing the label-length guard and the name-length guard
together makes a blank reference ambiguous rather than matched, so the assertion that would catch
either one passes. They have to be flipped separately to establish each kill.

---

## Slice 02 — Stryker.NET not run, same reason, same substitute

The slice adds a second live fixture, `JiraParentLinkDogfoodTest`, so the objection from slice 01 holds
with more force: every mutant would carry real HTTP against a shared instance whose credential CI uses.
Manual analysis again, over the slice-02 production diff — `IssueExtensions.ResolveParentFromLinks` and
its helpers, `ParentResolution`, `ParentSourceSelector`, and the connector's probe, per-refresh caches,
predefined exclusion and written-back key.

**Killed by existing tests:**

| Mutation | Killed by |
|---|---|
| Remove `Distinct(StringComparer.Ordinal)` | the duplicate-links-to-one-issue test — two links to the same key become two candidates and the item goes unparented |
| Remove the empty-key filter | the four malformed-link cases — a `[""]` candidate makes `Candidates` non-empty |
| `IsResolved => Count >= 1` (first-one-wins) | the ambiguity tests at unit and acceptance grain, and the live dogfood |
| Drop `name`, `inward` or `outward` from `LinkTypeAnswersTo` | its three-phrase casing outline |
| `CounterpartKeyOf`'s `Length > 0` → `>= 0` | the inward-end direction test |
| Flip `TheFieldTheInstanceHangsParentsOn`'s ternary | both no-override guard pins, Team and Portfolio |
| Remove the credential probe's throw, or negate its status check | the refresh-stops-rather-than-empties tests |
| Remove the probe's per-refresh short circuit | the credential-check count assertion |
| Remove the predefined exclusion, or widen it to cancel the whole lookup | 02-08's two tests |

**Survivors — three, all in the parser, one worth closing:**

1. **`Reads`'s `label.Length > 0` guard.** No test passes an empty reference, so removing it survives.
   This is the same guard slice 01 closed in the connector's matcher; `IssueExtensions` carries a second
   copy and it is unguarded. Worth closing for symmetry.
2. **`Distinct(StringComparer.Ordinal)` → `OrdinalIgnoreCase`.** No test has counterpart keys differing
   only by case. The comparer was chosen deliberately — two keys differing only in case are genuinely two
   references — and that reasoning is unasserted. Jira does not emit such keys, so this is near-equivalent.
3. **`CounterpartKeyOf`'s outward-before-inward preference.** Swapping it changes nothing unless one entry
   carries both ends, which real Jira never sends. Equivalent.

**Two suspicions that dissolved on inspection, recorded so they are not re-raised:**

- Removing the source guard in `ShowBackWhatTheLinksSaid` looked as though a field-source override would
  overwrite that field's stored value. It would write the value already there: `TheParentOf` returns
  `GetAdditionalFieldValue(overrideFieldId)` for a field source. **Equivalent mutant.**
- The refresh-versus-validation split on the predefined exclusion looked unpinned on the validation side.
  A single-operator mutation flips both sides, and the refresh side is pinned, so it dies there. A
  one-sided break is not reachable by one mutation.

**Structural note, not a mutant:** `ParentResolution.IsAmbiguous` has no production consumer — only tests
read it. It is written ahead of slice 03, which needs it for the ambiguity warning. If slice 03 ends up
not using it, it should go rather than stay as a public member nothing calls.

*(Settled in slice 03: `IsAmbiguous` now has two production consumers — the warning's filter and the
per-record marker the count is taken from. It stays.)*

---

## Slice 03 — Stryker.NET not run, and StrykerJS not run either

**Stryker.NET**: same objection as slices 01 and 02, now with a third live fixture in the covering set.
Every mutant would drag real HTTP against `letpeoplework.atlassian.net`, whose credential CI shares.
Manual analysis again.

**StrykerJS**: slice 03 is the feature's first frontend change, and the slice-01 note said the decision
should be re-taken here rather than inherited. Re-taken, and the answer is still no — for different
reasons, which is why it is written out rather than pointed at:

- The frontend diff is about fifteen lines in one component: one optional field on a model, one
  `Math.max` over a filtered list, one conditional stat, one `getTerm` call. The mutation space is small
  enough to enumerate by hand and be sure the enumeration is complete.
- A StrykerJS run is over the whole frontend, not this component, so the cost is unrelated to the size
  of what changed.
- This corpus has been bitten before: a previous StrykerJS run left `@ts-nocheck` in 661 files.

This is a deviation from Definition of Done item 5 on both stacks, recorded rather than skipped.

### Backend — killed by existing tests

| Mutation | Killed by |
|---|---|
| `LinksNamedMoreThanOneParent = IsAmbiguous` → `IsResolved`, or the assignment deleted | the two count scenarios — the row reads 0 where 3 is asserted |
| The propagation line in `WorkItemBase`'s protected copy constructor deleted | **measured**: both count scenarios fail. This is the seam between `CreateWorkItemFromJiraIssue` and `new WorkItem(base, team)`, the classic place coverage is zero, and it is covered |
| `Where(… IsAmbiguous)` negated or removed in the aggregated warning | `The_warning_names_the_item_and_every_candidate` |
| The `Count == 0` early return removed | the no-ambiguity scenario's `NothingWasWrittenToTheLogAsAWarning` — a warning appears on a clean refresh |
| The warning call moved inside the per-issue loop | `TheOneWarningTheRefreshWrote` — one warning per refresh, not one per record |
| `string.Join(", ", Candidates)` → first candidate only | the scenario naming both `EPIC-1` and `EPIC-4` |
| `CountTheRecordsNobodyCouldPlace` counting every record rather than the marked ones | the count scenarios (3 asserted, item total returned) |
| Either updater's `RecordsWhoseLinksNamedMoreThanOneParent = outcome.…` line deleted | **measured** during 03-03's RED: team and portfolio count scenarios, `Expected: 3 / But was: 0` |
| The `with { … }` dropped from either **whole-query** `SyncOutcome` site | the team and portfolio count scenarios |

### Backend — one survivor, measured and then closed

**Both delta `SyncOutcome` sites.** Replacing `CountTheRecordsNobodyCouldPlace(downloaded)` with a
literal `0` at `FetchOnlyWhatMoved` and `FetchOnlyTheFeaturesThatMoved`, rebuilding, and running 500
tests matching `ParentFromIssueLinks|WorkItemService|Updater|Delta|Sync` gave **0 failed, 500 passed**.
The mutant lived.

Worth closing rather than recording, because the cheap refresh is the **default** in production: the
unpinned path was the one most instances actually run, while the two pinned sites were the ones they
run least.

Closed in `f0fa9104d` by two service-level tests in `WorkItemServiceTest.cs`, one per driving port
(`UpdateWorkItemsForTeam` / `UpdateFeaturesForPortfolio` are separate entry points, so one test could
not reach both without faking one). Each asserts `Mode == SyncMode.Delta` beside the count as a positive
control — without it the portfolio test would pass on the whole-query branch, where the fixture's
default answer is an empty list and a count of zero looks the same as a fetch that never happened.
Re-applying the mutation fails both on the count assertion only, with the `Mode` control still green.

Reaching the delta path at all needed `WorkItemServiceTestBuilder` to stop hardcoding
`Mock.Of<IRepository<OptionalFeature>>()`. No fixture could opt into the cheaper refresh before, which
is its own small finding about what that builder could express.

### Frontend — hand analysis

| Mutation | Killed by |
|---|---|
| `Math.max(…)` → `reduce` sum, or → average | the 5-and-3 scenario: sum renders 8, average 4, max 5, and 8 and 4 are asserted absent |
| The `runs > 0` guard removed | an empty window spreads into `Math.max()` → `-Infinity`, which renders the row |
| `> 0` → `>= 0` on the conditional | the silent-at-zero scenario |
| `getTerm(WORK_ITEMS)` → the literal `"Work Items"` | the scenario that renames the term to `Tickets` and asserts `/work items/i` appears nowhere |

**One frontend survivor, and it is near-equivalent rather than a gap.** Removing `?? 0` lets
`undefined` reach `Math.max`, which yields `NaN`; `NaN > 0` is false, so the row hides — exactly what
the old-backend scenario already expects to see. That scenario therefore passes with the guard gone,
and so do the other two, whose fixtures all carry the field.

It is only near-equivalent because a *mixed* window would behave differently: `Math.max(5, undefined)`
is `NaN`, so one row missing the field would hide a count of 5 that ought to show. That cannot arise
from one backend — the column is non-nullable with a default of 0, so a given backend either sends it
on every row or on none — which is why this is recorded rather than closed. It would stop being
near-equivalent the moment anything served a partial payload.

The honest reading of the original enumeration: this entry was first written into the table as
"killed by the old-backend scenario", which is what it looks like until you work out what `NaN` does
to the comparison. Checked, not assumed.

### Two things that outlast this slice

**A count and a warning taken from the same fact can still disagree.** A Portfolio's parent sweep runs
through the same `CreateFeaturesFromIssues`, so an ambiguous parent Feature is named in the warning —
but that half of the refresh deliberately stays out of `SyncOutcome`, so it is never counted. No mutant
finds this: both sides are behaving exactly as written. It took reading the call graph.

**A mutation that cannot be reached by any fixture is not the same as one that is killed.** The delta
sites looked covered — `WorkItemService` is a heavily tested class and the count's own scenarios are
green — but nothing could opt into the cheap path, so the coverage stopped at the branch. The kill has
to be demonstrated, not inferred from the neighbourhood.

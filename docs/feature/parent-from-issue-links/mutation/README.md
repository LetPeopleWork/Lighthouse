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

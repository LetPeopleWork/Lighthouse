# Slice 05 — A setting costs a refetch only when it changes what is fetched

**Feature**: epic-5687-faster-updates · **ADO**: Epic #5687 → Story #5728 · **Story**: US-05 (+ US-08 precursor)
**Estimate**: ~6h · **Reference class**: `PrepareQuery` on both connectors — the fingerprint is, almost
exactly, the argument list of that method plus the connection it runs against.

## Goal

Lighthouse decides for itself whether an edit changed what gets fetched: a query, type, state, cutoff,
additional-field, parent-override or connection change forces the next cycle to be a full fetch;
everything else provokes no remote fetch at all.

## IN scope

- **Precursor commit (US-08, `@infrastructure`)**: a fetch-fingerprint column on Team and Portfolio,
  additive expand-only migration via `CreateMigration`.
- Fingerprint computed from exactly: `DataRetrievalValue`, `WorkItemTypes`, `AllStates`,
  `DoneItemsCutoffDays`, additional field definitions, parent-override field,
  `WorkTrackingSystemConnectionId`. Order-insensitive for the collections, so re-saving the same states
  in a different order is not a change.
- Mismatch → next cycle is `mode=full`, and the summary line names configuration as the reason.
- Everything outside the list — wait states, blocked rules, staleness threshold, named cycle times,
  ordering policy, terminology — leaves the fingerprint alone and provokes nothing.
- **A guard test** that enumerates the properties reachable from `PrepareQuery` and the connector call
  sites and fails when one is neither in the fingerprint nor on an explicit, commented exclusion list.
- An instance upgrading into this feature has no stored fingerprint, so its first cycle is full (D8).

## OUT of scope

- Triggering an update on save. Settings saves do not fetch today (`TriggerUpdate` is called only from
  the manual controller endpoints and the periodic loop) and this slice does not change that — it only
  decides what *mode* the next scheduled cycle runs in.
- A manual "Force full refresh" action. D3 makes it unnecessary and its presence would imply the
  automatic path is not trusted.
- Per-field granularity ("only the states changed, so refetch only the newly included states"). A
  fingerprint is a boolean, deliberately.
- Any UI.

## Amendment 2026-08-09 — there is already a second answer to this question, and it disagrees

Found while preparing slice 02. Read this before writing the fingerprint: **the "does this edit change
what gets fetched?" decision already exists in the codebase**, under a different name, with a different
property list, and only for one of the two entities.

### What is there today

`TeamController.UpdateTeam` (`:178`) calls `team.WorkItemRelatedSettingsChanged(teamSetting)`
(`API/Helpers/TeamExtensions.cs:65`). On true it runs `workItemRepository.RemoveWorkItemsForTeam(team.Id)`
— **it deletes every stored work item for the team** — and the next scheduled cycle refetches from
nothing. On false, nothing is deleted and nothing is fetched.

So the promise this slice is named after — *a setting costs a refetch only when it changes what is
fetched* — is **already half-kept**. Editing wait states, blocked rules, staleness thresholds, the SLE or
cycle-time definitions on a team is genuinely free today. That is worth knowing before writing a brief
that implies none of it exists.

### Three defects in what is there

1. **Portfolio has no equivalent at all.** `PortfolioController.UpdatePortfolio` (`:96`) syncs and saves;
   there is no change detection and no purge. Its Features are reconciled only by the fetch's own
   removal rule on the next cycle. Two entities, two different answers to one question.

2. **The team path is destructive, and possibly redundantly so.** It answers "the query changed" by
   deleting stored work items *and their history*, then refetching. But `removed = stored − fetched`
   already reconciles exactly that on any full cycle — which is precisely how the portfolio side copes
   without a purge. The purge looks like belt-and-braces bought with transition history. **Verify before
   removing it**: the plausible reason it exists is a case the removal rule does not cover, and the fact
   that nobody wrote that reason down is not evidence there isn't one.

3. **The property lists differ, and `DoneItemsCutoffDays` is a live gap.**

   | Property | `WorkItemRelatedSettingsChanged` | this slice's fingerprint |
   |---|---|---|
   | `DataRetrievalValue`, connection id, `WorkItemTypes`, all states | ✅ | ✅ |
   | `StateMappings` | ✅ | ❌ |
   | `DoneItemsCutoffDays` | ❌ | ✅ |
   | additional field definitions | ❌ | ✅ |
   | parent-override field | ❌ | ✅ |

   `DoneItemsCutoffDays` is part of the remote query's resolved-cutoff clause — it demonstrably shapes
   the result set — and changing it triggers no purge today.

### What this slice must therefore do

**One property set, two consumers.** The fingerprint and the save-time decision are the same question
asked twice; they must not ship as two lists. Fold `WorkItemRelatedSettingsChanged` onto the fingerprint's
property set, and extend the guard test (already an acceptance criterion here) to cover **both** call
sites rather than the fingerprint alone.

This is the difference between the slice's stated hypothesis holding and quietly failing. As briefed, the
guard test protects the fingerprint from drift while an older, shorter list sits in a file the guard does
not look at — which is the very drift the test exists to prevent, reintroduced one directory away.

While doing it, resolve the portfolio asymmetry: whatever the answer is, both entities give it.

### Two things this amendment is not

- **Not gated by the opt-in flag** (slice 02's amendment). This is true whether the next cycle is full or
  delta, so it ships plain.
- **Not blocked on delta either — but not independent of it.** With delta off, the fingerprint's only
  output ("the next cycle is `mode=full`") is what already happens, so it is *inert*, not inactive: the
  column fills, the guard test runs, and no behaviour changes until someone opts in. That makes it safe
  to ship early and cheap to ship ungated, and it is why this slice needs no flag of its own.

Whether the `DoneItemsCutoffDays` gap is severe enough to pull forward as its own bug ahead of this slice
is open, and is the maintainer's call.

## Learning hypothesis

**Disproves "the fetch-shaping property set can be enumerated in one place."** If it cannot — if a
property reaches the remote query through a path the guard test cannot see — then the fingerprint will
drift the first time someone adds a connector option, and delta will serve a stale result set with every
test green. That failure mode is wrong numbers with no error, which is why the guard test is an
acceptance criterion and not a nice-to-have.

If it fails, D3 collapses back to "any settings save invalidates", which is safe and wastes the win — a
worse product, but an honest one, and better learned in six hours than after three connectors ship.

Secondary: **disproves "local-only edits are actually local."** Blocked rules are the interesting case —
they are evaluated at sync time and drive `FeatureBlockedTransition` writes. If changing a blocked rule
requires re-deriving from remote data rather than from stored data, it belongs in the fingerprint after
all, and the epic's second promise shrinks.

## Acceptance criteria

AC-5.1 … AC-5.6 from `feature-delta.md` (US-05). AC-5.4 (the guard test) is the one that makes the rest
survive contact with the next connector option someone adds.

## Dependencies

- Slices 02-04. This is a correctness gate over everything delta has shipped so far: until it lands, a
  query edit is knowingly unprotected on three connectors, which is why it runs before the remaining
  three rather than after them.

## Effort

~6h. Migration ~0.5h, fingerprint + storage ~1.5h, mode/reason plumbing ~1h, guard test ~1.5h, remaining
tests ~1.5h.

## Production data / dogfood moment

On `:5169` with real recorded history: change a team's query, watch the next cycle log
`mode=full | reason=configuration-changed`; then add a wait state, watch the next cycle log
`mode=delta | fetched=0`. Two log lines, same day, and they are the acceptance for KPI-4.

## Pre-slice SPIKE

Not needed. The uncertainty is enumerability, and the guard test answers it inside the slice.

## Verdict

_(recorded at slice close)_

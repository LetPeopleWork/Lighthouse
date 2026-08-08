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

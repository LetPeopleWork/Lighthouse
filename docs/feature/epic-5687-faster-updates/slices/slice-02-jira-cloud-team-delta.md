# Slice 02 — A Jira Cloud team refresh fetches only what moved

**Feature**: epic-5687-faster-updates · **ADO**: Epic #5687 → Story #5725 · **Story**: US-02 (+ US-08 precursor)
**Estimate**: ~6h · **Walking skeleton for the epic.**
**Reference class**: `AzureDevOpsWorkTrackingConnector` — already two-phase (`QueryByWiqlAsync` → ids,
then `GetWorkItemsInChunks`). This slice gives Jira the same shape and adds the "only the changed ones"
step that ADO does not have either.

## Goal

The second and later refreshes of a Jira Cloud team download full issue payloads only for issues whose
`updated` timestamp moved, while still enumerating the whole query result so removals are still caught.

## IN scope

- **Precursor commit (US-08, `@infrastructure`)**: nullable UTC `LastChangedRemote` on the work item,
  additive expand-only migration via `CreateMigration`, copied explicitly inside `WorkItem.Update(…)`.
- A sweep capability on the connector: same JQL, `fields=updated`, returning `(referenceId, updated)` for
  the full result set.
- Per-item comparison (D12) — `sweep.updated != stored.LastChangedRemote` — with no global watermark, so
  clock skew and server-time drift are out of the design. Items whose timestamp falls inside the sweep's
  uncertainty window are treated as changed on the following cycle too.
- Payload fetch (fields + changelog, including the >30-entry paged path) restricted to the changed set.
- **Removal path unchanged**: `removed = stored − sweepIds` (D2).
- **Staleness evaluation moves off the fetched-item loop onto the stored set** (D10) —
  `AddStalenessEventIfThresholdCrossed` no longer lives inside the `foreach (actualWorkItems)` body.
- Mode resolution per D8: full when never swept, when any stored item lacks a timestamp, or when the
  sweep failed. No partial mode, ever.
- `mode=delta` and the real scanned/fetched counts start flowing into slice 01's summary line.

## OUT of scope

- Jira Data Center (slice 04) — this slice touches `GetIssuesByQueryFromCloud` only.
- Portfolio Features and parent Features (slice 03).
- The fetch fingerprint (slice 05). Until it lands, a query edit is knowingly unprotected — which is why
  slice 05 is a correctness gate and not an optimisation.
- ADO, ServiceNow, Linear.
- Any UI.

## Learning hypothesis

**Disproves "a cheap identity sweep is materially cheaper than the full fetch"** — the epic's entire
premise. Three ways it fails:

1. **The scan is the cost.** If Jira charges essentially the same for a JQL returning one field as for
   one returning the full field set plus changelog, the saving is a rounding error and the epic should
   stop after slice 01 having cost one day.
2. **`updated` is not trustworthy.** If `updated` does not move for a change Lighthouse cares about —
   a transition recorded in the changelog without a field write, say — then delta drops real changes and
   the metrics quietly go wrong. AC-2.4's byte-identical assertion is what catches this.
3. **Staleness cannot leave the fetch loop cheaply.** If evaluating staleness over the stored set needs
   a query shape the repository does not support, D10 becomes its own slice and the walking skeleton is
   thinner than planned.

Confirms, if it succeeds: the contract is real and slices 03/04/06/07/08 are applications of it, not
redesigns.

## Acceptance criteria

AC-2.1 … AC-2.9 from `feature-delta.md` (US-02). AC-2.5 (staleness under delta) and AC-2.7
(`LastChangedRemote` survives `Update(…)`) are the two that fail silently if skipped — both are
performance or correctness regressions with every other test still green.

## Dependencies

- Slice 01 (the summary line is where AC-2.8's request-count evidence is read).
- A real Jira Cloud project with ≥1000 issues in one team query.

## Effort

~6h. Migration + `Update(…)` copy ~1h, sweep ~1.5h, comparison + mode resolution ~1.5h, D10 move ~1h,
tests ~1h.

## Production data / dogfood moment

Point a team at the real Jira Cloud project, let one full cycle run, then let a delta cycle run, and read
both summary lines side by side. Same day. Synthetic issue counts prove plumbing, not the premise —
AC-2.8's ≤10% target only means something against a project with real changelog depth.

## Pre-slice SPIKE

Not needed — Jira Cloud's `fields` parameter and `updated` semantics are documented and already exercised
by `GetIssuesByQueryFromCloud`.

## Verdict

_(recorded at slice close — confirmed / disproved, with the two summary lines)_

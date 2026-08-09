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
- **The opt-in gate (added 2026-08-09 — see "Opt-in gate" below).** An `OptionalFeature` that defaults
  to off; delta is unreachable until an instance turns it on.

## OUT of scope

- Jira Data Center (slice 04) — this slice touches `GetIssuesByQueryFromCloud` only.
- Portfolio Features and parent Features (slice 03).
- The fetch fingerprint (slice 05). Until it lands, a query edit is knowingly unprotected — which is why
  slice 05 is a correctness gate and not an optimisation.
- ADO, ServiceNow, Linear.
- Any UI.

## Opt-in gate

**Added 2026-08-09, after slice 01 shipped.** Delta ships dark: present in the build, off until an
instance asks for it.

**Why.** D2 is the reason, not testing convenience. The removal rule is `removed = stored − sweepIds`,
so a sweep that loses an id **deletes live work items**. That is data loss with a green pipeline, and it
is the one failure mode this epic can produce that a user cannot undo. An opt-in means only instances
that volunteered can be hurt by it, and the named on-prem Data Center instance — whose pagination is
*already* the known duplicate-id hazard (OQ-1) — opts in deliberately rather than by upgrading.

Secondary benefits: a soft launch, and a real-world A/B where the same instance can toggle back and
compare two summary lines from slice 01.

**Mechanism.** The `OptionalFeature` machinery already exists — entity, seeder, repository, controller,
Settings UI, and `IsPreview` / `IsPremium` flags. It is currently **dormant**: all four historical keys
are deprecated and `OptionalFeatureSeeder.GetOptionalFeatures()` returns an empty list. Note that **no
backend service reads an optional feature today** — every past use gated UI only. This slice is the
first backend-gated one, so the read path is new work, not a call into an existing helper.

- New key in `OptionalFeatureKeys`, seeded `Enabled = false`, `IsPreview = true`.
- Read **per update**, in the update's own scope — never cached at startup — so toggling takes effect on
  the next cycle with no restart. That property is what makes it usable for a soft launch.
- The flag is a **parameter into** `SyncModeResolver`, not a dependency of it. DDD-5 keeps that type a
  pure static; `WorkItemService` resolves the flag and passes the bool.
- It composes with, and does not replace, `SupportsIncrementalSync(connection)`. Capability stays
  per-connector; the opt-in is per-instance. A global toggle plus a per-connector probe covers the
  rollout without a per-connector opt-in matrix.
- Off is not a special case: it resolves to `SyncMode.Full`, which is D8's existing "ambiguity resolves
  to a full fetch". One more branch into an outcome the resolver already has.

**Blast radius: this slice only.** Slices 03, 04, 06, 07 and 08 all route their mode decision through the
same resolver, so they inherit the gate for free and none of their briefs change. Slice 01 is
deliberately **not** gated — the log signal is the instrument by which anyone judges whether the toggle
did anything, and gating the instrument alongside the thing it measures leaves nothing to read. Slice 05
is not gated either; see its own amendment for why it is inert rather than independent.

**Additional acceptance criteria**

- AC-2.10 With the feature off (the default), every update runs `mode=full` and issues no sweep — asserted
  by a fake connector whose sweep method fails the test if it is called at all.
- AC-2.11 Turning the feature on takes effect on the next cycle without a restart.
- AC-2.12 A fresh install has the feature off, and an instance upgrading into this release stays off.

**Removal.** The flag is preview scaffolding with a defined end: once the epic has run against real
instances and KPI-3 (removal correctness, 20 consecutive delta cycles with zero drift) holds, the flag
flips to on-by-default and then goes away. Record that decision when it happens rather than leaving a
permanent branch — a gate nobody ever removes becomes a second code path forever.

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

~7h. Migration + `Update(…)` copy ~1h, sweep ~1.5h, comparison + mode resolution ~1.5h, D10 move ~1h,
tests ~1h, opt-in gate ~1h (key + seeder entry + the per-update read + AC-2.10…2.12). The gate is cheap
*because* it lands on the resolver rather than on each connector — if it starts looking bigger than an
hour, that is the signal the mode decision has leaked out of `SyncModeResolver`.

## Production data / dogfood moment

Point a team at the real Jira Cloud project, let one full cycle run, then let a delta cycle run, and read
both summary lines side by side. Same day. Synthetic issue counts prove plumbing, not the premise —
AC-2.8's ≤10% target only means something against a project with real changelog depth.

## Pre-slice SPIKE

Not needed — Jira Cloud's `fields` parameter and `updated` semantics are documented and already exercised
by `GetIssuesByQueryFromCloud`.

## Verdict

_(recorded at slice close — confirmed / disproved, with the two summary lines)_

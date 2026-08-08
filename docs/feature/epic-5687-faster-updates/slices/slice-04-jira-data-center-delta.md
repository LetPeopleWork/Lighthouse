# Slice 04 — The 25-year Data Center instance, safely

**Feature**: epic-5687-faster-updates · **ADO**: Epic #5687 → Story #5727 · **Story**: US-04 · **Estimate**: ~6h
**plus a 2h pre-slice probe** · **Reference class**: slices 02/03. Same contract, different transport —
`GetIssuesByQueryFromDataCenter` instead of `…FromCloud`.

This is the instance the epic was written about: *"looking at you, on prem jira instance with 25 years of
history"*.

## Goal

Jira Data Center teams and portfolios get delta refreshes, and the identity sweep is demonstrated
trustworthy on DC's pagination **before** it is allowed to drive deletions.

## Pre-slice SPIKE (2h, timeboxed) — resolves OQ-1

Run a dev build against the real DC system (access arranged, user 2026-08-08): issue the identity sweep
twice back to back over a query returning ≥5000 issues, and compare the id **sets**.

- Set identical → proceed; record the numbers here.
- Duplicates within a sweep but the same set → proceed, collapse duplicates the way
  `DeduplicateByReferenceId` already does, log the collapsed count.
- Sets differ → **stop**. Add a deterministic ordering clause to the sweep query and re-run the probe. If
  the set is still unstable, DC delta does not ship on that sweep, and this slice records the finding
  instead of shipping code.

Why this must run first: `removed = stored − sweepIds` deletes items. A sweep that can *lose* an id on a
page boundary would delete live work items on the biggest instance in the field. The failure would look
like data loss, not like slowness. DC offset pagination over an unordered JQL is already the documented
source of duplicate `ReferenceId`s (`docs/ci-learnings.md`, 2026-05-25) — the hazard is known to exist in
one direction; the probe establishes whether it exists in the other.

## IN scope

- The probe above, with its result written into this brief before implementation starts.
- The slice-02 sweep implemented on the DC transport, for both team work items and portfolio Features.
- Duplicate collapse inside the sweep, logged with a count.
- One shared sweep contract across Cloud and DC — a connector implements a sweep, it does not choose a
  strategy.

## OUT of scope

- ADO, ServiceNow, Linear.
- The fetch fingerprint (slice 05).
- Any attempt to fix DC's pagination generally — the scope is making the sweep safe, not making DC
  pleasant.
- Any UI.

## Learning hypothesis

**Disproves "DC's pagination yields a stable id set", and with it D1 on the instance that motivated the
epic.** If the probe fails, the two-phase design still holds on Cloud and ADO but cannot drive deletion
on DC, and the epic must choose between a DC-specific reconcile cadence and leaving DC on full fetches —
a decision worth making with evidence rather than after shipping.

Secondary: **disproves "the DC saving is worth the risk"**. If a DC cycle is dominated by the JQL scan
itself rather than by payload and changelog transfer, delta buys little on precisely the instance that
needed it most.

## Acceptance criteria

AC-4.1 … AC-4.5 from `feature-delta.md` (US-04). AC-4.2 is a stop condition, not a nicety.

## Dependencies

- A real Jira Data Center instance with substantial history. **Arranged** (user, 2026-08-08): a dev build
  will be run against a real DC system when this slice comes up, which is what makes the probe a
  measurement rather than an argument. Scheduled, not blocking — no other slice needs it.
- Slices 02 and 03 (the contract, on both entity types).

## Effort

2h probe + ~6h implementation. If the probe fails, the slice closes at 2h with a recorded finding — which
is the point of putting it first.

## Production data / dogfood moment

The probe *is* production data. After implementation, one full cycle and one delta cycle on the DC
instance, both summary lines recorded in the verdict.

## Verdict

_(recorded at slice close — probe result first, then the two summary lines)_

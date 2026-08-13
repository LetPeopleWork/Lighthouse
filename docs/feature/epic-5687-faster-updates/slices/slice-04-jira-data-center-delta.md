# Slice 04 — The 25-year Data Center instance, safely

**Feature**: epic-5687-faster-updates · **ADO**: Epic #5687 → Story #5727 · **Story**: US-04 · **Estimate**: ~6h
**the 2h pre-slice probe is spent — see below** · **Reference class**: slices 02/03. Same contract, different transport —
`GetIssuesByQueryFromDataCenter` instead of `…FromCloud`.

This is the instance the epic was written about: *"looking at you, on prem jira instance with 25 years of
history"*.

## Goal

Jira Data Center teams and portfolios get delta refreshes, and the identity sweep is demonstrated
trustworthy on DC's pagination **before** it is allowed to drive deletions.

## Pre-slice SPIKE — RUN 2026-08-11, OQ-1 ANSWERED: pagination is stable

**Result: the id set is stable, so this slice is transport work, not a redesign.** Probed against a real
`Jira Server 10.3.6` instance (it reports `deploymentType: Server`, and `GetDeploymentType` maps every
non-Cloud deployment to DataCenter, so the probed code path is the one this slice ships):

| Query | Issues | Pages | Passes | Duplicates | Walked vs total |
|---|---|---|---|---|---|
| Team | 5056 | 102 | 3 | 0 | equal |
| Portfolio (Epics) | 597 | 12 | 3 | 0 | equal |

`fields=key,updated`, page size 50. Every pass returned an identical set, and sorted and unsorted runs
agreed with each other. AC-4.1 is satisfied and AC-4.2's stop condition did not fire.

**What the probe did not cover, and what this slice does about it:**

- **Churn during the walk.** The instance was quiet and each walk took about 8 seconds. An item edited
  mid-walk can still move between pages under offset pagination, so the sweep appends its own
  `ORDER BY key ASC` regardless of the clean result — an ordering on an immutable field cannot be
  disturbed by an edit, which the default relevance ordering cannot promise.
- **The keyed `key in (…)` shape the parent path uses.** Probed only the plain team and portfolio
  queries. The parent path stays on its existing behaviour until an implementation step covers it.

Original probe design, kept because it is what makes the numbers above a measurement:

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

- ~~The probe above, with its result written into this brief before implementation starts.~~ Done
  2026-08-11, result recorded above.
- A deterministic `ORDER BY key ASC` on the DC sweep query, for the churn case the probe could not
  exercise on a quiet instance.
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

**Not disproved** — the probe ran clean on 2026-08-11, so D1 survives on the instance that motivated the
epic and this slice proceeds to implementation.

Secondary: **disproves "the DC saving is worth the risk"**. If a DC cycle is dominated by the JQL scan
itself rather than by payload and changelog transfer, delta buys little on precisely the instance that
needed it most.

## Acceptance criteria

AC-4.1 … AC-4.5 from `feature-delta.md` (US-04). AC-4.2 is a stop condition, not a nicety.

## Dependencies

- A real Jira Data Center instance with substantial history. **Met** — the probe ran against one on
  2026-08-11. The same instance is what the dogfood cycles below need.
- Slices 02 and 03 (the contract, on both entity types).
- Slice 05 (the fetch fingerprint), which shipped ahead of this slice so that a query edit is not
  knowingly unprotected for anyone already opted in.

## Effort

~6h implementation. The 2h probe is spent, and it returned the answer that lets the implementation
happen at all.

## Production data / dogfood moment

The probe *is* production data. After implementation, one full cycle and one delta cycle on the DC
instance, both summary lines recorded in the verdict.

## Verdict

**Shipped 2026-08-13, and the premise held on the instance the epic was written about.** The probe
result is above: pagination is stable, so the sweep was allowed to drive removal. The two cycles below
are from the maintainer's own on-premise Data Center instance, read off a CI-built package.

| Cycle | scanned | fetched | duration |
|---|---|---|---|
| Full | 1457 | 1457 | 468 856 ms (7 min 49 s) |
| Delta | 1457 | 0 | 2 087 ms (2.1 s) |

**225× faster on a cycle with nothing to fetch.** The number that matters for correctness is not the
duration though — it is that `scanned` is 1457 in both. The sweep enumerated exactly the result set the
full download enumerated, over roughly thirty offset pages. Removal is `stored − swept`, so a sweep
that had dropped a record on a page boundary would show a smaller `scanned` and would have deleted live
work items; at this size, on this instance, it did not.

AC-2.8's Data Center reading falls out of the same lines: a delta cycle that fetches nothing issues only
the sweep's page requests, against 1457 payload downloads plus paging for a full cycle — far inside the
ten-percent target. The Jira Cloud reading is still owed on a project of comparable size.

Not yet read: a third cycle after changing one issue, which should report `fetched=1`. Deliberately not
taken here — the instance is live production, and editing someone's real work item to watch a log line
is not a reasonable thing to do to it. Deferred to the next window on that instance (2026-08-17). The
mechanism is shared code and the Cloud dogfood at slice 03 showed exactly that progression, so this is
confirmation rather than an open question.

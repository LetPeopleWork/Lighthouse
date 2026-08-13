# Slice 06 — Azure DevOps stops re-reading every revision

**Feature**: epic-5687-faster-updates · **ADO**: Epic #5687 → Story #5729 · **Story**: US-06 · **Estimate**: ~6h
**Reference class**: `AzureDevOpsWorkTrackingConnector` is already two-phase — `QueryByWiqlAsync` returns
id references, `GetWorkItemsInChunks` fetches payloads. This slice adds the missing middle step.

**D7 checkpoint answered 2026-08-13**: this slice stays in the epic and is the last one. Slices 07
(ServiceNow) and 08 (Linear) are deferred — ADO's per-item revision read is the biggest remaining cost,
and it is a tracker Lighthouse itself runs on.

## Goal

An ADO refresh calls `GetRevisionsAsync` — the dominant cost, one round trip per work item per cycle —
only for items whose `System.ChangedDate` moved.

## IN scope

- Identity sweep = the existing WIQL, which already returns ids only and therefore already costs what a
  sweep should cost.
- Change stamps = one batched `GetWorkItemsAsync` over the swept ids asking for `System.ChangedDate` and
  nothing else, 200 ids per request. **Amended at DISTILL, 2026-08-13**: the second WIQL this bullet used to
  name cannot report a stamp — a WIQL answers with ids only — and a sweep that cannot report a stamp for a
  quiet item cannot say it is quiet. Comparison stays per-item against the stored stamp (D12); no watermark
  is involved at all. See the AC-6.1 amendment in `feature-delta.md`.
- `LastChangedRemote` populated from `System.ChangedDate`.
- `GetAdoWorkItemsById` and `GetAllStateTransitionsThrottled` restricted to the changed set.
- Portfolio Features and parent Features on the same path.
- Removal semantics unchanged — the plain WIQL still yields the full id set every cycle (D2).

## OUT of scope

- ServiceNow, Linear.
- Any change to the throttling / chunking machinery (`ExecuteWithThrottle`, `MaxChunkSize`) — this slice
  makes it run less often, not differently.
- Any UI.

## Learning hypothesis

**Disproves "`System.ChangedDate` aligns with the revision history that drives transitions."** ADO
transitions are rebuilt by walking every revision of an item (`GetAllStateTransitionsThrottled`). If a
revision can land without `System.ChangedDate` moving — or if the WIQL's `ChangedDate` filter is
evaluated against a different clock than the revision timestamps — then delta on ADO drops state
transitions, and every downstream metric built on transitions (time-in-state, aging pace, cumulative
state time, blocked history) is silently wrong for the affected items.

This is the connector the maintainer dogfoods, which is both why it is worth doing and why a silent
transition drop here would be noticed late and hurt most.

Confirms, if it succeeds: the largest per-item remote cost in the codebase is paid only on change.

## Acceptance criteria

AC-6.1 … AC-6.5 from `feature-delta.md` (US-06). AC-6.3 is the alignment assertion.

## Dependencies

- Slices 02 and 05 (the contract, and the fingerprint gate).
- The D7 checkpoint verdict — answered 2026-08-13, in scope.
- Lighthouse's own ADO project — real data, already in daily use.

## Effort

~6h. Second WIQL + changed set ~1.5h, restricting payload and revision reads ~1.5h, portfolio path ~1h,
alignment tests ~2h.

## Production data / dogfood moment

Lighthouse's own ADO board. One full cycle, one delta cycle, and a comparison of the stored transition
counts across both — if a single transition differs, the hypothesis failed. Same day.

## Pre-slice SPIKE

Not needed as a separate probe — the alignment question is answerable inside the slice by asserting
against Lighthouse's own board, which has items with rich revision history.

## Verdict

_(recorded at slice close)_

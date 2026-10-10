# ADR-235: A Work Item Carries Its Estimate, Normalised by the Code the Estimation Chart Uses

**Status**: Accepted (maintainer, 2026-10-10)
**Date**: 2026-10-10
**Feature**: story-6248-work-items-dialog-context-columns (ADO User Story #6248)
**Decider**: Morgan (Solution Architect), interaction mode = PROPOSE; confirmed by the maintainer on 2026-10-10. That the estimate
comes from the backend, on the work-item payload, was decided by the maintainer in DISCUSS (2026-10-10).

---

## Context

The Estimation vs. Cycle Time chart reads each item's estimate from the owner's configured additional field,
normalises it (a number, or a position in the owner's category list), and sends the browser one entry per chart
point with the item ids and the display value. No work-item or Feature row carries the estimate, so the dialog
opened from a bubble cannot show it, and no other context can either.

The rows the dialog lists are built in 16 places across five controllers, from four DTO types: one base and three
that inherit it (the Feature row and the Team and Portfolio run-chart rows). Their constructors are already at or
past Sonar's parameter limit. Twelve of the sites know exactly one owner (the Team or Portfolio in the route, or the
child item's own Team). Four have no owner whose estimation field fits: three build Features that can belong to
several Portfolios, each with its own estimation field (the feature list by ids, Delivery rules validation and
Delivery sources' Features coming along); the fourth lists a Team's Features in progress, and a Team's estimation
field describes its Work Items, not Features.

DISCUSS locked: the estimate shown per row must be the value the chart plots for that item, normalised by the same
code; where the owner has no estimation field, no Estimate column is offered at all.

## Decision

1. **One normalisation path.** The existing normaliser gains one reader of the owner's estimation field (an item
   that lacks the field reads as having no value, never as an error) and two functions over it: one item to its normalisation result, and many items to the existing batch result with its
   mapped, unmapped and invalid counts. Both return nothing when the owner has no field. The estimation chart
   builders switch to the batch form and keep their diagnostics from its counts; the row uses the single form; so
   the chart and the rows cannot disagree. It is pure and static: no new
   service, no dependency-injection registration.
2. **An init-only property on the row.** The work-item DTO (and so every DTO derived from it) gains an optional
   estimate object: the number the chart plots (used for sorting, so categories sort in their configured order), the
   display value, and the owner's unit. It is absent when the owner has no estimation field, and present with no
   display value when the field is configured but the item has no usable estimate. Controllers set it in an object
   initializer where they construct the row; no constructor changes.
3. **Only where there is one owner.** The twelve owner-known sites set it; the other four leave it absent, each for
   its own reason: the three multi-Portfolio Feature sites have no single Portfolio to ask, and the Team's
   Features-in-progress list has a Team whose estimation field describes Work Items, not Features. This is an
   exception to "offered wherever estimation is configured" in one dialog: the Delivery timeline lists a Feature
   from the feature-list endpoint, which has no Portfolio to ask, so it offers no Estimate. Confirmed by the
   maintainer.
4. **Only mapped estimates are displayed.** A value the chart excludes (empty, non-numeric in numeric mode, or a
   category not in the owner's list) shows as an empty cell.

## Alternatives considered

- **A constructor parameter on each DTO.** Rejected: the base and Feature constructors are already past the
  parameter limit, both with a suppression, and the two run-chart DTOs pass their arguments down to the base; all 16
  sites would change, including the four that have nothing to pass.
- **A dedicated estimates endpoint the dialog calls on open.** Rejected: a new request per dialog, with its own
  loading and failure states and a race on quick re-clicks, for data the server already had when it built the rows.
- **Map the chart's per-point ids onto rows in the browser.** Rejected: only the Estimation context has that
  response; the estimate is wanted wherever estimation is configured.
- **An injected estimate-reader service.** Rejected: a DI registration and a constructor parameter on controllers
  near the limit, for a pure function.
- **Use the first Portfolio's field for a multi-Portfolio Feature.** Rejected: silently wrong for the others.

## Consequences

- Positive: one place decides which field and how it parses; an architecture test keeps every caller of the low-level
  normalisation inside the normaliser, and an integration test pins row and chart to the same value.
- Positive: additive JSON field; the Lighthouse clients have no strict response schemas, so nothing breaks. Their MCP
  tools that relay raw rows will show the estimate to agents.
- Negative: run-chart payloads repeat an item once per day bucket, so the small estimate object is repeated too (the
  same buckets already repeat the item's tags and every additional field value). Rough bound: about 60 bytes per copy, so a 90-day window with 40 items in progress
  grows by about 0.2 MB before compression.
- Negative: Feature rows from the feature list, Delivery rules and Delivery sources endpoints, and from a Team's
  Features-in-progress list, carry no estimate, so the Delivery timeline dialog offers no Estimate column.

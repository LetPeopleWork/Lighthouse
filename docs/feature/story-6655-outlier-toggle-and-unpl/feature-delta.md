# Story 6655 — Outlier switch and UNPL-based outliers

## Maintainer decisions

- **Request (2026-11-17, story text):** a switch to show all items, on by default at every load, state not
  stored; outliers based on the UNPL of the Cycle Time process behaviour chart instead of 5 × median; no
  configuration for now.
- **Named Cycle Time definitions (2026-11-17, option 1):** use the same rule — the UNPL is computed the same
  way over that definition's Cycle Times with the same baseline.
- **Future-proofing (2026-11-17):** the outlier rule is a replaceable definition (strategy passed in), so a
  different outlier definition can be supplied later without reworking callers. No configuration UI now.
- **Open, for the sketch review at the start of DELIVER:** does the UNPL count look useful on the team that
  showed 20+ outliers?

## DISCUSS checklist (proposed by the product owner, root-reviewed)

- **RBAC impact:** N/A — the switch only changes the view; the new read route sits behind the existing
  metrics read guard.
- **Lighthouse-Clients CLI / MCP:** N/A — the new route is additive and the clients do not read outliers.
- **Website marketing surface:** no new line; at finalize, check no website or docs copy still says
  "5 times the median".
- **Context map:** Flow metrics only; no domain events; UI surface stays the Metrics tab of a Team or
  Portfolio (ADR-271). Glossary: "Outlier" rewritten, "Upper natural process limit" added (ADR-270).
- **Continuous-improvement check:** N/A — this is a single Story with no parent Epic, not a new feature Epic.

## DEVOPS

- **Platform risk:** none — one additive read route, no migration, no new external call.
- **Usage data:** name-only event `OutlierHandlingSwitchedOff` (next integer in `UsageDataEventName`),
  emitted when a reader switches outlier handling off. It shows whether readers want to see every item,
  which is the reason the switch exists. Emitted by the slice that adds the switch (S1), which also lists
  it in `docs/settings/usagedata.md`. Accepted by the root in AFK mode; the maintainer can revisit it at the
  sketch review.

## DESIGN

Accepted design and ADR in the nwdes handoff (`architecture.md`). Summary: backend strategy
`ICycleTimeOutlierDefinition` with `UpperNaturalProcessLimitOutlierDefinition`, sharing an
`UpperNaturalProcessLimit` value object with the PBC; new `cycleTime/outliers` read route for Team and
Portfolio; the chart fetches the verdict with a keyed query; no-limits, loading and failure looks as in the
design.

## Sketch review (start of DELIVER) — pending

# ADR-121: The CLI/MCP delivery-trend surface summarises client-side; no backend projection parameter

- **Status**: Accepted (2026-07-31, DESIGN wave for ADO #5585 / Story #5619). Interaction mode = **propose**.
- **Date**: 2026-07-31
- **Feature**: `epic-size-and-count-over-time` (Epic 5585, slice 06) — repo `lighthouse-clients`
- **Extends**: ADR-050 (metrics-history endpoint shape)

## Context

The clients already carry deliveries — CRUD on `packages/client/src/index.ts:1310-1324`, the
`lh delivery list --portfolio-id` command, and the `lighthouse_delivery_list` MCP tool — but expose
nothing from `metrics-history`, so every delivery over-time chart is browser-only.

`DeliveriesController.GetMetricsHistory(int deliveryId)` takes **no parameters** and returns the entire
recorded series in one response: per day, the scalars plus a `whenDistribution` array (4 percentiles) plus
one `featureBreakdown` entry per epic. A 90-day delivery with 15 epics is on the order of 1350 breakdown
objects and 360 distribution points in a single payload — fine for a browser, hostile to an LLM tool
result.

## Decision

Add the surface **without touching the backend**, and shape the payload in the client:

- `packages/client`: `getDeliveryMetricsHistory(deliveryId)` over the existing route, returned through
  the standard `LighthouseApiResult` contract. Reference class: `getPortfolioBlockedCountHistory` /
  `BlockedCountSnapshot` — a typed read-only time series already carried end-to-end.
- `packages/cli`: `lh delivery metrics --delivery-id <id>` inside the existing `runDeliveryGroup`.
- `packages/mcp-core`: read-only tool `lighthouse_delivery_metrics`, classified by `isReadOnlyTool`.
- **Default projection is one summarised row per day**: date, totalWork, doneWork, remainingWork,
  epicCount (= `featureBreakdown.length`), estimatedItemCount, likelihoodPercentage. Per-epic rows and
  `whenDistribution` require an explicit opt-in (`--detail epics` / a `detail` argument).

Epic count is derived from the breakdown array's length, exactly as the chart derives it — one rule, two
ports, no second definition of "how many epics".

## Alternatives considered

- **Add `from`/`to`/`fields` parameters to the endpoint** — the technically cleaner answer, rejected *for
  now* on sequencing: it is a backend contract change that would have to ship, be released, and be
  version-gated in the client before the port could rely on it. Client-side summarising delivers the same
  user-visible outcome against today's server. If the measured summarised payload still blows the budget
  (feature KPI 6), the backend parameter comes first and this port waits — that is the slice's explicit
  learning hypothesis, not a fallback to improvise later.
- **Return the raw response and let the assistant cope** — rejected: an unusable blob is worse than no
  tool, and it would burn a caller's context on first use.
- **A second, summary-only endpoint** — rejected: two routes for one resource, and the summary is a
  presentation concern, not a domain one.

## Consequences

- No server-version gate is needed for the endpoint itself (it shipped with Epic 3993), but the client's
  types must treat `totalItems` / `isUsingDefaultSize` as optional so a server predating Story #5615
  parses cleanly and reports per-epic size as unknown.
- The port must ship **after** Story #5615, or the client is published against a payload shape about to
  widen — two npm releases for one feature.
- The clients release needs a changeset plus a **manual** `pnpm release:version` before the release gate.
- The summary rule now lives in two places (the chart derives `epicCount` in the frontend, the client
  derives it in TypeScript). That is duplicated *code*, not duplicated *knowledge* — but if a third port
  appears, the derivation belongs in the backend DTO instead.

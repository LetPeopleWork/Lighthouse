# Slice 06 — Delivery metrics history reaches the CLI and MCP

**Feature**: epic-size-and-count-over-time · **ADO**: Epic #5585 · **Story**: US-06 · **Estimate**: ~4h
**Repo**: `lighthouse-clients` (not the Lighthouse product repo)
**Reference class**: `getPortfolioBlockedCountHistory` + `lighthouse_portfolio_metrics_blocked` — a
read-only time-series already carried end-to-end through client → CLI → MCP.

## Goal

The delivery trend stops being browser-only: a coach can ask an assistant "how has this delivery's scope
moved?" and get the recorded series, or read it from a terminal.

## The gap, precisely

The clients already know deliveries — `packages/client/src/index.ts:1310-1324` has
`listDeliveries` / `createDelivery` / `updateDelivery` / `deleteDelivery`, the CLI has
`lh delivery list --portfolio-id` (`runDeliveryGroup`), and `lighthouse_delivery_list` is a registered
MCP tool (`mcp-core/src/index.ts:1549, 2201-2217`). `metrics-history` appears in **none** of them, so
every over-time delivery chart — burnup, predictability, fever, and this feature's new one — is
invisible outside the browser.

## IN scope

- `packages/client`: `getDeliveryMetricsHistory(deliveryId)` against
  `GET /api/v1/deliveries/{deliveryId}/metrics-history`, returning through the existing
  `LighthouseApiResult` contract; typed series with `totalItems` / `isUsingDefaultSize` **optional**
  (a server predating slice 02 must parse cleanly, AC-6.6).
- `packages/cli`: `lh delivery metrics --delivery-id <id>` inside the existing `runDeliveryGroup`,
  matching the usage-error style of `lh delivery list`.
- `packages/mcp-core`: read-only tool `lighthouse_delivery_metrics`, registered in the tool list and
  classified by `isReadOnlyTool`.
- **Default projection is summarised** (D13): one row per day — date, total, done, remaining, epic count,
  estimated portion, likelihood. Per-epic rows and `whenDistribution` only behind an explicit
  `--detail epics` / `detail` argument.
- Tests in all three packages, plus a changeset; `pnpm release:version` run manually before the release
  gate (AC-6.7 — the bump is NOT automated in this repo).
- One line each in the CLI README and `skill/SKILL.md` so the command and tool are discoverable.

## OUT of scope

- Any backend change. In particular **no** `from`/`to` or projection parameter on `metrics-history` —
  raised as a separate backend story only if the size measurement below fails.
- Delivery **write** tools in MCP (create/update/delete exist on the client; exposing them is a separate
  decision about write surface).
- Any change to the Lighthouse product repo.

## Measure before shipping (K6)

`GET .../metrics-history` takes **no parameters** (`DeliveriesController.GetMetricsHistory(int deliveryId)`)
and returns the entire recorded series: for a 90-day delivery with 15 epics that is ~1350 breakdown
objects plus a `whenDistribution` array per day. Measure the summarised tool result against the dogfood
delivery with the longest history; target ≤ ~2k tokens without the detail opt-in.

## Learning hypothesis

**Disproves** "the recorded series is usable through an assistant as-is" **if** even the summarised
projection blows the tool-result budget on a real delivery. Then the honest move is a backend range /
projection parameter **first**, and this slice waits — shipping a tool that returns an unusable blob is
worse than not shipping it.
**Confirms**, if it holds, that every future delivery-metrics series gets an assistant surface for free.

## Acceptance criteria

AC-6.1 … AC-6.7 verbatim from `feature-delta.md`. Load-bearing pair:

- `lh delivery metrics --delivery-id 12` prints one row per recorded day with the summary columns (AC-6.3).
- Against a server whose snapshots predate slice 02, the client parses and reports per-epic size as
  unknown rather than erroring (AC-6.6).

## Dependencies

**Slice 02 must have shipped.** The client is written once against the final payload shape — publishing
against the pre-slice-02 shape would mean two npm releases for one feature. Independent of slices 03-05.

## Dogfood moment

Same day: point the CLI at the dogfood server and run the command against the delivery with the longest
history; then ask an assistant, through the MCP tool, why that delivery's scope stepped — and check the
answer matches what the chart shows.

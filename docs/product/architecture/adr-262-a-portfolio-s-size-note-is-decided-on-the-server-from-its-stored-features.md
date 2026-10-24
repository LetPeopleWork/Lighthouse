# ADR-262: A Portfolio's size note is decided on the server, from its stored Features

**Status**: Proposed (DESIGN, 2026-10-23, PROPOSE mode; revised after the architecture review). The failure look is
listed for the maintainer (Q-M3).
**Feature**: `epic-6400-fast-with-many-features` (Story #6410, slice 06)

## Context

A Portfolio holding more than 100 Features gets a non-blocking note on its edit page and one log line per update
(D23). The threshold must be one constant for both (AC-6.4). The count is what the last update stored, not what the
query would bring in now; a Portfolio never updated holds none and shows no note. The edit page today reads
`GET /portfolios/{id}/settings`, a DTO that is also bound as the request body of create and update.

DISCUSS asked for no note while the count is unknown or failed. The project's rule for fetching views asks for a
failure look that never reads as "no data", and a silent failure reads exactly like "100 or fewer".

## Decision

1. **A domain value `PortfolioSize`** (count, the threshold of 100 as its one constant, and "over the threshold" as
   strictly more than 100) lives in `Models`.
2. **A small read**: `GET api/latest/portfolios/{portfolioId}/size` (and `api/v1`), guarded `PortfolioRead`, answers
   `{ featureCount, threshold, isOverThreshold }`. The count is one `COUNT` over the Portfolio's stored Features
   through an `IPortfolioRepository` method; nothing else is read.
3. **The edit page** fetches it with a keyed query; the rest of the page does not wait for it. Its looks:
   - loading: nothing;
   - failed: the same info note, one line, "Couldn't check this {Portfolio}'s size.";
   - over 100: "This {Portfolio} holds {N} {Features} (as of its last update). …" with the D23 advice;
   - at or under 100, or never updated: nothing.
   Terms render through Terminology; copy is provisional and settled at DISTILL's sketch.
4. **The log line** is written at **Warning** by `PortfolioUpdater` after a successful Features update, from the
   Feature count it already holds, through the same `PortfolioSize` (maintainer decision of 2026-10-24; first drafted
   at Information): "Portfolio '{name}' ({id}) holds {N} Features after its update, more than 100. Its pages and
   forecasts get slower; narrowing its query keeps them fast." Being a Warning, it also lands in the shared 200-entry
   Recent Problems list, one entry per such update; accepted by the maintainer, with no sink exclusion. It is not a
   Kestrel heartbeat warning, so the stall reading (K3) does not count it.

## Alternatives considered

- **Silent on failure** (DISCUSS's look): nothing can be misread as a size, but a failure is indistinguishable from a
  small Portfolio. Rejected provisionally; the maintainer may restore it (Q-M3).
- **A field on `PortfolioSettingDto`**: no extra request, but a computed output on a DTO the API also binds from the
  request body, and a body the clients read. Rejected.
- **Count on the client from `PortfolioDto.features`**: no backend change, but the threshold would live in TypeScript
  and C# as two constants. Rejected (AC-6.4).
- **A blocking validation on save**: the maintainer chose a nudge, never a block. Rejected.

## Consequences

**Positive**
- One rule, one constant, in the domain; the note can never block Save or an update.
- The reader can tell a failed check and a stale count from a small Portfolio.

**Negative / accepted**
- One more request on the edit page; it costs one `COUNT`.

## Enforcement

- Unit: `PortfolioSize` at 100 and 101 Features; the log line and the endpoint both read it.
- Component: no note while loading, at or under 100, or never updated; the failure line on failure; the "as of its
  last update" wording over 100.

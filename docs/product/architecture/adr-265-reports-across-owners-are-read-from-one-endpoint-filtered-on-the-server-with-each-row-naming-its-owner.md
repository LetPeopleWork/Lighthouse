# ADR-265: Reports across owners are read from one endpoint, filtered on the server, each row naming its owner

**Status**: Proposed (DESIGN, 2026-10-27, PROPOSE mode).
**Feature**: `epic-6350-bulk-reports` (Epic #6350, slices 01, 02a–04, 06)
**Relies on**: ADR-219 to ADR-222 (a report belongs to one owner; reads go through a port with no write member; owner
scoping in the application ports, authorisation in the controllers), ADR-247 (clients read reports raw).

## Context

Reports are read per owner only (`GET api/latest/{teams|portfolios}/{id}/reports`, guarded Read). A top-level Reports
page, the bulk-creation wizard and the clients need every report and every owner the caller may read, in one place.
The list row carries no owner name. The bulk wizard reaches its Metrics step before any owner is chosen, while the
single wizard reads its metrics from one owner's `templates` endpoint. `GET /teams` computes blackout and forecast
filter state per Team, which makes it too heavy to serve as an owner list.

## Decision

1. A new `ReportsController` at `api/{v1,latest}/reports`, with no owner in the route, serves `GET reports`: every
   report on a Team or Portfolio the caller may read. The controller asks `IRbacAdministrationService`
   (`GetReadableTeamIdsAsync`, `GetReadablePortfolioIdsAsync`) over every owner id, then `IReportQueries.ListAcross`
   reads those owners' reports in one database query. An unreadable owner's reports are absent, never a 403.
2. Each row is the per-owner list row plus `ownerName`. The name is optional on the record and written only when set,
   so every per-owner list body is unchanged.
3. `GET reports/creation-options` answers, without an owner: today, the minimum window length, each template with its
   metric keys per owner kind, and every readable owner with its kind, id, name and first kept day. The bulk wizard
   reads it once for its Type, Metrics and owner steps; the owner step's limits come from it.
4. Owner names and kept-history cutoffs come from the Reports module's owner lookup, as a projection (id, name,
   cutoff), never by loading the owners themselves.
5. The list computes no Now; it reads the frozen Then window only to say whether Then can still be rebuilt.

## Alternatives considered

- **Fan out over the per-owner endpoints from the browser and the clients.** No new endpoint, but one request per
  owner (a dozen or more on a slow connection, against a 2-second budget), no owner names without two more list
  reads, and every client would repeat the fan-out. Rejected.
- **Reuse `/teams` and `/portfolios` for names and owners, and one `templates` read per owner for limits.** No new
  read for names, but `GET /teams` does per-Team forecast work the page never shows, and slice 04 would send one
  request per owner. Rejected.
- **Filter by Admin on the server for the owner list.** Duplicates the Admin set the browser already holds through
  `useRbac()` and would split UI gating across two sources. Rejected; the server re-checks Admin on every write
  instead (ADR-266).

## Consequences

- Positive: one request per view; the server alone decides what may be read; the clients gain the same list for free.
- Positive: the per-owner contract and its tests stay byte-identical.
- Negative: a second controller answers for Reports; the module gains two read operations and an owner projection.
- Neutral, on purpose: the creation options list every readable owner, not only those the caller administers; their
  names are already readable elsewhere, and the browser narrows the list to Admin owners.
- Negative: an owner deleted between the creation-options read and a bulk run is only met inside the run (ADR-266
  answers it per owner).

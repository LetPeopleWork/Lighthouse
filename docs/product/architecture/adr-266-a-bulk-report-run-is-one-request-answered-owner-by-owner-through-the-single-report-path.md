# ADR-266: A bulk report run is one request answered owner by owner, through the single report's path

**Status**: Accepted, built on main 2026-10-31 (not yet released). Proposed (DESIGN, 2026-10-27, PROPOSE mode). Items 1, 6 and 7 LOCKED by the maintainer on 2026-10-27:
one request with owners processed one by one; N = 50 (dropping to 25 if a 50-report print fails in the Linux desktop
app); the run finishes when the browser stops listening. Revised after DEVOPS, 2026-10-27: items 5 (already-gone
marker) and 8 (log fields and slow lines).
**Feature**: `epic-6350-bulk-reports` (Epic #6350, slices 02a, 03, 05)
**Relies on**: ADR-222 (authorisation in the controllers, owner scoping in the ports), ADR-265 (the cross-owner
controller), the existing `LicenseGuard` and `ReportCapPolicy`.

## Context

A coach creates the same report for many Teams and Portfolios in one go, and removes many reports in one go. Every
owner (or report) must come back as done or not, with a reason, never an all-or-nothing over the set. Same windows
meet different kept histories; an owner may be deleted, Admin rights may be lost and the licence may lapse while a run
is under way. Bulk creation is Premium; bulk removal is Community. The Reports module's ports take an owner, never a
user. Creating one Then & Now takes about 0.2 to 0.6 seconds. A failed save leaves its row tracked in the request's
database context, where the next save would try it again.

## Decision

1. **One request per run.** `POST reports/bulk` carries one report configuration (the single create request's body)
   and the owners; `POST reports/bulk-removal` carries report ids. Each answers `200` with one outcome per owner or
   report, in the order asked: done (with the new report's id) or not, with a code from the closed refusal set.
2. **The same path as one report.** A new `ReportBulkCommands` in the Reports module goes owner by owner, sequentially,
   and calls `ReportCommands.Create` or `Delete`; nothing in the module writes reports another way.
3. **Admin is checked per owner when the run reaches it**, through an asynchronous capability port
   (`MayWriteAsync(owner)`, answering a `Task<bool>`) that the
   controller binds to the caller over `IRbacAdministrationService.CanWriteTeamAsync` / `CanWritePortfolioAsync`. With
   RBAC or sign-in off it allows every owner, as those checks do. The module never sees the user.
4. **Each owner stands alone**: a failed creation or removal (a database update failure, such as an owner deleted
   between lookup and save) is caught by the bulk loop, the failed pending change is discarded through a new writer
   member (part of the run's bounded change: it removes only that item's own pending row, never anything saved), a Warning is logged naming the exception type only (never its message or stack, which can carry names), and that owner is answered with `not-saved`; the next owner
   proceeds and the response is still `200`.
5. **Refusals**: the existing codes, plus `owner-not-found` (deleted before or during the run), `not-admin` and
   `not-saved`. The whole request is refused (400, nothing written) for an unknown owner kind, an owner list empty
   after de-duplication, more than N owners or ids after de-duplication, or a body the single create would refuse
   as malformed. An id on an owner the caller may not administer answers `not-admin`, so a caller can tell such an id
   exists; it learns nothing of the owner or the report, and report ids are sequential anyway. Accepted. A run
   from a Community instance is refused whole by `LicenseGuard(RequirePremium)`; a licence that lapses mid-run shows
   on owners already holding one as the existing cap refusal. For removal, the server finds each report's owner
   itself; a report already gone counts as removed and its outcome carries `code: "already-gone"`, so the browser
   never counts it as a deletion in usage data, as the single path never counts a 404. It has no owner left to check,
   so it skips the Admin check on purpose and nothing is written for it.
6. **A ceiling N = 50** bounds the owners of one creation run and the reports of one removal (400 above it); the browser
   says so before sending.
7. **A run finishes** even when the browser stops listening; the browser's "could not confirm, check Reports" stays
   true. There is no server-side deadline: N bounds the run, and a timed run on a real instance is the check that N
   is small enough.
8. One log line per run, `report-bulk-run` (Information): action, template key and window lengths (create), items
   asked, done, not done, refusals by code, elapsed ms; no names. Written as Warning `report-bulk-run-slow` above
   20 s (create) or 10 s (remove). The cross-owner list and creation options warn above 2 s (`report-list-slow`,
   `report-creation-options-slow`). Each report keeps its own slow-creation line.

## Alternatives considered

- **The browser calls the per-owner create once per owner** (a few at a time). Live progress and no new write
  endpoint, but the server could no longer refuse a Community run as a whole, and a dropped connection would leave
  only some rows unknown while others are confirmed. Rejected by the maintainer (2026-10-27).
- **Streamed progress** (server-sent events) over the one request. Live progress with the server's rules, but a second
  transport to keep working through proxies and the desktop app, for a wait of at most about 30 seconds. Rejected.
- **Bounded parallelism on the server.** Faster on Postgres, but SQLite writes one at a time, the metric captures
  compete with forecasts for the same cores, and each owner would need its own context anyway. Rejected for now;
  revisit if a measured run exceeds its budget.
- **A fresh dependency-injection scope (and database context) per owner.** Isolation by construction, but the module
  would resolve its own commands through a scope factory, which is service location, for what discarding one failed
  pending change already gives. Rejected.
- **One writable-owner set computed at the start of the run.** One permission read instead of N, but an Admin lost
  mid-run would still be written to. Rejected.
- **Domain events for the usage counts of a run.** Usage events are sent by the browser under its consent; a backend
  handler could neither honour it nor attribute the event. Rejected.

## Consequences

- Positive: bulk and single creation cannot drift apart; every rule the single path enforces holds per owner.
- Positive: one round trip on a slow connection; the server is the only judge of Premium and Admin.
- Negative: no live progress; a run of N owners waits for the slowest total before showing anything.
- Negative: a lost answer leaves the whole run unconfirmed (the Reports page then shows the truth).

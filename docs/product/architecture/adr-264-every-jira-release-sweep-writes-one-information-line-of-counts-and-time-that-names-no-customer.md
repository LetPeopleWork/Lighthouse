# ADR-264: Every Jira Release sweep writes one Information line of counts and time that names no customer

**Status**: Proposed (DESIGN, 2026-10-24, PROPOSE mode). Number taken while Epic 6400 runs in parallel; re-checked at
push and renamed if taken. Whether the refresh's sweep logs too is listed for the maintainer (OQ-3).
**Feature**: `story-6398-jira-release-loads-once` (ADO Story #6398, slice 03)
**Relies on**: the connector's injected `TimeProvider`; the existing sweep, its 5-minute cache and its "only a complete
sweep is kept" rule, all unchanged.

## Context

Listing Jira Releases takes 3 to 5 s on the reporter's Data Center instance. The sweep reads the project list, then
every page of versions of every visible project, 8 projects at a time; on Data Center the version route cannot filter
by status, so archived and released versions are paged in and dropped afterwards. The only log line is a Debug count
of projects. The maintainer will decide on caching or narrowing from a measurement (MD-9: a cold-cache median over
2 s on the reporter's connection opens a follow-up). The reading comes from a customer's log, so the line must not
carry the customer's host, project keys or names, or Release names.

## Decision

1. **One Information line per sweep**, written from the connector's guarded sweep entry point after the sweep ends or
   fails, so a cache hit (which returns before sweeping) writes nothing.
2. **Fields, closed and countable only**: purpose (`Picker` or `Refresh`), elapsed ms (from the injected
   `TimeProvider`, spanning client acquisition, kind detection, the project list and every version page), deployment
   (`Unknown`, `Cloud`, `DataCenter`), Releases kept, versions read (before the in-memory status filter), version pages,
   projects, projects skipped, and complete (bool). No URL, host, connection name, project key or name, Release name or
   id, or credential.
3. **The counts travel back with each project's read** and are summed after the parallel loop; no counter is shared
   between threads.
4. **The template lives in a small internal value type** beside the connector, so its wording can be mutation-tested
   whole (the connector itself is too large to mutate).
5. **The refresh's sweep writes the same line** with purpose `Refresh` (recommended; OQ-3).

## Alternatives considered

- **Time the options call in `DeliverySourcesController`**: easy, but mixes in the Portfolio read Epic 6400 owns and
  cannot see pages, versions or completeness. Rejected.
- **A metric (counter or histogram)**: better for dashboards, but a self-hosted instance has no metrics pipe the
  reporter can send; the log is what reaches the maintainer. Rejected for now.
- **One line per project**: shows which project is slow, but hundreds of lines per sweep and project keys in a
  customer's log. Rejected.
- **Picker sweeps only**: fewer lines, but the refresh's sweep reads every status and is the costlier of the two.
  Offered to the maintainer as OQ-3 (b).

## Consequences

**Positive**
- The operator can tell Jira's time from Lighthouse's, and the maintainer can apply the MD-9 gate from numbers.
- `VersionsRead` against `ReleasesKept` shows directly how much of the cost the Data Center status filter wastes, which
  is the question the follow-up has to answer (longer cache or narrower sweep).

**Negative / accepted**
- One more Information line per picker sweep and, with OQ-3 (a), per refresh of a Portfolio with bound Deliveries.
- "Names no customer" holds for this line only. The same log still carries the existing per-project Information lines
  (project keys) and the could-not-ask line (exception text that may contain a host); reviewing those is a separate
  follow-up.
- On the failure path the line carries only what was learned before the failure (the kind if detected, the project
  count if listed, zero otherwise, complete false); it never invents counts.
- The elapsed time includes Lighthouse's own JSON reading of the pages; at these sizes it is noise beside the HTTP
  time, and it is what the operator waits for.

## Enforcement

- A captured-logger test asserts the entry's structured property names equal the allowlist above and that no value
  equals the stubbed host, a project key or name, or a Release name from the fixture.
- Tests with a fake time provider pin the elapsed figure, one line per sweep, no line on a cache hit, and
  `complete: false` with the skipped count when a project refuses.
- The existing connector specifications stay green unchanged: what is read, returned and cached does not change.

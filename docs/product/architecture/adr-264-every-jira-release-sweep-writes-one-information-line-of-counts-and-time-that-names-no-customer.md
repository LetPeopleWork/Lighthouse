# ADR-264: Every Jira Release sweep writes one Information line of counts and time that names no customer

**Status**: Accepted (2026-10-24); built with Story #6398, not yet released. Proposed at DESIGN the same day (PROPOSE
mode). The refresh's sweep logs too, as do the preview's and the save's, each naming its purpose (OQ-3 answered (a)).
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

1. **One Information line per sweep**, written from the connector's guarded sweep entry point in a `finally` around
   its `try`/`catch`, so a cache hit (which returns before sweeping) writes nothing. The line is written on success, on
   the four exceptions the guard catches (`HttpRequestException`, `TaskCanceledException`, `JsonException`,
   `UriFormatException`) and on any exception that escapes the filter, which then still propagates. The `finally` reads
   the per-sweep tally of decision 3, so the line carries whatever was counted before the failure, never invented
   numbers. A test for an escaping exception is required, with exact values: one Data Center project whose second
   version page throws an exception outside the guard's filter, after a first page of 3 versions, writes one line with
   deployment `DataCenter`, 1 project, 1 page, 3 versions read, 0 kept, 0 skipped, complete `false`, and the exception
   still reaches the caller.
2. **Fields, closed and countable only**: purpose (`Picker`, `Preview`, `Save` or `Refresh`), elapsed ms (from the injected
   `TimeProvider`, spanning client acquisition, kind detection, the project list and every version page), deployment
   (`Unknown`, `Cloud`, `DataCenter`), Releases kept, versions read (before the in-memory status filter), version pages,
   projects, projects skipped, and complete (bool). No URL, host, connection name, project key or name, Release name or
   id, or credential.
3. **The counts accumulate in one small per-sweep tally**, created by the guarded entry point and passed into the sweep
   and each project's read, rather than travelling back in return values: the sweep returns only the Releases and
   whether it saw everything, so counts summed after the parallel loop would be lost when an exception escapes it.
   The deployment kind and the project count are written once, before the parallel loop, on one thread; pages and
   versions read are added as each page is parsed, and Releases kept and projects skipped as each project's read ends,
   each with an `Interlocked` add so concurrent project reads never lose a count. The `finally` turns the tally into
   the immutable value it logs.
4. **The template lives in a small internal value type** beside the connector, so its wording can be mutation-tested
   whole (the connector itself is too large to mutate).
5. **Every caller of the sweep names its purpose, explicitly.** The sweep has four callers: the picker's options call
   (`Picker`), the preview sent on each pick (`Preview`), creating or updating a Delivery bound to a source (`Save`),
   and the delivery-source refresh (`Refresh`). The options call sweeps inside the connector and passes `Picker` there.
   The other three reach the sweep through the resolver, so the purpose travels as a new public closed enum
   `DeliverySourceSweepPurpose { Picker, Preview, Save, Refresh }`, added as a required trailing parameter to
   `IDeliverySourceResolver.ResolveForPortfolio` and `IDeliverySourceProvider.ResolveMany` (and their single
   implementations, and the connector's private sweep entry point). `GetOptions`, every controller action and every
   HTTP contract are unchanged. Each caller passes a constant, and a test per purpose pins it.

## Alternatives considered

- **Time the options call in `DeliverySourcesController`**: easy, but mixes in the Portfolio read Epic 6400 owns and
  cannot see pages, versions or completeness. Rejected.
- **A metric (counter or histogram)**: better for dashboards, but a self-hosted instance has no metrics pipe the
  reporter can send; the log is what reaches the maintainer. Rejected for now.
- **Counts returned with each project's read and summed after the loop**: no shared state, but an exception that
  escapes the parallel loop drops every count, so the failure line would read all zeros. Rejected.
- **One line per project**: shows which project is slow, but hundreds of lines per sweep and project keys in a
  customer's log. Rejected.
- **Picker sweeps only**: fewer lines, but the refresh's sweep reads every status and is the costlier of the two.
  Offered to the maintainer as OQ-3 (b).
- **Two purposes (`Picker`, `Refresh`)**, as first designed: the preview and save sweeps would be logged as a refresh,
  and the per-pick cost would be invisible. Rejected.
- **An optional purpose parameter with a default**: hides a forgotten caller behind a wrong label, and Moq setups
  (expression trees) cannot omit it anyway, so no test call site is spared. Rejected.
- **An ambient scope set by each caller**: no signature change, but an implicit channel nobody sees at the call site.
  Rejected.

## Consequences

**Positive**
- The operator can tell Jira's time from Lighthouse's, and the maintainer can apply the MD-9 gate from numbers.
- `VersionsRead` against `ReleasesKept` shows directly how much of the cost the Data Center status filter wastes, which
  is the question the follow-up has to answer (longer cache or narrower sweep).

**Negative / accepted**
- **Volume.** Because the Release list starts on every opening of the dialog by a premium user (ADR-263, MD-27), a
  picker line is written for every opening that misses the 5-minute cache, also when the user stays on Manual; openings
  within the cache window write nothing. **Resolving never reads that cache**: every pick in the picker sends a preview
  that costs one full sweep and one `Preview` line, and every save of a source-bound Delivery costs one full sweep and
  one `Save` line. A refresh adds one line per refresh of a Portfolio with bound Deliveries (OQ-3 (a), decided). So a
  user who opens the dialog, picks three Releases in turn and saves writes one `Picker` line (if the cache missed), three
  `Preview` lines and one `Save` line. This Story only makes that cost visible; whether resolving should share the
  cache is decided with the cache follow-up. At Information level that is a handful of lines per user per hour on a busy instance, beside the
  refresh summary lines already written.
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
- One test per purpose through the connector (the options call writes `Picker`; resolving with `Preview`, `Save` and
  `Refresh` writes that purpose), and at each caller a verify on the mocked resolver that it passes its constant.
- The interface change is a compile error at any caller that does not name a purpose.
- The existing connector specifications stay green with no change but the added purpose argument: what is read,
  returned and cached does not change.

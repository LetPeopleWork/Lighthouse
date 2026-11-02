# ADR-NNN (draft; number taken at DELIVER, next free after 271): A caught database failure becomes a typed fault with a fixed sentence

- **Status**: Proposed (DESIGN, 2026-11-02, Bug 6500)
- **Deciders**: Benjamin Huser-Berta (maintainer), Morgan (Solution Architect)
- **Related**: ADR-185 (Recent problems sink; gets a dated note), ADR-126 (typed refusal channel for connectors, the
  model), ADR-184 (verdict ladder; where the connector cannot tell authentication from other failures it reports Unreachable rather than guessing), ADR-270 (Platform lane; glossary row
  "Database fault")

## Context

Failures of the tracker are modelled and worded (`WorkTrackingRefusedException` and friends). Failures of
Lighthouse's own store are not: a catch block can only pass the exception on, and the surfaces an administrator
reads (Task Manager, Settings alerts, an anonymous health text, an HTTP error body) show whatever they are handed.
A full Postgres volume reached a person as a whole `PostgresException`. The rule that was never written down:
**the text a person reads is chosen, never rendered from an exception.**

Constraints: the Recent problems sink is built before dependency injection exists (ADR-185), so whatever it calls
cannot be injected; Work Items, Licensing and the API must not learn about Npgsql or Microsoft.Data.Sqlite; the
shared kernel (ADR-270 decision 7) may not take a class that references a database driver; tracker text must stay
exactly as it is.

## Decision

1. **A pure function in the Platform lane**, namespace `Lighthouse.Backend.Services.Implementation.DatabaseFaults`:
   `DatabaseFaultClassifier.Classify(Exception?)` returns `DatabaseFault?` (a kind plus the store it came from), or
   `null` for "not a database fault". It holds no state, reads no configuration and logs nothing, so it is usable
   from the sink at builder time, from a controller, from a health check and from an exception handler alike.
2. **Detection is a table keyed by the driver's own codes** (Postgres SqlState; SQLite `SqliteErrorCode`,
   which is already the primary code, read with no masking), in two small provider detectors the classifier tries in turn. Provider
   types are named only inside those two detectors. Callers see `DatabaseFault` and never a driver type.
3. **Unwrapping** is a bounded walk over `InnerException` and `AggregateException.InnerExceptions`, not a list of
   named wrappers: EF's `DbUpdateException` and `RetryLimitExceededException` need no mention. The most specific
   node wins (a `PostgresException` or `SqliteException` beats a bare `NpgsqlException`, which beats a bare
   `DbException`).
4. **One sentence catalogue** (`FaultSentences`) maps (kind, store) to a fix-first core of at most 80 characters, plus
   optional explanatory tails and the "tries again" tail. The tail is added only when the caller says Lighthouse
   retries (the sink decides this from the event: it carries a refresh `UpdateType`).
5. **Not every provider error is a database fault.** Query and data errors (Postgres classes 22, 23 and 42 except `42501`, which is a missing grant and is `PermissionRefused`; SQLite 1,
   19, 20, 21, 25) mean the store is healthy and something Lighthouse or its data did is wrong. They classify as
   `null` and keep their authored log text. Only infrastructure faults are reworded.
6. **The sink rewords unconditionally** (N2): `RecentProblemsSink` replaces the message of any event whose attached
   exception classifies, and keeps the full exception in the log file. Call sites that know more (Settings) choose
   their own sentence, still from the catalogue.
7. **Typed exceptions for operator-facing text we author.** Where a thrower knows the sentence (a rejected backup
   file, a failed `pg_dump`), it throws one `DatabaseOperationException` whose `Message` is the `OperatorSentence`, with the
   log detail (tool stderr, entry name) in its own property. A catch site never inspects a message.
8. **The same sentence on HTTP**, through an `IExceptionHandler` for `/api` that answers ProblemDetails: exactly two
   sentences, the same for every caller: a database sentence ("Lighthouse cannot use its database right now. Please
   tell your administrator.") for a database fault and a generic one for anything else. The handler makes no role
   check, because the check reads the database and would fail during the outage it reports; the specific cause is
   in Recent problems and the log.

## Alternatives considered

- **An injected `IDatabaseFaultClassifier` port.** Rejected: the function is pure, the sink is built before DI, and a
  port would force a second construction path just to be mockable. A pure function needs no mock.
- **Put the classifier in the shared kernel (`Models`).** Rejected: it references driver types, and ADR-270 limits
  the kernel to ids, shared value objects and the event base type.
- **Classify by message text.** Rejected: localised, version-dependent and exactly the thing we refuse to render.
- **Reword only at call sites (no sink rule).** Rejected: 15 sites today and every future one would have to
  remember; the sink makes the leak non-representable for Recent problems.
- **Publish a `DatabaseFaultOccurred` domain event.** Rejected: no consumer; Q5 dropped the early warning that
  would have been one.
- **Ban `Exception.Message` with BannedApiAnalyzers.** Rejected as the main guard: about 50 uses (33 outside
  connectors), types that override `Message` slip past the base-symbol ban, and it cannot see `{Exception}`
  template arguments. A per-file ratchet test covers both (see the feature delta).

## Consequences

- Positive: one place knows what a Postgres or SQLite code means and one place knows what to tell a person; the
  Task Manager, Settings and HTTP surfaces agree word for word; tracker text is untouched by construction (no
  tracker exception type is a `DbException`).
- Negative: the sink now reads the exception on every Warning-or-worse event (a type check and a short chain
  walk); acceptable at tens of events a day. The mapping encodes vendor facts and must be re-verified against real
  instances when a driver major version changes; the real-instance tests are that probe.
- A new database provider adds one detector and catalogue rows; callers and the sink do not change.

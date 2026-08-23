# ADR-185: Recent problems is a bounded in-process Serilog sink, constructed at builder time and registered as a singleton

- **Status**: **Proposed** (DESIGN, 2026-08-23)
- **Date**: 2026-08-23
- **Feature**: epic-5511-task-manager (ADO Epic #5511, slice 06 / ADO #5843)
- **Deciders**: Benjamin Huser-Berta (maintainer), Morgan (Solution Architect)

## Context

Warnings and errors exist only inside a rolling text file. `SerilogLogConfiguration.GetLogs()` finds
the newest `*.txt` in the log folder and `ReadToEnd()`s the whole thing into a string;
`LogsController` hands that string to the client. There is no structured store, nothing to query by
level, and no in-memory retention of any kind. Noticing that something has been failing therefore
requires an administrator to decide to go and read a log — which is the step that does not happen.

Two mechanical constraints shape the answer.

The output format is not stable enough to parse. `LoggingConfigurator` switches between
`ConsoleTextTemplate` and `ConsoleJsonTemplate` depending on configuration, so a regex over the file
would have to handle both and would break on either changing.

The logger is built before dependency injection exists. `LoggingConfigurator.CreateLogger` is a static
method taking an `IConfiguration` and a `LoggingLevelSwitch` — the switch being an object constructed
outside it, in `SerilogLogConfiguration`'s constructor, and registered separately so that the rest of
the application can reach it. Anything the logger writes to has to follow that same route.

## Decision

**A bounded in-memory `ILogEventSink` retaining the most recent warning-and-above events as structured
records — timestamp, level, source context, rendered message, exception type — read through the
existing, already `SystemAdmin`-guarded `LogsController`.**

The sink instance is constructed at builder time, handed to `CreateLogger` beside the level switch, and
registered as a singleton behind a read-only port. This is the pattern the `LoggingLevelSwitch` already
establishes; the sink is a peer of the file sink, not a change to how the file sink is read.

The buffer is bounded and evicts oldest-first. Its size is not fixed here: the slice-06 pre-check runs
a real instance for a working day at `Warning` and counts what actually lands, because choosing a
number before that measurement is how the section becomes noise.

**The copy must state plainly that this holds recent events since the instance started.** It is
per-process, it does not survive a restart, and under multiple replicas each pod holds its own. An
administrator who reads a short list as a complete history draws a wrong conclusion from it.

## Consequences

**Positive.** Structured records, queryable by level without parsing anything. The existing runtime
level switch continues to govern what is captured, with no restart. The guard is inherited rather than
re-argued: `LogsController` was unguarded until 2026-08-06 and now carries `RbacGuard(SystemAdmin)`
with a comment explaining that the log is instance-wide and that after ADR-137 "authenticated" includes
every viewer who reaches the Jira frame. That reasoning covers this route unchanged.

**Negative.** Not durable and not aggregated across replicas. Both are acceptable for "what has gone
wrong lately" and both are stated in the UI rather than hidden. This is not an audit log and nothing in
the product may imply it is.

If the level filter turns out to surface routine noise rather than signal, the follow-up is a curated
set of named operational events rather than a severity threshold. That is the slice's stated hypothesis
firing, not a defect — and the pre-check is deliberately cheaper than a built UI to learn it from.

## Alternatives considered

**Parse the rolling text file on each read.** No new sink. Rejected: `GetLogs()` reads the entire
newest file into a string, so every popover open pays for the whole file, and the parse is brittle
against the two output templates the configurator already switches between.

**A persistent, queryable log store — a table, or an external sink.** Durable and aggregatable.
Rejected as out of proportion: it is a logging-infrastructure decision with operational consequences,
made in service of one popover section. The self-hosted single-container product would gain a table it
must now prune.

**Aggregate across replicas through Redis.** Complete in the multi-replica product. Rejected: it makes
a fleet concern out of a feature whose stated promise is "what has gone wrong on this instance
lately", and it would put log content — team names, work-tracking URLs, connector errors — into a store
that today holds only integers.

# Slice 06 — The warnings, without reading the log

**Epic** #5511 Task Manager · **Story** US-06 ·
**Job** `job-operator-read-the-warnings-without-reading-the-log`

## Goal

Recent warnings and errors are visible where the operator already looks, instead of only inside a log
file they have to decide to open.

## IN scope

- A bounded in-memory Serilog sink retaining the most recent events at `Warning` and above as structured
  records: time, level, source context, rendered message, exception type where present.
- A read route on the existing, already-`SystemAdmin`-guarded `LogsController`.
- A **Recent problems** section in the popover, with a link through to the full log viewer.

## OUT of scope

- A persistent or queryable log store (D10).
- Parsing the rolling text file. `GetLogs()` reads the whole newest `*.txt` into a string, and the
  configurator switches between two different output templates — parsing that is expensive and brittle.
- Curated operational events. If the level filter turns out to be noise, that is this slice's hypothesis
  firing, and curation is the follow-up, not a hedge built in advance.
- Aggregating across replicas. The buffer is per-process.

## Learning hypothesis

**Disproves that a level filter alone yields signal rather than noise.**

If it succeeds: `Warning` and above is genuinely the set an operator wants, and the section earns its
place in the popover.
If it fails — the interesting warnings are drowned by routine ones, so the section is a wall of text
nobody reads — then the answer is a named set of operational events rather than a severity threshold,
which is a different and more opinionated build.

## Acceptance criteria

See US-06 in `feature-delta.md` — AC-06.1 through AC-06.7. The two that carry the risk:

- **AC-06.5** — the copy must say plainly that this is recent events since the instance started, not a
  complete history. It is per-process and does not survive a restart. An operator who believes it is an
  audit log will draw a wrong conclusion from a short list.
- **AC-06.7** — verified with warnings produced by a real failing connector. Injected log lines prove
  the sink works and prove nothing about whether the result is readable, which is the hypothesis.

## Dependencies

Slice 02 — the popover. Nothing else.

## Effort

Half a day.

## Reference class

`ForecastService` log-noise cleanup (12+ lines to 1) and `epic-5687-faster-updates`'s one-line refresh
summary — both were about what an operator can actually read, and both are the reason the interesting
warnings may already be sparse enough for a level filter to work.

## Pre-slice SPIKE

None. But before building the UI, run a real instance for a working day at `Warning` and count what
lands in the buffer. If it is three entries, the section is right as designed. If it is three hundred,
the hypothesis has already failed and curation is the slice — cheaper to learn from a log tail than from
a built UI.

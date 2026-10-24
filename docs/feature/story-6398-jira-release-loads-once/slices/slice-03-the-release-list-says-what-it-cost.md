# Slice 03: The Jira Release list says what it cost (ADO #6398)

**Goal:** every Jira Release sweep made for the picker writes one Information log line with its duration and size,
so the operator can tell Jira's time from Lighthouse's, and the maintainer decides on the cache from a measurement.

## IN scope
- One Information line per sweep: Releases offered, projects, versions read, pages, duration in ms, deployment kind
  (Cloud, Data Center, unknown), complete or not and how many projects were skipped. No URL, credential, project or
  Release name.
- No line on a cache hit.
- The reading on the dev instance and, through the reporter, on their Data Center connection, noting which Epic 6400
  release the instance runs; the MD-9 gate's outcome recorded in `feature-delta.md`.
- The line is written in `JiraWorkTrackingConnector.cs` only; no file Epic 6400 changes is touched.

## OUT of scope
- Changing the cache lifetime, warming the cache, or narrowing the sweep: follow-up F2, if the gate says so.
- Making `GetById` cheaper: owned entirely by Epic 6400, running in parallel (MD-11).

## Learning hypothesis
- **Disproves** "the Data Center version paging is where the Release list's 3 to 5 s go" if the logged sweep is under
  1 s of a 3 to 5 s options call (the time is then `GetById`, which Epic 6400 slice 02 addresses).
- **Confirms** it if the sweep is most of the call and grows with versions read rather than with projects.

## Decision gate (MD-9, decided by the maintainer on 2026-10-24)
Sweep p50 with a cold cache on the reporter's connection > 2 s → follow-up Story choosing between a longer cache
lifetime and narrowing the sweep to the projects the Portfolio's query names. Otherwise close with no change.

## Acceptance criteria
US-03 AC-3.1 to AC-3.3 in `feature-delta.md`.

## Dependencies
None in code. AC-3.3 needs one reading from the reporter.

## Effort
About half a day. Reference class: one structured log line with a stopwatch, plus its captured-logger tests.

## Dogfood
On the dev instance, open Add Delivery on a Jira Portfolio twice within five minutes: one sweep line the first time,
none the second.

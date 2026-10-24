# Slice 03: The Jira Release list says what it cost (ADO #6398)

**Goal:** every Jira Release sweep, whatever called it, writes one Information log line with its purpose, duration and
size, so the operator can tell from Settings → System Info → Logs whether a slow Release picker is Jira's time or
Lighthouse's, and whom to tell.

## IN scope
- One Information line per sweep, for each `SweepPurpose` (Picker, Refresh, Preview, Save; widened by the DESIGN
  re-review F-3): the purpose, Releases offered, projects, versions read, pages, duration in ms, deployment kind (Cloud,
  Data Center, unknown), complete or not and, when incomplete, how many projects were skipped. No URL, credential,
  project or Release name.
- No line when a picker request is answered from the cache (no sweep runs).
- **The docs, written in this slice** (NF-13, F-07 of the DEVOPS review): `docs/settings/systeminfo.md` gains a
  `### Jira Release sweeps` subsection under `## Logs` with the sweep line as an example, what each figure and each
  purpose mean, where to read it (the built-in Log Viewer, with Search, or the downloaded log file), and that the line
  is written at **Information**, so it appears only with the log level at Information or lower (`## Log Level`).
- The reading on the dev instance. The reporter's Data Center reading after the release, noting which Epic 6400 release
  the instance runs, and the MD-9 gate's outcome, are DoD release checklist item 10.
- The line is written in `JiraWorkTrackingConnector.cs` only; no file Epic 6400 changes is touched.

## OUT of scope
- Changing the cache lifetime, warming the cache, or narrowing the sweep: follow-up F2, if the gate says so.
- Making `GetById` cheaper: owned entirely by Epic 6400, running in parallel (MD-11).

## Learning hypothesis
- **Disproves** "the Data Center version paging is where the Release list's 3 to 5 s go" if the logged sweep is under
  1 s of a 3 to 5 s options call (the time is then `GetById`, which Epic 6400 slice 02 addresses).
- **Confirms** it if the sweep is most of the call and grows with versions read rather than with projects.

## Decision gate (the maintainer's, MD-9, decided 2026-10-24; a release step, not the operator's decision)
Sweep p50 with a cold cache on the reporter's connection > 2 s → follow-up Story (F2) choosing between a longer cache
lifetime and narrowing the sweep to the projects the Portfolio's query names. Otherwise close with no change. The
operator's own decision is a different one: whether a slow picker is Jira's time or Lighthouse's, and whom to tell.

## Acceptance criteria
US-03 AC-3.1 and AC-3.2 in `feature-delta.md`. AC-3.3 moved to the Definition of Done as release checklist item 10
(F-L4): the reporter's post-release reading and the MD-9 gate are a release step, not a test of this slice.

## Job
`job-operator-read-an-update-log-that-says-something` (re-pointed in the re-review, NF-03: one legible Information line
read from the log, saying how long and how much). MD-25's value ruling holds against it.

## Dependencies
None in code. The release checklist item needs one reading from the reporter after the release.

## Effort
About half a day. Reference class: one structured log line with a stopwatch, plus its captured-logger tests.

## Dogfood
On the dev instance, open Add Delivery on a Jira Portfolio twice within five minutes: one sweep line the first time,
none the second.

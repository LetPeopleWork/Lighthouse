# Slice 00 (SPIKE) — Jira Release reality check

**Goal**: find out, against a real Jira Cloud instance, whether a Release can carry a Delivery on the
terms D1, D7 and D8 assume — before any of them is designed.

**Type**: timeboxed PROBE, 2 hours. Ships findings, not code. Any throwaway script is deleted.

## Why this exists

`quiet-jira-writeback` locked six decisions on assumed Jira behaviour, then a spike broke three of them
and deleted a whole slice. Three decisions here rest on the same kind of assumption, and the cheapest
place for them to be wrong is a probe.

## IN scope — the four questions

**Q1 — Confirm the shape of `fixVersions` on the items Lighthouse tracks.**
**Downgraded from a gate to a confirmation (user, 2026-08-22): setting the version on the Portfolio's
Features is a convention the customer owns, not something Lighthouse has to discover or work around.** A
Feature without one simply is not in the Release, the same way a Feature that fails a rule is not in a
rule-based Delivery. The handler matches at the level Lighthouse tracks and does not roll up from
children.
What still needs confirming, because it decides the matching code and not the design: the field's JSON
shape on a real issue — an array of objects, each with `id` and `name` — and that matching on the
version **id** works against it.
Evidence to produce: the `fixVersions` value from one real Feature, verbatim.

**Q2 — Can the Releases be enumerated with their dates, and on both deployments?**
`GET rest/api/3/project/{key}/versions` on Cloud. Confirm the field names actually returned
(`id`, `name`, `releaseDate`, `released`, `archived`, `startDate`?), whether `releaseDate` can be absent,
and what the Data Center path is (or that it differs) — recorded, not built for.
Evidence: one verbatim response body.

**Q2 partially answered already (2026-08-22, live read of the demo instance).** `GET
rest/api/3/project/{key}/versions` returns objects carrying `self`, `id`, `name`, `archived`,
`released`, `projectId` — and `releaseDate`, `userReleaseDate` and `overdue` **only when a date is
set**. Two of the three Releases had no date at all, which is what D11 exists for; `description` was
absent on all three, so slice 04 must create the field rather than append to it. `self` confirms
`/rest/api/3/version/{id}` as the PUT target D7 assumes. Jira computes `overdue` itself, and the one
dated Release was six months past — D5 is exercised by the very first Release anyone binds.
Still open on Q2: the Data Center path.

**Q3 — What permission does writing a Version need, and what happens without it?**
`PUT rest/api/3/version/{id}`. Establish the permission (expected: Administer Projects) and the failure
shape without it — 403 with a body, silent no-op, or partial write. This is the D7/D8 permission bar and
it decides whether slice 05 is a real slice or a log line.
Evidence: one successful write and one refused write, both bodies captured verbatim.

**Q4 — Is the Version description a usable surface, and does a delimiter survive a round trip?**
**Partly answered 2026-08-22**: `description` is a column on the Releases page, so it renders where the
reader already is. What remains is what the block has to survive, now that its content is fixed at four
elements over multiple lines (attribution, write date, 70/85/95, target likelihood):

- its size limit, and its markup — wiki markup, ADF, or plain text,
- whether newlines survive storage and render as newlines,
- **whether a delimiter survives a round trip through the Jira UI.** This is the one that decides the
  slice: if a human opens the Release, edits the description, and Jira rewrites or strips the markers,
  the next write cannot find its own previous block and starts appending. That failure turns the feature
  into description spam, and it is worth provoking on purpose rather than assuming.

Evidence: write a candidate block, screenshot how it renders both in the list column and on the Release,
then edit the description by hand in the UI, re-read it over the API, and check the markers are intact.

## OUT of scope

- Any production code, any migration, any endpoint. Probe scripts only.
- Jira Data Center. Record what is knowable; verify post-release, same posture as `quiet-jira-writeback`.
- Deciding the fallback if Q4 fails — that is a DESIGN call informed by this.

## Learning hypothesis

**Disproves D7/D8** — now the whole point of this probe — if Q3 shows the permission bar for writing a
Version is one most credentials will not clear, or Q4 shows the description is not a surface a human
reads. Either finding detaches slices 04-05 from the Epic; inbound still ships whole.
**Disproves the matching code, not the design,** if Q1 shows `fixVersions` is shaped differently from
the assumed array of `{id, name}`.
**Confirms** the Epic can proceed as sliced if all four answer as assumed.

## Acceptance criteria

- **AC-00.1** — All four questions have a written answer with verbatim evidence, in this file, under a
  `## SPIKE OUTCOME` heading with the date.
- **AC-00.2** — Every answer that contradicts a locked decision is called out by decision number, in a
  was/now table, exactly as SPIKE-03 did.
- **AC-00.3** — No production code is committed by this slice.

## Dependencies

`letpeoplework.atlassian.net` credentials with, ideally, two identities: one holding Administer Projects
and one without, so Q3 can measure both sides.

**Note on the Jira API key**: hand-exploring the Jira API from this machine has previously tripped rate
limits that surface as unrelated-looking backend test failures. Keep the probe's call volume low and do
not run it alongside a backend suite.

## Effort

2 hours, timeboxed — shorter than first scoped, because Q1 stopped being an open question. Q3 and Q4
are the ones worth the time; if they consume the box, stop and report, because they are what decides
whether slices 04-05 exist.

## Reference class

`quiet-jira-writeback` slice 03 (`slice-03-spike-jira-notification-suppression.md`) — same shape, same
instance, same "assumed Atlassian behaviour" risk. It ran over its box because it chased a Data Center
answer it could not get; this one explicitly does not.

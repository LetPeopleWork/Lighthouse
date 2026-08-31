# What a Feature is waiting on, read from the tracker — Epic 4365

Shipped 2026-08-18 → 2026-08-23 in five slices. Free on every instance, community edition included.
Released in v26.8.31.7.

The complaint behind it: every tracker Lighthouse reads already records dependencies — a Predecessor
link in Azure DevOps, an *is blocked by* link in Jira, a relation between two Linear Projects — and
Lighthouse read none of it. The Features list was the one place the whole delivery was laid out in
order, and also the one place a Feature's blockers were invisible.

The Epic was split part-way through: reading and showing dependencies stayed here, and forecasting
with them became [Epic 5792](2026-08-31-epic-5792-dependency-aware-forecasting.md). The split is why
this Epic ships free and 5792 ships premium.

## What shipped

| Slice | ADO | What it added |
| --- | --- | --- |
| 01 | 5782 | The Dependencies column, read from Azure DevOps relations during the normal refresh. |
| 02 | 5783 | What exactly a Feature waits on, and one warning icon per Feature for the waits Lighthouse cannot take at face value. |
| 03 | 5786 | Jira and Linear read their own links. |
| 04 | 5787 | The two settings a Portfolio owns — a named field to read instead of the tracker's link, and *Ignore Dependencies*. |
| 05 | 5787 | The named field on Jira, and a CSV naming its column on the connection. |

ADRs **157** (references stored on the Feature, graph derived on read) and **158** (one honour policy,
two eligibility layers).

## Reading dependencies costs the refresh nothing, and that holds by construction

The first slice's hypothesis was that reading relations would be affordable inside the existing sync.
It was confirmed, but the way it was confirmed is the part worth keeping: `GetParentReferencesFromRelationFields`
makes one `GetWorkItemsInChunks(…, WorkItemExpand.Relations, …)` call and reads the parent references
*and* the dependency references out of that single response, then resolves each reference against
Features already in hand.

There is no code path on which a dependency causes a request. So this is not a tuned result that a
later change could regress into an N+1 — there is nothing to regress. `GetFeaturesForProject_ReadingTheDependenciesCostsTheRefreshNoRequestOfItsOwn`
pins it.

## Lighthouse shows what the tracker says, and will not let you author a dependency

Decided up front and never revisited. The value of the column is that it is not a second opinion: if
the link is in the tracker it is on the row, and if it is not, no amount of clicking in Lighthouse can
put it there. That also keeps a whole class of question out of the product — no authoring means no
conflict resolution, no write-back, no ownership argument about which system is right.

The one place this bites is the free-text field override (slice 04/05). A tracker refuses to create a
dependency cycle — Azure DevOps returns TF201035, transitively — but a field naming arbitrary
identifiers has no such guard, and neither does Jira. **Our own cycle detection is the only thing
standing between a Portfolio and a circular wait** on those paths, which is why it warns rather than
silently dropping.

## Four warning reasons, and why the set stops there

A dependency that cannot be taken at face value produces one icon with the reasons in its tooltip: the
blocker sits in no shared Portfolio, the two wait on each other, the blocker has no measured delivery
to forecast from, or it waits on something below it in the order. The last is not a problem — it is
worth knowing before re-planning, and saying so is cheaper than making the reader work it out.

`NotLicensed` is deliberately **not** in that set here. It arrives with 5792, where a licence can
actually change the answer; adding it in this Epic would have advertised a purchase against a wait that
no licence would have honoured anyway.

## Mutation testing, and the tooling wall it kept hitting

| Slice | Backend | Frontend |
| --- | --- | --- |
| 01 | 81.46 % — every one of the 24 survivors is pre-existing code sharing a file | 100 % (14/14) |
| 02 | not run — see below | 100 % (52/52) |
| 03 | 83.87 % on this slice's lines (52/62) | N/A, untouched |

**Stryker.NET 4.16 ignores the `mutate` filter in this repository** — in the config file and on the
command line, both line-spans and whole-file globs. A span silently widens to the whole file, so
naming a 40-line change in a 1300-line connector mutates the connector. Slice 01 hit it from one side
and slice 03 re-confirmed it; `score_the_slice.py` exists because of it, intersecting the report's
per-mutant lines against `git diff -U0` to recover a per-slice number the tool will not produce.

The eight unkillable survivors on slice 03 are all equivalent mutants in JSON guards — `||` → `&&`
where `TryGetProperty` returning false leaves `default(JsonElement)` with `ValueKind == Undefined`, and
`?? string.Empty` tails the guard above already makes unreachable. Recorded rather than chased.

## Lessons

- **Verify the premise before the migration.** Every slice here has a *verify the premise first* step
  ahead of any schema change, and slice 01's paid for itself: the single-request read was established
  before anything was stored, so the storage shape was designed against a known cost rather than a
  hoped-for one.
- **A split Epic should split on what the user pays for.** Reading is free and forecasting is premium,
  and because the split fell on that line, neither half needed a licence check it would not otherwise
  have had.
- **Score the slice, not the file.** When the mutation tool cannot be told what changed, the number it
  prints is about the file's history, not the work. Writing the intersection script once was cheaper
  than arguing about an 81 % that was really 100 % of the new lines.

## Open at finalize

- ADO #4365 and its Stories are left **Resolved**, not Closed — closing follows the maintainer's own
  pre-close feedback pass.
- The tracker's cycle guard does not extend to the field override or to Jira; our detection is the only
  guard there, and it warns rather than refusing.

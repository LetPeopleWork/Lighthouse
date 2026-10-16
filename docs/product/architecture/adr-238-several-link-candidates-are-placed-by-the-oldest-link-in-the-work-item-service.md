# ADR-238: Several Link Candidates Are Placed by the Oldest Link, and the Decision Moves From the Connector to the Work Item Service

**Status**: Accepted for the product rule; the engineering choices are provisional (2026-10-15, Morgan,
DESIGN wave, interaction mode PROPOSE). The product rule (narrow, then oldest link, then the fallbacks) is
the maintainer's own decision of 2026-10-15. The maintainer was AFK for this wave, so the engineering choices
below are the recommended options. They stand provisional until the maintainer ratifies them, at the latest
at the start of DISTILL, as ADR-237 did. Two of them are flagged for that ratification: the placed line at
Warning level, and the one-time Portfolio-grain move described under Consequences. *Update (DISTILL,
2026-10-15):* the maintainer confirmed Warning level at the DISTILL sketch review
(`docs/feature/story-6301-only-features-as-parent-candidates/feature-delta.md`, "Wave: DISTILL / [REF]
Maintainer decisions: UI sketch review (2026-10-15)"). The one-time move stays open for the maintainer at
the hold, recorded under the maintainer's Q5 answer (an item corrects itself the next time it is re-read).
*Update (DELIVER, 2026-10-16):* the maintainer accepted every open hold item as built, the one-time
Portfolio-grain move included, so the engineering choices are Accepted too.

**Feature**: `story-6301-only-features-as-parent-candidates`, ADO User Story #6301, under Epic #6028.

**Supersedes in part**: from ADR-193:
- §4 ("Two candidates is a refusal, not a partial answer").
- §6, the sentence that puts the ambiguity warning in the connector.
- §6's log budget: "**one aggregated warning per refresh**", and the reasoning built on Epic #5687's budget
  of two Information lines per entity. The new budget is per fetch: at most one placed and one not-placed
  line, both aggregated. A Team refresh is one fetch, so at most two lines. A Portfolio refresh is two
  fetches (its Features and the parent sweep), so at most four. Lines are written only when something was
  ambiguous, so the ordinary refresh still writes none.
- §6's enforcement row "Log budget respected" (one warning per refresh, regardless of how many items were
  ambiguous), replaced by this ADR's per-fetch log-budget row.
- The level of the outcome line: ADR-193 had only a warning, for refusals. A placement is a success, which
  the update path would normally write at Information. This ADR writes the placed line at **Warning**, as
  the maintainer decided at the DISTILL sketch review, because a placement is a choice the admin is asked to
  check and the Task Manager, where the admin checks refresh warnings, lists warnings only.

ADR-193 §1-§3 and §5, and §6's count on the page, stand unchanged.

## Context

ADR-193 refused any item whose links of the configured type named two or more issues, on the reasoning that
a wrongly chosen parent looks exactly like correct data. It pre-registered the consequence of that
assumption failing: if ambiguity was common, DESIGN would come back for a narrowing rule.

ADR-193's gate was a measured share: 5% of fetched items ambiguous. It fired on evidence that is
qualitative and second-hand. Steve, relaying the customer, reported that "most items appear to ignore their
parentage". No measured ratio exists yet. The ratio is collected from the first full refresh after the
upgrade (the customer's refresh row or log), and it is what confirms or refutes the narrowing's value.

It failed on a customer's Jira Data Center instance, where one link type ("is caused by") carries both the
hierarchy and the cause relations to incidents. Most Work Items named a Feature *and* an incident, were
refused, and counted toward nothing. Their Features were sized by the Portfolio default: the very symptom
refusing was meant to prevent. The refusal also did not do what its log line claimed. `WorkItemBase.Update`
overwrites the stored parent with this reading's value, so an item that was placed yesterday lost its
Feature the day a second link appeared.

The maintainer fixed the rule (2026-10-15):
- At Team grain, with two or more candidates, keep only those that are a stored Feature (any Portfolio,
  matched by reference id; stored parent Features count too). A lone candidate is stored as-is.
- Among what remains, at both grains, the candidate whose current link has the oldest changelog date wins.
  A dated link beats an undated one. Among undated candidates, the earliest Feature created date wins, then
  the lowest key. Never the `issuelinks` array order.
- Portfolio grain applies the pick only, never the narrowing, because the level above is never a Portfolio
  Feature.
- Every item with two or more candidates is reported, as placed or not placed.

Two facts about the code decide *where* the rule can run:

1. **The connector cannot see stored Features.** It is a leaf input adapter that may not reach the
   WorkItems/Sync module, which the architecture test `WorkTrackingIntegration_IsALeafInputAdapter`
   enforces. That test allows Platform/Persistence, though, so on its own it would not stop a connector
   from reading `IFeatureRepository`. A second ArchUnitNET rule in `ModuleBoundariesArchUnitTest.cs`,
   `WorkTrackingConnectors_DoNotDependOnAnyRepository`,
   added with this story, forbids WorkTracking-Integration types from depending on the repository
   interfaces or the persistence module: `Data` (the database context), `Services.Interfaces.Repositories`
   (every repository interface, `IFeatureRepository` included) and `Services.Implementation.Repositories`.
   Other platform services, such as the OAuth token service, stay allowed. `NamedModules_AreNonEmpty`
   checks that the connector module it applies to is not empty, so it cannot pass by matching nothing. Together the two rules
   enforce the boundary. Reading an
   issue is static, and it receives only the owner and field references.
2. **`WorkItemService` is the only caller of the three record reads**, in six places (full and delta for
   Team, Portfolio and the parent sweep). It already holds the Feature repository, and it already counts the
   ambiguous records right after deduplication, before anything is synced.

The oldest-link date is not on the `issuelinks` payload. It can only come from the changelog, which every
search already expands (`expand=changelog`) and which only status changes were parsed from. Whether Jira
writes a dated `Link` entry for every link addition is unverified on Cloud until Spike 00, and on Data
Center until a customer instance is read.

## Decision

### 1. The connector carries the candidates; it does not decide among several

The Jira connector attaches to each record the link candidates of the configured type and direction,
each with the date its current link was added, as a transient value: not stored, and copied by the record's
copy constructor. It replaces the boolean that said only "there were several". With exactly one candidate
the connector writes it as today. With two or more it writes no link-based parent, which leaves Jira's own
`parent` field or nothing, as today.

The link dates are parsed once, in `IssueFactory`, from the changelog already fetched. Histories are ordered
by their `created` stamp rather than by array position. An addition records the date for the counterpart
key, and a removal clears it, so a removed and re-added link counts from its re-adding. A key with no entry
is undated.

No fixture in this repository pins the shape of a changelog `Link` item. The key is read from `to` / `from`
when the value has the shape of an issue key. Otherwise it is read from the one key-shaped token in
`toString` / `fromString`. That reads an identifier, never the renamable link phrase. Spike 00 captures a
real Cloud payload (a link added, removed and re-added) before slice 02. The payload settles which field
carries the key and becomes the parser's test fixture.

### 2. The pick is a pure method on the shared model

`ParentResolution` gains one pure method. Given a read-only snapshot of stored Features (reference id →
created date, or unknown) and whether to narrow, it returns a placement plan: placed or not, the chosen key,
the rule that decided, and the candidates set aside. It reads no repository, writes nothing and logs
nothing. Its total order (dated first, earliest link date, known created date first, earliest created date,
lowest key, ordinal) makes the answer independent of link order, which a property test pins.

A created date that is null or `DateTime.MinValue` counts as unknown, because the factory writes
`MinValue` for an empty date and would otherwise always win.

### 3. One placing step in `WorkItemService`, called from every fetch, before anything is saved

Each of the three resolvers (Team, Portfolio, parent sweep) hands the downloaded, deduplicated records of
whichever branch ran, full or delta, to one private placing step. There are three call points, and the six
fetch paths are each tested to pass through one of them. The step does nothing when no record has two or
more candidates. Otherwise it loads the stored-Feature snapshot once per fetch, through one projection
query beside the existing `IFeatureRepository.GetAllReferenceIds`. It applies each placed plan, counts
placed and not placed onto `SyncOutcome`, and writes at most one aggregated warning line per outcome per
fetch. The parent sweep goes through the same step with narrowing off, and is logged but not counted:
the existing rule that the sweep stays out of `SyncOutcome` is kept.

The two places a parent is written, `ParentReferenceId` and the override's Additional Field value that
shows the key back, move from a private connector helper into `ParentSourceSelector`. The connector (one
candidate) and the placing step (several) then write the pair the same way. The selector lives in
`Services.Implementation.Parents`, which both modules may depend on.

The connector's ambiguity warning is deleted, together with its untrue claim that the items "were left
where they were". The only place that knows the outcome now reports it.

### 4. Refresh History splits its figure

`RefreshLog` keeps `RecordsWhoseLinksNamedMoreThanOneParent` under its name, now meaning "not placed".
Every historical row was in fact not placed, so history stays true. It gains
`RecordsPlacedFromLinksNamingMoreThanOneParent`: one expand-only migration generated by
`Create-Migration.ps1`. The frontend shows each figure as the window's maximum, only above zero, in the
Team's Work Items term or the Portfolio's Features term.

## Alternatives Considered

**Hand a snapshot of stored Features down into the connector (DIVERGE Option C).** The same pure pick, run
where the links are read, with the warning left in place. Rejected: it changes the connector's read port, or
puts transient state on the owner, for every connector because of one tracker's need. It also turns a pure
reader of the tracker into a reader that is told what Lighthouse holds, which is a back door for more of
Lighthouse's state into adapters. It scored 4.13 against this option's 4.42 in DIVERGE. The margin is
concept count and port change, not outcome.

**Narrow by the counterpart's issue type inside the connector (Option B).** No storage access, and immune to
the timing gap. Rejected: it answers "is this a Feature *type*", not "is this a Feature Lighthouse holds",
which contradicts the maintainer's definition. The link entry carries no created date, so the fallback
collapses to lowest key. A Team's reading does not have the Portfolio types either.

**Persist every candidate and derive the parent on read (Option D), or let a Portfolio refresh re-resolve
(Option E).** Both buy back the timing gap the maintainer accepted. D changes every reader of the parent:
the Feature's child match, its Work Item list and the "Parent Reference ID" rule fields. E adds a second
writer of one column across update-queue lanes. Rejected as cost without asked-for value.

**A set-based SQL update over persisted candidates (Option F).** Rejected: it needs a candidate table and
SQL that is portable across SQLite and PostgreSQL. It reports how many rows changed, not which item went
where, so naming the pick needs a second read. A tracked entity can later overwrite what the statement
wrote.

**Inject a new placing collaborator instead of a private step.** Cleaner separation on paper. Rejected:
the pure parts (the pick and the line formatter) will be outside `WorkItemService`. A collaborator would
add a type and a DI registration whose only content is the thin shell: a repository read, a loop and two log
calls. A lesser, practical reason is that a `Program.cs` edit forces the full live-connector integration
suite onto the change.

**Keep refusing (ADR-193 §4 unchanged).** Rejected by the evidence: refusing produced the outcome it was
meant to prevent, and its own pre-registered gate sent the decision back.

## Consequences

**Positive**
- Items linked to one Feature plus incidents count toward that Feature on the same refresh, with no
  additional Jira request.
- The pick is reproducible: the same links and stored Features give the same parent on every refresh, full
  or delta, whatever order Jira lists the links in.
- The rule lives in the tracker-neutral kernel, so a future link-based source inherits it.
- The log says what actually happened, and names the pick, the rule and the candidates set aside.
- One additive column; no other schema change; no new dependency; no `Program.cs` edit.

**Negative, stated honestly**
- A record leaves the connector half-decided. It is safe only because `WorkItemService` is the sole caller of
  the record reads. The placing step's invariant test (placed + not placed = records with two or more
  candidates, in every fetch) is what keeps a future call site from skipping it.
- The timing gap is accepted. A Feature stored after a Team read the item is picked up only when the item is
  next read. At Portfolio grain, undated candidates above that are not stored yet fall to lowest key, and can
  move once to the earliest-created one after the sweep has stored them. That happens when the Feature is
  re-read after its stored candidates change (in practice the cheap refresh after the Feature changes),
  never through re-resolution after the sweep. Refreshes run Full, Delta, Delta and the parent reads lowest key, then earliest
  created, then holds. A Portfolio's second refresh is already the cheap one, unlike a Team's.
- A placed Work Item counts toward its Feature in whichever Portfolio holds that Feature. That is the existing
  by-key child match, unchanged.
- The link date is per counterpart key: the changelog's `Link` item names no link type in a structured
  field. Two links of different types to one key share a date, and removing one of them leaves the key
  undated. Both are rare, and both fall to the deterministic fallback.
- "Lowest key" is ordinal, so `PAY-100` sorts before `PAY-99`. It is deterministic, but it carries no
  meaning, which is why it is the last resort.
- The Data Center changelog shape is unverified. If it differs, the pick degrades to created date and key,
  still deterministic.

## Architectural Enforcement

| Rule | Mechanism |
|---|---|
| Connector stays a leaf adapter | Existing ArchUnitNET `WorkTrackingIntegration_IsALeafInputAdapter`, unchanged |
| Connector cannot read stored Features | New ArchUnitNET rule `WorkTrackingConnectors_DoNotDependOnAnyRepository` in `ModuleBoundariesArchUnitTest.cs`: WorkTracking-Integration types depend on neither the repository interfaces nor the persistence module (`Data`, `Services.Interfaces.Repositories`, `Services.Implementation.Repositories`); guarded by `NamedModules_AreNonEmpty` |
| The pick stays pure | ArchUnitNET, **added in DELIVER slice 01** (not written yet): `ParentResolution`, `ParentCandidate`, `ParentPlacement` depend on no `Services` or `Data` namespace, with a `NamedModules`-style guard that the named kernel types exist |
| No fetch skips placement | One call per resolver, a unit test per fetch path (six), and the placed + not-placed invariant |
| Link order never matters | Property test: any permutation of candidates gives the same placement |
| Lone candidates untouched | Acceptance test: zero changed parents for one-candidate items |
| No extra Jira request | Request-count test: two cheap (Delta) refreshes of the same Team, first with one link per Work Item, then with a second link added; the second costs no more requests |
| One projection query per fetch, none when nothing is ambiguous | Query-count test |
| Log budget | At most one placed and one not-placed line per fetch (a Portfolio refresh has two fetches, its Features and the sweep) |
| The changelog actually dates links | Spike 00 before the slice; then a live, read-only `JiraIntegration` dogfood assertion on the demo instance's *Causes* links |

## Cross-feature impact

- **ADR-193**: §4, and in §6 the warning placement, the one-warning-per-refresh log budget and its
  enforcement row, are superseded as above. §1-§3 and §5 are unchanged.
- **Bug #6203** (`docs/evolution/2026-10-09-fix-link-parent-direction.md`): direction reading is
  unchanged. Its open follow-up (a different link type whose phrase equals the resolved type's name) stays
  out of scope. With narrowing in place it matters only if it names a stored Feature.
- **Lighthouse-Clients**: none read the refresh log (verified in DISCUSS). The additive field needs no client
  change.

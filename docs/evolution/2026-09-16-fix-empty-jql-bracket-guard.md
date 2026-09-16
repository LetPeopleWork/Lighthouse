# An empty mapped collection deletes everything — Bug #5974

**Delivered** 2026-09-16 · Backend only · ADO Bug #5974 · found during Bug #5973's mutation run

## The bug as filed was the least of it

#5974 was filed as "nothing protects the guard that stops an empty JQL bracket pair" — a missing test on
`PrepareGenericQuery`'s `options.Any()` check, found as a surviving mutant during #5973. It was P3, and
its own description said the guard was correct and the defect was that nothing would notice if it
stopped being.

Investigation corrected it in both directions.

**The filed hazard is smaller than described.** `AND ()` is a JQL **syntax error**, not a query matching
nothing. Settled from Atlassian's published `JqlParser` grammar: no rule on the
`subClause → LPAREN orClause RPAREN` path has an empty alternative, so `()` cannot derive. The surviving
mutant therefore produces a loud 400, which since #5973 becomes `JiraQueryRejectedException` and
propagates **before** removal runs. The deletion the work item feared never happens. The code comment
warning of it was overstated and has been corrected.

**The real hazard was live, with the guard intact, and worse.** Measured: with the work item type list,
all three state lists and `DataRetrievalValue` empty, the Jira connector put an **empty `jql=`** on the
wire. Atlassian documents an empty JQL as returning either every issue or none, depending on an instance
setting Lighthouse cannot see. "None" reaches removal.

## Why "none" is catastrophic rather than merely wrong

```csharp
// WorkItemService.cs:212-222
var itemsRemovedThisCycle = storedWorkItems.FindAll(stored => !stillOnTheTracker.Contains(stored.ReferenceId));
foreach (var itemToRemove in itemsRemovedThisCycle)
{
    workItemRepository.Remove(itemToRemove.Id);
    logger.LogDebug("Removed Work Item {WorkItemId}", itemToRemove.ReferenceId);
}
```

Removal is a set difference against what the tracker returned, with **no floor**. An empty answer deletes
every stored record for that owner, and the only trace is a `LogDebug` line invisible at the default log
level. `ValidateTeamSettings` reported `IsValid=true` for the configuration that produces it.

**Three connectors reached that same unguarded subtraction by different routes.** Linear's team read
matched issues against an empty state list with a bare `Contains` and returned zero — while the
portfolio path 470 lines above in the same file already guarded the identical concept. ServiceNow's state
filter and CSV's row filter have the same shape and are recorded as still open.

## Reachability

Not through any screen: the create wizards and both settings screens gate empty collections, with tests.
Reachable through **the API directly** — `SettingsOwnerDtoBase` carries no `[Required]` or `[MinLength]`,
`DataAnnotations` is not imported, and there is no `ModelState.IsValid` anywhere under `API/` — and
through a **database restore**, which passes through no controller.

## What shipped

| Commit | |
|---|---|
| `af65c1839` | Linear's team read agrees with its portfolio path |
| `e2377b860` | Jira and ADO refuse a configuration that narrows nothing; RCA |
| `3548645b7` | ADO wraps the operator's query only when there is one |
| `0504b288c` | L1-L6: the state-mapping rule said once; no clause built to be discarded |
| `4c0430187` | The guard's boundary and its message pinned |

## Key decisions

**The guard refuses rather than substituting a fallback.** Jira and ADO fail *open* on an empty list —
the clause is dropped and the fetch widens — while Linear, ServiceNow and CSV fail *closed* and match
nothing. No single query-build behaviour is right for all five, so refusing is the only answer correct
everywhere. That argument is why a test pinning the original `options.Any()` guard alone would have
blessed the wrong layer.

**The cutoff does not count as narrowing.** ADO's all-empty degenerate still carried a cutoff clause, so
a guard phrased as "the assembled query is empty" — which is what the RCA proposed — would never have
fired for ADO at all. The rule is instead that no types, no states and no operator query means nothing
narrows the fetch; bounding how far back finished work is read does not stop a query asking for the whole
instance.

**Jira and ADO keep separate copies of two helpers.** The bodies are identical; the contracts are not.
Jira's operand may legitimately be empty and dropping the bracket pair at worst over-fetches. ADO's lands
immediately after `WHERE`, where an empty one is a syntax error with no over-fetching fallback. One shared
helper would carry one explanation, and it would be wrong for one of its two callers.

**Linear's validation verdict moved, and nobody asked for it.** `ValidateTeamSettings` shares the filter
predicate, so a team with no mapped states used to be told "No work items were found for this team
configuration" and now validates clean. That is consistent once such a team legitimately reads everything
— and the old verdict was, by accident, the only thing warning anyone that the same configuration would
empty their team on the next refresh.

## Lessons

**A verdict message reached a mutation run with its substance unasserted for the fourth time in one
session.** #5973, #6012, #6013 and now #5974. Each time the fix was "pin the phrases"; each time the next
message shipped unpinned. That is not four lapses of attention, it is a gap in what the normal
test-writing habit covers: a verdict message is user-facing output that arrives as a string argument to
an exception factory, which reads like plumbing, so tests assert the code and the field name and stop.
Here the existing assertion matched two phrases that **two different fragments each carried**, so emptying
either one left it satisfied. If this recurs on the next connector the answer is probably structural
rather than another reminder.

**The mutation run found something worse than the messages, and it was not on anyone's list.** Eight
survivors on the guard predicate itself meant nothing distinguished "all three empty" from "one of them
empty". A mutated `&&` would have refused ordinary valid configurations — a team with types mapped but
nothing else — taking working installations offline, with the entire suite staying green. An emptiable
message is embarrassing; an invertible guard that throws is an outage.

**Hand-applying a logical mutant needs parentheses.** Editing `a && b && c` to `a || b && c` gives
`a || (b && c)` because `&&` binds tighter — a different and much louder mutant than the one Stryker
reports. The first attempt at reproducing the survivor failed for exactly this reason.

**Three RCAs this session proposed a fix that implementation corrected.** The empty-query post-condition
as worded would not have fired for ADO; #6013's proposal named a constructor signature that does not
exist; #6012's cited a `using` that was dead. The pattern is worth naming: an RCA's analysis has been
reliable this session, and its *proposed code* has not — it is a design to verify, not code to apply.

## A review blocker that was overridden, and why

The independent review returned NEEDS_REVISION on one blocker: the guard now refuses an installation
already in the all-empty state, where previously the refresh proceeded, and the RCA's save-time
validation layer is not implemented. The finding was not accepted, for two reasons.

**Save-time validation would not have helped the case the finding describes.** It stops the state being
created. An installation already in it hits the guard on its next refresh either way. The proposed
remedy does not address its own scenario.

**"The refresh proceeded" understates what it did.** For an all-empty configuration the connector sent an
empty `jql=`, which Atlassian documents as returning every issue or none depending on an instance
setting — and "none" deletes every stored record for that owner. The change is from possible total data
loss to a refusal that names the missing fields, is shown on the settings screen, and reaches the
operator through Recent Problems. Failing every cycle is the intended outcome, not a regression.

The review was right that the RCA prescribes the save-time layer and that it is absent; that is recorded
below rather than treated as a blocker.

## Still open

- **The floor is still missing.** `RemoveItemsThatLeftTheQuery` will still delete every record for an
  owner when a fetch answers empty, and still says so only at `LogDebug`. Every bug in this family has
  been a different route into that one unguarded subtraction; the routes are closing one at a time and
  the thing that makes them catastrophic is untouched. A warning when a cycle removes *every* stored
  record for an owner would have made all four visible in a log an operator reads.
- **ServiceNow's state filter and CSV's row filter** have Linear's shape and were not changed.
- **Save-time validation.** `SettingsOwnerDtoBase` still accepts empty collections from the API. The
  guard is at query assembly because that also covers the restore path, but refusing the configuration
  at save time is the layer that would stop it existing at all. Adding it needs care: validate the
  incoming DTO, never the stored row, or an otherwise-valid `PUT` gets refused over a pre-existing empty
  — and `IsWorkItemTypesRequired` is already false for Linear team, Linear portfolio and ServiceNow
  portfolio, so an unconditional rule breaks three owners.
- **An empty-collection contract across `IWorkTrackingConnector`**, so a connector cannot answer "no
  opinion" with "nothing" by accident.
- **`WithoutTheLeadingConjunction`'s false branch is now dead code in ADO**, given the guard. Left in
  place deliberately: deleting it would make the method depend silently on a precondition established two
  methods away. It is the one mutant left alive, and it is equivalent.
- **`PrepareCutoffDateFilter` carries `// Bug #5567 decision 4:`** in both connectors. The bug number
  resolves; "decision 4" points into a document that gets archived, which `CLAUDE.md` bans. Outside this
  diff, so left rather than churned during a behaviour-preserving pass.

## Artifacts

- `docs/feature/fix-empty-jql-bracket-guard/rca.md` — the grammar derivation, the measured wire formats,
  the reachability trace and the layer argument
- `docs/feature/fix-empty-jql-bracket-guard/deliver/roadmap.json`
- `docs/feature/fix-empty-jql-bracket-guard/mutation/stryker.5974.backend.json`

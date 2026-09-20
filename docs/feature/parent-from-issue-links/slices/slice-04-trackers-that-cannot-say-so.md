# Slice 04 — The trackers that cannot honour this say so

**Feature**: parent-from-issue-links · **ADO**: #6032 · **Story**: US-04 · **Estimate**: ~3h
**Reference class**: `DependencySourceSelector`'s header comment — the one place in the codebase that
states this failure mode outright, having already been bitten by it: a rule written inside a tracker "was
told to one of three, and the other two accepted the setting and ignored it, which reads from the outside
exactly like a field everyone left empty."

## RESCOPED 2026-09-20 — this slice is its docs, and nothing else

Read this before the rest of the brief, which is preserved as written and is wrong in its first half.

The hypothesis below was **not disproven by a test going red**. It was disproven by reading the column:
`ParentOverrideAdditionalFieldDefinitionId` is a foreign key to an `AdditionalFieldDefinition` row, not a
free-text reference. A ServiceNow, Linear or CSV connection therefore cannot hold "an Additional Field
naming a Jira link type" in any sense this feature gives those words — it can hold a field definition
whose name happens to be a string that does not resolve, which is the generic additional-field validation
question and has nothing to do with reading parents from links. **AC-4.1 is retired.** The full reasoning
is in `../feature-delta.md` under *DELIVER / [WHY] AC-4.1 retired*.

The port capability that DESIGN added (DDD-1, ADR-193) went with it: the link-type lookup is private to
`JiraWorkTrackingConnector` and nothing outside that file asks for it, so declaring the capability on
`IWorkTrackingConnector` would have added a member with no caller.

What remains is AC-4.2 and AC-4.3 — one docs step — and they are worth doing on their own merits.

## Goal

The docs say which trackers honour the Parent Override Field at all, and how to name a link type on the
one tracker that can read one.

## IN scope

- ~~Per-connector assertion that an Additional Field naming a Jira link type fails validation on Azure
  DevOps, ServiceNow, Linear and CSV, naming the reference (AC-4.1).~~ Retired, see above.
- `docs/teams/edit.md` and `docs/portfolios/edit.md` state which trackers honour the Parent Override Field
  (AC-4.2) and how to use a link type where one is honoured (AC-4.3), with a worked example of each
  direction, in the instance's configurable terms.

## OUT of scope

- Teaching any other connector to read link types (D6). Azure DevOps is the obvious second and is
  deliberately not here.
- Fixing Linear's and CSV's non-support of the Parent Override Field. That gap predates this feature
  (`Linear/…:361,:438`, `Csv/…:239` set `ParentReferenceId` without ever consulting the override) and
  fixing it is a different piece of work with its own value case. It is documented, not patched — the
  honest thing, and the docs line is what stops the next person rediscovering it from an empty Feature
  list.

## Learning hypothesis — SETTLED, and not the way either branch expected

Neither branch was right, because both assumed the question was well-formed. It was not: there is no
link-type reference for a non-Jira connection to refuse or accept. Recorded below as written.

**Disproves, if it fails**: that the other four connectors already refuse a link-type reference, so this
slice only has to hold the behaviour still rather than build it.

The reasoning is that each resolves Additional Field references against its own field list, and none of
those lists contains a Jira link type. If any connector turns out to accept an unresolved reference
instead — storing an empty value and carrying on — then this slice is not an assertion, it is a fix, and
it is bigger than three hours.

**Confirms, if it succeeds**: the setting on the shared `IWorkItemQueryOwner` is safe to leave shared. The
tests are what keep it safe the next time someone touches field resolution, which is the actual
deliverable here — the behaviour is already right, it is just undefended.

## Acceptance criteria

AC-4.1 through AC-4.3 in `feature-delta.md`.

## Dependencies

Slice 01 — the link-type lookup has to exist before "every connector except Jira refuses it" is a
statement about anything.

## Effort

~3h as planned, most of it the docs and their examples. After the rescope, the docs *are* the slice.

## Which trackers honour the Parent Override Field

Verified in the code on 2026-09-20 rather than assumed, and the substance of AC-4.2:

| Tracker | Honours it | Where |
|---|---|---|
| Jira | Yes — a field, and after this feature a link type | `ParentSourceSelector.TheParentOf` |
| Azure DevOps | Yes — override first, the tracker's own relation as fallback | `AzureDevOpsWorkTrackingConnector:1241` |
| Linear | No — the project or initiative id is assigned outright | `LinearWorkTrackingConnector:361`, `:438` |
| CSV | No — the parent comes from its own column | `CsvWorkTrackingConnector:239` |
| ServiceNow | No — no portfolio parenting at all | `ServiceNowWorkTrackingConnector:31` |

## The gap this slice names and does not close

ServiceNow, Linear and CSV never read `connection.AdditionalFieldDefinitions` during validation, so any
unresolvable field reference is accepted and silently ignored on those three. Pre-existing, unrelated to
reading parents from links, and left alone deliberately — it is a fix with its own value case, not a
side-effect of this Epic.

## Pre-slice SPIKE

No. The hypothesis is settled by writing the four tests; if one goes red, that is the answer and the slice
is re-estimated on the spot.

## Dogfood moment

Same day: the docs pages are the artifact. Read them as someone with a mixed estate and check they answer
"can I use this on my Azure DevOps connection?" without opening the code.

# Slice 04 — The trackers that cannot honour this say so

**Feature**: parent-from-issue-links · **ADO**: #6032 · **Story**: US-04 · **Estimate**: ~3h
**Reference class**: `DependencySourceSelector`'s header comment — the one place in the codebase that
states this failure mode outright, having already been bitten by it: a rule written inside a tracker "was
told to one of three, and the other two accepted the setting and ignored it, which reads from the outside
exactly like a field everyone left empty."

## Goal

A link-type reference is refused by every connector that cannot read one, and the docs say which trackers
honour the Parent Override Field at all.

## IN scope

- Per-connector assertion that an Additional Field naming a Jira link type fails validation on Azure
  DevOps, ServiceNow, Linear and CSV, naming the reference (AC-4.1).
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

## Learning hypothesis

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

~3h, most of it the docs and their examples.

## Pre-slice SPIKE

No. The hypothesis is settled by writing the four tests; if one goes red, that is the answer and the slice
is re-estimated on the spot.

## Dogfood moment

Same day: the docs pages are the artifact. Read them as someone with a mixed estate and check they answer
"can I use this on my Azure DevOps connection?" without opening the code.

# ADR-205: The lane holder on a queued task row is a piece of work, not a name

- **Status**: **Proposed** (DESIGN, 2026-09-21)
- **Date**: 2026-09-21
- **Feature**: story-6055-activity-names-the-work (ADO User Story #6055, slice 02)
- **Deciders**: Benjamin Huser-Berta (maintainer), Morgan (Solution Architect)
- **Supersedes nothing.** Refines the read path ADR-181 established.

## Context

`GET /api/latest/update/tasks` answers one object per admitted update. A queued row carries
`waitingBehind`, a `string?` holding the display name of whatever is running
(`UpdateController.cs:61-62,70`).

A name alone cannot answer the question the field exists to answer, because two different pieces of
work can share one entity. `PortfolioUpdater` ends every run by triggering a forecast of the same
portfolio (`PortfolioUpdater.cs:123`), so `Features` for portfolio 3 and `Forecasts` for portfolio 3
are both admitted, both resolve through `UpdateTaskNaming.NameOf` to the same string, and the queued
one is told it is waiting behind a name that is its own. That is #6055 as reported.

Three further facts constrain the fix.

The words differ by case and the words are not the backend's. A different entity reads *Queued behind
Ocean Explorer*; the row's own entity has to read *Queued behind its own refresh*, and *refresh* /
*forecast* / *removal* are three different words for three different update types. The entity nouns in
the same sentence are tenant-configurable Terminology. So the phrase is assembled in the browser, and
whatever the backend sends has to be a fact rather than a sentence.

The comparison is not the browser's either. The backend already selects the lane holder and already
resolves its name; it is the only place holding both sides of "is this the row's own entity". A second
comparison in the browser is the failure `ActivitySection.tsx:48-55` already warns about in its own
comment — two places deciding one thing, disagreeing under a partial re-read.

The blast radius is small and entirely internal. `waitingBehind` has one producer and one consumer:
six files, eighteen references, all in this repository (grep, 2026-09-21). No CLI, no MCP client and
no E2E spec reads it.

## Decision

**`waitingBehind` carries the lane holder as a described piece of work — its name, its update type,
and whether it is this row's own entity — instead of a bare name. The backend decides sameness; the
browser chooses the words.**

```csharp
public sealed record WaitingBehindResponse(string Name, UpdateType UpdateType, bool IsSameEntity);
```

`UpdateTaskResponse.WaitingBehind` changes from `string?` to `WaitingBehindResponse?`. Every other
field keeps its name, type and meaning.

`IsSameEntity` compares the *entity* the two pieces of work are about, not their ids. `Team` 3 queued
while `Features` 3 runs is not a self-reference — those are two different entities that happen to share
an integer. The comparison is (entity kind, id), where entity kind maps `Team`/`TeamDelete` to the team
and everything else to the portfolio, exactly as `UpdateTaskNaming.NameOf` already does when choosing a
repository.

The alternative shapes were weighed and rejected:

**Two nullable sibling fields** — keep `WaitingBehind` as a name and add a second field set only in the
self case. Nothing existing breaks, which is its whole appeal. Rejected because the two fields are
never both set: a discriminated union written as two nullables, whose invariant survives only as long
as the comment next to it, and which a third case would break silently.

**No contract change** — send `null` when the holder is the row's own entity, so the row reads a bare
*Queued*. The smallest possible change, and it does remove the reported confusion. Rejected because it
answers "what are you waiting for?" with silence in the one case where the answer is short and
reassuring: the thing above this row, about to finish.

## Consequences

**Positive.** The field says what it is for. One decider, on the read path that already holds both
sides. The browser keeps every word, so a tenant who renamed Portfolio still reads their own noun. No
new query — `NameOf` already loads the holder. Adding a sixth update type forces a decision in the
entity-kind map rather than joining a `default:` arm unnoticed, which is the defect class #6055 came
from.

**Negative.** A shipped response field changes type. Six files move together and a stale client would
read `waitingBehind` as an object where it expected a string — acceptable here only because the grep
showing no external consumer is part of this decision and not an assumption. A future consumer outside
this repository would make the same change expensive.

**Neutral.** `IsSameEntity` is computed per queued row against one selected holder, so the cost is a
comparison, not a lookup.

## Relationship to #5877

#5877's per-type lanes shipped on 2026-09-19 and were reverted the same day (`f216ef558`) because
concurrent refreshes destroy Feature ownership. Its own fix to this field (`53aa75a1b`) resolved the
holder *per lane* and needed no contract change; it was backed out with the rest, and the story is
shelved as of 2026-09-21.

That work is orthogonal to this one. #5877 asked *which* running thing holds your lane; this asks
*whether* the holder is you. If the lanes ever return, `WhatItIsWaitingFor` selects the holder per lane
and this record describes whichever holder it selected — the two compose rather than compete. Nothing
in this ADR re-lands any of #5877, and nothing in it depends on lanes existing.

# Upstream changes from DESIGN — story-6307-refresh-on-additional-fields-change

Morgan, DESIGN 2026-10-15. For the product owner / maintainer to accept before DISTILL writes scenarios. Proposed,
pending maintainer approval.

## UC-1: AC-1.5 and its scenario promise something the update queue does not do

**Original** (`feature-delta.md`, US-01):

> - [ ] **AC-1.5** An owner already queued is not queued twice; an owner updating at the moment of Save gets exactly one follow-up (D9).

> Scenario: A Team already updating is updated once more, not twice
>   Given Team Atlas's update is running and Team Cobalt is waiting in the queue
>   When Priya adds a field to "Nordwind Jira DC" and saves
>   Then Atlas is updated exactly once more after its running update finishes
>   And Cobalt is updated exactly once

**Why it changes.** `UpdateQueueService.EnqueueUpdate` (`:157-166`) parks one follow-up for any key already admitted,
whether it is running or still waiting. So Cobalt, waiting at Save, runs twice: its waiting run, which starts after
the save and therefore downloads everything, and one follow-up, which finds its fingerprint already current and is an
ordinary update (incremental where the connector supports it). It keeps a single Task Manager row throughout. Making
the queue drop the follow-up for a waiting key was considered and rejected (DDD-10): it changes every trigger source
and reverses a rule in `docs/ci-learnings.md`, to save one cheap run. The guardrail D9 exists for, one full download
per owner per save (O5), still holds.

**Proposed replacement.**

> - [ ] **AC-1.5** No owner downloads everything twice because of one save, and no owner gets a second Task Manager row: an owner updating at the moment of Save gets exactly one follow-up after it finishes; an owner waiting in the queue keeps its one row (D9).

> Scenario: A Team already updating or waiting is not fully downloaded twice
>   Given Team Atlas's update is running and Team Cobalt is waiting in the queue
>   When Priya adds a field to "Nordwind Jira DC" and saves
>   Then Atlas is updated exactly once more after its running update finishes
>   And Cobalt's waiting update downloads everything
>   And neither Atlas nor Cobalt downloads everything twice because of the save
>   And each has one row in the Task Manager

`OUT-6307-one-fetch-per-owner` keeps its wording ("exactly one [full fetch] per affected owner per save").

## No other story or AC changes

D11 is confirmed as proposed (DDD-5). Q4 needs no extra fetch (DDD-6/7), so DoD item 5 is satisfied by construction.

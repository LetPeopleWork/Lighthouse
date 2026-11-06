# Slice 05 — Levels shown in Organisation

> **Amended 2026-11-06.** Item types moved to the connection's mapping. The last section of `../design/upstream-changes.md` wins over this file; apply it at DISTILL.

Epic #6474 · **visible** · `job_id: job-config-admin-describe-our-levels-once` · ≤ 1 day ·
depends on: 02, **#6473's Organisation shell with Terminology in it** (M5: waits for it; invisible slices go first)

## Elevator Pitch

- **Before**: Priya cannot see Lighthouse's structure anywhere; "Team" and "Portfolio" are two of twenty words
  in a Terminology list.
- **After**: Organisation → Structure & terms opens with a Levels box: two rows, each with its unit, its item and
  its item types, the leaf marked "generates throughput".
- **Decision enabled**: Priya can check that Lighthouse holds the structure her organisation uses, and see where
  levels above will go.

## Problem

Priya Raman, System Admin of a Jira Cloud install with 12 Teams and 2 Portfolios, explains Lighthouse to a new
colleague by opening a Team and a Portfolio side by side. Nothing shows that Teams' Work Items roll up into
Portfolios' Features, or which tracker types each holds.

## Learning hypothesis

A read-only levels view is enough for admins to understand the structure. **Disproved if** the walk-through
testers still ask "where do I add a level?" more than "what is this?" (then copy, not features, changes).

## IN

- The Levels box (sketch in `feature-delta.md`), above the existing Terminology list, read-only, with "Your
  organisation has two levels. Levels above come in a later release."
- `GET /api/latest/levels` (public, read for any signed-in user).
- Loading look (dimmed, small spinner) and failure look ("Could not load the levels. Try again").

## OUT

- Editing (06, 08). Adding levels (#6455). Moving anything else into Organisation (#6473).

## Domain examples

1. Priya (Premium) sees: 2 · Initiative · Initiatives · Feature · Features · Epic, Change; 1 · Squad · Squads ·
   Work Item · Work Items · Story, Bug, Task, User Story.
2. Ana Lima, free tier, fresh install: Team / Work Item with User Story, Bug; Portfolio / Feature with Epic.
3. Slow connection: the box stays dimmed under a spinner; on a server error, the failure look, nothing else.

## UAT scenarios

### Scenario: The admin sees the organisation's two levels in its own words

Given Priya renamed Team to "Squad"
When she opens Organisation, Structure & terms
Then she sees level 1 "Squad" with Work Item and its four types, and level 2 with its unit, item and types
And level 1 is marked as the one that generates throughput

### Scenario: The levels are not shown before they arrive

Given a slow connection
When Priya opens Structure & terms
Then the Levels box shows a loading look and no names

### Scenario: A failed load says so

Given the levels cannot be loaded
When Priya opens Structure & terms
Then she reads "Could not load the levels" with a way to try again

## Acceptance criteria

- [ ] Two rows, names from the levels, types listed, leaf marked.
- [ ] Loading and failure looks as sketched; never seeded words shown as loaded.
- [ ] `GET /levels` returns both levels for any signed-in user.

## Outcome KPI

OUT-3 (walk-through).

## Technical notes

- Reuse the Terminology table look; keyed TanStack query.
- Docs: `docs/settings/configuration.md` gains a Levels section and screenshot per theme (at finalisation).

# Story 6538 — Test-writing guidance that the DISTILL and DELIVER agents follow

**Finished:** 2026-11-08 · ADO User Story #6538 · Epic #6506 "Fast, reliable test suites"

## The problem

What the earlier Stories of this work learned about fast, reliable tests sat in feature workspaces, review notes and a
ledger bullet. An agent writing a new test read none of it, so the next feature could rebuild the slow shapes the suites
had just been rid of.

## What changed

- **One home: `.claude/skills/test-writing/`.** `SKILL.md` holds the layers, the rule that a check which cannot fail is
  no check, the checklist and the short fast-cycle rule. `references/frontend.md` and `references/backend.md` hold each
  stack's rules with one example each and the guards' own messages. Each file ends with an "End of" line that a reader
  quotes back, so a cut-off read shows.
- **`CLAUDE.md` gained four lines:** new backend acceptance scenarios drive the application-service port; where the
  guidance lives; that DISTILL, DELIVER and reviewer dispatches load it in full and quote the last lines; and "improve
  the tests you touch". None carries a Story, slice or ADR number.
- **`docs/ci-learnings.md`'s "Where to test" bullet** is now a one-line pointer to the skill.
- **A read-only audit of the frontend component tests** answered how much logic could move to function tests.

## What it achieved

- **The audit verdict, band 4:** "Moving logic cannot reach 90 %. Keep the shares as a direction and let time stay the
  rule." About 84 % of the component tests would have to move to reach 90 % fast. Weighted by test count, 9.8 % are
  logic whose function already exists (95 % interval 0.0 to 21.7 %), 12.6 % with small extractions added (2.5 to 23.9 %),
  and 45.2 % at most, counting mixed tests and hook or context changes (33.9 to 54.7 %). Slice A record, two raters.
- **The trial** (one acceptance-designer dispatch on Story 6083, read by a separate reviewer against 20 checklist items):
  6 followed, 1 followed with a caveat, 4 not followed, 8 not applicable to a backend story, 1 not measured. It found two
  gaps, taught in `references/backend.md`: how to drive a seeder or background job at the port, and that a scenario's
  Given is stated in full, never by reference to another scenario.

## Key decisions

- Rules cite the measured finding in words, not a Story number, because Story numbers get archived.
- Guards are quoted by message only where the guard exists; each quote was found in the guard's source.
- No new guard, and no edit to an existing one. Candidate tools are out of T11, for small Stories after it.
- The pyramid is a direction, not a gate; time is the hard rule.
- Maintainer, 2026-11-08: slice A starts beside T5's DELIVER; the ledger bullet becomes a pointer; the trial runs on
  Story 6083; rigor `standard`; and "improve the tests you touch" goes into `CLAUDE.md` as a fourth line.

## Quality

- Reviews: DESIGN and DISTILL each came back needing revision, and were revised. The slice A adversarial review found 0
  critical and 4 high, all fixed. The skill review (`nw-skill-reviewer`) asked for revision on 2 critical scope points
  in round 1 and approved in round 2.
- Mutation: N/A, because only documents and a skill changed, so Stryker has nothing to mutate. The skill's
  deliberate-break rule stands in for the tests it guides.

## Finalize checklist

| Item | Answer |
|---|---|
| Docs prose | `CLAUDE.md`, `docs/ci-learnings.md` and the skill are the change. Product docs N/A, because nothing a user sees changed. |
| Screenshots, demo data, website | N/A, because no screen, data shape or website asset changed. |
| Clients (CLI, MCP) | N/A, because no route or DTO changed. |
| Release notes | N/A; release notes are written at /release, not at finalize. |
| Context map, `ARCHITECTURE.md` | Context map: no change, as guidance adds no context, concept, edge or domain event. `ARCHITECTURE.md` §15's test stack line points to the skill, in this finalize commit. |
| Usage-data event | N/A, because nobody using the product reaches agent guidance. |

## Follow-ups

- **The deferred check stays OPEN.** The trial ran on a delivered feature, on paper, by an agent without the nWave method
  loaded. The next feature's DISTILL writes "skill loaded: yes/no" and its deviations from the skill into its own
  `feature-delta.md`, and the maintainer reads it after that DELIVER.
- The trial's open reading: whether adding a wire-contract row touches an existing hosted class, and so owes a line off
  `HostUsersStillToMove`.

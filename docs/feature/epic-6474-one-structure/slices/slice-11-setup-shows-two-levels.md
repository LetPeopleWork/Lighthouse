# Slice 11 — New-install set-up shows the two levels

Epic #6474 · **visible** · `job_id: job-config-admin-describe-our-levels-once` · ≤ 1 day · depends on: 05

## Elevator Pitch

- **Before**: Ana's first-run stepper says "Add Team", then "Add Portfolio", without saying how they relate.
- **After**: above the steps it reads "Teams work on Work Items, which roll up into Features of Portfolios",
  with a link to rename them in Organisation.
- **Decision enabled**: Ana knows what she is setting up before she picks a board, and that the words can be hers.

## Problem

Ana Lima evaluates Lighthouse on the free tier. A prospect who maps the wrong tracker level to "Feature" stops
in onboarding (6455 evidence E1). The set-up never shows the structure it assumes (D8: levels are part of
set-up).

## Learning hypothesis

One sentence plus a link orients a first admin. **Disproved if** testers still map a type two levels above the
Teams' items as the Feature type at the same rate (then #6475's guided set-up is needed sooner).

## IN

- `OnboardingStepper` shows the structure sentence from the level names, and "Rename in Organisation" (System
  Admins, any licence: M7).
- Hidden once the stepper is done or dismissed, as today.

## OUT

- Templates and the guided set-up (#6475, D17). Choosing a number of levels.

## Domain examples

1. Ana, fresh install: "Teams work on Work Items, which roll up into Features of Portfolios."
2. Priya, renamed: "Squads work on Work Items, which roll up into Initiatives of Portfolios."
3. Levels not loadable: the stepper shows its steps without the sentence.

## UAT scenarios

### Scenario: A new install shows its structure

Given Ana's install has no connection yet
When she opens the Overview
Then the set-up names the two levels and how they roll up

### Scenario: The sentence uses the organisation's words

Given Priya renamed Feature to "Initiative"
When the set-up shows
Then it says Initiatives

### Scenario: Set-up still works when the levels cannot be read

Given the levels cannot be loaded
When Ana opens the Overview
Then the set-up steps show without the sentence

## Acceptance criteria

- [ ] Sentence from the level names; link to Organisation.
- [ ] Degrades to today's stepper on a failed load; no layout jump while loading.

## Outcome KPI

OUT-3.

## Technical notes

- Reuse `OnboardingStepper`; terms via `useTerminology()`.
- Overview screenshot may change (at finalisation).

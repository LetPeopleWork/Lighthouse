# Slice 15 — An install can run with Teams only

Epic #6474 · **visible** · `job_id: job-config-admin-describe-our-levels-once` · ≤ 1 day · depends on: 05, 06
(11 for the first-run choice) · maintainer decision M13 (prototyped, approved 2026-11-02)

## Elevator Pitch

- **Before**: Ana Lima only wants Team flow metrics and forecasts, yet every screen offers Portfolios, Features
  and Deliveries she will never set up.
- **After**: she picks "Teams only" in the first-run set-up, or switches the level above Teams off in
  Organisation; Lighthouse shows one level.
- **Decision enabled**: an organisation that works at Team level only gets a structure that matches it, and can
  turn the level above back on without losing anything.

## Problem

Ana Lima evaluates Lighthouse for three Teams on the free tier. The Portfolio steps, menus and empty Feature
lists suggest she is using it wrong. Priya Raman's sister install retired its two Portfolios and wants them out
of sight without deleting their history.

## Learning hypothesis

A one-level structure is a switch on the level above, not a different product. **Disproved if** hiding the
level leaves Portfolio or Feature traces on Team pages that testers notice (then the hide list grows at DESIGN).

## IN

- Organisation → Levels: a switch "Level above Teams: on / off". Off = one level. System Admin; **no licence
  rule** (free and Premium alike).
- Off: no Portfolios, Features or Deliveries shown anywhere (navigation, Overview, onboarding step, Team pages'
  Feature references). Teams keep throughput, flow metrics and their forecasts (manual forecasts).
- First-run set-up (`OnboardingStepper`) offers "Teams only" as a choice.
- Switching off with existing Portfolios: confirmation naming them; they are hidden, **their data is kept**;
  switching on shows them again.
- The switch is locked while more than two levels exist (#6455 adds them).
- Loading and failure looks as in slice 05; the switch is disabled while the levels load or fail.

## OUT

- Deleting Portfolio data. More than two levels (#6455). The API shape of hidden Portfolios (DESIGN).

## Domain examples

1. Ana picks "Teams only" at first run: the Overview lists her three Teams; no Portfolio step, no Features menu.
2. Priya switches off with Portfolios "Checkout Revamp" and "Legacy Platform": the dialog names both; after
   confirming, they are gone from every screen.
3. A month later Priya switches back on: both Portfolios reappear with their Features, Deliveries and history.

## UAT scenarios

### Scenario: A new install starts with Teams only

Given Ana runs the first-run set-up
When she chooses "Teams only"
Then she sees no Portfolio step, no Portfolios and no Features
And her Teams show throughput, flow metrics and forecasts

### Scenario: Switching off names the Portfolios it will hide

Given Priya's install has Portfolios "Checkout Revamp" and "Legacy Platform"
When she switches the level above Teams off
Then she is asked to confirm, with both Portfolios named
And nothing changes if she cancels

### Scenario: Hidden Portfolios come back unchanged

Given Priya switched the level off and confirmed
When she switches it back on
Then "Checkout Revamp" and "Legacy Platform" are shown again with their Features, Deliveries and history

### Scenario: Teams lose nothing when the level above is off

Given "Payments Crew" with pinned throughput and manual forecasts
When the level above Teams is switched off
Then its throughput, flow metrics and manual forecasts are unchanged

### Scenario: Only a System Admin can switch the level

Given Tom is Admin of "Payments Crew" only
When he opens the levels
Then he cannot change the switch

## Acceptance criteria

- [ ] Off: no Portfolios, Features or Deliveries shown; Teams keep throughput, flow metrics and forecasts.
- [ ] First-run set-up offers "Teams only".
- [ ] Switching off names the Portfolios first; their data is kept; switching on shows them again.
- [ ] System Admin only; no licence rule; locked while more than two levels exist.

## Outcome KPI

OUT-3 (structure matches the organisation); DEVOPS may add a name-only event for the switch.

## Technical notes

- **Licensing**: none. The free-tier count of one Feature-level unit is unaffected; hidden Portfolios still
  count while they exist (DESIGN confirms).
- **Refresh**: hidden Portfolios should stop refreshing (no forecasts, no Delivery recomputation) and catch up
  on switch-on with one refresh. To confirm at DESIGN, with slice 12's one refresh.
- **API**: what `/portfolios` returns while hidden is a DESIGN question; clients note in slice 14 if it changes.
- Docs: Levels section and onboarding docs mention "Teams only" (at finalisation).

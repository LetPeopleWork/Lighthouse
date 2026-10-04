# Slice 08 — Optional rules split refinement into stages

> **Folded into slice 03 on 2026-10-04 (maintainer); ADO #6146 Removed; #6141 retitled "Optional stage rules: see
> what is refined, being refined or waiting".** A Ready rule no longer overrides the votes: stage and votes are two
> separate signals. See `../feature-delta.md` → "Maintainer decision — the E2 UI, and slice 08 folded into 03
> (2026-10-04)" and `../distill/upstream-issues.md`.

**Feature**: epic-5510-5881-refinement · **Epic (proposed)**: E2 Refinement need (#5881) · **Story**: US-08 ·
**Estimate**: ~1d · **Tier**: Community (rule options stay free, D25)

## Goal

Teams whose states don't separate Waiting, Being refined and Ready can do it with a rule (tag, field, state). A
configured rule decides for the Work Items it matches, which also overrides the vote outcome (D15, D16, DD-5).

## IN

- One optional `WorkItemRuleSet` per stage. It has the same limits and reuses the same editor as the blocked rules
  (`FlowMetricsConfigurationComponent`, C7).
- The resolution order of DD-5. Rules only ever apply to Work Items that are already in refinement states.
- Expand-only storage.

## OUT

New rule operators, and rules that pull Work Items into refinement from outside its states.

## Learning hypothesis

**This disproves "the blocked-items rule editor is usable for stages"** if a config admin needs more than five minutes
to set a Ready rule such as "Tags contains ready". If so, add presets before the docs are written.

## Data and dogfood moment

- Demo: add a `ready` tag to two Team Pulsar Work Items, or use the dev instance if a Team there already tags this way.
- Dogfood: time the config admin setting the rule.

## Acceptance criteria

AC-8.1 … AC-8.3 (US-08).

## Dependencies

Slice 03. Its override effect becomes visible once slice 13 exists.

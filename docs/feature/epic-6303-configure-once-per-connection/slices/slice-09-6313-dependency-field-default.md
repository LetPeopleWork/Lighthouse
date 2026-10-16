# Slice 09 — Story 6313 — The Portfolios' Dependency Field is set once

**ADO**: Story #6313 (Epic #6303) · **User story**: US-08 · **Estimate**: ~0.5 day · **Depends on**: finished #6300, #6312, #6304; slice 08
**Release point**: yes — completes one setting and can be released on its own (L8).
**Locked decisions**: D1 (the default is an Additional Field — a field or a link type), D7, D9–D13, D21

## Goal
A "Dependency Field for Portfolios" default on the connection — a field or, after #6300 / #6312, a link type —
followed by Portfolios in *Connection default* mode. `Ignore Dependencies` stays per Portfolio.

## IN
- Default column + Portfolio mode column (D10 back-fill); new Portfolios follow (D11); Dependency Settings dropdown
  entry (`DependenciesComponent.tsx`); resolver, save notice, queue and slice 07's list reused.
- Shown on Jira and Azure DevOps connections only; CSV and Linear take dependencies from elsewhere (D21).
- `DependencySourceSelector` reads the effective value, so Jira and ADO follow without change.

## OUT
- Inheriting `Ignore Dependencies` (planning choice, not a tracker fact).

## Learning hypothesis
Disproves "the dependency source is per connection in practice" if dogfood Portfolios on one connection need
different link types — then few follow the default and the setting stays mostly *Own*.

## Production data / dogfood
Slice 01's Portfolio plus a second Portfolio on the same connection, both following the default.

## Acceptance criteria
AC-8.1 – AC-8.4; AC-F.1 – AC-F.5.

## Changed assumptions
(filled during DELIVER)

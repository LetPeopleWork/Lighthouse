# Slice 09 — Story 6313 — The Portfolios' Dependency Field is set once

**ADO**: Story #6313 (Epic #6303) · **User story**: US-08 · **Estimate**: ~0.5 day · **Depends on**: finished #6300, #6312, #6304 (05a – 06); slice 08
**Release point**: yes — completes one setting and can be released on its own (L8).
**Locked decisions**: D1 (the default is an Additional Field — a field or a link type), D7, D9–D13, D21

## Goal
A "Dependency Field for Portfolios" default on the connection — a field or, after #6300 / #6312, a link type —
followed by Portfolios in *Connection default* mode. `Ignore Dependencies` stays per Portfolio.

## IN
- Default column + Portfolio mode column (D10 back-fill as amended: a value pointing at a deleted field becomes
  *Connection default*); new Portfolios follow (D11); Dependency Settings dropdown entry (`DependenciesComponent.tsx`,
  UI-2); resolver, save dialog count, log entry and queue reused.
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
AC-8.1 – AC-8.5 (AC-8.5 is AC-4.4 at Portfolio grain); AC-F.1 – AC-F.5 (save dialog count and log entry).

## Release notes (finalize)
"Portfolios whose Dependency Field pointed at an Additional Field that had been deleted now read the tracker's own
dependency link again, from their next update."

## Changed assumptions
- 2026-10-16 (DESIGN, maintainer P-5, P-12, UI-4): save dialog count and log entry instead of a save notice;
  deleted-field values repaired at the upgrade; slice 07's list no longer exists.

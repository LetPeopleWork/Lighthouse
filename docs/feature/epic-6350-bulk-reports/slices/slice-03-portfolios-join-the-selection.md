# Slice 03 — Portfolios join the same selection

**Feature**: epic-6350-bulk-reports · **Epic**: #6350 · **Story**: US-03 (ADO: to be created) · **Estimate**: ½d ·
**Tier**: Premium · **Ships alone**: yes (on 02b) · `job_id: job-lead-compare-every-owner-against-its-own-past-in-one-go`

## Goal

The owner step lists Teams and Portfolios in one selection (D3), so one run gives Teams and their Portfolio the same
comparison.

## IN

- Owner step grouped by kind ({Teams}, {Portfolios}) in the instance's terms; Portfolios where the user is Admin only.
- Window presets that fit a mixed selection (PROPOSED: 30 / 90 / 180 and any number of days ≥ 14; settled in the
  DISTILL sketch walk).
- Result rows name the owner kind ("Portfolio Ocean Explorer · Created"); every 02a reason applies, including "Not
  created: {Portfolio} Ocean Explorer does not capture {Work Item Age}".
- Server: the same per-owner checks for Portfolios; the Admin set (02a) includes Portfolios with `PortfolioWrite`.

## OUT

Everything 04 adds · per-owner configuration.

## Learning hypothesis

**Disproves "leads compare Teams and Portfolios on one setup"** if no dogfood run on the dev instance picks a
Portfolio together with Teams.

## Data and dogfood moment

Dev instance: a quarter run over Teams plus their Portfolio. Demo: Ocean Explorer with two Teams.

## Acceptance criteria

AC-3.1 … AC-3.4 in `feature-delta.md` (US-03).

## Dependencies

Slice 02b.

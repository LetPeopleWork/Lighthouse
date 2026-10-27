# Slice 02b — The owner step says why Create is unavailable

**Feature**: epic-6350-bulk-reports · **Epic**: #6350 · **Story**: US-02b (ADO: to be created) · **Estimate**: ¼d
(rev. after re-review 2026-10-27: was ½d; the Admin-set filter moved into 02a) · **Tier**: Premium ·
**Ships alone**: yes (on 02a) · `job_id: job-lead-compare-every-owner-against-its-own-past-in-one-go`

> The file name predates the re-review; the slice no longer adds the Admin-set filter (02a does).

## Goal

A user who administers nothing is told so up front, and the owner step never shows an empty list that reads as "no
Teams" while its owners are loading or could not be loaded.

## IN

- Premium, no Admin owner: Create reports disabled with "You are not Admin of any Team or Portfolio." (D17, LOCKED,
  NMD-4).
- While permissions load, Create reports is disabled.
- If the permission load fails, Create reports is enabled (permissive fallback, DDD-17) and the owner step shows
  "Your Teams and Portfolios couldn't be loaded." with Retry; Create is not available there.
- Choosing Create reports first loads the creation options: spinner in the button, disabled; the wizard opens at step 1
  only once they are ready, or on "What a report needs couldn't be loaded." with [Cancel] [Retry] (NMD-15).

## OUT

The Admin-set filter itself (02a) · Portfolios (03) · owners that cannot take the report (04).

## Learning hypothesis

**Disproves "saying why beats an empty step"** if a dogfood user without Admin rights, or on a throttled connection,
cannot tell why there is nothing to pick.

## Data and dogfood moment

Dev instance with a Viewer-only user; the browser throttled to a slow connection; the Admin-set read made to fail.

## DESIGN must answer

The loading and could-not-load looks (reuse the existing patterns).

## DISTILL must sketch first

The no-Admin disabled state and its copy; the owner step's loading and could-not-load looks. The sketch walk is a
hard gate for D17.

## Acceptance criteria

AC-2b.1 … AC-2b.4 in `feature-delta.md` (US-02b).

## Dependencies

Slice 02a.

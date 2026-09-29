# Slice 03 — Every Initiative on one page, grouped by Portfolio

**Feature**: epic-5806-initiatives · **Epic**: #5806 "Visualize Initiatives" · **Story**: US-03 · **ADO**: #6115 · **Estimate**: ~7h (≤1 day) · **Walking skeleton**

## Goal

The RTE clicks one navigation entry and sees every Initiative, grouped by the Portfolio it lives in,
with how many of its Features are done, and a count of Features that have no Initiative.

## IN scope

- Top-navigation entry named by the plural term, beside Features.
- One read request returning all rows the caller may see.
- Grouping by Portfolio; an Initiative spanning Portfolios once, under "Spans {portfolio}, {portfolio}".
- Per row: name linked to the tracker (or id and "name not available"), tracker type as a secondary
  label, "{done} of {total} {features} done".
- "Show: Not done / All" (default Not done); fully done Initiatives marked "All done".
- Footer: "{N} {features} have no {initiative}".
- Premium gate (API 403, Premium prompt in the page). Visibility by decision D23: a row shows
  when the reader can read a Portfolio holding its Features; its numbers cover all its Features and
  Features in unreadable Portfolios are only counted (D22). Through `IRbacAdministrationService` and
  `useRbac()`. Initiative grants arrive in slice 07, Epic #6119.
- One Playwright walking skeleton on demo data through a page object; per-theme `@screenshot` later at
  finalization.

## OUT of scope

- Any date, likelihood or status (slices 04-05).
- Expanding a row (slice 06).
- Filtering by tracker type; sorting beyond Portfolio then name.

## Learning hypothesis

**Disproves, if it fails**: that the Initiative level is populated well enough to be worth a page. If
the dev instance shows most Features under "no Initiative", that is the data-readiness signal (decision
D19), learned before any forecast work.

## Acceptance criteria

AC-3.1 to AC-3.7 in `feature-delta.md`.

## Dependencies

Slice 02 (the term). Slice 01 for the demo walking skeleton.

## Effort

~7h: backend read and gates ~3h, page ~3h, E2E ~1h.

## Dogfood / production data note

Open the page on the dev instance (real Portfolios, Jira and ADO parents). Write down the footer count
and the share of Features with an Initiative; that number is the first data point for the count D19 requires before release copy.

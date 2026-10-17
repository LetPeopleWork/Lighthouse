# Slice 04 — Read reports through the Lighthouse clients

**Feature**: epic-6298-report-pdf-export · **Epic**: #6298 · **Story**: US-04 (ADO: to be created) · **Estimate**: ~1d ·
**Tier**: Community · **Ships alone**: yes · **Repo**: `lighthouse-clients` · `job_id: job-share-numbers-outside-lighthouse`

> **Conditional on Q1.** Recommended option (a). If the maintainer chooses (b) REST from the skill or (c) the skill
> works from what the user supplies, this slice is dropped and slice 05 stands alone.

## Goal

An assistant connected to Lighthouse (MCP stdio or HTTP) and the `lh` command line can list a Team's or Portfolio's
reports and read one, with the same values, windows and dashes the Reports tab shows — so an assistant never has to
rebuild a frozen Then from today's metrics.

## IN

- Read-only: list an owner's reports (name, type, created date) and read one report, for Teams and Portfolios
  (D23). Tool and command names are DESIGN's, following the existing `lighthouse_{owner}_{noun}_{verb}` pattern.
- Uses the existing report read endpoints under `TeamRead` / `PortfolioRead` with the caller's own credential; no
  Lighthouse backend change expected.
- Relays: refused → no access to that {Team} / {Portfolio}; endpoint missing → "upgrade Lighthouse"; a report made
  by a newer version → "can't be shown here".
- The `lighthouse` skill names the new tools and command (the drift test stays green).
- Changesets for the touched packages; manual `pnpm release:version`, commit and push before the clients release.

## OUT

Creating, editing or deleting reports through the clients (stay web-only) · the rendering skill (05) · exporting a
PDF from the clients.

## Learning hypothesis

**Disproves "reading the report is enough for an assistant to answer about it"** if, in the `lighthouse` skill's
eval for a report question, the assistant still calls the metric tools to rebuild Then.

## Data and dogfood moment

- Dev instance (`:5169`): read the busiest Team's report through MCP stdio and `lh`; values compared with the
  Reports tab.
- Integration smoke against the demo image (the clients' existing smoke script) once a demo report exists there.

## Usage data

DEVOPS decides whether the clients report the web's `ReportOpened` with their source for these reads (K4, D27).

## Acceptance criteria

AC-4.1 … AC-4.5 in `feature-delta.md` (US-04).

## Dependencies

None in this Epic (Reports read endpoints shipped with Epic 5878). Independent of slices 01–03.

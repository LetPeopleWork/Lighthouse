# Slice 04 — Read reports through the Lighthouse clients

**Feature**: epic-6298-report-pdf-export · **Epic**: #6298 · **Story**: US-04 (ADO: to be created) · **Estimate**: ~1d ·
**Tier**: Community · **Ships alone**: yes · **Repo**: `lighthouse-clients` · `job_id: job-share-numbers-outside-lighthouse`

> **Q1 answered (a)** by the maintainer on 2026-10-17: this slice is in. The read returns the report's raw values
> (maintainer, 2026-10-17); the presentation lives in the skill (slice 05).
>
> **Estimate (rev. 2026-10-19): ~1d holds.** The clients carry no phrasing, and the usage report added below reuses
> the clients' existing consent-gated usage-data path with one type mapping and one recorded-answer test per source;
> it sits at the upper edge of the day.

## Goal

An assistant connected to Lighthouse (MCP stdio or HTTP) and the `lh` command line can list a Team's or Portfolio's
reports and read one, with the report's own values, windows and dashes as the backend answers them (unrounded, with no
change or percent; the Reports tab's presentation comes from the skill in slice 05) — so an assistant never has to
rebuild a frozen Then from today's metrics.

## IN

- Read-only: list an owner's reports (name, type, created date) and read one report, for Teams and Portfolios
  (D23): `lh report list` / `lh report get` and the MCP tools `lighthouse_team_report_get` /
  `lighthouse_portfolio_report_get` with their `list` twins. Raw values only; the summary names the report, its owner
  and both windows and phrases no metric line (U-5).
- **Usage data**: a successful `get` of a report whose type the client can name reports the web's `ReportOpened` with
  the type and its source (`Cli` for `lh`, `Mcp` for the MCP server), once per run or tool call, **only after the user's usage-data consent**;
  `list`, a refused read, an older Lighthouse and an unknown type report nothing; never a name, id or value (DEVOPS).
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

- Dev instance (`:5169`): read the busiest Team's report through MCP stdio and `lh`; raw values compared with the
  report's answer behind the Reports tab.
- Integration smoke against the demo image (the clients' existing smoke script) once a demo report exists there.

## Usage data

Decided at DEVOPS: the clients report the web's `ReportOpened` with their source for a successful `get`, after
consent (IN above; K4, measured per client identifier as a lower bound for instances).

## Acceptance criteria

AC-4.1 … AC-4.6 in `feature-delta.md` (US-04).

## Dependencies

None in this Epic (Reports read endpoints shipped with Epic 5878). Independent of slices 01–03.

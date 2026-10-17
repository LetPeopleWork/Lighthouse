# ADR-247: Reports are read through the clients as four read-only tools that return the report's raw values

- **Status**: Accepted (2026-10-17). Read-only client access decided by the maintainer in DISCUSS (Q1 = a); the
  content of the read decided by the maintainer on 2026-10-17 (raw values, not the web's phrased lines — MQ-3 = b,
  against the architect's recommendation, which is recorded below as the rejected alternative).
- **Feature**: epic-6298-report-pdf-export (ADO Epic #6298), slice 04 — repo `lighthouse-clients`
- **Deciders**: the maintainer; Morgan (Solution Architect)
- **Reopens, for reading only**: Epic 5878's exclusion of reports from the CLI and MCP. Creating, editing and deleting
  stay web-only.
- **Relies on**: ADR-219 (report read model), ADR-223 (narrow readers, generic fallback), ADR-224 (an MCP summary
  rides beside unchanged facts), ADR-229/230/231 (skills: folder per skill, the drift test, evals)

## Context

An assistant asked about a Then & Now report has nothing to read: no MCP tool and no `lh` command reaches reports, so
it rebuilds Then from today's metrics and gets it wrong. The Lighthouse backend already answers everything needed:
`GET /api/v1/{teams|portfolios}/{id}/reports` (name, type, created day) and `GET …/reports/{reportId}` (both windows
and every shown metric's Then and Now values, Now computed on read, a reason where a value is absent), under the
owner's Read guard. Nothing in the backend changes.

Existing patterns in `lighthouse-clients`:

- MCP tools are `lighthouse_{owner}_{noun}_{verb}` (`lighthouse_team_refinement_get`); single-owner tools take `id`.
- `lh` groups take the owner as `--team-id` / `--portfolio-id`; output is pretty by default, `--json` / `--toon`
  return the facts unchanged.
- A server-dependent feature is gated through `FEATURE_REQUIRES_SERVER_NEWER_THAN`; an older server is told to
  upgrade and is never asked.
- An object answer carries `summary` as a field beside unchanged facts (ADR-224 rule 1).

## Decision

1. **Client package** (`lighthouse-client`): `getTeamReports(teamId)`, `getTeamReport(teamId, reportId)`,
   `getPortfolioReports(portfolioId)`, `getPortfolioReport(portfolioId, reportId)`, calling the `/v1` report routes,
   gated by a new `reports` entry in `FEATURE_REQUIRES_SERVER_NEWER_THAN` (the last release without the report
   endpoints, read from the release history at DELIVER). Refusals: 403 → no access to that {Team} / {Portfolio};
   the version gate → upgrade Lighthouse; 422 `template-not-supported` → made by a newer version of Lighthouse, can't
   be shown here; 404 → no such report for that owner.
2. **MCP** (`lighthouse-mcp-core`): four tools, annotated read-only — `lighthouse_team_report_list` and
   `lighthouse_portfolio_report_list` with input `{ id }`; `lighthouse_team_report_get` and
   `lighthouse_portfolio_report_get` with input `{ id, reportId }`.
3. **`lh`** (`lighthouse-cli`): a `report` group — `lh report list (--team-id <id> | --portfolio-id <id>)` and
   `lh report get (--team-id <id> | --portfolio-id <id>) --id <reportId>`.
4. **The read returns the report's raw values.** The facts are the backend's answer unchanged: the header (report
   name, type, owner kind and id, created day, Then and Now windows) and, per shown metric, each member's Then and
   Now value as the server sends it, with its absence reason where there is no value. Nothing is rounded, signed,
   compared or judged in the clients. The `summary` (ADR-224 rule 1; also what `lh` prints by default) names the
   report, its owner in the instance's terms and both windows, and for a list counts the reports with their names,
   types and created days. It phrases no metric line, change or verdict.
5. **The tool descriptions say so**: the values are unrounded; how Lighthouse shows them (rounding, which way is
   better, change and percent change) is in the `lighthouse-reports` skill, which an assistant should load before
   presenting a report.
6. **The `lighthouse` skill names the four tools and both commands** (the drift test of ADR-230 stays red until it
   does).
7. **Releases**: changesets — `lighthouse-client`, `lighthouse-mcp-core` and `lighthouse-cli` minor; the stdio and
   HTTP packages follow their dependency bump; manual `pnpm release:version`, commit and push before the clients
   release.

## Consequence for the skill

The knowledge the web encodes for showing a Then & Now report moves into the `lighthouse-reports` skill
(`references/then-and-now.md`), copied at DELIVER from these sources in `Lighthouse.Frontend/src/pages/Common/Reports/`:

| Knowledge | Source |
|---|---|
| Metric order and titles; direction of good (Cycle Time lower, Throughput higher, WIP lower, Total Work Item Age lower) | `panels/reportPanelRegistry.ts` (`METRIC_CATALOG`, `reportPanelRegistry`) |
| Lines per metric, their labels and decimals: Cycle Time p95/p85/p70/p50 in days, 0 decimals; Throughput total (0) and per day (2 decimals, the only change line); WIP and Total Work Item Age average/median/min/max, 0 decimals | `panels/CycleTimePanel.tsx` (`PERCENTILES`), `panels/ThroughputPanel.tsx` (`CHANGE_LINES`, `TOTAL_DECIMALS`, `PER_DAY_DECIMALS`), `panels/DailySpreadPanel.tsx` (`SPREAD_LINES`), `panels/WipPanel.tsx`, `panels/WorkItemAgePanel.tsx` |
| Rounding (halves away from zero, at the line's decimals), the change computed from the two rounded values, percent change of the rounded Then rounded to a whole number, "Then was 0" when Then rounds to 0, "No change" when the rounded values are equal, the minus sign, a dash when either side has no value | `panels/thenNowComparison.ts` (`roundForDisplay`, `shownValue`, `compareThenNow`, `changeText`, `signed`, `DASH`) |
| Units: "N day(s)", "N.NN per day", plain numbers for WIP | `panels/thenNowComparison.ts` (`daysText`), `ThroughputPanel.tsx` (`perDayText`), `WipPanel.tsx` (`plainNumber`) |
| Absence reasons ("No finished {Work Items} in Then/Now", "Not captured for this report", "Can't be shown in this version of Lighthouse") | `panels/thenNowComparison.ts` (`reasonTexts`, `absenceTextOf`) |
| Summary groups: Improved / Got worse / Mixed / Unchanged, per metric from its change lines; a metric with no comparable line is in none | `panels/changeSummary.ts` (`summariseChange`), `ThenAndNowSummary.tsx` |

The skill never invents precision: it shows a value only at its line's decimals, never more, and a value the report
does not hold stays "—" with its reason.

**Drift risk, accepted, with mitigation.** These rules now live in two places, the web and the skill text. Mitigation:
(a) the skill's eval fixtures are real report answers taken from the dev instance, each paired with the lines the web
shows for it (a screenshot or copied text of the Reports tab at the same moment), and the evals score the assistant's
dashboard against the web's lines, so a skill that rounds, signs or judges differently from the web fails its eval;
(b) the DELIVER checklist for any change to the files above — or a new metric in Then & Now — includes updating the
skill's reference file and re-running its evals; (c) the reference file names its source files, so a reader of either
side finds the other.

## Alternatives considered

- **The summary states each line as the web's panel shows it, pinned by a shared contract fixture** (MQ-3 option a,
  the architect's recommendation). One deterministic string per number; the rules in TypeScript, tested. **Rejected
  by the maintainer** in favour of the raw values: the clients stay a thin pass-through of the server's answer.
- **The backend adds the shown text to its answer.** Moves presentation into the API and changes a shared contract.
  **Rejected.**
- **The skill calls the REST API directly** (DISCUSS Q1 b). **Rejected by the maintainer.**
- **One tool per verb with an `ownerKind` input.** Breaks the owner-first naming. **Rejected.**

## Consequences

- Positive: the clients add no presentation logic; slice 04 stays about one day; the facts are byte-for-byte the
  server's answer.
- Positive: no backend change, no endpoint, no migration; RBAC is the caller's own credential against the existing
  Read guards.
- Negative: the web's presentation rules are restated in prose an assistant follows; correctness rests on the skill's
  evals rather than on a unit test.
- Negative: an assistant without the `lighthouse-reports` skill gets unrounded values and no direction of good; the
  tool descriptions point it to the skill.

## Earned trust (what is probed, not assumed)

- Refusal relays tested with recorded answers: 403, 404, 422 `template-not-supported`, an older server version.
- A pass-through test: the facts block equals the recorded server answer.
- The dev-instance dogfood compares `lh report get` against the Reports tab value by value (raw vs rounded).
- The skill evals against web-rendered lines (above).

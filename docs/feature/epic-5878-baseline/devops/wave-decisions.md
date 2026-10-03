# Wave Decisions — DEVOPS — epic-5878-baseline

**Agent**: Apex (`nw-platform-architect`) · **Date**: 2026-10-03 · **Mode**: autonomous subagent, documents only.
Full reasoning: `feature-delta.md` → `## Wave: DEVOPS / …`. Machine artifact: `environments.yaml`.

## The nine decisions (brownfield — read, not asked)

| # | Decision | Answer |
|---|---|---|
| 1 | Deployment target | Existing artifacts: standalone packages / Tauri desktop, Docker image, Helm chart. Hosted platform torn down — nothing to deploy there now |
| 2 | Orchestration | Unchanged; no chart change |
| 3 | CI/CD | Existing GitHub Actions; no workflow added or edited |
| 4 | Existing infrastructure | All reused; no new component |
| 5 | Observability | Structured logging + *Recent problems* sink + opt-in usage data (browser detects, backend forwards, PostHog EU) |
| 6 | Deployment strategy | Calver release (cut after slice 10, D34); startup migrations; rollback = previous release, migrations left in place (expand-only) |
| 7 | Continuous learning | The usage-data catalogue; exposure is per owner by construction (nothing written until an admin creates a report) |
| 8 | Branching | Trunk-based on `main` |
| 9 | Mutation testing | `per-feature`, ≥ 80% (unchanged); once per Epic at finalize, acceptance suite excluded; `CLAUDE.md` not edited |

## Decisions taken in this wave

- **DVO-1** Three events, per the maintainer's instruction of 2026-10-03 (the name is the contract; the integer is the
  next free one when the slice lands — 12/13/14 if Reports lands before Refinement): `ReportCreated` (slice 01,
  `report_template`) · `ReportOpened` (01, `report_template`; Portfolio reports from 06 through the shared view) ·
  `ReportDeleted` (08, name-only; never for the owner cascade).
- **DVO-2** One closed enum `UsageDataReportTemplate` {`ThenAndNow`}, wire property `report_template`, on
  `ReportCreated` and `ReportOpened`. Usage data's own list; the browser maps `templateKey` through an exhaustive `Record`, so a new
  template fails to compile until somebody decides what it discloses. 5935 appends `Signal`. Supersedes D33's
  name-only "report created".
- **DVO-3** `ReportOpened` carries `report_template` from the start (maintainer, 2026-10-03): symmetry with
  `ReportCreated`, so the event's shape does not change when Signals arrives. Fires after the read's 2xx and 5 s of
  dwell, once per mount.
- **DVO-4** Route keys `TeamDetail_Reports` (`/teams/:id/reports`, slice 01) and `PortfolioDetail_Reports`
  (`/portfolios/:id/reports`, slice 06) on the existing tab-open events; they count the list (K1 funnel). The report
  view `/…/reports/:reportId` maps to **no** key (the existing `/:id/:tab?` matcher does not match it) — `ReportOpened`
  counts it, never both.
- **DVO-5** No owner-kind property and no Team-/Portfolio-prefixed names: no KPI splits by owner, and the route keys
  already give the split.
- **DVO-6** KPI measurability: K1 per-browser proxy; K2 per-browser lower bound on `ReportOpened`; K4 an
  upper-bound proxy only (Community browsers with ≥ 2 `ReportCreated`), not a count; K5 qualitative; G1 per-browser
  proxy (reads high, safe for a ceiling); G2/G3 CI + logs. K3 dropped. Proxies for K1, K2, G1 accepted by the
  maintainer 2026-10-03.
- **DVO-7** One expand-only migration in slice 01 (`Create-Migration.ps1 -MigrationName AddReports`, both providers);
  CHECK inline in `CREATE TABLE` (new table, so SQLite is fine); DB-level cascade asserted on both providers;
  `HistoricalSchemaPatch` needs **no** entry (it patches columns on seeded tables; this adds a table); rollback leaves
  the table; one rollback rehearsal against the previous release.
- **DVO-8** Forward tolerance ships in v1, because v1 is what 5935/5882 roll back to: unknown template key → listed,
  counted toward the cap, deletable, opening it gives a closed refusal code, never 500; unknown payload members ignored
  (no `UnmappedMemberHandling.Disallow`).
- **DVO-9** Rate limiting N/A: Lighthouse limits anonymous and credential surfaces only; no authenticated domain write
  has a policy, including costlier ones (manual forecast, reality check, refresh). Residual stated.
- **DVO-10** Logging: created Information (Warning `report-creation-slow` above 10 s, `ElapsedMs`); create refused
  Information; deleted Information; edited Debug; unreadable stored report Warning. Fields `ReportId`, `OwnerKind`,
  `OwnerId`, `TemplateKey` (+ lengths, `ElapsedMs`, `Reason`, `PayloadSchemaVersion`). Never the name or any value.
- **DVO-11** E2E: one walking skeleton (slice 01, Lightspeed, create a 30-day report, see the {Cycle Time} panel), run
  by `ci_verifysqlite` and `ci_verifypostgres`. No new `@auth` spec. `Program.cs` registration (slice 01) forces the
  full live-connector Integration suite — expected.
- **DVO-12** Lighthouse-Clients N/A (D15): additive endpoints, no existing shape changes, no client release.

## Upstream change

None. DVO-8 adds a behaviour to the v1 read path and changes nothing DESIGN decided.

## Maintainer answers (2026-10-03)

- **No fourth event.** Three events only; K4 stays an upper-bound proxy, labelled as such.
- **K3 dropped** — no contract, no dogfood query.
- **`ReportOpened` carries `report_template`** from the start (DVO-3).
- **Per-browser proxies for K1, K2, G1 accepted.**

## Artefacts

| Path | What |
|---|---|
| `feature-delta.md` → `## Wave: DEVOPS / …` | environment matrix, CI/CD outline, monitoring contracts, deployment + rollback, rate limiting, observability, mutation, branching, coexistence, pre-requisites, handoff, changed assumptions |
| `environments.yaml` | environments, scenario axes per slice, coexistence, deployment assumptions |
| `docs/product/kpi-contracts.yaml` | `OUT-5878-*` entries appended |

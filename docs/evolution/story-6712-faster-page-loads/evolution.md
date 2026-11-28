# Story 6712 — Faster page loads on slow connections

**Story**: ADO #6712 (https://dev.azure.com/letpeoplework/Lighthouse/_workitems/edit/6712), no parent Epic.
**Route**: spine waves with nwdes DELIVER. DISCOVER and DIVERGE were covered by the API-shape evaluation
(`docs/feature/api-shape-evaluation/spike/findings.md`, ADR-288). DISCUSS and DESIGN ran with the maintainer;
DEVOPS and DELIVER ran in AFK mode. Rigor: S1 and S2 `thorough`, S3 and S5 `standard`; the whole-candidate
review and EXAMINE on Sonnet. Product, design and acceptance facts: `docs/feature/story-6712-faster-page-loads/`
(`handoff.json`, `brief.md`, `architecture.md`, `acceptance.md`, `feature-delta.md`).

## What shipped

Pages load faster on a slow connection, and nothing on them looks different, apart from the update dialog.

- **Compression.** The server sends the frontend's JavaScript, the SPA page and safe API answers (GET and HEAD)
  compressed with Brotli or gzip when the browser asks (about 3.2× smaller bundle). Answers that may carry a
  secret are never compressed: the logs controller, the embed handshake and the update hub carry a
  `[NeverCompressResponse]` marker, and an architecture test keeps it on them. `ResponseCompression:Enabled`
  (default on) switches it off.
- **One shared permissions answer.** Every component that checks permissions reads one shared answer, asked once
  per page move and at once after the user's own grant, revoke or create. A component that stays on screen keeps
  what it shows until the new answer lands; a failed ask keeps a shown answer, and one Retry asks once for all.
- **One shared Teams list, Portfolios list and licence status**, asked once per page, with the user's own create,
  rename or delete shown at once.
- **A smaller update check.** The footer starts its checks together and asks `GET version/newest` for the newest
  version string only, instead of the full release notes on every page. The Update Available dialog names that
  version and loads the notes when it opens, with a loading look and a could-not-load message with Retry.

| Slice | Delivers | Commits |
|---|---|---|
| S1 | Compression and the never-compress marker | 9642f1559, d98d630b8, 34dede91d |
| S2 | One shared permissions answer behind `useRbac()` | 0b29f24c3, 8f623a856, da62ca442 |
| S3 | One shared Teams, Portfolios and licence read | 4b8df786e, 164092287, 9e237b1d2 |
| S4 | Dropped (see below) | 35fa85a7b |
| S5 | Newest-version route, parallel footer, notes on dialog open | 15f361c9d |
| Final | Refactor, typed marker tests, mutation kill tests, docs | d88106d7e, 4c3f3c431, ef171200c, this commit |

## Finalize checklist

| Item | Answer |
|---|---|
| Docs prose | Done: "Response Compression" in `docs/Installation/configuration.md` (S1); the update dialog's version line, loading and could-not-load looks in `docs/Installation/server.md` (this commit). |
| Screenshots | N/A: no `@screenshot` test shows the update dialog, and nothing else a screenshot shows changed. |
| Demo data | N/A: no new data; the feature changes how pages read existing data. |
| Website assets | N/A: no asset the website hot-links from `docs/assets/` changed. |
| Clients (CLI, MCP) | N/A, as DISCUSS answered: the clients call only `/version/current`, which is unchanged; a client that sends no `Accept-Encoding` gets answers as today. No release needed. |
| Context map, `ARCHITECTURE.md` | N/A, as DISCUSS answered: read caches and transport in the platform lane, reading Access, Organisation set-up and Licensing; no new concept, term, edge or domain event. |
| Usage-data event | N/A, as DEVOPS answered: nothing here is a user action; the outcome (bytes and calls per page) is measured by the spike's probes. |
| Mutation | Done: frontend **83.06 %** (201 of 242) after one kill round, from 71.90 %; backend feature code **100 %** (27 of 27). Accepted survivors and the whole-file caveat for `LighthouseReleaseService.cs` are in `docs/feature/story-6712-faster-page-loads/mutation/results.md`. |
| Release notes | N/A; written at /release. |

## Decisions taken in AFK mode, worth a look

- **S4 dropped (maintainer, 2026-11-28).** The design claimed nothing reads the per-Team settings the Portfolio page
  loads; the Feature WIP quick setting does (`featureWIP`). The fetch stays. Follow-up if wanted: add `featureWIP`
  to the portfolio answer, then drop the fetch.
- **Exclusion by marker, not by route** (maintainer asked for "something smarter" than literal paths during S1).
- **Freshness rule (D10):** a component waits for an answer asked on or after the page it mounted on; a component
  kept across a page move keeps its answer. A failed own-change ask leaves kept components on the pre-change answer
  until the next page move.
- **EXAMINE waiver (maintainer):** E1 and E3 (byte sizes on a running server) waived as an observation gap; the
  wire contract, the measured ratios and the CI image check are their coverage.
- `ADR-004` (the compression rule) stays `Proposed`; its acceptance is the maintainer's.
- The 5-minute review windows were skipped for this story (maintainer).

## Gates

- Slice reviews: every slice accepted, S3 and S5 after one rework round each.
- Whole-candidate review (Sonnet): accepted; its one low finding (by-name marker lookup in two S1 tests) was fixed
  and re-approved.
- EXAMINE (Sonnet, source-blind): accepted; 10 of 12 rows met, E1 and E3 waived.
- Mutation: see the checklist.
- Full suites at the final refactor: `pnpm test` 742 files, 10,592 tests passed; filtered `dotnet test` 15,636
  passed, 1 failed under load only (a regex timeout in a module-rules test, 5 of 5 alone). Builds: zero warnings.

## Left open, found in passing

- The wire-contract test host does not require sign-in when only `WithTestAuthentication` is used: `Program.cs` reads
  `Authentication:Enabled` from the host settings before the test configuration applies. Other wire contracts may be
  testing an open host.
- Under parallel load: `DataGridBase.paging` and two `ModifyTeamSettings` tests overrun the 2 s time budget (one
  even alone); a regex-timeout flake in the module-rules architecture tests; the forecast test
  `When_FixedThroughputAndRemainingDays_ReturnsCorrectForecast` failed once in a full run.
- No compression on an error page that has no endpoint is a design residue (it may be compressed); no secret is
  known to reach such a page.

# Story 6625 — Then & Now: Now is always as long as Then

Delivered 2026-11-17, nwdes feature `story-6625-baseline-report-creation`, three slices. Rigor: S1 `thorough` (Opus
crafter and acceptance designer, Sonnet reviewers), S2 and S3 `standard` (Sonnet; Haiku reviewers). The whole-candidate
review and EXAMINE ran on Sonnet.

## The problem

A Then & Now report let Now have a different length from Then. A 90-day Then against a 30-day Now compares a total of
three months with a total of one: the {Throughput} total, Arrivals and the Work Item counts in the summary drop for no
reason a reader can see. The wizard also asked about Now in a step of its own, although almost every report wants the
same length, and it refused too little kept history in words that named neither the Team or Portfolio nor the way out.

## What changed

- **Now is always as long as Then.** The wizard has three steps (Type, Then, Metrics & name). The Then step shows both
  windows by date, back to back by default ("Then 17 Sep – 16 Oct 2026 (30 days)", "Now 17 Oct – 15 Nov 2026 (30 days,
  ends today)"), says when they are the same days today or names the last day they share ("Now overlaps Then until
  31 Oct 2026"), and refuses too little kept history naming the owner and the way out ("Lightspeed keeps history from
  18 Sep 2026: too little for a 30-day Then and Now. Pick a shorter length or a later end date."). The detail pages hand
  the owner's name down; no request was added.
- **Creating reports for many owners** drops the "Same as Then / Custom" choice; the owner step is now step 4 of 4.
- **Saved reports and Edit.** A saved report reads its Now from Then: as long as Then, ending today. Edit opens on
  Metrics & name and sends no Now length. The server refuses a create or an edit whose Now length differs from Then's
  (`now-length-differs-from-then`, "Now is always as long as Then.") and stores no Now length for a new report. An older
  report that stored a longer Now keeps its stored member untouched and reads exactly like one without it: no notice,
  its frozen Then and earlier PDFs unchanged.
- **Throughput's Total gets its change.** Now that both windows have the same length, the Total line carries its own
  change ("▲ 13 (+25 %)") beside the per-day one ("▲ 0.44 per day (+25 %)"), and the CHANGE summary judges Throughput on
  both. A line that is 0 on both sides reads "No change (both 0)". The per-day change keeps the shared rule (the shown
  values are subtracted, so Then plus the change reads as Now on screen): the maintainer confirmed 0.44 over the
  sketch's 0.43 on 2026-11-17.
- Backend: `ReportWindowPolicy` loses `NowRefusal` and `NowWindow` and gains `NowLengthRefusal`; `ReportWindow.NowEndingOn`
  derives Now; `ThenAndNowPayload` loses its Now length; `ReportEdit`'s member is `RequestedNowLengthDays`, read only to
  refuse.

## Key commits

| Commit | What |
|---|---|
| `fc382f418`, `557f552dd` | Examiner expectations E1–E14; S1 design revision (owner name, window relation, Create-only backend) |
| `b1d2867a5`, `b5606ed9d` | S1: oracle, then GREEN — the wizard without a Now step, the window lines, the refusal copy, Create refuses an unequal Now |
| `35249c444`, `2c436d34d`, `709eba4ff` | S2: oracle, GREEN — saved reports and Edit derive Now — and the review's no-notice and log rows |
| `4b7f0c212`, `aaf312696`, `c101042b6` | S3: oracle, GREEN — Throughput's Total change — and the report-view rows rebuilt on values equal windows can produce |
| `5e9a4d2f4`, `851d5d116` | Fixed Now examples; the same-days test title quotes the whole line, so EXAMINE can observe it |

## Gates

- Oracle approvals: every slice approved before craft with its RED observed by the reviewer; S3 needed two rounds (an
  unrounded fixture, then the 0.44 decision). Shared test files between slices made earlier approvals stale twice; each
  was refreshed at the candidate.
- Whole-candidate review (Sonnet): accepted; one non-blocking finding (a test named after the removed Now step), fixed in
  the finalize commit.
- EXAMINE (Sonnet): accepted, E1–E14 met, on the frontend wizard and panel rows and the backend service-port scenarios.
  The first pass left E3 as an observation gap (a title quoting only the first sentence of the same-days line) and the
  backend readbacks unobserved (a probe too wide to finish); both were re-observed.
- Mutation: backend **87.93 %** (58 mutants, 4 m 25 s; one survivor killed by a new edit row, one equivalent, five unreachable exception texts), frontend **85.38 %** (130 mutants, 24 m at concurrency 1; the draft guards got new tests, the rest accepted with reasons), see `docs/feature/story-6625-baseline-report-creation/mutation/results.md`. The first frontend run was killed for low memory by another session's process; the re-run narrowed the specs to those covering the changed lines.

## Finalize checklist

| Item | Answer |
|---|---|
| Docs prose | **Done** — `docs/reports/thenandnow.md`: Now as long as Then, the three-step wizard with both windows and the overlap and same-days lines, the owner-named history refusal, the bulk wizard without the Now choice, Edit opening on Metrics & name, older reports with a stored Now, Throughput's two change lines and "No change (both 0)". |
| Screenshots | **Done** — `report_wizard.png` (the three steps and both windows by date), `report_thenandnow.png` (Throughput's Total with its own change), `reports.png`, `reports_page.png` and `report_bulk_owners.png` regenerated with `--grep` on their tests after deleting the old images; `report_bulk_result.png` came out identical. The E2E page objects walk the three-step wizard and the owner step 4 of 4; `Reports.spec.ts` and `ReportPdfExport.spec.ts` pass locally (7/7). |
| Demo data | **N/A**, because the demo Teams and Portfolios keep a year of history, so the default back-to-back windows fit. |
| Website assets | **Done** — the regenerated images under `docs/assets/features/`; the website hot-links them from `main`, so pages showing them update once this is pushed. No website copy mentions a Now length. |
| Clients (CLI, MCP) | **N/A**, because the clients only read reports (`thenWindow` and `nowWindow`, whose shape is unchanged) and never create or edit one; checked in `lighthouse-clients` (no `nowLengthDays` anywhere). |
| Context map, `ARCHITECTURE.md` | **Done** — `ARCHITECTURE.md` §4's Reports row now says Now is a window as long as Then that ends today. ADR-270 has no definition of Now; no context, edge, event or glossary term changes. |
| Usage-data event | **N/A by maintainer decision** (2026-11-17): `ReportCreated` already counts reports. |
| Mutation | **Done** — see Gates. |
| Release notes | **N/A; written at /release.** |

Knowingly left open: the frontend `BaseMetricsView` split item is unrelated; nothing from this Story is deferred.

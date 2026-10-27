# Wave Decisions — DESIGN — epic-6350-bulk-reports

**Agent**: Morgan (`nw-solution-architect`) · **Date**: 2026-10-27 · **Mode**: PROPOSE, autonomous subagent.
**Predecessor**: DISCUSS (`../discuss/wave-decisions.md`, committed e082bce11). **Successor**: DEVOPS
(`nw-platform-architect`), then DISTILL with the sketch walk.

> **Revised after DEVOPS, 2026-10-27**: `../devops/upstream-changes.md` DU-1 to DU-4 applied (DDD-9 already-gone
> marker, DDD-13 log fields and slow lines, the usage placement's no-dwell page opening and two-or-more combined
> export); ADR-266 items 5 and 8 updated.

Full text: `../feature-delta.md` → "Wave: DESIGN" sections. Back-propagation: `upstream-changes.md`. ADRs:
ADR-265, ADR-266, ADR-267 in `docs/product/architecture/` (Proposed). Brief: `docs/product/architecture/brief.md` →
"Application Architecture — epic-6350-bulk-reports".

## Scope and style

Application scope. No new bounded context and no module line crossed: the Reports module gains a cross-owner face
(ports-and-adapters, as built). No new container, external system, dependency or migration (D11 confirmed).

## Key Decisions

Decided:
- **DDD-1/2** one cross-owner list `GET api/latest/reports`, server-filtered by Read, rows = per-owner rows + `ownerName`
  (optional, so per-owner bodies stay identical).
- **DDD-3** one owner-free `GET api/latest/reports/creation-options`: templates with metrics per owner kind, window
  bounds, readable owners with name and kept-history start (serves 02a, 03, 04).
- **DDD-4** the Admin set is derived in the browser through `useRbac()` (it already carries it); no new RBAC member.
- **DDD-5** Admin checked per owner when the run reaches it, through a principal-bound capability port the controller
  hands into the Reports module (the module still never sees a user).
- **DDD-6** each owner stands alone: a failed save is caught, its pending change discarded, answered `not-saved`.
- **DDD-8** three refusal codes added: `owner-not-found`, `not-admin`, `not-saved`; 400 rules for a malformed run; a
  mid-run licence lapse is the existing cap refusal.
- **DDD-9** bulk removal = one request of report ids; owners resolved by the server; already gone = removed, with
  `code: "already-gone"` so it is never counted as a deletion in usage data (DU-1).
- **DDD-12** usage events stay browser-sent; the domain-event bus was weighed and rejected (consent and the browser
  proxy live in the browser; no second consumer of a run's effects).
- **DDD-13** one `report-bulk-run` log line per run (fixed fields, no names), Warning `report-bulk-run-slow` above
  30 s create / 10 s remove; `report-list-slow` and `report-creation-options-slow` above 2 s (DU-4); per-report slow
  lines unchanged.
- (rev. at the final review gate, 2026-10-27; NMD-8, NMD-11..16) **DDD-3/16** Create reports prefetches the creation
  options keyed per opening and opens the wizard only once they are in, else on "What a report needs couldn't be
  loaded." [Cancel] [Retry] · **DDD-15** Owner cell = name with a kind icon; row icons from slice 01, Edit/Delete by
  `useRbac()` · **DDD-9** already-gone skips the Admin check on purpose · **R9** +3 codes incl. `not-saved`, copy from
  NMD-12's table · combined-print failure = NMD-8's dialog · `DiscardPendingChanges` inside the run's bounded change.
- **DDD-14–16** `/reports` page on a keyed query; `ReportList` + `DataGridBase` extended with an owner column and an
  opt-in selection; the bulk wizard composes the single wizard's steps.
- **DDD-18/19** combined PDF = the hidden print frame drawing several reports, ready only when all are; macOS
  disabled state reused.
- **DDD-20** clients: `lighthouse_report_list` and `lh report list` with no owner read the cross-owner list.
- **DDD-21** Create reports gated by the shared licence query.

LOCKED by the maintainer (2026-10-27), each the recommendation: **DDD-7 / MQ-1 (a)** one bulk-create request, owners
one by one · **DDD-10 / MQ-2** N = 50, shared by selection and owners per run, dropping to 25 if a 50-report print
fails in the Linux desktop app · **DDD-11 / MQ-5 (a)** the run finishes on the server · **DDD-17 / MQ-4 (a)**
`useRbac()` reports a failed load, the owner step shows Retry · **MQ-3 (a)** slice 04's metric half dropped.
The options as first put (kept for the record): **MQ-1** one bulk-create request (recommended) vs browser fan-out vs streamed progress ·
**MQ-2** ceiling N = 50 (recommended; 25 / 100), shared by selection and owners per run · **MQ-3** drop slice 04's
metric half (recommended) · **MQ-4** `useRbac()` reports its load failure (recommended) · **MQ-5** a run finishes when
the browser stops listening (recommended).

## Constraints established

Bulk goes through `ReportCommands` only (ArchUnitNET) · no user inside the Reports module · per-owner outcome in the
order asked · N enforced on the server and said in the browser first · every fetching view has loading and failure
looks via `WidgetShell` status and keyed queries.

## Upstream changes

U-1 … U-8 in `upstream-changes.md`: the `metric-not-captured` row and slice 04's metric half have nothing to act on
(capture is per owner kind); AC-2b.3 depends on MQ-4; N also bounds owners per run; AC-2.7 holds only under MQ-1 (a).
**Applied 2026-10-27** to the DISCUSS sections of `../feature-delta.md`, the slice briefs and the journey yaml, after
the maintainer's answers.

## Peer review

`nw-solution-architect-reviewer`, 2026-10-27: approved with conditions, no critical issue. All three high items
(async capability, per-owner isolation mechanism, Admin-gated controls waiting for `useRbac()`) and the medium items
are addressed; details in the review block of `../feature-delta.md`.

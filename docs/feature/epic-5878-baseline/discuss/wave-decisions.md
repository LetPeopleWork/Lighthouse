# Wave Decisions — DISCUSS — epic-5878-baseline

**Agent**: Luna (`nw-product-owner`) · **Date**: 2026-10-02, revised 2026-10-03 · **Mode**: autonomous subagent; the
maintainer was not available mid-run, so open questions carry provisional assumptions.
**Predecessor**: DIVERGE (Option 3 "Claims checklist"; D8–D16, DV-1..DV-9 and the 2026-10-02 maintainer resolutions
settled, not re-opened — except where the maintainer's 2026-10-03 answers supersede them). **Successor**: DESIGN
(`nw-solution-architect`). Peer review (`nw-product-owner-reviewer`) is dispatched by the coordinator.

**Revision 2026-10-03.** The maintainer answered Q1–Q7 (D37–D44). The report no longer gives verdicts: it shows one
panel per metric — Then, Now, change, % change, and Now's points beyond Then's frozen limits — and people interpret.
Slices 03, 04, 05, 07, 10 re-cut; slice 11 dropped. Slice numbers kept so ADO Stories #6159–#6169 still match.

Full text of everything below: `../feature-delta.md` → "Wave: DISCUSS" sections. Slice briefs: `../slices/`.

## Config (given, not asked)

feature_type user-facing (full stack) · walking skeleton brownfield · research depth comprehensive, reusing DIVERGE ·
JTBD light (job reused, not re-derived) · density lean.

## Scope Assessment: PASS — 10 stories, 1 new module (reports + metric catalog with panels) on 3 existing ones (metrics, licence, RBAC), estimated ~9½ days

Slice 11 dropped on 2026-10-03, so the story count is back to 10; effort just under 2 weeks; one user outcome,
nothing ships without the Reports tab. No Epic split. The release is cut after slice 10 (Edit report is part of v1
since D43/D44); no slice is cancellable any more.

## Key Decisions

- **D16** (DIVERGE) — rule/verdict part superseded by D39; the user-selected, extensible catalog stands, as metrics.
- **D17** Every story carries `job_id: job-flow-coach-show-whether-flow-changed`; no new job or persona.
- **D18** Reports tab after Metrics on Team (after Refinement once epic-5510 lands) and Portfolio; always enabled;
  role-specific empty state.
- **D19** Create flow: template card → Then end date and length → Now length → metrics (all four ticked) → defaulted
  name. *Rename part superseded by D44.*
- **D20** *Superseded by D37* (was: Then presets only, Team 14/30/90, Portfolio 30/90/180).
- **D21** Then validation: end ≤ today, ≥ 14 days, whole window inside `DoneItemsCutoffDays`; refusal names the
  earliest start.
- **D22** *Superseded by D43* (was: Now preset per view in the address, not saved).
- **D23** *Superseded by D38* (was: Now clipped to start after Then).
- **D24** *Superseded by D39* (was: verdict rules, sustained signal for Holds).
- **D25** *Reshaped by D41* (was: own minimum-data thresholds).
- **D26** Frozen at creation for every applicable metric: the Then values the panel shows, Then's XmR average and
  limits unrounded, readiness; plus window, Now length, template, selection, name, settings snapshot. Whether the
  full Then series is stored too is DESIGN's call. Never recomputed; all-or-nothing creation. *Reshaped by D40.*
- **D27** Metrics added later: "Not captured for this report".
- **D28** *Superseded by D39* (was: zero-clamp sentence; the fact behind it is Q8).
- **D29** Cap 2 per owner across all templates (Community), Premium unlimited, lapse keeps all readable/deletable;
  reports go with their owner (confirmed by D42).
- **D30** Delete for editors, confirmed; warns when Then can no longer be rebuilt.
- **D31** Settings-changed notice over types, state mapping, query, blackout days; one header line.
- **D32** Words: "Reports", "Then & Now", "Then", "Now", "{Cycle Time}: Then & Now"; no "Baseline"; no verdict
  words; no new Terminology key.
- **D33** Usage data via new route keys + name-only candidates (DEVOPS designs); editing gets no event.
- **D34** Slices 01–07 safe on trunk before the cap; release notes/docs/website wait for slice 08; release after 10.
- **D35** No intervention-date field; header shows both windows and days since Then ended.
- **D36** One foundation for Epics 5878/5882/5935: templates are flavors of Reports. 5878 lays the report model, the
  Reports view and the metric catalog with its panel registry; **no rule engine** in 5878 (Then & Now has no rules) —
  Signals (5935) adds rules and live evaluation on top of the catalog. No Signals tab; PDF/email/schedule per report,
  any template, with 5882; reuse chart components where a template has charts; no app-level page in v1; cap 2 across
  all templates incl. signals.
- **D37** (Q1) Then = a picked end date and a length going back: Team 30/90, Portfolio 90/180, or custom days;
  ≥ 14 days, inside `DoneItemsCutoffDays`; calendar days, inclusive, instance time zone.
- **D38** (Q2) Now is the full rolling window ending today and may overlap Then; never clipped.
- **D39** (Q3) No verdicts, no narrative, no charts, no Stable/Unstable or data-maturity label. One registered panel
  per metric, ValueFlow-style (Then left, accent bar, Now right with change and % change). v1: M1 {Cycle Time}
  (Then 85th; Now 85th/70th/50th), M2 {Throughput} (weekly median, weeks), M3 {WIP} (average, range), M4 {Work Item
  Age} (total, average). Green/red by a fixed per-metric direction of good, known only to the frontend panel. Room
  for a later per-panel author note.
- **D40** (Q3 follow-up) Each PBC metric's panel counts Now's points beyond the XmR limits frozen from Then, split
  above / below, coloured by direction of good; limits frozen unrounded, never the owner's PBC Baseline.
- **D41** (Q4) Too little data: "—" with a reason exactly where the Metrics page's existing guards refuse a value
  (percentile minimum, PBC too few points); no new thresholds; never 0.
- **D42** (Q5) Reports are deleted with their Team/Portfolio; its delete confirmation names "and its N reports".
- **D43** (Q6) Now length set at creation (default = Then length; same presets + custom), saved on the report, not
  in the address; viewers cannot change it; editors can, stored for everyone. Scheduled sending (5882) needs it.
- **D44** (Q7) "Edit report" dialog for editors: name, Now length, shown metrics. Then is never editable.

## Requirements Summary

- **Who**: `flow-coach` — consulting (Elena Kovacs, Team Lightspeed, engagement from 1 Aug 2026), short assessment
  (Dev Malhotra, Team Gravity, ends 2 Oct 2026) and internal (Priya Raman, Team Gravity, WIP limit from 14 Sep 2026);
  reader `delivery-lead-rte` (Martin Achterberg, Viewer).
- **What**: a Reports tab on Teams and Portfolios; a "Then & Now" report that freezes a Then window (picked end date,
  preset or custom length, inside the data cutoff) and sets a saved-length rolling Now beside it, one panel per
  metric: Then and Now values, change, % change, and Now's points beyond Then's frozen limits, coloured by each
  metric's direction of good. No verdicts; people interpret.
- **Slices (order = priority)**: 01 Reports tab + Team report + {Cycle Time} panel (WS) · 02 past end date + custom
  length · 03 {Cycle Time} points beyond Then's limits · 04 metric picker + {Throughput} panel · 05 {WIP} + {Work Item
  Age} panels · 06 Portfolio · 07 Now length at creation + days since · 08 delete + cap + lapse + owner deletion ·
  09 settings notice · 10 Edit report · ~~11 claim chart~~ dropped.
- **KPIs**: north star K2 (≥ 40% of report-creating instances return to Reports ≥ 7 days later); K1 activation
  ≥ 10% in 90 days; K3 reports saved with a Now shorter than Then (informational); K4 cap hits (land-and-expand
  learning); K5 qualitative (2 consultants show it to management); guardrails G1 (re-roll deletes ≤ 20%), G2 (≤ 2 s
  open, ≤ 10 s create), G3 (never 0 for absent data). Re-checked against D39: none counted verdicts.

## Constraints Established

- RBAC: create/edit/delete = `TeamWrite`/`PortfolioWrite`; view = Read; UI via `useRbac()` only.
- Then limits come from Then, frozen unrounded (`XmRResult` rounds), never from the owner's PBC Baseline setting.
- The direction of good is a fixed frontend panel property; never stored or computed in the backend.
- No synthesised values; absent data is "—" with a reason from the existing guards only.
- One expand-only migration in slice 01 via `CreateMigration`, all providers; cascade with the owner.
- Terminology tokens for every metric word; no "Baseline", "Epic", "Story", "Initiative"; no verdict words.
- NFR: create ≤ 10 s on 365 days of history; open ≤ 2 s; Metrics tab unchanged.
- Out of scope: CLI/MCP, export, Delivery level, cross-owner reports, verdicts, rules/thresholds, narrative, charts,
  author note (later), viewer-changeable Now, editing Then, nice-to-have metrics.

## Upstream Changes

DISCOVER and DIVERGE documents untouched; see `feature-delta.md` → "Changed Assumptions":

- DISCOVER V4 ("no previous-period comparison in the codebase") is partly contradicted: Story 5914's step-back and
  previous-period widget comparisons exist; none freezes, so differentiation holds.
- DISCOVER journey's intervention date and "read without Lighthouse open" replaced by the Then end date and in-app
  reading (D35, D13).
- DIVERGE's "Claims checklist" with verdicts replaced by metric panels without verdicts (D39); catalog C1–C6 → M1–M4
  (C5 dropped, C6's 85th percentile inside M1); "freeze limits" → freeze unrounded limits, series storage DESIGN's
  call (D40); Now overlaps Then (D38) and its length is saved on the report (D43).

SSOT updates:

- `docs/product/journeys/epic-5878-baseline.yaml`: 2026-10-02 — both journeys re-pointed to
  `job-flow-coach-show-whether-flow-changed`, steps aligned, reader step added, shared artifacts rewritten.
  2026-10-03 — verdicts, claims, clipping and charts removed; panels, saved Now length, Edit report and frozen
  limits in; changelog entry.
- `docs/product/jobs.yaml`: 2026-10-03 — `job-flow-coach-show-whether-flow-changed` job story and dimensions no
  longer speak of rules and verdicts (D39); changelog line in its note.

## Open questions for the maintainer

**Resolved 2026-10-03**: Q1 → D37 · Q2 → D38 · Q3 → D39, D40 · Q4 → D41 · Q5 → D42 · Q6 → D43 · Q7 → D44.

**Resolved 2026-10-03**: Q8 a lower limit at 0 shows "—" below with "Then's lower limit is 0", not 0 · Q9 % change
from a Then of 0 shows "—" with "Then was 0" · Q10 total {Work Item Age} shows both the window average of the daily
totals and the actual total on the window's last day.

## Risks carried forward

R-A sponsors may read a red panel without context — there is no narrative to soften it, by design; the author note
(later) is the answer · R-C/R-D low counts and the zero clamp make the beyond-limits count mute on per-item data
(tested first, slice 03; Q8) · R-E evidence base (DISCOVER G1–G3 failed; interview protocol still owed by the
maintainer, K5) · demo history is short; screenshots may need a CSV adjustment (checked in slice 03) · without
verdicts the differentiation against ValueFlow rests on the frozen Then and the count against frozen limits.

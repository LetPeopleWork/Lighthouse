# Wave Decisions — DISCUSS — epic-5878-baseline

**Agent**: Luna (`nw-product-owner`) · **Date**: 2026-10-02, revised 2026-10-03 · **Mode**: autonomous subagent; the
maintainer was not available mid-run, so open questions carry provisional assumptions.
**Predecessor**: DIVERGE (Option 3 "Claims checklist"; D8–D16, DV-1..DV-9 and the 2026-10-02 maintainer resolutions
settled, not re-opened — except where the maintainer's 2026-10-03 answers supersede them). **Successor**: DESIGN
(`nw-solution-architect`). Peer review (`nw-product-owner-reviewer`) is dispatched by the coordinator.

**Revision 2026-10-03.** The maintainer answered Q1–Q7 (D37–D44). The report no longer gives verdicts: it shows one
panel per metric — Then, Now, change, % change, and Now's points beyond Then's frozen limits — and people interpret.
Slices 03, 04, 05, 07, 10 re-cut; slice 11 dropped. Slice numbers kept so ADO Stories #6159–#6169 still match.

**Revision 2026-10-03 (after DESIGN).** The maintainer answered DESIGN's decisions to confirm: D45 (sample size
visible), D46 (no settings-changed notice; slice 09 dropped), D47 ({Throughput} total and per-day average), D48
(average {Work Item Age} as window average and last day).

**Revision 2026-10-14 (after the DISTILL sketch review).** The maintainer decided D49 (no counts beyond Then's limits
on any panel; Then no longer freezes XmR average and limits), D50 ({WIP} as Average, Median, Min, Max), D51 (Total
{Work Item Age} as Average, Median, Min, Max of the daily total age; supersedes D48 and Q10) and D52 (summary header;
panel side headings read just THEN / NOW). Slice 03 (ADO #6161) becomes the summary header; 04 and 05 lose their
beyond-limits parts. Superseded entries below are marked, not deleted.

**Revision 2026-10-14 (after the slice-04 live check).** The maintainer decided D53 (the summary header gains a third
segment, CHANGE: per shown metric, Improved / Got worse / Mixed / Unchanged from the panels' own marks) and D54 (the
{Cycle Time} panel adds the 95th percentile). Both go into a new slice 05b, after slice 05 and before the final
quality gates, within this Epic (ADO Story: to be created); US-05b added.

Full text of everything below: `../feature-delta.md` → "Wave: DISCUSS" sections. Slice briefs: `../slices/`.

## Config (given, not asked)

feature_type user-facing (full stack) · walking skeleton brownfield · research depth comprehensive, reusing DIVERGE ·
JTBD light (job reused, not re-derived) · density lean.

## Scope Assessment: PASS — 10 stories, 1 new module (reports + metric catalog with panels) on 3 existing ones (metrics, licence, RBAC), estimated ~9–9½ days (slice 05b added 2026-10-14, ~½–1 day; was ~8½; slice 01 re-estimated 1 → 1½ days on 2026-10-13; slice 03 re-cut to the summary header, 1 → ½ day, on 2026-10-14; was ~9)

Slice 11 dropped on 2026-10-03 and slice 09 after DESIGN (D46), so the story count was 9; slice 05b (D53, D54, 2026-10-14) makes it 10 — at the limit, not over it; effort ~9–9½ days, under 2 weeks; one user outcome,
nothing ships without the Reports tab. No Epic split. The release is cut after slice 10 (Edit report is part of v1
since D43/D44); no slice is cancellable any more.

## Key Decisions

- **D16** (DIVERGE) — rule/verdict part superseded by D39; the user-selected, extensible catalog stands, as metrics.
- **D17** Every story carries `job_id: job-flow-coach-show-whether-flow-changed`; no new job or persona.
- **D18** Reports tab after Metrics on Team (after Refinement once epic-5510 lands) and Portfolio; always enabled;
  role-specific empty state.
- **D19** Create flow: template card *(— superseded by DT2 (2026-10-13): users read "Type"; ① Type of the shared
  wizard, DT3)* → Then end date and length → Now length → metrics (all four ticked) → defaulted
  name. *Rename part superseded by D44.*
- **D20** *Superseded by D37* (was: Then presets only, Team 14/30/90, Portfolio 30/90/180).
- **D21** Then validation: end ≤ today, ≥ 14 days, whole window inside `DoneItemsCutoffDays`; refusal names the
  earliest start.
- **D22** *Superseded by D43* (was: Now preset per view in the address, not saved).
- **D23** *Superseded by D38* (was: Now clipped to start after Then).
- **D24** *Superseded by D39* (was: verdict rules, sustained signal for Holds).
- **D25** *Reshaped by D41* (was: own minimum-data thresholds).
- **D26** Frozen at creation for every applicable metric: the Then values the panel shows, Then's XmR average and
  limits unrounded *(superseded by D49, 2026-10-14: no XmR average or limits frozen)*, sample sizes; plus window, Now length, template, selection, name (no settings snapshot, D46). The
  full Then series is not stored (DESIGN, maintainer-accepted). Never recomputed; all-or-nothing creation. *Reshaped by D40.*
- **D27** Metrics added later: "Not captured for this report".
- **D28** *Superseded by D39* (was: zero-clamp sentence; the fact behind it is Q8).
- **D29** Cap 2 per owner across all templates (Community), Premium unlimited, lapse keeps all readable/deletable;
  reports go with their owner (confirmed by D42).
- **D30** Delete for editors, confirmed; warns when Then can no longer be rebuilt.
- **D31** *Superseded by D46* (was: a settings-changed notice in the header).
- **D32** Words: "Reports", "Then & Now", "Then", "Now", "{Cycle Time}: Then & Now"; no "Baseline"; no verdict
  words; no new Terminology key.
- **D33** Usage data via new route keys + name-only candidates (DEVOPS designs); editing gets no event.
  *Superseded in part by DEVOPS (maintainer, 2026-10-03)*: `ReportCreated` and `ReportOpened` carry closed enum
  `report_template`; `ReportDeleted` name-only; no cap-refusal event.
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
  (Then 85th; Now 85th/70th/50th; *Then 70th/50th too since DT9, and the 95th on both sides by D54*), M2 {Throughput} (total and per-day average, reshaped by D47), M3 {WIP} (average,
  range — *reshaped by D50*: Average, Median, Min, Max), M4 {Work Item Age} (total and average, each as window
  average and last day — Q10, D48; *reshaped by D51*: Total {Work Item Age} only, Average, Median, Min, Max). Green/red by a fixed per-metric direction of good, known only to the frontend panel. Room
  for a later per-panel author note.
- **D40** *Superseded by D49 (2026-10-14).* (Q3 follow-up) Each PBC metric's panel counts Now's points beyond the XmR limits frozen from Then, split
  above / below, coloured by direction of good; limits frozen unrounded, never the owner's PBC Baseline.
- **D41** (Q4) Too little data: no new thresholds; never 0. *Wording superseded by D45* (was: "—" where the Metrics
  page's existing guards refuse a value — DESIGN found no percentile minimum guard exists).
- **D45** (after DESIGN) Every value shows its sample size ("85th: 12 days · 3 {Work Items}"); "—" + reason only when
  truly empty: no finished {Work Item}, < 2 points for Then's limits, collapsed band, lower limit 0 for the
  below-count (Q8), Then was 0 for % (Q9). No threshold. *(The three limit reasons are superseded by D49,
  2026-10-14.)*
- **D46** (after DESIGN) No settings-changed notice; nothing about settings stored; slice 09 and US-09 dropped (ADO
  #6167 is the maintainer's call).
- **D47** (after DESIGN) {Throughput} over the whole window: total finished and the average per day, Then and Now;
  change and % change on the per-day average; no weekly median, no week count; beyond-limits count unchanged
  *(that count dropped by D49, 2026-10-14)*.
- **D48** *Superseded by D51 (2026-10-14).* (after DESIGN) Average {Work Item Age}, like the total, has two values per side: the window average (mean
  over the window's days of total age ÷ {WIP}, days without {WIP} left out) and the last day's value.
- **D49** (maintainer 2026-10-14, after DISTILL sketch review) No counts beyond Then's limits, on any panel ("start
  simple"); supersedes D40, the beyond-limits parts of M1–M4 and D47, and D45's three limit reasons (Q8, too few
  points, collapsed band). Then no longer freezes XmR average and limits. A process-behaviour entry may come later;
  Signals (Epic 5935) owns "is this a real change".
- **D50** (maintainer 2026-10-14, after DISTILL sketch review) {WIP}: Average, Median, Min and Max of the window's
  daily {WIP}, Then and Now, rows "Average N", "Median N", "Min N", "Max N", each with its own change and % change,
  like the {Cycle Time} percentiles. Was: "Average 9 (6–12)" with one change.
- **D51** (maintainer 2026-10-14, after DISTILL sketch review) {Work Item Age} shows the total age only, in a panel
  titled "Total {Work Item Age}": Average, Median, Min and Max of the window's daily total age (sum of the ages of
  every in-progress {Work Item} that day), rows "Average N days" … "Max N days", each with its own change and %
  change; growing age reads worse. No per-item average age, no last-day rows; supersedes D48 and Q10. Nothing in
  progress = total age 0, a real 0 (no dash), as {WIP} 0 is.
- **D52** (maintainer 2026-10-14, after DISTILL sketch review; records DT14) Summary header between breadcrumb and
  first panel: each side's window, its length and "N {Work Items} closed"; panel side headings read just THEN / NOW
  (no "· N {Work Items}", no "· N days"). More header content only after a maintainer-seen sketch. Slice 03 (#6161)
  is this header plus a behaviour-preserving refactor already done. *(Extended by D53, 2026-10-14: a third segment,
  CHANGE.)*
- **D53** (maintainer 2026-10-14, after the slice-04 live check; extends D52) The summary header gains a third
  segment, CHANGE, matching the panels' THEN | NOW | CHANGE. Per **shown metric** (not per line), judged by the
  panels' own better / worse / no-change marks on that metric's lines (direction of good as in D39: lower is better
  for {Cycle Time}, {WIP}, {Work Item Age}; higher for {Throughput}): **✔ Improved** ≥ 1 line better, none worse ·
  **✘ Got worse** ≥ 1 worse, none better · **± Mixed** ≥ 1 better and ≥ 1 worse · **– Unchanged** no line changed.
  Groups always in that order; an empty group is left out; the metrics of one group share a line ("✔ Improved:
  {Throughput}, {WIP}"), named as the panel titles in the instance's Terminology ("Total {Work Item Age}"). A line with
  a dash on either side is ignored; a metric whose lines are all dashes is not listed; only shown metrics count. No
  weighting, no threshold: a 1-day move counts. Colour backs the marks (green, red, neutral, neutral); mark and word
  carry the meaning. Why: per-metric counts scale as panels are added; "Mixed" is honest when the 85th worsens while
  the 50th improves (the spread widened), where a majority rule would hide the tail forecasts lean on. "The 85th
  leads" is a possible later refinement only if Mixed proves too common. Sketch approved by the maintainer (in
  `feature-delta.md`, D53). Slice 05b.
- **D54** (maintainer 2026-10-14, after the slice-04 live check; extends M1 and DT9) The {Cycle Time} panel adds the
  95th percentile: Then freezes it with the 85th, 70th and 50th; Now computes it; its line carries the `ForecastLevel`
  icon like the others (DT10), has its own change line and feeds D53. A report frozen before D54 reads "Not captured
  for this report" for Then's 95th (D27). The backend already computes it (`BaseMetricsService.BuildPercentiles`:
  50 / 70 / 85 / 95); it was never decided against, only not requested at DT9. Slice 05b.
- **D42** (Q5) Reports are deleted with their Team/Portfolio; its delete confirmation names "and its N reports".
- **D43** (Q6) Now length set at creation (default = Then length; same presets + custom), saved on the report, not
  in the address; viewers cannot change it; editors can, stored for everyone. Scheduled sending (5882) needs it.
- **D44** (Q7) "Edit report" dialog *(— superseded by DT4 (2026-10-13): the Create wizard in edit mode, Type and
  Then locked)* for editors: name, Now length, shown metrics. Then is never editable.

## Requirements Summary

- **Who**: `flow-coach` — consulting (Elena Kovacs, Team Lightspeed, engagement from 1 Aug 2026), short assessment
  (Dev Malhotra, Team Gravity, ends 2 Oct 2026) and internal (Priya Raman, Team Gravity, WIP limit from 14 Sep 2026);
  reader `delivery-lead-rte` (Martin Achterberg, Viewer).
- **What**: a Reports tab on Teams and Portfolios; a "Then & Now" report that freezes a Then window (picked end date,
  preset or custom length, inside the data cutoff) and sets a saved-length rolling Now beside it, one panel per
  metric: Then and Now values, change, % change, and Now's points beyond Then's frozen limits *(dropped by D49,
  2026-10-14; a summary header with each side's window and closed count instead, D52)*, coloured by each
  metric's direction of good. No verdicts; people interpret.
- **Slices (order = priority)**: 01 Reports tab + Team report + {Cycle Time} panel (WS) · 02 past end date + custom
  length · 03 summary header (D52; *was* {Cycle Time} points beyond Then's limits, dropped by D49) · 04 metric picker
  + {Throughput} panel · 05 {WIP} + Total {Work Item Age} panels (Average, Median, Min, Max; D50, D51) · 05b the header's CHANGE summary + the {Cycle Time} 95th (D53, D54; added 2026-10-14) · 06 Portfolio · 07 Now length at creation + days since · 08 delete + cap + lapse + owner deletion ·
  ~~09 settings notice~~ dropped (D46) · 10 Edit report · ~~11 claim chart~~ dropped.
- **KPIs**: north star K2 (≥ 40% of report-creating instances return to Reports ≥ 7 days later); K1 activation
  ≥ 10% in 90 days; ~~K3 reports saved with a Now shorter than Then~~ (dropped, maintainer 2026-10-03); K4 cap hits
  (land-and-expand learning; measured only as an upper-bound proxy); K5 qualitative (2 consultants show it to management); guardrails G1 (re-roll deletes ≤ 20%), G2 (≤ 2 s
  open, ≤ 10 s create), G3 (never 0 for absent data). Re-checked against D39: none counted verdicts.

## Constraints Established

- RBAC: create/edit/delete = `TeamWrite`/`PortfolioWrite`; view = Read; UI via `useRbac()` only.
- Then limits come from Then, frozen unrounded (`XmRResult` rounds), never from the owner's PBC Baseline setting.
  *(Superseded by D49, 2026-10-14: no limits are frozen or shown.)*
- The direction of good is a fixed frontend panel property; never stored or computed in the backend.
- No synthesised values; every value shows its sample size; "—" with a reason only when truly empty (D45).
- One expand-only migration in slice 01 via `CreateMigration`, all providers; cascade with the owner.
- Terminology tokens for every metric word; no "Baseline", "Epic", "Story", "Initiative"; no verdict words.
- NFR: create ≤ 10 s on 365 days of history; open ≤ 2 s; Metrics tab unchanged. Added 2026-10-13: meaning never by
  colour alone (sign and number always carry it) and a keyboard-operable wizard; a stale edit is refused and the user
  told to reload (DT4); report volume per owner — Community 2, Premium unlimited, so the list handles many rows.
- Earliest allowed Then start = today − `DoneItemsCutoffDays` (2 Oct 2026, 365 → 2 Oct 2025), a bound on the start
  date independent of the inclusive window-length count (D21).
- Maintainer-accepted risks: DISCOVER G1–G3 failed (D2) — problems are clear but not validated, the spreadsheet
  workaround is assumed; no settings-changed notice (D46) against O5, guardrail G4.
- Out of scope: CLI/MCP, export, Delivery level, cross-owner reports, verdicts, rules/thresholds, narrative, charts,
  author note (later), viewer-changeable Now, editing Then, nice-to-have metrics, a settings-changed notice (D46), a
  {Throughput} weekly median (D47), counts beyond Then's limits (D49), a per-item average {Work Item Age} and
  last-day values (D51); in the CHANGE summary, weighting, thresholds, a majority rule and "the 85th leads" (D53);
  any {Cycle Time} percentile beyond the 95th (D54).

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
from a Then of 0 shows "Then was 0" in place of the % · Q10 total {Work Item Age} shows both the window average of the daily
totals and the actual total on the window's last day.

**Resolved 2026-10-03 (DESIGN's decisions to confirm)**: thin data → D45 · settings notice → D46 (dropped) ·
{Throughput} → D47 · average {Work Item Age} → D48.

**Decided 2026-10-14 (after the DISTILL sketch review)**: D49–D52 (above). Q8 is superseded by D49 (no below-count);
Q10 by D51. **Open**: whether "days since Then ended" (slice 07, D35) still joins the summary header — per D52 it
needs a sketch the maintainer has seen first.

**Decided 2026-10-14 (after the slice-04 live check)**: D53, D54 (above). **Provisional, to confirm with a sketch at
the start of DISTILL (Q11)**: (a) a CHANGE segment with no metric to list reads "—"; (b) metrics inside a group in
panel order; (c) "Not captured for this report" ignored like a dash; (d) the 95th row above the 85th; (e) a long
group wraps under its mark on a narrow page.

## Risks carried forward

R-A sponsors may read a red panel without context — there is no narrative to soften it, by design; the author note
(later) is the answer · ~~R-C/R-D low counts and the zero clamp make the beyond-limits count mute on per-item data
(tested first, slice 03; Q8)~~ (retired by D49, 2026-10-14) · R-E evidence base (DISCOVER G1–G3 failed; interview protocol still owed by the
maintainer, K5) · demo history is short; screenshots may need a CSV adjustment (checked in slice 03) · without
verdicts the differentiation against ValueFlow rests on the frozen Then and the count against frozen limits *(since
D49, 2026-10-14: on the frozen Then alone)*.
- **D55** (maintainer 2026-10-14) Community allows 2 reports per owner **per report type** (was 2 across every type, D36); Premium unlimited; built in slice 08.
- **D57** (maintainer 2026-10-14) Community allows exactly **1** report per owner per report type (supersedes D55's 2); Premium unlimited; slice 08 DISTILL reworks its tests and copy.

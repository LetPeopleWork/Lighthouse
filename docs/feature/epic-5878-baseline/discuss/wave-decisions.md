# Wave Decisions — DISCUSS — epic-5878-baseline

**Agent**: Luna (`nw-product-owner`) · **Date**: 2026-10-02 · **Mode**: autonomous subagent; the maintainer was not
available mid-run, so open questions carry provisional assumptions.
**Predecessor**: DIVERGE (Option 3 "Claims checklist"; D8–D16, DV-1..DV-9 and the 2026-10-02 maintainer resolutions
settled, not re-opened). **Successor**: DESIGN (`nw-solution-architect`). Peer review (`nw-product-owner-reviewer`) is
dispatched by the coordinator.

Full text of everything below: `../feature-delta.md` → "Wave: DISCUSS" sections. Slice briefs: `../slices/`.

## Config (given, not asked)

feature_type user-facing (full stack) · walking skeleton brownfield · research depth comprehensive, reusing DIVERGE ·
JTBD light (job reused, not re-derived) · density lean.

## Scope Assessment: PASS — 11 stories, 1 new module (reports + claim catalog) on 3 existing ones (metrics, licence, RBAC), estimated ~10 days

At the boundary: story count (11 > 10) and effort (~2 weeks) fire marginally; one user outcome, nothing ships
without the Reports tab. **Provisional proposal, no Epic split**: keep everything in #5878, cut the release after
slice 09; slices 10 and 11 are cancellable follow-ups.

## Key Decisions

- **D17** Every story carries `job_id: job-flow-coach-show-whether-flow-changed`; no new job or persona.
- **D18** Reports tab after Metrics on Team (after Refinement once epic-5510 lands) and Portfolio; always enabled;
  role-specific empty state.
- **D19** Create flow: template card → Then window → claims (C1–C4 preselected) → defaulted name. Rename later out of
  v1 (provisional, Q7).
- **D20** Then lengths = Metrics presets ≥ 14 days (Team 14/30/90, Portfolio 30/90/180); calendar days, inclusive,
  instance time zone (provisional, Q1).
- **D21** Then validation: end ≤ today, whole window inside `DoneItemsCutoffDays`; refusal names the earliest start.
- **D22** Now: rolling, default = Then length, per-view preset choice in the address, not saved (provisional, Q6).
- **D23** Now never reaches back into Then; a report frozen today reads No change yet (provisional, Q2).
- **D24** Verdicts: "trending" Holds only on a sustained signal (2 of 3, 4 of 5, run of 8) on the good side; any
  signal on the bad side = Does not hold; lone good-side point = No change yet, named (provisional, Q3).
- **D25** Not enough data: ≥ 8 finished {Work Items} in Then and ≥ 1 in Now for C1/C5; daily claims need a non-zero
  Then series; C6 uses the percentile guard; "—" with a reason, never 0 (provisional numbers, Q4).
- **D26** Frozen at creation for every applicable claim: Then series, shown values, Then signal state, readiness; plus
  window, template, selection, settings snapshot. Never recomputed; all-or-nothing creation.
- **D27** Claims added later: "Not captured for this report".
- **D28** Zero-clamp disclosure text on downward claims.
- **D29** Cap 2 per owner across all templates (Community), Premium unlimited, lapse keeps all readable/deletable;
  reports go with their owner (provisional, Q5).
- **D30** Delete for editors, confirmed; warns when Then can no longer be rebuilt.
- **D31** Settings-changed notice over types, state mapping, query, blackout days; one header line.
- **D32** Words: "Reports", "Then & Now", "Then", "Now"; no "Baseline"; no new Terminology key.
- **D33** Usage data via new route keys + name-only candidates (DEVOPS designs).
- **D34** Slices 01–07 safe on trunk before the cap; release notes/docs/website wait for slice 08.
- **D35** No intervention-date field; header shows days since Then ended.

## Requirements Summary

- **Who**: `flow-coach` — consulting (Elena Kovacs, Team Lightspeed, engagement from 1 Aug 2026) and internal (Priya
  Raman, Team Gravity, WIP limit from 14 Sep 2026); reader `delivery-lead-rte` (Martin Achterberg, Viewer).
- **What**: a Reports tab on Teams and Portfolios; a "Then & Now" report that freezes a Then window (ending today or
  in the past, inside the data cutoff) and judges a rolling Now against it with claims C1–C6 and four verdicts.
- **Slices (order = priority)**: 01 Reports tab + Team report + {Cycle Time} values (WS) · 02 past-dated Then ·
  03 C1 verdict + Now after Then · 04 C2–C4 + picker · 05 C5 + C6 · 06 Portfolio · 07 Now window + days since ·
  08 delete + cap + lapse · 09 settings notice · 10 change shown claims · 11 claim chart.
- **KPIs**: north star K2 (≥ 40% of report-creating instances return to Reports ≥ 7 days later); K1 activation
  ≥ 10% in 90 days; K4 cap hits (land-and-expand learning); K5 qualitative (2 consultants show it to management);
  guardrails G1 (re-roll deletes ≤ 20%), G2 (≤ 2 s open, ≤ 10 s create), G3 (never 0 for absent data).

## Constraints Established

- RBAC: create/delete/edit-claims = `TeamWrite`/`PortfolioWrite`; view = Read; UI via `useRbac()` only.
- Then limits come from the frozen Then series, never from the owner's PBC Baseline setting.
- `XmRResult` rounds average and limits to integers → freeze the Then series, recompute limits from it.
- No synthesised values; absent data is "—" with a reason.
- One expand-only migration in slice 01 via `CreateMigration`, all providers; cascade with the owner.
- Terminology tokens for every metric word; no "Baseline", "Epic", "Story", "Initiative".
- NFR: create ≤ 10 s on 365 days of history; open ≤ 2 s; Metrics tab unchanged.
- Out of scope: CLI/MCP, export, Delivery level, cross-owner reports, per-report rule tuning, nice-to-have claims.

## Upstream Changes

DISCOVER documents untouched; see `feature-delta.md` → "Changed Assumptions":

- DISCOVER V4 ("no previous-period comparison in the codebase") is partly contradicted: Story 5914's step-back and
  previous-period widget comparisons exist; none freezes, so differentiation holds.
- DISCOVER journey's intervention date and "read without Lighthouse open" replaced by the Then end date and in-app
  reading (D35, D13).
- DIVERGE's "freeze limits" extended to "freeze the series" (S3); DV-3's "≥ 1 special cause" narrowed to sustained
  signals for Holds (provisional, Q3).

SSOT updates:

- `docs/product/journeys/epic-5878-baseline.yaml`: both journeys re-pointed to
  `job-flow-coach-show-whether-flow-changed`, steps aligned with D-decisions, reader step added, shared artifacts
  rewritten, changelog entry. Parses cleanly as YAML (checked 2026-10-02).
- `docs/product/jobs.yaml`: unchanged — the DIVERGE job already matches this wave.

## Open questions for the maintainer (provisional assumptions applied)

Q1 Then lengths presets only (D20) · Q2 Now clipped after Then (D23) · Q3 sustained signal for Holds (D24) ·
Q4 ≥ 8 finished {Work Items} in Then (D25) · Q5 reports deleted with their owner (D29) · Q6 Now choice per view
(D22) · Q7 no rename in v1 (D19). Q2–Q4 must be confirmed before slice 03 is accepted.

## Risks carried forward

R-A sponsors read "No change yet" as evasive (C6 and the values on the row mitigate) · R-C/R-D low counts and the
zero clamp (tested first, slice 03) · R-E evidence base (DISCOVER G1–G3 failed; interview protocol still owed by
the maintainer, K5) · demo history is short, so screenshots need a CSV adjustment (slice 03).

# Outcome KPIs — remove-action-buttons (ADO #5077)

## Feature: remove-action-buttons

### Objective

By the end of the feature, every settings and forecast surface in Lighthouse obeys one
interaction law — "valid input is acted on immediately; no ceremonial Save/Run button" —
and the two stopgap "you must refresh" Alerts that exist only because this work was
pending are deleted.

### Honest framing (telemetry constraint)

Lighthouse customer instances do not phone home (self-hosted telemetry gap, Epic 5015 —
no timeline). Classic adoption KPIs ("admins lose fewer edits", "forecasters explore
more scenarios/session") are the TRUE outcomes but are NOT instrumentable cross-instance
today. They are recorded as **aspirational, gated on Epic 5015**. The committed KPIs
below are in-repo verifiable proxies (test + grep) plus one usability target measured by
moderated walkthrough.

### Outcome KPIs

| # | Who | Does What | By How Much | Baseline | Measured By | Type |
|---|-----|-----------|-------------|----------|-------------|------|
| 1 | team/portfolio admins & forecasters | interact with surfaces that act on valid input with no Save/Run button | 0 of 6 surfaces still require an explicit Save/Run click for valid input | 6 of 6 require a click today | repo test asserting no Save/Run button on each converted surface + grep | Leading (committed) |
| 2 | the product | retains stopgap "you must refresh" Alerts | 0 stopgap Alerts remain (both removed) | 2 (`forecast-filter-takeeffect-hint` + StateMappings "must reload") | grep for the two data-testids/strings returns nothing; test asserts absence | Leading (committed) |
| 3 | admins/forecasters | experience consistent valid-input-acts-immediately behaviour across all 6 surfaces (parity with the shipped How Many/When forecast) | 6 of 6 surfaces exhibit the behaviour | 1 of 6 today (only How Many/When) | per-surface acceptance test asserting auto-save/auto-run + save-state/live-result | Leading (committed) |
| 4 | a user who makes an invalid edit | notices the problem without a Save button to click | ≥ 90% of moderated-walkthrough participants correctly identify the invalid field within 5s, with no Save button present | not measured (Save button exists today) | moderated usability walkthrough on Slice 1 (5-8 internal participants) | Leading (committed, usability) |
| 5 | team/portfolio admins | lose fewer valid-but-unsaved edits (navigate away without saving) | aspirational target: −50% lost-edit incidents | unknown (not instrumented) | Epic 5015 opt-in telemetry (BLOCKED) | Lagging (aspirational) |
| 6 | forecasters | explore more forecast scenarios per session | aspirational target: +30% scenario recomputes/session | unknown (not instrumented) | Epic 5015 opt-in telemetry (BLOCKED) | Lagging (aspirational) |

### Metric Hierarchy

- **North Star (committed):** KPI 1 — surfaces still requiring an explicit Save/Run click
  for valid input, driven to 0/6.
- **Leading Indicators:** KPI 2 (stopgap Alerts removed), KPI 3 (interaction consistency).
- **Guardrail Metrics (must NOT degrade):**
  - No half-typed / invalid state is ever persisted (auto-save fires only on `formValid`).
  - RBAC parity: auto-save fires nowhere the Save button was disabled (no new write surface).
  - No forecast run fires on mount; stale runs never overwrite fresh (reuse shipped guards).
  - No API contract / DTO change (frontend interaction change only).

### Measurement Plan

| KPI | Data Source | Collection Method | Frequency | Owner |
|-----|------------|-------------------|-----------|-------|
| 1 | repo tests + grep | CI assertion per converted surface | per slice / per release | DELIVER |
| 2 | repo grep + test | CI assertion of Alert absence | Slices 2 & 3 | DELIVER |
| 3 | acceptance tests | per-surface auto-save/auto-run test | per slice | DISTILL/DELIVER |
| 4 | moderated walkthrough | 5-8 internal participants on Slice 1 | once, post-Slice-1 | Luna / DESIGN |
| 5, 6 | Epic 5015 telemetry | opt-in instrumentation | BLOCKED on Epic 5015 | platform-architect |

### Hypothesis

We believe that replacing ceremonial Save/Run buttons with debounced auto-save / auto-run
(gated on validity and RBAC, reassured by a calm save-state indicator and inline-error-as-
primary-feedback) for delivery forecasters and team/portfolio admins will achieve one
uniform interaction law and retire both stopgap Alerts. We will know this is true when
0 of 6 surfaces require an explicit Save/Run click for valid input, both stopgap Alerts
are gone, and ≥90% of walkthrough participants notice an invalid field within 5s without
a Save button.

### Handoff to DEVOPS

Instrumentation for KPIs 5 & 6 (lost-edit incidents, scenario recomputes/session) is the
only DEVOPS dependency — and it is BLOCKED on Epic 5015 opt-in telemetry. No instrumentation
is required to ship Slices 1-6; the committed KPIs (1-4) are repo/test/walkthrough verifiable.

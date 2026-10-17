# Wave Decisions — DISCUSS — epic-6298-report-pdf-export

**Agent**: Luna (`nw-product-owner`) · **Date**: 2026-10-17 · **Mode**: autonomous subagent; the maintainer was not
reachable from the session, so the maintainer's decisions carry a proposed default marked PROPOSED and are listed as
Q1–Q6. **Predecessor**: DIVERGE (Option 4 "Browser-printed PDF"; DV-1..DV-9) and the maintainer's 2026-10-17
decisions on it plus the Linux print probe — taken as locked. **Successor**: DESIGN (`nw-solution-architect`).
Per-wave peer review skipped by instruction; the consolidated review runs at the end of DISTILL.

Full text: `../feature-delta.md` → "Wave: DISCUSS" sections. Slice briefs: `../slices/`. Journey SSOT:
`docs/product/journeys/epic-6298-report-pdf-export.yaml`.

## Config (given, not asked)

feature_type user-facing · walking skeleton "depends" → brownfield, slice 01 · research depth lightweight · JTBD
reused (`job-share-numbers-outside-lighthouse`), refined not re-run · density lean.

## Scope Assessment: PASS — 5 stories, 2 code bases (Lighthouse frontend, lighthouse-clients), no new bounded context, estimated ~4 days

Two user outcomes (PDF; assistant dashboard) kept in one Epic by the maintainer's choice (D18); ordered, not
interleaved; the skill pair builds only on finished slices.

## Key Decisions

Locked (maintainer, 2026-10-17):

- **D10** Community PDF with LetPeopleWork and Lighthouse branding; branding removal or replacement is a later Premium
  extra, not v1.
- **D11** First payload Then & Now (X2 gap accepted).
- **D12** Standalone may be left out where it saves cost; browser print covers it; Linux proven, macOS checked in
  slice 01.
- **D13** Aggregates plus owner and report name; header as recommended. No confidentiality line (PROPOSED part).
- **D14** Anyone who can read the report may export.
- **D15** On demand, downloaded, never stored; anonymising deferred; no migration.
- **D16** Own print layout, small cross-browser differences acceptable. Requirement for #5882 (not built): a server
  render must be pixel-comparable to the Chromium print of the same view, so the print view is loadable on its own.
- **D17** Light theme, no app chrome, own header and branding footer, marks independent of system fonts, no
  near-empty last page; the Export action calls print itself.
- **D18** The report skill is in scope, shippable alone.

This wave:

- **D19** Produced = when the printed values were read; Export re-reads first (PROPOSED default).
- **D20** Export PDF on the report view only, for every reader (PROPOSED; sketched at DISTILL).
- **D21** "How to read this" as its own thin slice (03).
- **D22** Fonts ship with the app (PROPOSED, Q4).
- **D23** Read-only report access in MCP and `lh` (PROPOSED, Q1).
- **D24** Skill name `lighthouse-reports` (PROPOSED, Q6).
- **D25** Skill rules: exact values, no rebuilt Then, dashes kept, instance terms, dated header, attribution, own
  comments apart (PROPOSED, Q3).
- **D26** Order 01 → 02 → 03 → 04 → 05 (PROPOSED, Q5).
- **D27** Usage data wanted: one export event (DEVOPS designs; counts the choice, not a saved file); client reads
  possibly via the web's `ReportOpened` with the client source.
- **D28** macOS Standalone fallback if print does nothing: visible but disabled with a reason (PROPOSED, Q2).

## Slices

| # | Slice | Est. | Ships alone |
|---|---|---|---|
| 01 | Export a Then & Now report as a PDF (walking skeleton; Team and Portfolio) | 1d | yes |
| 02 | The PDF reads the same on every machine (marks, fonts, page breaks) | 1d | yes |
| 03 | The PDF explains itself ("how to read this") | ½d | yes |
| 04 | Read reports through the Lighthouse clients (conditional on Q1) | 1d | yes |
| 05 | `lighthouse-reports` skill | ½d | yes (on finished 04) |

## Requirements Summary

- One Export PDF action on every report for any reader; a fresh read; the browser's or Standalone webview's print.
- A print-designed, light, chrome-free page: header (owner, report, Then and Now windows, produced date and time),
  summary and panels as on screen, branding footer; later a "how to read this" line.
- Same look on every machine and offline; no split panels, no footer-only page; A4 and Letter.
- Reports readable through MCP and `lh` (read-only); a skill that renders a Then & Now faithfully.
- NFRs: dialog ≤ 2 s after the click; keyboard-operable; selectable text; never colour alone; ≤ 1 MB growth.
- KPIs: K1 ≥ 20% of report-opening instances export (north star); K2 ≥ 30% of exporters repeat in another week;
  K3 cold-reader check ≥ 2 of 3; K4 ≥ 5 instances read reports through the clients; K5 skill evals.

## Constraints Established

Community, no licence check · Read-only RBAC, `useRbac()` gating · nothing stored · values equal to the screen at
export · instance terminology · no {Work Item} titles or person names · no server rendering in this Epic · the
report itself stays verdict-free.

## Upstream Changes

None to DISCOVER or DIVERGE documents (they are not edited). Superseded for DESIGN onward:

- DISCOVER D4 (Premium until extras) and DV-6's Premium un-branding → D10 (Community, branded, no un-branding in v1).
- DISCOVER journey failure modes "export unavailable on this licence" and "rendering unavailable on this host" →
  removed (journey YAML refined).
- DV-9 and DISCOVER's "Adjacent opportunity (out of scope)" → in scope, slices 04–05 (D18).
- Epic 5878 D15 (no CLI/MCP for reports) → reading reopened, if Q1 = (a) (D23). Epic 5878's documents are not edited.
- The probe's "Quicksand embedded" holds only online (S5) → D22.
- ADO Epic #6298 still tagged `Premium` → retag is the maintainer's call.

## Changed Assumptions

See `../feature-delta.md` → "Wave: DISCUSS / [REF] Changed Assumptions" (six rows).

## Questions for the maintainer

Q1 how the skill gets report data (recommend: read-only client tools, slice 04) · Q2 macOS Standalone fallback
(recommend: check in 01; disabled with a reason if needed) · Q3 assistant's own view on a dashboard (recommend:
allowed, labelled, apart) · Q4 ship the typeface with the app (recommend: whole app, slice 02) · Q5 slice order
(recommend: PDF first) · Q6 skill name (recommend: `lighthouse-reports`). Details and options in the feature delta.

## DoR

PASSED with item 1 waived (clear, not validated; maintainer-accepted risk D2). All five stories carry an Elevator
Pitch and `job_id: job-share-numbers-outside-lighthouse`.

## Wave: DISCUSS / [REF] Maintainer answers (2026-10-17)

These settle the questions above. Every PROPOSED default they touch is now decided.

| Q | Answer |
|---|---|
| Q1 How the skill gets a report's data | **(a) Read-only client tools in slice 04** (MCP and `lh`, Teams and Portfolios). The skill falls back to values the user pastes in. This reopens Epic 5878's "no reports in the clients" decision, for reading only. |
| Q2 macOS Standalone | **Assume printing works there and confirm it in production.** Only released macOS builds exist, with no devtools (a release Tauri webview is not inspectable), so there is no way to test before slice 01 ships the button. If it fails, the button is disabled or changed in a follow-up. The maintainer checks it once 01 is released. |
| Q3 The assistant's own reading | **Yes, kept apart.** It goes under its own labelled heading; the report's numbers stay exactly as Lighthouse shows them. |
| Q4 Lighthouse font | **Bundle Quicksand for the whole app** (slice 02, under 1 MB), with this condition: Quicksand is under the SIL Open Font License 1.1 (Google Fonts keeps it under `ofl/quicksand`). The licence allows bundling and redistributing it with software, commercial use included, as long as the licence text (`OFL.txt`) ships with the font, the font is not sold on its own, and it is not modified under its reserved name. DESIGN confirms the licence against the actual font files, and slice 02 ships `OFL.txt`. |
| Q5 Slice order | **01 → 02 → 03 → 04 → 05.** |
| Q6 Skill name | **`lighthouse-reports`**, so the zip is `lighthouse-reports-skill.zip`. |
| ADO `Premium` tag on Epic #6298 | **The maintainer changes it.** Epic fields and states are never touched from a session. |

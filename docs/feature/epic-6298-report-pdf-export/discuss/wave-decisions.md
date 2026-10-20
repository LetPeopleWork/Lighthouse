# Wave Decisions — DISCUSS — epic-6298-report-pdf-export

**Agent**: Luna (`nw-product-owner`) · **Date**: 2026-10-17 · **Mode**: autonomous subagent; the maintainer was not
reachable from the session, so the maintainer's decisions carry a proposed default marked PROPOSED and are listed as
Q1–Q6. **Predecessor**: DIVERGE (Option 4 "Browser-printed PDF"; DV-1..DV-9) and the maintainer's 2026-10-17
decisions on it plus the Linux print probe — taken as locked. **Successor**: DESIGN (`nw-solution-architect`).
Per-wave peer review skipped by instruction; the consolidated review runs at the end of DISTILL.

> **Revised after review 2026-10-19.** Applied DESIGN's upstream changes U-1..U-15, DEVOPS's DU-7 and the DISTILL UI
> walk-through (NMD-1..7, locked): example Now window "2 Aug – 30 Oct 2026" (90 days); the Reports list PDF icon in
> slice 01 (D20 superseded by NMD-2; slice 01 ~1.5d); D28 superseded by the Q2 answer and NMD-7 (no disabled state in
> slice 01); export-time failure with Retry (PD-1) and Back destinations added to US-01; two-line header and footer
> copy (NMD-3); three "How to read this" lines (NMD-5); raw client values plus consent-gated `ReportOpened` from the
> clients (slice 04, ~1d holds); the skill carries the web's presentation rules (slice 05, ~1d, risk HIGH); KPIs
> measured as per-browser / per-client proxies, K6 added. PD-1..PD-5 stay **confirmed by the maintainer on 2026-10-20**.

Full text: `../feature-delta.md` → "Wave: DISCUSS" sections. Slice briefs: `../slices/`. Journey SSOT:
`docs/product/journeys/epic-6298-report-pdf-export.yaml`.

## Config (given, not asked)

feature_type user-facing · walking skeleton "depends" → brownfield, slice 01 · research depth lightweight · JTBD
reused (`job-share-numbers-outside-lighthouse`), refined not re-run · density lean.

## Scope Assessment: PASS — 5 stories, 2 code bases (Lighthouse frontend, lighthouse-clients), no new bounded context, estimated ~5 days (rev. 2026-10-19, was ~4)

Two user outcomes (PDF; assistant dashboard) kept in one Epic by the maintainer's choice (D18); ordered, not
interleaved; the skill pair builds only on finished slices.

## Key Decisions

Locked (maintainer, 2026-10-17):

- **D10** Community PDF with LetPeopleWork and Lighthouse branding; branding removal or replacement is a later Premium
  extra, not v1.
- **D11** First payload Then & Now (X2 gap accepted).
- **D12** Standalone may be left out where it saves cost; browser print covers it; Linux proven, macOS assumed to
  work and checked by the maintainer after slice 01 is released (Q2 answer; NMD-7 if it fails).
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
- **D20** Export PDF on the report view, for every reader. "Report view only" is **superseded by NMD-2**
  (2026-10-19): a PDF icon also goes first in the Reports list's Actions column, for every reader, in slice 01.
- **D21** "How to read this" as its own thin slice (03).
- **D22** Fonts ship with the app (PROPOSED, Q4).
- **D23** Read-only report access in MCP and `lh` (PROPOSED, Q1).
- **D24** Skill name `lighthouse-reports` (PROPOSED, Q6).
- **D25** Skill rules: exact values, no rebuilt Then, dashes kept, instance terms, dated header, attribution, own
  comments apart (PROPOSED, Q3).
- **D26** Order 01 → 02 → 03 → 04 → 05 (PROPOSED, Q5).
- **D27** Usage data wanted: one export event (DEVOPS designs; counts the choice, not a saved file); client reads
  possibly via the web's `ReportOpened` with the client source.
- **D28** ~~macOS Standalone fallback: visible but disabled with a reason~~ — **superseded by the Q2 answer and
  NMD-7**: no disabled state in slice 01; a follow-up only if the maintainer's post-release check fails.

## Slices

| # | Slice | Est. | Ships alone |
|---|---|---|---|
| 01 | Export a Then & Now report as a PDF from the report view or the Reports list icon (walking skeleton; Team and Portfolio) | ~1.5d (was 1d) | yes |
| 02 | The PDF reads the same on every machine (marks, fonts, page breaks) | 1d | yes |
| 03 | The PDF explains itself ("how to read this") | ½d | yes |
| 04 | Read reports through the Lighthouse clients (raw values; `ReportOpened` after consent) | ~1d | yes |
| 05 | `lighthouse-reports` skill (carries the web's presentation rules; risk HIGH) | ~1d (was ½d) | yes (on finished 04) |

## Requirements Summary

- Export PDF on every report view and as an icon in the Reports list, for any reader; a fresh read on a print page of
  its own with preparing and failed looks (Retry); the browser's or Standalone webview's print; back to where it
  started.
- A print-designed, light, chrome-free page: header (owner, report, Then and Now windows, produced date and time),
  summary and panels as on screen, branding footer; later three "how to read this" lines.
- Same look on every machine and offline; no split panels, no footer-only page; A4 and Letter.
- Reports readable through MCP and `lh` (read-only); a skill that renders a Then & Now faithfully.
- NFRs: dialog ≤ 2 s after the click; keyboard-operable; selectable text; never colour alone; ≤ 1 MB growth.
- KPIs: K1 ≥ 20% of report-opening instances export (north star); K2 ≥ 30% of exporters repeat in another week;
  K3 cold-reader check ≥ 2 of 3; K4 ≥ 5 instances read reports through the clients; K5 skill evals; K6 exported
  report types (descriptive, added at DEVOPS). K1, K2 and K6 are measured per browser and K4 per client identifier —
  proxies for instances; targets unchanged.

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
- Epic 5878 D15 (no CLI/MCP for reports) → reading reopened (Q1 answered (a), D23). Epic 5878's documents are not edited.
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

(rev. 2026-10-19) **7 PASS + 1 accepted waiver (D2)** on the eight-item gate (item 1 waived: clear, not validated);
the ninth item, outcome KPIs, also passes. US-01 re-checked for size: ~1.5d, 7 scenarios. All five stories carry an Elevator
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

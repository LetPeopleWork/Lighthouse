# Wave Decisions — epic-6298-report-pdf-export / DISCOVER

**Agent**: Scout (`nw-product-discoverer`) · **Date**: 2026-10-17 · **Mode**: subagent, autonomous, light
DISCOVER at the maintainer's request. The maintainer answered the interview on 2026-10-17 about their users' past
behaviour; no direct customer interviews this round.
**Predecessor**: none. **Successor**: DIVERGE (`nw-diverger`). The full narrative is in `../feature-delta.md`.

## Sources

**Independence**: S-A, S-B and S-F all come from **one informant, the maintainer**. S-A and S-B are two dated
backlog records the maintainer wrote; S-F is the maintainer's interview. The only customer-originated signal is the
reference-customer observation S-A relays second-hand (screenshots exported by hand).

| Ref | Source |
|---|---|
| S-A | ADO #1822 *Project Reports* (created by the maintainer 2025-03-02, Removed). Verbatim description: "To support project updates, it may be a nice feature to allow to \"export\" some reports from Lighthouse directly into PDF (e.g. with the latest numbers and some charts). We have seen from reference customers that they manually export screenshots to track thing - may be nice to support this... Also we got feature requests to \"export forecasts to csv\" so we can" [sic, truncated in source] |
| S-B | ADO #4753 *Export to PDF* (created by the maintainer 2026-04-08, Premium, Removed 2026-08-21). Verbatim description: "Add \"Print to PDF\" functionality, so that you can: - Export all metrics and they look clean - Have an overview section on top for an overview (e.g. with Team name, last update, sle, wip etc.) - Export Deliveries - Option: Anonymise so it can be shared? Should be premium only. Target users: - People giving us their data for a Flow Analysis? - People wanting to share their charts etc. in reports/slides?" |
| S-C | Epic 5878 *Then & Now* discovery and `docs/feature/epic-5878-baseline/recommendation.md` P4 plus dissent ("gating the management-facing PDF works against land-and-expand") |
| S-D | Epic #6298 description (maintainer 2026-10-14) |
| S-E | Spike #6052: `docs/feature/story-6052-server-side-chart-rendering/spike/findings.md` and `docs/feature/story-6052-server-side-chart-rendering/spike/wave-decisions.md` (Probe 2 in the latter supersedes the findings table's row C "DOESN'T WORK") |
| S-F | Maintainer interview 2026-10-17 |

## Decisions

- **[D1] One workspace, `epic-6298-report-pdf-export`.** Rationale: one Epic, the successor of #1822 and #4753; nothing to merge. Source: maintainer dispatch 2026-10-17.
- **[D2] Light DISCOVER; proceed to DIVERGE with G1–G3 failing and G4 not evaluated, as a maintainer-accepted risk.** Rationale: the problem signal comes from one informant (the maintainer): two dated backlog records the maintainer wrote and one interview, with one relayed customer observation (S-A). The solution space is wide (9 open questions), so DIVERGE is the cheap next step. Same pattern as Epic 5878 D2: a learning investment. Source: maintainer request 2026-10-17.
- **[D3] The PDF is a capability of a report, built once; first payload Then & Now, charts with Custom Metric Reports.** Rationale: Then & Now has frozen values and no charts, so no chart rendering is needed. Its fit to the observed job is challenged (X2) and left to DIVERGE. Source: S-D.
- **[D4] Licence, decision-in-principle: Premium until there are extras; then free PDF, Premium extras.** Rationale, in the maintainer's words: "leaning now to free PDF, premium with extras. However, as long as we don't have the extras, we may make PDF a premium thing that later on becomes free once we have more advanced stuff." Risk recorded, as an **assumption**: we have no record of moving a capability from Community to Premium; we assume it would read as a take-away. If so, the later Premium → Community flip is effectively one-way, while starting Premium keeps the option. The 5878 dissent (S-C) argues the gate itself works against land-and-expand. Source: S-F, S-C.
- **[D5] MCP plus AI dashboard recorded as a competing alternative.** Rationale: it is a present, free way to get numbers out, and it lets the user compose the view, which a fixed PDF does not. Source: S-F.
- **[D6] Product direction: reports are for sharing, the interactive charts for exploring.** Graded **stated intent** (product direction), not past behaviour. The maintainer verbatim: "the idea of reports is exactly to make this 'shareable', so later on we want reporting for different types like metrics charts optimized for sharing and not for 'playing around'. Example: in the charts now a coach wants to check different things interactively, while with a report you get a 'static' thing for... reporting, pdfs, slides, etc." Source: S-F.
- **[D7] Mechanism not chosen here.** Rationale: the spike left it open at the maintainer's request (2026-09-24); DIVERGE owns it. DISCOVER records constraints only. Source: S-E.
- **[D8] No ICE change.** Rationale: no new direct evidence; the interview consolidates what the maintainer's own #1822 and #4753 already said. Source: this wave.
- **[D9] #4753's "print the page" shape is dropped.** A decision, not evidence: the maintainer confirmed on 2026-10-17 that #4753 was removed as "superseded by Reports". Source: S-F, S-B.

## Constraints (with evidence)

| # | Constraint | Evidence |
|---|---|---|
| C1 | One delivery mechanism for every report type | S-D |
| C2 | Server zips and Docker hard targets (scheduled, no UI); Standalone in-app view plus PDF at near-zero size; Tauri kills the backend on exit | S-E findings.md |
| C3 | Helm 1 GiB has no headroom for Chromium; non-root Chromium needs `--no-sandbox` with requests restricted to own origin | S-E findings.md |
| C4 | Charts in a PDF look like the frontend's charts | S-E findings.md (maintainer 2026-09-24) |
| C5 | Fonts ship with the app (Quicksand loads from Google Fonts today) | S-E wave-decisions.md |
| C6 | A PDF says when it was produced and which windows it covers | Project rule: no value the reader cannot tell is stale |
| C7 | Configurable terms in the PDF | `TerminologySeeder.cs`; project rule |
| C8 | Export permission decided through RBAC (`useRbac()`) | Project architecture rule |
| C9 | DEVOPS designs the usage event; no report content or names in it | Project rules |
| C10 | PDF values equal on-screen values | Epic 5878 journey |

## Validated assumptions

| # | Assumption | Confidence |
|---|---|---|
| V1 | Numbers leave Lighthouse through effortful workarounds: screenshots into slides, CSV rebuilt elsewhere, MCP plus AI dashboard. One informant (the maintainer): two dated backlog records the maintainer wrote (S-A, S-B) plus the interview (S-F); S-A relays the only customer observation (screenshots) | Low-Medium |
| V2 | Readers are mixed: some will not open Lighthouse, some cannot (Standalone, no connection) (S-F, S-C) | Medium |
| V3 | Capture happens per engagement (coach/consultant), on a recurring cadence (team lead/PM) and as ad-hoc one-offs (S-F) | Low-Medium |
| V4 | A PDF is feasible in every distribution (S-E findings.md; Probe 2 in S-E wave-decisions.md supersedes the findings table's row C "DOESN'T WORK": in-process rendering came out near-identical) | Medium-High for feasibility; none for mechanism |
| V5 | Then & Now is the cheapest first payload (no charts) | High for cost; none for value |

## Invalidated assumptions (with evidence)

| # | Assumption | Evidence |
|---|---|---|
| X1 | "A PDF is what people want" (challenged) | Two of three workarounds (CSV, MCP plus AI) are about reusing or composing data, not a static picture (S-F) |
| X2 | "Then & Now serves the observed job" (challenged) | Captured today: metrics charts and forecasts/Deliveries; Then & Now is new and not captured yet (S-F) |
| X3 | "Print the whole interactive page" (#4753 shape) | The interactive page splits badly across pages (S-E findings.md). Its drop is a decision (D9), not evidence |
| X4 | "Rendering charts outside the browser is cheap" | Five effect-driven components render empty server-side; bundled browser +99–120 MB Standalone, ≈480 MB Docker disk (S-E findings.md) |
| X5 | "Premium now, free later at no cost" (challenged) | Rests on an assumption, not a record: we have never moved a capability from Community to Premium and assume it would read as a take-away, which makes the flip effectively one-way. The 5878 dissent says the gate hurts land-and-expand (D4, S-C) |

## Gate status

G1 FAIL (0 direct interviews; one informant, two backlog records the maintainer wrote, one relayed customer
observation) · G2 FAIL (no opportunity scores) · G3 FAIL (nothing tested with users) · G4 NOT EVALUATED (light
DISCOVER, no Lean Canvas). Partial signals for G4, as notes only: feasibility amber-to-green with the mechanism open;
licence a decision-in-principle; a free substitute exists (MCP plus AI); no usage event yet. Proceeding is a
maintainer-accepted risk (D2).

## Open questions for DIVERGE

1. **Mechanism**: browser print with a print stylesheet, headless Chromium, in-process Jint + HarfBuzz + Skia, or Gotenberg, under C2–C5. Is browser/Tauri print enough until Scheduled Report Delivery (#5882) needs unattended rendering?
2. **PDF vs other share forms**: slide-ready image, shareable read-only link, CSV, or reports reachable through MCP for AI dashboards, given the three observed workarounds (X1, D5).
3. **First payload**: Then & Now (cheapest, not captured today) or a chart-bearing report closer to what is captured: metrics charts, forecasts and Deliveries (X2).
4. **Licence gate timing** and which extras would justify Premium (branding, scheduled email, anonymising) (D4, X5).
5. **Downloaded vs server-stored** PDF: on-demand download, or stored for #5882 (retention, RBAC on files, migration).
6. **Snapshot honesty**: how the PDF shows generation time and windows (C6); Now recomputed at export or taken as on screen.
7. **Anonymising** (#4753): in or out, and which names it covers.
8. **Who may export**: any reader of the report, or editors only (C8).
9. **What leaves Lighthouse**: what may a PDF carry out of Lighthouse (Work Item titles, Team and person names, or aggregates only), and how is a file outside the access model marked as confidential? Linked to #4753's "Option: Anonymise so it can be shared?" (S-B) and Q7.

## Open for the maintainer (non-blocking)

1. Ask 3–5 users: "The last time you showed Lighthouse numbers to someone else, what did you send?" Ask to see the artefact; count charts, forecasts, tables, Then & Now. This tests X2 directly and adds informants beyond the maintainer.
2. Ask one Standalone user how their numbers reach anyone else today (V2, the "cannot open" reader).

## Adjacent opportunity (out of scope for this Epic)

- **A Lighthouse skill that builds dashboards from the existing report types** (maintainer, 2026-10-17). One observed
  workaround is an AI assistant generating a dashboard over the MCP connection. A dedicated skill would let AI-generated
  dashboards compose from report types we define, rather than from raw data. It would also tell us which reports and
  metrics people actually pull out. It belongs with the existing Lighthouse agent skills (`docs/feature/story-6217-lighthouse-skills`),
  not with PDF export. DIVERGE may cite it as a complement when it weighs "PDF vs other share forms",
  but it must not fold it into this Epic's scope.

**Update (maintainer, 2026-10-17): moved into scope.** The skill becomes its own slice of this Epic. It describes how
to show each report type, starting with Then & Now, the only type that exists so far. The maintainer expects it to
be low effort and wants to give it a try. It sits beside the PDF export, it does not replace it, and it must ship on
its own.

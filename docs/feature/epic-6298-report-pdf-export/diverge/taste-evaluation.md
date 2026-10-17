# Taste Evaluation — epic-6298-report-pdf-export (DIVERGE, Phase 4)

Inputs: the curated 6 in `options-raw.md`. Scoring started only after `options-raw.md` was complete.
**Revision 1 (2026-10-17)**, after peer review round 1 (`review.yaml`):
- First payloads now differ across the options. Option 5 carries a chart-bearing report; Option 6 carries a
  Delivery with its forecasts. Both were re-scored.
- Feasibility is graded on one evidence standard. Where a path is desk-checked rather than measured, the option
  scores at most 4, so Option 4 drops from 5 to 4, the same as Option 1.
- The resulting tie between Options 4 and 1 is broken by a rule stated in §4.

## 1. Weights (locked before scoring)

| Criterion | Weight | Rationale |
|---|---|---|
| DVF (average) | 30% | Default. The evidence base is thin (DISCOVER G1-G3 fail), so desirability and cost must not be swamped by taste. |
| T1 Subtraction | 20% | Default. Share features accumulate options (expiry, branding, formats). |
| T2 Concept count | 20% | Default. Two audiences: the sender, and an outside reader who knows nothing of Lighthouse. |
| T3 Progressive disclosure | 15% | Default. |
| T4 Speed-as-trust | 15% | Default, not the developer-tool 25%: sharing is occasional (per engagement, recurring, ad hoc), not a tight loop. |

**Rules applied to every option.**

- **Feasibility** covers size and operating cost per distribution. If the cheapest path has not been measured in
  the needed distribution (it is only desk-checked, marked [D] in spike #6052) or is new work with no precedent in
  the code, the option scores **at most 4**.
- **Standalone exclusion is not marked down on Desirability** (maintainer input 2026-10-17). OS2, the reader
  without access, is over-served (`job-analysis.md` §6): a Standalone user still has screenshots. An option is
  marked down only if it fails readers for some other reason, such as needing network reach to the instance.

## 2. DVF filter

| # | Option (first payload) | D | F | V | Total | Avg | Notes |
|---|---|---|---|---|---|---|---|
| 1 | Slide image + copy as text (metrics charts, forecast widgets, Then & Now panels) | 4 | 4 | 3 | 11 | 3.67 | D: the observed workaround (screenshots of metrics charts into slides, OS1) with a dated caption (OS3); per-panel images lose the story (OS6). F: front-end only, but clipboard images in the macOS and Linux WebKit webviews are unchecked. V: does not build the report-as-share capability (D6, C1) or anything Scheduled Report Delivery (#5882) can reuse. |
| 2 | Report over MCP (Then & Now) | 3 | 4 | 3 | 10 | 3.33 | D: serves the compose-it-yourself sender (D5), not the "cannot" reader directly; dating relies on the assistant quoting the fields. F: a read endpoint plus a clients release; Standalone MCP reach is a desk assumption. V: no Lighthouse-owned artefact reaches the sponsor; organisations that block AI tools are excluded. |
| 3 | Snapshot link (Then & Now) | 3 | 2 | 3 | 8 | 2.67 | D: marked down for **network reach**, not Standalone: a self-hosted instance is often not reachable from a sponsor's or client's network (desk judgement, I4; no Lighthouse data on this), and then the "cannot" reader gets nothing. F: unauthenticated route, token lifecycle, revocation, security review; Lighthouse withdrew its only no-sign-in identity design (ADR-129 → ADR-137). V: operators may forbid it; the products researched put it behind admin switches or top tiers (I3). |
| 4 | Browser-printed PDF (Then & Now) | 4 | 4 | 4 | 12 | 4.00 | D: the Epic's named form; a file reaches every reader, dated and self-explaining (OS3, OS6); PDF into a slide is one extra step (OS1 partial). F: a print route, print stylesheet and fonts shipped with the app (C5) are new work (no `@media print` exists today), and Tauri and WebKit print-to-PDF is desk-checked in the spike [D], so 4. ≈0 MB in every distribution. V: the print route is the seam a server engine drives later for #5882; charts from #6299 print with no new mechanism because the browser draws them (C4). |
| 5 | Server-rendered chart report, server + Docker only (a Custom Metric Report from #6299) | 4 | 2 | 4 | 10 | 3.33 | D: charts are what people capture today (X2), and a file reaches every reader. F: works with caveats (spike #6052): +95–110 MB Docker pull; the Helm memory limit raised to about 1.5 GiB (spike findings, Recommendation); about 120 MB first-use download on server zips; 18 Linux libraries; Chromium without a sandbox, restricted to Lighthouse's own origin; a render token. It also waits for #6299 to ship. V: the engine #5882 needs; Premium scheduled delivery rides on it. |
| 6 | Slide deck (a Delivery with its Features' forecasts) | 4 | 3 | 2 | 9 | 3.00 | D: forecasts are captured today and land in the sender's carrier, editable (OS1). F: a slide layout per payload; the settled Delivery table already exists (ADR-172). V: a Delivery is not a report, so this grows sharing beside reports, against C1 and D6; a second layout to maintain; nothing #5882 can reuse. |

Elimination rule (taste-evaluation method): an option whose DVF **total** is below 6 is eliminated before taste
scoring. No option totals below 6. **None eliminated.**

### Standalone coverage and its cost

| # | Standalone | Cost to cover | Cost to leave out |
|---|---|---|---|
| 1 | ✔ | ≈0 MB; check clipboard images in WebKit webviews (download as fallback) | — |
| 2 | ✔ (desk) | none in Lighthouse; MCP client must reach the local backend | — |
| 3 | ✘ | not possible: no reachable address | 0 |
| 4 | ✔ | ≈0 MB (Tauri webview print, desk-checked [D]); slight text anti-aliasing difference on macOS and Linux | — |
| 5 | ✘ | +99–120 MB per OS plus notarising a nested browser | 0, plus a "not available in this edition" state (hidden or disabled with a reason is DISCUSS's or DESIGN's call) |
| 6 | ✔ | a few hundred KB of script, or a managed library | — |

## 3. Taste scoring

| # | T1 Sub | T2 Concept | T3 Prog | T4 Speed | Justification |
|---|---|---|---|---|---|
| 1 | 4 | 5 | 5 | 5 | T1: image + caption; the merged copy-as-text is one removable element. T2: "copy image" is known to everyone. T3: one action on the panel. T4: instant, client-side. |
| 2 | 4 | 3 | 3 | 3 | T1: nothing to add in Lighthouse's UI. T2: the sender must know MCP and prompting (two concepts for a non-AI user). T3: the first use means configuring a client and writing a prompt. T4: assistant round-trips over several turns. |
| 3 | 3 | 3 | 3 | 4 | T1: expiry, revoke, a list of live links, a no-sign-in warning. T2: snapshot versus live, plus link lifetime. T3: creating a link asks for an expiry and an acknowledgement. T4: instant for the reader, if reachable. |
| 4 | 5 | 5 | 4 | 4 | T1: one button, one document. T2: PDF and Save as PDF are universal. T3: one action, then an OS dialog that shows printer options the user does not need. T4: print preview takes a moment; one dialog step. |
| 5 | 4 | 5 | 5 | 3 | T1: the "unavailable here" and "cannot render on this host" states are extra parts. T2: as 4. T3: one click, the file downloads. T4: about 3–4 s cold with a spinner; first use on a server zip waits for a 120 MB download. |
| 6 | 3 | 5 | 4 | 4 | T1: title slide, one slide per Feature or panel, layout per payload. T2: slides are familiar. T3: one click, one file. T4: generated in about a second. |

## 4. Weighted matrix

Final = 0.30·DVF + 0.20·T1 + 0.20·T2 + 0.15·T3 + 0.15·T4

| Rank | Option | DVF | T1 | T2 | T3 | T4 | **Weighted** |
|---|---|---|---|---|---|---|---|
| 1= | **4 Browser-printed PDF** | 4.00 | 5 | 5 | 4 | 4 | **4.40** |
| 1= | 1 Slide image | 3.67 | 4 | 5 | 5 | 5 | **4.40** |
| 3 | 5 Server-rendered chart report | 3.33 | 4 | 5 | 5 | 3 | **4.00** |
| 4 | 6 Slide deck | 3.00 | 3 | 5 | 4 | 4 | 3.70 |
| 5 | 2 Report over MCP | 3.33 | 4 | 3 | 3 | 3 | 3.30 |
| 6 | 3 Snapshot link | 2.67 | 3 | 3 | 3 | 4 | 3.05 |

**Tie-break rule.** It was introduced in revision 1, after re-grading feasibility produced the tie, so it is
post-hoc and labelled that way. It adjusts no score or weight. Rule 1 reads the Viability component already in the
matrix, so it adds no new information. It makes explicit which half of DVF decides a tie.

1. Prefer the higher **Desirability + Viability**, the evidence and business half of DVF. Feasibility is already
   level (4 and 4). Option 4: 4 + 4 = 8. Option 1: 4 + 3 = 7.
2. If still tied, prefer the option that serves more of the under-served outcomes (OS3, OS1, OS6). Option 4
   serves OS3 and OS6 fully and OS1 partly. Option 1 serves OS1 and OS3 but not OS6.

**Option 4 wins the tie on rule 1.** Rule 2 is not needed. On its own it is close: OS3 is covered by both;
Option 4 adds OS6 (12, borderline) and part of OS1, while Option 1 adds OS1 (13) in full. How it falls depends on
how partial coverage is weighed. The margin is a tie-break, not a lead, and the
dissent (`recommendation.md` §2) is written with that weight.

## 5. Sensitivity

| Test | Change | 4 | 1 | 5 | Leader |
|---|---|---|---|---|---|
| S1 Developer-tool weights | 25/15/20/15/25 | 4.35 | **4.52** | 3.93 | 1 |
| S2 X2 holds: nobody shares Then & Now | D of 4: 4 → 3 | 4.30 | **4.40** | 4.00 | 1 |
| S3 Print route proves small and Tauri print works as desk-checked | F of 4: 4 → 5 | **4.50** | 4.40 | 4.00 | 4 |
| S4 Images count as the report capability | V of 1: 3 → 4 | 4.40 | **4.50** | 4.00 | 1 |
| S5 Standalone becomes a hard target again | D of 5: 4 → 3 | 4.40 | 4.40 | 3.90 | tie → 4 by rule |
| S6 Self-explanation weighs more (OS6) | T1 of 1: 4 → 3 | **4.40** | 4.20 | 4.00 | 4 |
| S7 WebKit clipboard images fail; download only | F of 1: 4 → 3 | **4.40** | 4.30 | 4.00 | 4 |
| S8 #6299 has already shipped | F of 5: 2 → 3 | 4.40 | 4.40 | 4.10 | tie → 4 by rule |

**Reading.** Options 4 and 1 split the single-point tests (1 leads in S1, S2 and S4; 4 leads in S3, S6 and S7).
No test lets any other option reach them. The choice between 4 and 1 is therefore a **direction** choice: a
dated, self-explaining report document versus dated pieces of what people capture today. The tie-break follows the
stated product direction (D6, C1) and the under-served OS6. The two options do not compete for the same code (a
document route against a front-end panel action), so the losing one stays open as a follow-up rather than a
rejected idea. Option 5 never leads. It is a print route plus a server engine plus a wait for #6299, and the engine
pays off once unattended delivery (#5882) arrives.

## 6. Pool scoring — first payload (DISCOVER Q3)

The options already carry four payloads (Then & Now in 2, 3 and 4; chart widgets in 1; a chart report in 5;
Deliveries in 6). This pool scores the payload apart from the share form, so that DISCUSS can see whether the
recommended form should carry a different payload.

Rubric, locked before scoring: **J** fit with what people capture today (OS1, X2) 35% · **C** cost and
dependencies (no waiting on another Epic, no chart engine) 30% · **D** product direction (a report is the share
surface; PDF is a capability of any report, D6 and C1) 20% · **H** snapshot honesty (frozen or dated, C6 and
C10) 15%.

| # | Payload | J | C | D | H | Weighted | Verdict |
|---|---|---|---|---|---|---|---|
| F1 | **Then & Now only** | 2 | 5 | 5 | 5 | **3.95** | **Adopt.** A frozen Then, no charts, a report that exists today |
| F3 | Wait for Custom Metric Reports (#6299) | 4 | 2 | 5 | 4 | 3.60 | Runner-up. Fits the capture, but puts this Epic behind another |
| F2 | Then & Now plus the existing metrics chart widgets | 4 | 3 | 2 | 3 | 3.15 | Reject. Printing interactive widget pages is the #4753 shape the maintainer dropped (D9, X3) |
| F4 | Forecasts and Deliveries first | 4 | 3 | 2 | 3 | 3.15 | Reject for now. Not a report type; the Delivery already exports CSV (ADR-172) |

The pool agrees with the options matrix: the options whose payload fits the capture best (1, 5, 6) lose on
direction or cost, which are the same reasons F2, F3 and F4 lose here. F1 is the X2 risk taken knowingly. With
Option 4 it is cheap to carry, because #6299's charts print through the same route once it lands.

## 7. Pool scoring — licence line (DISCOVER Q4)

Rubric, locked before scoring: **R** the management-facing output reaches outside readers on Community
(land-and-expand, Epic 5878 dissent) 35% · **C** conversion pressure at a moment of shown value 25% · **K**
keeps the decision reversible (X5: the Community → Premium direction is assumed one-way) 20% · **S** simple to
explain 20%.

| # | Line | R | C | K | S | Weighted |
|---|---|---|---|---|---|---|
| L3 | Community PDF with a "Made with Lighthouse" footer; Premium removes it or adds own branding | 5 | 3 | 3 | 4 | **3.90** |
| L4 | Community PDF on the reports Community already allows (2 per owner); Premium unlimited reports | 5 | 3 | 2 | 5 | **3.90** |
| L2 | Community from the start; Premium extras later | 5 | 2 | 2 | 5 | 3.65 |
| L1 | Premium now, flip later (DISCOVER D4 as stated) | 1 | 4 | 5 | 3 | 2.95 |

**L3 and L4 tie at the base weights.** Tie-break, chosen after the base tie appeared and so post-hoc: under the
reversibility-heavy weights that the maintainer's own
caution suggests (R 25 · C 25 · K 35 · S 15), L3 scores 3.65, L1 3.45, L4 3.45 and L2 3.20, so **L3 leads**.
L4 is not a new gate: it is the existing 2-reports-per-owner cap, so L3 and L4 combine. The recommendation is
therefore **L3 on top of the existing cap**. The footer gives Premium a real extra on day one, so D4's reason for
starting Premium ("as long as we don't have the extras") falls away. L1 is rejected: the PDF is what reaches the
sponsor, and gating it blocks the path by which Lighthouse becomes known. Later Premium extras that fit the
pattern: Scheduled Report Delivery (#5882), anonymising (#4753), stored PDF history.

## 8. Evidence found after scoring

- Revision 1 folded these facts into the scores, they are no longer only recorded: no `@media print` or
  `window.print` exists in the frontend, and Tauri and WebKit print-to-PDF is desk-checked in the spike
  ("Unverified": WebView2 / WKWebView / WebKitGTK print-to-PDF). **Before DESIGN, a short probe should print one
  Then & Now from the Tauri app on macOS and Linux.**
- Quicksand is fetched from Google Fonts at runtime (spike #6052). Every PDF option has to ship it with the app,
  so it does not separate the options.

## DIVERGE gate G4

- [x] DVF filter applied; no eliminations, reasons recorded.
- [x] Weights locked and justified before scoring; one feasibility rule and one Standalone rule for all options.
- [x] All 6 options scored on all criteria; Standalone coverage and its cost stated per option.
- [x] Tie stated, with the tie-break rule; sensitivity shows where the choice flips.
- [x] First-payload and licence pools scored with their own rubrics, locked before scoring; the licence tie stated.

**DIVERGE-G4: PASS.**

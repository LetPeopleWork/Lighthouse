# Wave Decisions — epic-6298-report-pdf-export

DISCOVER decisions D1–D9 live in `discover/wave-decisions.md`.

## DIVERGE (2026-10-17)

**Agent**: Flux (`nw-diverger`), subagent mode, autonomous · **Research depth**: lightweight (maintainer config) ·
**Artifacts**: `diverge/job-analysis.md`, `diverge/competitive-research.md`, `diverge/options-raw.md`,
`diverge/taste-evaluation.md`, `diverge/review.yaml`, `recommendation.md`.

### Decisions

- **[DV-1] Job elevated and recorded as `job-share-numbers-outside-lighthouse`.** When someone who will not or
  cannot open Lighthouse needs to know where flow or a forecast stands, the sender hands over the numbers in a
  form that explains itself and says when it was taken. Under-served outcomes (desk estimates): OS3 read as current
  (14), OS1 time into the reader's carrier (13), OS6 misread without explanation (12). Added to
  `docs/product/jobs.yaml`. Source: `diverge/job-analysis.md`.
- **[DV-2] Maintainer decision-in-principle, for DISCUSS to confirm: PDF export need not cover every deployment
  model.** Leaving out Standalone (Tauri) is acceptable if it makes the feature cheaper or simpler ("I think that's
  fair"). Relayed by the coordinating session from the maintainer on 2026-10-17. This relaxes DISCOVER C2's
  "Standalone needs in-app view plus PDF export". Applied in scoring: every option states its Standalone coverage
  and cost, and Option 5 uses the relaxation. **Open point for DISCUSS/DESIGN**: if Standalone is left out, how a
  Standalone user learns that export is unavailable (hidden, or disabled with a reason). The recommended option
  covers Standalone at ≈0 MB, so the relaxation first matters when a server engine arrives with #5882.
- **[DV-3] Direction: Option 4, Browser-printed PDF (4.40), tied with Option 1 Slide image (4.40) and chosen by
  the stated tie-break rule** (higher Desirability + Viability, 8 against 7; then coverage of the under-served
  OS3, OS1 and OS6). It is a print-designed report with a dated, self-explaining header, saved through the browser
  or the Standalone webview. The single-point sensitivity tests split three to three between 4 and 1, so this is a
  direction call: a report document, not pieces of the interactive charts (D6, C1). Third: Option 5
  Server-rendered chart report (4.00), the later #5882 step. Trailing: Slide deck 3.70, Report over MCP 3.30,
  Snapshot link 3.05. Revised after peer review round 1, where feasibility was re-graded on one evidence standard
  (Option 4 from 5 to 4). Source: `diverge/taste-evaluation.md`.
- **[DV-4] Pre-committed trigger for the dissent.** If the 3–5 user question shows most shared artefacts are
  single charts pasted into slides, add "Copy as image" with the same dated caption to report panels as a
  follow-up. Do not swap directions. Source: `recommendation.md` §2.
- **[DV-5] First payload: Then & Now** (pool F1 3.95; runner-up "wait for Custom Metric Reports #6299" 3.60).
  The options themselves carry four payloads: Then & Now, chart widgets, a chart report and Deliveries. The options
  whose payload fits today's capture best lose on cost or direction.
  The X2 gap (Then & Now is not captured today) is accepted knowingly. With a browser print route, #6299's charts
  print later with no new mechanism. Source: `diverge/taste-evaluation.md` §6.
- **[DV-6] Licence recommendation, for the maintainer to decide in DISCUSS: Community PDF with a "Made with
  Lighthouse" footer; Premium removes it or adds own branding** (L3 3.90; it still leads under reversibility-heavy
  weights; it ties L4, the existing 2-reports-per-owner cap, at base weights, and the two combine). This would
  replace DISCOVER D4's "Premium until extras exist", because the footer is a Premium extra
  from day one. Later extras: Scheduled Report Delivery (#5882), anonymising (#4753), stored PDF history.
  Source: `diverge/taste-evaluation.md` §7.
- **[DV-7] Mechanism stays a DESIGN decision.** This wave names the cheapest viable path per option for honest
  feasibility scores only. The recommendation sets a rendering path, not a mechanism: the user's browser or the
  Standalone webview prints a print-designed view, with no server renderer until #5882. Standalone's "≈0 MB" is
  desk-checked in the spike, so a short Standalone print probe on macOS and Linux should run before DESIGN.
- **[DV-8] Downloaded, not stored; anonymising out of v1; exporters = report readers via RBAC; Q9 low-risk for
  Then & Now (aggregates and the Team or Portfolio name only).** All for DISCUSS to confirm. Source:
  `recommendation.md` §3.
- **[DV-9] The AI-dashboard skill stays out of scope.** It is cited as a complement to Option 2 only
  (`docs/feature/story-6217-lighthouse-skills`).

### Decisions DISCUSS must take from the maintainer

1. **Licence line** (DV-6): Community PDF with a branded footer and Premium un-branding, or DISCOVER D4's
   Premium-first.
2. **First payload** (DV-5): confirm Then & Now, knowing it is not what is captured today.
3. **Deployment scope** (DV-2): confirm the relaxation in principle; if a later step leaves Standalone out,
   hidden or disabled-with-reason.
4. **Header wording and an optional confidentiality line** (Q6, Q9).
5. **Who may export** (Q8): readers or editors.
6. **Entry condition for DESIGN**: run the Standalone print probe (one Then & Now printed to PDF from the
   Standalone app on macOS and Linux). It confirms the "≈0 MB" Standalone coverage and is the one piece of evidence
   that turns the 4-versus-1 tie into a lead (S3).

### Gate status

DIVERGE-G1 PASS · DIVERGE-G2 PASS (Nave unverified, not used) · DIVERGE-G3 PASS · DIVERGE-G4 PASS. DISCOVER's
G1–G3 failures remain open and accepted by the maintainer (DISCOVER D2). Peer review: see `diverge/review.yaml`.

## Maintainer decisions on the DIVERGE recommendation (2026-10-17)

DISCUSS starts from these; they settle the decisions `recommendation.md` handed over.

1. **Licence:** Community PDF carrying LetPeopleWork and Lighthouse branding. Removing or replacing the branding (a
   Premium extra) is to be decided later and is not in the first version.
2. **First payload:** Then & Now. Agreed.
3. **Deployment scope:** Standalone may be excluded where that makes the feature cheaper. Confirmed.
4. **Content and header:** aggregates plus the Team or Portfolio name, as recommended. Good enough for now.
5. **Who may export:** anyone who can read the report. Agreed.
6. **Delivery:** the PDF is always made on demand and downloaded, never stored. Anonymising is deferred.
7. **Open, raised by the maintainer:** can a server-side render (needed later for Scheduled Report Delivery) look
   (almost) identical to the PDF the user prints from the browser? DISCUSS records the fidelity target; DESIGN owns
   the mechanism.
8. **Standalone print probe:** the maintainer offered to run it on Linux.

## Standalone print probe on Linux (2026-10-17, run by the maintainer)

**Result: it works.** The standalone app's Linux webview engine (WebKitGTK 4.1) printed a live Then & Now report to
PDF through `window.print()` and the GTK print dialog, with no Rust code and no added size. The maintainer's verdict:
"not perfect but usable … promising".

- **How it was run:** a bare WebKitGTK window loaded the built frontend through a local proxy, which forwarded `/api`
  to the standalone backend. The standalone backend does not serve the frontend itself (`/` returns 500); Tauri
  bundles the UI separately. Pressing Ctrl+P in the real app does nothing, because no shortcut or menu is wired to
  print. Whether Tauri's own wrapper changes WebKit's default print behaviour was not tested; a debug build would
  settle it.
- **The PDF:** 2 A4 pages, 875 KB. The text is vector and selectable, and Quicksand Regular, Medium and Bold are
  embedded as subsets. Producer: Skia/PDF.
- **Findings for DESIGN:**
  1. The ✔ ✘ ▲ characters are not in Quicksand, so they fall back to system fonts (LiberationSans, DejaVuSans). They
     will differ on each OS, and a minimal server container may have no font for them at all. The print view should
     draw them as SVG icons, not text characters.
  2. The dark theme printed as dark, with full backgrounds. The print view forces the light theme.
  3. The app chrome printed too: the navigation bar, tabs, Edit and Delete buttons, and the footer, which spilled onto
     a nearly empty second page. The print view needs a dedicated layout with our own header (Team or Portfolio, the
     windows, when it was made) and a branding footer, not the interactive page.
- **Maintainer, on fidelity:** with our own print stylesheet, small differences between browsers are acceptable.
- **Still unprobed:** Standalone on macOS (WKWebView) and on Windows (WebView2, which is Chromium-based and so the
  lowest risk).
- **Follow-up in the real app, same day:** a debug build of the Standalone app (Tauri 2 on Linux, run against a copy of
  the maintainer's data) printed the same Then & Now report through `window.print()` from the devtools console. The
  output matched the bare-engine probe: 2 A4 pages, ~870 KB, Skia/PDF, the same embedded Quicksand subsets and the
  same fallback fonts for the symbols. **Tauri's wrapper does not interfere with printing**, so an Export PDF action
  that calls `window.print()` from the frontend works in Standalone on Linux with no Rust code. Release builds wire no
  shortcut or menu to print, so the action has to call it. macOS (WKWebView) is still unprobed.

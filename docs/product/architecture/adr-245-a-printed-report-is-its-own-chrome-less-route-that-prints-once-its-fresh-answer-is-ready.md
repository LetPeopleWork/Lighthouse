# ADR-245: A printed report is its own chrome-less route, and it prints only once its fresh answer is ready

- **Status**: Accepted (2026-10-17). The maintainer chose the print page itself as what stands behind the print
  dialog (MQ-1 = a, 2026-10-17).
- **Feature**: epic-6298-report-pdf-export (ADO Epic #6298 "Report PDF Export"), slices 01–03
- **Deciders**: the maintainer (MQ-1); Morgan (Solution Architect)
- **Relies on**: ADR-219 (a report is an owner-scoped record; its read computes Now), ADR-233 (a widget is loading
  until its data answers the current request), ADR-223 (a pretty view reads the answer through a narrow reader)

## Context

Anyone who can read a report gets an **Export PDF** action. It must re-read the report so Now ends on the export
day, then hand a print-designed page to the browser's print dialog or to the Standalone app's webview, which has no
print shortcut of its own. That page is light whatever the user's theme, shows none of the app's navigation, tabs,
buttons, footer, survey nudge or usage-data question, and carries its own header and branding footer.

A later feature, Scheduled Report Delivery (#5882), will render reports on the server with headless Chromium. The
maintainer set the bar: that render must be pixel-comparable to the Chromium print of the same view. So the
print-designed page must be something a renderer can open by itself and know when it is finished, not something
assembled only inside a click handler.

Facts from the code that shape the choice:

- `App.tsx` renders `Header`, `Footer`, `SurveyNudge` and `UsageDataAsk` around every route, inside the auth guard
  and the providers (query client, API services, terminology, usage-data consent).
- The two themes are built inside `main.tsx`; neither is importable today.
- In the Standalone app the SPA boots once: it waits for the backend's `backend-ready` event and shows a five-second
  splash. A second copy of the SPA (in a frame or a new window) would boot again.
- The Linux Standalone probe printed through `window.print()` from the real app with no Rust code. macOS is
  unprobed; the maintainer checks it after release.
- Spike #6052 already recommended "a dedicated report route with a ready flag" for the server renderer, and the same
  route printed by the webview for Standalone.

Quality attributes, in order: **correctness** (never print a stale or half-loaded page), **fidelity** (one page that
both the browser and a later server render produce), **reach** (browsers and every Standalone webview with no native
code), **simplicity**.

## Decision

1. **One route per owner kind, outside the app shell**: `/print/teams/:id/reports/:reportId` and
   `/print/portfolios/:id/reports/:reportId`. `App` gains a layout route that holds the header, footer, survey nudge
   and usage-data question; the print routes sit beside it, under the same auth guard and providers but outside that
   layout. The page is wrapped in the light theme, which moves out of `main.tsx` into an importable module so both
   places use the same object.
2. **The page reads its own data.** It reads the report with the same query key the report view uses, forced fresh on
   mount, and the owner's name through the owner's existing read. It never takes values handed over from the screen.
   Because the key is shared, the report view shows the same fresh answer when the reader comes back.
3. **Ready is one predicate, written to the page as `data-render-ready`.** The page is ready when the report answer
   it holds was fetched after the page mounted, the owner's name and the instance's terms have answered, and
   `document.fonts.ready` has resolved. Until then it shows its loading look; on any failure it shows its failure
   look and is never ready. The attribute is `"true"` only in the ready state. A renderer waits for it; so does the
   E2E walking skeleton.
4. **Printing is a separate, opt-in step.** Export PDF navigates in the same window to the print route with a
   one-shot "print when ready" flag in router state, never in the URL. The page calls `globalThis.print()` once, on
   the first ready render with the flag set, and goes back to the report on the `afterprint` event. A screen-only
   "Back to report" control (hidden by the print stylesheet) is always there, so an engine that does not fire
   `afterprint` still leaves the reader a way back. A page opened directly — by a renderer, a test, or a pasted link —
   never prints.
5. **The page owns its print stylesheet**: `@page { margin: 12mm }` with no fixed size, so A4 and US Letter both work
   from the dialog's paper choice; a fixed content width that fits both; `break-inside: avoid` on the summary and on
   every panel; the header kept with what follows; the branding footer in the flow, kept with the last panel, so no
   page ever holds only the footer; `print-color-adjust: exact` so the coloured marks keep their colour; animations
   off. The page's `document.title` names the report and its owner, because browsers offer it as the file name.
6. **The click is what is counted.** The usage-data event for an export is emitted by the Export PDF action on the
   report view, before it navigates. The print route emits nothing and never reports `ReportOpened`, so a server
   render or a test opening it counts as nothing.

## Alternatives considered

- **A print stylesheet on the existing report view** (hide the chrome under `@media print`, show a print-only
  header). Cheapest for slice 01, but the page is not something a renderer can open on its own; the server render
  would need a second layout, and "pixel-comparable to the Chromium print of the same view" would compare two views.
  It also fights the app shell: every future overlay added to the shell has to remember to hide itself in print.
  **Rejected.**
- **A hidden frame loading the print route, printed from the frame.** Keeps the reader on the report while the dialog
  is open, but boots a second SPA: the auth guard again, and in Standalone a splash that waits for a `backend-ready`
  event the backend has already sent. Frame printing also behaves differently across WebKit builds, and macOS cannot
  be probed before release. **Rejected.**
- **A print-only copy rendered into a portal on the report view** (the same document component, shown only to the
  printer). Keeps the reader on the report and shares the component with a route used by a renderer. Rejected as the
  default because the browser would print the portal inside the app's page while a server would render the route:
  two surroundings, two sets of global styles, and the fidelity requirement compares exactly those. Offered as
  MQ-1 option b; **rejected by the maintainer** (2026-10-17).
- **A server-made PDF now** (PuppeteerSharp or a sidecar). Out of scope for this Epic: +100–480 MB per distribution,
  a render token and a browser lifecycle, for a capability the webview provides free. The route built here is what
  that later renderer loads. **Deferred to #5882.**

## Consequences

- Positive: the browser, the Standalone webview and a later server renderer all load the same page; #5882 adds a
  renderer and a render token, not a layout.
- Positive: the shell's overlays (survey nudge, usage-data question) cannot reach the printed page, because they are
  not in its tree.
- Positive: a stale page cannot print: the print call sits behind a ready predicate that requires an answer fetched
  after the page mounted.
- Negative: while the dialog is open the reader sees the light print page instead of the report. Accepted by the
  maintainer (MQ-1 = a).
- Negative: going back depends on `afterprint`; engines differ on when it fires. The always-present Back control
  bounds the damage to one extra click.
- Negative: `App.tsx` is restructured into a layout route — a change to the file every page passes through. No
  route path changes; the existing E2E suite is the regression net.
- Negative: browsers may print their own header and footer (the page's address and the date) unless the reader turns
  them off in the dialog; the docs carry the tip. The address names the instance's host.

## Earned trust (what is probed, not assumed)

- **Behavioural, every CI run**: the E2E walking skeleton opens the print route under print-media emulation, waits for
  `data-render-ready="true"`, and checks the header and the absence of the app's navigation; it then asks Chromium
  for the PDF (`page.pdf`) and fails if any embedded font is not the Lighthouse typeface (this font check is added
  in slice 02, when the typeface ships with the app; ADR-246). That last check is the seed of the #5882 fidelity
  comparison.
- **Unit**: the ready predicate (answer older than mount → not ready; a failed read → never ready; fonts pending →
  not ready); print is called once, only with the flag, only when ready; nothing is printed after a failure.
- **Structural**: a Vitest guard that the print route's module tree imports neither `Header`, `Footer`,
  `SurveyNudge` nor `UsageDataAsk`, and that the print page does not call the usage-data reporter.
- **By hand, per slice** (DoD 7): Chrome, Firefox and the Linux Standalone app; the maintainer checks macOS
  Standalone after release. If nothing happens there, the follow-up disables or changes the action (maintainer
  decision, 2026-10-17).

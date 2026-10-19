# ADR-245: A printed report is its own chrome-less route, and it prints only once its fresh answer is ready

- **Status**: Accepted (2026-10-17). The maintainer chose the print page itself as what stands behind the print
  dialog (MQ-1 = a, 2026-10-17). Revised after review 2026-10-19 (decisions 2, 3, 4 and 6 sharpened, decision 7
  added; see "Revised after review 2026-10-19" at the end; a second revision the same day sharpened decisions 2–4).
  Three of the revised points (Retry continues the export; Back on a directly loaded page; the "Part of the page"
  sentence) apply defaults the maintainer has not yet confirmed; each is marked **PROVISIONAL — maintainer confirms**.
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
2. **The page reads its own data, through a read-only port.** It reads the report with the same query key the report
   view uses, and the owner's name through the owner's existing read. It never takes values handed over from the
   screen. Because the key is shared, the report view shows the same fresh answer when the reader comes back.
   (rev. 2026-10-19) *Read-only by type*: the page is given a narrow interface holding only the report read and the
   owner-name read (in TypeScript a `Pick` of `IReportService`'s `getReport` and of the owner services' `getTeam` /
   `getPortfolio`), built by the route wiring from `ApiServiceContext`; the page's own modules never read that context,
   so `createReport`, `editReport`, `deleteReport` and the owner services' writes are out of its reach, and a type-level
   test fails if the port gains a member. (rev. 2, 2026-10-19) A `Pick` is erased at runtime, so the route wiring
   builds the port as a new object literal of exactly the three reads (`getReport`, `getTeam`, `getPortfolio`, each
   bound to its service) and a runtime test checks its keys. The instance's terms reach the page the same way, as a
   narrow read of `getTerm`, `isLoading` and `error` only — never the whole terminology context with its refetch.
   The fresh read does not retry an answer that will not change (404, 422), as the report view does; anything else
   gets the app's default retry. *Fresh, mechanically*: the app's query client keeps answers fresh for five
   minutes and reads again on mount only when stale, so the print page's own observer asks to read on every mount
   (`refetchOnMount: "always"`) while the report view's options stay as they are. The page records when it mounted
   and counts an answer as fresh only when it arrived at or after that moment; a cached answer from before is held,
   never rendered as the print.
3. **Ready is one predicate, written to the page as `data-render-ready`.** The page is ready when the report answer
   it holds was fetched after the page mounted, the owner's name and the instance's terms have answered, and the
   fonts have loaded. Until then it shows its loading look; on any failure it shows its failure look and is never
   ready. The attribute is `"true"` only in the ready state. A renderer waits for it; so does the E2E walking
   skeleton. (rev. 2026-10-19) The predicate is a **pure function of explicit inputs** — when the answer arrived,
   when the page mounted, the report error and whether it is final (404, 422) or retryable, the owner name or its
   error, whether the terms have answered or failed, and the fonts state — returning preparing, failed (retryable or
   final) or ready. The fonts input is asked for **after the document has rendered**: (rev. 2, 2026-10-19) the page
   asks the effect port (decision 4) to load each typeface face and weight it shows — loading a face changes the
   document's font set, so it is an effect, not a read; the port's browser binding calls `document.fonts.load` per
   face and then awaits `document.fonts.ready`. A rejected load, or one that finds no face, is a retryable failure,
   because printing then would embed a fallback typeface. (rev. 2) The report error is the API's error type, read
   through its HTTP status; 404 and 422 are final. Two retryable failures differ only in their sentence: a failed
   report read says "This report couldn't be loaded, so no PDF was made."; when the report was read but the owner's
   name, the terms or the fonts could not be loaded, the page says "Part of the page couldn't be loaded, so no PDF
   was made." (**PROVISIONAL — maintainer confirms**). Both offer Retry.
4. **Printing is a separate, opt-in step.** (rev. 2026-10-19) Export PDF — the report view's button or the Reports
   list's icon (maintainer, 2026-10-19) — navigates in the same window to the print route with a one-shot "print when
   ready" flag **and the origin** (`report` or `list`) in router state, never in the URL. A print-once hook, given
   the page's state, the flag, the origin and an **effect port** (print, a subscription to `afterprint`, and
   navigation), does three things and nothing else: when the page is ready and flagged it first **consumes the flag**
   by replacing the current history entry with the flag cleared, then calls print once; on `afterprint` it returns
   to the origin. Router state survives a reload and comes back with Forward, and React's development mode runs
   effects twice, so consuming the flag before printing (and a per-mount record that the print happened) is what
   keeps it to one print. The flag is consumed only by an actual print: after a retryable failure, a successful Retry
   makes the page ready and it prints once (**PROVISIONAL — maintainer confirms**). The production binding of the
   effect port is the one module naming `globalThis.print()` (rev. 2: and `document.fonts`; the port also carries
   the font loading of decision 3, which the page, not the hook, calls). (rev. 2, 2026-10-19) The hook subscribes to
   `afterprint` once per mount and unsubscribes on unmount, independent of the flag and the state: clearing the flag
   re-renders the hook and must not drop the listener, which returns to the origin only when this mount printed. A
   screen-only Back control (hidden by the print
   stylesheet) is always there, so an engine that does not fire `afterprint` still leaves the reader a way back; its
   label and target follow the origin — "‹ Back to report" to the report view, "‹ Back to Reports" to the Reports
   list. A final failure always leads to the Reports list. A page opened directly — by a renderer, a test, or a
   pasted link — carries no flag and no origin, never prints, and its Back leads to the Reports list
   (**PROVISIONAL — maintainer confirms**). Every return replaces the print entry and carries a return-from-print
   marker.
5. **The page owns its print stylesheet**: `@page { margin: 12mm }` with no fixed size, so A4 and US Letter both work
   from the dialog's paper choice; a fixed content width that fits both; `break-inside: avoid` on the summary and on
   every panel; the header kept with what follows; the branding footer in the flow, kept with the last panel, so no
   page ever holds only the footer; `print-color-adjust: exact` so the coloured marks keep their colour; animations
   off. The page's `document.title` names the report and its owner, because browsers offer it as the file name.
6. **The click is what is counted.** The usage-data event for an export is emitted by the surface that starts it,
   before it navigates — (rev. 2026-10-19) the report view's button and the Reports list's icon, both through one
   export starter: exactly one event per click from either. The print route emits nothing and never reports
   `ReportOpened`, so a server render or a test opening it counts as nothing. A return from print reports no second
   `ReportOpened` (the marker of decision 4). A return to the Reports list may count one tab opening after the usual
   dwell, as any arrival on that tab does; that is accepted rather than suppressed, so the page-opening detector keeps
   reading the path alone.
7. (rev. 2026-10-19) **The page paints its own light ground.** Its root sets full height, the light theme's
   background and text colour and `color-scheme: light`, and its print stylesheet paints `html` and `body` white,
   because the app's baseline paints `body` from the user's theme and a nested light theme does not repaint it. The
   shell's container, with the user's background, moves into the shell layout route with the rest of the shell.

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
- Negative (rev. 2026-10-19): the Reports list's actions contract changes shape — from an editor-only pair (edit,
  delete) to an export action every reader gets plus an optional editor group — so viewers gain an Actions column
  and the list's existing column-layout test for viewers changes.
- Negative (rev. 2026-10-19): a reload of the report view right after a return from print keeps the return marker in
  its history entry and reports no opening; an undercount of one reload, never a double count.

## Earned trust (what is probed, not assumed)

- **Behavioural, on every CI run once slice 01 has shipped** (until then the walking skeleton is parked as `fixme`
  and proves nothing): the E2E walking skeleton opens the print route under print-media emulation, waits for
  `data-render-ready="true"`, and checks the header and the absence of the app's navigation; it then asks Chromium
  for the PDF (`page.pdf`) and fails if any embedded font is not the Lighthouse typeface (this font check is added
  in slice 02, when the typeface ships with the app; ADR-246). That last check is the seed of the #5882 fidelity
  comparison.
- **Unit**: the ready predicate as a table over its explicit inputs (answer older than mount → not ready even with
  data present; a failed read → never ready; fonts pending or failed → not ready); print is called once, only with
  the flag, only when ready; nothing is printed after a failure. (rev. 2026-10-19) The print-once hook is driven
  through a fake effect port: after the print the entry carries no flag; a reload, a Forward to the entry and React's
  double effect each print zero more times; a successful Retry prints once; the return goes to the origin.
- **Type-level** (rev. 2026-10-19): a test fails if the print page's read port gains any member other than its reads.
  (rev. 2) **Runtime**: a test fails if the read port's or the terms read's own keys are anything but their named
  members. **Unit** (rev. 2): one `afterprint` listener per mount, kept when the flag clears, gone after unmount;
  `globalThis.print` and `document.fonts` named only in the effect port's binding (structural scan).
- **Structural**: a Vitest guard that the print route's module tree imports neither `Header`, `Footer`,
  `SurveyNudge` nor `UsageDataAsk`, and that the print page does not call the usage-data reporter. (rev. 2026-10-19)
  Also: `globalThis.print` named only in the effect port's binding; `ApiServiceContext` read under `print/` only by
  the route wiring; the export event named only by the export starter.
- **By hand, per slice** (DoD 7): Chrome, Firefox and the Linux Standalone app; the maintainer checks macOS
  Standalone after release. If nothing happens there, the follow-up disables or changes the action (maintainer
  decision, 2026-10-17).

## Revised after review 2026-10-19

The consolidated review at the end of DISTILL, and the maintainer's DISTILL walk-through (an Export PDF icon in the
Reports list for every reader; the Back label following where the export started), sharpened this decision without
changing its direction:

- The page's read surface is read-only by type, and its effects (print, after-print, navigation) sit behind one
  injected port (decisions 2 and 4). Before, "reads only" and "one hook" were conventions.
- "Forced fresh on mount" now names its mechanism, because the app's five-minute freshness window would otherwise
  serve a cached answer without reading (decision 2).
- Ready is a pure function of named inputs, and the fonts are asked for after the content renders (decision 3).
- The flag is consumed before printing, and the origin travels with it (decision 4); two surfaces start an export
  and count it the same way (decision 6); the page paints its own light ground (decision 7).
- Earned trust: the end-to-end check runs on every CI run *once slice 01 has shipped*, not before.
- Provisional, for the maintainer to confirm: a successful Retry prints; Back on a directly loaded page leads to the
  Reports list.

Alternatives weighed in the revision: suppressing the tab opening counted on a return to the Reports list (rejected:
the page-opening detector would have to read router state beside the path, and the tab counts arrivals, which a
return is); moving the app's baseline styles into the shell layout route to give the print page a clean ground
(rejected: the sign-in, splash and blocked pages render outside the router and rely on it); clearing the flag only
after the dialog closes (rejected: a reload while the dialog is open, or a second effect run, would print again).

**Second revision 2026-10-19** (re-review; no change of direction):

- Font loading moved onto the effect port (decisions 3 and 4): it changes the document's font set, so it is an effect;
  with it on the port, everything the page does to the browser beyond its own title, its ready attribute and its one
  cache entry goes through one port a test fakes. Rejected: declaring font loading as part of the page's own bounded
  change — true, but it would leave the page's tests reaching for a global.
- The `afterprint` listener lives as long as the mount, not as long as the flag (decision 4).
- The read port and the terms read are object literals of exactly their named members, checked at runtime, because a
  type-level `Pick` vanishes when the code runs (decision 2).
- The fresh read does not retry a final answer (404, 422); the dialog opens within two seconds of the click on a
  loaded report, checked by hand on the dev instance.
- Provisional, for the maintainer to confirm: a failure of the owner's name, the terms or the fonts, with the report
  read, says "Part of the page couldn't be loaded, so no PDF was made." with Retry.

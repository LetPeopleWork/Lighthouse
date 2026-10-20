# ADR-255: Export PDF prints from a hidden frame the app draws the print page into, so the reader never leaves

- **Status**: Accepted (2026-10-20). Supersedes ADR-245 in part: its decision 4 (navigation, the one-shot print flag,
  the origin, the return after printing, the Back control after an export) and the return clauses of its decision 6.
  ADR-245's route, read-only port, ready predicate, print stylesheet and light ground stand.
- **Feature**: epic-6298-report-pdf-export (ADO Epic #6298 "Report PDF Export"), the export rework after the
  maintainer's review of slices 02 and 03 (NMD-15, NMD-16)
- **Deciders**: the maintainer (NMD-15, NMD-16: the reader never sees a print view; the waiting spinner and the error
  snackbar); Morgan (Solution Architect) for the mechanism
- **Relies on**: ADR-245 (the print page, its reads, its ready predicate), ADR-246 (the bundled typeface and drawn
  marks), ADR-233 (a widget shows when its data is on the way or could not be loaded)

## Context

Since slice 01, Export PDF navigated in the same window to `/print/…`, printed there, and stepped one entry back in the
history on `afterprint`. In Firefox the print dialog does not block the page: the step back never took effect, the
reader stayed on the light print page, and the next Back went two entries back (from a report, to the Overview). The
maintainer reviewed it hands-on in Firefox and Chrome and decided:

- **The reader never sees a print view.** Export PDF prints from the page the reader is on (the report view or the
  Reports list); Save or Cancel leaves them exactly where they were; no history entry is added.
- **While the PDF is prepared** the Export PDF button (or the list row's PDF icon) shows its waiting spinner and is
  disabled; **when it cannot be made**, the app's error snackbar says the sentence the print page says today, with
  Retry where the print page offered it. An inline alert was offered and declined.
- The `/print/…` address stays as a page (a pasted link, the E2E PDF check, the later server renderer); nothing sends
  a reader there.

Facts from the code that shape the mechanism:

- `App` boots once per document: it waits, in the Standalone app, for a `backend-ready` event the backend sends once,
  runs the auth guard, and builds one `QueryClient`. A second document that loads the app boots all of it again.
- The print page reads the report with the report view's query key, in the app's one cache, so whatever it reads is
  also what the report view shows (D19, G1: the PDF equals the screen at export).
- MUI writes its styles through Emotion into the document whose head its cache points at; the bundled typeface's
  `@font-face` rules come in with the app's own style sheets (a `link` in a build, a `style` element in development).
- A window's `print()` prints that window's document. Chrome fires `afterprint` inside `print()`; Firefox fires it when
  its dialog closes; some webviews may not fire it at all.
- The app's error snackbar (`useErrorSnackbar`) wraps both surfaces already (`TeamDetail`, `PortfolioDetail`); it
  takes a string only, and hides itself after 6 s.

Quality attributes, in order: **correctness** (never a stale or half-loaded PDF; the screen and the PDF agree),
**usability** (the reader stays put, sees progress and failure where they clicked), **reach** (Chrome, Firefox, the
Standalone webviews, with no native code), **simplicity**.

## Decision

1. **Export prints from a hidden frame that the running app draws into.** The export starter counts the choice (one
   `ReportPdfExportStarted`, as before) and mounts a print frame on the surface the reader is on: a same-origin
   `<iframe>` whose `srcdoc` is a bare HTML skeleton with a doctype (standards mode) and the app's `lang`. Once the
   frame has loaded, the app draws the print page's content into the frame's body through a React portal. The content
   therefore runs under the app's own providers: one query cache, one auth session, one terminology, one Standalone
   backend address. When the content is ready, the **frame's window** is asked to print, so only the frame's document
   reaches the dialog.
2. **The frame lays out like paper and stays unseen.** It sits at a fixed position outside the viewport, sized
   210 mm × 297 mm — never `display: none` or zero-sized, because it must lay out at paper size and stay printable in
   every engine's print path — with
   `aria-hidden="true"`, `tabindex="-1"` and a title. It holds no control. Its head receives a copy of the app's own
   style sheets (each `link[rel=stylesheet]`, awaited until loaded, and each `style` element not written by Emotion),
   so the typeface's faces are declared there and the app's dark baseline is not; the frame gets its own Emotion cache
   pointing at its head (`@emotion/cache`, made a direct dependency; already installed with `@emotion/react`), and the
   content is wrapped in the light theme with its own baseline. A style sheet that fails to load is a part that could
   not be loaded.
3. **The content is the print page's content, unchanged in what it reads and decides.** The print page splits into the
   content (reads through the read-only port, the narrow terms read, `printPageStateOf`, the fonts and the mark through
   the effect port, `ReportPrintDocument`) and the route component around it. The route and the frame draw the same
   content. The ports' builders become one hook in the routes module, used by both, so `ApiServiceContext` is still
   named in that one module under `print/`.
4. **The effect port is made for a window, loses navigation and gains the title.** `browserPrintEffects(target)` binds
   print, `afterprint`, font loading (`fonts.load` per face, then `fonts.ready`, every subset), the mark's picture and
   a new `setTitle(title) → restore` to the window it is given: the frame's window for an export, the app's own for the
   route (which never prints). `navigate` is removed. The frame's binding sets the frame document's title and the app's
   (restoring the app's when the frame goes), because engines differ on which title they offer as the file name. A
   second factory in the same module, `openPrintFrame()`, makes the frame, copies the style sheets and hands back its
   window, head, body and a `close()`; it is the only code that names a frame.
5. **One frame per surface, gone on every exit.** The print-once hook now runs only in the frame: once the content is
   ready it prints once per mount and tells the frame host `printing`, then `closed` on the frame window's
   `afterprint`, or `failed` with the sentence and whether Retry is offered. The host removes the frame a turn after
   `afterprint` (never inside it), on a failure, when a new export starts, and when the surface unmounts. The control's
   waiting ends when `print()` has been called, so an engine that never fires `afterprint` cannot leave it stuck.
6. **The waiting and failure looks belong to the surface.** The report view's Export PDF keeps its label and shows the
   small spinner the list's Edit already shows while it reads; the list row's icon turns into that spinner and every
   row's PDF icon is disabled while one export prepares. The waiting control stays focusable (`aria-disabled`,
   `aria-busy`) and ignores clicks. A failure shows the app's error snackbar with the print page's sentence — "This
   report couldn't be loaded, so no PDF was made." or "Part of the page couldn't be loaded, so no PDF was made." — and
   Retry; a report that no longer exists or was made by a newer version gets the report view's own sentence and no
   Retry. The snackbar gains an optional action and a dismiss handle; a message with an action stays until closed;
   every existing string-only call behaves as today. Retry mounts a fresh frame for the same report and counts nothing;
   a successful Retry prints once. The starter dismisses its own message when its surface unmounts.
7. **A 30-second budget.** A frame whose content has neither printed nor failed 30 s after it mounted fails with
   Retry. The budget stops just before `print()` is called. `printPageStateOf` (whose inputs are already the fresh
   answer's time, the mount time, the report error and whether a read is in flight, the owner's name or its error, the
   terms' loading or error, the fonts and the Lighthouse mark) gains a `timedOut` input, which turns a page still preparing into the report sentence
   when the fresh answer never came, or the part sentence when it did. Only the frame has a budget; the route is
   visible, and a renderer sets its own.
8. **The route stays, quieter.** `/print/…` opened from its address shows its preparing, failed and ready looks as
   before, sets `data-render-ready`, never prints, and offers one screen-only link, "‹ Back to Reports". The one-shot
   print flag, the origin, `RETURNED_FROM_PRINT` and the step back are deleted. `ReportOpened` keeps counting once per
   history entry; an export adds no entry, so it neither adds nor loses an opening.

9. **Closed by the design review (2026-10-20).**
   - *A spike gates the work.* The rework's first step is a thin frame host and binding with the real content and a
     real print, probed by hand in Chrome, Firefox and the Linux Standalone app: the subframe's `print()` previews
     only the report, `afterprint` reaches the frame's own window, and the offered file name is recorded per engine.
     The go / no-go is recorded before any other step. If an engine fails, the maintainer chooses between Export PDF
     not offered in that engine (recommended, as for macOS) and print-in-place in that engine only.
   - *A render error in the printed content* is caught by the frame host's own error boundary around the portal,
     never by the surface's: it becomes the part sentence with Retry, and the frame is removed.
   - *`print()` throwing or missing* is the same outcome. A `print()` that silently does nothing cannot be seen from
     code; the spike is what catches it.
   - *The waiting ends at the call*: the control leaves its waiting look just before `print()` is called, because in
     Chrome the call returns only when the dialog closes.
   - *Recovery on the report view*: the snackbar's Retry and the view's own Retry read the same shared key, so either
     one recovers the view; Export PDF is offered again when the read lands.
   - *Accepted as they are*: leaving the surface while a non-blocking dialog is open ends the export (the frame goes
     with the surface); a frame that never hears `afterprint` stays hidden until the next export or the surface goes;
     a newer snackbar message replaces the export's failure and its Retry; no extra announcement while preparing
     (the control is `aria-busy`), and focus stays on the control rather than jumping to the snackbar, whose alert is
     announced.

## Alternatives considered

- **A hidden frame that loads `/print/…`** with a flag telling the page it is embedded, printing itself and reporting
  ready, failed and printed to the parent by `postMessage`. It also prints only the frame and keeps the reader in
  place, and it was the starting point of this decision. **Rejected**: the frame boots a second copy of the app — the
  auth guard again; in the Standalone app, a wait for a `backend-ready` event the backend has already sent, or a
  dependence on whatever the webview injects into subframes, which cannot be probed on macOS before release; a second
  query cache, so the fresh answer the PDF prints never reaches the report view on screen (the screen and the PDF would
  disagree, against G1); and a message protocol with origin checks to carry three outcomes that a shared React tree
  carries as typed callbacks. The boot is exactly why ADR-245 rejected a hidden frame; drawing into the frame removes
  the boot and keeps the benefit.
- **Print in place**: the print document rendered into a print-only portal in the app's own document, `@media print`
  hiding the app. **Rejected**: the printout would share the app's document — its baseline painted from the reader's
  (possibly dark) theme, its `@page`, every present and future overlay — the fidelity risk for which MQ-1 option b was
  rejected on 2026-10-17.
- **`window.open`** of the print route in a new tab or window. **Rejected**: pop-up blockers, a new view the reader
  sees and must close (against NMD-15), and unproven in the Standalone webviews.
- **Keep the navigation and fix the return** (step back on a timer, or on focus returning). **Rejected**: the reader
  still sees a print view, which NMD-15 rules out, and every engine-specific return heuristic is a new way to land in
  the wrong place.

## Consequences

- Positive: the reader stays on the report view or the list with the same address and history; the Firefox defect
  cannot recur, because nothing navigates.
- Positive: the screen and the PDF agree: the frame's fresh read lands in the cache the report view reads, so the view
  behind the dialog shows the answer that was printed.
- Positive: no second boot, no message protocol, no frame-loaded address to guard; the route is still the one page a
  later server renderer loads.
- Positive: a stalled preparation ends in 30 s with a sentence and Retry instead of a spinner with no end.
- Negative: the printed document now has two surroundings — the route's document and the frame's — so the frame must
  rebuild what the app's document gives the route for free (style sheets, a style cache, a baseline). A missed style
  sheet would print in a fallback; the fonts and the mark are awaited, and the by-hand probe compares both.
- Negative: on the report view, a failed fresh read is also the view's answer: its body shows its own could-not-load
  look beside the snackbar. Accepted: the screen must not keep showing an answer the export failed to confirm.
- Negative: subframe printing differs across engines. Chrome, Firefox and the Linux Standalone app are probed by hand
  in the spike that opens the rework (decision 9), and again before it closes; macOS stays the maintainer's post-release check (DVO-10). If an engine prints the app's
  page or nothing, the work stops for a maintainer decision, with no silent fallback.
- Negative (simplicity): five moving parts are added, each with its simplest failure. The style-sheet copy can miss
  a sheet, so something prints in a fallback. The frame's style cache can be left out, so MUI styles land in the
  app's head and the print is unstyled. The portal can throw in a foreign document, which the frame's boundary
  catches. The budget can fire on a very slow connection, and Retry follows. The two titles can be restored wrongly,
  leaving the tab named after the report. The spike, the binding tests and the by-hand probe are aimed at these.
- Negative: the app's title changes while a frame lives (the file name some engines offer), restored when it goes.
- Negative: `@emotion/cache` becomes a direct dependency (MIT, already installed); the error snackbar's contract grows
  an optional action and a dismiss handle.
- Neutral: the E2E walking skeleton changes from "returns to the report" to "stays on the report"; the PDF font check
  keeps opening `/print/…` directly.

## Earned trust (what is probed, not assumed)

- **Behavioural (Vitest)**: the starter with a fake frame port — one event then one frame per choice; no navigation,
  the location and `history.length` unchanged; the spinner while preparing; a second click does nothing; a failure
  shows the snackbar with the right sentence, Retry mounts a fresh frame with no event and prints once; a final failure
  has no Retry; the message is dismissed when the surface unmounts. The frame host — one frame per export, removed a
  turn after close, on failure, on unmount and on a new export; the 30-s budget (fake timers). The reshaped print-once
  hook — one print per mount, only when ready, once under StrictMode, a dialog it did not open reports nothing, one
  listener per mount.
- **Binding (Vitest, jsdom, wiring only)**: jsdom loads no `srcdoc` document and no style sheet, and has neither
  `document.fonts` nor `print`, so these tests use stand-ins for each. They prove which window, element, attributes and
  sheets are used, not that an engine prints or loads them; the spike, the E2E check and the by-hand probe prove that.
  The effect binding made for a stand-in window prints that window, hears its
  `afterprint`, loads into its fonts and sets its title and the app's; `openPrintFrame` makes an `aria-hidden`,
  untabbable, titled, off-screen A4 frame, never `display: none`, copies the app's `link` and non-Emotion `style`
  elements and not Emotion's, rejects when a sheet fails, and `close()` removes exactly that frame.
- **Structural (Vitest scans)**: frames (`iframe`, `srcdoc`, `contentWindow`, `contentDocument`), a window's `print(`,
  `"afterprint"` and `.fonts.load` / `.fonts.ready` named in `browserPrintEffects.ts` only; no `window.print`; no
  `document.title` write under `print/` outside it; nothing under `print/` and nothing in the starter navigates;
  `@emotion/cache` imported by the frame host only; the print content's tree reaches none of the shell's chrome or the
  usage-data reporter, walked from the frame host too.
- **E2E (Playwright, demo data)**: Export PDF on a Team's report asks for the dialog once — the stand-in must reach the
  frame's window — and the reader is still on the report's address with the same history length; the `page.pdf` check
  of the route keeps proving the typeface.
- **By hand, before the rework closes**: Chrome, Firefox and the Linux Standalone app, from the report view and the
  list: the preview shows only the report, light, with the running line and footer on every page; Save and Cancel
  each leave the reader where they were; the offered file name is the report's (noting which title each engine used);
  the dialog opens within 2 s of the click on the dev instance's busiest Team; a throttled connection shows the
  spinner, then the dialog or the snackbar.

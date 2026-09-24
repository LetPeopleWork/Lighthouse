# SPIKE Decisions -- story-6052-server-side-chart-rendering

## Assumption Tested
- Can the backend produce a PDF report whose charts are identical to the frontend's `@mui/x-charts`, on server zips (win-x64, linux-x64) and Docker (amd64, arm64, non-root), without the user installing a runtime? Standalone only needs an in-app report plus PDF export, at near-zero size cost.

## Probe Verdict
- WORKS WITH CAVEATS: headless Chromium (PuppeteerSharp + chrome-headless-shell) rendering the real SPA to PDF works in the aspnet:10.0 image as non-root, at 2.5 s and ~440 MB for a 9-chart page. MUI X server-side rendering does not give identical charts: on the server, text measures 0×0, and the real chart components derive their data in effects that server rendering never runs.

## Probe 2 Verdict (server-side rendering with real font metrics)
- NEAR-IDENTICAL: the server-rendered layout of the real cycle-time scatterplot is pixel-identical to the browser (0 px coordinate delta). The PDF text sits within 0.28 px of Chrome's own PDF. It runs entirely in .NET (Jint + HarfBuzz + Svg.Skia + Skia PDF), with zero apt packages in the aspnet:10.0 image as non-root, at ~0.34 s per chart warm, ~240 MB RSS and +9 MB gz per RID. Costs: ~3 days refactoring five effect-driven chart components, pinning two MUI X hooks at build time, emulating Chrome's text metrics, and a CI fidelity guard. "Identical" is only defined against one reference client configuration: font hinting changes the browser's own tick set.

## Promotion Decision
- DISCARD (no walking skeleton): the user scoped this spike as probe-only. The findings are the deliverable. On 2026-09-24 the user asked to explore further report-generation options before choosing a direction, so the recommended mechanism is not yet decided.

## Design Implications
- Report output is a PDF, stored server-side, viewed in-app and emailed when an email channel is connected.
- A "chart renderer" seam (chart kind × team/portfolio × timeframe → PDF/PNG/SVG) serves reports, PDF export, notification images and marketing screenshots.
- Browser path: a chart-only SPA route with a render-ready flag and a short-lived read-only render token.
- Chart components that derive data in `useEffect` block any non-browser rendering route.

## Constraints Discovered
- The Docker base image is Ubuntu 24.04 noble, not Debian: apt names carry `t64`, and the distro chromium is a snap stub.
- Server zips are self-contained single-file publishes.
- Tauri kills the backend when the standalone app exits, so standalone cannot run unattended jobs.
- The Helm memory limit (1 GiB) has no headroom for an in-process browser.
- Headless Chromium as non-root in Docker requires `--no-sandbox`, so requests must be restricted to Lighthouse's own origin.
- The Ease verdict for #5882 is 3 on the browser route, and 4 on the server-rendering route if it holds across the remaining charts, locales and dark theme.
- Quicksand is loaded from Google Fonts at runtime and not shipped with the SPA, so offline browsers fall back to other fonts. Any server route must ship the font files.

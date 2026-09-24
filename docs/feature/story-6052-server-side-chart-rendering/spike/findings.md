# Spike findings: server-side chart rendering (ADO #6052)

**Phase:** PROBE only (no walking skeleton). **Date:** 2026-09-24. **Timebox:** about 2 h, used in full.
**Probe code, PNGs and PDFs:** session scratchpad `spike_6052/` (throwaway; not in the repo).

Each claim is tagged **[M]** (measured in this probe) or **[D]** (desk-checked from package metadata, workflow files or source; not run).

## The question, as it ended up

The question at the start was: *can the backend produce an email-safe image of a Lighthouse chart in every distribution, with nothing extra for the user to install?* The user narrowed it three times while the probe ran:

1. **What gets built is a PDF.** The backend generates and stores a report PDF, users open it in the app, and it goes out as an email attachment when an email channel is connected. Inline PNG is only a nice-to-have for an email body preview.
2. **Charts must look exactly like the frontend.** That rules out every approach that draws the charts a second time. Only the real `@mui/x-charts` React components will do.
3. **Distribution scope:** server zips (win-x64, linux-x64) and Docker (amd64 + arm64, non-root) are **hard** targets, because they run scheduled jobs with no UI. **Standalone (Tauri)** needs no email, only in-app viewing plus a PDF export, and should cost next to nothing in size.

## Corrections to the brief

- **The Docker base image is Ubuntu 24.04 (noble), not Debian [M].** On noble, apt package names carry a `t64` suffix (`libglib2.0-0t64`, `libasound2t64`, …). Ubuntu's own `chromium` package is a snap stub, so it cannot be apt-installed into a container [D].
- **Server zips are self-contained single-file publishes, not framework-dependent [M]** (`.github/actions/build-backend/action.yml`). The RIDs are win-x64, linux-x64 and osx-arm64 (standalone only).
- **Standalone: Tauri kills the backend sidecar when the app exits [M]** (`kill_sidecar` on app exit in `src-tauri/src/lib.rs`). A scheduled job can therefore only run while the app is open, and the app is never in a position to run a headless job while its window is closed. That is acceptable now that standalone sends no email.
- **Chrome for Testing now publishes linux-arm64 [M]** (HTTP 200, 120.3 MB zip), so multi-arch Docker needs no fallback to a distro package.

## Verdict per mechanism

| Mechanism | Server + Docker (hard) | Standalone (nice-to-have) | Charts identical to the frontend? |
|---|---|---|---|
| **A. Headless Chromium + the real SPA** (PuppeteerSharp → chrome-headless-shell → `page.pdf`) | **WORKS WITH CAVEATS** [M on linux-x64 host and aspnet:10.0 container; D on Windows and arm64] | Disqualified: +99–120 MB per OS, and a second browser engine to notarize | **Yes** [M]: real components, vector PDF, the app's own web font embedded |
| **B. Tauri webview print-to-PDF** | N/A: no webview on a server | **WORKS** [D]: no size cost; WebView2 is Chromium, macOS and Linux are WebKit | Yes on Windows. On macOS and Linux the shapes are identical and text rasterisation differs slightly [D] |
| **C. React SSR of MUI X Charts in-process** (Jint) → SVG → Svg.Skia PDF | **DOESN'T WORK** for "identical" [M] | Would cost only ~3.6 MB | **No** [M]: axis ticks and labels are laid out differently, and the real components render empty (see below) |
| **D. .NET-native redraw** (ScottPlot 5 / SkiaSharp) | Works technically [M]; disqualified | Works technically; disqualified | **No.** Every chart is drawn a second time, so the result is only visually equivalent |
| **E. Gotenberg sidecar** (Chromium over HTTP) | **WORKS WITH CAVEATS** [D]: Docker/Helm only, 504 MB compressed extra image | N/A | Yes: same engine as A |
| browserless sidecar | Rejected: SSPL / commercial licence [D] | N/A | N/A |
| QuestPDF as assembler | Not needed, since Chromium assembles the PDF. Its community licence is revenue-capped, so flag it before any use [D] | N/A | N/A |

## Measurements

### A. Headless Chromium (PuppeteerSharp 25.11, MIT; chrome-headless-shell 153; no Node)

Two workloads: a **synthetic page** (the real MUI X `ScatterChart` with 200 points and 4 percentile reference lines, plus a 30-bar `BarChart`), and the **real Lighthouse Flow Metrics page** (9 charts) from the `ghcr.io/letpeoplework/lighthouse:dev-latest` image with auth off and the "Too Much WIP" demo scenario loaded.

| Workload | Where | Cold | Warm | Peak memory (PSS, whole process tree) |
|---|---|---|---|---|
| Synthetic, 2 charts | host | 0.74–0.78 s (launch + page + PDF) | 176 ms per chart PNG; 350 ms per 2-chart page incl. PDF | 340 MB (about 300 MB of it Chromium) |
| Synthetic, 2 charts | aspnet:10.0, non-root | 0.59 s | 130 ms per chart; 272 ms per page incl. PDF | 333 MB |
| Real Flow Metrics page, 9 charts | host | 0.7 s launch + 3.2 s first page | 2.5 s per page incl. PDF + full-page PNG (≈0.28 s per chart) | 447–473 MB |
| Real Flow Metrics page, 9 charts | aspnet:10.0, non-root | 0.6 s launch + 2.8 s | 2.5 s per page | 426–445 MB |

- **Budget [M]:** warm ≤ 2 s per chart ✔ and cold ≤ 10 s ✔. **Added memory ≤ 300 MB is at the limit for one chart and over it for a 9-chart page (about 400 MB)**, so render one chart or one short report at a time and close the browser after each job.
- **Helm headroom [M]:** the chart limits memory to 1 GiB (`chart/values.yaml`), and the running dev app already sits at about 470 MiB. Chromium on top would reach 0.9 GiB, which is an OOM-kill risk. Raise the limit, or use a sidecar.
- **Output [M]:** 5 A4 pages, 956 KB. The charts are vector with selectable text, and `Quicksand` is embedded. The font comes with the SPA as a web font, so host and container render with the same font and no system fonts are needed for chart text. The page was not designed for print and splits across pages, so a dedicated print route is needed.
- **Docker requirements [M]:** 18 apt packages (`libglib2.0-0t64 libnss3 libnspr4 libatk1.0-0t64 libatk-bridge2.0-0t64 libdbus-1-3 libx11-6 libxcomposite1 libxdamage1 libxext6 libxfixes3 libxrandr2 libgbm1 libexpat1 libxcb1 libxkbcommon0 libasound2t64 libatspi2.0-0t64`, plus `fonts-liberation fontconfig` for non-web-font text).
- **Sandbox [M]:** as non-root, Chromium dies with `FATAL: No usable sandbox`, so `--no-sandbox` is required. That is acceptable only if the browser can load nothing but Lighthouse's own origin, so block every other request.
- **Docker size delta [M]:** +221 MB (libs) + 261 MB (browser) ≈ **+480 MB on disk**, and ≈ **+95–110 MB compressed pull** (the browser alone is 108 MB gzip). The current image is about 603 MB on disk.

### Size cost per distribution variant

| Variant | Browser path (A) | Webview (B) | SSR in Jint (C) | ScottPlot (D) |
|---|---|---|---|---|
| Docker amd64 / arm64 | +95–110 MB pull, +480 MB disk [M amd64; D arm64] | – | +3.6 MB [M] | +6.4 / 5.9 MB gz [M] |
| Server win-x64 zip | 0 if Edge is used; else +120 MB (bundled or downloaded on first use) [M zip size; D Edge] | – | +3.6 MB | +6.8 MB gz |
| Server linux-x64 zip | +120 MB download, **plus the user installs the 18 system libs** [M] | – | +3.6 MB | +6.4 MB gz |
| Standalone win / mac / linux | +120 / 99 / 120 MB, plus notarizing a nested browser on macOS | **≈0** | +3.6 MB | +6.8 / 9.6 / 6.4 MB gz |

Chrome for Testing headless-shell zip sizes, from HTTP HEAD [M]: linux64 119.7 MB, linux-arm64 120.3 MB, win64 120.3 MB, mac-arm64 98.8 MB, mac-x64 104.0 MB.

### C. React server-side rendering of MUI X Charts 9.0.1

- **(a) Fixed `width`/`height` is mandatory [M/D].** With them, `renderToStaticMarkup` produced a complete SVG: 200 circles, 64 lines, 31 bars, 4 reference lines, the same shape counts as the browser [M]. The real Lighthouse charts use `ChartsContainer` sized by their parent through a resize observer [D], and that needs a DOM.
- **(b) Text measurement is stubbed on the server [M].** `internals/domUtils.js` `getStringSize` returns `0×0` when `window` is undefined. As a result, tick thinning is off (44 x-axis labels on the server vs 17 in the browser, so they overlap) and the axis title shifts. The pixel diff against the browser render was **0.94 %, concentrated in the axis-text bands** [M]. The geometry is identical, the text layout is not.
- **(c) The real components render empty [D, from source].** `CycleTimeScatterPlotChart` computes its grouped points and fixed axis domains in `useEffect` → `setState`, which SSR never runs. `WorkItemAgingChart`, `FeatureSizeScatterPlotChart`, `StackedAreaChart` and `WorkDistributionChart` follow the same pattern. Making SSR work means refactoring those components to derive the data during render, and faking text measurement with real font metrics.
- **(d) Runtime [M].** Jint 4.16.3 (BSD-2, pure managed, every RID) ran the 647 KB bundle in-process and produced **byte-identical** output to Node: 1.0 s engine and bundle load, 0.64 s cold, **~95 ms per chart warm**, 141 MB RSS, **≈3.6 MB** added. Node SEA would be about 57 MB per RID [M, local node binary]. ClearScript V8 is about 30 MB per RID [D]. bun `--compile` was not measured (blocked in this environment).
- **(e) SVG → vector PDF [M].** Svg.Skia 5.2.3 + `SKDocument.CreatePdf` produced vector shapes but **dropped all text** on the first attempt (no fonts in the PDF; 3.4 % pixel diff). Svg.Skia 5.x also pins SkiaSharp natives to 3.148. Fixing text needs work on emotion-CSS inlining and font registration.

### D. ScottPlot 5 (MIT), recorded for completeness

It works in aspnet:10.0 as non-root with **zero apt packages, provided the fonts are embedded** [M]. The base image has no `/usr/share/fonts`, and without embedded fonts all text silently disappears. The bold face must also be registered, or titles vanish. Timings: cold 124–226 ms, warm 17 ms per chart, 68 MB RSS. Size: +14–22 MB raw per RID. **Disqualified**: it draws every chart a second time, so it can only ever be "visually equivalent".

## Chart coverage (21 chart components under `components/Common/Charts` plus 3 page widgets)

Cycle-time scatterplot, throughput / arrivals / WIP run charts (bar and line), cumulative state time (CFD-like stacked area), work item aging, process behaviour chart and PBC over time, percentiles over time, feature size scatter, estimation vs cycle time, predictability score, delivery predictability, burn-up, fever chart, Feature size per delivery, blocked items over time, total work item age, work distribution, and the blackout overlays.

| Mechanism | Coverage | Identical |
|---|---|---|
| A. Chromium | 21/21 [D: every one is an ordinary React component; M: the 9 on Flow Metrics] | Yes |
| B. Webview | 21/21 | Yes on WebView2 (Chromium); WebKit text differs slightly |
| C. SSR | Shapes yes. The 5+ components that derive data in effects render empty until refactored | No (text layout) |
| D. Redraw | 0 until written; about 0.5–2 days per chart type, more for the overlays (blackout, pace bands, process behaviour chart limits), and every frontend change needs a matching backend change | No |

## Output format

- **Primary: a PDF attachment** generated by Chromium's `page.pdf`. It is vector, has the fonts embedded, is small (about 1 MB for 9 charts), and prints well.
- **Secondary: an inline PNG via `cid:`** for an email body preview (Chromium screenshot, 10–40 KB per chart).
- **Not SVG in email:** Gmail and Outlook do not render inline SVG [D].

## Recommendation

**Render with headless Chromium, driven from .NET by PuppeteerSharp over the DevTools protocol, against a dedicated render/report route in the SPA the backend already serves.** This is the only mechanism that meets "100 % identical" on server and Docker. Acquire the browser per distribution:

- **Docker / Helm:** bake the libs and chrome-headless-shell into the image (+~100 MB pull, measured), run with `--no-sandbox` with requests restricted to Lighthouse's own origin, and raise the Helm memory limit to about 1.5 GiB. If the image-size increase is unacceptable, the fallback is a **Gotenberg** sidecar (MIT [D], 504 MB compressed extra image, URL → PDF/PNG with a wait-for-expression option). That keeps the main image untouched, but it can reach only Docker/Helm users and needs network reachability plus a render token.
- **Server zips:** on Windows, use installed Edge through the same protocol (Chromium, so identical output) [D]. Otherwise download chrome-headless-shell on first use into the data directory (about 120 MB). **Linux servers must also install the 18 system libs**, so the "nothing to install" constraint cannot be fully met on minimal Linux hosts [M]. Document it and surface a clear "report rendering unavailable: missing X" state. Air-gapped hosts need a manual browser drop.
- **Standalone:** do not bundle a browser. Render the same report route in the Tauri webview and export with print-to-PDF: `window.print()` → the OS "Save as PDF" dialog costs nothing [D]. A silent export needs a small Rust plugin per OS (WebView2 `PrintToPdfAsync`, WKWebView `createPDF` on macOS 11+, which is already the minimum version, and a WebKitGTK print operation), reached through Tauri 2's platform webview handle [D]. This gives one report route and two engines: the PDF matches exactly on Windows, and on macOS and Linux it matches except for text anti-aliasing.

### Common pieces the report path needs (design implications, not built)

- **The "chart renderer" capability** is a good seam: *render chart X for team or portfolio Y over timeframe Z → PDF / PNG / SVG*. On the browser path it is a **chart-only SPA route**, e.g. `/render/chart/{kind}?scope=team&id=…&from=…&to=…`, that mounts exactly one existing chart component at a fixed pixel size, fetches data through the same hooks and services, turns animations off, and sets a **ready flag** (`data-render-ready` or `window.__lighthouseRenderReady`) after data has loaded and two animation frames have passed. The real page has no such flag today; the probe had to poll until the SVG stopped changing, which costs about 0.6 s per page. Reports are a second route that composes several charts, and Chromium does the PDF assembly, so no separate PDF library is needed. This wrapper is cheapest on the browser path. On the SSR path it would sit behind the component refactors described above. For assembly outside a browser, extracting the chart's SVG needs computed styles inlined, so prefer letting `page.pdf` produce the vector PDF.
- **Authentication for the headless browser:** the backend mints a short-lived, read-only render token (HMAC-signed, TTL about 60 s, scoped to the render routes and the GET APIs they call) and passes it as a header (`SetExtraHTTPHeadersAsync`) to `http://127.0.0.1:<port>`. Instances with auth off need no token. With a sidecar, the token travels in Gotenberg's extra headers and the backend must be reachable from the sidecar.
- **Where it runs and what a report costs:** it runs inside the backend's scheduled job (Task Manager) as a short-lived child browser. For a 5-chart report, expect about 0.6 s launch + ~1.5–2.5 s render, 350–450 MB transient memory, and about 0.5–1 MB of PDF. Launch per job and close afterwards, which avoids holding about 300 MB idle.

## Ease verdict for Feature #5882 Email Reports: **3** (was a provisional 4)

This prices the capability as shared infrastructure. The same render route and browser manager also serve PDF export, chart images in signal notifications and sanitised marketing screenshots.

- **Why lower:** "100 % identical" forces a real browser engine onto server and Docker. That brings three browser-acquisition strategies (baked into the image, Edge or first-use download, webview), a Linux system-library prerequisite that cannot be fully removed, an image about 100 MB bigger to pull, a Helm memory bump, a sandbox trade-off, a render-token auth path, a new SPA render route with a ready flag, and browser lifecycle handling (timeouts, crash recovery, zombie processes). None of it is research; all of it is well-trodden. The breadth across six distribution variants is what costs.
- **Why not 2:** the probe measured it working end-to-end in the actual runtime image as non-root, against the real SPA and real charts, within the time budget. PuppeteerSharp needs no Node, and standalone gets its PDF from the webview at no size cost.

### What "100 % identical" costs compared with "visually equivalent"

"Identical" means shipping a browser engine: about 100 MB more to pull in Docker, a first-use download of about 120 MB (or Edge) on server zips, 18 system libraries on Linux servers, 300–450 MB of transient memory per render, and a sandbox exception. In return, every chart the frontend has, and every chart it will ever get, is available on day one with no duplicated drawing code. "Visually equivalent" (a ScottPlot redraw) costs about 6–10 MB per variant, needs no browser, no system libraries and about 70 MB of memory, and runs identically on every OS including standalone. But each chart type is written twice, at roughly 0.5–2 days each for the chart types a report needs, and the two copies drift apart with every frontend chart change. For a report with 3–5 fixed charts, the redraw is cheaper to ship (Ease about 4). For "any chart, anywhere, forever", the browser path is cheaper to own.

## Unverified

- Windows and macOS: nothing was run. Edge-over-DevTools on Windows Server, WebView2 / WKWebView / WebKitGTK print-to-PDF, and macOS notarization are all desk-checked.
- Docker **arm64**: the host has no qemu, so the arm64 image could not run. The Chrome for Testing linux-arm64 build exists (HTTP 200); whether PuppeteerSharp's fetcher resolves it was not checked.
- Gotenberg: not pulled or run; size and licence are from Docker Hub metadata.
- Memory for a **single-chart** render route on the real SPA (only the whole 9-chart page was measured).
- The host-vs-container diff of the real page (0.56 %) is not a controlled comparison: the two runs were minutes apart against live demo data.

## Probe 2: SSR with real font metrics (2026-09-24)

**Phase:** PROBE only (no walking skeleton, no production code). **Timebox:** about 2 h, used in full.
**Probe code and artefacts:** session scratchpad `spike_6052_ssr2/` (throwaway; not in the repo).
Claims are tagged **[M]** (measured in this probe) or **[D]** (desk-checked from source, package metadata or documentation; not run).

### The assumption tested

*If MUI X gets real text measurements from the font the browser uses, does a server render of a real Lighthouse chart produce the same SVG layout as the browser, and can that SVG become a vector PDF with the same text?*

### Verdict: **NEAR-IDENTICAL**

- **Layout: identical [M].** The server-rendered markup of the cycle-time scatterplot, drawn by Chrome, is **pixel-for-pixel identical (0 of 430,080 pixels differ)** to the unmodified production `CycleTimeScatterPlotChart` rendered in the browser. The SVGs have the same structure: 847 elements, 188 markers, 21 text elements with the same labels in the same order, and a **maximum coordinate delta of 0 px** over all 1,485 numeric coordinates (every x, y, transform and path value).
- **PDF: near-identical [M].** The PDF is vector, its text is real text (extractable by `pdftotext`), and it embeds Quicksand as a subset TrueType font. Compared with Chrome's own `page.pdf` of the same chart, every one of the 23 text runs sits within **0.28 px** of Chrome's position (ink-centroid offset, both PDFs rasterised by the same renderer at 4×). Shapes are copied as SVG geometry, so they are exact by construction [D]. Raster pixel diffs only measure the PDF viewer's anti-aliasing, not the content. Our PDF rasterised by poppler differs from Chrome's screen render in **1.54 %** of pixels. Chrome's own PDF, rasterised by the same viewer, differs in **2.66 %**.
- **But "the frontend's render" is not one fixed picture [M].** The same browser, same page and same font produce different tick sets depending on the machine's font-hinting setting. With hinting off, Chrome shows 9 date labels on the x-axis; with Chrome's default Linux hinting, it shows 12, because hinted glyph advances snap to whole pixels (up to 1.34 px wider or narrower per label). The probe reproduces the unhinted, fractional-metric render exactly. Windows (DirectWrite) and macOS (Core Text) each measure text their own way [D]. So "100 % identical" has to be defined against one reference client configuration. The server can match that reference; it cannot match every user's screen at once, and neither can the browser path.

### What made it work

1. **Text measurement [M].** MUI X 9.0.1 `internals/domUtils` (`getStringSize`, `batchMeasureStrings`) returns 0×0 whenever `typeof window === 'undefined'`. Otherwise it builds a hidden `<svg>` under `document.body`, puts an SVG `<text>` in it for each string with the tick style set through `element.style[...]`, and reads `getBBox()`, falling back to `getBoundingClientRect()`. Results are cached per string and style string, capped at 2,000 entries. The tick style passed in already carries `font-family`, `font-weight: 400` and `font-size` (12 px ticks, 14 px axis titles), and `letter-spacing` is `normal` [M, computed styles from the browser].
   - **Shim:** a 30-line `window`/`document` stand-in whose `getBBox()` calls a .NET function. It is installed **after** the bundle has been evaluated, so emotion, which decides between browser and server mode when its module loads, stays in server mode and still inlines its `<style>` tags.
   - **Measurer (.NET, HarfBuzzSharp):** shapes the Quicksand variable font at `wght` = the requested weight, at a scale of font size in 16.16 fixed point, as Chrome does. Three Chrome rules had to be reproduced to get an exact match, found by logging every `getBBox` call MUI made in the browser:
     - the width is the union of the advance run and each glyph's ink box rounded out to whole pixels, because a trailing "6", "e" or "/" pokes past its advance (+0.544 px on every date label);
     - the advance run is rounded **up** to 1/64 px;
     - the height is rounded ascent plus rounded descent (15 px at 12 px, 18 px at 14 px).
   - **Result:** the maximum width error over all 20 measurements MUI made is **0.00034 px**, with 0 px height error. Before the ink rule, the 0.544 px error alone was enough to flip tick thinning from 9 to 12 date labels. The margin on this chart is under 1 px.
2. **Two more effect gates inside MUI X [M].** Besides `getStringSize`, MUI X gates layout behind two "has the client mounted" hooks that only flip inside an effect: `hooks/useMounted` (tick-label overlap thinning uses 0×0 sizes until it flips) and `hooks/useIsHydrated` (the **y-axis title is not rendered at all** until it flips, and label shortening is skipped). The SSR build replaces both modules with `return true` through a 10-line Vite plugin. MUI X is not patched.
3. **Bundle conditions [M].** The chart pulls `WorkItemsDialog`, which pulls services, axios and SignalR. A Node-targeted SSR build imports `node:module`, `http` and similar modules, which Jint cannot load. Building with the `webworker` target and the resolve conditions `worker, browser, import, module, default` makes emotion pick its server-mode `worker` build and axios its browser build. The bundle is 1.5 MB, most of it the dialog's dependencies.
4. **Locale and time zone [M].** Tick labels use `toLocaleDateString()` and day bucketing uses local midnight. Both sides were pinned to en-US and UTC, and Jint then produced "6/7/2026" exactly as Chrome did. Other locales go through .NET `CultureInfo` in Jint and through ICU in Chrome, and were not compared [D: they are likely to differ for some cultures].

### Component effects and sizing

- **Probe surgery [M]:** a copy of `CycleTimeScatterPlotChart` with two changes. (1) The fixed-axis-domain effect and the grouping/filter effect became `useMemo`. (2) `ChartsContainer` got optional `width`/`height` props. **In the browser, the probe copy's SVG is byte-identical to the unmodified production component's SVG**, so the refactor does not change behaviour.
- **Fixed size is enough [M].** With explicit `width`/`height`, `ChartsContainer` needs no ResizeObserver and the axis auto-size selectors see non-zero dimensions. The probe took the production size from the browser (896×480 in a 960×640 card) and passed it in.
- **Other effect-driven charts [D, from source]:**

| Chart component | What the effect does | Refactor |
|---|---|---|
| `Lighthouse.Frontend/src/components/Common/Charts/CycleTimeScatterPlotChart.tsx` | axis domains + grouping → `setState` | effects → `useMemo` (done in the probe copy); ~0.5 day incl. tests |
| `…/Charts/WorkItemAgingChart.tsx` | grouping/filtering → `setState`; also defaults "now" to `new Date()` | `useMemo` + pass "now" explicitly; ~0.5 day |
| `…/Charts/FeatureSizeScatterPlotChart.tsx` | fixed axis maxima → `setState` | `useMemo`; ~0.5 day |
| `…/Charts/StackedAreaChart.tsx` | copies props into state | derive in render; ~0.25 day |
| `…/Charts/WorkDistributionChart.tsx` | **fetches Feature names over HTTP** in the effect | lift the fetch out and pass names in as a prop (the server supplies them); ~1 day |

The other ~15 chart components compute everything during render [D]. All of them still need an explicit-size prop path and a server entry point that feeds props directly (backend DTOs → the chart's frontend model, `Date` objects included), terminology and theme. **About 3 days for the component refactors, plus the SSR entry and data assembly** [D].

### SVG → vector PDF

| Route | Result |
|---|---|
| **(a) Svg.Skia 5.2.3 + Skia PDF — chosen** | **Works [M].** The first probe lost all text for two reasons. (1) The Google Fonts Quicksand file names its family **"Quicksand Light"** (the browser only sees "Quicksand" because the `@font-face` alias says so), so no typeface matched. (2) There are no system fonts in the image. Fix: static 400/500/700 cuts made from the same variable font with fontTools, renamed to family "Quicksand" and registered through `CustomTypefaceProvider`. The emotion CSS is folded into one `<style>` in the SVG, with `rem` converted to `px`. `dominant-baseline` is replaced by an explicit `dy` computed from Quicksand's rounded ascent/descent: hanging 0.8 × ascent, central (ascent − descent)/2, text-after-edge −descent, text-before-edge +ascent. Without that last step the x-axis title sits 1.2 px low. Worst text offset: 0.28 px. |
| (b) resvg 0.45 + svg2pdf 0.13 (Rust) | Also works [M], with the same 0.28 px worst text offset after baseline resolution (0.72 px before). About 24 ms per chart. There are no .NET bindings, so it means shipping and cross-compiling a native binary or cdylib per RID: 3.4 MB raw / 1.6 MB gz per RID [M, linux-x64]. Licences: resvg/usvg are now **Apache-2.0 OR MIT** (no longer MPL-2.0), and svg2pdf is MIT OR Apache-2.0 [M, crate metadata]. |
| (c) Typst | Not tried: route (a) already works. |

**Pipeline, all in one .NET process [M]:** Jint SSR → HarfBuzz measurement → SVG extraction and baseline resolution (C#) → Svg.Skia → `SKDocument.CreatePdf`. The PDF is 153 KB for one chart with the font subset embedded. It is **byte-identical between the host and `mcr.microsoft.com/dotnet/aspnet:10.0` run as non-root (`app`, uid 1654) with zero apt packages**. The image has no `/usr/share/fonts` and no fontconfig, and none is needed, because `SkiaSharp.NativeAssets.Linux.NoDependencies` is used and the fonts ship with the app.

### Measurements [M] (linux-x64 host; the container figures match within 10 %)

| | Cold | Warm | Memory |
|---|---|---|---|
| Jint engine + 1.5 MB bundle load | 1.7 s | – | 146 MB RSS (whole process) |
| SSR of one chart (Jint, HarfBuzz measurement) | 1.2–1.3 s | 165–172 ms | 216 MB RSS |
| End to end, chart → PDF | 1.8 s (3.5 s from process start; 3.7 s in container) | **335–342 ms** | **240 MB RSS peak (whole process)** |
| SVG → PDF via svg2pdf (for comparison) | – | 24 ms | – |

Warm SSR is slower than the first probe's 95 ms because the real component is a bigger tree: the card, legend chips, and a marker, button and title for each of the 188 points.

**Size added per RID [M]** (files a framework-dependent publish adds to the app, excluding the app itself and pdb files):

| RID | Raw | gzip | Largest pieces |
|---|---|---|---|
| linux-x64 | 23.5 MB | 9.2 MB | libSkiaSharp 11.7 MB, libHarfBuzzSharp 3.1 MB, Jint 2.6 MB, SSR bundle 1.5 MB, Svg.* + ExCSS 3.0 MB |
| win-x64 | 22.9 MB | 9.4 MB | libSkiaSharp 12.2 MB, libHarfBuzzSharp 2.1 MB |
| linux-arm64 | 23.3 MB | 9.0 MB | libSkiaSharp 11.5 MB |

Self-contained single-file publishes grow by the same amount (+23.5 MB raw / +9.2 MB gz on linux-x64 against a hello-world baseline). The Rust route (b) would cost about 3 MB (Jint) + 1.5 MB (bundle) + 3.4 MB (svg2pdf) + 2–3 MB (HarfBuzzSharp) ≈ **10 MB raw / ~4 MB gz** per RID, at the price of a Rust cross-build per RID [D, from the measured parts]. Trimming the bundle to the chart path, without the dialog and its services, would save most of its 1.5 MB [D]. Fonts add 0.16 MB.

### Licences

Jint BSD-2-Clause, Acornima BSD-3-Clause, SkiaSharp / HarfBuzzSharp / Svg.Skia / ShimSkiaSharp / ExCSS MIT, resvg / usvg / svg2pdf Apache-2.0 OR MIT, tiny-skia BSD-3-Clause, Quicksand SIL OFL 1.1 (embedding in PDFs allowed) [M, package metadata]. **Flag: `Svg.Custom` (the SVG.NET fork under Svg.Skia) is MS-PL.** MS-PL is permissive and fine for a commercial binary, but source redistributed under it must stay MS-PL [D]. Typst Apache-2.0 [D, not used].

### Fragility, and a guard

The route depends on MUI X internals that carry no stability promise [D]:

- `internals/domUtils`: the `typeof window` check and the DOM calls the shim implements (`createElementNS`, `style[...]`, `replaceChildren`, `children[i]`, `getBBox`);
- the module paths `hooks/useMounted` and `hooks/useIsHydrated`, which the build pins to `true`. **If MUI X renames them or adds a third effect-gated hook, the server output changes silently.** The first visible symptom would be the y-axis title disappearing, or tick thinning switching off;
- emotion's `worker` export condition and its inline `<style>` SSR output.

It also depends on Chrome's text-box rules (ink overflow union, 1/64 px rounding, rounded ascent/descent, the 0.8 × ascent hanging baseline) [M on Chrome 153 headless, Linux]. A Chrome change moves the frontend but not the server.

**Guard:** a CI test that renders a fixed set of charts from fixed data twice, once in headless Chromium (Playwright is already in the repo) with the reference font settings and once through the server pipeline. It asserts identical SVG structure, identical text, a maximum coordinate delta of 0 px, and PDF text offsets ≤ 0.5 px. Run it on every MUI X, emotion, React, SkiaSharp or Chrome update. The probe's `cmp_svg.mjs`, `cmp_measure.mjs` and `textpos.py` are that test in throwaway form.

### Corrections to the first probe

- **Quicksand is not shipped with the SPA [M].** `index.html` loads it from `fonts.googleapis.com` at runtime. An offline or air-gapped browser falls back to Roboto → Arial → sans-serif, so those users never see Quicksand, and their charts are laid out with different metrics. The server route has to ship the font files itself (OFL permits this). The browser route's "font embedded by the SPA" holds only when the browser can reach Google Fonts.
- **Browser layout also races the web font [D].** MUI X caches the first measurement of each label. A chart that mounts before Quicksand has loaded keeps fallback-font sizes until the cache clears. The reference render waited for the font to load first.

### Revised Ease view for Feature #5882 Email Reports if this route holds: **4** (was 3)

- **Why higher:** one mechanism covers all six distribution variants, standalone included, in-process. There is no browser, no system libraries, no sandbox exception, no render token or loopback auth, no Helm memory bump, and no child-process lifecycle. The cost is about 9 MB gz per RID (about 4 MB on the Rust route), about 240 MB process RSS, and about 0.35 s per chart warm. It ran unchanged as non-root in the stock runtime image.
- **What it costs instead:** about 3 days of effect refactors across five chart components, plus a server entry point that feeds props (data, terminology, theme, locale, time zone, "now"); a build step that pins two MUI X hooks; a Chrome text-metrics emulation to maintain; and the CI fidelity guard above, which must gate every upgrade of MUI X, emotion or Chrome.
- **What would push it back to 3:** if "identical" must hold for each user's own screen rather than one reference configuration. Hinted Linux, Windows and macOS render sizes differently in the browser itself. Or if the guard shows MUI X upgrades regularly breaking the shim.

### Unverified

- **Only one chart, one size, light theme, en-US, UTC.** Dark theme, other locales (.NET `CultureInfo` vs ICU in Jint), multi-line labels, legends drawn inside the SVG, and the other 20 chart components were not run.
- **Only unhinted Chrome on Linux** was matched exactly. Hinted Linux (measured to differ), Windows DirectWrite and macOS Core Text references were not modelled. The ink-box rule was checked only on Quicksand digits and Latin letters; glyphs with a negative left bearing were not exercised.
- **arm64** (no qemu on the host), **Windows** and **macOS / Tauri**: published and sized, not run.
- HarfBuzzSharp 8.3 was used for the measurement comparison and HarfBuzzSharp 14.2 (pulled in by Svg.Skia) in the pipeline. The pipeline PDF's text offsets match (0.28 px), but the per-string measurement log was not re-diffed on 14.2.
- Svg.Skia's CSS support beyond this chart's selectors (descendant and class rules), and PDF assembly of several charts per document (one font subset shared across pages), were not tested.

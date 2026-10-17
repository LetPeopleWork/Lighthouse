# ADR-246: Everything a report draws ships with the app — change marks are icons, and the Lighthouse typeface is bundled

- **Status**: Accepted (2026-10-17). The maintainer decided both halves on 2026-10-17: the typeface is bundled for
  the whole app (DISCUSS Q4), and the drawn marks are used everywhere, on screen as well as in print (MQ-2 = a).
- **Feature**: epic-6298-report-pdf-export (ADO Epic #6298), slice 02
- **Deciders**: the maintainer (typeface scope, marks everywhere); Morgan (Solution Architect)
- **Relies on**: ADR-245 (the print route)

## Context

The Linux print probe embedded Quicksand only because the machine was online: the app loads it from
`fonts.googleapis.com` at runtime (`index.html`, weights 400, 500 and 700). An offline or air-gapped instance prints
in a fallback typeface, and so would a minimal server container later. The probe also found that the report's change
marks are text characters Quicksand does not carry, so each machine drew them from its own fonts (LiberationSans,
DejaVuSans), or not at all.

The marks in the report code today are characters:

| Where | Character | Meaning |
|---|---|---|
| Summary, `ThenAndNowSummary.tsx` | ✔ (U+2714), ✘ (U+2718), ± (U+00B1) | Improved, Got worse, Mixed |
| Summary and panels, `NO_CHANGE_MARK` | – (U+2013) | No change |
| Panels, `thenNowComparison.ts` | ▲ (U+25B2), ▼ (U+25BC) | Went up, went down |
| Values | — (U+2014, `DASH`), − (U+2212, in `signed`) | No value; a negative change |

Panels already draw their line icons with `@mui/icons-material` (`NeutralLineIcon`, the Cycle Time levels), so SVG
icons are the house way of drawing a symbol.

The maintainer's condition on the typeface: Quicksand is under the SIL Open Font License 1.1; bundling and
redistributing it with software is allowed, commercial use included, as long as the licence text ships with the
font, the font is not sold on its own, and it is not modified under its reserved name. DESIGN was asked to confirm the
licence against the actual font files.

## Decision

1. **Marks are drawn, not typed.** ✔ ✘ ± ▲ ▼ and the no-change mark become one small component that draws an SVG
   icon for a mark kind (improved, worse, mixed, up, down, none), coloured as today. The comparison code returns the
   kind; the glyph is a view concern. The icons are `aria-hidden`, as the summary's marks already are; the words
   ("Improved", "No change", the signed values) keep carrying the meaning, so a black-and-white print and a screen
   reader lose nothing. Icons come from `@mui/icons-material` where one fits; a mark it lacks (±) is drawn once with
   `createSvgIcon`. **The same icons on screen and in print** (maintainer, MQ-2 = a), so there is one look to
   maintain and the screen stops depending on system fonts too.
2. **Text characters that stay text** are the dash (—), the minus sign (−), the en dash in ranges, the middle dot and
   the ellipsis. They are expected to be in the bundled typeface; the PDF font check below proves it rather than
   assuming it.
3. **Quicksand is bundled for the whole app from `@fontsource-variable/quicksand` 5.3.0** (published 2026-07-19, well
   past pnpm's 24-hour minimum release age; licence field `OFL-1.1`). One import in `main.tsx` replaces the two
   Google Fonts lines in `index.html`. The variable font covers weights 300–700, so the theme's 600 renders as a true
   semibold; to keep today's rendered look, where 600 fell back to the loaded 700, the theme's headings declare 700.
   The theme's font stack names the package's family (`Quicksand Variable`) first, then today's fallbacks.
4. **Size**: the package ships one variable woff2 per subset — latin 28,244 bytes, latin-ext 26,616 bytes, vietnamese
   8,840 bytes — about 64 KB in total, loaded per page by `unicode-range` only when a character needs it. Well inside
   the 1 MB budget for the Standalone installer and the Docker image.
5. **The licence text ships with the font**: `Lighthouse.Frontend/public/licenses/quicksand-OFL.txt`, a byte copy of
   the package's `LICENSE`. `public/` lands in every build output — the backend's `wwwroot` (server zips), the Docker
   build's `dist`, and the Standalone bundle — so every distribution that carries the font carries the licence. A
   Vitest guard fails when the copy differs from `node_modules/@fontsource-variable/quicksand/LICENSE`, so a package
   upgrade cannot leave a stale licence behind.
6. **No runtime request leaves the instance for fonts.** The preconnect to `fonts.gstatic.com` goes too. Nothing
   needs a Content-Security-Policy change: none is set today (`tauri.conf.json` has `csp: null`, the backend sends
   none), and a future `font-src 'self'` would already be satisfied.

## Licence verification (primary sources, read 2026-10-17)

- Google Fonts' own copy, `github.com/google/fonts/blob/main/ofl/quicksand/OFL.txt`, line 1: "Copyright 2011 The
  Quicksand Project Authors (https://github.com/andrew-paglinawan/QuicksandFamily), with Reserved Font Name
  'Quicksand'." Line 3: "This Font Software is licensed under the SIL Open Font License, Version 1.1."
- `ofl/quicksand/METADATA.pb`: `license: "OFL"`, designer Andrew Paglinawan, one variable file `Quicksand[wght].ttf`
  with axis `wght` 300–700.
- npm registry, `@fontsource-variable/quicksand` 5.3.0: `"license": "OFL-1.1"`. The package's own `LICENSE` (4,443
  bytes) opens "Copyright 2019 The Quicksand Project Authors (…QuicksandFamily.git), with Reserved Font Name
  'Quicksand'" and the same OFL 1.1 statement. The two copyright years differ (2011 in Google's file, 2019 in the
  package's, which matches the 2019 variable release); both name the same authors and licence.
- Conditions as they apply here: the licence text ships with the font (decision 5); the font is never sold on its
  own; Lighthouse does not modify the font files. The reserved name applies to modified versions; the files are
  used exactly as Fontsource publishes them, and the subsetting into per-script files is Fontsource's, as it is
  Google's for the files Lighthouse loads today. PDFs the browser writes embed subsets of the font, which the OFL
  allows for documents without putting the document under the OFL.
- Not verified by DESIGN: the byte sizes of the static `@fontsource/quicksand` files (the fetch tool could not reach
  unpkg for that package). Not needed for the decision.

## Alternatives considered

- **`@fontsource/quicksand` (static weights 400/500/600/700).** Same licence (5.x is OFL-1.1; 4.x was mislabelled
  MIT), but four files per subset instead of one, and 600 would need its own file. **Rejected** for size and for
  the one-file simplicity of the variable font.
- **Self-hosted woff2 copied by hand into `public/fonts/`, with our own `@font-face`.** No package to update, but no
  Renovate updates either, a hand-kept `unicode-range`, and nothing tying the licence text to the files. **Rejected.**
- **Bundle the typeface for printing only** (option b of the DISCUSS font question). Two font paths and an offline screen that
  still falls back. The maintainer chose the whole app. **Rejected.**
- **Keep the marks as characters and ship a symbol font** (for example a subset of DejaVu or Noto Symbols). Another
  font file, another licence, and the marks would still not match the app's icons. **Rejected.**
- **Print-only icons, characters on screen.** Two renderings of one meaning that can drift; the screen keeps
  depending on system fonts. Offered as MQ-2 option b; **rejected by the maintainer** (2026-10-17).

## Consequences

- Positive: an offline instance, an air-gapped host and a later server container print the same typeface and the
  same marks; the screen offline gets its typeface too.
- Positive: no request to Google at runtime (one less third party seeing the instance's users).
- Negative: headings change from "600 rendered as 700" to an explicit 700 — the same pixels, a different declared
  value; any test pinning `fontWeight: 600` on headings changes.
- Negative: the mark characters disappear from the DOM text. A test or E2E locator matching "✔", "▲" and friends
  breaks; the crafter greps `Lighthouse.Frontend/src` and `Lighthouse.EndToEndTests/` for each character in the same
  change. Locators target the existing `data-change-kind` / `data-change` attributes, never the icon: MUI strips
  `data-testid` from icons in production builds.
- Negative: the frontend SBOM gains an OFL-1.1 component; the licence review that reads it should expect one.

## Earned trust (what is probed, not assumed)

- **Behavioural, every CI run**: the E2E walking skeleton prints the print route with Chromium (`page.pdf`) and fails
  when any embedded font in the PDF is not Quicksand. That catches a missing glyph (a fallback font would be
  embedded), a font that failed to load, and a regression to runtime loading. It runs with the network as CI has it,
  so it is paired with the offline check below.
- **Structural**: a Vitest guard that `index.html` references no external font host, and the licence-copy guard of
  decision 5.
- **By hand, slice 02**: the app served with no internet (or the Standalone app with networking off); `pdffonts` on
  the PDF lists only Quicksand. Chrome on Windows, Firefox on Linux and the Linux Standalone app compared side by side
  on A4 and Letter.

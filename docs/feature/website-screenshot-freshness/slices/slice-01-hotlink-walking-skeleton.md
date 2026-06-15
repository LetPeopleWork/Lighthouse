# Slice 01 — Hotlink walking skeleton (one image, live)

**Goal:** Prove that one website Lighthouse screenshot can be served live from a GitHub-hosted canonical `docs/assets` PNG and renders reliably on the running marketing site.

## IN scope
- Pick ONE canonical screenshot already in `docs/assets` that is marketing-suitable and matches the current UI (candidate: `docs/assets/features/metrics/metricsoverview.png`).
- In the website repo, replace that one bundled `import` (in `Lighthouse.tsx` / `LighthouseSection.tsx`) with a remote `<img src>` URL.
- Capture the URL convention as a small constant/helper: `cdn.jsdelivr.net/gh/LetPeopleWork/Lighthouse@<ref>/docs/assets/<path>` (D2 host, D3 ref).
- Verify live in a local dev run (browser Network panel: remote fetch 200, correct dimensions, no broken image).

## OUT scope
- The other 9 screenshots (slice-02).
- Adding/regenerating any `@screenshot` test (slice-02).
- The finalization gate (slice-03).
- Video and OG/SEO image (D4, D5).

## Learning hypothesis
Disproves *"GitHub-hosted hotlinking renders reliably for our marketing site"* if the image fails to load, is CORS-blocked, rate-limited, or served with a content-type that breaks rendering. If `raw.githubusercontent` fails, confirm jsDelivr (D2) resolves it.

## Acceptance criteria
- One image renders from the remote URL on the running website (Network-panel verified), correct dimensions, no broken-image icon.
- URL-convention helper exists and is reused-ready for slice-02.

## Dependencies
None. Canonical asset already exists; both repos present.

## Effort / reference class
~2–3h. Reference class: the existing per-feature screenshot wiring; trivial website markup change.

## Pre-slice SPIKE
Low uncertainty mechanically, but D2 (raw vs jsDelivr) is the whole point — this slice IS the spike. Try `raw.githubusercontent` first; if blocked/unreliable, switch to jsDelivr and record which won.
</content>

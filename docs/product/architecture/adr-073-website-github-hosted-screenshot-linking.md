# ADR-073: Website Marketing Screenshots Are Hotlinked from Lighthouse `docs/assets` via the jsDelivr GitHub CDN at `@main`

> **Scope: spans the WEBSITE repo (`/storage/repos/website`, `LetPeopleWork/website`) AND the Lighthouse repo's `@screenshot` E2E pipeline + finalization process.** ADR numbering continues the Lighthouse-product sequence by team convention; ADR-031 set the precedent that website-repo decisions live in this numbering. Number 073 chosen as the lowest free above the committed maximum on disk (066) and above Epic #5074's reserved-but-unwritten 067–072 range.

## Status

Accepted (DESIGN wave, 2026-06-14). All substantive choices locked in DISCUSS (feature-delta D1–D7, ADO #5259); this ADR formalizes the mechanism.

## Context

The marketing website (`LetPeopleWork/website`, a Vite + React + TS app) bundles its own copies of 10 Lighthouse product screenshots under `src/assets/screenshots/*.png` and `import`s them into `src/pages/Lighthouse.tsx` and `src/components/LighthouseSection.tsx`. These copies are stale 2025-era UI captures (verified: `Metrics_Team_1.png`, `Metrics_Project_1.png`, `ALM_Connection.png` all show pre-2026 layouts). Refreshing one means a maintainer manually re-exporting from the product and committing an image into a separate repo — a cross-repo chore that is reliably skipped, so the site silently drifts from the shipped UI.

Meanwhile the Lighthouse repo already runs a `@screenshot` Playwright E2E suite (`Lighthouse.EndToEndTests/tests/specs/screenshots/Screenshots.spec.ts` via `tests/helpers/screenshots.ts` → `getPathToDocsAssetsFolder()`) that regenerates ~105 canonical PNGs under `docs/assets/**` per feature finalization, keeping the public docs site current. Those assets are public on `main`. The opportunity: make that one already-maintained pipeline the single source of truth for marketing imagery too, instead of running a parallel, hand-maintained image set.

The riskiest assumption is that GitHub-hosted hotlinking renders reliably on the live marketing site (CORS, content-type, rate-limit, cache). DISCUSS sequenced US-01 as a walking skeleton to prove it live before bulk migration.

## Decision

**The website references the canonical `docs/assets/**` PNGs over HTTP through the jsDelivr GitHub CDN, pinned to `@main`, instead of bundling local copies.**

1. **Host = jsDelivr (`cdn.jsdelivr.net/gh`).** URL shape:
   `https://cdn.jsdelivr.net/gh/LetPeopleWork/Lighthouse@main/docs/assets/<path>.png`.
2. **Ref = `@main`.** Tracks the default branch where `@screenshot` regen lands at feature finalization (before release). jsDelivr caches branch refs ~12h — acceptable for marketing stills.
3. **One small URL helper in the website repo** (`lighthouseAsset(path: string): string`, ~10 LOC, e.g. `src/lib/lighthouseAsset.ts`) builds the CDN URL from a `docs/assets`-relative path. It is the single place the host + repo + ref convention lives, so the ref is a one-line swap (`@main` → a release tag) if ever wanted. Every migrated `<img>` uses `lighthouseAsset(...)` instead of an `import`.
4. **The OG/SEO image is excluded.** `website/public/forecasts-project.png` (referenced by `SEO.tsx`, `sitemap.xml`, JSON-LD `screenshot`/`thumbnailUrl`) stays website-hosted and same-origin. Social scrapers and SEO crawlers require a stable same-origin absolute URL and reject many cross-origin/redirecting sources (D5).
5. **The `GitHub.png` website screenshot is excluded.** It is a screenshot of github.com (the repo README), not a Lighthouse product surface — the `@screenshot` suite screenshots the running Lighthouse app and structurally cannot produce it. It stays website-bundled / manually refreshed (same class of exclusion as the OG image).
6. **Freshness is held by a manual finalization gate, not automated drift detection** (D7). The in-repo `CLAUDE.md` "DELIVER Wave — Docs & Screenshots at Finalization" mandate gains a Website-freshness item, referenced from `nw-finalize`. An automated check is the named escalation if the gate proves insufficient.

This introduces **no new backend architectural style, no API contract, no persistence, and no RBAC surface.** The website's relationship to `docs/assets` is a driven dependency on an external CDN (jsDelivr) fronting a produced artifact (the canonical PNGs); the Lighthouse-side producer (the `@screenshot` suite) is unchanged in mechanism and only gains marketing-gap shots where the canonical set has no equivalent.

## Alternatives Considered

### CDN host: raw.githubusercontent.com (rejected)
Serve via `raw.githubusercontent.com/LetPeopleWork/Lighthouse/main/docs/assets/...`.
- **Rejected because**: it is not a CDN — it is rate-limited, sends `Content-Type: text/plain; charset=utf-8` (which breaks `<img>` rendering in some browsers/proxies), and GitHub explicitly discourages it for hotlinking. jsDelivr is the standard, cached, correctly-typed CDN front for GitHub files and is the reason US-01 proves the choice live before bulk migration.

### Bundle-and-copy, keep importing local PNGs (status quo, rejected)
Continue committing screenshot copies into `website/src/assets/screenshots/` and refresh them per release.
- **Rejected because**: this is exactly the drift source the feature exists to remove. It duplicates the canonical asset, requires manual cross-repo effort the maintainer reliably skips, and has no single source of truth. The whole value (KPI: 0 per-release website screenshot commits for covered surfaces) depends on not doing this.

### Freshness ref: pin to the latest release tag instead of `@main` (rejected for now)
Pin the jsDelivr URL to a released tag (e.g. `@v26.6.7.1` or jsDelivr `@latest`) so prospects see only released UI.
- **Rejected because**: the user judged released-vs-main "not that big of a deal" and asked for the simpler option (2026-06-14). `@main` needs no tag-resolution step and the canonical assets already land on `main` at finalization (pre-release), so `@main` is current and correct for marketing stills. The only cost is jsDelivr's ~12h branch cache, irrelevant here. The URL helper makes a later swap to a release tag a one-line change, so this is deferred, not foreclosed.

### Automated drift detection / CI check (rejected for this story)
Add a CI job that visually diffs each website Lighthouse image against the latest canonical asset and fails on drift.
- **Rejected because**: out of scope for a small story; the manual finalization gate (D7) is the lightweight first step. US-03's learning hypothesis names the trigger to escalate to automation: if the next finalized UI-changing feature still ships a stale website image, build the automated check then.

## Consequences

- **Positive**: one maintained asset pipeline serves both docs and marketing; covered website screenshots become current automatically when the `@screenshot` suite regenerates them at finalization; target of 0 manual per-release website screenshot commits for covered surfaces. The URL helper localizes the convention to one ~10-LOC function.
- **Positive**: no backend change, no API contract, no client (CLI/MCP) impact, no RBAC surface — the blast radius is website markup + a few new `@screenshot` tests + two process-doc edits.
- **Negative**: the website now has a runtime dependency on jsDelivr availability and on `main` not regressing an asset. A broken `main` asset or a jsDelivr outage shows a broken marketing image. Mitigated by: marketing stills tolerate the ~12h cache; the walking skeleton (US-01) proves live reliability before bulk migration; KPI "hotlink reliability = 100%" is link-checked on the deployed site.
- **Negative**: two screenshots cannot use the mechanism — the OG/SEO image (D5) and `GitHub.png` (a non-product surface). Both are explicitly carved out and stay website-hosted, so "GitHub-hosted everywhere" is not literally true; the design names them rather than silently omitting them.
- **Negative**: freshness depends on a human honoring the manual gate. Accepted for this story; US-03 names the escalation to an automated check.
- **Follow-up (platform-architect handoff)**: jsDelivr is an external integration (driven dependency of the website at runtime). A lightweight link-check / smoke test on the deployed site is recommended to detect a CDN or asset-path regression — the contract-test analogue for a static-asset CDN. No consumer-driven contract (Pact) applies, as there is no typed API surface.

# DESIGN Wave Decisions — website-screenshot-freshness (ADO #5259)

Architect: Morgan (Solution Architect) · Mode: PROPOSE (all decisions pre-locked in DISCUSS; no interactive questions) · Date: 2026-06-14 · Density: lean.
Scope: application/component (cross-repo wiring + process). **No new backend architectural style, no API contract, no persistence, no RBAC surface, no Lighthouse-Clients impact.**
Outcome Collision Check (`nwave-ai outcomes check-delta`): **SKIPPED** — no new backend typed-contract surface (website markup + E2E screenshots + a process gate); out of the code-feature-pipeline scope.
Per-wave review: deferred to the consolidated DISTILL gate.

## Key Decisions

| ID | Decision | ADR |
|---|---|---|
| DDD-1 (=D1) | `docs/assets` is the single source of truth for marketing screenshots; the website references the canonical PNGs the `@screenshot` suite already regenerates, not bundled copies. | ADR-073 |
| DDD-2 (=D2) | Host = jsDelivr GitHub CDN (`cdn.jsdelivr.net/gh`). raw.githubusercontent rejected (not a CDN, rate-limited, `text/plain`). Proven live by US-01. | ADR-073 |
| DDD-3 (=D3) | Ref = `@main` (simpler; tracks default branch where regen lands at finalization; ~12h cache OK for marketing stills; release-tag pin is a one-line later swap). | ADR-073 |
| DDD-4 (=D4) | Video out of scope. | — |
| DDD-5 (=D5) | OG/SEO image (`public/forecasts-project.png`) stays website-hosted same-origin (SEO/social scrapers need a stable same-origin URL). | ADR-073 |
| DDD-6 (=D6) | One-time refresh = the 10 current website screenshots; each mapped, gap-filled, or explicitly excluded (no silent omission). | ADR-073 |
| DDD-7 (=D7) | Freshness held by a manual finalization gate (CLAUDE.md DELIVER mandate + `nw-finalize`); no automated drift detection this story. | ADR-073 |
| DDD-8 (NEW) | URL convention lives in one ~10-LOC website helper `lighthouseAsset(path)` at `src/lib/lighthouseAsset.ts`; the ref swap is localized here. | ADR-073 |
| DDD-9 (NEW) | `GitHub.png` is a structural exclusion (a github.com README, not a product surface the `@screenshot` suite can produce); stays website-bundled. | ADR-073 |
| DDD-10 (NEW) | Marketing-gap `@screenshot` tests extend the existing `Screenshots.spec.ts` (driven from `testWithDemoData`, written via `getPathToDocsAssetsFolder()`), not a new file. | ADR-073 |

## Architecture Summary

The Lighthouse product architecture (ports-and-adapters / hexagonal, ADR-027) is **unchanged**. This is a cross-repo asset-flow + process feature spanning the Lighthouse repo (canonical-asset generation + finalization gate) and the separate `LetPeopleWork/website` repo (marketing-site consumption).

The website stops bundling 10 stale screenshot copies (`src/assets/screenshots/*.png`, imported into `Lighthouse.tsx` / `LighthouseSection.tsx`) and hotlinks the canonical `docs/assets/**` PNGs through the jsDelivr GitHub CDN at `@main`, via a single ~10-LOC `lighthouseAsset()` helper. The `@screenshot` E2E suite (unchanged mechanism) is the sole producer of those PNGs; it gains marketing-gap shots only where the canonical set has no equivalent. A manual finalization gate keeps the website fresh.

C4: System Context & Container unchanged for the Lighthouse product; the delta is the cross-repo wiring (website → jsDelivr → `docs/assets` ← `@screenshot` suite + finalization gate). Diagrams in the feature-delta DESIGN C4 section and `docs/product/architecture/c4-diagrams.md`.

## 10 → canonical mapping (sizes slice-02)

| Verdict | Count | Items |
|---|---|---|
| MAP cleanly to existing canonical asset | 5 | ALM_Connection→`concepts/worktrackingsystem_AzureDevOps`, Metrics_Team_1→`features/metrics/metricsoverview`, Metrics_Team_2→`features/metrics/*`, Forecasts_Team_Manual→`features/teamdetail`, Forecasts_Project→`features/portfoliodetail` |
| MAP pending crop/tab confirmation at audit | 3 | Query_Configuration (connector-wizard crop), Metrics_Team_2 (Flow-Metrics view choice), Forecasts_Team_Epics (team Features tab) |
| LIKELY NEW `@screenshot` gap | 2 | Metrics_Project_1, Metrics_Project_2 (portfolio Metrics tab + charts — no single canonical crop) |
| STRUCTURAL EXCLUSION | 1 | GitHub.png (non-product surface, DDD-9) |

Net: ~2 confirmed new `@screenshot` tests likely (portfolio metrics), up to ~2 more pending the audit. At the US-02 learning-hypothesis threshold (≤2 gaps ⇒ canonical set sufficient; >2 ⇒ dedicated marketing shot set). Slice-02's first task is the audit that resolves items 2/4/8 and confirms the final gap count.

## Reuse Analysis

| Existing asset | Verdict |
|---|---|
| `@screenshot` → `docs/assets` pipeline (`Screenshots.spec.ts` + `screenshots.ts`, `getPathToDocsAssetsFolder()`) | **REUSE / EXTEND** — add gap shots; no parallel pipeline. |
| `testWithDemoData` fixture | **REUSE** — drives new shots cheaply (destructure `testData`). |
| 105 canonical `docs/assets/**` PNGs | **REUSE** — 5–8 of 10 website shots map directly. |
| `Lighthouse.tsx` / `LighthouseSection.tsx` | **EXTEND** — bundled `import` → `lighthouseAsset()` URL; no layout redesign. |
| `MediaCarousel` / `Carousel` | **REUSE (unchanged)** — accept an `src` string already. |
| `CLAUDE.md` DELIVER mandate + `nw-finalize` | **EXTEND** — Website-freshness gate mirrors the existing docs/screenshot discipline. |
| `lighthouseAsset()` helper | **CREATE NEW (justified)** — no remote-asset helper exists in the website repo today (all refs are bundled imports); ~10 LOC. |

## Tech Stack

- **Asset CDN:** jsDelivr `cdn.jsdelivr.net/gh` pinned `@main` (free; correctly-typed `image/png`; cached). raw.githubusercontent rejected.
- **Website:** Vite + React 19 + TypeScript (existing, unchanged) — only `<img src>` values move from bundled imports to CDN URL strings + one helper.
- **E2E:** Playwright `@screenshot` suite (existing, unchanged mechanism; `testWithDemoData`).
- **URL helper:** plain TS function (~10 LOC), no new dependency.

## Constraints

- External CDN dependency (jsDelivr) at website runtime — the highest-risk boundary. Probed live by US-01; deployed-site link-check at the platform handoff.
- Two surfaces cannot use the mechanism: OG/SEO image (DDD-5) and `GitHub.png` (DDD-9) — both stay website-hosted, named explicitly.
- Freshness depends on a human honoring the manual gate (DDD-7); US-03 names the escalation to an automated check if it fails.
- DELIVER discipline: any new `@screenshot` test must be run live against a clean backend before commit (project rule); `bun run build` clean; no dead imports remain.

## Upstream Changes

None. All DISCUSS decisions (D1–D7) were already locked; DESIGN only formalized them and added DDD-8/9/10 (helper placement, the GitHub exclusion, gap-shots in the existing suite) — none of which change any story, AC, or KPI. No `design/upstream-changes.md` needed.

## Platform-Architect Handoff (external integration annotation)

- **jsDelivr GitHub CDN is an external integration** (driven dependency of the website at runtime). Recommended: a lightweight **deployed-site link-check / image smoke test** verifying each hotlinked marketing image returns 200 with `Content-Type: image/png` at correct dimensions. This is the static-asset-CDN analogue of a contract test — **no consumer-driven contract (Pact)** applies, as there is no typed API surface. KPI "hotlink reliability = 100%" measures it.

## Deferred

- DISTILL: per-story Gherkin (US-01 live-render of one image from the CDN URL; US-02 all-10 mapped/gap-filled/excluded + no dead imports; US-03 gate present + non-skippable). 4-reviewer gate.
- DELIVER: the slice-02 mapping audit (resolves items 2/4/8, confirms gap count); the ~2 portfolio-metrics `@screenshot` tests (run live); the website edits + `lighthouseAsset()` helper + bundled-copy removal; the CLAUDE.md gate + `nw-finalize` reference.

# Evolution — website-screenshot-freshness (ADO #5259)

**Finalized:** 2026-06-15 · **Type:** cross-repo asset-flow + process · **Density:** lean
**Repos:** `LetPeopleWork/Lighthouse` (canonical-asset generation + finalization gate) · `LetPeopleWork/website` (marketing-site consumption)

## Summary

The marketing website bundled its own stale copies of Lighthouse product screenshots
(`website/src/assets/screenshots/*.png`), so they drifted out of sync after every UI change
and were only noticed at release. The Lighthouse `@screenshot` E2E suite already regenerates
canonical PNGs into `docs/assets/**` per feature. This story made the website **consume those
canonical assets** over the jsDelivr GitHub CDN (`@main`) instead of bundling copies, and added a
**finalization gate** so the drift cannot re-accumulate unseen.

Keeping the marketing site honest is now a side effect of work already done per feature, rather
than a separate cross-repo chore.

## Business context

- **Persona:** lighthouse-maintainer (`docs/product/personas/lighthouse-maintainer.yaml`).
- **Job:** `job-maintainer-keep-website-current` (`docs/product/jobs.yaml`) — keep the website's
  screenshots current without a manual re-export into a separate repo.
- **Journey:** `docs/product/journeys/website-screenshot-freshness.yaml`.
- No RBAC, no API contract, no persistence, no Lighthouse-Clients impact. Website is the primary surface.

## Key decisions (see ADR-073)

| ID | Decision |
|---|---|
| D1 | `docs/assets` is the single source of truth for marketing screenshots; the website references canonical PNGs, not bundled copies. |
| D2 | Host = jsDelivr GitHub CDN (`cdn.jsdelivr.net/gh`); raw.githubusercontent rejected (not a CDN, rate-limited, `text/plain`). |
| D3 | Ref = `@main` (simpler; tracks the default branch where regen lands at finalization). |
| D5 | OG/SEO image (`public/forecasts-project.png`) stays website-hosted same-origin (social/SEO scrapers need a stable same-origin URL). |
| D7 | Freshness held by a manual finalization gate (CLAUDE.md DELIVER mandate + `nw-finalize`); no automated drift detection this story. |
| DDD-8 | URL convention isolated in one ~10-LOC website helper `lighthouseAsset(path)` (`src/lib/lighthouseAsset.ts`). |
| DDD-9 | `GitHub.png` is a structural exclusion (a github.com README, not a product surface the suite can produce); stays website-bundled. |
| DDD-10 | Marketing-gap `@screenshot` tests extend the existing `Screenshots.spec.ts` (driven from `testWithDemoData`), not a new pipeline. |

ADR: `docs/product/architecture/adr-073-website-github-hosted-screenshot-linking.md`.
Architecture context: `brief.md` + `c4-diagrams.md` cross-repo asset-flow sections.

## Steps completed

| Slice | Step | Outcome |
|---|---|---|
| 01 | 01-01 | `lighthouseAsset()` helper + Vitest (incl. leading-slash normalization) + one image hotlinked via jsDelivr. Website commit `935e102`. |
| 02 | 02-01 | Portfolio-metrics overview `@screenshot` test added to `Screenshots.spec.ts`, run live, canonical `features/metrics/portfoliometricsoverview.png` produced. Lighthouse commits `383625f3` (capture) + `aba72fa2` (stabilize with predictability-score wait + regenerate). |
| 02 | 02-02 | Remaining 6 marketing images migrated to `lighthouseAsset()` CDN URLs; 9 dead bundled PNGs removed; source-inspection migration-guard test added. Website commit `1243f08`. |
| 03 | 03-01 | Website-freshness gate added to the CLAUDE.md DELIVER docs mandate (non-skippable, requires an explicit `N/A, because …`). Lighthouse commit `7643ba2d`. |

Workspace + SSOT artifacts committed in Lighthouse `52cfa746`.

## Mapping outcome (US-02 hypothesis)

The 10 original website screenshots resolved to **6 referenced marketing slots** mapping to canonical
`docs/assets` assets, requiring only **1 net-new canonical shot** (`portfoliometricsoverview.png`) —
well under the ≤2-gap learning-hypothesis threshold. **Hypothesis confirmed:** the canonical
`@screenshot` set is sufficient for the marketing site; no dedicated bespoke marketing-shot slice was needed.
`GitHub.png` (DDD-9) and the OG/SEO image (DDD-5) remain website-hosted by design.

## Verification

- Website Vitest green (173 passed, incl. `lighthouseAsset` unit + normalization + migration guard); production build clean.
- All 6 referenced assets resolve through jsDelivr `@main` (HTTP 200, `image/png`) after the Lighthouse push.
- Portfolio-metrics overshot re-run live against a clean local backend; PNG inspected (Project Apollo metrics overview, predictability score rendered).

## Lessons learned

- **Date-relative demo data ages screenshots silently.** Running the portfolio-metrics test also
  refreshed `featureSizeProcessBehaviourChart.png` (20.8 KB → 30.2 KB) purely because the demo
  window had shifted — a concrete instance of the staleness this feature fights. Committed it as a refresh.
- **Stabilize before capturing full-page marketing shots.** The first portfolio-metrics capture could
  race a half-loaded dashboard; waiting on the predictability score's `%` text before the screenshot
  makes the asset deterministic.
- **Push order matters for `@main` hotlinks.** A net-new canonical asset 404s on jsDelivr `@main` until
  the Lighthouse repo is pushed. The cross-repo sequence is Lighthouse → verify CDN → website.

## Follow-ups (deferred, not blocking)

- US-03 escalation: an automated deployed-site image smoke test (each hotlinked image returns 200 /
  `image/png`) if the manual gate proves insufficient — the static-asset-CDN analogue of a contract test.
- Release-tag pin: `@main` → `@vX.Y.Z` is a one-line `lighthouseAsset()` base swap if cache freshness ever matters.

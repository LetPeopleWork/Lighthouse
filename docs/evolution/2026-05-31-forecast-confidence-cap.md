# Evolution: forecast-confidence-cap

- **Date finalized**: 2026-05-31
- **ADO**: Story #5126 — "Never show 100% Confidence" (persona delivery-forecaster)
- **Status**: Shipped to `main`, CI-green (run 26704177193).
- **Workspace (history)**: `docs/feature/forecast-confidence-cap/`

## What shipped

Forecast likelihoods that read as a near-certainty no longer display a precise,
deterministic-looking figure. When a likelihood is **> 95% and the item still has
remaining work**, every surface renders **`>95%`** instead of `96–100%`. A
genuinely complete item (no remaining work) is exempt and still reads `100%`
(D4). At or below 95%, the precise value is shown unchanged.

The intent is a communication-honesty fix for the forecaster's leadership
conversation: a `>95%` band reads as "very likely, not guaranteed" in one glance,
so deterministic-thinking stakeholders stop locking onto a screenshotted "100%"
as a committed date.

Surfaces routed through the cap:

- Manual forecast headline (`ForecastLikelihood`)
- Portfolio delivery header chip + per-feature likelihood column (`DeliverySection`)
- Delivery overview chips (`DeliveriesChips`)

## Key decisions

Full decision log (D1–D5, with verbatim user framing) lives in the workspace
`feature-delta.md`. The load-bearing ones:

- **D1 — bucket, not clamp**: `> 95%` collapses to a single `>95%` label rather
  than clamping to 99.9%. Removes 97/98/99 false-precision too, not just 100%.
- **D2 — FE-only, DTOs unchanged**: `>95%` is a *label*, not a number. The numeric
  DTO fields (`ManualForecastDto.Likelihood`,
  `DeliveryWithLikelihoodDto.LikelihoodPercentage`,
  `FeatureLikelihoodDto.LikelihoodPercentage`) stay `double`. No backend production
  code changed. See **ADR-038**.
- **D4 — completed items exempt**: a finished item is a fact, not a probabilistic
  forecast; the cap applies only to likelihoods computed from remaining work + a
  target date. The remaining-work signal is already present at every FE call site
  (`remainingItems`, `delivery.remainingWork`, `row.getRemainingWorkForFeature()`)
  — no DTO field needed to enforce the exemption.
- **D5 — RAG bands unchanged**: `>95%` lands in the existing "Certain" band, fed
  the raw numeric likelihood. The cap label and the RAG band are orthogonal, both
  derived from the same raw number.
- **D3 — min-data guard out of scope**: deferred to ADO #5125 "Don't Forecast with
  too little Data".

## Architecture (ADR-038, Option A)

A single pure frontend helper owns the whole rule:

```ts
formatLikelihood(value, { hasRemainingWork, precision: 'round' | 'fixed2' })
// value > 95 && hasRemainingWork  → '>95%'
// else precision==='round' ? `${Math.round(value)}%` : `${value.toFixed(2)}%`
```

Routed through all four likelihood render sites; `ForecastLevel` (RAG) keeps
receiving the raw number. Chosen over a backend display field (Option B, redundant
once the old-server fallback is considered) and a hybrid DTO field (Option C, an
unnecessary contract change). No new dependencies (fast-check intentionally absent).

## Slices

| Slice | Scope |
|-------|-------|
| 01 | `formatLikelihood` helper + boundary matrix; three delivery surfaces routed |
| 02 | Manual forecast headline routed (propagation of the validated rule) |
| 03 | ADR-038 DES-5 enforcement close-out (FE structural drift guard + NUnit DTO-stability reflection guard) |

## Mutation testing

Feature-scoped Stryker on the single mutation target `formatLikelihood.ts`.
Report: workspace `deliver/mutation/mutation-baseline.md`.

- **100.00% (16/16 mutants killed)**, no survivors. The boundary-matrix `it.each`
  (`94.9 / 95 / 95.01 / 100` × `hasRemainingWork ∈ {true,false}` × `precision ∈
  {round, fixed2}`) is a complete oracle for the strict-`>` boundary, the `&&`, the
  precision branches, the `>95%` literal, and the threshold constant. Threshold
  `per-feature ≥ 80%` met.

## Lessons learned

- **A new display format breaks every E2E POM that parses the number back.** The
  `>95%` label made `parseFloat(">95")` → `NaN` in three forecast POM getters
  (`TeamDetailPage.forecast`, `DeliveryItem.getLikelihood/getFeatureLikelihoods`);
  the demo Team Zenith manual forecast hits the new branch, so `verifysqlite` /
  `verifypostgres` went red. A clean local build did not catch it — only the live
  E2E did. Fix: tolerate an optional leading `>` in the parse regexes. Ledger entry
  added to `docs/ci-learnings.md`. (Rule: grep E2E POMs for parse-back getters when
  changing how a number renders.)
- **MCR 403 on the Docker base-image manifest is a transient infra flake**, not a
  code failure; it self-healed on the next push. Ledger entry added.

## Follow-ups (open)

- **Lighthouse-Clients (CLI + MCP)**: non-blocking, presentation-only. No new
  endpoint → no version gate. The clients should apply the same `>95%` rendering
  *only if* they print a likelihood to a human; raw-JSON-only ⇒ N/A. File a follow-up
  clients task; the numeric value clients receive is unchanged, so a client that
  does nothing stays correct.

## Pointers

- Decision log + slices + ACs: `docs/feature/forecast-confidence-cap/feature-delta.md`, `slices/`
- ADR: `docs/product/architecture/adr-038-forecast-confidence-cap-display-formatter.md`
- Architecture delta: `docs/product/architecture/brief.md` → "Application Architecture — forecast-confidence-cap"
- Journey: `docs/product/journeys/forecast-confidence-cap.yaml`
- Mutation report: `docs/feature/forecast-confidence-cap/deliver/mutation/mutation-baseline.md`
- Frontend: `src/utils/forecast/formatLikelihood.ts`, `components/Common/Forecasts/ForecastLikelihood.tsx`, `pages/Portfolios/Detail/Components/DeliveryGrid/DeliverySection.tsx`, `components/Common/DataOverviewTable/DeliveriesChips.tsx`
- DTO-stability guard: `Lighthouse.Backend/Lighthouse.Backend.Tests/API/DTO/ForecastLikelihoodCapDtoStabilityTest.cs`

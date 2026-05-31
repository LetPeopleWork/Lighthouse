# ADR-038: Forecast-confidence cap lives in a frontend shared formatter, not a backend display field

## Status

Accepted — 2026-05-30 (DESIGN wave, feature `forecast-confidence-cap`, ADO #5126)

## Context

DISCUSS locked five decisions for "Never show 100% Confidence":

- **D1** — a forecast likelihood strictly `> 95%` (with remaining work) renders as the label `">95%"`; `≤ 95%` shows the precise value unchanged.
- **D2** — numeric DTO fields stay `double` and unchanged (`ManualForecastDto.Likelihood`, `DeliveryWithLikelihoodDto.LikelihoodPercentage`, `FeatureLikelihoodDto.LikelihoodPercentage`). `">95%"` is a presentation label, not a number.
- **D4** — genuinely completed items (no remaining work) are **exempt**; they still read `100%`/Done. The rule must distinguish "a forecast that computed high" from "nothing left to forecast".
- **D5** — `ForecastLevel` RAG bands are unchanged; `">95%"` lands in the existing default "Certain" band.

The question this ADR resolves: **where does the `">95%"` rule live** so that every likelihood surface (manual forecast headline + portfolio delivery chip + portfolio overview chip + per-feature chip) applies it consistently and the CLI/MCP clients do not drift?

### Decisive grounding read (D4 signal availability per surface)

| Surface | Render site | Likelihood source | Remaining-work signal available *at the call site*? |
|---|---|---|---|
| Manual forecast | `ForecastLikelihood.tsx` | `likelihood` prop | **Yes** — `remainingItems` prop already present (from `ManualForecastDto.RemainingItems`) |
| Delivery chip | `DeliverySection.tsx` | `delivery.likelihoodPercentage` | **Yes** — `delivery.remainingWork` on the `Delivery` model (from `DeliveryWithLikelihoodDto.RemainingWork`) |
| Portfolio overview chip | `DeliveriesChips.tsx` | `delivery.likelihoodPercentage` | **Yes** — `delivery.remainingWork` |
| Per-feature chip | `DeliverySection.tsx` likelihood column | `fl.likelihoodPercentage` (`IFeatureLikelihood`) | **Yes — via the row**: `row.getRemainingWorkForFeature()` (the full `IFeature` is in scope in `renderCell`). `FeatureLikelihoodDto`/`IFeatureLikelihood` itself carries **no** remaining-work field, but the sibling feature row does. |

The critical finding: **every frontend call site can already source the D4 remaining-work signal locally**, with zero DTO change. The per-feature chip is the only non-obvious case, and it is satisfied by the feature row already bound in the same cell.

## Decision

**Option A — a single frontend shared formatter.** Introduce one small pure helper:

```
formatLikelihood(likelihoodPercentage: number, options: { hasRemainingWork: boolean }): string
```

Rule: `likelihoodPercentage > 95 && hasRemainingWork → ">95%"`; otherwise the surface's existing precise format (`Math.round(...)%` for chips, `toFixed(2)%` for the manual headline). To preserve each surface's existing precision, the helper returns the bare label only for the capped case and the caller keeps its own precise formatter for the uncapped case — equivalently, the helper takes a `precision: "round" | "fixed2"` option and owns both branches. DESIGN prefers the latter (one function owns the whole decision; callers pass intent).

Consumed by all four surfaces. Numeric DTOs untouched (honours D2). D4 sourced locally at each call site per the table above. `ForecastLevel` is constructed from the raw numeric `likelihoodPercentage` as today, so styling/banding is unchanged (honours D5).

CLI/MCP clients **adopt the same rule independently** where they print a likelihood to a human (presentation-only, non-blocking — see Consequences and the cross-cutting note).

## Alternatives Considered

### Option B — backend carries a derived display hint (e.g. `LikelihoodBand` / `IsCapped` field on the DTOs)

All consumers (FE + CLI + MCP) inherit one source of truth from the server; the rule is computed once in `DeliveryWithLikelihoodDto.FromDelivery` / `ForecastController.RunManualForecastAsync`.

- **Rejected.** It adds a *new* typed contract surface to three DTOs to express a pure presentation concern, which sits uncomfortably against D2's spirit ("`">95%"` is a label, not a number — keep the DTOs stable for API consumers"). A new `IsCapped`/band field is exactly the kind of derived display state D2 wanted to keep off the wire. It also forces a server-version dependency for a formatting nuance: an old server would omit the field and new clients would have to fall back to computing the rule themselves anyway — so the FE rule must exist regardless, making the backend field redundant. Per-feature D4 would still need `FeatureLikelihoodDto.RemainingWork` added (a real contract change) to compute server-side, whereas FE already has it via the row.

### Option C — hybrid (backend exposes `RemainingWork` per feature for robustness; FE owns the rule)

Add only `FeatureLikelihoodDto.RemainingWork` so the per-feature D4 signal is explicit on the contract, keep the formatting rule in the FE helper.

- **Rejected as unnecessary.** The grounding read shows the per-feature feature row already exposes `getRemainingWorkForFeature()` in the exact cell that renders the chip. Adding a contract field to remove a one-line local lookup is not worth a DTO change and a clients-repo ripple. Kept on the table as the fallback **if** a future surface renders a per-feature likelihood *without* the feature row in scope — at which point add the field then, justified by that surface.

## Consequences

**Positive**

- Zero contract change; numeric DTOs stay `double` and stable (D2 honoured exactly).
- One pure, fully unit-testable function owns the whole D1/D4 decision; boundary tests (94.9 / 95.0 / 95.01 / 100 / remaining-work=0) live in one place — strong for the ≥80% mutation gate.
- `ForecastLevel` and RAG bands untouched (D5) — the formatter and the level are orthogonal, both fed by the same raw number.
- No server-version coupling for a formatting nuance.

**Negative / accepted**

- The rule is implemented twice in spirit — once in the Lighthouse FE, once in each client that renders likelihood to a human (CLI/MCP). This is **deliberate**: a presentation rule belongs to each presentation layer. Drift risk is mitigated by (a) the rule being a one-line threshold, (b) a shared boundary-test table documented in the feature delta that the clients repo can mirror, and (c) a follow-up clients task (non-blocking).
- Four FE call sites must each pass the correct `hasRemainingWork` source. Mitigated by an enforcement test (below).

## Architectural Enforcement

| Rule | Mechanism |
|---|---|
| Every likelihood-rendering FE surface routes through `formatLikelihood` (no raw `Math.round(likelihood)%` / `toFixed(2)%` on a forecast likelihood) | Vitest structural/grep test asserting the four call sites call `formatLikelihood`; no inline likelihood formatting remains in `DeliverySection`, `DeliveriesChips`, `ForecastLikelihood` |
| Numeric DTO fields unchanged (D2) | NUnit reflection test asserting `ManualForecastDto.Likelihood`, `DeliveryWithLikelihoodDto.LikelihoodPercentage`, `FeatureLikelihoodDto.LikelihoodPercentage` remain `double` with no new band/cap sibling field |
| D4 boundary behaviour | Vitest unit tests on `formatLikelihood` at 94.9 / 95.0 / 95.01 / 100 with `hasRemainingWork` true and false |

## Clients consistency verdict

Likelihood rides existing DTOs; **no new endpoint → no `FEATURE_REQUIRES_SERVER_NEWER_THAN` version gate**. Clients adopt the `">95%"` rule **only if** they render a likelihood to a human; if they emit raw JSON only, this is **N/A**. This is a one-line follow-up in the clients repo and **does not block** #5126. The numeric value the clients receive is unchanged, so a client that does nothing remains correct (it just shows the precise high number) — there is no breakage, only a missed honesty improvement.

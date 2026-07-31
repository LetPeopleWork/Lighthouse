# Slice 01 — Licence price auto-flips at the cutover instant (page + checkout)

**Feature**: pricing-adjustment-2026-08 · **ADO**: #5563 · **Stories**: US-01, US-02
**Ships**: 2026-07-31 (deploy in daylight; effect at 2026-07-31T22:00:00Z) · **Estimate**: ~4h

## Goal

One pinned instant drives both what the pricing page displays and what Stripe charges, so the CHF 999 →
CHF 2,000 change happens at Swiss midnight with no deploy at that moment.

## IN scope

- New `src/lib/pricing.ts`: `PRICE_CUTOVER_INSTANT = "2026-07-31T22:00:00Z"`, pre/post Self-Service
  prices, and a resolver taking `now`.
- Rewire S2-S7 to read from it: banner (`Lighthouse.tsx:1055-1063`), tier card (`:1099`), rate chip
  (`:1102`), checkout block (`:1386-1393`), JSON-LD offers (`Lighthouse.tsx:534`, `Index.tsx:86-95`).
- Post-cutover: banner, rate chip and lock-in note render nothing at all (D8).
- `supabase/functions/create-payment/index.ts`: pure `resolvePriceId(now)` selecting the CHF 999 id
  before the instant and the new CHF 2,000 id at-or-after it.
- Vitest fake-clock tests at three clocks (−1 min, exact instant, +1 min) for both the page and the
  resolver; plus the constant-identity test (AC-1.5) and the no-stray-literal grep test (AC-1.6).
- One live Stripe **test-mode** checkout session observed post-cutover-clock (AC-2.3).

## OUT of scope

- Services ladder (slice 02). Static surfaces S8/S9/S10 and gate removal (slice 03).
- Enterprise price. Promo codes / grandfathering (D2). Moving the price id to a Supabase secret.
- Archiving the CHF 999 Stripe price — explicitly forbidden until slice 03 (AC-2.4/AC-4.5).

## Learning hypothesis

**Disproves** "a single pinned instant can drive both a browser render and a server-side Stripe
decision without a deploy" **if** a checkout session created at 22:01Z still bills CHF 999, or the page
still prints CHF 999 on a reload after 22:00Z.
**Confirms**, if it holds, that D4's whole ship-early-flip-later approach is sound — and slice 03
becomes a pure deletion rather than a rescue.

## Acceptance criteria

Verbatim from feature-delta: **AC-1.1 … AC-1.6** and **AC-2.1 … AC-2.5**. The two that carry the slice:

- Reload `/lighthouse` either side of `2026-07-31T22:00:00Z` (faked in test, real in production) → the
  Self-Service card reads `CHF 999` then `CHF 2,000`, with no deploy between.
- A real Stripe test-mode checkout session created at a post-cutover clock shows `CHF 2,000.00`.

## Dependencies

- **BLOCKING**: the live CHF 2,000 `price_...` id from Benjamin (D5). Code can be written and unit-tested
  against a placeholder, but the slice does not ship until the real id is wired.
- Test-mode twin of that price for AC-2.3.
- Deploy access before 22:00Z on 31 July. **If the deploy misses the instant, this degenerates to a
  manual cutover** — flag immediately rather than shipping late.

## Reference class

Comparable to the JSON-LD/meta surfaces already threaded through `Lighthouse.tsx` and to the existing
`create-payment` edits. No new dependency, no schema, no migration. The only unfamiliar element is
clock-dependent rendering, which the repo's vitest setup already supports.

## Pre-slice SPIKE

Not required. The one uncertainty — whether JSON-LD is baked at build time and therefore cannot flip —
was resolved during DISCUSS: `scripts/prerender-meta.mjs` rewrites only `<head>` title/description/OG,
and `grep -c priceCurrency dist/index.html` = 0.

## Dogfood moment

Same day: the maintainer runs the suite with the clock faked past the instant and sees the future site
render CHF 2,000 before deciding to deploy. That is the sign-off that the night is unattended.

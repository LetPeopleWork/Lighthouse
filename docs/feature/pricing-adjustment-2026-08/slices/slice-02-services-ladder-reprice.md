# Slice 02 — Services ladder moves on the same gate + static surfaces go flat

**Feature**: pricing-adjustment-2026-08 · **ADO**: #5563 · **Story**: US-03
**Ships**: 2026-07-31 (services gated to 22:00:00Z; static files ungated, live on deploy) · **Estimate**: ~2h

## Goal

The pilot and the Flow Clarity Assessment reprice at the same instant as the licence, so the site never
shows two different offerings at the same CHF 2,000 and the Assessment keeps its service margin — and
the three crawler-facing static files state CHF 2,000 flat from today (revised D9).

## IN scope

- `src/components/EngagementPath.tsx:50` — Implement/BYOD pilot **CHF 2,000 → CHF 3,000** (D10).
- `src/components/ExpertiseAndServices.tsx:126` — Flow Clarity Assessment **CHF 3,500 → CHF 4,500**,
  retaining "· Includes Lighthouse Premium License" verbatim (D10).
- Both read `pilotPrice` / `assessmentPrice` from slice-01's `src/lib/pricing.ts`, gated on the same
  `PRICE_CUTOVER_INSTANT`.
- Fake-clock tests either side of the instant for both surfaces, plus an assertion that Transform and
  Flow Health Check are untouched.
- **Static surfaces, ungated (revised D9)** — `public/llms.txt:63,82`, `public/compare/index.html:99`,
  root `index.html:76`: Self-Service stated as **CHF 2,000/year flat**, transitional qualifiers
  ("today", "from August 2026") removed. These are plain files with no runtime, so they cannot gate;
  they go live on the 31 Jul deploy, a few hours ahead of the real price. Accepted (D9): a crawler
  caching 2,000 early beats one caching 999 for a week.

## OUT of scope

- The React licence surfaces and anything gated (slice 01) — the static files state the *post*-cutover
  price while the app still renders CHF 999 until 22:00Z. That divergence is intentional, not a bug.
- **Transform** (`EngagementPath.tsx:67`, from CHF 10,000) — unchanged (D10).
- **Flow Health Check** (`ExpertiseAndServices.tsx:133`, from CHF 200/team · CHF 500/portfolio) — unchanged.
- **Lighthouse Setup & Introduction** (Free) — unchanged.
- The licence price and anything on the Stripe path (slice 01). These are `mailto:` CTAs; no checkout
  exists for services and none is added here.

## Learning hypothesis

**Disproves** "the pricing module from slice 01 is a genuine single source of truth for *every* price on
the site, not just the licence" **if** wiring two service prices through it needs a second mechanism, a
second cutover constant, or leaves a literal behind.
**Confirms**, if it holds, that slice 03's cleanup is a one-file deletion.

Second, smaller hypothesis (revised D9): **disproves** "S8/S9/S10 were the complete set of
non-auto-flipping surfaces" **if** slice 03's AC-4.2 grep still finds a price literal after this slice —
meaning the DISCUSS inventory was incomplete.

## Acceptance criteria

Verbatim from feature-delta: **AC-3.1 … AC-3.6**.

- Pre-cutover clock: Implement `CHF 2,000`, Assessment `CHF 3,500`.
- Post-cutover clock: Implement `CHF 3,000`, Assessment `CHF 4,500`.
- Transform and Flow Health Check identical at both clocks.
- Post-cutover, no two different offerings on one rendered page state the same CHF figure for licence
  and pilot.
- `llms.txt`, `/compare/` and the root `<noscript>` block state CHF 2,000/year flat, agreeing verbatim
  with each other on the price sentence.

## Dependencies

- `src/lib/pricing.ts` from slice 01 (module only — **not** the Stripe id). If the Stripe handover
  stalls, this slice still ships: it is the reason the split exists.

## Reference class

Copy-constant edits to two presentational components that already take their prices from a typed
`price: string` field. Lower risk than slice 01 — no payment path, no server code.

## Pre-slice SPIKE

Not required.

## Dogfood moment

Same day: view `/` at both clocks and read the engagement path top to bottom — Free → CHF 3,000 → from
CHF 10,000 — checking the ladder still reads as a ladder against a CHF 2,000 licence.

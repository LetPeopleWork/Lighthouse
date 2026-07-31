# Slice 03 — Post-cutover cleanup: gate out

**Feature**: pricing-adjustment-2026-08 · **ADO**: #5563 · **Story**: US-04
**Ships**: 2026-08-01, after the instant has passed · **Estimate**: ~1h

## Goal

Delete the date gate, the pre-cutover branch and the transitional JSX, so the site states one price flat
and the codebase carries no unreachable branch. (The static surfaces moved to slice 02 under revised D9;
this slice re-verifies rather than edits them.)

## IN scope

- ~~llms.txt / compare / noscript~~ — **moved to slice 02 by revised D9**; they ship 31 July, ungated.
  This slice only re-verifies them (AC-4.1).
- `src/lib/pricing.ts` — delete `PRICE_CUTOVER_INSTANT` and the pre-cutover prices; export flat values.
- `supabase/functions/create-payment/index.ts` — delete `resolvePriceId` and the CHF 999 id; inline the
  CHF 2,000 id.
- Replace every clock-dependent test with a flat assertion; delete the constant-identity test (AC-1.5)
  along with the constants it guarded.
- Verify no transitional copy survives: banner, rate chip and lock-in note were already gated off by
  slice 01 — remove the now-dead JSX rather than leaving it behind a `false`.

## OUT of scope

- Any price *number* change — this slice changes zero prices; it removes the machinery that already
  changed them.
- Enterprise, Transform, Flow Health Check.
- Stripe **archival** of the CHF 999 price object: that is a manual dashboard action by Benjamin
  **after** this slice ships (AC-4.5), not part of the slice.

## Learning hypothesis

**Disproves** "the gate was the only thing holding the old price alive" **if** the AC-4.2 grep finds a
price literal after the deletion — meaning the DISCUSS inventory missed a surface and something told
visitors the wrong price across the cutover.
**Confirms**, if it holds, that the inventory was complete and the next price change is a one-constant
edit.

## Acceptance criteria

Verbatim from feature-delta: **AC-4.1 … AC-4.5**. The one that carries the slice:

- `curl -s https://letpeople.work/llms.txt | grep -i self-service` reports `CHF 2,000/year` with no
  transitional qualifier, and `grep -rn "999" src public index.html | grep -v 999999` returns zero
  price hits.

## Dependencies

- The cutover instant must have **passed** (`2026-07-31T22:00:00Z`). Shipping this before the instant
  would flip the site early and break the pre-cutover branch.
- Slices 01 and 02 deployed and observed working across the instant.

## Reference class

Deletion slice against code written the previous day by the same hands. Risk is confined to deleting one
branch too many — bounded by `pnpm test` + `pnpm build` (AC-4.4).

## Pre-slice SPIKE

Not required.

## Dogfood moment

Ask an AI assistant "what does Lighthouse cost?" after the deploy and check the answer says CHF 2,000
flat — that is the surface `llms.txt` exists to serve, and the only way to confirm the crawler copy
landed.

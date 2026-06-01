# Slice 01: Shareable `/survey` page → anonymous store → dashboard read

**Feature**: lighthouse-user-survey
**Repo**: website (`/storage/repos/website`)
**Stories shipped**: US-01, US-02, US-05 (read view)
**Estimate**: ~1 day / ≤ 6h

## Goal

Ship the standalone, shareable survey page at the stable public `/survey` route, rendering
PLACEHOLDER questions, that stores submitted answers **anonymously** in the shared Supabase
platform (5123's response table, survey source/kind), and is readable by the maintainer in
5123's internal dashboard survey view. NO Lighthouse app change — shippable and shareable on
Slack / LinkedIn the moment it lands.

## IN scope

- `/survey` route added to the website router (above the catch-all), rendering the current
  question config (questions as data/config, not hardcoded into the route).
- Submit → anonymous response write to 5123's response table tagged `kind = user-survey`.
- Thank-you confirmation shown ONLY on a confirmed successful write.
- Graceful "survey temporarily unavailable" if the question config can't load.
- Retry-able error on Supabase write failure (no false thank-you; answers preserved).
- Idempotent / de-duplicated submit (no double-counting).
- Minimal maintainer survey view on 5123's dashboard listing responses (anonymous).

## OUT scope

- Trial opt-in / email (slice 03).
- Editable-questions-as-config guarantee proven end-to-end (slice 02 hardens it; slice 01 just
  renders from config).
- Any in-app Lighthouse nudge (slices 04-05).
- Redesigning 5123's platform (D6 — reuse only).

## Learning hypothesis

**Confirms if it succeeds**: community users will answer an anonymous, no-login survey when given
a shareable link, and 5123's generalized response table accepts a survey `kind` without redesign.
**Disproves if it fails**: either nobody answers (the channel premise is wrong), or 5123's schema
needs changes to host survey responses (platform-extension assumption broken → escalate to 5123).

## Acceptance criteria

See US-01, US-02, and US-05 (read) in `../feature-delta.md`. Slice specifics:

- Navigate to `/survey` (direct + via a shared link) → same page renders with no login.
- Submit a complete response → row appears in Supabase with `kind = user-survey`, no PII columns
  populated → thank-you shown.
- Force a write failure → retry-able error, no thank-you, answers preserved.
- Double-submit → one logical response recorded.
- Maintainer opens the dashboard survey view → the response is listed, anonymous.

## Dependencies

**Hard**: 5123 has shipped the shared Supabase response table (source/kind discriminator) and the
minimal internal dashboard (D6). If not, this slice blocks on 5123 — the top scheduling risk.

## Production data requirement

**Preferred.** After deploy, the link is shared in the real Lighthouse community Slack; real
community responses (not synthetic) populate the dashboard.

## Dogfood moment

Post-deploy: share `https://<website>/survey` in the Lighthouse community Slack; watch real
anonymous responses land in the dashboard survey view within the first day.

## Pre-slice spike candidates

- 30 min: confirm 5123's response table + insert path (RLS-guarded insert vs edge function) is
  available on `main` and accepts a new `kind` without migration.
- 15 min: confirm `react-router-dom` v7 route addition pattern + the website build/deploy of a new
  page.

# ADR-041: Trial-lead capture — forked `capture-survey-lead` Edge Function (no score/band, no results email)

> **Scope: WEBSITE repo (`/storage/repos/website`) + its Supabase project, NOT the Lighthouse product.** Authored for ADO Epic #5124; extends 5123's `leads` table + Edge-Function PII path (ADR-032/034).

## Status

**Superseded by [ADR-046](adr-046-survey-submission-and-team-notification.md)** (same DESIGN wave, 2026-05-31). The new per-submission team-notification requirement forces every submission through one server-side function, so the trial-lead write is folded into the consolidated `submit-survey` Edge Function rather than a standalone `capture-survey-lead`. **The reasoning below is carried forward and still binding**: do NOT bend `capture-lead`'s anti-forgery invariant; never anon-INSERT PII; the trial lead is a distinct concept that shares the `leads` table (not a function) with the assessment lead. Read this ADR for the *why*; read ADR-046 for the *resulting shape*.

Accepted (DESIGN wave, 2026-05-31)

## Context

US-04 lets a respondent volunteer an email to request a premium trial (`wantsTrial = true`). Per the PII discipline (D3) and ADR-032/034, the email is the ONLY PII and MUST be written through a `service_role` Edge Function into the separate `leads` table — never via the anon key.

5123's existing `capture-lead` function (`supabase/functions/capture-lead/index.ts`) is **assessment-shaped**:

- it **requires** an integer `score` and a non-empty `band` (`parsePayload` returns null otherwise);
- it runs **`bandMatchesScore(score, band)`** anti-forgery re-derivation (ADR-032 security boundary);
- it **sends a Mailgun results email** rendering the assessment band.

A survey trial opt-in has **no score, no band, and warrants no results email** — the human follow-up is manual and out-of-band (D4). Forcing the survey through `capture-lead` would require making score/band optional, skipping `bandMatchesScore`, and skipping the email — i.e. branching the function's core invariant on `source`, eroding the very anti-forgery guarantee that justifies 5123's function.

## Decision

**A separate `capture-survey-lead` Edge Function, modelled on `capture-lead` but for the trial concept** — score/band-free, no results email.

- Accepts `{ source: "user-survey-trial", email, wantsTrial: true }`. Validates with the **same email pattern** as `capture-lead` and rejects any payload carrying a `score`/`band` (the survey lead must NOT smuggle scored fields — fail closed on unexpected shape).
- Inserts into the SAME `leads` table (ADR-034) with `score = null`, `band = null`, `wants_trial = true`, `source = "user-survey-trial"`. The `leads.score`/`leads.band` columns are already nullable, so no schema change is needed for the lead itself.
- **No Mailgun send.** The trial is issued manually (D4, no-auto-issuance); the function records the signal + email only and returns `{ recorded: true }`.
- **No `bandMatchesScore`** — there is no score to re-derive against, and the anti-forgery property does not apply to a non-scored lead. The trust-boundary validation that DOES apply (email format, exact `source` allowlist, rejection of stray scored fields) is enforced.
- Both functions share the email pattern and CORS/`service_role` plumbing via the existing `supabase/_shared/` modules; the band re-derivation (`_shared/bands.ts`) and results-email (`_shared/resultsEmail.ts`) are imported ONLY by `capture-lead`.

This is the deliberate **"don't abstract different business concepts"** call from CLAUDE.md: results-gated lead-capture (an exchange: email → unlock breakdown, with anti-forgery on the claimed band) and trial-opt-in (a signal: "a human should contact me") are different business concepts that will evolve independently. They share a table, not a function.

## Alternatives Considered

- **(a) Widen `capture-lead` — make score/band optional, skip `bandMatchesScore` + skip the email when `source = "user-survey-trial"`**: rejected. It branches the function's load-bearing anti-forgery invariant on a `source` string, so a future edit to the assessment path risks the survey path and vice versa; it also couples two PII writes that have different validation needs into one mutable surface. The DRY saving (a shared `serve` skeleton) is real but small and is captured instead via the shared `_shared/` plumbing.
- **(c) Direct anon-INSERT of the trial lead**: rejected outright — violates ADR-032/034 (the `leads` table has NO anon policy; PII must go through `service_role`). A trial email written by the anon key would breach the structural-anonymity + PII-sealing guarantee.
- **A single generic `capture-pii` function with a `type` switch**: rejected — same coupling problem as (a) plus a leaky generic payload; the two concepts are clearer as two named functions.

## Consequences

- **Positive**: the assessment anti-forgery path is untouched (no regression risk to 5123); the trial path has exactly the validation it needs and no results-email coupling; PII stays sealed behind `service_role`; structural anonymity (ADR-034) preserved (the trial lead carries no answers, no FK to `responses`).
- **Negative**: a second Edge Function to deploy (`deploy-supabase-functions.yml` gains an entry) and reason about; mitigated by shared `_shared/` plumbing and a near-identical test shape.
- **External integration note**: `capture-survey-lead` is first-party (same Supabase project + Mailgun NOT invoked), so no consumer-driven contract test is required. Its request shape is a contract between the `EdgeFunctionSurveyLeadCapture` adapter (ADR-040 port) and the function — covered by a shared zod intent + a component test asserting degrade behavior on non-200. DISTILL can assert: *"a survey trial POST carrying a `score`/`band` is rejected with no `leads` row written"* and *"no Mailgun call is made for a survey trial."*

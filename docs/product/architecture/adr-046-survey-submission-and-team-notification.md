# ADR-046: Consolidated `submit-survey` Edge Function + per-submission team notification email

> **Scope: WEBSITE repo (`/storage/repos/website`) + its Supabase project, NOT the Lighthouse product.** Authored for ADO Epic #5124; supersedes the survey-write split in ADR-040 (anon-INSERT adapter), ADR-041 (separate `capture-survey-lead`), and ADR-042 §RLS-widening (DS-1). Extends 5123's Mailgun transport (`_shared/mailgun.ts`, ADR established for #5138) and the two-table model (ADR-034).

## Status

Accepted (DESIGN wave, 2026-05-31). Supersedes the relevant parts of ADR-040/041/042 noted above.

## Context

A new user requirement (2026-05-31): **the maintainer must receive an email on EVERY survey submission**, to `survey.answer@letpeople.work`, stating that a survey was filled in **and whether the respondent left an email** (so a human can action the trial-license follow-up).

This is a *team* notification (inbound ops alert), distinct from 5123's *visitor* results email (`_shared/resultsEmail.ts`). Two facts make it architecturally load-bearing:

1. **It fires on every submission**, not only on trial opt-in — so it cannot live solely in a path that runs only when an email is volunteered.
2. **Email requires server-side secrets** (the Mailgun API key lives in Supabase Edge-Function secrets, never in the browser) — so the submission MUST traverse a server-side function.

The DESIGN as first drafted wrote the anonymous response via the **anon-INSERT** path (`SupabaseResponseRepository` → `responses`, RLS-allowlisted, ADR-040/042 DS-1) and the trial lead via a **separate** `capture-survey-lead` Edge Function (ADR-041). Neither path sends a team email, and the anon-INSERT path never reaches server code at all. Satisfying the new requirement under that split would need an *additional* always-invoked notify function plus the existing two paths — three server touchpoints for one submission.

## Decision

**Route the whole survey submission through a single `submit-survey` Edge Function (service_role)** that performs, in order:

1. **Validate** the payload (answer shape against a shared zod intent; optional `email` against the same `EMAIL_PATTERN` as `capture-lead`; reject any stray `score`/`band` — fail closed on unexpected shape).
2. **Insert the anonymous response** into `responses` (`source = 'user-survey'`, `kind`, `answers` jsonb; `raw_sum`/`score`/`band` = null) using `service_role`.
3. **If** `wantsTrial && valid email`: **insert the trial lead** into `leads` (`source = 'user-survey-trial'`, `email`, `wants_trial = true`, `score`/`band` = null) using `service_role` — the same target ADR-041 chose, now in-process.
4. **Send ONE team notification email** to `survey.answer@letpeople.work` via the existing `_shared/mailgun.ts` transport, rendered by a new `_shared/surveyNotificationEmail.ts`. The body summarises the answers and states the trial status: either *"No trial requested"* or *"Trial requested — contact: <email>"*. **Degrade-open**: a Mailgun failure is caught, logged, and does NOT fail the submission (the response is already recorded) — mirroring 5123's `sendResultsEmail` degrade-open pattern.
5. **Return** `{ recorded: true }`, or a structured partial-failure (`response` saved / `lead` failed) so the page can surface the journey's partial-write error path (the trial opt-in specifically failed → user can retry the opt-in).

### What this supersedes

- **No anon-INSERT for survey responses** → migration `0003` RLS-allowlist widening (DS-1, ADR-042) is **no longer needed**; the `responses_anon_insert` policy stays scoped to `'readiness-assessment'`. Survey answers can no longer be injected by an arbitrary anon client — a security tightening, not just a simplification.
- **No separate `capture-survey-lead`** (ADR-041) → the trial-lead write is step 3 here. ADR-041's *reasoning* (don't bend `capture-lead`'s anti-forgery invariant; never anon-INSERT PII) is **carried forward and still honoured**: `submit-survey` is a distinct function from `capture-lead`, the assessment anti-forgery path is untouched, and PII still flows only through `service_role`.
- The driven port `SurveySubmission.submit(answers, trialOptIn?)` (one adapter, `EdgeFunctionSurveySubmission`) replaces the split `ResponseRepository.save` (anon) + `SurveyLeadCapture.capture` adapters for the survey kind. The shared `responses`/`leads` *tables* and the *ports widening* (ADR-040 unions + non-scored shape) are unchanged.

## Alternatives Considered

- **(b) Keep anon-INSERT response + `capture-survey-lead`, add a separate always-invoked `notify-survey` Edge Fn**: rejected. Three server/anon touchpoints per submission; the notify fn would need the answers re-sent from the client anyway, and coordinating the team email's "did they leave an email?" line across two independent calls (lead vs notify) is racy and duplicative. Consolidation is strictly simpler.
- **(c) Postgres insert trigger / database webhook (pg_net) firing the email**: rejected for v1. Adds DB-side HTTP infrastructure and secret handling outside the Edge-Function model 5123 established; harder to test from the website hexagon; the trial-status correlation would require a join the two-table model deliberately avoids.
- **(d) Send the team email from the existing `capture-lead`/results path**: rejected — that path is assessment-shaped and visitor-facing (ADR-041); reusing it re-introduces the exact concept-coupling ADR-041 rejected.

## Consequences

- **Positive**: one server path, one email per submission, server-validated answers, tighter RLS (no public anon survey insert), PII still sealed behind `service_role`, partial-write semantics expressible. Reuses the `_shared/mailgun.ts` transport (no new dependency). DB-level structural anonymity (ADR-034: two tables, no FK join) is **preserved**.
- **Anonymity trade-off (flagged for the user)**: the team email **correlates answers with the volunteered email ONLY for trial opt-ins** — i.e. only for respondents who *chose* to identify themselves for a trial. An anonymous-only submission's notification carries **no identity** (no email exists to correlate). This transient inbox-level correlation for opt-ins is the explicit operational intent (action the trial). If a stricter bar is later wanted, the notification for opt-ins can omit the answer summary and carry only "a trial was requested — see the dashboard," keeping answers and email un-correlated even in the inbox. Recorded as an open question, not blocking v1.
- **Negative / operational**: `survey.answer@letpeople.work` must be a **real, deliverable inbox** (mailbox or forwarding rule) — a USER MANUAL STEP, alongside the existing Supabase Mailgun secrets (`MAILGUN_API_KEY`, `MAILGUN_DOMAIN`, `MAILGUN_API_BASE`, `MAILGUN_FROM`) which `submit-survey` reuses. No new secret is introduced; the recipient is a constant in `_shared/surveyNotificationEmail.ts`.
- **DISTILL hooks**: assert (1) a notification email is sent on a submission with NO opt-in, stating "no trial requested" and carrying no email; (2) on an opt-in, the notification states the trial + the volunteered email and a `leads` row is written; (3) a Mailgun failure still records the response and returns success (degrade-open); (4) survey answers are written via `service_role`, never the anon key (no anon `responses` policy for `'user-survey'`); (5) a payload with a stray `score`/`band` is rejected with no rows written.

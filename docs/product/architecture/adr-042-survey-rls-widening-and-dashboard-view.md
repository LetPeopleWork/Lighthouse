# ADR-042: RLS anon-insert widening for `user-survey` + survey dashboard view

> **Scope: WEBSITE repo (`/storage/repos/website`) + its Supabase project, NOT the Lighthouse product.** Authored for ADO Epic #5124; extends 5123's RLS (ADR-032/034) + dashboard (ADR-033).

## Status

Accepted (DESIGN wave, 2026-05-31). **Seam (1) — RLS anon-insert widening (migration `0003`, DS-1) — is SUPERSEDED by [ADR-046](adr-046-survey-submission-and-team-notification.md)**: survey responses now write via the `service_role` `submit-survey` Edge Function (because every submission must trigger a server-side team email), so the anon-insert allowlist does NOT need `'user-survey'` and `responses_anon_insert` stays scoped to `'readiness-assessment'`. **Seam (2) — the dashboard survey view — STANDS unchanged** and is the operative part of this ADR.

## Context

Two seams resolve here. *(Seam (1) below is retained for the record but superseded by ADR-046 — see Status.)*

**(1) RLS anon-insert.** 5123's `responses_anon_insert` policy (`0001_assessment_responses.sql`) has `with check (source in ('readiness-assessment'))`. A survey response insert with `source = 'user-survey'` is therefore **rejected today** — the anon client cannot write it. The survey response is anonymous and PII-free (D3), so it belongs on the same anon-INSERT path as the assessment (ADR-032), but the allowlist must admit the new `source`.

**(2) Dashboard survey view (US-05).** The maintainer needs to read survey responses and the trial-requests. 5123's dashboard (`AdminDashboard.tsx` + `summarizeDashboard`) is **band-distribution-shaped**: it counts responses into the four scored bands and lists leads as `(email, score, band, date)`. The survey has **no bands and no scores**, so reusing `summarizeDashboard` verbatim would render an all-zero band distribution and blank score columns — a wrong shape, not just empty.

## Decision

**(1) Minimal additive migration `0003_user_survey_source.sql`** that widens the allowlist:

```sql
drop policy if exists responses_anon_insert on public.responses;
create policy responses_anon_insert
  on public.responses
  for insert
  to anon
  with check (source in ('readiness-assessment', 'user-survey'));
```

- It adds `'user-survey'` to the existing `WITH CHECK` allowlist and nothing else. `responses_authenticated_select`, the `leads` policies, and the schema are unchanged (ADR-034's "5124 adds only nullable columns / new source values — never restructure" rule). `'user-survey-trial'` is NOT added to any anon policy — the trial lead is `service_role`-only (ADR-041), so the anon allowlist deliberately excludes it.
- The policy is **fail-closed by construction**: anything outside the two allowed sources is rejected; RLS stays enabled.

**(2) A dedicated survey summarizer + a survey tab on the existing dashboard.**

- `DashboardRepository.load(source)` is parameterized by `source` already (ADR-034) — it loads survey rows with one extra call keyed by `'user-survey'` (responses) and `'user-survey-trial'` (leads). The repository widening is the union from ADR-040.
- A **new pure `summarizeSurvey(data)` core fn** (sibling of `summarizeDashboard`) produces a survey-shaped summary: total responses, per-question answer-option counts (Q1-Q4 single-select tallies), trial-request count, and the trial-request list `(email, date)` — **no band distribution, no score column**. It never calls `bandOfScore`.
- The dashboard gains a **survey tab/section** reusing 5123's auth (ADR-033 — Supabase Auth, `authenticated` SELECT RLS, accounts `benjamin@`/`peter@`), layout primitives (the existing `Card`/`Table` shadcn components), and the `AdminDashboard` page shell. The assessment view is unchanged; the survey view is an additional render branch keyed by the active source/tab. **No platform redesign** (D6).
- The trial-requests list shows the volunteered email (the maintainer actions it by hand — D4). Because `leads` SELECT is already `authenticated`-gated (ADR-033), no new RLS is needed for reading survey leads.

## Alternatives Considered

- **(1) A second anon policy for `user-survey`** (rather than editing the allowlist): rejected — two policies on the same table for the same operation is harder to reason about than one allowlist; the `WITH CHECK` `in (...)` set is the idiomatic extension point ADR-034 anticipated.
- **(1) Route survey responses through an Edge Function too**: rejected — same rationale as ADR-032's "responses stay anon-INSERT": the survey response carries no PII and no anti-forgery need (it is unscored), so a function adds a cold-start hop for zero security gain.
- **(2) Reuse `summarizeDashboard` with bands forced to zero**: rejected — renders a misleading all-zero band grid and blank score cells; the survey's signal is per-question option tallies, a different shape. A dedicated summarizer is the honest render.
- **(2) A separate survey dashboard page/route**: rejected — duplicates auth + layout for no benefit; D6 says extend the one dashboard. A tab within it reuses everything.

## Consequences

- **Positive**: one additive migration, fail-closed RLS preserved; the assessment dashboard untouched; the survey view renders the right shape (per-question tallies + trial list) on the same auth/layout; the trial lead stays `service_role`-only.
- **Negative**: the dashboard component now branches on source (assessment vs survey render); bounded and tested per branch. The survey summarizer is new code, but it is small, pure, and unit-testable without a DOM (the 5123 functional-core idiom, ADR-035).
- **Security note**: the survey response anon-INSERT carries the SAME residual as ADR-034 (timestamp+content correlation between a `responses` row and a `leads` row is not defeated, only the FK join is). For the survey this is weaker than assessment because survey responses have no score/band to correlate on, so the only correlate is `created_at` proximity — accepted for v1, flagged identically to ADR-034's residual.

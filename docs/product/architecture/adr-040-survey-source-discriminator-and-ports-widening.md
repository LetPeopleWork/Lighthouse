# ADR-040: Survey `source` discriminator + ports widening (non-scored response kind)

> **Scope: WEBSITE repo (`/storage/repos/website`) + its Supabase project `tkkghzcpwefwrgacgvdv`, NOT the Lighthouse product.** Authored for ADO Epic #5124; extends the 5123 platform (ADR-032/034/035) without redesigning it (D6).

## Status

Accepted (DESIGN wave, 2026-05-31). **Partially revised by [ADR-046](adr-046-survey-submission-and-team-notification.md)**: the `source`/`kind` discriminator and the ports widening (unions + guarded non-scored `CapturedResponse` shape) described here STAND. What changes is the *write adapter* — survey responses are written via the `service_role` `submit-survey` Edge Function (one consolidated `SurveySubmission.submit` port), NOT the anon-INSERT `SupabaseResponseRepository`, because every submission must trigger a server-side team-notification email. The structural-test guard (survey responses never route through `bandOfScore`) is unchanged.

## Context

5124's survey responses must land in the SAME generalized `responses` table 5123 established (D6, ADR-034), tagged with a new `source` value. But the 5123 code is assessment-shaped at three points the survey violates:

1. `ResponseSource` is a **closed union** `"readiness-assessment"` (`ports/index.ts`).
2. `CapturedResponse` requires **non-null** `rawSum`/`score: number`/`band: BandName`; the survey is **not scored** (no rawSum/score/band — D3, journey).
3. `DashboardData.load(source: ResponseSource)` and `DashboardResponse`/`DashboardLead` carry a required `band: BandName`; the survey has no bands, and `bandOfScore`/`summarizeDashboard` (`core/scoring.ts`, `core/dashboardSummary.ts`) **throw** or miscount on a null band.

The survey answers (single-select Q1-Q4) reuse the existing `answers jsonb` column (ADR-034 explicitly reserved this) with `raw_sum/score/band` left null — the migration already permits nulls there.

## Decision

**Widen the existing shared port types to admit a non-scored response kind; do NOT fork a parallel survey port set.**

- `ResponseSource` becomes a union: `"readiness-assessment" | "user-survey"`. The trial-lead `source` value is `"user-survey-trial"` (ADR-041) — added to a `LeadSource` union so a lead is never mistaken for a response.
- Make the scored fields **optional on the survey kind** via a discriminated shape rather than blanket-nullable (which would weaken 5123's invariants). The survey-carrying `CapturedResponse` sets `rawSum/score/band` to `null`; a small type guard (`isScoredResponse`) gates any code path that reads them. The `SupabaseResponseRepository.toRow` already maps these straight through to the nullable columns — it needs only to accept `null`.
- The survey answers are a **typed, zod-validated shape** distinct from the 6-ordinal assessment `Answers` — they live in the survey content module (ADR-043), serialized into the same `answers jsonb` column. The discriminator (`source`) tells the dashboard which shape to expect.
- **Survey responses never touch `bandOfScore`/scoring.** Routing is by `source`: the survey insert path writes `raw_sum/score/band = null`; the dashboard survey view (ADR-042) reads the survey `source` with its own summarizer that never indexes a band distribution.

## Alternatives Considered

- **Blanket-nullable `score`/`band` on `CapturedResponse`**: rejected — it weakens 5123's invariant that an assessment response always carries a valid scored band, and pushes null-checks into the assessment code paths that legitimately never expect null. A discriminated/guarded shape keeps the assessment path total.
- **A parallel `survey` port set (`SurveyResponse`, `SurveyRepository`, …)**: rejected as the default — it duplicates the anon-INSERT adapter and the dashboard repository for what is genuinely the same storage concern (one table, one discriminator). The DISCUSS D6 mandate is EXTEND, not fork. (Note: per CLAUDE.md "don't abstract different business concepts", the *lead* path IS forked — see ADR-041 — because trial-capture is a genuinely different concept from results-gated lead-capture. The *response* path is the same concept with a new discriminator, so it is widened, not forked.)
- **A generic open-ended `Record<string, unknown>` answers type**: rejected — loses the trust-boundary validation the survey answers need (single-select option allowlist) and the typed dashboard render.

## Consequences

- **Positive**: one table, one anon-INSERT adapter, one discriminator serve both epics (D6 honored); the assessment path keeps its total non-null invariants; survey answers are zod-validated at the boundary.
- **Negative**: the shared `ports/index.ts` now carries two `source` unions and a guarded scored shape (mild added surface; bounded by the discriminator). Touching this shared contract requires re-running the 5123 assessment component tests to confirm no regression (grep-for-usages-first per CLAUDE.md).
- **Regression guard**: a test asserts an assessment response still round-trips with non-null score/band, and a survey response round-trips with null score/band, through the same repository.

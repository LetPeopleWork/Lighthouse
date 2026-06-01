# ADR-034: Two-table structural-anonymity schema with `source`/`kind` discriminator

> **Scope: WEBSITE repo (`/storage/repos/website`) Supabase project `tkkghzcpwefwrgacgvdv`, NOT the Lighthouse product.** Authored for ADO Epic #5123; reused by sibling Epic #5124.

## Status

Accepted (DESIGN wave, 2026-05-30) — confirming the DISCUSS D6 schema sketch with DESIGN-level RLS and naming detail.

## Context

DISCUSS D6 + the Shared-Platform section lock a generalized capture platform that sibling Epic #5124 (User Survey) reuses, storing **no PII beyond the opt-in email**. Two anonymity needs collide with one lead-gen need:

- 5124 promises *anonymous* surveys — answers must not be joinable to a person.
- 5123 still needs its lead — the (email, band) pair to follow up.

A single table with a nullable email cannot offer *structural* anonymity (a row would couple answers to email whenever email is present). The DISCUSS sketch resolves this with **two tables and no foreign key between them**.

## Decision

Two tables on the existing Supabase project, generalized by a `source`/`kind` discriminator:

```
table responses        -- anonymous; NEVER holds PII
  id          uuid pk default gen_random_uuid()
  source      text not null      -- "readiness-assessment" | (5124) "user-survey"
  kind        text               -- optional sub-type within a source
  answers     jsonb not null     -- six 0-3 ordinals (assessment); survey reuses the shape
  raw_sum     int                -- assessment 0-18; null for non-scored sources
  score       int                -- assessment 0-100; null for non-scored sources
  band        text               -- assessment band name; null for non-scored sources
  created_at  timestamptz not null default now()

table leads            -- the ONLY table with PII (email)
  id          uuid pk default gen_random_uuid()
  source      text not null      -- "readiness-assessment" | (5124) "user-survey-trial"
  email       text not null      -- volunteered; validated server-side in the Edge Function
  score       int                -- lead carries its OWN copy of score/band (NOT an FK)
  band        text
  wants_trial bool not null default false   -- 5124 trial opt-in signal, default false for 5123
  created_at  timestamptz not null default now()
```

- **No foreign key, no join key** between `responses` and `leads`. The lead row carries its own `score`/`band` copy, so 5123 gets a usable lead without the lead being joinable back to a specific anonymous answer row. This makes 5124's anonymity *structural*, not a promise.
- **RLS** (see ADR-032/033): `responses` — `anon` INSERT-only (`WITH CHECK` bounds `source`/`kind`), no anon SELECT; `authenticated` SELECT for the dashboard. `leads` — **no `anon` policy at all**; written only by the `capture-lead` Edge Function via `service_role`; `authenticated` SELECT for the dashboard. RLS enabled on both (fail-closed).
- **Discriminator drives the dashboard**: every dashboard query is parameterized by `source` (and optionally `kind`), so 5124 rows slot into the same view via a filter/tab with no schema change.
- **Typed in the client**: regenerate `src/integrations/supabase/types.ts` (currently empty `Tables: never`) from the new schema, or hand-author the two `Row`/`Insert` types, so the client gets `Database` typing for the inserts/selects. A zod schema mirrors the insert shape at the trust boundary.

## Alternatives Considered

- **Single `submissions` table with nullable `email`**: rejected — cannot offer structural anonymity (answers and email live on the same row whenever email is present), breaking 5124's core promise. Also forces RLS to protect a PII column inside an otherwise-anon table, which RLS column-masking does poorly.
- **`leads` with an FK to `responses`**: rejected — the FK is exactly the join that de-anonymises the answers; carrying a duplicated score/band copy on `leads` is the deliberate trade that preserves anonymity at the cost of a few bytes.
- **Separate tables per epic (`assessment_responses`, `survey_responses`)**: rejected — defeats the platform-reuse goal (D6); would force 5124 to build a parallel schema + dashboard. The `source`/`kind` discriminator generalizes one schema instead.

## Consequences

- **Positive**: structural anonymity for 5124; a usable lead for 5123; one schema + one dashboard serve both epics; fail-closed RLS by default.
- **Negative**: `score`/`band` are duplicated across the two tables for assessment leads (negligible cost; the intended trade). De-duplication of repeat email submissions is a dashboard concern, not a constraint (each submission is a distinct row — per journey error-paths).
- **Migration discipline**: schema changes go through Supabase migrations under `supabase/migrations/`; regenerate `types.ts` after each. 5124 must add only nullable columns / new `source` values — never restructure 5123's rows (the discriminator's whole purpose).

## Residual risks

The anonymity guarantee delivered here is **structural ("no FK / no join key back from a `leads` row to its `responses` row"), NOT cryptographic anonymity.** A `responses` row and a `leads` row created within the same session seconds apart, carrying the same `score`/`band`, could in principle be **timestamp + score correlated** by someone with direct table access — re-associating an answer set with the volunteered email without any FK. The two-table no-FK design defeats a *join*, not a *statistical correlation*. This residual is **accepted for v1** in exchange for schema simplicity (no per-row salting, no decoupled write timing, no `created_at` coarsening); it does **not** weaken the no-FK structural guarantee the design makes. Flag it for monitoring if privacy becomes a harder requirement — and, importantly, **sibling Epic 5124's DESIGN (which inherits this exact platform) should weigh this as an explicit constraint**, since "anonymous survey" may demand a stronger bar (e.g. coarsened/withheld `created_at` on `responses`, or decoupled write timing) than v1 provides.

# Slice 02 — Persist every completed assessment to Supabase (platform-establishing)

**Goal**: Every completed assessment (the six answers, rawSum, score, band) is written to a generalized Supabase responses table on the existing website Supabase project — establishing the shared capture platform with a `source`/`kind` discriminator so epic 5124 can reuse it.

## IN scope

- Supabase table (e.g. `assessment_responses`) with a `source`/`kind` discriminator column, `answers` (the six ordinals), `raw_sum`, `score`, `band`, `created_at`. NO PII in this slice (email arrives in Slice 03).
- RLS policy allowing anonymous INSERT of a response row from the website anon client (read locked down).
- Client write on results computation: on reaching the results page, insert the response row.
- Degrade-open behavior: the results still render if the write fails; retry once; surface a non-blocking notice.

## OUT scope

- Email capture column/flow (Slice 03 extends the row / adds a leads relationship).
- Admin dashboard (Slice 04).
- Any 5124 survey fields (only the discriminator so we do not paint into a corner).

## Learning hypothesis

"A generalized response schema with a source/kind discriminator can capture readiness responses now and survey responses (5124) later without a migration that breaks v1."
Disproved if: the discriminator cannot accommodate a differently-shaped survey response without restructuring the readiness rows.

## Acceptance criteria

- [ ] Completing the assessment writes exactly one row carrying answers, raw_sum, score, band, source/kind = readiness-assessment, created_at.
- [ ] A failed/blocked write does NOT prevent the results page from rendering; one retry is attempted; a non-blocking notice appears.
- [ ] The anon client can INSERT but cannot SELECT other visitors' rows (RLS verified).
- [ ] No personal data is stored in this slice (email column is absent or null).

## Dependencies

- Slice 01 (needs the computed answers/rawSum/score/band to persist).

## Effort estimate

~5h. **Reference class**: a single-table insert with RLS on an already-wired Supabase project (client.ts + config.toml exist; create-payment/stripe-webhook functions show the established pattern).

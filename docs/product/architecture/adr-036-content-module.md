# ADR-036: Single typed content module for questions / ladders / band copy / CTAs / anchor

> **Scope: WEBSITE repo (`/storage/repos/website`), NOT the Lighthouse product.** Authored for ADO Epic #5123.

## Status

Accepted (DESIGN wave, 2026-05-30)

## Context

Question text, the six 0-3 answer ladders, band names/ranges, both-pillars breakdown copy, the band-keyed CTA sets, and the credibility anchor are consumed by multiple surfaces: intro, quiz, teaser, breakdown, and (band/score) the dashboard. The journey's shared-artifacts registry flags `${credibilityAnchor}`, `${band}`, and `${breakdownContent}` as multi-consumer, single-source values; a content gap for any band must "never ship" (journey failure mode). Content is load-bearing for credibility (a DISCUSS open risk: exact wording confirmed before DELIVER).

## Decision

A single **`assessmentContent` module** (`src/features/assessment/content/`) as the one source of truth, **typed and zod-validated at module load**:

- Shape (informal): `questions: Question[6]` (each `{ id, prompt, pillar: "metrics"|"forecasting", ladder: [string, string, string, string] }`), `bands: Band[4]` (each `{ name, range: [min,max], pillarRead, breakdown, primaryCta, secondaryCta|null }`), `credibilityAnchor: string`, `communityCta: Cta` (the invariant free CTA present in every band).
- A **zod schema** validates the literal content object at module load (dev/test) — asserting exactly 6 questions each with a 4-rung ladder, exactly 4 bands with contiguous non-overlapping ranges covering 0-100, every band has non-empty breakdown copy, and every band's CTA set includes the Community CTA. This turns the "every band has content" and "Community in every band" ACs into a load-time invariant, not a runtime hope, and gives the scoring module's band table a single definition to align with.
- Types derived via `z.infer` so consumers get typed access; the band-mapping table in the `scoring` module (ADR-035) references the *same* `bands` ranges (one definition of the thresholds).
- **The `bands` ranges are also the server-side anti-forgery source of truth.** Because scoring is client-evaluable, the `capture-lead` Edge Function (ADR-032) must re-derive the expected band from the submitted `score` and reject any forged `band`/out-of-range `score` before writing PII. To keep ONE definition of the thresholds across the browser core AND the Deno Edge Function, the four `(name, [min,max])` ranges are extracted into a small **Deno-importable shared module under `supabase/`** (e.g. `supabase/_shared/bands.ts`) that both `assessmentContent` and the Edge Function import — or, if the bundler cannot reach across into `supabase/`, mirrored here with a **CI/migration parity assertion** that fails the build if the two range tables diverge. The Edge Function never hand-rolls a second copy of the band thresholds.
- Pure data + the validator only — no React, no I/O. Lives inside the hexagon-adjacent content layer, importable by both core (band lookup) and shell (rendering).

## Alternatives Considered

- **Inline copy in each component (JSX literals)**: rejected — duplicates band/anchor strings across intro/teaser/breakdown, invites drift, and makes the "every band has content / Community everywhere" invariants untestable in one place.
- **Content in the Supabase DB (CMS-style)**: rejected for v1 — adds a fetch, a loading state, and a server round-trip to *static* copy that changes rarely and is version-controlled with the code; contradicts the client-evaluable, no-round-trip constraint. Revisit only if non-engineers need to edit copy without a deploy.
- **Plain `const` object with no validation**: rejected — loses the load-time guarantee that the 4 bands are contiguous/exhaustive and that every band carries the Community CTA; those are exactly the invariants the ACs assert, so encoding them in a zod schema is cheap insurance.

## Consequences

- **Positive**: one place to confirm the load-bearing copy before DELIVER; band ranges defined once and shared with scoring; band-coverage and CTA invariants enforced at load + by a test; no fetch for static content.
- **Negative**: copy edits require a deploy (acceptable — content is version-controlled and changes rarely). The zod-at-load validation runs in the bundle; keep it lightweight (it validates a small literal).
- **Open**: the exact ladder/band wording is a DISCUSS open risk — DELIVER must confirm final copy against the epic before shipping (flagged for the user).

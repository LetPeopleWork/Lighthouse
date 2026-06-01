# ADR-032: Capture transport split — anon-INSERT for `responses`, Edge Function for `leads`

> **Scope: WEBSITE repo (`/storage/repos/website`) + its Supabase project `tkkghzcpwefwrgacgvdv`, NOT the Lighthouse product.** Authored for ADO Epic #5123.

## Status

Accepted (DESIGN wave, 2026-05-30)

## Context

The website is a **public client-side SPA**: the Supabase `anon` key ships in the browser bundle and is **not a secret** (confirmed in `src/integrations/supabase/client.ts`). All write security therefore rests on Row Level Security (RLS), never on key secrecy.

Two distinct things get captured (DISCUSS D6, two-table structural-anonymity model — see ADR-034):

1. **`responses`** — anonymous answers/score/band. NEVER PII. High volume (every completion).
2. **`leads`** — the volunteered email (the ONLY PII), plus a copy of score/band. Lower volume (only email-gate submitters).

DISCUSS recommended: anonymous `responses` via direct anon-INSERT (matches the already-wired `client.ts`), and `leads` via a Supabase Edge Function using `service_role`. The open DESIGN question: should `responses` *also* go through an Edge Function for validation/rate-limiting symmetry, or stay direct anon-INSERT?

The existing `supabase/functions/create-payment/index.ts` is the proven Edge-Function pattern in this repo (Deno `serve`, CORS headers, `Deno.env.get`, zod-style validation, invoked from the client via `supabase.functions.invoke("create-payment", { body })` — see `Lighthouse.tsx:489`).

## Decision

**Asymmetric transport, by data sensitivity:**

- **`responses` → direct anon-INSERT** guarded by an RLS policy that grants `INSERT` (with a `WITH CHECK` constraining `source`/`kind` to known values and bounding the JSON shape) and grants **no `SELECT`/`UPDATE`/`DELETE`** to `anon`. The absence of a SELECT policy is the load-bearing protection: a visitor can write one row but can never read back others. This matches the existing `client.ts` wiring (zero new infrastructure) and keeps the walking-skeleton-plus-persistence slice (02) thin.
- **`leads` → Supabase Edge Function (`capture-lead`) using `service_role`**, modelled on `create-payment`. The `leads` table has **no anon policy at all** (fully sealed). The browser POSTs `{ email, score, band, source, kind }`; the function validates with **zod** (email format, score/band ranges, allowed source/kind), then inserts with the privileged `service_role` key. This centralises PII validation, keeps the email table unreachable from the public key, and gives a single chokepoint where a Turnstile/hCaptcha token check or per-IP rate limit slots in later if abuse appears.

  **Anti-forgery re-validation (security-boundary property, not just format validation).** Because scoring is *client-evaluable* (ADR-035/036), the `{ score, band }` in the POST body is attacker-controllable: a tampering client can submit a forged `band` (e.g. claim "Probabilistic" with a `score` of 4) or an out-of-range `score`. Format-only zod validation would accept it. The `capture-lead` function MUST therefore **re-derive the expected band from the submitted `score` against the SAME band-range table that scoring uses (the `assessmentContent.bands` ranges from ADR-036) and reject the request when `score` is outside 0-100, OR `band` is not the band whose range contains `score`** — rejecting with a 4xx and **writing no `leads` row**. This makes the band a server-checked invariant, not a client claim, at the one trust boundary that touches PII.

  **Parity without duplicating the source of truth.** The band-range table is defined once and consumed by both the browser scoring core and the Deno Edge Function. Concretely: the four `(name, [min,max])` band ranges live in a small **Deno-importable shared module under `supabase/`** (e.g. `supabase/_shared/bands.ts`) that the Edge Function imports directly, and that the website's `assessmentContent`/`scoring` module also imports (or, if the website bundler cannot reach across into `supabase/`, the ranges are mirrored in `assessmentContent` with a **CI/migration parity assertion** failing the build if the two range tables diverge). Either way there is ONE authoritative band table; the Edge Function never hand-rolls a second copy of the thresholds. DISTILL can write an acceptance test against this: *"a POST with a forged `band` (band name not matching the submitted `score`'s range) is rejected with no `leads` row written."*

`responses` is NOT promoted to an Edge Function in v1. Rationale below.

Both paths **degrade open** (DISCUSS constraint): a write/function failure never blocks the visitor's result or the breakdown; retry once; surface a non-blocking notice via the existing `use-toast` / sonner.

## Alternatives Considered

- **Both tables via Edge Functions (symmetry)**: rejected for v1. The anonymous `responses` row carries no PII and no trust-boundary validation need that RLS `WITH CHECK` cannot express; routing it through a function adds a cold-start latency hop and an operational surface for zero security gain. Spam mitigation, the one real argument, is deferrable (community volume) and, if needed, the *email* path (the valuable target) is already a function. Revisit only if bot-writes to `responses` become a measured problem — at which point promote `responses` to a function or add a Turnstile gate, a contained change behind the same driven-port interface (ADR-035).
- **Both tables via direct anon-INSERT** (incl. `leads`): rejected outright. Granting `anon` INSERT on the PII table — even INSERT-only — widens the email table's exposure to the public key and forecloses server-side validation/rate-limiting. The structural-anonymity guarantee (ADR-034) is materially stronger when the PII table has *no* anon policy whatsoever.
- **A backend service / Lighthouse API**: rejected — this is a website-only feature; the Lighthouse C# backend is a different product and codebase (confirmed cross-cutting checklist: Clients/RBAC N/A). No server exists to add this to except Supabase Edge Functions.

## Consequences

- **Positive**: PII fully sealed behind `service_role`; anonymous high-volume path stays zero-infra and fast; one validation/rate-limit chokepoint for the valuable data; **the `score`/`band` are server-re-validated against the shared band table so a forged submission cannot write a lead with a fabricated band** (anti-tampering at the PII trust boundary); both paths swappable behind driven ports (ADR-035) so a future "promote responses to a function" is contained.
- **Negative**: two capture code paths to reason about (mitigated — each is behind its own driven-port adapter). `responses` has no server-side spam gate in v1 (accepted risk at community volume; mitigation path documented).
- **External integration note**: `capture-lead` is a new Edge Function consuming the Supabase `service_role` + Postgres. It is first-party (same Supabase project), so consumer-driven contract testing is *not* required, but its request/response shape is a contract between the browser adapter and the function — cover it with a zod schema shared in intent (the function validates; the client pre-validates) and a component test asserting degrade-open on a non-200.

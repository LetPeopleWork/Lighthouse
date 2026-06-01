# ADR-033: Internal dashboard auth — Supabase Auth (email/password) + `authenticated` SELECT RLS

> **Scope: WEBSITE repo (`/storage/repos/website`) + its Supabase project, NOT the Lighthouse product.** Authored for ADO Epic #5123, Slice 04.

## Status

Accepted (DESIGN wave, 2026-05-30)

## Context

US-06 / Slice 04 require a protected internal dashboard for the LetPeopleWork team showing totals, email-capture count, band distribution, and the lead table (email, score, band, created_at), filtered by `source`/`kind`. Constraints (DISCUSS security model rule 5): the dashboard must use a **privileged read path, never `anon` SELECT**, and the route must be inaccessible to unauthenticated visitors (US-06 AC + Slice 04 AC).

The Supabase client is already configured for auth (`persistSession: true`, `autoRefreshToken: true`, `storage: localStorage` in `client.ts`) — so the Supabase Auth session machinery is wired and unused today. The team is small (LetPeopleWork internal; a handful of people).

Open DESIGN question: Supabase Auth (admin logs in, `authenticated` SELECT RLS gates the rows) **vs** an Edge Function + `service_role` behind a shared secret.

## Decision

**Supabase Auth with email/password sign-in, plus a `SELECT` RLS policy on `responses` and `leads` granting `authenticated` users read access.**

- A `/admin/assessment` route (registered above the catch-all in `App.tsx`) renders a sign-in form when there is no Supabase session and the dashboard when there is. Gating derives from `supabase.auth.getSession()` / `onAuthStateChange` via a small `useAdminSession` hook — the route never renders lead data without an active `authenticated` session.
- RLS: add `SELECT … USING (auth.role() = 'authenticated')` (or `TO authenticated`) policies on both tables. `anon` still has no SELECT (ADR-032 unchanged); only signed-in team members read.
- Accounts are provisioned manually in the Supabase dashboard (Auth → Users) for the LetPeopleWork team. **v1 account holders (confirmed by the product owner 2026-05-30): `benjamin@letpeople.work` and `peter@letpeople.work`.** No public sign-up; no password-reset self-service needed at this scale (reset via the Supabase console).
- The dashboard reads through the **same anon client object** but now carries the user's JWT after sign-in, so RLS evaluates `authenticated`. No `service_role` ever reaches the browser.

## Alternatives Considered

- **Edge Function + `service_role` behind a shared secret**: rejected for v1. It re-implements authentication (secret distribution, rotation, comparison) that Supabase Auth already provides correctly; a shared secret in a browser app is itself a non-secret (same problem as the anon key), so it would need its own login UI anyway — i.e. all the work of Supabase Auth with weaker semantics (no per-user identity, no session expiry, no revocation). Supabase Auth gives real per-user accounts, session expiry, and one-click revocation for free, and the client is already configured for it.
- **Reuse the website's existing auth**: rejected — there is none; the app is anonymous-public today. Supabase Auth is the lowest-friction real auth available.
- **IP allowlist / basic-auth at the hosting layer**: rejected — brittle (team works from varying networks), no per-user identity for audit, and couples access control to deploy config rather than the app.

## Consequences

- **Positive**: real per-user auth with expiry/revocation at zero new infra; `service_role` never in the browser; RLS does the gating uniformly (anon=INSERT-only, authenticated=SELECT); the same model extends to epic 5124's dashboard rows for free.
- **Negative**: team accounts are provisioned manually (acceptable at this scale; documented as an operational step). The client-side route guard is a UX affordance, not the security boundary — **RLS is the real boundary** (an unauthenticated direct query returns zero rows regardless of routing). This must be asserted by test (the Slice 04 AC "not reachable without authentication" is satisfied at the data layer, not just the route).
- **Confirmed (2026-05-30)**: the two accounts to provision are `benjamin@letpeople.work` and `peter@letpeople.work`. No longer an open question.

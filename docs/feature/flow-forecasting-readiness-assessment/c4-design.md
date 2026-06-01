# C4 Design — Flow & Forecasting Readiness Assessment (ADO #5123)

> **Scope: the WEBSITE app (`/storage/repos/website`)**, not the Lighthouse product. The "system" is the LetPeopleWork website; external systems are the website's Supabase project (Postgres + Auth + Edge Functions) and any website analytics. These docs live in the Lighthouse repo by team convention; the implementation lands in the website repo.

## Level 1 — System Context

```mermaid
C4Context
  title System Context — Forecasting Readiness Assessment (website)

  Person(prospect, "Forecasting Prospect", "Delivery lead / EM / flow coach evaluating maturity; not yet a Lighthouse user")
  Person(team, "LetPeopleWork Team", "Internal; reviews and acts on captured leads")

  System(website, "LetPeopleWork Website", "React 19 + Vite SPA; hosts the /assessment flow and the internal dashboard")

  System_Ext(supabase, "Supabase Project", "Postgres + RLS, Auth, Edge Functions (Deno)")
  System_Ext(analytics, "Website Analytics", "Funnel events sink (vendor TBD at DELIVER; Supabase rows are the durable fallback)")
  System_Ext(community, "Lighthouse Community / Consulting / Paid", "External CTA destinations, UTM-tagged for manual attribution")

  Rel(prospect, website, "Takes the assessment, sees teaser, trades email, clicks a CTA on")
  Rel(team, website, "Signs in to review leads on")
  Rel(website, supabase, "Inserts anonymous responses (anon RLS); captures leads (Edge Function); reads dashboard rows (authenticated)")
  Rel(website, analytics, "Emits funnel events to")
  Rel(prospect, community, "Is routed to (UTM-tagged) by")
  Rel(website, community, "Links the band-specific CTA to")
```

## Level 2 — Container

```mermaid
C4Container
  title Container Diagram — Assessment feature within the website

  Person(prospect, "Forecasting Prospect")
  Person(team, "LetPeopleWork Team")

  Container_Boundary(site, "LetPeopleWork Website (React 19 + Vite SPA)") {
    Container(assessmentRoute, "Assessment Flow", "React route /assessment", "intro to Q1..Q6 to teaser to gate to breakdown; hosts the quiz machine")
    Container(nav, "Navigation", "React component", "Adds the Forecasting Readiness entry")
    Container(core, "Scoring + Quiz Core", "Pure TS (no I/O)", "score(answers); quizMachine reducer; content module (zod-validated)")
    Container(adapters, "Driven Adapters", "TS", "Supabase responses-INSERT; capture-lead Edge invoke; sessionStorage; analytics sink")
    Container(dashboard, "Admin Dashboard", "React route /admin/assessment", "Supabase-auth-gated totals / band distribution / lead table")
  }

  ContainerDb(responses, "responses table", "Supabase Postgres + RLS", "Anonymous; anon INSERT-only, authenticated SELECT")
  ContainerDb(leads, "leads table", "Supabase Postgres + RLS", "PII (email); no anon policy; service_role write only")
  Container(edgeFn, "capture-lead", "Supabase Edge Function (Deno)", "zod-validates then inserts the lead via service_role")
  Container(auth, "Supabase Auth", "Supabase", "Email/password sessions for the team")
  System_Ext(analytics, "Website Analytics")

  Rel(prospect, nav, "Finds the assessment via")
  Rel(nav, assessmentRoute, "Routes to")
  Rel(prospect, assessmentRoute, "Answers questions, submits email, clicks CTA in")
  Rel(assessmentRoute, core, "Computes score/band and drives transitions via")
  Rel(assessmentRoute, adapters, "Persists and tracks through")
  Rel(adapters, responses, "Inserts a completion row into (anon RLS, degrade-open)")
  Rel(adapters, edgeFn, "POSTs the email/lead to")
  Rel(edgeFn, leads, "Inserts the lead into (service_role)")
  Rel(adapters, analytics, "Emits funnel events to")
  Rel(team, dashboard, "Signs in to and reviews")
  Rel(dashboard, auth, "Authenticates via")
  Rel(dashboard, responses, "Reads counts / band distribution from (authenticated SELECT)")
  Rel(dashboard, leads, "Reads the lead table from (authenticated SELECT)")
```

## Level 3 — Component

Not produced. The feature's internal decomposition (quiz machine, scoring fn, content module, four driven adapters, the page surfaces) is fully captured by the component-decomposition and ports tables in the DESIGN sections of `feature-delta.md`; no subsystem here has the 5+-component internal complexity that warrants an L3 diagram (per the architecture-patterns C4 guidance, L3 is for complex subsystems only).

## Quality attribute scenarios (ISO 25010 highlights)

- **Reliability / fault tolerance (degrade-open)**: a Supabase `responses` insert or `capture-lead` failure never blocks the result/breakdown; the adapter retries once and surfaces a non-blocking notice. Asserted by component tests with a rejecting fake adapter.
- **Security / confidentiality**: PII (email) sealed in `leads` with no anon policy; written only via `service_role` in the Edge Function; dashboard read gated by `authenticated` RLS, not by routing alone. The anon key being public is by design — RLS is the boundary.
- **Functional correctness**: deterministic, client-evaluable scoring; band boundaries exhaustive/non-overlapping over 0-100; exhaustive boundary unit tests (0/25/26/50/51/75/76/100, all-0, all-3).
- **Usability**: one question at a time, "N of 6" progress, back-nav preserves answers, resume-on-refresh, usable at 375px.
- **Maintainability / testability**: pure core + four driven ports; effects substitutable by fakes; one content module with load-time invariants.
- **Privacy (structural anonymity)**: no FK between `responses` and `leads`; survey answers (5124) never joinable to an email.

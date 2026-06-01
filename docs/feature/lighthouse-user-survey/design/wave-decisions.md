# DESIGN Wave Decisions — lighthouse-user-survey (ADO Epic #5124)

- **Wave**: DESIGN (application / component scope)
- **Interaction mode**: PROPOSE (non-interactive; recommended option committed per seam, alternatives in ADRs)
- **Architect**: Morgan (solution-architect)
- **Date**: 2026-05-31
- **Paradigm**: locked — Lighthouse repo = OOP ports-and-adapters (CLAUDE.md); website repo = functional-core/imperative-shell hexagonal idiom continued from 5123 (ADR-035). Not re-decided.
- **Default style**: modular monolith + ports-and-adapters (both repos already follow it). No new style introduced; this is a brownfield EXTEND of the 5123 platform (D6).

## Multi-architect context

This feature's `## Application Architecture` delta is appended to `docs/product/architecture/brief.md`. Prior architects' sections (work-tracking-oauth, filter-forecast, time-in-state, aging-pace, state-time-cumulative, target-architecture-4618, forecast-confidence-cap, forecast-minimum-data-guard) are untouched. The 5123 sibling established ADR-031..037 and the website platform this feature extends.

## Outcome Collision Check

`nwave-ai outcomes check-delta` — **SKIPPED, not applicable**: the nWave outcome-registry CLI is not present in this workspace. The KPIs (feature-delta KPIs 1-6) were checked manually against the 5123 KPI set; the survey North Star (response count) and 5123's North Star (teaser→email capture) are distinct funnels sharing only the dashboard surface — no collision. Q4 deliberately doubles as a 5123 demand signal (reinforcing, not colliding).

## Per-seam decisions

| Seam | Decision | Status | ADR |
|---|---|---|---|
| 1. ~~RLS anon-insert widening~~ | **SUPERSEDED by seam 8 / ADR-046**: survey writes via `service_role`, so no `'user-survey'` anon allowlist and no migration `0003`; policy stays `'readiness-assessment'`-only. | Superseded | ADR-042→046 |
| 2. ~~Trial-lead capture path~~ | **SUPERSEDED by seam 8 / ADR-046**: folded into `submit-survey` (ADR-041 reasoning — don't bend `capture-lead`'s invariant, never anon-INSERT PII — retained). | Superseded | ADR-041→046 |
| 3. Ports widening | Widen `ResponseSource` union + add a guarded non-scored `CapturedResponse` shape; add `SurveySubmission.submit` port. Do NOT fork the response port. Assessment invariants preserved + regression test. | Committed | ADR-040, 046 |
| 4. Dashboard survey view | Reuse 5123 Supabase Auth + layout; add a survey tab + a NEW pure `summarizeSurvey` (per-question option tallies + trial list, NO band distribution). | Committed | ADR-042 |
| 5. In-app nudge eligibility | **FE-derived** (Option a): FE computes from `canUsePremiumFeatures` + server `installTimestamp` + `lastShownAt`; premium-first/fail-closed; UTC-stable; NO new feature endpoint ⇒ CLI/MCP N/A. | **CONFIRMED (user, 2026-05-31)** | ADR-044 |
| (5 support) Per-instance settings | Extend the existing AppSettings mechanism with two keys; first-run write-once `installTimestamp`; non-admin read surface (existing controller is `[RbacGuard]`-admin); EF migration via `CreateMigration` for Sqlite+Postgres; startup probe (Earned Trust). | Committed | ADR-045 |
| 8. Consolidated `submit-survey` + team email | One `service_role` Edge Fn: response insert + optional trial lead + per-submission team notification email to `survey.answer@letpeople.work` (degrade-open). New `_shared/surveyNotificationEmail.ts`; reuse `_shared/mailgun.ts`. | Committed (user requirement 2026-05-31) | ADR-046 |

## Quality attributes (ISO 25010, prioritized)

- **Security / confidentiality (CRITICAL)** — PII discipline: email only on opt-in, service_role-only, separate table, no FK (structural anonymity). Premium exclusion fails closed.
- **Reliability / fault tolerance** — degrade-open on website writes (no false thank-you); fail-closed on nudge eligibility uncertainty; Earned-Trust startup probe on the settings store.
- **Maintainability / modifiability** — questions are config (US-03 capability KPI); route stable; ports widened not forked where the concept is shared.
- **Functional suitability** — 7 user stories traced to components (table below).
- **Usability** — calm dismissible nudge (Lighthouse design system); forgiving survey validation.

## Requirements → component trace

| Story | Component(s) | Repo | ADR |
|---|---|---|---|
| US-01 stable `/survey` page | `/survey` route + survey content module | website | 043 |
| US-02 anonymous submit | `EdgeFunctionSurveySubmission` + `submit-survey` (service_role) | website | 040, 046 |
| US-03 editable questions / stable link | zod survey content module; route constant | website | 043 |
| US-04 trial opt-in | `submit-survey` (trial-lead branch) into `leads` | website | 046 |
| US-05 dashboard survey view | survey tab + `summarizeSurvey`; reuse 5123 auth | website | 042, 033 |
| US-06 nudge eligibility (premium/age) | FE eligibility fn + `installTimestamp` setting + startup probe | Lighthouse | 044, 045 |
| US-07 cadence / dismissal | nudge FE component + `lastShownAt` setting | Lighthouse | 044, 045 |
| US-08 per-submission team email | `submit-survey` + `surveyNotificationEmail` + `_shared/mailgun.ts` → `survey.answer@letpeople.work` | website | 046 |

## C4 diagrams

System Context (L1) + Container (L2) produced in `feature-delta.md` (DESIGN section) and inline below. A Component (L3) diagram is produced for the in-app nudge subsystem (the one place with non-trivial internal collaboration: eligibility fn + premium hook + settings adapter + probe).

## External integrations (DEVOPS handoff annotation)

- **Supabase** (Postgres + Auth + Edge Functions) and **Mailgun** are first-party / same-project (5123-established). `submit-survey` DOES call Mailgun (the per-submission team notification, ADR-046) via the shared `_shared/mailgun.ts` transport — degrade-open, so a Mailgun outage never blocks a submission. No consumer-driven contract test required (first-party); cover the adapter↔function shape with a shared zod intent + a degrade component test, and a `@real-io` describe.skip live-POST contract like 5123's. Operational pre-req: `survey.answer@letpeople.work` must be a deliverable inbox (USER MANUAL STEP, alongside the existing Supabase Mailgun secrets).
- **Optional analytics sink** (KPI 3/5 nudge events) inherits ADR-037's swappable-sink port; fire-and-forget, degrade-silent, no contract test.
- **Telemetry caveat** (MEMORY `project_self_hosted_telemetry_gap`): self-hosted Lighthouse instances do not phone home, so nudge KPIs 3/5 are field-measurable only on opt-in/dogfood instances → the premium guardrail (KPI 5) is ALSO enforced by a deterministic test.

## Open questions (carried to DISTILL / DEVOPS)

1. **Seam 5** — **RESOLVED 2026-05-31: FE-derived, user-confirmed.** No new endpoint; CLI/MCP N/A.
2. **D7 nav/footer entry DEFERRED** — `/survey` ships HIDDEN (no nav, no sitemap, no robots Disallow) for silent testing (mirrors #5132). The nav/footer entry + new-visual-language adoption is deferred to a post-silent-window slice — recorded as not-done-now, not dropped.
3. **Q4 response scale** — interest scale vs yes/no, confirmed at DELIVER (DISCUSS open item); the content module's zod schema accommodates either.
4. **ADR-034 anonymity residual inherited** — timestamp+content correlation between a `responses` and a `leads` row is not defeated (only the FK join is). For the survey the correlate is weaker (no score/band), only `created_at` proximity; accepted v1, flagged for monitoring if "anonymous survey" demands a harder bar (coarsened `created_at` / decoupled write timing).

## Self-review (nw-sa-critique-dimensions)

- **Bias** — no resume-driven complexity: every seam EXTENDS existing platform; the one FORK (ADR-041) is justified by a genuinely different business concept, not novelty. No new infra, no microservice, no new style.
- **ADR quality** — each ADR carries context, ≥2 alternatives with rejection rationale, consequences, scope header (WEBSITE vs LIGHTHOUSE).
- **Completeness** — security (PII, premium-fail-closed), reliability (degrade-open/fail-closed/probe), maintainability addressed; performance is non-critical (community volume, anon-INSERT no cold start).
- **Feasibility** — both teams already own these stacks; no new capability needed; testable via the existing test stacks (Vitest/RTL website, NUnit+EF-InMemory Lighthouse).
- **Priority (Q1-Q4)** — largest risk is the 5123 sequencing dependency (flagged) + premium-bother guardrail (test-enforced); simpler alternatives documented per ADR; constraints not inverted (the design is minimal-extend, not a >50% rebuild for a small feature); KPI-5 guardrail is test-justified, not telemetry-dependent.

# ADR-043: Stable hidden `/survey` route + zod-validated survey content module

> **Scope: WEBSITE repo (`/storage/repos/website`), NOT the Lighthouse product.** Authored for ADO Epic #5124; mirrors 5123's content-module + functional-core idiom (ADR-035/036).

## Status

Accepted (DESIGN wave, 2026-05-31)

## Context

US-01/US-03: a standalone, shareable `/survey` page at a **stable** public route whose URL never changes when the questions change (D1 — questions are data/config). US-02: answers submit anonymously into the shared platform. The page is reached by direct URL, a shared link, or the in-app nudge — all resolving identically.

**NEW USER CONSTRAINT (2026-05-31): `/survey` ships HIDDEN** — no nav link, not in the sitemap, reachable only by direct URL for a few days of silent testing (mirrors the #5132 hidden-assessment pattern). This defers D7's nav/footer entry; it does NOT change the route or storage.

The page must also adopt the website's new visual language (D7 — scroll-reveal idiom, restyled section styling) without editing the `alt/redesign-2026` branch (style-only, no collision).

## Decision

**A stable `/survey` route hosting a config-driven survey flow, built on the 5123 functional-core/imperative-shell idiom (ADR-035), with questions in a zod-validated content module (mirroring ADR-036).**

### Route + hidden-launch deploy fallback

- `/survey` is registered in `App.tsx` **above the catch-all `*`**, exactly like `/assessment`. The route is a constant; question edits never touch it (US-03 capability KPI 6).
- **Hidden launch**: NO entry is added to `Navigation.tsx` `navItems` and NO sitemap entry is added (D7 nav/footer entry DEFERRED — recorded as an open question). The page is reachable only by typing/sharing the URL.
- **GH-Pages SPA deep-link fallback** must be extended in `deploy.yml` so a direct hit on `https://<site>/survey` serves a 200 SPA shell with no redirect flash, exactly as `/assessment` and `/admin/assessment` already do:

  ```yaml
  - name: Create SPA route fallbacks for GitHub Pages
    run: |
      mkdir -p dist/lighthouse dist/assessment dist/admin/assessment dist/survey
      cp dist/index.html dist/lighthouse/index.html
      cp dist/index.html dist/assessment/index.html
      cp dist/index.html dist/admin/assessment/index.html
      cp dist/index.html dist/survey/index.html
  ```

- **NO `robots.txt` Disallow** for `/survey`: `robots.txt` is public, so a `Disallow: /survey` would *advertise* the hidden path. Hiding is by omission (no link, no sitemap), not by a public disallow directive.

### Survey content module (`src/features/survey/content/surveyContent.ts`)

- A single typed, **zod-validated-at-load** module is the one source of truth for the questions: `questions: SurveyQuestion[]` (each `{ id, prompt, options: string[] }` — single-select), Q4's response scale (confirmed at DELIVER per the DISCUSS open item), the exchange-screen copy, and the trial opt-in label.
- The zod schema asserts the structural invariants at module load (non-empty prompt, ≥2 options per question, unique question ids) — turning "the survey renders the current question set" into a load-time guarantee, mirroring ADR-036. Editing a question is a data edit to this module; the route and storage shape are unaffected (US-03).
- The answer payload serialized into the `answers jsonb` column is a typed `SurveyAnswers` shape (question-id → selected-option), zod-validated at the submit boundary. Q4 doubles as the 5123 demand signal (it travels in `answers`, read by the dashboard survey view).

### Flow + driven ports (reuse 5123's shapes)

- A small survey flow (render questions → optional trial opt-in → submit → thank-you) using `react-hook-form + zod`, with the same degrade/retry discipline 5123 established.
- Driven ports reuse the widened shared ports (ADR-040): `ResponseRepository.save` (anon-INSERT via the existing `SupabaseResponseRepository`, now accepting null score/band) for the anonymous answers; a new `SurveyLeadCapture` adapter (`EdgeFunctionSurveyLeadCapture`) invoking `capture-survey-lead` (ADR-041) on opt-in.
- The hexagon interior (the survey content + answer shape + any flow reducer) imports neither `@supabase/...` nor `window`, per ADR-035's import convention.

### Error paths (from the journey)

- Content fails to load → graceful "survey temporarily unavailable", not a blank page (zod-at-load makes a malformed config a build/test failure, not a runtime blank).
- Supabase write fails → clear retry-able error, NO thank-you, answers preserved (journey `step-submit-to-supabase`).
- Double-submit → de-duplicated/idempotent submit (disable-on-submit + a client-side submitted guard; one respondent not double-counted).
- Partial write (response saved, trial lead failed) → surface that the trial request specifically failed so the user can retry the opt-in (journey failure mode).

## Alternatives Considered

- **Questions in Supabase (CMS-style)**: rejected for v1 (same as ADR-036) — adds a fetch + loading state + round-trip to static, version-controlled copy; contradicts the no-round-trip render. Revisit only if non-engineers must edit without a deploy.
- **A query-param-driven survey selector (`/survey?set=v2`)**: rejected — makes the URL carry version state, which breaks "the link never changes" (D1) and the shared-link guarantee. Questions are config, the route is constant.
- **A `robots.txt` Disallow to hide the page**: rejected — `robots.txt` is publicly readable and would advertise the path; hidden-by-omission (no nav, no sitemap) is the correct mechanism, matching #5132.
- **Reuse the assessment quiz machine verbatim**: rejected — the assessment has a 6-step linear scored flow with resume + a partial-completion scoring guard; the survey is a short single-page-ish unscored form. Reusing the *idiom* (functional core, content module, driven ports) without the *scoring machine* is the right granularity of reuse.

## Consequences

- **Positive**: stable route + config-driven questions satisfy US-01/US-03 with a load-time content guarantee; the page reuses 5123's functional-core idiom and the (widened) shared ports; hidden launch needs only the one `deploy.yml` cp line, no robots risk.
- **Negative**: the deferred D7 nav/footer entry is an explicit open question (must be added before the public launch, after the silent-testing window). A small amount of new survey-flow code (justified — it is a genuinely different, unscored flow).
- **Open**: Q4's exact response scale (interest scale vs yes/no) is a DISCUSS open item to confirm in DELIVER; the content module's zod schema accommodates either.

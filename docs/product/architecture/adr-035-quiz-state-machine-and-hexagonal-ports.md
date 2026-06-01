# ADR-035: Client quiz state machine + hexagonal port boundaries (pure scoring core / driven adapters)

> **Scope: WEBSITE repo (`/storage/repos/website`), NOT the Lighthouse product.** Authored for ADO Epic #5123.

## Status

Accepted (DESIGN wave, 2026-05-30)

## Context

The assessment is a multi-step client flow (intro → Q1..Q6 → teaser → email gate → breakdown) with: resume-on-refresh (sessionStorage), a partial-completion guard (block scoring, redirect to first unanswered question), deterministic client-evaluable scoring, and several effectful boundaries (sessionStorage, Supabase responses-INSERT, the `capture-lead` Edge Function, analytics). The website is functional React (hooks). Lighthouse CLAUDE.md mandates OOP **for the Lighthouse product**; the website is a separate functional-React codebase. We need an explicit architectural shape that keeps the riskiest logic (scoring + state transitions) pure and testable while isolating effects.

## Decision

Apply **functional-core / imperative-shell**, which maps cleanly onto hexagonal ports-and-adapters for a React SPA:

### Pure domain core (no React, no I/O — the hexagon interior)

- **`scoring` module** — `score(answers): { rawSum, score, band }` and the band-mapping table. Pure function over an immutable `Answers` (exactly six `0|1|2|3`). The single source of truth for `rawSum`, `score`, `band`. Exhaustively unit-tested at the boundaries (0, 25, 26, 50, 51, 75, 76, 100; all-0; all-3).
- **`quizMachine`** — a pure reducer `(state, event) => state` over the flow states `intro | question(n) | teaser | gate | breakdown`, with events `start | answer(n,v) | back | next | submitEmail | unlock`. Encodes: back-nav preserves answers; scoring is gated on exactly-six-answers; deep-link/partial guard computes `firstUnansweredIndex`. No effects inside the reducer.
- **`content` module** — see ADR-036 (single typed, zod-validated source for questions/ladders/band copy/CTAs/anchor). Consumed by core (band→content lookup) and shell (rendering).

### Driving ports (inbound — the imperative shell drives the core)

| Driving port | Surface | Notes |
|--------------|---------|-------|
| Route `/assessment` | `App.tsx` route (above catch-all) | hosts the flow; reads/writes the machine |
| Navigation entry | `Navigation.tsx` `navItems` | "Forecasting Readiness" → `/assessment` |
| Email-gate form | react-hook-form + zod | submits `email` into the capture path |
| Dashboard route `/admin/assessment` | `App.tsx` route | Supabase-auth-gated read surface (ADR-033) |

### Driven ports (outbound — the core/shell depend on interfaces, not Supabase/Storage directly)

| Driven port (interface) | Adapter (impl) | Backed by |
|-------------------------|----------------|-----------|
| `ResponseRepository.save(response)` | `SupabaseResponseRepository` | anon-INSERT into `responses` (ADR-032) |
| `LeadCapture.capture(lead)` | `EdgeFunctionLeadCapture` | `supabase.functions.invoke("capture-lead")` (ADR-032) |
| `QuizPersistence.load()/save(answers)/clear()` | `SessionStoragePersistence` | `sessionStorage` |
| `AnalyticsSink.track(event)` | website analytics adapter | ADR-037 funnel events |

The driven ports are plain TypeScript interfaces (types for data, `interface` for behaviour per CLAUDE.md TS convention). Adapters are injected at the page composition root (a small provider / hook factory at the `/assessment` route), so component tests substitute fakes and assert behaviour (e.g. degrade-open: a `LeadCapture` that rejects still unlocks the breakdown). This is the website analogue of the composition-root "wire then use" invariant; the hexagon interior never imports `@supabase/...` or touches `window`.

### Resume / guard mechanics

- On mount, the `/assessment` host calls `QuizPersistence.load()`; if it yields a partial `Answers`, the machine resumes at `firstUnansweredIndex`; if empty/cleared, it restarts at `intro` with a gentle notice.
- Every `answer` event persists via `QuizPersistence.save` (write-through), so a refresh restores. No server round-trip exists before submit, so a refresh never loses a server record.
- Reaching teaser/breakdown with fewer than six answers (deep link / tamper) is impossible through the machine: the host redirects to `firstUnansweredIndex` before computing a score.

## Alternatives Considered

- **Component-local `useState` per screen, scoring inline in the results component**: rejected — scatters the state-transition rules and scoring across components, makes the boundary table hard to test in isolation, and couples scoring to React render. The pure reducer + pure scoring fn make the riskiest logic testable without a DOM.
- **A state-machine library (XState)**: rejected for v1 — the flow is a small linear machine with one guard; a hand-rolled pure reducer is fewer dependencies, trivially testable, and avoids learning-curve cost. Revisit only if the flow grows branches.
- **Persisting answers to `localStorage`**: rejected — the spec calls for *session* resume (mid-quiz refresh), not cross-session; `sessionStorage` matches the intent and auto-clears on tab close, which is the privacy-friendlier default for pre-email answers.
- **Writing OOP classes to honor CLAUDE.md**: rejected — CLAUDE.md's OOP paradigm is scoped to the Lighthouse product; the website is functional React. Forcing classes here fights the idiom. The functional-core/imperative-shell framing delivers the same isolation/testability that ports-and-adapters delivers in the C# product. (No paradigm is written into the website repo; this ADR records the choice.)

## Consequences

- **Positive**: scoring + transitions are pure and exhaustively testable without a DOM; effects isolated behind four driven ports → degrade-open, resume, and the partial-guard are each unit/component-testable with fakes; swapping `responses` from anon-INSERT to an Edge Function later (ADR-032 follow-up) is one adapter change.
- **Negative**: the port indirection is mild ceremony for a small app; justified by the testability the DoD demands. Adapter wiring at the route root must be done once and kept out of the core (enforced by review / an import convention — the core directory must not import `@supabase` or `window`).

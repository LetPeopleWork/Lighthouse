# Evolution — lighthouse-user-survey (ADO Epic #5124)

Archived 2026-06-01. Feature: a standalone, shareable user-feedback survey on the website, nudged occasionally and respectfully from inside Lighthouse, with an optional trial opt-in. Extends the 5123 Supabase platform (ADR-031..037); ADRs 040..046.

## What shipped

Two surfaces, two repos:

- **Website** (`/storage/repos/website`) — slices 01-03 (US-01..05, US-08), shipped earlier sessions:
  - Stable hidden `/survey` page rendering a zod-validated, editable 6-question survey content module (ADR-043); questions are config, the route never changes.
  - One `service_role` `submit-survey` Edge Function: anonymous response write + optional trial lead + per-submission team-notification email to `survey.answer@letpeople.work` (ADR-046, consolidating ADR-040/041/042); degrade-open.
  - Survey view on the 5123 internal dashboard (`summarizeSurvey`, per-question tallies + trial list; reuses 5123 Supabase Auth/shell, ADR-042/033).
  - Structural anonymity preserved (two-table, no FK; PII only on trial opt-in, ADR-034/046).
- **Lighthouse** (this repo) — slices 04-05 (US-06, US-07), the in-app nudge:
  - Write-once per-instance `Install:Timestamp` AppSetting, set on first run via a catch-and-degrade startup probe (`InstallTimestampSeeder`, ADR-045), exposed read-only on the `[Authorize]` non-admin `SystemInfo` surface.
  - FE-derived eligibility (`nudgeEligibility.ts`, ADR-044): premium-first/fail-closed, install age ≥ ~14 days, absolute-UTC comparison; a premium instance NEVER renders the nudge at any age (KPI-5 guardrail, deterministically tested).
  - A non-blocking, dismissible `SurveyNudge` MUI card linking OUT to `https://letpeople.work/survey` (never embeds the survey).
  - Cadence: a `[Authorize]` `SurveyNudgeController` (GET state / POST action) + `AppSettingService.RecordSurveyNudgeAction`, persisting a server-computed next-eligible instant + a remind-later counter (new AppSetting rows, no schema migration).

## Key decisions and one mid-DELIVER pivot

- **ADR-044 FE-derived eligibility** (user-confirmed): no new eligibility endpoint, so CLI/MCP clients N/A. Persisting the user's choice still needed a non-admin WRITE surface — a recorded extension of ADR-045 (the `SurveyNudgeController` action POST), logged in the feature-delta Upstream Issues note. RBAC: the nudge surfaces are `[Authorize]` (any authenticated user), NOT `[RbacGuard]`/`IRbacAdministrationService` — showing/snoozing a feedback nudge is not an authorization operation; premium is a license-tier check.
- **US-07 refined during the live slice review (2026-06-01)**: the original single ~6-month cadence became a THREE-CHOICE, two-tier model — **Take the survey** / **Not interested** quiet ~6 months; **Remind me later** (and the card ✕, treated as remind-later so no one is guilt-declined) quiet ~1 week, backing off to ~6 months after two reminders so it never becomes a weekly nag. Copy made explicitly opt-in with a soft trial-license hint pointing at the existing survey opt-in (no new entitlement logic). Back-propagated to `nudge-cadence-dismissal.feature`, US-07, content-spec, and ADR-044/045.

## Quality

- Live dogfood verified (isolated DB, backdated install timestamp): the three choices, the ✕-as-remind-later, the cadence, and the premium-gone guardrail all behaved as specified.
- Adversarial review: APPROVED (one test-gap finding fixed).
- Per-feature mutation: backend 83.33% (gate ≥80%), FE eligibility logic 91.11%; the `SurveyNudge.tsx` component blend is presentational (justified survivors). Report: `docs/feature/lighthouse-user-survey/deliver/mutation/mutation-report.md`.
- CI green on `c9f78775` (one stochastic Monte Carlo forecast flake re-run, unrelated).

## Lessons (also in docs/ci-learnings.md)

- **S6964 recurrence 3** — a value-type **enum** on a `[FromBody]` DTO under-posts to enum 0 (`TakeSurvey`), silently quieting the nudge ~6 months. Fixed with nullable + `BadRequest()` rather than `[JsonRequired]` (which previously broke 22 live Playwright tests via empty-body 400s); safe here because the only sender always posts a concrete action.
- **S4782** — an optional member (`?`) plus an explicit `| undefined` is redundant; drop the `| undefined`.
- Local `dotnet build`/`pnpm build` green does NOT imply the SonarCloud `new_violations = 0` gate passes — both S6964 and S4782 sit below build severity and burned a CI cycle each.

## ADO

Epic #5124. Stories #5133/#5134/#5135 (website) Closed; #5136 (US-06) and #5137 (US-07) Resolved 2026-06-01 at CI-green.

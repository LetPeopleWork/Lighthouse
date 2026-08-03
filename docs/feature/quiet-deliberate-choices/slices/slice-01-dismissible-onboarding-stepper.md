# Slice 01 — Dismissible "Get Started" panel

**Story**: US-01 | **Job**: `job-config-admin-dismiss-onboarding-guidance` | **Estimate**: ~2h

## Goal

Give the Overview's "Get Started" panel a close control that hides it permanently for that browser.

## IN scope

- `OnboardingStepper.tsx` — a close `IconButton` in the panel's top-right, and the read/write of
  `localStorage["lighthouse-hide-onboarding-stepper"]`.
- Guarded storage access so private-browsing / storage-disabled does not take the Overview down.
- Vitest coverage for AC-1.1 … AC-1.8.

**Dropped from this slice after investigation: the Playwright assertion.** Two facts kill it, both
verified: (1) the seeded demo data has a connection, Teams and a Portfolio, so `activeStep === 3` and
the panel never renders in any demo-data spec — no Overview walking skeleton can reach it; (2) the one
fixture where it does render is the `@rbac` auth-bootstrap spec
(`RoleBasedAccessControl.spec.ts:103`), and dismissing it there would write the flag into that browser
context's `localStorage`, so the later RBAC gate at `:347` — "a team reader does not see the onboarding
stepper" — would pass for the wrong reason. Reaching the panel any other way means an unseeded
instance, which is a re-seed to get to one page. The dismissal is pure client state with no backend
surface and is covered by 8 unit tests; the E2E would cost more than it proves.

## OUT of scope

- Server-side or per-user persistence (D1).
- Any way to restore the panel from the UI (D3).
- Changing what counts as onboarding-complete; the Portfolio step stays required (D4).
- Touching `OverviewDashboard.tsx` beyond what the component change forces — the `rbac.isSystemAdmin`
  gate and the props stay as they are.

## Learning hypothesis

Confirms, if it succeeds: a browser-local flag is enough — nobody needs this preference to follow them
across machines.
Disproves, if it fails: either the panel cannot be hidden before first paint without a flash, or the
RBAC E2E specs (`RoleBasedAccessControl.spec.ts:103` and `:347` both assert on
`getByTestId("onboarding-stepper")`) depend on the panel in a way a dismissal breaks. Either failure is
visible in the first hour.

## Acceptance criteria

AC-1.1 … AC-1.8 in `feature-delta.md`. The two load-bearing ones:

- **AC-1.5** — key already `"true"` at first render → the panel never mounts. Read the flag in the same
  `useMemo`/initial state that computes `activeStep`, not in an effect.
- **AC-1.6** — `localStorage` throwing must not break the Overview.

## Dependencies

None. `LighthouseVersion.tsx:45-84` is the pattern to copy, not a dependency.

## Reference class

`LighthouseVersion.tsx` — same shape (boolean dismissal, `lighthouse-hide-*` key, no server state),
already shipped and tested.

## Pre-slice SPIKE

None. No uncertainty worth a timebox.

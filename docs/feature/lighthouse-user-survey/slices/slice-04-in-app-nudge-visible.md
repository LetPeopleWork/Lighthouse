# Slice 04: In-app nudge appears for eligible non-premium users

**Feature**: lighthouse-user-survey
**Repo**: Lighthouse (`/storage/repos/Lighthouse`) — FE + small BE
**Stories shipped**: US-06
**Estimate**: ~1 day / ≤ 6h

## Goal

Deliver the visible in-app nudge value: a non-premium Community instance, once it is at least
~2 weeks old, shows a calm, dismissible nudge inviting feedback with a button that opens the
stable `/survey` page; **premium instances and brand-new installs never see it**. This slice is
deliberately the "the nudge becomes visible and clickable" slice — it is NOT infrastructure-only.

## IN scope

- Lighthouse BE: a per-instance **install/first-run timestamp** (`installTimestamp`) persisted
  server-side, written once on first run.
- Lighthouse BE/FE: an eligibility evaluation — show only if `isPremium == false` AND install age
  ≥ ~14 days. The premium check is evaluated FIRST and is absolute; the gate **fails CLOSED** on
  any uncertainty (backend unreachable / tier unknown → no nudge).
- Lighthouse FE: a small, non-blocking, dismissible nudge component (Lighthouse's own design
  system, D7) with a primary action that opens `/survey` (links out; does NOT embed the survey).
- UTC-stable / monotonic-safe install-age comparison (no early fire on clock skew).
- Deterministic test: premium instance NEVER renders the nudge at any install age (KPI 5 made
  executable).

## OUT scope

- ~6-month cadence + dismissal persistence (slice 05 — here the nudge can show; recurrence
  correctness is slice 05).
- Embedding the survey in the pop-up (out, D2).
- Any new RBAC role/permission (premium is a license-tier check, not authorization).
- Website-side work (done in slices 01-03).

## Learning hypothesis

**Confirms if it succeeds**: a well-timed in-app nudge reaches non-premium users without ever
touching premium users, and clicking it lands them on `/survey` — adding reach to the channel.
**Disproves if it fails**: the premium gate is leaky (a paying customer sees it → hard blocker),
or the install-age timing fires early on clock skew.

## Acceptance criteria

See US-06 in `../feature-delta.md`. Slice specifics:

- Non-premium, install age ≥ ~14 days → nudge shown with a working `/survey` link.
- Install age < ~14 days → no nudge (never day 0).
- Premium instance → no nudge at any install age (deterministic test).
- Backend unreachable / tier uncertain → no nudge (fail closed).
- Clock skew / timezone edge → not-yet-eligible never becomes eligible early.

## Dependencies

**Hard**: the stable `/survey` route exists (slice 01) for the nudge to link to.
**Hard**: the existing `canUsePremiumFeatures` / license-tier signal (existing Lighthouse concept).

## Production data requirement

**Required.** Verified on the project's own Lighthouse dev/dogfood instance: a non-premium
instance older than ~2 weeks shows the nudge; importing a premium license makes it disappear.

## Dogfood moment

On the project's own Lighthouse instance: with a non-premium config and install age ≥ ~14 days,
confirm the nudge appears and its button opens `/survey`. Then import a premium license and
confirm the nudge is gone.

## Pre-slice spike candidates

- 30 min: confirm where a per-instance setting (install timestamp) is best persisted server-side
  in the existing Lighthouse settings/persistence (ports-and-adapters); confirm whether the
  eligibility evaluation needs a new endpoint or can be FE-derived from existing signals — this
  determines the CLI/MCP version-gate question (feature-delta cross-cutting checklist).
- 15 min: confirm the existing `canUsePremiumFeatures` read path the nudge will reuse.

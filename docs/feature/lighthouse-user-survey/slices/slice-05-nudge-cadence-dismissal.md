# Slice 05: Nudge cadence + dismissal + timezone-safe timing

**Feature**: lighthouse-user-survey
**Repo**: Lighthouse (`/storage/repos/Lighthouse`) — FE + small BE
**Stories shipped**: US-07
**Estimate**: ~1 day / ≤ 6h

## Goal

Make the in-app nudge respectful: a non-blocking, dismissible invitation that, whether
clicked-through or dismissed, does not reappear for ~6 months — persisting last-shown/dismissal
server-side so it never re-nags across sessions. This delivers the "feels like an invitation, not
a nag" value (the annoyance-proxy KPI). User-visible, not infrastructure-only.

## IN scope

- Lighthouse BE: a per-instance `lastShownAt` / dismissal-state setting persisted server-side.
- Nudge sets/confirms `lastShownAt` on BOTH click-through and dismissal.
- Eligibility extended: show only if (never shown) OR (`lastShownAt` older than ~6 months), on top
  of the slice-04 premium + install-age gates.
- Dismiss closes the nudge with no side effects and persists so it does not return next session.
- Click-through opens `/survey` (stable route) and also confirms `lastShownAt`.
- Tests: after show/dismiss, no re-show until `lastShownAt` + ~6 months; dismissal survives a
  restart/refresh; cadence comparison is UTC-stable (no early re-show on clock skew).

## OUT scope

- Per-user (vs per-instance) cadence — cadence is per-instance this round.
- A/B timing experiments on the ~2-week / ~6-month constants.
- Website-side work; embedding the survey in the pop-up (out, D2).

## Learning hypothesis

**Confirms if it succeeds**: the nudge is rare and respectful enough that dismiss-without-click
stays low and users don't experience it as a nag — sustaining the channel without annoyance.
**Disproves if it fails**: dismissal doesn't persist (re-nags), or the ~6-month cadence misfires
on clock skew → the exact habit-pain users hate, requiring rework.

## Acceptance criteria

See US-07 in `../feature-delta.md`. Slice specifics:

- Click primary action → `/survey` opens; nudge does not reappear until `lastShownAt` + ~6 months.
- Dismiss → closes with no side effects; persists server-side; no re-show until +~6 months.
- Nudge is non-blocking, dismissible, Lighthouse design system, link-out only (no embedded survey).
- Both paths set/confirm `lastShownAt`.

## Dependencies

**Hard**: slice 04 (the nudge shows for eligible non-premium users; premium + install-age gates
exist).

## Production data requirement

**Required.** Verified on the project's own Lighthouse dogfood instance: dismiss the nudge,
restart/refresh, confirm it stays gone.

## Dogfood moment

On the project's own instance: dismiss the nudge, refresh and restart, confirm it does not
reappear; verify (by adjusting `lastShownAt` in a controlled test) that it would return only after
~6 months.

## Pre-slice spike candidates

- 15 min: confirm the per-instance setting store used in slice 04 can hold `lastShownAt` cleanly
  alongside `installTimestamp`.

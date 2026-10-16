# Slice 07 — Story 6304 — Owners whose own value equals a default follow it in one step

**ADO**: Story #6304 (Epic #6303) · **User story**: US-06 · **Estimate**: ~1 day · **Depends on**: slices 05a, 05b, 06
**Release point**: yes — #6304's last slice; #6304 releases after it (L8).
**Locked decisions**: D9, D10, D12, D14 (with G-07: `SystemAdmin` is enough)

## Goal
The connection's defaults group shows, per default, how many Teams / Portfolios follow it and which carry their own
value (named, with the value). Owners whose own value already equals the default can be moved to follow it in one
step; deliberate differences (another field, None) are listed but not offered.

## Why it still earns its place after D10
D10 moves only *empty* owners onto the default at the upgrade. A hand-configured installation — the reporter's,
whose 14 Teams all carry "Caused by" — keeps every Team on *Own*, so without this the default helps only new Teams
there and the next change of it is 14 edits again. It also gives the "who differs" view between saves, which the
save notice (a moment, not a page) cannot. Pass 1's "see who differs" and "adopt" stories are merged here.

## IN
- Read: per-default follower count and the own-value owners, named with their value (configurable terms).
- Write: follow = change mode only; effective value and fingerprint unchanged → nothing queued.
- Each owner written with its concurrency token; a conflict is reported per owner, never overwritten.
- Generic over settings: Story #6313's slices 08–12 appear in the list by registering their setting, with no new
  UI.
- Loading and could-not-load looks per DESIGN (house rule); likely `DataGridBase` or a plain list.
- **Permission (G-07, decided by the maintainer 2026-10-16)**: the action writes Team and Portfolio settings from the
  connection page, and `SystemAdmin` is enough — the route is guarded by `SystemAdmin` through
  `IRbacAdministrationService`, with no per-owner `TeamWrite` / `PortfolioWrite` check and no new permission
  (AC-6.6).

## OUT
- Setting a *different* value on several owners (that is editing their settings, not following).

## Learning hypothesis
Disproves "hand-configured owners want to follow the default" if, in the dogfood, the maintainer leaves the
matching owners on *Own* — then the slice shrinks to the read-only list.

## Production data / dogfood
Dev instance's real Teams, several of which already share a Parent Override field. After following, count the
share of owners in *Connection default* mode on that connection (`OUT-6303-followers-share`, per instance).

## Acceptance criteria
AC-6.1 – AC-6.6.

## Changed assumptions
(filled during DELIVER)

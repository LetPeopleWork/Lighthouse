# Slice 18 — Run the Refinement on one shared screen

**Feature**: epic-5510-5881-refinement · **Epic**: E4 Live Refinement sessions (new) · **Story**: US-18 ·
**Estimate**: ~1d (less than before; no on-behalf recording) · **Tier**: Community (live sessions are the gateway
and stay free, D8, D25)

## Goal

The coach presents the Refinement tab on one screen. It shows one Work Item at a time, starting with the highlighted
ones that are not yet Ready, with splits and comments visible. The room talks; the screen keeps its place. This needs
no push channel and works standalone (C2, D3).

## What presenter mode records (DD-20, maintainer 2026-10-02)

- **Auth on**: only the facilitator's own vote and comments, under their account. Colleagues vote from their own
  devices if they want to. Whatever the room concludes, the facilitator writes as a comment.
- **Auth off**: the same. The presenting browser can save its own vote and comments, under its self-declared name,
  and nothing on anyone else's behalf.
- Entries saved while presenter mode is open are marked as live (K4, K7).

## IN

- A Present action on the tab, opening a full-screen, keyboard-operable view.
- Navigation that skips Ready Work Items.
- The usual vote control and comment box for the presenter's own identity.
- The splits are visible (DD-12 exemption).

## OUT

Recording votes for named colleagues or for the room. Remote participants (spike 19). Timers and card animations.

## Learning hypothesis

**This disproves "a shared screen is enough to make the room discuss only the doubted" (D8 gateway)** if the dogfood
session still walks every Work Item. That would mean the room needs its own devices and live votes, which is the
remote mode (19).

## Data and dogfood moment

- Demo: Team Gravity. GR-073 is Ready; GR-051 and GR-054 are not.
- Dogfood: run one real Refinement in presenter mode. Count the Work Items discussed against those skipped as Ready.

## Acceptance criteria

AC-18.1 … AC-18.4 (US-18).

## Dependencies

Slices 11 and 13 (12 recommended).

# Slice 15 — Vote under my own account (auth on)

**Feature**: epic-5510-5881-refinement · **Epic (proposed)**: E3 Sizing votes (#5510) · **Story**: US-15 ·
**Estimate**: ~½d · **Tier**: Community code; named votes are Premium by platform (authentication is Premium, P2)

## Goal

With authentication on, a vote carries the signed-in account. There is no name prompt and no guest link (D14). Team
read (Viewer) is enough to vote.

## IN

- Identity is taken from the session for every write in the log.
- An RBAC check. With RBAC on, the user needs Team read; without it, the call is a 403 and nothing is rendered. With
  RBAC off, any signed-in user may vote.
- The UI is gated through `useRbac()`.

## OUT

Guest or anonymous voting on auth-on instances.

## Learning hypothesis

**This disproves "readers vote through the existing Team-read requirement"** if Team read cannot authorise a write
without a new `RbacGuardRequirement`. If DESIGN finds that, it records the new requirement as a decision, rather than
quietly borrowing the edit permission.

## Data and dogfood moment

A Premium dev instance with auth on and two users: one Viewer, and one with no role on the Team.

## Acceptance criteria

AC-15.1 … AC-15.3 (US-15).

## Dependencies

Slice 11.

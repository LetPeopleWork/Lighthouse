# Slice 16 — Take back my vote

**Feature**: epic-5510-5881-refinement · **Epic (proposed)**: E3 Sizing votes (#5510) · **Story**: US-16 ·
**Estimate**: ~½d · **Tier**: Community · **Lower priority** (maintainer)

## Goal

A voter can take back their own vote. It stops counting, and the log records the revocation (DD-9). Only the same
account, or the same browser when auth is off, can take a vote back (DD-10).

## IN

- A revocation entry in the log, and readiness recomputed afterwards.
- A "Take back" action on the voter's own vote only.

## OUT

Admins removing other people's votes.

## Learning hypothesis

**This disproves "people need to revoke rather than re-vote"** if nobody uses it within a month of dogfooding. It is a
low-cost slice either way.

## Data and dogfood moment

Dogfood: watch for questions like "how do I undo my vote?" before this slice exists. If nobody asks, defer it.

## Acceptance criteria

The US-16 scenarios.

## Dependencies

Slice 11.

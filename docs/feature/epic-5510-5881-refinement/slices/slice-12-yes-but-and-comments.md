# Slice 12 — Say the condition behind "Yes, but…"; comments and open questions

**Feature**: epic-5510-5881-refinement · **Epic (proposed)**: E3 Sizing votes (#5510) · **Story**: US-12 ·
**Estimate**: ~1d · **Tier**: Community

## Goal

A vote can carry a comment, and a "Yes, but…" prompts for its condition. A comment can be left without a vote; it
marks the Work Item as having an open question and counts for nothing (DD-11). Each Work Item has a readable log.

## IN

- An optional comment on every answer, prompted (but not forced) for "Yes, but…".
- Comment-only entries in the same log.
- A marker in the list for comments and for open questions.
- The log view per Work Item, oldest first, with names and dates.

## OUT

Editing or deleting comments (the log is append-only; a correction is a new comment), mentions, notifications.

## Learning hypothesis

**This disproves "comments carry the conditions" (D5)** if most "Yes, but…" votes in dogfood arrive without a
condition. In that case the condition becomes mandatory.

## Data and dogfood moment

- Demo: GR-051 carries Ana Lima's "Yes, but… only if the PDF export moves to its own Work Item".
- Dogfood: count the "Yes, but…" votes that have a condition after two Refinements.

## Acceptance criteria

AC-12.1 … AC-12.3 (US-12).

## Dependencies

Slice 11. DD-11 confirmed by the maintainer (DD-17): no "can't tell yet" answer.

# Slice 17a — Read votes and readiness from the CLI or an assistant

**Feature**: epic-5510-5881-refinement · **Epic**: E3 Sizing votes (#5510) · **Story**: US-17a ·
**Estimate**: ~½d · **Tier**: Community · **Repo**: `lighthouse-clients`

Split 2026-10-02 after the maintainer put client voting in scope (DD-19). 17a = read. 17b = cast or take back a vote
(`slice-17b-clients-cast-a-vote.md`). Both fit the 1-day bar; together they would not.

## Goal

The refinement command and tool from slice 09 add each Work Item's readiness and vote count: Ready, "2 more Yes
needed" or "Needs discussion". The split stays hidden until the client's voter has voted on that Work Item (DD-12).

## IN

- Additive fields in the existing response.
- A changeset and a minor bump. 17a and 17b may share one release.

## OUT

Casting or taking back votes. That is 17b.

## Learning hypothesis

This is slice 09's hypothesis applied to votes. If the client needs data it can only get by voting, the read shape
is wrong and is fixed before 17b builds on it.

## Data and dogfood moment

Dogfood: before the Refinement, ask the MCP assistant "which Work Items need discussion at Team Gravity's
Refinement?".

## Acceptance criteria

AC-17a.1 … AC-17a.3 (US-17a).

## Dependencies

Slices 09 and 13.

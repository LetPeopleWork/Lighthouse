# Slice 11 — Cast a sizing vote from the list

**Feature**: epic-5510-5881-refinement · **Epic (proposed)**: E3 Sizing votes (#5510) · **Story**: US-11 ·
**Estimate**: **~1½d — over the 1-day bar, called out** · **Tier**: Community

## Goal

Anyone with Team read votes Yes, "Yes, but…" or No on any Work Item in a refinement state, at any time (DD-1). On a
Community instance, with auth off, the voter gives a self-declared name once per browser (DD-10).

## Why it is not split further

This slice needs four things together: a new vote-log aggregate, its migration, a vote endpoint with self-declared
identity, and the vote control in the list. Shipping the endpoint alone gives a slice no voter can use. Shipping the
control alone has nothing to write to. Shipping only the auth-on path would serve no Community instance, because
Community is always auth-off (DV-5). Accepted at about 1½ days rather than disguised as one.

## IN

- An append-only vote log (DD-9). The current vote is the latest entry per voter per Work Item.
- A vote write gated by **Team read** (US-15 / DESIGN note: a write behind a read permission).
- With auth off, a name prompt on the first vote, held per browser (ADR-191 precedent) and editable afterwards.
- A tally cell, "n votes", with the user's own vote marked.
- Votes are accepted whatever the verdict and whatever the row's position relative to the line.
- The usage-data "vote cast" event, with the refinement-day enum (K4) as designed in DEVOPS.

## OUT

Comments (12), readiness (13), hiding the split (14), auth-on identity (15), revoking a vote (16).

## Learning hypothesis

**This disproves "a vote costs seconds from the list" (O5)** if a dogfood voter needs more than 2 minutes to vote on 5
Work Items. In that case a focused, one-at-a-time voting mode is needed before E3 continues.

## Data and dogfood moment

- Demo: seed a few votes from "Jonas Weber", "Ana Lima" and "Mo Okafor" (three, so the default of 3 Yes can be met,
  DD-21) for screenshots and the E2E.
- Dogfood: ask two people to vote on the dev Team's top five between two Refinements. Time it, and note *when* they
  voted (this is the R4 signal — R4 is measured by K4; no push channel (D21)).

## Acceptance criteria

AC-11.1 … AC-11.6 (US-11).

## Dependencies

Slice 02. Slice 10 should land first so the question carries its number.

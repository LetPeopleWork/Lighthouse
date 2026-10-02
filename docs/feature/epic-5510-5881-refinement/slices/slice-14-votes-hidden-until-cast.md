# Slice 14 — Others' votes stay hidden until you cast yours

**Feature**: epic-5510-5881-refinement · **Epic (proposed)**: E3 Sizing votes (#5510) · **Story**: US-14 ·
**Estimate**: ~½d · **Tier**: Community · **Cancellable by design**

## Goal

Until you have voted on a Work Item, you see "3 votes" but not how they split or what the comments say. Once you vote,
everything is shown (DD-12). Readiness status stays visible, because it does not reveal the split. Presenter mode is
exempt.

## IN

- Hiding per voter, for both account voters and per-browser voters.
- No bypass for Team admins outside presenter mode.
- The API omits the split and the comments for a caller who has not voted. Hiding them only in the UI is not enough.

## OUT

Timed reveals and facilitator-controlled reveals.

## Learning hypothesis

**This disproves "anchoring matters in async voting" (O4)** if no dogfood voter notices or cares after a month. In that
case the slice is a candidate for removal. The fact that it was built does not settle whether it stays.

## Data and dogfood moment

- Demo: Jonas has not voted on GR-051.
- Dogfood: ask two voters whether they would have voted differently had they seen the split first.

## Acceptance criteria

AC-14.1 … AC-14.3 (US-14).

## Dependencies

Slice 12.

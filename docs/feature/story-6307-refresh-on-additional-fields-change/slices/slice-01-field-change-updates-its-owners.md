# Slice 01: A field change on a connection updates its Teams and Portfolios

**Goal:** saving a connection whose additional fields changed queues, at once, an update of every Team and
Portfolio on it whose fetch fingerprint the save moved, and the page stops saying "next refresh of data".

## IN scope
- After the connection PUT stores the save, queue every Team and Portfolio on that connection whose fingerprint
  this save moved, through the existing per-owner update and queue (D1, D2, D8). Every licence (D4).
- Nothing queued for a save that moves no fingerprint (credential, URL option, auth method, name, write-back only),
  for owners on other connections, or for a refused save (403, 409, 400) (D8, D10).
- Existing coalescing keeps it to one update per owner per save (D9).
- Replace the pre-save hint at `ModifyConnectionSettings.tsx:674-680` with copy saying that saving updates the Teams
  and Portfolios on the connection (AC-1.8; copy from the DISTILL sketch, UI-2).

## OUT of scope
- The after-save message with counts (slice 02); save without updating (slice 03).
- Updates started by reading a connection (D11).
- Narrowing the fingerprint (D3); any threshold (D5).

## Learning hypothesis
- **Disproves** "starting every owner's already-owed full fetch at Save is acceptable load" if, on the dev instance
  (`:5169`, real history), one field save holds the queue long enough to delay a manual update by more than one
  scheduled interval (60 min), or the tracker throttles.
- **Disproves** "the fingerprint picks exactly the right owners" if a credential-only save queues anything.
- **Confirms** both if the Task Manager rows clear within one scheduled round and a token rotation queues nothing.

## Acceptance criteria
US-01, AC-1.1 … AC-1.9 in `../feature-delta.md`.

## Dependencies
None open. ADR-030/ADR-140 amendment text from DESIGN (D1) lands with or before this slice.

## Effort
About 6 hours: owners-by-connection query (the delete path already asks it, controller `:133-134`), the
"moved by this save" predicate on the pure `FetchFingerprint.For`, the enqueue, one copy change, tests.
Reference class: a Team settings save that refreshes its owner.

## Dogfood
Dev instance (`:5169`): add a field to its connection, watch the Task Manager rows, then build a rule on the field
and check it matches. Rotate the token and check nothing is queued.

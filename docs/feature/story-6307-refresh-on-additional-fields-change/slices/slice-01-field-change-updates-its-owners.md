# Slice 01: A field change on a connection updates its Teams and Portfolios

Reshaped at DISTILL (2026-10-15) after the sketch session: backend only. The page changes move to slices 02 and 03.

**Goal:** saving a connection whose Additional Fields changed starts, at once, an update of every Team and Portfolio
on it, through the same queue every update goes through, Teams first. A save can ask for the updates to be left to
their next scheduled run with `?startUpdates=false`.

## IN scope
- After the connection PUT stores the save, start every Team and Portfolio on that connection whose fetch
  fingerprint the save moved, through a domain event and a handler calling `TriggerUpdate` (D1, D2, D8; ADR-239).
  Every licence (D4).
- Start nothing for a save that changes nothing fetched (API token, name, address, sign-in method, write-back
  mapping, the same fields in another order), for Teams on other connections, for a connection nobody uses, for a
  refused save (409, 403, 400), for a read by id that registers the tracker's own fields, or for the save after it
  (D8, D10, D11).
- `?startUpdates=false` stores the save and starts nothing; any value but `true` or `false` is refused with 400 and
  stores nothing (DDD-8, S6).
- A stored save answers 200 even if the fan-out or the owner lookup fails afterwards; that is logged (DDD-16) and
  pinned by a unit test of the controller in DELIVER, because the acceptance host cannot make it fail.
- The existing queue keeps it to one full download per owner per save (UC-1).
- The fetch fingerprint's digest stays byte-identical (`FetchFingerprintPinnedDigestTest`), so the upgrade itself
  starts no downloads.

## OUT of scope
- Anything on the connection page (slices 02 and 03). Until slice 02 ships, the page saves exactly as today, so
  every field save starts the updates and the old "next refresh of data" alert stays, now pessimistic.
- Any change to the PUT response: DESIGN dropped the save-result DTO after S3.
- Narrowing the fingerprint (D3); any threshold (D5).

## Learning hypothesis
- **Disproves** "starting every owner's already-owed full fetch at Save is acceptable load" if, on the dev instance
  (`:5169`, real history), one field save holds the queue long enough to delay a manual update by more than one
  scheduled interval (60 min), or the tracker throttles.
- **Disproves** "the fingerprint picks exactly the right owners" if a credential-only save starts anything.
- **Confirms** both if the Task Manager rows clear within one scheduled round and a token rotation starts nothing.

## Acceptance tests
`Slice01FieldChangeUpdatesItsOwnersTest` (14 pending, walking skeleton first) and `Slice01SavesThatStartNothingTest`
(15 guards, green now) under `Lighthouse.Backend.Tests/API/Integration/ConnectionFieldChange/`, plus
`FetchFingerprintPinnedDigestTest` (2 guards). US-01 AC-1.1 to AC-1.7 and AC-1.9; US-03 AC-3.1 and AC-3.3 at the port.

## Dependencies
None open. ADR-030/ADR-140 amendments from DESIGN land with or before this slice.

## Effort
About 6 hours: the connection's share of the fingerprint as its own function, the owners-by-connection query, the
event and handler, the query parameter, tests. Reference class: a Team settings save that refreshes its owner.

## Dogfood
Dev instance (`:5169`): add a field to a connection, watch the Task Manager rows, then build a rule on the field and
check it matches. Rotate the token and check nothing starts. `lh` saves update too, since the parameter defaults to
`true`.

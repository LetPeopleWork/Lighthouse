# Slice 03 — Say so when the Release is gone

**Goal**: a Delivery whose Release no longer exists keeps its values, says the source is unavailable, and
offers a way out.

**Story**: US-04.

## IN scope

- Detect "this Release resolved to nothing" as distinct from "the read failed" (slice 02 already handles
  the second).
- **A cleared date raises the same state (D12).** A bound Release whose `releaseDate` is removed in Jira
  freezes and flags exactly like a deleted one — different message, identical behaviour. This is not an
  exotic case: D11 tells users to go set dates in Jira, so the feature teaches the gesture that produces
  it, and two of three Releases on the demo instance are dateless today.
- Freeze: last synced date, name and Features are kept. Nothing cleared, nothing deleted (D6).
- A broken-source state on the Delivery naming when it last synced successfully.
- An **Unbind** action returning it to Manual with those values, editable.
- The same degradation when the connection stops offering the capability at all (D2) — a credential
  downgrade must not error and must not silently unbind.

## OUT of scope

- Re-pointing at a different Release in one step. Unbind then rebind is enough for now.
- Notifying anyone. The state is visible where the Delivery is read; no email, no alert.
- Auto-unbind (explicitly rejected by D6).

## Learning hypothesis

**Disproves D6** if the frozen state reads as "working" to someone who did not set it up — if a user sees
a date and does not register the banner, freezing is worse than auto-unbinding, because it looks live and
is not.

**Confirms** that a durable-record Delivery (#5698) and a remote-bound one can coexist without the remote
being able to destroy the record.

## Acceptance criteria

AC-04.1 through AC-04.6 in `feature-delta.md`. The two that carry the slice:

- A transient read failure does **not** raise the broken-source state. Only a resolved "does not exist"
  does. Getting this wrong makes every network blip look like a deleted Release.
- A cleared `releaseDate` raises it with its own message. Reusing the deleted-Release wording would send
  the reader looking for a Release that is sitting right there.

## Dependencies

Slice 02.

## Effort

~4 hours.

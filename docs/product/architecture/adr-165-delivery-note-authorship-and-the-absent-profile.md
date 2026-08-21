# ADR-165: A note stores both an author reference and the name it was written under, and "may I edit this" is two explicit branches rather than a null-tolerant equality

- **Status**: **Proposed** (DESIGN, 2026-08-21)
- **Date**: 2026-08-21
- **Feature**: epic-5698-deliveries-as-durable-records (ADO Epic #5698, slices 02–03)
- **Deciders**: Benjamin Huser-Berta (maintainer), Morgan (Solution Architect)

## Context

D5 says a note is authored by a person and degrades honestly when there is nobody to name.
`CurrentUserProfileService.GetOrCreateFromPrincipalAsync` **returns `null` when the principal carries
no stable subject claim** — which is every request on an auth-off instance. AC-02.5 requires such a
note to read as unattributed; AC-03.5 requires the author-only restriction to lift when there is no
author to compare against.

Separately, this epic's whole thesis (D1) is that a record must not silently rewrite itself after the
fact. That thesis applies to the author line as much as to the numbers.

## Decision

**Two fields, and a predicate with two named branches.**

1. **`AuthorUserProfileId` (`int?`, FK to `UserProfile`, `ON DELETE SET NULL`)** — the identity used
   for authorship comparison. `SET NULL` rather than cascade because removing a person from the
   instance must not delete the record they wrote; the note survives and becomes unattributed, which
   is the honest outcome and the same state an auth-off note is born in.

2. **`AuthorDisplayName` (`string?`)** — the name captured **at write time** and the only thing
   rendered. A note is a dated record of what someone said; re-labelling last quarter's note because
   a display name changed is the same silent rewriting D1 exists to prevent. `null` renders as
   unattributed.

3. **The permission predicate is explicit about the absent profile.** The naive form is a live bug:

   ```
   note.AuthorUserProfileId == currentProfile?.Id
   ```

   *(Rationale corrected 2026-08-21. The earlier version named the wrong failing case: it claimed a
   profile-less caller would gain rights over an **attributed** note. For `int?`, `null == 5` is
   false, so that caller is already refused by the naive one-liner. The prescribed test therefore
   guarded nothing — it passed against the very implementation it was meant to reject.)*

   Enumerated properly, the naive form has exactly one wrong outcome:

   | Note author | Caller | Naive result | Correct |
   |---|---|---|---|
   | attributed (5) | no profile (`null`) | false | false — ok |
   | unattributed (`null`) | no profile (`null`) | true | true — ok |
   | attributed (5) | profile 5 | true | true — ok |
   | **unattributed (`null`)** | **profile 7** | **false — refused** | **true — should be allowed** |

   The failing case is a note written while the instance had auth off, on an instance that has since
   turned auth on: nobody can ever edit or withdraw it, because every caller now has a profile and no
   caller's id equals `null`. That is the auth-off-then-auth-on instance this ADR's Consequences
   section already half-identifies, and it strands every note written before the switch.

   The rule is therefore written as two branches, in one place, used by both the edit and the
   withdraw paths:

   - the note has **no** author → anyone with `PortfolioWrite` may edit or withdraw it;
   - the note **has** an author → only a caller with a profile whose id equals it may.

   The first branch is D5 as written: where there is no author, the author restriction cannot apply.
   The second never depends on a null comparison succeeding.

4. **Notes cascade with the Delivery.** `DeliveryNote.DeliveryId` → `Delivery` is `ON DELETE CASCADE`
   (AC-02.8), matching `DeliveryMetricSnapshot` under ADR-048.

5. **`CreatedOn` is `DateTime` and `LastEditedOn` is `DateTime?` — UTC instants, not `DateOnly`.**
   This is deliberately the opposite call from `Delivery.ArchivedOn`
   ([ADR-160](./adr-160-delivery-closure-pin-as-one-row-per-delivery-table.md) point 4), and the
   reason is that a note records *when a person wrote something* — a moment — whereas `ArchivedOn` is
   a calendar day.

   The global `Properties<DateTime>()` converter is **correct** for a genuine UTC instant: it
   round-trips the value and restores `Kind = Utc` on read. It damages a `DateTime` only when the
   value is a local-kind midnight standing in for a day, which is why `ArchivedOn` must not be one and
   why these may be.

   AC-02.2 and AC-03.3 still render a day, so **the reduction happens once, on the server, in a named
   zone**: the DTO carries the instant *and* a `DateOnly` day produced by
   `ILighthouseClock.ToInstanceDay`. The instance zone is the zone every other day in the product is
   expressed in. The client renders the supplied day and is never handed an instant to reduce, which
   structurally removes the `toISOString().split("T")[0]` foot-gun. The instants remain the sort key —
   newest-first, and two notes written on the same day — and "edited" is `LastEditedOn is not null`.

   A `Kind == Utc` assertion does not prove the day is right, so the test reads a note back through a
   **fresh** EF context and asserts the rendered **day**.

## Alternatives considered

- **Store only the FK and render the profile's current display name.** **Rejected** — it makes an
  archived, frozen note change its byline months later. For an ordinary comment surface that would be
  a feature; for a durable record it is the defect this epic was written to remove. Noted that it also
  loses the name entirely once the profile is deleted, which is when the record matters most.

- **Store only a denormalised name and no FK.** **Rejected** — authorship comparison would fall back
  to string matching, which two users sharing a display name would break, and renaming a user would
  silently transfer edit rights.

- **Refuse to accept notes at all when there is no profile** (auth-off instances cannot annotate).
  **Rejected** — it contradicts D5 and would make the feature unavailable on the default
  configuration.

- **Treat an absent profile as a synthetic "anonymous" `UserProfile` row.** **Rejected** — it
  manufactures an identity, so every anonymous note on the instance would appear to share one author
  and would be mutually editable *by construction* rather than by an explicit rule anyone can read.

## Consequences

- **Positive**: AC-02.5 and AC-03.5 fall out of the two branches directly, and neither depends on
  `null` equality behaving a particular way.
- **Positive**: a deleted user leaves readable, correctly-dated notes behind.
- **On an auth-off instance the whole feature still works, and every note is unattributed.** Writing a
  note requires `PortfolioWrite`, which an auth-off instance grants. `GetOrCreateFromPrincipalAsync`
  returns `null`, so **both** author fields are written `null` — no placeholder string, no synthetic
  "Unknown" author, because a fabricated name is exactly the dishonesty D5 rules out. The note renders
  as unattributed, and by branch one of the predicate any writer may edit or withdraw it. That is
  AC-02.5 and AC-03.5 together, and it is the only self-consistent reading: with nobody to name, there
  is nobody to restrict to.
- **Negative**: an unattributed note is editable by any writer on the Portfolio. That is D5's stated
  intent, but it also applies to a *legacy* unattributed note on an instance that has since enabled
  auth. Recorded rather than hidden; the alternative (freezing unattributed notes permanently) would
  strand every note written before auth was turned on.
- **Negative**: two author fields can disagree if one is written and the other is not. Both are set in
  the same constructor call and neither has a setter.
- **Enforcement**: the predicate is one method on the entity, used by both mutating endpoints. The
  pinning test is **unattributed note + profiled caller → allowed** — the one case the naive
  one-liner gets wrong, and therefore the only test that can tell the two implementations apart. The
  other three rows of the table above are covered too, but they do not discriminate and must not be
  mistaken for the guard.
- **Reuse verdict**: `UserProfile` → **REUSED AS IS** (no new columns).
  `CurrentUserProfileService` → **REUSED AS IS** — its `null` return is the input this design is built
  around, not a defect to fix. `DeliveryNote` → **CREATE NEW** (no existing entity carries free text
  against a Delivery). No new identity mechanism.
- Cross-refs [ADR-164](./adr-164-archived-delivery-write-refusal-in-the-aggregate.md) (D6 — notes
  freeze when the Delivery is archived, by the same aggregate invariant),
  [ADR-001](./adr-001-rbac-ui-gating-strategy.md) (the RBAC gating the note endpoints inherit),
  [ADR-048](./adr-048-delivery-metric-snapshot-store.md) (the cascade convention followed here).

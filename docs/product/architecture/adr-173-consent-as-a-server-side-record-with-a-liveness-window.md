# ADR-173: Consent is a server-side record keyed by a hashed opaque token, and it is live only while the browser holding it keeps showing up

- **Status**: **Proposed** (DESIGN, 2026-08-22)
- **Date**: 2026-08-22
- **Feature**: epic-5733-opt-in-usage-data (ADO Epic #5733, slices 01-02)
- **Deciders**: Benjamin Huser-Berta (maintainer), Morgan (Solution Architect)

## Context

Consent is per browser, recorded by an opaque token minted server-side after the click, never by a
fingerprint. There is no user identity to key on: authentication is optional and impossible in
standalone, and with it off `DisabledAuthenticationHandler` hands every caller the subject
`lighthouse|auth-disabled`, so an account-scoped record would collapse to one decision the first
person makes for everybody.

Four acceptance criteria constrain where the record can live, and they do not all pull the same way.

- **The next emit after a revocation must not happen** - not on the next cycle, not after a backend
  restart - and it is asserted at the emit path rather than at the UI. The emitter is a server-side
  background service, so the record it consults has to be reachable from the server. A record that
  exists only in the browser cannot gate it.
- **Revoking the last consenting browser stops the instance heartbeat too.** So the gate is not a
  per-browser boolean; it is a question about a *set* - does any live grant remain on this instance.
- **Clearing browser storage is equivalent to never having decided.** This is the hard one, and the
  delta does not say how it is achieved. The server cannot observe a cleared `localStorage`. A row
  written on the click and never touched again says "granted" forever, so the instance would keep
  emitting on behalf of a browser that no longer exists and whose owner believes they revoked by
  clearing their data.
- **Nothing is written to the browser and nothing is transmitted until a button is pressed**, which
  is what keeps the token inside the strictly-necessary exemption rather than requiring consent to
  record consent.

The third of those is what forces the design below. Without it, a plain grant/revoke table would do.

## Decision

**A `UsageDataConsent` row per browser, keyed by the SHA-256 digest of the token, carrying a
`LastSeenAt` that the state endpoint refreshes - and a grant counts only while that stamp is
recent.**

1. **The entity is additive and small.**

   | Column | Purpose |
   |---|---|
   | `Id` | surrogate key |
   | `TokenHash` | SHA-256 of the token, base64, **unique index** - the lookup key |
   | `Decision` | `Granted`, `Declined` or `Revoked` - three states, not two; see point 6 |
   | `DecidedAt` | when the button was pressed, and when a revoke flipped it |
   | `LastSeenAt` | last time this browser presented its token |
   | `AskedAt` | when this browser was last shown the dialog unprompted (slice 02) |

   One additive migration per provider through `Create-Migration.ps1`, named `AddUsageDataConsent`
   on both, matching the house convention `<yyyyMMddHHmmss>_<PascalCaseVerbNoun>`.

2. **The token is minted the way this codebase already mints bearer secrets.**
   `RandomNumberGenerator.GetBytes(32)`, base64url-encoded, returned once, and stored by the browser
   under `lighthouse:usagedata:consent`. Only the digest is persisted. The plaintext token is never
   written to a log, never included in an error response, and never sent to the collector - the
   collector never sees a browser at all, because emit is server-side.

3. **Liveness, not just decision.** A grant is *live* when
   `Decision == Granted && LastSeenAt > now - ConsentLivenessWindow`. The window is configurable and
   defaults to **30 days**. A browser still in use keeps its consent alive by using Lighthouse; a
   browser that cleared its storage stops presenting a token, its row ages out, and it stops counting.

   **The refresh is throttled, and the endpoint must not be cached.** A naive design stamps
   `LastSeenAt` on every state call, which puts a write on a read-shaped hot path - and this product
   ships SQLite as a first-class provider, where writers serialise process-wide and would contend with
   the update background services once a user has several tabs open. So the touch is itself a
   conditional update carrying `WHERE LastSeenAt < @stale`, where `@stale` is a small fraction of the
   window (roughly seven hours at thirty days). Tab count then stops mattering: the write happens at
   most a few times a day per browser regardless of how many requests arrive.

   The endpoint must send `Cache-Control: no-store`. A caching reverse proxy in front of it would
   absorb the state call, suppress the touch, and let consent decay under a user who is actively using
   the product - the instance would stop emitting with no symptom on any surface. This is the sharpest
   edge in the design, because it fails silently and in the safe direction, which is exactly the kind
   of fault nobody reports.

   And the window measures what it measures: **a browser still presenting its token**. Not human
   activity. A wall-mounted dashboard or a kiosk renews consent indefinitely with nobody in the room,
   and a replayed token from a pinned test fixture would do the same. That is a real limitation of
   decay-on-silence, and there is no signal available that would do better.

4. **The heartbeat gate is one indexed existence question.**
   `AnyLiveGrantAsync(threshold)` compiles to a SQL `EXISTS`. Revoking the last live grant makes it
   false on the next evaluation, which is how "revoking the last consenting browser stops the
   heartbeat" falls out with no separate rule to write.

5. **The repository is bespoke, not `IRepository<T>`** - following the precedent
   [ADR-131](./adr-131-embed-token-lifecycle-and-revocation-store.md) set for embed tokens, whose
   interface says so in as many words: *"Deliberately not `IRepository<T>`: single use is a
   conditional update returning an affected-row count, which the generic add/save shape cannot
   express."* Two independent reasons apply here.

   - `RepositoryBase.GetByPredicate` takes a `Func<T, bool>`, so it evaluates **client-side** over a
     materialised `DbSet`. That is harmless for the dozen rows in `AppSettings`. Asking "is there at
     least one live grant" through it would load every consent row on the instance on every emit.
   - Revoke and touch are conditional updates whose affected-row count *is* the verdict. A
     read-then-write would let a revoke and a concurrent touch race, and the losing order silently
     re-arms a consent the user just withdrew.

   ```
   Task<UsageDataConsent?> FindByTokenHashAsync(string tokenHash, CancellationToken ct);
   Task<int>  TouchAsync(string tokenHash, DateTime seenAt, CancellationToken ct);
   Task<int>  TryRevokeAsync(string tokenHash, DateTime revokedAt, CancellationToken ct);
   Task<bool> AnyLiveGrantAsync(DateTime livenessThreshold, CancellationToken ct);
   Task<int>  PruneStaleAsync(DateTime threshold, CancellationToken ct);
   ```

6. **Revocation flips the row to a distinct `Revoked` state rather than deleting it or reusing
   `Declined`.** A deleted row would read as "never decided", and the browser would be asked again on
   the next session - the opposite of what it asked for.

   The third state is load-bearing and an earlier draft got this wrong by collapsing revocation into
   `Declined`. "Never ask a Premium browser that said No again" plus "a revoked browser can consent
   again later; the cadence does not lock it out" are only jointly satisfiable if the two are
   distinguishable. With two states, a Premium user who granted and then changed their mind would be
   silently locked out of ever being asked again - which is the opposite of the acceptance criterion
   that says revocation must not be a one-way door. The cadence therefore keys on `Decision` plus
   `AskedAt`, and treats `Revoked` as re-askable. It also gives the maintainer a revocation-rate
   signal for free, which is the single most useful number this feature could produce about itself.

7. **The consent endpoints are rate-limited.** `POST /consent` mints a CSPRNG token and writes a
   durable row on every unauthenticated call, against a table this ADR already admits grows with users
   rather than with data, on instances that are frequently internet-exposed and frequently SQLite.
   Left open it is an unbounded insertion sink; worse, a single caller can plant one `Granted` row and
   keep the instance emitting for a full liveness window regardless of what any real person chose.
   Every other anonymous or token-minting endpoint in this codebase already carries a policy -
   `AuthLoginPolicy`, `ApiKeysPolicy`, `EmbedSessionPolicy`, `BootstrapSystemAdminPolicy` - and this
   one gets `UsageDataConsentPolicy` on the same pattern.

8. **The state endpoint requires no authentication and carries the token in a request header.** The
   driving-ports table calls `GET /api/latest/usagedata/state` *tokenless*; that is read here as
   "requires no **authentication**", because on an auth-off instance there is nothing to
   authenticate against, and because per-browser state is not answerable without the browser naming
   itself. A request with no token is answered `Undecided`, which is also what a token that resolves
   to nothing gets - the two are deliberately indistinguishable, so the endpoint is not an oracle for
   which tokens exist.

## What this design does not achieve, stated plainly

**Clearing browser storage stops that browser counting as consenting after the liveness window, not
instantly.** The acceptance criterion says the two are equivalent. They are equivalent in the two
respects a user can observe - the browser is immediately undecided, and is immediately eligible to be
asked again - because both are decided by the absence of a token in the browser. They are *not*
equivalent in the third respect: if that was the only consenting browser on the instance, the
heartbeat keeps emitting until the row ages out.

No design closes this gap, because the signal never reaches the server: clearing storage is a local
act that produces no request. The only levers are the length of the window and the honesty of the
documentation. Shortening it narrows the gap and makes consent decay faster for people who use
Lighthouse infrequently; lengthening it does the reverse. **30 days is a proposal, not a finding**,
and it is the kind of number the product owner should set rather than the architect.

The consequence is also a correction to what the census means. The delta already requires the docs to
say the instance count means "instances with at least one consenting user". With a liveness window it
means "instances with at least one *recently active* consenting browser". That is arguably a better
number for an installed-base census - it excludes instances nobody has opened in a month - but it is
a different number, and the docs must say which one it is.

## Alternatives considered

- **The consent record lives only in the browser; the backend holds nothing.** Rejected on two
  independent grounds. The heartbeat has no browser to originate from, and the emitter is a
  background service that must answer "may I send" with no browser present. And revocation is
  required to be enforced at the emit path, which is server-side by definition - a browser-held
  record could only ever be advisory.

- **A server-side row with no liveness signal: granted until explicitly revoked.** The simplest thing
  that could work, and it satisfies every acceptance criterion except the one about cleared storage.
  Rejected because that criterion is not decorative: a privacy feature whose consent cannot be
  withdrawn by the one action every user believes withdraws it - clearing their browser data - is
  making a promise it does not keep. The failure is silent and permanent, which is the exact class of
  defect this Epic exists to remove.

- **A cookie rather than `localStorage`.** Rejected. A cookie is attached to every request to every
  endpoint automatically, which widens the surface the per-browser token was deliberately kept narrow
  to avoid, and invites the ePrivacy argument the token design exists to sidestep. `localStorage` is
  read only by the code that needs it, and only after the click.

- **A device fingerprint as the browser key.** Forbidden by the locked decision, and correctly:
  reading canvas, fonts or screen entropy is access to information stored in terminal equipment under
  ePrivacy Article 5(3), so a fingerprint used to *record* consent would need consent to exist. The
  circularity is unfixable.

- **Reuse `IRandomNumberService` to mint the token.** Rejected on inspection - it is
  `new Random().Next(maxValue)`, a statistical PRNG whose only production consumer is the Monte Carlo
  forecaster. It is not a CSPRNG and must not become one by accident.

- **Reuse `EmbedSessionTokenService` itself.** Rejected - its lifecycle is single-use redemption with
  a 60-second window, a handshake nonce, and a `tokenId.secret` split that exists so redemption can be
  atomic. Consent is long-lived, re-presented indefinitely, and revocable. Sharing the service would
  mean one class serving two opposite lifecycles. Its *technique* is copied wholesale; its machinery
  is not.

## Consequences

**Positive**

- Revocation is immediate and structural: the next evaluation reads the database, so no cached copy
  exists anywhere that could disagree with the row.
- "Stop the heartbeat when the last browser revokes" needs no rule of its own - it is the same
  `EXISTS` returning false.
- The token never reaches the collector, so nothing in the vendor's dataset can be joined back to a
  browser even in principle.

**Negative / accepted**

- A cleared-storage revocation is eventual rather than immediate, bounded by the liveness window. See
  above; this is inherent, not incidental.
- The consent table grows one row per browser that is ever asked, including refusals. `PruneStaleAsync`
  bounds it and the row is small, but it is a new table that grows with users rather than with data.
- Every state call performs a write (the touch). On an instance with many open tabs this is a steady
  trickle of small updates. It is one conditional update on an indexed column and is expected to be
  negligible, but it is a write on a read-shaped endpoint and should be watched.

**Reuse verdict**: `EmbedSessionTokenService` -> **UNCHANGED**, technique copied (CSPRNG, base64url,
digest-at-rest, conditional-update-as-verdict), machinery not shared - different lifecycle.
`IEmbedSessionTokenRepository` -> **UNCHANGED**, cited as the precedent for not deriving from
`IRepository<T>`. `IRepository<T>` / `RepositoryBase<T>` -> **UNCHANGED**, assessed and rejected as
the base for this store on the client-side-evaluation and affected-row-count grounds above.
`IRandomNumberService` -> **UNCHANGED**, assessed and **rejected as unsuitable** - not a CSPRNG.
`ApiKeyService` -> **UNCHANGED**; its `FindMatchingKey` re-derives PBKDF2 across every row on every
call, a lookup shape this design must not copy. `AppSetting` -> not used for consent (one row per
browser is not key/value shaped); see
[ADR-175](./adr-175-instance-identifier-as-an-appsettings-scalar-minted-on-first-grant.md) for the
one scalar that does live there.

**Enforcement**

| Rule | Mechanism |
|---|---|
| The plaintext token is never persisted | NUnit: grant, then assert no column of the stored row equals the returned token |
| The plaintext token never appears in a log | NUnit with a capturing logger across grant, state and revoke; assert the token substring is absent |
| A revoke and a concurrent touch cannot re-arm consent | NUnit: interleave `TryRevokeAsync` and `TouchAsync`, assert the row is `Declined` under either order |
| A stale grant does not count | NUnit: a row whose `LastSeenAt` predates the window is not returned by `AnyLiveGrantAsync` |
| An unknown token and an absent token are indistinguishable | NUnit: both responses are byte-identical |
| Nothing is written to the browser before a click | Vitest: render the dialog, close without choosing, assert `localStorage` is untouched |

Cross-refs [ADR-131](./adr-131-embed-token-lifecycle-and-revocation-store.md) (the token technique and
the bespoke-repository precedent),
[ADR-174](./adr-174-the-emit-gate-is-uncached-fail-closed-and-mints-a-permit.md) (the gate that reads
this record),
[ADR-175](./adr-175-instance-identifier-as-an-appsettings-scalar-minted-on-first-grant.md) (minted by
the same grant that writes the first row here).

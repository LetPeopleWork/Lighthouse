# ADR-175: The instance identifier is one `AppSettings` scalar, minted from a CSPRNG on the first grant and never on a refusal

- **Status**: **Proposed** (DESIGN, 2026-08-22)
- **Date**: 2026-08-22
- **Feature**: epic-5733-opt-in-usage-data (ADO Epic #5733, slice 01)
- **Deciders**: Benjamin Huser-Berta (maintainer), Morgan (Solution Architect)

## Context

Nothing that identifies an instance exists today. Everything else the heartbeat needs already does -
version from `GetCurrentVersion()`, platform from `PlatformService`, licence tier from
`CanUsePremiumFeatures()`, install timestamp from `AppSettingKeys.InstallTimestamp`.

The identifier must be random and derived from nothing - not hostname, licence key, database name, or
any other pre-existing value - stable across restarts, and minted lazily so that an instance which
never consents never acquires one. With PostHog it becomes the `distinct_id`, so it is also the join
key for every count the maintainer will ever read.

"Derived from nothing" is the requirement that does the work. An identifier derived from a stable
local value would let anyone holding both the collector dataset and a guess at the input test that
guess offline, which turns an opaque census into a lookup table.

## Decision

**One `AppSetting` row, `UsageData:InstanceId`, holding 16 CSPRNG bytes base64url-encoded, minted in
its own transaction immediately before the first granted consent is written.**

**Ordering, and why it is not "the same save".** An earlier draft said the identifier was written
inside the same `SaveChanges` as the consent row, while also specifying insert-then-catch-the-unique-
violation. Those cannot both hold: a violation aborts the whole save, so the losing browser's grant
would be **lost** while the user watched the indicator flip, and the `DbContext` would be left in a
state where the obvious recovery replays the write - a shape this codebase has already been bitten by
in the archived-delivery race. So the two writes are separate and ordered: mint (or resolve) the
identifier first, in its own transaction, catching the unique-constraint violation and re-reading the
winner; then write the consent row. A browser that loses the identifier race still records its grant.

1. **Storage is the existing key/value table.** `AppSettings` already holds `Install:Timestamp`,
   `FeatureOrdering:Policy` and the survey-nudge cadence stamps. A single opaque scalar is exactly
   what that table is for, and it needs no schema change - only a new constant on `AppSettingKeys`
   and two methods on `IAppSettingService`.

2. **Minting is `RandomNumberGenerator.GetBytes(16)`, base64url-encoded.** This is the same technique
   `EmbedSessionTokenService`, `ApiKeyService`, `OAuthStateTokenIssuer` and `GeneratedKeyRingStore`
   already use. It is chosen over `Guid.NewGuid()` - which on .NET is also random - because a call
   that visibly takes no inputs makes "derived from nothing" self-evident to a reader and testable
   without knowing how the platform implements v4 GUIDs.

3. **It is minted only on a grant.** A refusal writes a consent row and mints nothing. The
   requirement is that an instance with no consenting browser has *no identifier at all*, and an
   instance where one person clicked No is such an instance. This is the detail most likely to be got
   wrong, because both buttons mint a *consent token* and only one mints an *instance identifier*.

4. **Get-or-create is atomic, arbitrated by the primary key.** `AppSettingService` is registered
   `AddScoped`, so two browsers granting at the same moment run two scopes with two `DbContext`s.
   Read-then-insert would produce two identifiers and the losing one would already have been returned
   to a caller. The minting path inserts and, on a unique-constraint violation, re-reads and returns
   the winner.

   **The arbiter already exists and needs no migration.** `LighthouseAppContext.cs:101` configures
   `modelBuilder.Entity<AppSetting>().HasKey(a => a.Key)` — `Key` *is* the primary key of the table.
   Duplicate keys have never been storable, so the insert-then-catch pattern works as written against
   every provider, on every existing customer database, with nothing added.

   **Correction, 2026-09-11.** Points 4 and its Consequences previously required this migration to add
   a unique index on `AppSetting.Key` and to de-duplicate before indexing — keeping the lowest `Id` per
   key and deleting the rest — on the reasoning that a long-upgraded install might hold duplicates. It
   cannot: the schema has never permitted them. That paragraph was written without checking the
   `DbContext`, the DESIGN peer review then sharpened it into a bricked-upgrade hazard (H4), and DEVOPS
   inherited it and rewrote the rollback contract around it. **All of it defended a state that cannot
   occur, and the `DELETE` it prescribed could never have been exercised.** The dedup step, its
   two-provider test, and the restore-rather-than-rollback contract are withdrawn. The migration for
   this feature is purely additive again — it adds the consent table and nothing else — so an image
   rollback is a supported recovery path, as the project's expand-only rule intends.

   **What is real, and is smaller.** `AppSetting.Id` exists (from `IEntity`) but is **not** the key and
   carries no uniqueness constraint. `UpsertSetting` (`AppSettingService.cs:174`) mints new rows as
   `new AppSetting { Key = ..., Value = ... }`, so every row this feature creates carries the default
   `Id = 0` — the same trap already known on `OptionalFeature`. `AppSettingSeeder.RemoveObsoleteSettings`
   deletes **by `Id`**, and its `obsoleteIds` list currently starts at 9, so the identifier row is safe
   today. It would not be if a low id were ever added to that list: the sweep would take the instance
   identifier with it, silently, and the instance would mint a new one and appear in the census as a
   second instance. Guard it with a test asserting the sweep leaves usage-data rows alone, not with a
   migration.

5. **The read is cheap and the write happens once.** After the first grant, resolving the identifier
   is one indexed key lookup, which is what the gate does when constructing a permit. A missing
   identifier is not an error; it is `Suppressed(NoInstanceIdentifier)`, which is the correct state
   for an instance nobody has consented on.

6. **It is never rotated, never exposed to the browser, and never returned by any API.** The consent
   endpoints answer with a decision state, not with the identifier. Nothing in the frontend needs it.

7. **Retention and erasure, decided.** Withdrawal stops future processing; it does nothing about past
   processing, and saying so plainly is part of what the consent dialog owes a reader. The position
   was taken by the maintainer on 2026-09-12 and is written out here so that it is not quietly
   re-litigated later.

   **The collector keeps events for 13 months.** Every question the outcome measures ask is a rolling
   window - a 24-hour reporting count, a 30-, 60- or 90-day trend - and the longest question anyone
   asks of the census is whether the installed base grew against the same month a year earlier.
   Thirteen months answers that, and nothing longer answers anything we ask. If the vendor's project
   settings do not offer thirteen months, take the shortest option **at or above one year** and give
   up the year-on-year comparison rather than buying three years to keep it. The continuous-integration
   project holds only canary probes and takes the shortest retention available to it.

   **The identifier is not deleted when the last live grant expires.** Deleting it would make a
   returning instance a new instance in the census, silently inflating the one number this feature
   exists to produce. It is a random scalar that identifies a database rather than a person, and
   keeping it costs nothing that deleting it would buy.

   **Revocation does not trigger a deletion request at the collector, and the dialog must not imply
   that it does.** Revocation is per browser while the identifier is per instance, so one browser
   revoking cannot mean "erase this instance's history" while another browser on the same instance is
   still consenting. Once the last browser revokes, the instance stops emitting and what it already
   sent ages out on the retention period. The promise the dialog can honestly make is the one it
   should make: revoking stops anything further being sent, and what was already sent is kept for up
   to thirteen months.

## Alternatives considered

- **A dedicated `UsageDataInstance` table.** Rejected - a table with one row and one meaningful column
  is a schema change, a migration on two providers, a repository and a registration, to hold a
  scalar that the existing key/value store already models. There is no relational structure here and
  none is foreseeable.

- **A file beside the database, in the manner of the encryption key store.** Rejected. That precedent
  exists because a key must *not* live in the database it protects; the reasoning does not transfer.
  Here the database is already the durable store, and a second custody location would introduce the
  container-restart and volume-mount failure modes the key store had to solve, for no benefit.

- **`Guid.NewGuid()`.** Not wrong, and it would work. Rejected on legibility grounds only: see point 2.

- **Derive it from a stable local value - machine name, database path, licence subject - hashed.**
  Rejected, and it is the option the requirement exists to forbid. A hash of a low-entropy known input
  is reversible by enumeration, so the census would stop being opaque the moment anyone guessed the
  input format.

- **Mint at first startup rather than at first consent.** Rejected - it would give every instance an
  identifier whether or not anyone ever consents, which is precisely the state the acceptance
  criterion says must not exist. It would also be a silent behaviour change on upgrade for every
  existing installation.

- **Mint on any decision, including a refusal.** Rejected; see point 3.

## Consequences

**Positive**

- No schema change for the identifier itself, and no new component - two methods on a service that
  already owns exactly this kind of value.
- The census is opaque by construction. There is no input to guess.
- An instance that never consents acquires no identifier, so there is nothing for the collector to key
  on and nothing to correlate. (It is *not* indistinguishable from an instance that was never
  installed - a refusal still writes a consent row locally. That row never leaves the instance, but
  the stronger claim would be false and is not made.)

**Negative / accepted**

- Restoring a database backup onto a second instance clones the identifier, and the two will report
  as one. This is inherent to storing it in the database and is the same trade the install timestamp
  already makes. It should be named in the docs rather than engineered around; the alternative
  (binding it to something machine-local) is the rejected option above.
- Adding a unique index to `AppSetting.Key` is a schema change to a shared table. It is additive and
  matches what every existing writer already assumes, but it is a shared-contract edit and should be
  verified against the seeders before it ships.

**Reuse verdict**: `IAppSettingService` / `AppSettingService` -> **EXTEND**. It already owns this
exact shape - `EnsureInstallTimestamp()` is a lazy get-or-create over an `AppSettings` row and is the
direct model for the minting method - and it already holds the private `UpsertSetting` helper, a
`TimeProvider` and the repository. `AppSettingKeys` -> **EXTEND**, one constant, following the
existing `Area:Name` convention. `AppSetting` entity -> **UNCHANGED**, with no schema change at all:
`Key` is already the primary key (see point 4), so the arbiter the mint relies on ships today. `IRandomNumberService` -> **UNCHANGED**, assessed and **rejected as unsuitable**: it is
`new Random().Next(maxValue)`, a statistical PRNG for Monte Carlo forecasting, and using it for an
identity value would be a security defect wearing the costume of reuse.
`ISystemInfoService` / `SystemInfo` -> **UNCHANGED**, and deliberately: the identifier must not join
the system-information response, which is readable by any signed-in viewer including one inside an
embedded frame. That record's own doctrine - *"they are named here rather than at the call site so
that a fourth one added later is withheld by this sentence instead of by somebody remembering to
guard it"* - is the reasoning being honoured by keeping the identifier out of it.

**Enforcement**

| Rule | Mechanism |
|---|---|
| An instance that never granted has no identifier | NUnit: refuse consent, assert the `AppSettings` key is absent |
| A refusal does not mint | NUnit: post a decline, assert no identifier row was written |
| Two concurrent grants yield one identifier | NUnit against a real provider. EF InMemory *does* enforce primary keys, so it would catch a missing arbiter here — but it models neither the write contention nor the provider-specific violation type the catch clause keys on, so run it where the consent-store tests already run |
| The obsolete-settings sweep leaves the identifier alone | NUnit: mint the identifier, run `AppSettingSeeder`, assert the row survives. `RemoveObsoleteSettings` deletes by `Id`, and every row this feature mints carries the default `Id = 0` |
| A browser that loses the identifier race still records its grant | NUnit: force a unique violation on the mint, assert the consent row is written and the returned identifier is the winner's |
| It is derived from nothing | NUnit: mint twice under identical hostname, database name and licence, assert the values differ; and the minting method takes no parameters |
| It never leaves through an API | ArchUnitNET: no controller or DTO type may reference the identifier accessor except the usage-data emit path |
| It is stable across restarts | NUnit: mint, dispose the context, re-resolve, assert unchanged |

Cross-refs [ADR-173](./adr-173-consent-as-a-server-side-record-with-a-liveness-window.md) (the grant
whose save also mints this),
[ADR-174](./adr-174-the-emit-gate-is-uncached-fail-closed-and-mints-a-permit.md) (the permit that
carries it, and the `NoInstanceIdentifier` suppression),
[ADR-149](./adr-149-key-store-beside-the-database.md) (the beside-the-database precedent, assessed
and found not to transfer).

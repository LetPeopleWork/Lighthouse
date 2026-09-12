# ADR-191: The analytics identity is a per-browser pseudonym minted beside the consent record, resolved server-side, and never on the wire — and install counts are given up for it

- **Status**: **Proposed** (DESIGN, 2026-09-12)
- **Supersedes**: [ADR-175](./adr-175-instance-identifier-as-an-appsettings-scalar-minted-on-first-grant.md)
- **Date**: 2026-09-12
- **Feature**: epic-5733-opt-in-usage-data (ADO Epic #5733, slice 01c)
- **Deciders**: Benjamin Huser-Berta (maintainer), Morgan (Solution Architect)

## Context

Every event needs a `distinct_id`. The abandoned design used an instance identifier for it, and
ADR-175 existed to mint that identifier. **The maintainer has decided that no instance identifier
appears in the payload at all**: the unit of counting is the consenting browser, and nothing in what
we send ties two browsers of the same installation together.

ADR-175 was checked before being superseded, because an ADR that still does something must not be
retired for tidiness. It does not. Its own Context says the identifier *"must be random and derived
from nothing … With PostHog it becomes the `distinct_id`, so it is also the join key for every count
the maintainer will ever read."* Point 6 adds that it *"is never rotated, never exposed to the
browser, and never returned by any API"*, and point 5 says the only read is the one the gate performs
when constructing a permit. So the identifier has exactly one consumer, that consumer is the
`distinct_id`, and with the `distinct_id` reassigned there is nothing left for it to do.

It is nonetheless **shipped**. `UsageDataConsentService.RecordDecisionAsync` calls
`appSettingService.EnsureUsageDataInstanceId()` on every grant
(`Services/Implementation/UsageData/UsageDataConsentService.cs:80`), backed by
`AppSettingService.EnsureUsageDataInstanceId()` at `:101` and `GetUsageDataInstanceId()` at `:148`,
under the key `UsageData:InstanceId` (`Models/AppSettings/AppSettingKeys.cs:27`). Nothing reads it.
So this ADR has to say what happens to running instances, not merely what the design is.

Two further constraints come from the maintainer's brief and are inputs rather than conclusions:
**the consent token must not be reused as the analytics id**, because it is the capability to revoke
and must never reach a third party; and **fingerprinting is rejected**, which this ADR records
rather than re-argues.

## Decision

**One `AnalyticsId` column on the existing `UsageDataConsent` row: 16 CSPRNG bytes, base64url,
minted only on a grant, resolved server-side from the presented token, and never transmitted to or
from the browser.**

### 1. It lives on the consent row, not in `AppSettings`

The identity is per browser and the consent record is already per browser, keyed by
`TokenHash` on a unique index (`Data/LighthouseAppContext.cs:166-168`). A key/value scalar was right
for an instance-scoped value and is wrong for a per-row one. One additive, expand-only column,
nullable — it is null for every row that recorded a refusal or a withdrawal, and for every row
already in the table when the migration runs.

One EF migration per supported provider through the existing `Create-Migration.ps1`, additive only,
so an image rollback stays a supported recovery path.

### 2. The browser never sees it and never sends it

This is the load-bearing property and it is the reason for choosing this option over the obvious
one. The browser presents only its consent token. The backend resolves the token to the consent row
and reads the analytics id off it. **The analytics id is never a field in any request or response,
in either direction.**

That makes two bug classes unrepresentable rather than guarded against:

- **A client cannot influence the `distinct_id`.** There is no field to put one in, so nobody can
  emit under someone else's pseudonym, and nobody can emit under a pseudonym of their own choosing to
  poison a cohort.
- **The consent token cannot leak into the collector by confusion.** The two values are different
  columns of different kinds — one is a digest of a capability and never leaves the database, the
  other is a pseudonym whose whole purpose is to leave — and no code path holds both in the same
  string-typed variable.

It also means the frontend gains no new storage key. Nothing is added to `localStorage` beyond the
`lighthouse:usagedata:consent` token already written by the shipped
`useUsageDataConsent` hook.

### 3. Minted on a grant, never on a refusal, and never re-minted

Carried forward from ADR-175 point 3, for the same reason and at browser scope instead of instance
scope: a browser that said No is a browser that has no pseudonym anywhere. `RecordDecisionAsync`
mints the value in the same `AddAsync` that writes the consent row — one save, no ordering problem,
because unlike ADR-175 there is no second table and no race between two browsers for one shared
value. The get-or-create transaction, the unique-violation catch and the "a browser that loses the
identifier race still records its grant" rule all disappear with the shared scalar that made them
necessary.

Minting is `RandomNumberGenerator.GetBytes(16)`, base64url, through the same `UrlSafeValue.Generate`
helper the shipped token path already uses. Derived from nothing, and visibly so: the call takes no
inputs.

**It is not derived from the token.** An HMAC of the token hash under a server secret was considered
and is rejected below.

### 4. Re-consent after a withdrawal produces a new pseudonym

A browser that revokes and later grants again writes a new consent row with a new token, and
therefore a new analytics id. That is the correct behaviour and worth naming: the person withdrew,
and the history they accumulated under the old pseudonym must not silently reattach to them when they
change their mind. The same follows for clearing browser storage.

### 5. Erasure, retention, and what the dialog may promise

ADR-175 point 7's retention position survives unchanged and is not re-litigated: PostHog fixes
retention by plan — one year free, seven on any paid plan — it cannot be shortened on request, and a
plan upgrade would silently widen it and falsify what the docs state. If volume ever forces a paid
plan, the usage data page changes in the same act.

What changes is what erasure means, and it changes in the user's favour. Under ADR-175 the vendor's
"person" was an *instance*, so deleting one erased a whole installation's history and could not be
offered to an individual — point 7 says as much. Here the vendor's "person" is a **browser**, which
is the same unit the person in front of the dialog controls. A deletion request is therefore
answerable for exactly the right scope. It is still deliberately not *promised* in the dialog,
because a promise of erasure is a promise to operate a process and there is no process yet; but the
thing that was structurally impossible is now merely unbuilt, and that is a different sentence.

The shipped dialog's line — *"A random value lets us tell a repeat visit from a new one; it carries
nothing about you and means nothing outside Lighthouse"* — was written for the instance identifier
and is true, unchanged, of the browser pseudonym. No copy change is required by this ADR.

### 6. The shipped instance identifier is removed, and the orphan row is left alone

`EnsureUsageDataInstanceId`, `GetUsageDataInstanceId`, the `AppSettingKeys.UsageDataInstanceId`
constant, the call site at `UsageDataConsentService.cs:80` and its `try`/`catch` all go. An unread
random value sitting in the settings table of a **privacy** feature is a liability with no benefit,
and leaving dead code whose name claims it identifies the instance would mislead the next reader.

**The row already written on instances that have granted is left where it is.** It is sixteen random
bytes that nothing reads and that identify nothing. It is deliberately *not* added to
`AppSettingSeeder.RemoveObsoleteSettings`, which deletes by `Id` — and ADR-175 point 4 established
that every row minted through this path carries the default `Id = 0`, so naming a low id in that
sweep would take unrelated rows with it. The cleanup is not worth that risk. It is recorded in the
usage data page instead.

## The loss, stated without softening

**How many Lighthouse installations exist is now permanently unanswerable from usage data.** Not
approximately, not with a wide error bar — not at all. Nothing in the payload distinguishes "one
instance with five consenting browsers" from "five instances with one each", and no amount of later
analysis recovers it, because the information was never sent.

This is accepted, and it is the direct cost of the maintainer's decision that the payload carries
user and feature counts only.

**What it does to US-04** ("Know how many instances exist and which versions they run",
`feature-delta.md:404`):

| Element | Verdict |
|---|---|
| "how many instances exist" | **Dead.** Permanently. No design recovers it |
| "which versions they run" | **Survives, transformed** — as the distribution of consenting *browsers* by version. A three-browser instance counts three times |
| AC-04.1 heartbeat once per day per instance | **Dead** — there is no heartbeat |
| AC-04.2 exactly five fields | **Replaced** by the closed event and property contract in ADR-190 |
| AC-04.3 / AC-04.4 instance identifier, random, stable, derived from nothing | **Relocated** to the browser pseudonym. "Derived from nothing" and "minted only on a grant" carry over verbatim; "stable across restarts" becomes "stable for as long as the browser keeps its token" |
| AC-04.5 silent degradation | **Survives unchanged** |
| AC-04.6 the dogfood instance appears in the dashboard on ship day | **Survives**, retargeted at events |
| AC-04.7 docs say the count means "instances with at least one consenting user" | **Must be rewritten.** It now means consenting browsers, and instance counts are not produced at all |

For the job US-04 actually serves — picking a deprecation date that does not strand people — the
browser distribution is arguably the better number, because a deprecation strands *people*, not
databases. But it is a **different** number, and the honest requirement is that the docs say which
one it is rather than letting a reader assume the old one.

`OUT-usagedata-instances-reporting` ("≥ 25 distinct instance identifiers report in a rolling 24h
window") is **unmeasurable as written** and needs a new unit and a re-set target.

These are upstream changes to the DISCUSS delta and are written up for back-propagation in
**`docs/feature/epic-5733-opt-in-usage-data/design/upstream-changes.md`** — six items, U1 to U6.

**This is a coordination gate, not an architecture fix, and it should block DISTILL rather than this
ADR.** The design is implementable as written; what is not yet settled is whether the product owner
has re-worded US-04, re-set `OUT-usagedata-instances-reporting` to a unit that exists, and accepted in
writing that the install census is gone. If a crafter builds this and the feature documents still
promise an installed-base number, the code is right and the acceptance criteria are unsatisfiable —
which is the expensive order to discover it in.

## Alternatives considered

- **Keep the instance identifier as the `distinct_id`, as ADR-175 specified.** It is the option that
  answers the installed-base question, and the whole reason ADR-175 exists. **Rejected by the
  maintainer**, and the trade is worth recording rather than burying: an instance id is a stable
  handle on one customer's deployment that we would be sending to a third party for the lifetime of
  that installation, alongside a version string and a deployment shape. Giving it up costs the
  install census and buys a payload in which nothing persistent identifies a customer's
  infrastructure. The maintainer chose the second.

- **Both**: an instance id *and* a browser pseudonym, joined at the collector through PostHog Groups.
  The option that answers every question. **Rejected twice over** — Groups is a paid feature, and it
  is moot regardless, because with no install entity in the payload there is nothing to group *by*.

- **The browser mints the analytics id and sends it with every batch.** The obvious implementation
  and the one an SDK would use. **Rejected.** The value is then client-controlled: a caller can emit
  under any pseudonym, including one copied from a colleague, and the cohort counts the whole feature
  exists to produce become writable by anyone who has clicked Yes once. It also puts a second value
  in `localStorage` and a second identifier on the wire, and every design where two identifiers
  travel together eventually has a bug where the wrong one is sent.

- **Derive the analytics id as an HMAC of the token digest under a server-held secret.** No new
  column, no new storage, still unguessable offline. **Rejected on two independent grounds.** It makes
  the pseudonym a function of the capability, which is the coupling the maintainer's "do not reuse the
  consent token" rule exists to prevent — anyone holding both a leaked token and the dataset can
  confirm a match by computing it, so the separation would depend on a secret staying secret rather
  than on the two values being unrelated. And it would make a re-consent after a withdrawal produce
  the *same* pseudonym, silently reattaching a history the person had walked away from.

- **Reuse the consent token itself as the `distinct_id`.** **Forbidden by the maintainer's decision,
  and correctly**: the token is the capability to revoke. Sending it to a third party would mean the
  vendor's dataset contained the credential that switches the feature off.

- **A browser fingerprint** — canvas, fonts, screen entropy — as the analytics identity.
  **Rejected, and the rejection is recorded so it is not reopened.** Four separate reasons, each
  sufficient. It is "gaining access to information stored in terminal equipment" under ePrivacy
  Article 5(3) as read by EDPB Guidelines 2/2023, with no strictly-necessary exemption available for
  an analytics purpose, so it would need consent before it could happen. It contradicts what the
  shipped dialog tells the reader — *"a random value … carries nothing about you"* — which would stop
  being true. It defeats revocation *by design*: the point of a fingerprint is that clearing storage
  does not change it, so a person who withdrew would be re-recognised, which is the opposite of the
  one promise this feature makes. And it is less accurate than a random value in both directions —
  it merges distinct people who share a hardware and browser build, and it splits one person across
  an update that changes their font list.

- **Delete the shipped `UsageData:InstanceId` rows through the seeder's obsolete sweep.** **Rejected**
  — see decision point 6. The sweep deletes by `Id` and these rows carry `Id = 0`.

## Consequences

**Positive**

- The `distinct_id` cannot be chosen, forged or replayed by a caller, because it is not on the wire.
- A withdrawal is clean: nothing links the pseudonym before it to the one after.
- Erasure becomes answerable at the scope the person actually controls.
- No `AppSettings` key, no cross-scope get-or-create, no unique-violation catch, no two-write
  ordering rule — the three most fragile parts of ADR-175 disappear with the shared scalar.
- Nothing persistent about a customer's *infrastructure* leaves the instance as an identifier.

**Negative / accepted**

- **Install counts are gone, permanently.** The largest single loss in this redesign.
- Shipped code is deleted, and shipped tests with it. That is a real cost and it is smaller than
  carrying a dead identifier in a privacy feature.
- One additive migration on both providers, on a table that is one release old.
- A browser that clears storage looks like a new subject, so any longitudinal measure over a horizon
  longer than the liveness window is biased upward. Name it in the docs; it is inherent to per-browser
  identity and is exactly the property that makes withdrawal work.
- Restoring a database backup onto a second instance clones every consent row, and both instances
  will report under the same pseudonyms. This is the same trade ADR-175 already made for the instance
  identifier; it should be named in the docs rather than engineered around.

**Reuse verdict**: `UsageDataConsent` entity → **EXTEND**, one nullable column, additive migration.
`UrlSafeValue.Generate` → **REUSE AS-IS**, already the minting path for the consent token.
`IAppSettingService.EnsureUsageDataInstanceId` / `GetUsageDataInstanceId` / the
`AppSettingKeys.UsageDataInstanceId` constant → **DELETE**; ADR-175's EXTEND verdict on
`IAppSettingService` is reversed. `AppSetting` entity → **UNCHANGED**, and ADR-175's whole
`Key`-as-primary-key analysis becomes irrelevant to this feature, which now stores nothing there.
`AppSettingSeeder` → **UNCHANGED**, and deliberately not extended — see decision point 6.
`IRandomNumberService` → **UNCHANGED, still rejected as unsuitable**: `new Random().Next(maxValue)`
is a statistical PRNG for Monte Carlo forecasting. `ISystemInfoService` / `SystemInfo` →
**UNCHANGED, and deliberately**: it is readable by any signed-in viewer including one inside an
embedded frame, and the pseudonym must not join it. ADR-175's rule that the identifier never leaves
through an API carries over to the pseudonym unchanged, and is now easier to enforce because no
endpoint has any reason to name it.

**Enforcement**

| Rule | Mechanism |
|---|---|
| The analytics id never appears in any request or response | ArchUnitNET: no controller, DTO or API type may reference the `AnalyticsId` member; NUnit asserting the `/state` and `/consent` response bodies do not contain the stored value |
| A refusal mints no pseudonym | NUnit: post a decline, assert the row's `AnalyticsId` is null |
| A withdrawal followed by a fresh grant yields a different pseudonym | NUnit over the two rows |
| It is derived from nothing | NUnit: two grants under identical hostname, database and licence produce different values; the minting call takes no parameters |
| The consent token and the analytics id are never the same value | NUnit over a grant: the returned token, its stored digest and the stored pseudonym are three distinct values |
| The token is never what reaches the publisher | NUnit over the serialised outbound body: the `distinct_id` equals the stored pseudonym and the token substring is absent |
| `UsageData:InstanceId` is gone from the code | NUnit: the constant does not exist; a grant writes no `AppSettings` row |
| The migration is additive on a real provider | Migration test on Sqlite and Postgres — EF InMemory skips migrations |

Cross-refs
[ADR-175](./adr-175-instance-identifier-as-an-appsettings-scalar-minted-on-first-grant.md)
(superseded; its retention position and its derived-from-nothing rule are carried forward),
[ADR-173](./adr-173-consent-as-a-server-side-record-with-a-liveness-window.md) (the row this column
joins), [ADR-190](./adr-190-usage-data-events-detected-in-the-browser-forwarded-by-the-backend.md)
(the pipe that resolves and spends this pseudonym),
[ADR-176](./adr-176-posthog-cloud-eu-as-a-named-adapter-with-payload-carried-privacy-controls.md)
(the collector that receives it as the `distinct_id`).

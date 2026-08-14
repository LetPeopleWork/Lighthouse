# ADR-149: The key store resolves beside the database, and the app refuses to mint where it cannot argue durability

**Status**: Accepted
**Date**: 2026-08-14
**Feature**: `epic-5775-secret-encryption-key-custody` (ADO Epic #5775, slice 02 / Story #5024)
**Decider**: Morgan (Solution Architect), DESIGN application layer, interaction mode = PROPOSE
**Implements**: D4, D10, D12 · AC-2.1, AC-2.7, AC-2.10, AC-2.11

---

## Context

D4 makes the first boot of a fresh instance generate its own key, wrapped with ASP.NET Data Protection
and written beside the existing key store — the shape `EnsureOAuthStateSecret` (`Program.cs:429-486`)
already ships. D12 then observes that the *location* of that store is wrong the moment anything
important lives in it.

The concrete numbers, read from code:

- `ResolveDataProtectionKeyStoreDir` (`Program.cs:448-457`) returns
  `Lighthouse:DataProtection:KeyStorePath` if set, else `ContentRootPath/data-protection-keys`.
- **Standalone already sets it correctly.** `StandaloneInitializer.InitializePaths` puts the database at
  `<AppData>/Lighthouse/LighthouseAppContext.db` and the key store at
  `<AppData>/Lighthouse/data-protection-keys` — same directory, already beside the database.
- **The container does not.** `ContentRootPath` is `/app`, so the store is `/app/data-protection-keys`,
  which lives in the container's writable layer. The documented Docker setup mounts a volume at
  `/app/Data` and puts the database there. Today that costs nothing: the encryption key ships inside the
  image, so a recreated container reads the same one and only the auth-cookie ring and the OAuth state
  secret regenerate, both of which are harmless.
- **After D4 it inverts.** `docker rm` and recreate would hand the operator their database beside a
  brand-new key, and every stored secret unreadable. That failure is worse than the one the epic fixes
  and it lands on people who changed no setting.

There is a third case the DISCUSS wave did not separate out, and it decides the shape of the answer:
**Postgres deployments have no database file at all.** "Beside the database" is undefined for them, and
they are precisely the deployments (Kubernetes, and Docker-with-external-Postgres) where the app pod's
filesystem is least likely to be durable.

## Decision

**One resolver, `ResolveKeyStoreDirectory(builder)`, used by the encryption ring, the OAuth state
secret and the Data Protection ring alike, with four ordered cases:**

1. `Encryption:KeyStorePath` explicitly configured → that directory. The escape hatch, and the answer
   for any deployment the rules below cannot reason about.
2. `Lighthouse:DataProtection:KeyStorePath` explicitly configured → that directory. This is what
   standalone already sets, so **standalone changes by nothing** and AC-5.7's byte-unchanged sibling
   holds for slice 02 too.
3. Provider is SQLite **and** the connection string's `DataSource` resolves to an absolute path with a
   directory → `<that directory>/keys`. This is the container case, and it is what makes AC-2.10 and
   AC-2.11 true: an operator who already mounted `/app/Data` keeps their key by doing nothing, because
   the key follows the database onto the volume they already have.
4. Otherwise — Postgres, or SQLite with a bare relative filename → `ContentRootPath/data-protection-keys`,
   today's behaviour, **and minting is refused**. See below.

**Three stores, one directory, deliberately.** The encryption ring file is wrapped with Data Protection,
so it is only readable where the Data Protection ring is. Splitting the two directories would produce a
deployment where the encryption blob survives and the keys that unwrap it do not — a startup refusal
(D10) rather than a data loss, but a refusal nobody could diagnose. They move together or not at all.

**Migrating off the old location is a read-both, write-new rule, never a silent regeneration.** On
resolution, if the newly-resolved directory holds no ring and the legacy `ContentRootPath/data-protection-keys`
does, the legacy directory is read and its contents are copied into the new location, once, and the
startup line says so. If both hold a ring and they differ, startup **stops** and names both paths. There
is no case in which an existing key ring is ignored and a fresh one generated.

**Case 4 refuses to mint, and this is the load-bearing part.** Where the resolver cannot point at a
directory it can argue is durable, the application will not create a key it may lose. It starts, it runs
on whatever ring it *can* read — which after ADR-148 always includes the retired published default, so
every existing secret still works — and it reports custody as `NoDurableStore`. The startup line and the
encryption panel say, in one sentence, that this instance is on the published shared key and name the two
ways out (`Encryption__Key`, or `Encryption__KeyStorePath` on a mounted volume). Writes continue under the
legacy default, which is exactly today's behaviour and therefore not a regression, and the operator is
told, which is not today's behaviour and is the whole point.

**Refusing to start was considered for case 4 and rejected for existing instances.** An upgrade that
refuses to boot a working Postgres install because the *product* got better at security is a worse
outcome than a loud warning, and it would land on the same population D12 is protecting. For a **fresh**
install with an empty database the calculus reverses — nothing is at stake and starting silently on a
public key is indefensible — so a fresh case-4 instance **does** refuse to start, with the same two
one-line remedies. The discriminator is "does this database already hold an encrypted secret?", which
is a query, not a flag.

## Alternatives Considered

**Leave the key store where it is and document a volume mount.** Zero code. **Rejected**: it converts a
silent catastrophic failure into a documentation problem, and the population that would hit it is
defined by *not having read the documentation*. D12 exists because this was considered and rejected in
DISCUSS; it is recorded here so the reasoning survives.

**Give the encryption ring its own directory, separate from Data Protection.** Cleaner separation of
concerns. **Rejected**: the ring is Data-Protection-wrapped, so the two are one unit whether the
directory structure says so or not, and separating them creates an undiagnosable startup refusal.

**Derive the store location from the database for Postgres too** — a directory named after the host and
database. **Rejected**: it is a fabricated path with no relationship to anything durable, and it would
give case 4 the *appearance* of a considered location while having exactly the properties that make
case 4 dangerous.

**Store the generated key in the database for Postgres deployments.** Solves durability perfectly.
**Rejected outright** for the reason in ADR-148: a key in the database it encrypts fails the
leaked-backup scenario this epic exists for.

**Put the key on a PVC for Kubernetes.** Would let the chart offer app-owned custody. **Rejected**: a
ReadWriteOnce PVC on the API workload contradicts the multi-replica story (ADR-075/076), a ReadWriteMany
one is not available on most clusters, and the answer for Kubernetes is a Secret the operator owns
(ADR-153) — which is better on every axis and needs no volume at all.

## Consequences

**Positive**

- The Docker failure mode D4 would otherwise have introduced never arrives, for operators who changed
  no setting, because the key follows the database onto the volume they already mounted.
- Standalone is untouched: it already resolved case 2 correctly before this epic existed.
- There is no path in which an existing key ring is silently replaced. Every ambiguity is a startup
  refusal that names both paths, and every non-durable case is a loud, actionable startup line.
- Postgres and Kubernetes are handled by being told the truth — *this instance is on the shared key* —
  rather than by a location that only looks durable.

**Negative / accepted**

- Three cases plus a migration branch is more logic than one `Path.Combine`. It is all in one function,
  and each branch corresponds to a deployment topology that actually exists.
- A Postgres install that supplies nothing keeps writing under the published default until the operator
  acts. That is not a regression — it is today, made visible — but it means KPI-1 is not automatically
  satisfied for that population by slice 02 alone. The release notes and the panel are what close it.
- A fresh case-4 install refuses to start, which is a new failure for someone hand-rolling a Postgres
  deployment. Two one-line remedies are in the message, and the chart (ADR-153) always supplies a key,
  so no chart user meets it.

## Earned Trust — the substrate lies, and the probe exercises the lie

| Substrate lie | Probe |
|---|---|
| A container filesystem accepts a write and loses it on recreate | Gold test: write the ring, recreate the container against the same mounted data directory, read the same key id (AC-2.11) — the only test that actually proves D12 |
| Docker overlayfs and WSL2 DrvFs no-op `fsync` | The bootstrapper re-reads and unprotects the ring file after writing; a mismatch fails startup rather than being assumed |
| The old key store is empty when it is not | Test: legacy directory populated, new directory empty → contents migrated and the startup line names both; both populated and different → startup stops |
| SQLite `DataSource` parses the same on Windows, Linux and macOS | Table test over relative, absolute, `:memory:`, and a UNC-style path — `:memory:` and relative land in case 4, absolute in case 3 |
| A fresh instance can be told apart from an upgraded one | Test: empty database → refuse; database holding one encrypted option → start with the warning |

## Cross-reference

- [ADR-148](./adr-148-key-ring-canonical-form-and-retired-default.md) — what the file contains.
- [ADR-150](./adr-150-key-ring-resolved-at-builder-time-into-a-singleton.md) — when this resolver runs
  relative to `builder.Build()`.
- [ADR-153](./adr-153-kubernetes-key-custody-is-operator-supplied.md) — why Kubernetes never reaches
  case 3 or case 4.

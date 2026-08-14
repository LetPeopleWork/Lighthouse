# ADR-150: The ring is resolved at builder time into a singleton holder, and never into `IConfiguration`

**Status**: Accepted
**Date**: 2026-08-14
**Feature**: `epic-5775-secret-encryption-key-custody` (ADO Epic #5775, slice 02 / Story #5024)
**Decider**: Morgan (Solution Architect), DESIGN application layer, interaction mode = PROPOSE
**Implements**: D4, D9, D10 · AC-2.2, AC-2.7, AC-2.8, AC-2.9

---

## Context

D4 says standalone custody copies `EnsureOAuthStateSecret` "verbatim in shape". That method
(`Program.cs:429-446`) resolves a secret before `builder.Build()`, using a transient mini-host that
pins the Data Protection ring to the same on-disk directory every boot will use, and then **writes the
resolved value into `builder.Configuration` via `AddInMemoryCollection`**.

The builder-time part is right and this ADR keeps it. Three requirements force it:

- `ICryptoService` is a singleton (`Program.cs:1162` neighbourhood) injected into `LighthouseAppContext`,
  which is constructed per scope. A ring resolved lazily on first use would fail at the first save of a
  credential rather than at startup.
- D10 requires a key store that exists and cannot be read to **stop startup**. There is no "stop
  startup" after the host is running; there is only a crash on the first request.
- AC-2.8 requires a startup line naming the key source, and AC-2.10 requires it to name the resolved
  path. Both are printed by `PrintSystemInfo`, which runs at `Program.cs:121`/`:136`.

The configuration part is wrong, and copying it verbatim would be the mistake. `IConfiguration` is a
global, enumerable, string-keyed dictionary. Anything in it is reachable from
`IConfigurationRoot.GetDebugView()`, from any future diagnostics endpoint, and from any code that
enumerates a section. Putting 32-byte encryption keys there makes "no key material anywhere" a promise
held by nobody having written the wrong `foreach`. The OAuth state secret already sits there; that is an
existing exposure this epic should not widen just because it has precedent.

There is also an ordering problem D12 introduces. The key store directory now depends on the database
path, so resolution has to happen after whatever sets the database path.

## Decision

**A new `EnsureEncryptionKeyRing(builder)` runs at builder time, in a fixed position, and registers a
singleton holder rather than a configuration entry.**

Bootstrap order in `Program.Main`, stated because it is now load-bearing:

1. `StandaloneInitializer.InitializePaths(builder)` — sets `Database:ConnectionString` and
   `Lighthouse:DataProtection:KeyStorePath` on the standalone profile. Must stay first: it is what makes
   case 2 of ADR-149's resolver true.
2. `ResolveKeyStoreDirectory(builder)` — ADR-149's four cases. Reads configuration only; creates the
   directory.
3. `EnsureOAuthStateSecret(builder)` — unchanged behaviour, now consuming (2) instead of resolving its
   own directory.
4. **`EnsureEncryptionKeyRing(builder)`** — new. Same transient-mini-host idiom as (3), same directory,
   same Data Protection pinning. Produces an `EncryptionKeyRing` value object and calls
   `builder.Services.AddSingleton<IEncryptionKeyRingHolder>(new EncryptionKeyRingHolder(ring))`.
5. `ConfigureDataProtection(builder)` — unchanged except that it consumes (2).
6. `builder.Build()`.

**No key material enters `IConfiguration`.** `Encryption:Key` / `Encryption:Keys` / `Encryption:KeysFile`
are *read* from configuration when the operator supplies them — there is no way around that, it is how
an operator supplies a value — but nothing resolved is ever written back, and the generated ring never
appears there at all.

**The holder is a mutable snapshot, not an immutable injection.** Rotation (ADR-151) and an
operator-added key in a mounted Secret (ADR-153) both have to change the ring while the process runs.

```csharp
public interface IEncryptionKeyRingHolder
{
    EncryptionKeyRing Current { get; }        // volatile read of an immutable snapshot
    void Replace(EncryptionKeyRing ring);      // atomic reference swap
}
```

`EncryptionKeyRing` itself is immutable, so a reader that captured `Current` at the top of a decrypt
sees a consistent ring for the whole operation even if a rotation swaps it mid-flight. `CryptoService`
takes the holder, not the ring — which is what lets rotation work without restarting the process and
without any component holding a stale key set.

**`CryptoService` stops reading `IConfiguration`.** Its constructor takes `IEncryptionKeyRingHolder`.
The `configuration["EncryptionSettings:EncryptionKey"]` lookup at `CryptoService.cs:12` is deleted, and
with it the entire class of "which configuration key is the real one?" defects that Bug #5776 is fixing.
There is exactly one place that decides what a key is, and it runs once, at startup, before anything can
have used the wrong one.

**`ISystemInfoService` does not carry key state.** `SystemInfoController.GetSystemInfo` is `[Authorize]`
only, and after ADR-137 that includes any viewer who reaches an embedded frame. Key source, active key
id and the resolved key store path are **instance security posture** and belong behind
`[RbacGuard(SystemAdmin)]` on the encryption endpoint (ADR-152). The Settings → System page renders them
by calling that endpoint. This is a correction to AC-2.8 as written, which named System Info as the
surface; the *user-visible* outcome is unchanged and the audience is narrowed to the people the AC
actually meant.

**The startup line carries source, active key id and resolved path, and never material** — one line in
`PrintSystemInfo`, e.g. `Encryption   : generated for this instance (k-2026-08-14-01) · /app/Data/keys`.

## Alternatives Considered

**Copy `EnsureOAuthStateSecret` literally, including `AddInMemoryCollection`.** Maximum consistency with
an idiom that works, and D4 asked for it. **Rejected**: it puts the thing this epic exists to protect
into the one store in the process designed to be enumerated and printed. Consistency with a pattern is
not a reason to inherit its weakest property, and the divergence costs one line
(`AddSingleton` instead of `AddInMemoryCollection`). Making the OAuth state secret follow *this* shape
instead is worth doing and is explicitly **not** in this epic's scope.

**Resolve the ring lazily inside `CryptoService`'s factory.** No builder-time work, no ordering
question, and DI handles it. **Rejected**: the failure surfaces at the first credential save rather than
at startup, which loses D10 and AC-2.7 outright, and it means the startup line cannot report a key
source because nothing has resolved one yet.

**A hosted service that resolves the ring on start.** `IHostedService.StartAsync` can stop the host, so
D10 survives. **Rejected**: hosted services start *after* the container is built and after the first
requests can be served on some hosting models, so there is a window in which a scoped
`LighthouseAppContext` can be constructed with a `CryptoService` that has no ring. A window that narrow
is worse than no window, because it is the kind that is never reproduced in a test.

**Inject `EncryptionKeyRing` directly and rebuild the container on rotation.** Immutable all the way
down, which is otherwise the right instinct. **Rejected**: rebuilding the DI container of a running
ASP.NET Core application is not a supported operation, and the alternative — restarting the process
after a rotation — makes a routine administrative action an outage.

## Consequences

**Positive**

- A key problem is a startup failure with a message, which is the only kind an operator can act on
  before it costs them anything.
- No encryption key material exists in `IConfiguration`, so "no key material in a debug view, a config
  dump or a diagnostics endpoint" is a structural property rather than a review habit.
- One resolution point means Bug #5776's class of defect — documented setting, different setting read —
  cannot recur: there is one function that decides, and its output is the startup line.
- Rotation and hot-reload both become a reference swap, with readers holding consistent snapshots.

**Negative / accepted**

- Builder-time work with a transient mini-host is unusual and needs a comment for a stranger. The
  existing one at `Program.cs:461-464` is the model, and the same explanation applies.
- `Program.Main`'s bootstrap order is now load-bearing in a way a reader could break by reordering two
  lines. Enforced by test rather than by comment — see below.
- The holder is mutable state in a singleton. Bounded to one reference swap of an immutable object,
  which is the smallest mutable thing that can express "the ring changed".

## Earned Trust — what is probed, not assumed

| Assumption | Probe |
|---|---|
| Bootstrap order is what the design says | Integration test: a `WebApplicationFactory` boot with `Lighthouse:DataProtection:KeyStorePath` unset and a SQLite path set → the ring lands beside the database. Reordering (1) and (2) fails it |
| No key material reaches `IConfiguration` | Structural test walking `IConfigurationRoot.GetDebugView()` after boot and asserting no value decodes to 32 bytes matching a ring entry |
| A key store that exists but cannot be read stops startup | Gold test: corrupt `encryption-keyring.protected` → boot raises, message names the file, **no replacement file is written** (the assertion that matters) |
| A restart is not a rotation | Test: boot, write a secret, restart, read it → same key id, same plaintext (AC-2.2) |
| Key state is not readable by a non-admin | Integration test: a viewer principal calling the encryption endpoint gets 403; `GET systeminfo` contains no key field |
| A ring swap mid-decrypt does not tear | Concurrency test: 1 000 decrypts racing 100 `Replace` calls → zero failures, because each read captures a snapshot |

## Cross-reference

- [ADR-149](./adr-149-key-store-beside-the-database.md) — step 2 of the order above.
- [ADR-148](./adr-148-key-ring-canonical-form-and-retired-default.md) — what step 4 parses.
- [ADR-152](./adr-152-custody-mode-and-the-encryption-admin-surface.md) — the guarded endpoint that
  replaces System Info as the key-state surface.
- `Program.cs:429-486` — the idiom this extends, and the one property of it deliberately not copied.

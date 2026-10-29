# The shared kernel stops reaching into Services, and repository leaks can only shrink — Stories 6367 + 6369

Delivered 2026-10-27 as one batch of two G0 items from Epic #6264 (path to a modular monolith): S04a, ADO #6367
("Close the `Models ↛ Services` gap") and S05a, ADO #6369 ("Ratchet guard: `GetAllByPredicate` callers and
controllers on repositories"). Pushed to `main` in `e3ac351d4..93139eca0`, CI 40563773352 green. Both items are
Closed; the Epic's state is left to the maintainer. Behaviour-free, so there are no release notes.

Before this batch, `Models` (the shared kernel) still depended on eight types in `Services`, and `ARCHITECTURE.md`
carried a note admitting the gap. Nothing stopped a new service from calling the catch-all `GetAllByPredicate`, or a
new controller from taking a repository directly, so both leaks could grow without anyone noticing.

Now three architecture tests guard these boundaries, each with an inline list of the files or types it allows:

| Rule | Allowed today | Fails when |
| --- | --- | --- |
| `Models` does not depend on `Services` | nothing (empty list) | any `Models` type depends on `Services` |
| `GetAllByPredicate` is called only from the listed files | 15 files | a new file calls it (named as `file:line`), or a listed file stops calling it |
| The `API` depends on a repository only in the listed types | 23 types (20 controllers + 3 helpers) | a new `API` type takes a repository, or a listed one stops |

A test that fails names the file or type, so each list can only shrink. The later S05b–S05g, S12* and S04b items
remove entries from these lists.

## What shipped

### S04a (#6367) — `Models ↛ Services`

| Step | What it changed |
| --- | --- |
| 01-01 | `IEntity` and `IConcurrencyTokenEntity` move into `Models` (`d39e4bf33`), with the stale `Services` usings dropped (`0e14adf9e`). |
| 01-02 | `WorkTrackingSystems` moves into `Models` (`93c00f05d`). Its new path stays in the test-selection classifier's shared-connector set, so a change to the enum still runs the live-connector suites (`443e8f1a4`, `7d17fbbd5`). |
| 01-03 | `ReportOwner`/`ReportOwnerKind` (`ff78469e6`) and `BlackoutDaysExtensions` (`d66e6d25f`) move into `Models`. |
| 01-04 | `UpdateType` moves into `Models` (`bb0883096`), and the update-queue rule loses its last exception (`7a1602c06`). |
| 01-05 | `WithLegacyDefault` becomes a `Services` extension, so `Models` no longer reaches the encryption service; call syntax unchanged (`692d6f973`). |
| 01-06 | The `Models ↛ Services` rule lands with an empty exception list (`af69c31a0`), and the `ARCHITECTURE.md` §4 gap note goes (`4a3d55367`). |

### S05a (#6369) — repository ratchets

| Step | What it changed |
| --- | --- |
| 02-01 | `ProductionSource` and a two-way baseline comparison that names a newcomer and a stale entry separately (`af8325179`). |
| 02-02 | The call-site scan reports `GetAllByPredicate` calls as `file:line` and ignores comments, strings and the declaration (`ac35f245a`). |
| 02-03 | The `GetAllByPredicate` ratchet with its file list (`06641a5ba`). |
| 02-04 | Spike on how ArchUnitNET reports compiler-made types; `DeclaringTypeOf` maps nested types to the type that wrote them (`1035851cf`). |
| 02-05 | The `API` repository ratchet with its 23 types (`5e3a25dc1`), tidied in `86396cbfa`. |

Follow-ups in the same range: the kernel rules also read the compiled IL (`f7d26e892`, documented in `21480424b`);
the mutation kill round pinned unguarded boundaries (`11a36ad6c`, results `f7e321de3`); and the ratchets landed on
top of the Reports work from Epic 6350 (`93139eca0`).

## Key decisions

- **Seven types move into `Models` and one dependency is reversed, instead of keeping them as listed exceptions.**
  The brief named two types; the code had eight. `LegacyDefaultEncryptionKey` needs the classifier, envelope and
  ring holder, all in `Services`, so it does not belong in the kernel. The dependency was reversed instead:
  `WithLegacyDefault` became a `Services` extension method.
- **The `API` repository rule covers the whole `API` namespace, not only controllers** (maintainer, 2026-10-27).
  A helper that takes a repository leaks just as much as a controller does, which makes 23 entries rather than 20.
- **Repository types are matched by name and by the repositories implementation namespace, not by
  `Services.Interfaces.Repositories`.** That namespace also holds service contracts such as `IFeatureListReader`
  and `IUserProfileLookup`.
- **Dependency rules read the compiled IL with Mono.Cecil** (already a dependency of ArchUnitNET). The spike
  showed that in a Release build, which is what CI tests, ArchUnitNET drops a dependency written only inside an
  `async` body. A ratchet that only works in Debug would let a newcomer through in CI. The design's fallback, a
  plain text scan of the source, was also rejected: it cannot tell which type wrote the code without tracking
  braces, and it breaks on interpolated strings.
- **ADR-268** records the convention: a ratchet lists its offenders by name, checks both directions, and only
  ever shrinks. ADR-027 got a status note.

## Measures

| KPI (Epic table) | Before | After |
| --- | --- | --- |
| 3 — `Models` file count | 246 | 252 (+6 kernel types; S04b brings it down) |
| 4 — files calling `GetAllByPredicate` | 14 (unguarded) | 15, guarded (`ReportOwnerLookup` from Epic 6350 arrived at landing) |
| 5 — `API` types depending on a repository | ≈20 (unguarded) | 23, guarded |
| 12 — mutation score, touched code | — | 84.95 % full run, 91.30 % after the kill round |

## Lessons

- **Re-count before designing.** The brief's two-type gap was eight types, and its "controllers" were 23 `API`
  types. Both changes came from re-counting at DESIGN.
- **Test the architecture in the configuration CI runs.** A clean Debug run hid the async gap entirely. ADR-268 records why the rules read the IL.
- **Ratchets meet in-flight work.** A parallel Epic added a `GetAllByPredicate` caller between DESIGN and landing.
  The list grew by one, on purpose and by name, rather than the new caller being silently allowed.
- **Close-out happened later.** The batch was picked up before `/continuous-improvement` existed, so the
  workspace was archived and the Epic's Improvement log was started on 2026-10-29.

## Not done here

- Moving the five private source-scan helpers to `ProductionSource` was left out of scope (DESIGN D8). It is
  still open.

## Artifacts

- Workspace (delivery history): `docs/feature/stories-6367-6369-kernel-and-repository-ratchets/`
- ADR: `docs/product/architecture/adr-268-a-ratchet-guard-lists-its-offenders-by-name-in-both-directions-and-only-ever-shrinks.md`
- Tests: `Lighthouse.Backend/Lighthouse.Backend.Tests/Architecture/RepositoryLeakRatchetArchUnitTest.cs`,
  `ModuleBoundariesArchUnitTest`
- Mutation: `docs/feature/stories-6367-6369-kernel-and-repository-ratchets/mutation/results.md`

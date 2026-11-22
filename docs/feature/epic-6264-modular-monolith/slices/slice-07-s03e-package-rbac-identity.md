# Slice 07 — S03e Package RBAC / Identity

Epic #6264 · `@infrastructure` · job_id: infrastructure-only · **Size M**

## Goal

Move authorization, RBAC administration, authentication, API keys, embed sessions and licensing into the RBAC/Identity context folder. Authorization behaviour is unchanged.

## IN

- `Services/*/Auth/**`, `Authorization/**`, `Licensing/**`.
- `Models/Auth/**`, `Models/Authorization/**`.
- `RbacGuardAttribute`, `LicenseGuardAttribute`.
- Namespace ArchUnit rule. The existing RBAC rules are re-pointed in the same PR.

## OUT

- Any change to roles, guards or `IRbacAdministrationService`'s surface.

## Hypothesis

RBAC can be packaged without touching a single authorization decision. **Disproved if** a guard's or service's public surface has to change.

## Acceptance criteria

- [x] `IRbacAdministrationService` remains the single entry for RBAC business logic, and its public surface is unchanged (pinned by `AccessApplicationLayoutArchUnitTest`).
- [x] `RbacGuard` on every action unchanged (`AccessHttpAdaptersLayoutArchUnitTest` pins the guarded-action count and the attributes' shape). RBAC ArchUnit rules green in the new namespaces.
- [x] Frontend untouched (`useRbac` unchanged); the end-to-end project changed only where it built the old licence file path.
- [x] No file of Access or Licensing remains in `Services/Interfaces` or `Services/Implementation`, checked by `AccessAndLicensingUnitTestPlacementTest` with no named exception (the repositories and seeders were never in the move). ARCHITECTURE.md §4 updated.
- [ ] Behaviour-free (all authorization tests green unmodified), mutation score held. Tests green apart from moves, `using` lines and rule files: done. Mutation: pending, see the evolution record.

## Dependencies

- Hard: S01, S02 gate.

## Notes for DESIGN

- Highest-stakes move. Run the full authorization and API-key integration suites.
- The emergency-admin config key and the auth-off "one subject" behaviour must not change.

## Slicing

Carpaccio slicing into ≤ 1-day slices happens at this Story's DESIGN.

## Update after S01 (2026-11-01, ADR-270 accepted)

Retitled **Package Access and Licensing**: two small generic contexts in one Story, two folders. Licensing comes from RBAC/Identity today (`Services.Implementation.Licensing`). Ratchet candidate: the updaters resolve `ILicenseService` directly (`TeamUpdater.cs:31`, `PortfolioUpdater.cs:65-68`, `ForecastUpdater.cs:292`).

## Update after S03e (#6363, 2026-11-22)

Delivered in `Modules/Access` (32 files: `Domain` 8, `Application` 16, `Adapters/Http` 8) and `Modules/Licensing` (4 files and the
public key in `Application`, 1 in `Adapters/Http`); record:
`docs/evolution/2026-11-22-story-6363-s03e-package-access-licensing/evolution.md`. Size M. The ASP.NET guards, markers and
authentication handlers live in a new `Adapters/Http` folder of each module, not in `Application` and not in `API`. Four
`Models` value types moved into Access's `Domain`; entities, controllers, DTOs, the six repositories and the two seeders
stayed. Notes for later Stories:

- **Repositories by role** (Epic 6507): the six repositories stay with the platform, and three Access services use the database context directly (21 listed edges in R-A6).
- **Epic 6474 and #6657**: the 11 outside types that ask `ILicenseService` are listed in R-L3 (ratchet candidate 2).
- **S03g** (Refinement): `VoterIdentityResolver` stays with Refinement and is one of Access's 5 listed callers.
- **S09a**: the two seeders and the `Program.cs` registrations stay; no `AddAccess` or `AddLicensing`.

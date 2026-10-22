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

- [ ] `IRbacAdministrationService` remains the single entry for RBAC business logic, and its public surface is unchanged.
- [ ] `RbacGuard` on every action unchanged. RBAC ArchUnit rules green in the new namespaces.
- [ ] Frontend untouched (`useRbac` unchanged).
- [ ] No file of this context remains in `Services/Interfaces` or `Services/Implementation`. ARCHITECTURE.md §4 updated.
- [ ] Behaviour-free (all authorization tests green unmodified), mutation score held.

## Dependencies

- Hard: S01, S02 gate.

## Notes for DESIGN

- Highest-stakes move. Run the full authorization and API-key integration suites.
- The emergency-admin config key and the auth-off "one subject" behaviour must not change.

## Slicing

Carpaccio slicing into ≤ 1-day slices happens at this Story's DESIGN.

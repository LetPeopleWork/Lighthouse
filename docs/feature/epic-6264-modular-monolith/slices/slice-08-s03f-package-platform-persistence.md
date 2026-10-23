# Slice 08 — S03f Package Platform / Persistence

Epic #6264 · `@infrastructure` · job_id: infrastructure-only · **Size L**

## Goal

Move the platform pieces into the place S01 decided: either a Platform/Persistence context, or infrastructure under the composition root. The pieces are `LighthouseAppContext`, repositories, DB management, the domain-event dispatcher, seeding, OAuth token store, encryption and key custody, usage data, and logging.

## IN

- `Data`, `Services/*/Repositories/**`, `DatabaseManagement/**`, `DomainEvents/DomainEventDispatcher` (the handlers go to their owning contexts), `Seeding/**`, `Encryption/**`, `UsageData/**`, `Logging/**`, the platform services among the loose files (`AppSettingService`, `PlatformService`, `GitHubService`, `LighthouseReleaseService`, `SystemInfoService`, `FileSystemService`, `ProcessService`, `CryptoService`, …).
- Namespace rule, or the composition-root exemption, per S01.

## OUT

- Entity configuration per context and table ownership (S10). Persistence ports per aggregate (S05c–e).

## Hypothesis

Platform code can leave the layer folders without touching EF migrations. **Disproved if** moving `LighthouseAppContext` or the configurations forces migration-assembly changes. In that case stop at the namespace move and leave the rest to S10.

## Acceptance criteria

- [ ] No platform file remains in `Services/Interfaces` or `Services/Implementation`.
- [ ] Migrations untouched. Both migration assemblies build (they are `HintPath` references, so build first).
- [ ] Domain-event handlers live with the context that reacts. The dispatcher lives here.
- [ ] ARCHITECTURE.md §4 / §8 updated. Behaviour-free, tests green.

## Dependencies

- Hard: S01 (module vs composition root), S02 gate.
- Soft: most other S03* first, since platform is what they all lean on.

## Notes for DESIGN

- Read the key-ring and dev-server notes in CLAUDE.md before touching `Encryption`. Never point a key store at `data-protection-keys`.
- `Program.cs` edits force the full Integration suite.

## Slicing

Carpaccio slicing into ≤ 1-day slices happens at this Story's DESIGN.

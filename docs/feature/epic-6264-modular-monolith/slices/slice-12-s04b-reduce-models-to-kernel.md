# Slice 12 — S04b Reduce `Models` to the shared kernel + kernel-size rule

Epic #6264 · `@infrastructure` · job_id: infrastructure-only · **Size M** · Pick-up group G4 (end state)

## Goal

`Models` holds only what S01's ADR allows in the shared kernel (shared IDs, value objects, domain-event base types), and a rule keeps it that way.

## IN

- Move every remaining context-specific type out of `Models` (242 files on 2026-10-22) to its owning context.
- Add an ArchUnit kernel rule, e.g. an allow-list, or "no type in `Models` is used by only one context".

## OUT

- Contracts namespaces (S09a).

## Hypothesis

After S03*, the residue in `Models` is small and clearly shared. **Disproved if** many types are shared by exactly two contexts. That would signal a missing context or a contract, which goes back to S01 and S09a.

## Acceptance criteria

- [ ] Every type in `Models` is on the ADR's allowed list.
- [ ] Kernel rule green, and it fails naming any context-specific type added to `Models`.
- [ ] ARCHITECTURE.md §4 "Models — the shared kernel" text updated. Behaviour-free.

## Dependencies

- Hard: all S03*, S04a.

## Notes for DESIGN

Decide whether domain-event records stay in the kernel or move to each publisher's `Contracts`. Coordinate with S09a.

## Slicing

Carpaccio slicing into ≤ 1-day slices happens at this Story's DESIGN.

## Update after S03b (#6360, 2026-11-16)

S03b moved no `Models` type. The Work Items types still in `Models` (the entities `WorkItem`, `Feature`, `WorkItemBase`, `FeatureWork`, the transition and blocked-transition tables, `RefreshLog`, `FeatureDependencyReference`; the `WorkItemRules`, `Parent*`, sync and staleness types) land on this Story's per-type list: kernel, or Work Items. Entities move with S12c/S13, not here. Work Items' R-W6 list holds 6 entries instead of 4 only because `IAppSettingService` and `ILighthouseClock` share the top-level `Services` namespaces with Organisation set-up's `IFeatureOrdering`; once Organisation set-up has its own namespace, those two entries leave the list.

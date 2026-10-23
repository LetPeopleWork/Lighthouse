# Slice 20 — S06 Connector capability interfaces

Epic #6264 · `@infrastructure` · job_id: infrastructure-only · **Size L**

## Goal

Split the 15-member `IWorkTrackingConnector`, the anti-corruption layer's port, into capability interfaces, so that no connector throws `NotSupportedException` for a member it claims to implement.

## IN

- Capability interfaces, e.g. `IWorkItemSource`, `IIncrementalSource` (sweep + by-id fetch), `IWriteBackTarget`, `IConnectionValidator`. Final names per S01.
- Callers check capability by type (`connector is IIncrementalSource`) instead of `Supports*` flags.
- Baseline, verified 2026-10-22: **20 member throws**. The brief said CSV 5.
  - CSV 6: two sweeps, two by-id fetches, parent sweep, write-back.
  - Linear 6: sweeps and by-id fetches pending Epic #5687, write-back.
  - ServiceNow 8: sweeps and fetches pending Epic #5687, write-back.
- Jira Cloud vs Data Center (ADR-139) stays a genuine runtime capability (`DeploymentNotKnownYet`). Document how it maps onto the interfaces.
- **Backend naming drift**: rename `GetFeaturesForProject(Portfolio project)` per the S01 glossary. It is internal only, with no wire change.

## OUT

- Implementing missing capabilities (that is Epic #5687's work). A connector that cannot sweep simply does not implement `IIncrementalSource`.

## Hypothesis

Capabilities are static per connector type, except for Jira's deployment. **Disproved if** more connectors turn out to need runtime capability. Then extend the ADR-139 runtime mapping rather than the type split.

## Acceptance criteria

- [ ] `grep -rn "throw new NotSupportedException" …/WorkTrackingConnectors/{Csv,Linear,ServiceNow}` finds no throw from a member of an implemented interface.
- [ ] `Supports*` flags removed or reduced to the documented runtime case.
- [ ] ADR-139 or a new ADR documents the Cloud/DC mapping.
- [ ] Behaviour-free: same sync paths chosen per connector, and connector tests green (run the live categories deliberately).

## Dependencies

- Soft: S01. Ideally before S03a.

## Notes for DESIGN

- Coordinate with Epic #5687's open slices for Linear and ServiceNow sweeps. When they land, they add an interface rather than delete a throw.
- Watch the shared Linear key's rate limit.

## Slicing

Carpaccio slicing into ≤ 1-day slices happens at this Story's DESIGN (one capability per slice).

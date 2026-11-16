# Feature: story-6376-s06-connector-capability-interfaces

Story #6376 · S06 Connector capability interfaces · Epic #6508 (Thin services and controllers)

- Catalogue: [`docs/feature/epic-6264-modular-monolith/feature-delta.md`](../epic-6264-modular-monolith/feature-delta.md), Stories → S06
- Brief: [`slices/slice-20-s06-connector-capability-interfaces.md`](../epic-6264-modular-monolith/slices/slice-20-s06-connector-capability-interfaces.md)
- ADRs: [ADR-270](../../product/architecture/adr-270-bounded-contexts-context-map-and-module-layout.md) (Tracker integration is the ACL; its glossary), [ADR-139](../../product/architecture/adr-139-incremental-sync-capability-probe-on-connector-port.md) (incremental-sync probe on the connector port; Jira Cloud vs Data Center)
- Why now: the maintainer put S06 before S03a #6359 on 2026-11-16, so S03a packages the final connector port once. Order: S06 → S03a, S03d, S03e, S03f, S03g, S03h, S03i → S09a → Epic #6474 (see the end of [`epic-6474-one-structure/feature-delta.md`](../epic-6474-one-structure/feature-delta.md)).

DISCUSS: covered at Epic level (see the Epic's Wave Decisions); waves start at DESIGN.

## Wave: DESIGN / [REF] Inherited

Linked, not copied. The catalogue is the single source of truth.

- Catalogue: Story Map (G2: "can run before or alongside packaging"), How to pick up a Story, Stories → S06 and S03a,
  Outcome KPI 7 (connector `NotSupportedException` throws, target 0), Wave Decision D-6264-10 (the
  `GetFeaturesForProject` rename belongs here), Cross-Cutting Impact Checklist, Improvement log.
- ADR-270: Tracker integration context, its glossary (Connection, Translation, Read refusal), the edge "Tracker
  integration → Work Items: the connector port is the negotiated contract".
- ADR-139: incremental sync as a per-connection runtime probe (`SupportsIncrementalSync`), Jira's
  `DeploymentNotKnownYet`.
- ADR-268 (ratchet convention: inline file lists, never loosened).
- S03b evolution `docs/evolution/2026-11-16-story-6360-s03b-package-work-items/` (the connector leaf rule; the source
  selectors stay with Tracker integration).

## Wave: DESIGN / [REF] DoR re-run (2026-11-16)

| DoR item | Status | Evidence |
|---|---|---|
| 1 Problem statement | PASS | `IWorkTrackingConnector` has 16 members; CSV, Linear and ServiceNow throw `NotSupportedException` from 20 of them; callers ask `Supports*` flags instead of the type. |
| 2 Persona | PASS | `lighthouse-maintainer` plus the CI build; `@infrastructure`. |
| 3 Domain examples | PASS | Real throws and flags below. |
| 4 UAT | PASS (catalogue) | Four AC in the brief, CI-observable. |
| 5 AC from UAT | PASS | AC map to the Elevator Pitch "After" (`grep` finds no throw from an implemented member; callers test `connector is …`). |
| 6 Right-sized | TO CONFIRM at DESIGN | Brief says L; the slicing ("one capability per slice") decides whether it fits 5 nwdes slices. |
| 7 Technical notes | PASS | Re-answered below. |
| 8 Dependencies | PASS | Soft: S01 Closed. No hard dependency. Epic #5687 (Faster Updates) is **Closed**, so the brief's "coordinate with its open slices" no longer applies (see re-count). |
| 9 Outcome KPIs | PASS | KPI 7. |

## Wave: DESIGN / [REF] Baseline re-count (2026-11-16, `main` at `487be05c4`)

| Measure | Catalogue / brief | Today |
|---|---|---|
| `IWorkTrackingConnector` members | 15 | **16** (2 `Supports*` probes, `GetPredefinedAdditionalFields`, 2 `GetWorkItemsForTeam`, `SweepWorkItemsForTeam`, 2 `GetFeaturesForProject`, `SweepFeaturesForPortfolio`, `GetParentFeaturesDetails`, `SweepParentFeatures`, 3 `Validate*`, `WriteFieldsToWorkItems`) |
| Member throws, CSV / Linear / ServiceNow (KPI 7) | 20 (6 / 6 / 8) | **20 (6 / 6 / 8)** |
| Other `NotSupportedException` in the connector folder | — | 4, all genuine: Jira `DeploymentNotKnownYet` (ADR-139 runtime case), one in Azure DevOps (`AzureDevOpsWorkTrackingConnector.cs:1804`, to classify), `AuthenticationMethodKeys`, `AuthenticationMethodSchema` (no auth method for an unknown system) |
| `Supports*` on the connector port | — | `SupportsTransitionHistory`, `SupportsIncrementalSync`; one caller each outside the connectors: `Modules/WorkItems/Application/WorkItemService.cs` |
| Other `Supports*` flags | — | `SupportsAdditionalFields`, `SupportsWriteBackMappings` (API helpers and the two connection controllers), `SupportsDeliveryForecastPublishing` (`IDeliveryForecastPublisher`, Jira, `DeliveryForecastPublishingService`) |
| Capability interfaces that exist already | — | `IDeliveryForecastPublisher`, `IDeliverySourceProvider`, `IBoardInformationProvider`, plus per-system `IJira…`, `IAzureDevOps…`, `ILinear…`, `IServiceNow…WorkTrackingConnector` |
| Files referencing `IWorkTrackingConnector` | — | 9 production, 40 test |
| `GetFeaturesForProject` references | — | 19 |
| Epic #5687 | open slices | **Closed**. The Linear and ServiceNow throws still say "pending Epic #5687": that text is stale |

## Wave: DESIGN / [REF] Questions DESIGN must answer

1. **The capability set**: which interfaces, by which glossary names (ADR-270 Tracker integration), and which
   connector implements which. Reuse the existing `IDeliveryForecastPublisher` / `IBoardInformationProvider` pattern.
2. **Static vs runtime**: incremental sync is per connection (ADR-139, Jira Cloud vs Data Center). Which capabilities
   are static per type and which stay runtime probes; is the hypothesis ("only Jira's deployment is runtime") still
   true?
3. **ServiceNow's work-item reading** (`WorkItemReadingUnavailable…`, lines 760-771): a missing capability or a
   per-record-class runtime case?
4. **The other `Supports*` flags** (additional fields, write-back mappings, delivery publishing): in scope or not.
5. **The caller side**: `WorkItemService`, the write-back service, validation callers, the factory's return type.
6. **The `GetFeaturesForProject` rename** (D-6264-10) and the stale "pending Epic #5687" messages.
7. **The ratchet**: KPI 7 as a test (no `NotSupportedException` from a member of an implemented interface), by
   inline file list.
8. **Behaviour-free proof**: the same sync path is chosen per connector; the live connector categories run
   deliberately (Linear key shared with CI).
9. **What S03a then moves**: the final port, so S03a's leaf rule and S09a's contract can be written against it.
10. **Size and split** into at most 5 nwdes slices.

## Wave: DESIGN / [REF] Checklist (ticked through DELIVER)

- [ ] **Behaviour-free**: routes, DTO shapes, serialized responses, UI pixels and topologies unchanged; existing API, E2E and response tests green unmodified, apart from tests that move with their code. The same sync path per connector and connection.
- [ ] **Ratchet in the same change**: KPI 7 rule lands in the commit range that makes it pass; inline lists; no rule loosened.
- [ ] **Moves separate from edits**: renames (`GetFeaturesForProject`) in their own `refactor(connectors): rename …` commits.
- [ ] **Re-count before designing**: done above.
- [ ] **Mutation last, on frozen code**: no survivor or no-coverage mutant added in the touched code.
- [ ] **Architecture docs in the same change**: `ARCHITECTURE.md` §4 Tracker integration row; ADR-139 or a new ADR for the Cloud/DC mapping; ADR-270 glossary for new capability names.
- [ ] **One context at a time**: the connectors stay in `Services/*/WorkTrackingConnectors`; packaging is S03a.
- [ ] **Terminology**: ADR-270 glossary names; configurable terms not hard-coded.
- [ ] **Cross-cutting re-check**: RBAC, Lighthouse-Clients, Website, usage-data event, UI sketch, each answered, no silent N/A.
- [ ] **Live connector categories run deliberately** before push (connector code changes), watching the Linear rate limit.
- [ ] **The connector leaf rule holds**: no connector reaches `Modules.WorkItems`.

# Slice 01 — S01 Bounded contexts, context map and layout (ADR)

Epic #6264 · `@infrastructure` · job_id: infrastructure-only · **Size M** · docs-only · Natural DESIGN owner: **nw-ddd-architect**

## Goal

One ADR, covering both stacks, that every later Story moves towards. It frames the nine modules as bounded contexts, draws the context map, fixes the folder layout, defines the shared kernel's allowed content and the naming conventions, and gives each context a ubiquitous-language glossary.

## IN

- Confirm **nine** contexts: WorkTracking-Integration, WorkItems/Sync, Forecasting, Portfolio/Delivery, Metrics, RBAC/Identity, Platform/Persistence, Refinement, Reports. Record that the brief said eight and that Reports was added since.
- **Context map** (picture + table). Each relation is typed:
  - WorkTracking-Integration is the **anti-corruption layer** in front of Jira / ADO / Linear / CSV / ServiceNow (`WorkItemCategoryCrossing`, `*WorkItemMapper`s translate their models into WorkItem / Feature).
  - WorkItems/Sync is the customer of WorkTracking-Integration (customer/supplier).
  - Forecasting, Portfolio/Delivery, Metrics and Refinement are downstream of Sync.
  - Reports is a deliberate **conformist** of Metrics ("a report and the tab never disagree").
  - RBAC/Identity is upstream of all through guards. The OIDC IdP is an external upstream (claims parsed by a thin translation).
  - `Models` is the **shared kernel**.
  - The public HTTP API is the **published language** for the SPA and Lighthouse-Clients (CLI/MCP).
  - PostHog is an external downstream of the usage-data pipe.
- **Glossary per context**: term → meaning → code name. Flag drift:
  - `Project` vs Portfolio (`GetFeaturesForProject`, `ModifyProjectSettings`, the wire field `Feature.projects`).
  - The generic `*Service` suffix vs role names (`SyncModeResolver`, `RefinementNeedCalculator`).
  - Configurable display terms vs code names: the code uses the seeded defaults from `TerminologySeeder.cs`, and Settings → Terminology renames only what users see.
- **Layout**:
  - Backend `Modules/<Context>/{Domain, Application, Adapters}` (or `Api`/`Persistence`/`Connectors` sub-folders, or vertical slices). Interfaces co-located with their implementations.
  - Frontend feature folders (`src/modules/<context>/` or similar), with one public entry per module.
- **Shared-kernel allowed content**: shared IDs, value objects, domain-event base types.
- **Naming conventions**: use case / policy / calculator / projector / query / reader / store instead of `*Service`.
- Resolve the brief's open questions:
  - Is Platform/Persistence a module, or composition-root infrastructure?
  - Sub-folder naming inside a module.
  - Optionally, F5/F6 vs F3 ordering, which may be left to S02.

## OUT

- Moving any code (S02, S03*, FE2). Aggregate catalogue (S08). Contracts rule (S09a).

## Hypothesis

A single written layout, map and glossary lets eight later packaging Stories proceed without re-deciding. **Disproved if** S02 has to change the layout fundamentally. S02 records that as an amendment, which is expected to some degree.

## Acceptance criteria

- [ ] ADR Accepted with the nine contexts, the context map (typed edges, external upstreams and downstreams), and the layout for both stacks.
- [ ] Glossary per context, with the drift list.
- [ ] Shared-kernel allowed content and naming conventions written down.
- [ ] Open questions answered with reasons.
- [ ] ARCHITECTURE.md §4 links the ADR and says "nine bounded contexts".

## Dependencies

None. Precondition (hard) for S02, S03*, FE2. Source of truth for FE6 and for backend renames.

## Size

M. Several decisions plus a map and nine glossaries, but no code.

## Notes for DESIGN

- **Open with a Big Picture Event Storming session, run together with the maintainer** (maintainer decision, 2026-10-22). Before the session, seed a draft board from the code: existing domain events and the dispatcher's handlers, state transitions, controller actions (commands), background updaters (policies), read models, and connectors (external systems). Then walk the timeline with the maintainer section by section in chat, mark hot spots, and find the pivotal events and language shifts that suggest context boundaries. Publish the board as a visual page (timeline, swimlanes, colour-coded note types) so it can be revisited across sessions. The proposed context map and glossary come out of this session; they are not assumed from the nine modules in ARCHITECTURE.md §4.
- Run it with **nw-ddd-architect**.
- Read ARCHITECTURE.md §4–§8 and §12, ADR-027, ADR-139, ADR-163, and the Refinement and Reports module write-ups (they already follow the target shape).
- Check the context map against the legal edges in the §4 mermaid graph. Any edge the map calls illegal becomes a ratchet item.
- Keep the glossary short: the terms that actually drift, not a dictionary.

## Slicing

Carpaccio slicing into ≤ 1-day slices happens at this Story's DESIGN (e.g. map + glossary first, layout second).

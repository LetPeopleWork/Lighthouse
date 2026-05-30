# Slice 04: Enforce the seven module boundaries with ArchUnitNET

**Feature**: epic-5121-domain-events-cqrs-concurrency
**ADO child**: #5101
**Story shipped**: US-5101 (`@infrastructure`)
**Estimate**: ~1 crafter day
**ADR**: D5, D8

## Goal

Name and enforce the seven logical modules via `TngTech.ArchUnitNET` layered-dependency rules in a single assembly (no physical assembly split), building on the ArchUnitNET harness shipped by #5098. Also enforce the seam invariants (`Services.Implementation ↛ API`; dispatcher forbids `GetRequiredService`) as first-class named rules.

## IN scope

- Name the seven modules as ArchUnitNET layer definitions over their existing namespace folders: WorkTracking-Integration, WorkItems/Sync, Forecasting, Portfolio/Delivery, Metrics/Time-in-state, RBAC/Identity, Platform/Persistence.
- Layered-dependency rules per module namespace forbidding illegal cross-module dependencies.
- Seam invariants as named rules: `Services.Implementation` must not depend on `API`; the dispatcher and handler types must not use `IServiceProvider.GetRequiredService`.
- All in a SINGLE assembly (D5) — reject physical assembly split.

## OUT scope

- Concurrency tokens (#5100), work-item events (#5122), reaction migration (#5099).
- Any code restructure to SATISFY a new rule beyond what is required to make the seven modules pass — if a rule reveals a genuine existing illegal dependency, surface it as a finding; the remediation may be its own follow-up rather than bundled here (keeps this slice ≤1 day).
- Physical assembly separation (rejected by D5).

## Learning hypothesis

**Confirms if it succeeds**: the seven modules, as they exist in namespace folders today, can be expressed as ArchUnitNET layers and the codebase already (or with minimal touch-up) satisfies the layered-dependency rules — proving the modular-monolith boundaries are real and now guarded at test time, with no physical split needed.
**Disproves if it fails**: the rules reveal substantial existing illegal cross-module dependencies that cannot be resolved within the slice (e.g. Metrics reaching into Sync internals), meaning the "already a correctly-sized modular monolith" premise (ADR-027 context) is weaker than assumed and a remediation backlog is needed before the rules can go green.

## Acceptance criteria

See US-5101 in `../feature-delta.md`. Slice specifics:

- ArchUnitNET test defines all seven modules as layers and asserts no illegal cross-module dependency; the test is RED if a forbidden dependency is introduced.
- Named rule: `Services.Implementation` has no dependency on `API` (re-stated from #5098 as a first-class module rule).
- Named rule: dispatcher + handler types contain no `IServiceProvider.GetRequiredService`.
- The rule suite runs in CI as part of `dotnet test`; a deliberately-introduced violation in a throwaway test branch fails the build (validated locally before merge).

## Dependencies

**Hard**: slice 01 / #5098 (the ArchUnitNET harness). **Ordering note**: should land before or alongside slice 02 / #5099 Family A/C if those migrations would otherwise introduce cross-module Forecasting handlers — see slice 02 open question.

## Production data requirement

**Not required.** Pure test-time enforcement; no runtime surface, no production data. Dogfood: a `dotnet test` run on the project shows the seven-module rule suite green; intentionally breaking a boundary turns it red.

## Carpaccio taste tests

- **Independently shippable?** YES — ships as a precursor with test-observable value (named module rules green in CI).
- **One day or less?** YES (~1 day), provided no large pre-existing violation forces a remediation backlog (see learning hypothesis disprove branch; if so, split the remediation out).
- **End-to-end?** YES — the rules execute end-to-end in `dotnet test`.
- **`@infrastructure`?** YES — labelled `@infrastructure`; value is test/CI-observable architecture enforcement, not user-visible.

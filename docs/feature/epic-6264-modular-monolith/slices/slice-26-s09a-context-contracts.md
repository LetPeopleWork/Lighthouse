# Slice 26 — S09a Context contracts + contracts-only cross-context rule

Epic #6264 · `@infrastructure` · job_id: infrastructure-only · **Size M** · Pick-up group G4

## Goal

Each context exposes a `Contracts` namespace (facade, DTOs, events), and ArchUnit allows cross-context references only to `Contracts`.

## IN

- A `Contracts` namespace per context.
- An ArchUnit rule: a type in context A may reference context B only through `B.Contracts`. Use a ratchet baseline by inline file list if needed.
- List the direct cross-context service calls that are really reactions, and move them to domain events via the existing dispatcher (transport only, ADR-027 D7).
- Note the payoff: CI test selection by module path, extending today's path-based connector selection. A change only under one context runs that context's tests plus the contract tests and ArchUnit. A change to the kernel, Platform, `Program.cs` or the migrations runs everything. Implementing that is Story #6302's area.

## OUT

- Assemblies (S09b).

## Hypothesis

After S03*, cross-context references are few and concentrated. **Disproved if** the baseline is large. That signals a wrong context cut, which goes back to S01.

## Acceptance criteria

- [ ] Every context has `Contracts`. The contracts-only rule is green, or ratcheted with a named-file baseline.
- [ ] Reaction-style calls moved to events where listed. Behaviour-free.
- [ ] ARCHITECTURE.md §4 describes contracts.

## Dependencies

- Hard: all S03*.
- Soft: S04b.

## Slicing

Carpaccio slicing into ≤ 1-day slices happens at this Story's DESIGN (one context per slice).

## Update after S03b (#6360, 2026-11-16)

Work Items' `Program.cs` registrations stayed in place (no `AddWorkItems` extension yet; that is this Story). Order matters: the Flow metrics reaction `TeamDataRefreshedTeamMetricsUpdateHandler` must stay **first** among the `TeamDataRefreshed` handlers, because the forecast trigger and the snapshot recorders read the metrics it refreshes. `DomainEventHandlerOrderPinTest` fails on any other order, so move registrations as a block and keep cross-module handler order explicit.

## Update: no early start; all S03* first (maintainer, 2026-11-16)

The hard dependency on all S03* stands, S03i included. S09a is not cut down to the three packaged modules to start
early: it runs once, over every context. Backlog order in ADO (all Next): S06 (before S03a), S03a, S03d (Deliveries), S03e, S03f,
S03g, S03h, S03i, then S09a. Epic #6474 waits for S09a.

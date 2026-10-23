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

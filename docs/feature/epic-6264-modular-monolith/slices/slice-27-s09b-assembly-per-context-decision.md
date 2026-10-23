# Slice 27 — S09b Decision: one assembly per context?

Epic #6264 · `@infrastructure` · job_id: infrastructure-only · **Size S–M** (an ADR; may conclude "no") · Pick-up group G5 (deliberately last)

## Goal

Re-check ADR-027 D5 (one assembly because of the single-binary publish) once the codebase is ready, and record the decision.

## IN

Weigh, in an ADR:

- **For**:
  - Compiler-enforced `internal`.
  - Sound CI test selection: an unchanged context and its references cannot affect its own unit tests. This extends today's path-based connector selection.
  - Stryker runs scoped to one context and its test assembly.
  - Module test assemblies plus one integration test assembly.
- **Costs**:
  - One `DbContext` must live in a project that references all contexts.
  - The migration-DLL `HintPath` setup needs rethinking.
  - Skipped test assemblies make Sonar's overall coverage patchy.
  - Does a project-reference bundle still produce one binary in every topology (Standalone, Docker, Helm)?
- **#6302 note**: measured CI slowness comes from test bodies, not the build, so a split helps only by skipping unaffected tests.

## OUT

- Doing the split. If the answer is "yes", it gets a follow-up Story.

## Readiness preconditions (all hard)

- Contexts packaged (all S03*), contracts in place (S09a), and **no cycles between contexts**, since assemblies cannot have cycles.

## Hypothesis

ArchUnit rules are enough, and assemblies add cost for little extra enforcement. **Disproved if** the CI test-selection payoff is large and measured.

## Acceptance criteria

- [ ] ADR covering every weigh-point above, with the decision yes / no / partial.
- [ ] If yes: a follow-up Story outline that keeps the single binary and topologies unchanged.

## Slicing

Carpaccio slicing happens at this Story's DESIGN (a measurement spike, then the ADR).

# ADR-268: A ratchet guard lists its offenders by name, fails in both directions, and only ever shrinks

**Status**: Accepted (2026-10-27, DESIGN of stories 6367 and 6369, propose mode, maintainer sign-off)
**Date**: 2026-10-27
**Feature**: `docs/feature/stories-6367-6369-kernel-and-repository-ratchets/`

## Context

The path to a modular monolith (Epic 6264) removes dependencies one Story at a time. Between the
Stories, new code can reintroduce what the previous one removed, so each remaining leak needs a guard
that stops it growing and makes its removal visible. Two such leaks are guarded now: callers of
`GetAllByPredicate` outside the persistence adapters, and `API` types that depend on a repository.
Many later Stories will shrink the same kind of list.

## Decision

A ratchet guard is an architecture test with an inline list of the offenders that exist today.

- The list names the **file or type**, never a count. A count hides which offender was fixed and
  which was added.
- The guard fails **both ways**. An offender not on the list fails the build, naming it. A listed
  entry that no longer offends also fails, naming it, until the entry is deleted.
- The list is **never loosened**. Adding an entry means a port is missing; create the port instead.
- The Story that fixes an offender deletes its entry **in the same change**.
- A rule with no remaining offenders has an empty list and stays as a plain rule.

**Compiled IL or a source scan.** Answer a question about what a type depends on (constructor
parameters, fields, generic arguments, what its methods use) from the compiled IL with Mono.Cecil.
The walk includes the nested types the compiler makes for lambdas and async methods, and reports
what they use against the type that wrote them. Start from the shared helper `IlDependencies` in the
architecture tests. ArchUnitNET's dependency graph is not safe for these questions: in a Release
build, which is what CI tests, it drops what an async method uses, so a dependency written only
inside an `async` body passes unseen. Use a source scan with a file list when the question is who
calls one member, because the failure should name the file and line. Both feed the same two-way
comparison. Source scans share one internal helper that finds the production sources and strips
comments.

Comments in the lists and in the failure messages say the reason in plain words. They do not cite
internal reference codes as the explanation.

## Alternatives considered

- **A count as the baseline** (fail when the number rises). Rejected: one fix plus one new offender
  passes, and the failure names nothing.
- **A frozen-violations store** (a generated file of known violations, as the Java ArchUnit offers).
  Rejected: ArchUnitNET has none, a generated file is regenerated without review, and it hides the
  list from the person reading the test.

## Consequences

- Positive: every Story that fixes an offender shows up as a deleted line in a reviewed diff; a new
  offender names itself.
- Negative: two techniques to know. A forgotten stale entry blocks the build until deleted, which is
  the intent.
- The list is part of the test, so a rename of a listed file touches the test.

## Status notes

- 2026-10-29: the convention now covers the frontend too. ADR-269 applies it to the layer rules in
  `Lighthouse.Frontend/src`: Fallow reports the leaking imports and one enforcement test holds the list.

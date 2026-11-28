# ADR-289: Module boundaries stay in one assembly (not now)
Status: Accepted (decided by the maintainer on 2026-11-26)

**Keeps**: ADR-027 D5's decision (one production assembly, boundaries enforced by architecture tests), ADR-027 D4
(single store, single binary, no broker) and ADR-270 (contexts, `Contracts`, rules R-C1 to R-C5).
**Replaces**: the reason ADR-027 D5 gave for that decision ("a physical split complicates the single-binary
publish"). That reason does not hold. The decision stands for the reasons below.

## Context

```text
ADR-027 D5 said: enforce the module boundaries with architecture tests in a single assembly, and do not split the
backend into one assembly per module. It gave one reason: a split would complicate the single-binary publish.

Story 6383 re-checked this once the contexts were packaged (Stories 6359 to 6368) and the contexts had a Contracts
folder (Story 6382). The question: should each bounded context become its own assembly?

The numbers below are text measurements on main (git grep, git log, a scratch awk over using lines and changed
paths), taken on 2026-11-24. They are not compiler or publish runs. They miss references in the same namespace and
fully qualified names, and they match comments. They are good enough to decide "not now"; they are not good
enough to start a split. A split must re-measure from the compiled code first.

What the design found:

1. The single binary is not a blocker. The project already publishes as a single file (PublishSingleFile, self
   contained, win, linux and osx), and the two migration assemblies already travel in that one file next to the
   backend assembly. Docker runs "dotnet Lighthouse.dll" from a folder publish, where more DLLs beside it change
   nothing. Helm uses the image. So D5's stated reason is wrong. (The contents of the bundle were read from the
   project files, not from a publish run.)
2. A split is blocked by three dependency cycles. An assembly cannot depend on a package that depends on it.
   a. Between contexts: Flow metrics and Work Items depend on each other. Work Items' work item service asks Flow
      metrics' percentile calculator (listed in ADR-270's residue table); Flow metrics' handlers for blocked-count
      history call into Work Items.
   b. Between the contexts and the code that is not yet in a context: 61 module files name the old Services, API or
      Data namespaces (mostly Services.Interfaces), and 20 Services files and 57 API files name modules.
   c. Inside persistence: the database context names Services.Interfaces, so even an assembly holding only the
      database context and the entities would form a cycle. This also keeps the migration projects bound to the
      backend by committed DLL copies (a "HintPath" reference) instead of an ordinary project reference.
3. Models would become a fat shared assembly. After Story 6368 the kernel holds three base types, but 44 entities,
   about 86 wire types and 36 held-by types stay in Models for measured reasons. Every context assembly would
   reference it. That is a shared blob, not a kernel.
4. The payoff over what we already have is small. Test selection by path is already how CI works for connectors.
   Once the Contracts rule (R-C1) has no residue, path-based selection by module is sound too. It reads the
   compiled code, so it sees what a compiler would see, and also generic arguments and service lookups. What an
   assembly adds is compile-time errors, and only for references a rule already catches one step later in the
   test run. Neither sees reflection by string or "dynamic".
5. Only 17 % of the 1,344 test files sit under a module folder. 210 start a web host that spans all contexts. Those
   would stay in one integration assembly and run on every change. Measured CI slowness comes from the test
   bodies, so only skipping tests helps, and path selection skips the same set.
6. 29 of the last 300 backend commits (about 10 %) touch the files of exactly one context. That window is the
   packaging period itself, so it is biased towards moves, but it is far from a share that would justify a
   restructure.
```

## Decision

```text
Do not split the backend into assemblies per context, now. One production assembly stays (plus the two migration
assemblies as today). Nothing in the project files, the publish, the Dockerfile, the workflows, the Stryker
configuration or the CI path classifier changes.

The boundary between contexts is:
- the Contracts namespaces of Story 6382 (the only surface one context may use of another), and
- the architecture rules that read the compiled code: the per-module rules, and R-C1 to R-C5 for Contracts.

The split may come later. It points towards microservices, and it would let mutation testing run per assembly,
which is faster. That is a reason to keep the door open, not to open it now. It stays closed until the three
cycles above are gone (see "When to reopen").

If a split comes, the data must follow. A context that is its own assembly and, later, its own service cannot keep
reading tables another context owns. Persistence must be split per context at the same time or before: each
context owns its tables, its repositories and its migrations, and reads another's data only through its Contracts
or by reacting to its events. A split of code without a split of data gives separate assemblies with a shared
database: the coupling stays, and it only moves where the compiler cannot see it.

Option (c), one persistence assembly (the database context and the entities), is the first candidate once the
Data to Services edge (cycle 2c) is gone. Its payoff does not depend on a per-context split: the migration
projects could reference it by an ordinary project reference, and the committed migration DLL copies would
disappear. It is also the first step towards data per context. It is not done now because the cycle makes it
impossible today.
```

## Weigh-points

The questions the decision had to answer, and the answer to each.

| Weigh-point | Answer |
|---|---|
| For: compiler-enforced `internal` | True, and the one thing the rules cannot match. It needs every module type made `internal` except its Contracts, and an `InternalsVisibleTo` per context for its tests. 21 production files declare a non-public type today. The cost is a visibility sweep of every module, for a check that R-C1 already makes in the test run. |
| For: sound CI test selection | Possible without assemblies: select by module path, once R-C1's residue is empty. An assembly adds only the blind spot of reflection by string and "dynamic" (trigger T1 below). |
| For: mutation testing scoped per context | Small gain today. Stryker's per-test coverage already runs only the tests that cover a mutant, and its mutate globs already scope the files. It would be faster per assembly after a split, which is one reason to keep the door open. |
| For: module test assemblies and one integration assembly | About 17 % of the test files would move. The other 83 %, including the 210 that start a web host, stay in one assembly and run on every change. |
| Cost: one database context referencing all contexts | The entities stay in Models (Story 6368), so the database context references Models, not the contexts. What blocks it is the database context naming Services.Interfaces (cycle 2c). |
| Cost: migration setup | Model snapshots store type full names, not assembly names, so a move of assembly does not rewrite them (a move of namespace would). A persistence assembly would remove the committed-DLL cycle (option c). |
| Cost: patchy Sonar coverage | Not applicable. CI collects no coverage and the Sonar gate has no coverage condition. |
| Cost: one binary in every topology | Yes, it still works: a single-file publish bundles project references, as it does for the migration DLLs today. Not a blocker. |
| CI slowness note (Story 6302) | Agreed. Slowness comes from the test bodies, so a split helps only by skipping unaffected tests, and path-based selection skips the same set. |

Options considered:

| Option | Verdict |
|---|---|
| (a) No, not now. One assembly; the Contracts namespaces and the compiled-code rules are the boundary. | **Chosen** |
| (b) One assembly per context. | Rejected now. The three cycles block it. It would first need the repository, `Data`, `Services` and `API` work (Epic 6507, S10 and the Services/API moves) to be done. |
| (c) One persistence assembly. | Rejected now (cycle 2c). The first candidate later (see Decision). |
| (d) Assemblies only for leaf contexts (no other context depends on them). | Rejected. Licensing is used by three contexts and Access by one. The few real leaves still name Services.Interfaces. Two small assemblies would buy nothing measurable. |

## What a later split must keep

```text
- One binary in Standalone (single file on win, linux and osx), Docker and Helm. The bundle must still contain
  every context assembly. The linux single-file publish keeps IncludeNativeLibrariesForSelfExtract. The osx publish
  relies on the project file's single-file setting. The Docker image gets more DLLs beside Lighthouse.dll.
  Code signing and the software bill of materials see only the bundle.
- All four deployment topologies keep working with no change for users.
- One DbContext, unless persistence is split per context on purpose (see Decision). Migrations stay expand-only and
  are created with the Create-Migration script for every database provider.
- The architecture tests load every production assembly of the contexts, not only one (today they load one).
- The licence public key is an embedded resource of Licensing and is read with Assembly.GetExecutingAssembly()
  today; it moves with Licensing. The version is read the same way in two places; it must come from the entry
  assembly.
- The Contracts rules R-C1 to R-C5 stay as tests: assemblies replace what they enforce only partly.
- Every context starts up through its own Add<Context> extension (Story 6382), and the composition root stays the
  only place that knows them all.
```

## When to reopen

Reopen this ADR when all three preconditions hold and at least one trigger fires. The maintainer checks it when
the data-ownership work of Epic 6264 (S10) closes and at each milestone review of Epic 6264. If the preconditions hold and no
trigger fires, this ADR stays "no" and gets a dated note.

| Id | Condition | How it is checked, and by whom |
|---|---|---|
| P1 | No cycle between contexts. | **No rule checks this today.** A rule that would (`ContextDependencyGraphArchUnitTest`, with an inline `ContextCycleEdges` list that only shrinks) **does not exist yet**. It would build the graph of contexts from the compiled code (an edge A to B when a type in A depends on a type in B) and fail on any cycle, naming the path. Until it exists, the maintainer reads the compiled-code edges by hand with a throwaway probe. |
| P2 | The old Services, API and Data code does not form a cycle with the contexts: no module type depends on `Services`, `API` or `Data`, and `Data` does not depend on `Services`. | The maintainer, with the same throwaway probe over the compiled code. Not checked by any test today. A cycle that runs through that code (A to Services to B) is not a context edge and would not be seen by the P1 rule. |
| P3 | The residue of R-C1 is empty. Today (after Story 6382) 8 edges are listed, in the residue table of ADR-270. | Read the `CrossContextEdgesOutsideContracts` list in `ContextContractsArchUnitTest`. The test run checks the list both ways. Checked by the maintainer. |
| T1 (test time) | Module-path test selection is live (Story 6302 line), and over six months at least 25 % of backend commits touch one context only, and at least one selection miss happened that the compiled-code rules could not see (reflection by string, "dynamic"). | Commit share from `git log --name-only` over the window. A miss is recorded as a Bug. The maintainer. |
| T2 (build time) | The incremental backend build after a one-line change in a module has a median of 9 runs of 60 s or more, on the maintainer's machine or the CI runner. | A timed `dotnet build` ten times, nine counted. The maintainer. No baseline was measured. |
| T3 (escape) | Two or more cross-context references that broke a rule's intent reached `main` within six months without a rule seeing them. | Each is recorded as a Bug. The maintainer counts them. |

The thresholds are a starting point. The maintainer may change a number without changing the decision. A later
"yes" also needs a fresh measurement of the compiled code, a publish measurement of the bundle, and the plan for
the data (see Decision).

## Consequences

```text
Nothing a user sees changes. The project files, the publish and the pipelines stay as they are.

Maintainers get one decision with its real reasons. ADR-027 D5's reason was wrong, so an argument that cites it
("it breaks the single binary") is not valid and should be answered with this ADR.

The enforcement stays in tests. It sees the compiled code, so it is as strict as a compiler on the edges a
compiler can see, but it fails in the test run, not at build. A context's internal types stay public in the
assembly; a rule, not the compiler, keeps other contexts off them.

The cycle between Flow metrics and Work Items, the old Services, API and Data code, and the Data to Services edge
remain. They are already owned by other work (the listed edges of ADR-270, Epic 6474, Epic 6507 and S10).
This ADR adds no new work and no new rule. The context-cycle rule named under P1 is a possible future item, not
a part of this decision.

If the project later grows towards separate services, the first steps are not assemblies. They are the data per
context, the events in place of the remaining direct calls, and the persistence assembly (option c).
```

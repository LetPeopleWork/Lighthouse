# Story 6657 — Place the write-back round in Forecasting

## The problem

The write-back round (what is written back to a tracker after a refresh, and when) was the last group of types left
in the loose `Services.Implementation` and `Services.Interfaces` namespaces that the earlier packaging Stories named
as an exception. Because the round had no namespace of its own, three contexts (Promises, Access, Licensing) each kept
their own list of its 8 full names to refuse it, and Work Items named those 8 names in its stray lists.

## What changed

- **S1 — rules see the round in both places.** `ModuleRules.TheWriteBackRound` became the one owner of "which types
  are the round", matching the old names and the new namespace; the Promises, Access and Licensing predicates read it
  next to their lists.
- **S2 — the move.** `git mv` of the 8 round types into `Modules/Forecasting/Application/WriteBack`
  (`WriteBackCollector`, `WriteBackRound`, `WriteBackRoundContext`, `WriteBackService`, `WriteBackTriggerService` and
  the three ports), and of their 8 unit-test files beside them. Production changed only in `namespace` and `using`
  lines (`Program.cs` gained one `using`; no registration, order or lifetime moved). The rule lists follow the moved
  names: Work Items' stray lists name the round by its new full names, Forecasting's inbound list grows from 1 to 5
  entries, its licence edges from 1 to 2, Flow metrics' inbound list goes from 4 to 3.
- **S3 — rules by namespace only.** `TheWriteBackRound` holds only the new namespace; the three 8-name lists and the
  Promises write-back prefixes and newcomer case are deleted. New round rules R-WB1 to R-WB4 with planted offenders.
  `ARCHITECTURE.md` §4 and ADR-270 answer the open write-back question (Forecasting `Application/WriteBack`) and record
  the measured edges; the catalogue closes its last loose-Services exception.

## Decisions taken in AFK mode that the maintainer may revisit

- **S2 against an S1 law.** The S1 role law kept Work Items' stray lists as "old place only", which the S2 design
  revision (rename the round entries to the new names) contradicted. Rather than loosen the law, the three fields are
  declared (`WriteBackPatternRoles.NameTheRoundByItsNewFullNames`), each checked to name nothing else in the new place.
  The S1 approval was re-reviewed and accepted.
- **S3 grouped alternations.** The S3 oracle review found the old-name sweep missed a grouped alternation. The fix sits
  in S3's own oracle file; the shared `NamespaceMoveLaw` is untouched, because changing it would have re-opened the S1
  approval. Deeper nesting than the two named forms is not expanded.
- **Measured, not designed.** R-WB2 lists 3 Tracker integration edges where the design said 2; the docs follow the
  measurement.
- **Candidate review on sonnet.** S1 was `thorough`, S2 and S3 `standard`; the whole-candidate review ran on the reviewer
  model of the strictest slice (sonnet), EXAMINE on haiku as recorded.

## Knowingly left open

- The staying `Services.Implementation` names in Work Items' `StrayLooseImplementation` are read by no rule (a probe
  removing one stays green). Pre-existing; worth a look in the Work Items packaging.
- The moved files keep comments that cite ADR ids (`IWriteBackTriggerService.cs`, `WriteBackService.cs`): the move was
  kept byte-identical on purpose. They should be rewritten the next time those files change for a reason.
- 10 surviving and 7 no-coverage mutants in the round (all pre-existing): candidates for an improvement item on the
  round's tests.

## Commits

- `b7fe2a99d` docs(forecasting): story 6657 — product facts and design recorded in nwdes
- `1845e5255` docs(forecasting): story 6657 — EXAMINE expectations recorded
- `ace607cb2` test(forecasting): story 6657 S1 oracle — rules see the write-back round before it moves
- `8c515a0e1` docs(forecasting): story 6657 S1 oracle review accepted
- `c12b04bd6` test(forecasting): story 6657 S1 — Promises, Access and Licensing rules read the write-back round pattern
- `0972b9b58` docs(forecasting): story 6657 S1 slice review accepted
- `f22190cc6` test(forecasting): story 6657 S2 oracle — the write-back round lives in Forecasting's WriteBack namespace
- `37b8f1769` docs(forecasting): story 6657 S2 oracle review rejected — Work Items rule file missing from S2 targets
- `d67704b01` docs(forecasting): story 6657 design — S2 renames the round entries of the Work Items stray lists
- `513eb3b8a` test(forecasting): story 6657 S2 oracle revised — flat Application name is named; method name counts eight
- `e38fa76af` docs(forecasting): story 6657 S2 oracle review accepted
- `e22d88adf` refactor(forecasting): story 6657 S2 move the write-back round into Forecasting's WriteBack namespace
- `ee74d50b8` test(forecasting): story 6657 S2 rules follow the write-back round to its new names
- `f55159952` test(forecasting): story 6657 S1 oracle declares the stray lists that name the round by its new full names
- `370987dc1` docs(forecasting): story 6657 S1 oracle re-review accepted after the stray-list declaration
- `376e100d0` docs(forecasting): story 6657 S2 craft accepted
- `1ae26cc60` docs(forecasting): story 6657 S2 slice review accepted
- `59bf9df06` test(forecasting): story 6657 S3 oracle — rules guard the write-back round only by its Forecasting namespace
- `5ec1c93ce` docs(forecasting): story 6657 design — the round rules file is an S3 oracle file; S3 docs follow the measured edges
- `9628ba08b` docs(forecasting): story 6657 S3 oracle review rejected — the sweep misses a grouped old alternation
- `03fc1124e` test(forecasting): story 6657 S3 oracle revised — the sweep reads grouped old alternations
- `7bcdcb8c1` docs(forecasting): story 6657 S3 oracle re-review accepted
- `e4f3b0987` test(forecasting): story 6657 S3 rules name the write-back round only by its Forecasting namespace
- `8e841823d` docs(architecture): story 6657 the write-back round lives in Forecasting
- `6dbfb9536` docs(forecasting): story 6657 S3 craft accepted
- `a485c09e3` docs(forecasting): story 6657 S3 slice review rejected — ARCHITECTURE.md still lists the round as unplaced
- `8710bd7c4` docs(architecture): story 6657 the write-back round is no longer listed as unplaced
- `f291914bf` docs(forecasting): story 6657 S3 craft re-closed after the docs fix
- `dfa3f8cde` docs(forecasting): story 6657 S3 slice re-review accepted
- `30b5e6c5a` docs(forecasting): story 6657 candidate review accepted
- `e0805c82f` docs(forecasting): story 6657 EXAMINE round 2 — E1 to E3 unmet as observation gaps
- this finalize commit

## Finalize checklist

| Item | Answer |
|---|---|
| Docs prose | N/A, because nothing a user sees changed: no page under `docs/` describes the code layout. |
| Screenshots | N/A, because no UI changed. |
| Demo data | N/A, because no behaviour or data changed. |
| Website assets | N/A, because no asset under `docs/assets/` changed. |
| Clients (CLI, MCP) | N/A, because no route or DTO changed. |
| Context map, `ARCHITECTURE.md` | Done in `8e841823d` and `8710bd7c4`: ADR-270 (write-back answered, the two edge rows, the measured edges, the glossary row) and `ARCHITECTURE.md` §4 (the Forecasting row and rules bullet, the Flow metrics count, row 4). |
| Usage-data event | N/A, because the Story changes no behaviour a user can use (DEVOPS); write-back's existing events are unchanged. |
| Mutation | Done: Stryker.NET over `Modules/Forecasting/Application/WriteBack/**`, **89.17 %** (140 killed, 10 survived, 7 no coverage of 157); the 14 production files the Story touched are identical to their pre-move selves apart from `namespace` and `using` lines, so the move added no survivor. See `docs/feature/story-6657-write-back-round/mutation/results.md`. |
| Release notes | N/A; written at /release. |

## Gates

- `dotnet build Lighthouse.Backend`: 0 warnings.
- Whole filtered backend suite on `6dbfb9536`: 15044 passed, 0 failed, 2 skipped. Production and tests unchanged after it.
- Architecture namespace + the round's unit tests on `dfa3f8cde`: 2157 passed, 0 failed (candidate reviewer).
- Slice reviews: all accepted (S2 oracle at round 2; S1 re-reviewed after its support revision; S3 oracle at round 2;
  S3 slice at round 2). Whole-candidate review: accepted, no findings.
- EXAMINE: round 1 indeterminate (draft aborted: totals only); round 2 published E1-E3 unmet as observation gaps with no product failure (architecture-rules WriteBack 61/61, Forecasting 147/147, EF model unchanged on Sqlite and Postgres, backend-tests WriteBack 313/313); the maintainer waived the three gaps (2026-11-26), coverage named per row in the handoff.
- CI: see the push of this commit.

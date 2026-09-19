# RESUME — slices 03 and 04

Slices 01 and 02 are shipped, pushed and CI-green. ADO: Epic 6028 Active, 6029 and 6030 Resolved,
6031 (slice 03) and 6032 (slice 04) New. Backend suite baseline **7119 passed, 0 failed**.

## Read first

`../feature-delta.md` (DISCUSS/DESIGN/DISTILL, plus three DELIVER sections added during delivery:
Live Jira fixtures, Accepted risk, and the Gherkin that specifies slices 03-04) · `roadmap.json`
(22 steps; slices 03-04 are `03-01..03-04` and `04-01..04-03` — their `implementation_notes` carry
findings that contradict the DESIGN, and the notes win) · `../mutation/README.md` ·
`../slices/slice-0{3,4}-*.md` · `CLAUDE.md` · `docs/ci-learnings.md`.

## How to run a step

Dispatch each roadmap step to `@nw-software-crafter` with the full DES template from
`~/.claude/skills/nw-execute/SKILL.md`. Never implement directly. **One step per dispatch,
sequential** — two agents on `JiraWorkTrackingConnector.cs` collide, and both run the full suite.
Each dispatch carries the step's criteria, its Gherkin verbatim, an explicit `files_to_modify`, the
current baseline, and the traps below.

## Traps already paid for

- `des-commit --owned-paths` takes every path after **one** flag, space-separated. Repeating the
  flag silently keeps only the last and commits one file.
- `dotnet format analyzers Lighthouse.sln --severity info --verify-no-changes` — **the flag is
  mandatory**; without it this is FIX mode, it rewrites ~35 EF migration files and reports a false
  clean. The `.sln` is under `Lighthouse.Backend/`. Filter output by filename, and union
  `git diff --name-only` with `git status --porcelain -uall` (plain `--porcelain` lists an untracked
  directory and drops new files).
- Error-severity here, not warnings: `S1481` unused local, `S1172` unused parameter, `S2699`
  assertion-free test, `S8969`/`S8970` null-forgiving operator, `S1066` collapsible if, `S6618`
  `FormattableString.Invariant`. `NUnit2046` wants `Has.Count.EqualTo(n)` — INFO, invisible to
  `dotnet build`, fatal to the Sonar gate.
- ArchUnit enforces module boundaries by test: a connector may not depend on
  `Services.Implementation.WorkItems`. Hence `ParentSourceSelector` under `.../Parents`.
- Always `dotnet test --filter "TestCategory!=Integration&TestCategory!=JiraIntegration&TestCategory!=LinearIntegration&TestCategory!=AdoIntegration&TestCategory!=ServiceNowIntegration"`.
  Never unfiltered — those hit real trackers and throw rather than skip. Environmental, not
  regressions: `SQLite Error 15: 'locking protocol'` under load, `ReleaseServiceTest` contention;
  both pass alone.
- Live Jira dogfoods run in CI. **Reads only, never write to the instance.** Fixture map in the
  delta under "Live Jira fixtures" (`LGHTHSDMO-24/1716/1725/1726`, link type `Cloners`). Do not
  repoint the `Blocks` links on `LGHTHSDMO-7..10`.
- **No comment may cite a section identifier** (`D3`, `DDD-1`, `AC-2.6`, `ADR-193`). Write the reason.

## Slice 03

- **DDD-8 is wrong.** The Team page does not fetch the refresh log. `RefreshLog` is served only by
  `SystemInfoController.GetRefreshLog()` and consumed only by
  `Lighthouse.Frontend/src/pages/Settings/SystemInfo/RefreshHistorySection.tsx`.
- **No aggregation site on the Team path.** `ReportLinksThatMeantNothingHere` is called only from the
  Portfolio path, and `CreateWorkItemFromJiraIssue` is `private static` — it can neither log nor
  count. Candidates must reach its two instance-method callers.
- **No route for the count to `SyncOutcome`.** It is a positional `sealed record` whose own comment
  records that `Reason` was added as an `init` member specifically to avoid a fourth positional
  parameter. Follow that.
- `RefreshLog.AmbiguousParentCount` is expand-only and additive. Use `Create-Migration.ps1`
  (hyphenated, in `Lighthouse.Backend/`, takes `-MigrationName`), never `dotnet ef migrations add`.
  **Build the backend first** or `dotnet ef` reports phantom pending model changes — the migration
  DLLs are HintPath references.
- First frontend change of the feature: `pnpm test`, `pnpm build` (zero errors **and** warnings),
  Biome clean on `./src` all become gates.
- `ParentResolution.IsAmbiguous` has no production consumer yet. Slice 03 should use it; if it ends
  up unused, delete it rather than leave a public member nothing calls.

## Slice 04 — STOP AND ASK before writing code

**Its premise is false.** DISCUSS D6 assumed the other four connectors already refuse a link-type
reference "by accident". They do not: only Jira and Azure DevOps validate Additional Fields at all.
ServiceNow, Linear and CSV never read `connection.AdditionalFieldDefinitions`, so a link-type
reference is accepted and silently ignored on three connectors. Slice 04's own brief pre-committed
the consequence — *"then this slice is not an assertion, it is a fix, and it is bigger than three
hours."* That is a scope decision for the user. Present it and wait.

Also: `IWorkTrackingConnector` has **no** default interface implementations — `AzureDevOps:72`,
`Csv:33`, `Linear:45` each write `=> []` explicitly. Four one-line additions, not inheritance.

## Slice wrap-up, after the last step of each slice

1. **L1-L6 refactor** over everything the slice delivered. Behaviour unchanged, suite count unmoved.
2. **Adversarial review** via `@nw-software-crafter-reviewer` over the whole slice diff. **Verify any
   BLOCKER yourself against the code before acting on it** — one in slice 02 was wrong, and its
   remedy would have caused the bug it was trying to prevent.
3. **Mutation analysis by hand**, not Stryker (user's decision: the live `JiraIntegration` tests drag
   real HTTP into every mutant run). Enumerate operators over the slice's production diff, match each
   to a killing test, close the survivors worth closing, append to `../mutation/README.md`. The
   analyzer gate makes some mutants uncompilable — check a survivor is writable before calling it a
   gap. Slice 03 adds frontend code; decide there whether StrykerJS is worth running.
4. **Full gates**: build zero warnings (6 pre-existing transitive TFM ones are baseline), filtered
   suite green, plus the frontend three for slice 03.
5. Commit the feature docs, excluding `execution-log.json`.
6. **STOP. Hold for the user's manual verification. Do not push.**
7. After they confirm: push, watch CI green (the three `Package * Standalone` jobs sit in `waiting` —
   release gates, not failures), then move the slice's ADO Story to Resolved.

## ADO

`az` CLI, not MCP. 6031 → Active when slice 03 starts, Resolved at its boundary; 6032 likewise.
**Epic 6028 stays Active — never Closed. Closed means released.**

## Autonomy

Proceed without asking except: the slice-04 scope decision, the two holds before pushing, and
anything that would change behaviour beyond a step's stated criteria. Report where the code
contradicts the design — that has been the most valuable output of every step.

## Still owed, not closeable here

AC-1.4's Data Center half and AC-2.6's customer-value half need a Jira Data Center instance that does
not exist. Post-release verification run, accepted by the user. `OUT-PFIL-hierarchy-recovered` stays
open as the marker.

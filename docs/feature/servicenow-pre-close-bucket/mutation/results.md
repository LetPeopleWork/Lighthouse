# Mutation testing — US 5612 (ServiceNow pre-close bucket)

Run 2026-08-01 against `worktree-mutable-prancing-church` @ `21f22d327`. Gate is 80 % kill rate on
each stack that changed.

| stack | score | tested | killed | survived | timeout | no coverage | wall clock |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Backend (Stryker.NET) | **85.91 %** | 430 | 367 | 58 | 5 | 3 | 10 m 20 s |
| Frontend (StrykerJS) | **N/A** | — | — | — | — | — | — |

**Frontend is N/A because the slice changed zero frontend files**, which is DD-6 rather than an
oversight: `WorkItemBase.Url` and `WorkItemBase.Type` already flow through `WorkItemDto` to the five
places the UI reads them, so making the connector fill `Url` and translate `Type` needed no component,
no DTO field and no migration. (5611 did need a frontend run — it changed
`DataRetrievalSchemaDefaults.ts`. This does not.)

Config: `stryker.5612.backend.json`. Run from `Lighthouse.Backend.Tests/`. See `/mutation-testing`.

## Backend

| file | tested | killed | survived | timeout | score |
| --- | --- | --- | --- | --- | --- |
| `ServiceNowWorkItemMapper.cs` | 35 | 35 | 0 | 0 | **100 %** |
| `ServiceNowReadScope.cs` | 18 | 18 | 0 | 0 | **100 %** |
| `ServiceNowHistoryVerdict.cs` | 14 | 14 | 0 | 0 | **100 %** |
| `ServiceNowWorkTrackingConnector.cs` | 260 | 238 | 17 | 5 | **93.5 %** |
| `ServiceNowClassLabels.cs` | 103 | 62 | 41 | 0 | **60.2 %** |

The three files carrying this story's logic are at 100 %. The two below the line are discussed
individually, because they fail for opposite reasons: the connector's survivors are all in code this
story never touched, and the labels file's survivors are all map *data* rather than logic.

### This story's changed lines specifically

Every mutant landing on a line this slice edited in the 1096-line connector was accounted for: **9
killed, 9 compile-error (Stryker safe mode), 2 ignored, 0 survived.** Those lines are `ValidateConnection`'s
advisory removal, the `MapRecord(record, team, scope, instanceUrl)` call site, `FirstUnreadableKindOfWork`'s
`scope.AsTyped(recordClass)`, and the `asTyped` parameter threaded through `WhyThisKindOfWorkCannotBeRead`.

### Accepted survivors — `ServiceNowWorkTrackingConnector.cs` (17)

All seventeen are in code this story did not write, and all of them predate it:

| lines | what | origin |
| --- | --- | --- |
| 143, 211 | log message strings | #5611 |
| 398 | `returnedToTheQueue > startedDate` boundary and its `&&` | #5621 |
| 464, 522 | first-page short-circuit, and the paging dedupe key | #5621 |
| 838–927 | `Link` header parsing — `FirstOrDefault`, `|=`, the index walk, the `Uri.TryCreate` guard | pre-#5611 |

Left alone deliberately. Writing tests for #5621's paging internals inside a story about labels and
deep links would put the coverage in a place no reader would look for it, and #5621's own run already
triaged that file at 88.11 %. Worth a note for whoever closes the epic: the `Link`-header walk at
838–927 is the densest untested cluster in the connector and is nobody's slice so far.

### Accepted survivors — `ServiceNowClassLabels.cs` (41)

**Every one is a `String mutation → ""` on a row of the 50-entry map. None is in a method.** The
methods (`ClassFor`, both lookups, the passthrough branches) are fully killed; what survives is the
table itself.

Three things were checked before accepting them.

**It is not a coverage artefact.** Each survivor was covered by 162 tests, 32 of them from
`ServiceNowClassLabelsTest`. The tests ran and did not care.

**The mutation is usually invisible by construction.** Stryker blanks either half of
`["sc_request"] = "Request"`. Blanking the **key** makes `ClassFor("")` return `""` — because `""`
becomes a known class name — and leaves `ClassFor("sc_request")` answering `sc_request` anyway, via
passthrough. So the empty-name test still passes and behaviour is unchanged for every input except
that one label. Blanking the **value** removes one label from the reverse map, and the class name
keeps working.

**Being wrong about an entry is loud, not silent.** A coach who types a label the map has lost gets
passthrough, so the query filters on `sys_class_nameINChange Task`, which matches nothing — and the
per-class readability probe (ADR-124 D1) refuses the save and names the string they typed. This is
ADR-128's "being wrong about an entry costs nothing", and it is the reason the map is allowed to be a
static list at all.

**The alternative was rejected on purpose.** A `[TestCaseSource]` over all 50 pairs would kill all 41
— and would be a second copy of the map asserting the first copy. That is the same test-only
duplication the review removed with `LabelFor` earlier in this story, and it locks the data without
proving anything about behaviour. The nine entries a flow team actually configures — `incident`,
`problem`, `change_request`, `sc_task`, `task`, `release_task`, `change_request_imac`,
`sysapproval_group`, `ticket` — are asserted by name. The 41 survivors are platform and automation
internals (`cmdb_multisource_recomp_task`, `sn_creatorstudio_*`, `upgrade_history_task`,
`orphan_ci_remediation`) that no team runs a board on.

**If OC-4 restores `LabelFor`** for #5610's picker, a single `Has.None.Empty` over every class's label
becomes expressible and would kill most of these without mirroring the table. Worth doing at that
point, not before.

## Not mutated

`ServiceNowClassLabelsTest.cs` and the other test files, obviously; and no frontend file, per the N/A
above. Nothing in the production change set was excluded — unlike the first (dead) attempt at this
run, whose config left `ServiceNowWorkTrackingConnector.cs` out on buried-score grounds. Including it
is what let the table above show that this story's own lines have no survivors, which is the more
useful fact.

## Run notes

An earlier attempt on the same tree died silently ~35 minutes in: the log froze mid
`Capture mutant coverage`, no process remained, and no report was written. Two hypotheses were
recorded at the time and both turned out to be wrong, so they are written down here rather than left
to be re-derived:

- *"`ServiceNowTeamSyncAcceptanceTest` slips past `!~IntegrationTest` because its namespace is
  `API.Integration`, and spawns a WebApplicationFactory per test under `perTestInIsolation`."* It does
  slip past — but it landed in #5575, so it was already inside 5621's identical filter when that run
  finished cleanly in 13 m 41 s. Not the cause.
- *"The `mutate` globs do not bind, hence 13 707 mutants."* They bind. Stryker.NET mutates the whole
  project in the create phase and applies `mutate` at the test stage; `13 277 total mutants are
  skipped` / `430 total mutants will be tested` is the proof, and the create-phase compile-error
  warnings about `RunChartData.cs` or `OAuthService.cs` are normal noise from that same phase.

The remaining explanation is the skill's own first warning — this session had been running
`dotnet build` and `dotnet test` throughout, and nothing else may touch `bin/` while Stryker runs.
The successful run was left strictly alone.

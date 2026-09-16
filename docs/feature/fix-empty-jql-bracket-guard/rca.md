# RCA — ADO Bug #5974: nothing protects the empty-bracket guard in `PrepareGenericQuery`

Method: Toyota 5 Whys, multi-causal, evidence required at every level.
Date: 2026-09-16 · Investigator: Rex (nw-troubleshooter)
Repo state: worktree `frolicking-fluttering-karp`, branch `worktree-frolicking-fluttering-karp`,
HEAD `858fa2926`.
Origin: found while triaging surviving mutants during Bug #5973's mutation run
(`docs/feature/fix-jira-field-lookup-error-message/mutation/stryker.6012.backend.json`);
pre-existing, outside that diff.

Unless stated otherwise, every `:NNN` refers to
`Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/WorkTrackingConnectors/Jira/JiraWorkTrackingConnector.cs`
(2730 lines at HEAD).

**Evidence grading used throughout.** `[MEASURED]` = observed by running code in this worktree.
`[READ]` = read off a file at a cited line. `[CITED]` = from a named external source.
`[INFERRED]` = reasoning, not measurement — flagged wherever it carries weight.

---

## 0. The headline, before the method

Three things this investigation found that change what the fix has to be:

1. **The guard is correct, and its stated nightmare is not real.** `AND ()` cannot parse as JQL —
   Jira's own grammar has no production for it (§4). So the mutant's output would be a **loud 400**,
   not a silent deletion. The comment at `:1972-1979` overstates what is at stake for `AND ()`.
2. **But the nightmare is real one step further out, and it is live today with the guard intact.**
   When the type list, all three state lists *and* the operator's own query are empty, the connector
   puts **`jql=`** — the empty string — on the wire. `[MEASURED]`. Atlassian documents an empty JQL
   as returning either *everything* or *nothing* depending on an instance setting (§4b). "Nothing"
   is the mass-deletion case, and no guard stands between it and `RemoveItemsThatLeftTheQuery`.
3. **Three other connectors already delete silently on an empty list, today, with no mutant
   required** — Linear's team read, ServiceNow's state filter, and CSV's row filter all return zero
   records for an empty collection (§7, Chain D). Those are worse than the bug as filed.

The bug as filed is real but is the smallest of the four holes it sits next to.

---

## 1. Problem statement (scoped)

`PrepareGenericQuery` (`:2005-2010`) turns a team's or portfolio's mapped work item types, or its
mapped states, into a JQL fragment:

```csharp
private static string PrepareGenericQuery(IEnumerable<string> options, string fieldName, string queryOperator, string queryComparison)
{
    var query = string.Join($" {queryOperator} ", options.Select(o => $"{fieldName} {queryComparison} \"{QuotedForJql(o)}\""));
    query = options.Any() ? $"AND ({query}) " : string.Empty;   // :2008
    return query;
}
```

Stryker forces `options.Any()` to always-true and **the mutant survives**: no test in the suite
constructs a team or portfolio with an empty work item type list or an empty state list.

**In scope:** the `options.Any()` guard at `:2008`, its two call sites (`:1961`, `:1962`), every
route by which the collections it guards can become empty, the identical guard in the Azure DevOps
connector (`AzureDevOpsWorkTrackingConnector.cs:1334`), the equivalent empty-collection behaviour
of the Linear, ServiceNow and CSV connectors, and the removal path the whole thing feeds
(`WorkItemService.cs:212-222`).

**Out of scope:** the JQL quoting rules (`QuotedForJql`, `:2019-2022`, already pinned),
`ORDER BY` stripping (already pinned), and the field-lookup path (Bug #5973 / #6012).

**Not a live defect.** No customer report, no failing refresh. The subject is the absence of
protection, and the failure mode if the protection were removed.

---

## 2. Seed-claim verification

| # | Seed claim | Verdict | Evidence |
|---|---|---|---|
| 1 | `:2007-2008` is as quoted; `options` is the mapped types or the mapped states | **CONFIRMED** | `:1961` `PrepareGenericQuery(owner.WorkItemTypes, JiraFieldNames.IssueTypeFieldName, "OR", "=")` · `:1962` `PrepareGenericQuery(owner.AllStates, JiraFieldNames.StatusFieldName, "OR", "=")` `[READ]` |
| 2 | The mutant forcing `options.Any()` true survives | **CONFIRMED by construction, not re-measured** | The archived artefact at `docs/feature/fix-jira-field-lookup-error-message/mutation/stryker.6012.backend.json` is the **run config only** (1 400 bytes, a `stryker-config` object), not a result report — the surviving-mutant list is not in this repo. The survival is nonetheless certain: a repo-wide grep of `Lighthouse.Backend.Tests` for `AND ()`, `Does.Not.Contain("AND ()")` and for any owner whose four collections are all empty returns nothing, and every `WorkItemTypes.Clear()` in the test project is immediately followed by an `.Add(...)` (e.g. `JiraBoardQueryAssemblyTest.cs:649-650`, `:637-638`). `[READ]` `[INFERRED]` |
| 3 | The mutant would emit `AND ()` | **CONFIRMED** `[MEASURED]` | `string.Join(sep, <empty>)` is `""`, so the true-branch yields `"AND () "`. Executed in a throwaway fixture in this worktree (§3); printed `MUTANT-FRAGMENT >>>AND () <<<` |
| 4 | Blast radius: an empty bracket read as "matches nothing" ⇒ sweep reports no records ⇒ removal is "stored minus swept" ⇒ every stored record deleted | **MECHANISM CONFIRMED; TRIGGER CORRECTED** | Mechanism: `WorkItemService.cs:141-142` then `:212-222` — `storedWorkItems.FindAll(stored => !stillOnTheTracker.Contains(stored.ReferenceId))` followed by `workItemRepository.Remove(...)`, with **no floor and no sanity check** `[READ]`. Trigger: `AND ()` is **not** a "matches nothing" query, it is a parse error (§4) — so this trigger does not fire. A different trigger, the fully-empty JQL, does (§3, §4b) |
| 5 | The comment at `:1972-1979` states the blast radius | **CONFIRMED, and it is the only place it is written down** | `:1975-1978` "what Jira does with one is not knowable from here: reading it as 'matches nothing' would make the sweep report no records … every stored record for that team or portfolio would be deleted" `[READ]` |

---

## 3. Measurement: what the connector actually puts on the wire

A throwaway NUnit fixture was added to
`Lighthouse.Backend.Tests/Services/Implementation/WorkTrackingConnectors/Jira/`, driven through the
public `ValidateTeamSettings` over a stub `HttpMessageHandler` (the `JiraFieldLookupTest` pattern),
run with the live-connector categories excluded, and then **deleted**. No production code touched;
no network call; `git status` clean afterwards.

| Team shape | JQL the connector issued | `[MEASURED]` |
|---|---|---|
| Baseline (1 type, 3 states, `project = PROJ`) | `(project = PROJ) AND (issuetype = "Story")  AND (status = "To Do" OR status = "In Progress" OR status = "Done")  ` | yes |
| All three state lists empty | `(project = PROJ) AND (issuetype = "Story")   ` | yes |
| `WorkItemTypes` empty | `(project = PROJ)  AND (status = "To Do" OR status = "In Progress" OR status = "Done")  ` | yes |
| **Types, all states and `DataRetrievalValue` all empty** | **the empty string — nothing at all** | yes |
| Mutant's fragment for an empty list | `AND () ` | yes |

Raw request line for the all-empty case, exactly as issued:

```
/rest/api/3/search/jql?jql=&fields=%2Aall&expand=changelog&maxResults=10
```

Two facts follow, and neither was in the seed:

- **The guard silently widens the query.** An empty state list does not fail — it removes the state
  clause, so the team fetches items in states it never mapped. Same for types. That is a
  data-exposure direction (records from outside the configured scope enter Lighthouse), not just a
  performance one.
- **An empty `jql=` parameter is reachable.** `WithoutTheLeadingConjunction` (`:1980-1988`) has
  nothing to strip and returns `""`; `PrepareQuery` (`:1959-1970`) has no post-condition;
  `GetIssuesByQuery` (`:1602-1617`) logs it and issues it. Nothing between `:1959` and the socket
  refuses an empty query.

`ValidateTeamSettings` returned `code=valid, IsValid=true` for **every** row above, including the
all-empty one (the stub answered with one issue, so the `no_work_items_found` branch at `:1064-1071`
did not fire). Jira validation therefore does not notice an empty configuration. `[MEASURED]`

---

## 4. Question 3, settled: what Jira does with `AND ()`

### 4a. `()` is a syntax error. Settled from the grammar, without a live call.

Jira's JQL parser is generated from an ANTLRv3 grammar (`Jql.g`) shipped inside `jira-core`. The
generated parser's Javadoc publishes the `FOLLOW_*` bitset names, one per grammar element reference,
and those names reconstruct the rules unambiguously ([Atlassian, `JqlParser` API, Jira Server
9.4.5][jqlparser]):

| FOLLOW field | Rule it reconstructs |
|---|---|
| `FOLLOW_LPAREN_in_subClause310`, `FOLLOW_orClause_in_subClause312`, `FOLLOW_RPAREN_in_subClause314` | `subClause : LPAREN orClause RPAREN` |
| `FOLLOW_andClause_in_orClause153`, `FOLLOW_OR_in_orClause158`, `FOLLOW_andClause_in_orClause164` | `orClause : andClause (OR andClause)*` |
| `FOLLOW_notClause_in_andClause197`, `FOLLOW_AND_in_andClause202`, `FOLLOW_notClause_in_andClause208` | `andClause : notClause (AND notClause)*` |
| `FOLLOW_set_in_notClause231`, `FOLLOW_notClause_in_notClause243`, `FOLLOW_subClause_in_notClause250`, `FOLLOW_terminalClause_in_notClause278` | `notClause : (NOT\|BANG) notClause \| subClause \| terminalClause` |
| `FOLLOW_field_in_terminalClause355` | `terminalClause : field …` |

A `subClause` requires an `orClause` between its brackets. `orClause` requires at least one
`andClause`; `andClause` requires at least one `notClause`; `notClause` requires a `subClause` or a
`terminalClause`, and `terminalClause` begins with a `field`. **No rule on that path has an empty
alternative.** `()` therefore cannot derive, and the parser must fail at the `)`.

Corroboration from the field — the error Jira actually produces, reported by an administrator whose
gadget serialised an empty group option into an empty bracket pair:

> `Error in the JQL Query: Expecting a field name but got ')'. You must surround ')' in quotation
> marks to use it as a field name. (line 1, character 129)`
> — [Atlassian Community, 2019-08-20][community-rparen]

This is the same sentence shape this repo already pins for a leading `AND`
(`JiraBoardQueryAssemblyTest.cs:44-45`), so the failure mode is one the codebase already models.

**Consequence.** The mutant's `AND ()` produces HTTP 400. Since Bug #5973, a 400 on the search
endpoints becomes `JiraQueryRejectedException` (`RejectedQuery`, defined `:1729`, thrown at `:1661`
and `:1708`). On the refresh path
that exception propagates out of `FetchEverything` (`WorkItemService.cs:281`,
`await connector.GetWorkItemsForTeam(team, Stopping)`), which is awaited by
`RefreshWorkItems` at **line 140** (`var fetch = await ResolveRemoteFetch(...)`) — **two statements
before** `RemoveItemsThatLeftTheQuery` at **line 142**. The refresh aborts and **nothing is
deleted**. `[READ]`

So for `AND ()` specifically: **the failure is loud and the deletion never happens.** The code
comment at `:1975-1976` should be corrected, not reinforced.

**Why the FOLLOW names are load-bearing and not a guess.** ANTLRv3 emits exactly one `FOLLOW_x_in_yNNN`
bitset per *reference to element `x` inside rule `y`*, named after the rule it appears in and the
character offset of that reference in the grammar file. The set of FOLLOW names for a rule is
therefore a complete inventory of that rule's right-hand side, and the `NNN` offsets give their
order. `subClause` has exactly three — `LPAREN`, `orClause`, `RPAREN`, at offsets 310/312/314 — so it
has exactly one alternative and that alternative contains an `orClause`. A rule with an empty
alternative would still show its non-empty alternative's references, so this alone does not prove
`orClause` is non-nullable; but `orClause` **does** show `FOLLOW_andClause_in_orClause153` at the
lowest offset in its own rule, meaning its first element is a mandatory `andClause`, and the same
holds one level down. `[INFERRED]` from standard ANTLRv3 code-generation behaviour, applied to
`[CITED]` field names.

*Confidence:* high, from Atlassian's own published parser artefact plus a matching field report. Not
verified against a live instance — that was forbidden, and the grammar makes it unnecessary. **And
the fix does not depend on it:** if this inference were wrong and `AND ()` did parse as "matches
nothing", that would make the hazard *worse*, not absent, and every recommendation in §8 would stand
unchanged.

### 4c. Alternatives ruled out — could `AND ()` reach Jira and be accepted?

| Hypothesis | Ruled out by |
|---|---|
| Lighthouse rewrites or strips the query before sending | **No.** The measured request line (§3) carries the JQL verbatim, URL-escaped and nothing more: `Uri.EscapeDataString(jqlQuery)` at `:1646`, and the Cloud path builds the same way. Round-tripping the captured parameter through `Uri.UnescapeDataString` returned exactly what `PrepareQuery` produced. `[MEASURED]` |
| A proxy in front of Jira normalises it | Possible in principle, but it would have to *add* a clause, not remove one — no proxy invents a `field = value`. And Lighthouse cannot rely on a component it does not ship. |
| Jira is lenient about empty brackets specifically | Contradicted by the field report at [community-rparen], where Jira rejected exactly this with a character offset. |
| `ORDER BY` stripping could leave an empty bracket that is then treated differently | Different mechanism, already pinned: `JiraBoardQueryAssemblyTest.cs:221-255` covers nested and trailing orderings and asserts brackets stay balanced. |
| The guard could be bypassed rather than removed | `PrepareGenericQuery` is `private static` with exactly two call sites (`:1961`, `:1962`), both inside `PrepareQuery`. There is no other producer of the type or state clause. `[READ]` |

### 4b. The empty JQL is the case that is genuinely not knowable from here

An empty `jql=` is not a syntax error; it is a query with no `whereClause`, and Jira's behaviour for
it is **an instance setting**:

> "either returning no results at all (ON), or all existing issues (OFF)" … "The default state is
> OFF, since some plugins may be using empty JQL queries on purpose."
> — [JRASERVER-65602, *As a Jira Administrator I want to make empty JQL return no results*][jra65602],
> shipped in Jira Software 7.9 and backported to 7.2.14 / 7.3.10 / 7.4.7 / 7.5.5 / 7.6.6 / 7.7.4 / 7.8.2

**On an instance with that toggle ON, the query Lighthouse was measured to emit (§3) would return
zero issues, and `WorkItemService.cs:212-222` would then delete every stored work item for that
team.** With it OFF, the same query returns every issue the credential can see — every project,
every type, every state.

Precisely what is and is not measured here, because the distinction matters: **`[MEASURED]`** — that
Lighthouse puts `jql=` on the wire, and the exact request line (§3). **`[CITED]`** — that Jira
answers an empty JQL with either nothing or everything, per the toggle. **`[READ]`** — that a
zero-record answer reaches `RemoveItemsThatLeftTheQuery` with no floor. **Not measured** — Jira's
actual response to this request; no live call was made, and none is needed, because both documented
answers are unacceptable.

There is a second, weaker report that on Cloud an explicitly empty `jql=` parameter returns no
results regardless of any toggle (community answers on `/rest/api/2/search`). That one is **not
settled** from primary documentation and is marked `[INFERRED]`; the enhanced-search endpoint's
reference page could not be read in full from here.

**Safe assumption, stated plainly:** Lighthouse must never emit an empty query, because both
documented outcomes are unacceptable — one deletes the customer's history, the other pulls in
records from projects they never configured. This does not need resolving against a live instance to
act on.

---

## 5. Question 1, answered: is an empty list reachable?

**Yes — but not through any screen.** Route by route:

| Route | Reachable? | Evidence |
|---|---|---|
| **Team/Portfolio creation wizard** | **NO** | `useCreateWizard.ts:46-60` `hasEveryConfigInput` requires `toDoStates.length > 0 && doingStates.length > 0 && doneStates.length > 0`, and `workItemTypes.length > 0` unless `schema.isWorkItemTypesRequired === false`. Gates the Next button at `CreateWizardShell.tsx:289`, and re-checked on the auto-jump at `useCreateWizard.ts:236-237`. Tested: `useCreateWizard.test.ts:288-304` `[READ]` |
| **Team / Portfolio settings screens** | **NO** | Autosave is the only write path (`ModifyTeamSettings.test.tsx:370-374` asserts there is no Save and no Validate button). The autosave effect returns early unless `formValid` (`useModifySettings.ts:300-317`), and `formValid` is `validateForm(...).length === 0` (`:162-165`). Blockers at `ModifyTeamSettings.tsx:66-82` and `ModifyProjectSettings.tsx:76-92`. Tested: `ModifyTeamSettings.test.tsx:376-400`, `ModifyProjectSettings.test.tsx:605-628` `[READ]` |
| **The API directly** (`POST /api/teams`, `PUT /api/team/{id}`, and the portfolio equivalents) | **YES — no enforcement of any kind** | `SettingsOwnerDtoBase.cs:50,52,54,56` — four plain `List<string>` properties, **no `[Required]`, no `[MinLength]`**; `System.ComponentModel.DataAnnotations` is not even imported. The four hits for that namespace in the whole backend are all `.Schema` (`[NotMapped]`/`[Column]`). `[ApiController]` is present (`TeamsController.cs:22`) but has nothing to validate. No `ModelState.IsValid` anywhere in `API/`. FluentValidation is not referenced by any `.csproj`. `CreateTeam` (`TeamsController.cs:89-136`) validates baselines, state *mappings* and the licence, then `newTeam.SyncTeamWithTeamSettings(teamSetting)` at `:109`. `TeamExtensions.cs:42` `team.WorkItemTypes = teamSetting.WorkItemTypes;` and `:68-70` `team.ToDoStates = TrimListEntries(teamSetting.ToDoStates);` are unconditional copies. `[READ]` |
| **The database** (an existing row already empty) | **YES, and it round-trips** | The columns are `IsRequired()` → `NOT NULL`, with no `CHECK`: Postgres `PrimitiveCollection<List<string>>("WorkItemTypes") … HasColumnType("text[]")` (`Lighthouse.Migrations.Postgres/.../LighthouseAppContextModelSnapshot.cs:1161-1163, 1405-1407`); SQLite `PrimitiveCollection<string>(…) … HasColumnType("TEXT")` (`Lighthouse.Migrations.Sqlite/.../LighthouseAppContextModelSnapshot.cs:1107-1109, 1343-1345`). Postgres stores `{}`, SQLite stores `[]`. The 2024-10-26 migration `AddStatesToWorkTrackingSystemOptionsOwner.cs:18,25,32` backfilled **non-empty** defaults onto existing rows, so no *historical* row was created empty by a schema change — but nothing stops one being written empty afterwards. `[READ]` |
| **Database restore** | **YES, and it bypasses every check above** | `DatabaseManagementService.cs:89-137` — unzip, `provider.RestoreBackup(extractDir)` at `:113`, then migrate/seed. No per-entity validation. `[READ]` |
| **Tracker-side change** (board columns removed, mapping resolves to nothing) | **Partly — one live route, currently blocked at the controller** | Lighthouse never recomputes `WorkItemTypes`/state lists from the tracker during a refresh; they are configuration and only a save changes them. **But** `AllStates` is *derived*: `WorkTrackingSystemOptionsOwner.cs:33-35` → `GetRawStatesForCategory` `:98-118`, which substitutes `mapping.States` for any entry naming a `StateMapping`. A fully-populated trio whose every entry names a mapping with an **empty** `States` list resolves to an empty `AllStates` at query-build time. `StateMappingValidator.cs:33-36` rejects `mapping.States.Count == 0` at both controllers — so this is closed via the API, **open via a DB restore**, and it is not an entity invariant. `[READ]` |
| **Demo-data seeder** | **NO** | `DemoDataFactory.cs:28-31` and `:47-50` always set non-empty lists; the seeders under `Services/Implementation/Seeding/` never touch these fields. `[READ]` |
| **CSV / ADO / Linear / ServiceNow owners** | **Same as any other owner** — they share `Team`/`Portfolio` and the same DTO. For **Linear team, Linear portfolio and ServiceNow portfolio**, `IsWorkItemTypesRequired = false` (`DataRetrievalSchemaDto.cs:59, 124, 142`), so an empty `WorkItemTypes` is a **UI-blessed, legitimate** state there. `[READ]` |

### What that means for the fix

The empty list is **not** a shape any operator can reach by clicking. It is reachable by
(a) any API caller — an API key, the Lighthouse CLI/MCP client, a script, a `curl`; and
(b) a restored database. The frontend is the *only* thing enforcing the rule, and the backend
publishes that rule to the frontend and then declines to check it:

> `/// What a team's settings screen asks for, and what it refuses to save without. Twinned in`
> `/// DataRetrievalSchemaDefaults.ts; the two disagreeing is Bug #5613.`
> — `DataRetrievalSchemaDto.cs:27-29` `[READ]`

Not measured, and worth checking before the fix ships: whether the Lighthouse-Clients CLI/MCP can
write team/portfolio settings, and whether it reproduces the frontend's gate. That repo is not in
this worktree. `[INFERRED — follow-up]`

---

## 6. Question 4, answered: the same hole in the other connectors

The five connectors split on a question nobody ever decided: **which way does an empty collection
fail?**

| Connector | Empty `WorkItemTypes` | Empty `AllStates` | Guard | Verdict |
|---|---|---|---|---|
| **Jira** | all items (clause dropped) | all items (clause dropped) | `options.Any()` `:2008` — fails **open** | the filed bug |
| **Azure DevOps** | all items | all items | `options.Any()` `AzureDevOpsWorkTrackingConnector.cs:1334` — byte-identical shape | **must fix with this** |
| **Linear — team read** | n/a (types unused) | **ZERO items, silently** | **none** | **separate item, higher severity** |
| Linear — portfolio read | n/a | all features | `states.Count > 0 &&` `LinearWorkTrackingConnector.cs:137` | already correct |
| **ServiceNow** | ZERO — deliberate, logged, no HTTP call | **ZERO items, silently** | types: `ServiceNowReadScope.cs:61` + `ServiceNowWorkTrackingConnector.cs:277-284`; states: **none** | **states are a separate item** |
| **CSV** | ZERO | ZERO | fetch path: **none**; `ValidateTeam` only (`CsvWorkTrackingConnector.cs:162-176`) | **separate item** |

- **ADO — must fix with this.** `AzureDevOpsWorkTrackingConnector.cs:1330-1337` is the same method
  with the same guard, reached from `:288`/`:327` (fetch) and `:1257`/`:1260` (sweep). It is the only
  other connector with a **live sweep**, so it is the only other one where the filed bug's exact
  chain applies. Its all-empty degenerate is worse than Jira's: `:1296-1299` builds
  `… FROM WorkItems WHERE ({query}) …`, so an empty `DataRetrievalValue` yields a literal `WHERE ()`
  — invalid WIQL, i.e. loud. `[READ]`
- **Linear team read — separate item, but raise it above this one.**
  `LinearWorkTrackingConnector.cs:607-612`: `var states = team.AllStates.ToList(); return issues.Where(i => states.Contains(i.State.Name)).ToList();`
  — no `Count > 0` guard, so an empty list discards every issue. `GetWorkItemsForTeam` then logs
  `"No issues found for team {TeamName}"` at **information** level (`:79-83`) and returns `[]`. The
  same file's class doc at `:50-54` spells out the consequence: *"answering a failed fetch with no
  records deletes every Work Item the team has - and their blocked spells, which no tracker can
  rebuild, do not come back with them."* The guarded portfolio path (`:137`) and the unguarded team
  path (`:611`) are **in the same file**. `[READ]`
- **ServiceNow states — separate item.** `ServiceNowWorkTrackingConnector.cs:300-305` drops every
  record whose `StateCategory` is `Unknown`; with no states mapped, that is all of them, with **no
  log line at all**. The types side is guarded and loud (`:277-284`); the states side is neither.
  `[READ]`
- **CSV — separate item.** `CsvWorkTrackingConnector.cs:222-225`
  `if (!owner.AllStates.IsItemInList(state) || !owner.WorkItemTypes.IsItemInList(type)) { return null; }`,
  and `EnumerableExtensions.cs:5-13` `IsItemInList` is `Any(...)`, false on an empty sequence. Either
  list being empty empties the whole result. The file's own neighbouring comment (`:45-47`) names the
  exact consequence. Its `ValidateTeam` **does** check (`:162-176`) — but validation is a separate
  endpoint the save path never calls (§5). `[READ]`

**Mitigating the severity of the three "separate items" — and the expiry date on that mitigation.**
None of Linear, ServiceNow or CSV supports incremental sync (`SupportsIncrementalSync => false`;
every `Sweep*` throws `NotSupportedException`), so today they reach the deletion through the
full-fetch path rather than the sweep. Same destination, same `WorkItemService.cs:212-222`.

But the mitigation is **scheduled to be removed**, and the code says so:

> `/// <summary>Epic #5687 slice 08 turns this on for Linear; until then the sweep is unreachable.</summary>`
> — `LinearWorkTrackingConnector.cs:38-40` `[READ]`
> `/// <summary>Epic #5687 slice 07 turns this on for ServiceNow; until then the sweep is unreachable.</summary>`
> — `ServiceNowWorkTrackingConnector.cs:115-117` `[READ]`

When slices 07 and 08 land, Linear's and ServiceNow's unguarded empty-collection behaviour becomes
reachable through the sweep as well — which is the path where "stored minus swept" is at its most
literal. **Fix the Linear and ServiceNow guards before those slices, not after.** That is a stronger
reason to raise Linear above P3 than the current exposure is.

---

## 7. Root-cause chains

```
PROBLEM: The guard at :2008 is correct and unprotected. Removing it produces a query Jira refuses
         outright (§4a) - a loud failure, not the silent deletion the code comment predicts. The
         silent deletion is real, but it lives one layer out (the empty query, §3/§4b) and in three
         other connectors (§6), and none of those is protected either.
```

### Chain A — no test pins the guard (the bug as filed)

- **WHY 1A** — Stryker forces `options.Any()` true and the mutant survives.
  `[Evidence: no test in `Lighthouse.Backend.Tests` builds an owner with an empty types or states
  list; every `WorkItemTypes.Clear()` is followed by an `.Add(...)` —
  `JiraBoardQueryAssemblyTest.cs:637-638, 649-650`. `[READ]`]`
- **WHY 2A** — Every Jira fixture starts from one builder that seeds exactly one type and three
  states, and no fixture models the empty shape.
  `[Evidence: `JiraConnectorTestSetup.cs:133-141` (team) and `:106-114` (portfolio) each `Clear()`
  then `Add()` a value into all four lists. `[READ]`]`
- **WHY 3A** — The project *has* the machinery to pin an emitted query and uses it for the rules
  next door, but not for this one.
  `[Evidence: `JiraBoardQueryAssemblyTest.cs:673-687` `TheQueryIssuedFor` reads the JQL back off the
  request URL; it is used to pin `OR` between states (`:552-561`), quote escaping (`:634-643`) and
  backslash escaping (`:645-655`) — never emptiness. `[READ]`]`
- **WHY 4A** — The author wrote the guard from a hazard they had reasoned about rather than one they
  could observe, and said so in the code.
  `[Evidence: `:1975` "what Jira does with one is not knowable from here". `[READ]`]`
- **WHY 5A** — The convention in this codebase is *"pin the emitted string when you can say what the
  remote does with it"*. A rule whose wrong output was believed unobservable fell outside the
  convention, so no assertion was written — even though the assertion needed is on **Lighthouse's own
  output**, which is entirely observable and needs no knowledge of Jira at all.
  `[Evidence: the three pinned rules above all assert a substring the author could justify from
  Jira's documented syntax; the guard asserts only that a substring is *absent*, which none of them
  do. `[READ]` `[INFERRED]` for the intent.]`

→ **ROOT CAUSE A: "Can I say what the remote does with it?" was used as the test for whether a
query-shaping rule deserves a regression test. The right test is "can the emitted string be
wrong?" — which needs nothing from the remote.**

### Chain B — the empty list is reachable because validation is placed by screen, not by invariant

- **WHY 1B** — A `POST`/`PUT` with `"workItemTypes": []` and empty state arrays is accepted and
  persisted.
  `[Evidence: `SettingsOwnerDtoBase.cs:50-56` carries no validation attribute; `TeamsController.cs:101-109`
  validates baselines and state *mappings* only, then copies; `TeamExtensions.cs:42, 68-70` copy
  unconditionally. `[READ]`]`
- **WHY 2B** — The non-empty rule exists and is authored on the backend, but only as data shipped to
  the UI.
  `[Evidence: `DataRetrievalSchemaDto.cs:17` `IsWorkItemTypesRequired`; `:27-29` "what it refuses to
  save without … Twinned in `DataRetrievalSchemaDefaults.ts`". Nothing on the backend reads the flag
  to gate a save — grep for it returns only the DTO itself and a ServiceNow comment. `[READ]`]`
- **WHY 3B** — Where a connector *does* check, the check sits in `Validate*Settings`, which the save
  path never calls.
  `[Evidence: `CsvWorkTrackingConnector.cs:162, 170` and `ServiceNowWorkTrackingConnector.cs:791` are
  the only non-empty checks in the backend. The only production call sites of `ValidateTeamSettings`
  / `ValidatePortfolioSettings` are `TeamsController.cs:153` and `PortfoliosController.cs:126`, both
  inside a separate `[HttpPost("validate")]` action. Calling it is client-driven and optional.
  `[READ]`]`
- **WHY 4B** — An intent to refuse at save time was recorded, and never implemented.
  `[Evidence: `ServiceNowReadScope.cs:57-59` — "A team that named no kinds of work. It reads nothing
  and is refused at save time (ADR-123 decision 4)". There is no save-time refusal anywhere.
  `[READ]`]`
- **WHY 5B** — The system has six writers of this configuration (two wizards, two settings screens,
  the HTTP API, a database restore) and no layer that owns the invariant, so each writer decides for
  itself. Two decided to enforce it; four did not.
  `[Evidence: the §5 route table. `[READ]`]`

→ **ROOT CAUSE B: "A query owner must name at least one work item type (where the connector requires
types) and at least one state" is a domain invariant that no layer owns. It is currently enforced
only in the React components that happen to be the usual writer.**

### Chain C — the empty query, one step past the guard

- **WHY 1C** — With all four lists and the operator's query empty, the connector issues
  `jql=` (empty).
  `[Evidence: §3, `[MEASURED]`: `/rest/api/3/search/jql?jql=&fields=%2Aall&expand=changelog&maxResults=10`]`
- **WHY 2C** — `PrepareQuery` returns the empty string and nothing downstream refuses it.
  `[Evidence: `:1965` builds `lighthousesOwnFilters` from three empty strings; `:1967-1968` routes to
  `WithoutTheLeadingConjunction`, which at `:1983-1987` finds no leading `AND ` and returns the
  trimmed input — `""`. `GetIssuesByQuery` `:1602-1616` logs and dispatches without checking.
  `[READ]`]`
- **WHY 3C** — `PrepareQuery`'s contract is "assemble whatever the owner configured". It has no
  post-condition asserting the result selects a proper subset.
  `[Evidence: `:1947-1958`, the method's doc comment, is entirely about *agreement between the sweep
  and the download* — the invariant it protects is "both ask the same question", not "the question
  is answerable". `[READ]`]`
- **WHY 4C** — The author reasoned one level down (the bracket pair) and stopped; the same reasoning
  applied to the whole assembled string would have caught this.
  `[Evidence: `:1978` — "Leaving the pair out asks the question Lighthouse's own filters ask and
  nothing more, which at worst over-fetches." That sentence is true only while Lighthouse's own
  filters are non-empty. When they are all empty there is no question left to ask. `[READ]`]`
- **WHY 5C** — The guard is a repair at the **fragment** level. The property that actually matters
  — *the assembled query must select something narrower than the whole instance* — lives at the
  **whole-query** level, and no code owns it.

→ **ROOT CAUSE C: The invariant was enforced on the part instead of the whole. Guarding each
fragment individually cannot express "the conjunction of all fragments must be non-vacuous".**

### Chain D — the other connectors fail the opposite way, and already delete

- **WHY 1D** — Linear's team read, ServiceNow's state filter and CSV's row filter each return zero
  records for an empty collection, with no exception and (for two of them) no warning.
  `[Evidence: `LinearWorkTrackingConnector.cs:611`; `ServiceNowWorkTrackingConnector.cs:300-305`;
  `CsvWorkTrackingConnector.cs:222-225` + `EnumerableExtensions.cs:5-13`. `[READ]`]`
- **WHY 2D** — All three filter **in memory** with `Contains`/`Any` over the configured list, and an
  empty list makes that predicate false for every record. The two that build a **query string**
  instead have an "omit the clause" step, which is where a guard can live.
  `[Evidence: the two shapes side by side — `:2008` (`options.Any() ? … : string.Empty`) versus
  `LinearWorkTrackingConnector.cs:611` (`states.Contains(...)`). `[READ]`]`
- **WHY 3D** — Each connector inherited the default of its own mechanism rather than a shared rule:
  query-building defaults to fail-open, predicate-filtering defaults to fail-closed.
  `[Evidence: Linear holds **both** answers in one file — `:137` `states.Count > 0 &&` on the
  portfolio path, `:611` unguarded on the team path. `[READ]`]`
- **WHY 4D** — `IWorkTrackingConnector` specifies *what* to fetch, never *what an empty configuration
  means*. Where the rule was written down at all, it was written as a per-connector comment.
  `[Evidence: three separate comments each restating the removal model for their own connector —
  `:82-87` and `:91-94` (Jira), `LinearWorkTrackingConnector.cs:50-54`, `CsvWorkTrackingConnector.cs:45-47`.
  Three copies of one rule is how a rule ends up applied inconsistently. `[READ]`]`
- **WHY 5D** — There is no cross-connector contract for the empty-collection case, and no shared test
  that would have made the divergence visible.

→ **ROOT CAUSE D: The connector port does not state what an empty mapped collection means, so five
implementations answered it four different ways, and three of those answers are silent deletion.**

### Chain E — removal has no floor

- **WHY 1E** — A fetch that legitimately returns zero records deletes every stored record for that
  owner.
  `[Evidence: `WorkItemService.cs:212-222` — `FindAll(stored => !stillOnTheTracker.Contains(...))`
  then `workItemRepository.Remove(...)`, no floor, no threshold, `LogDebug` per item. `[READ]`]`
- **WHY 2E** — `StillOnTheTracker` is built straight from the fetch result with no provenance.
  `[Evidence: `WorkItemService.cs:284-287`. `[READ]`]`
- **WHY 3E** — This is deliberate and load-bearing: removal must be a difference against *the whole
  query*, never against what was downloaded, or a delta sync would delete everything it did not
  re-download.
  `[Evidence: `WorkItemService.cs:211` doc comment "Removal is a set difference against the whole query, never against
  what was downloaded"; and `WorkItemService.cs:225-230`. `[READ]`]`
- **WHY 4E** — The safety contract that makes this sound is *"a connector must throw rather than
  answer short"*, which covers a **failed** fetch. It does not cover a **successful** fetch of a
  configuration that cannot match anything.
  `[Evidence: the contract is stated three times — `:82-87` and `:91-94`, `LinearWorkTrackingConnector.cs:50-54`,
  `CsvWorkTrackingConnector.cs:45-47` — always about failure. `[READ]`]`
- **WHY 5E** — At the point of deletion, "the tracker genuinely has nothing" and "we asked a question
  that cannot match" are indistinguishable. The one signal that separates them is the owner's
  configuration, and `RemoveItemsThatLeftTheQuery` does not receive it.
  `[Evidence: its signature takes `List<WorkItem>` and `HashSet<string>` only — `WorkItemService.cs:212`. `[READ]`]`

→ **ROOT CAUSE E: The removal step has no way to tell an empty tracker from an unanswerable question,
so every other root cause in this document terminates in the same deletion.**

### Aggravating factor (not a chain of its own)

Emptying any of the four lists changes the fetch fingerprint, which forces the **next** cycle to be a
full download rather than the cheap delta scan. The damaging path therefore fires on the very next
refresh rather than lying dormant.
`[Evidence: `FetchFingerprint.cs:30-34` registers, and `:76-80` digests, `WorkItemTypes`, `ToDoStates`, `DoingStates`,
`DoneStates`; `WorkItemService.cs:119-126` `FetchShape.Of`. `[READ]`]`

### Cross-validation

Forward trace, each root cause to an observed symptom:

| Root cause | If it holds, does it produce the symptom? | Check |
|---|---|---|
| A | No test constructs an empty list ⇒ the `options.Any()` mutant is never killed | **Yes** — grep finds no such test; the mutant survived the #5973 run |
| B | No layer owns the invariant ⇒ an API caller can persist `[]` | **Yes** — `[READ]` at every layer in §5; no attribute, no validator, no ModelState check, no save-time call to `Validate*Settings` |
| C | The invariant is enforced per fragment ⇒ the whole query can still be vacuous | **Yes** — `[MEASURED]`: `jql=` on the wire |
| D | No port-level contract ⇒ connectors diverge | **Yes** — `[READ]`: four different answers, two of them inside one Linear file |
| E | No floor on removal ⇒ any zero-record fetch deletes everything | **Yes** — `[READ]` `WorkItemService.cs:212-222`; and the fetch is awaited at `:140`, two statements before the removal at `:142`, so an *exception* skips the deletion while a zero-record *success* does not |

Consistency between causes — no contradictions:

- A and B are independent and compose badly: B makes the state reachable, A means nothing would catch
  a regression in the one guard that handles it.
- C is not a restatement of A. A is about the `AND ()` fragment; C is about the empty whole. §4 shows
  they have **opposite** outcomes: A's is a loud 400, C's is a silent 200. Conflating them is what
  made the original comment overstate one hazard and miss the other.
- D and E compose into today's live exposure without any of A, B or C: an operator who reaches an
  empty state list on Linear (which the frontend does block, but the API does not) loses their team's
  history on the next cycle, mutant or no mutant.

**Do all five together explain every observed symptom?** Yes, with one gap: none of them explains
*why the guard is on the type and state clauses but not on the cutoff filter*
(`PrepareCutoffDateFilter`, `:1990-2003`, returns `string.Empty` for `cutOffDays <= 0`). That is the
same fail-open shape and is correct there — a zero cutoff genuinely means "no cutoff". No gap.

---

## 8. Question 2, answered: where the guard belongs

**Answer: configuration time, at the API save path — with the query-build guard kept as defence in
depth and the whole-query post-condition added as the actual stopgap. Three layers, one of them the
fix and two of them belts.**

Reasoning, not options:

1. **A query-build guard cannot be right for all five connectors, so it cannot be the fix.** §6 shows
   the emptiness question has no single correct answer at query-build time: Jira/ADO drop the clause
   (over-fetch), Linear/ServiceNow/CSV match nothing (deletion). Whichever behaviour is chosen,
   two connectors will be wrong. The only answer that is right in all five is **the configuration
   never reaches them**.
2. **An empty configuration is meaningless, not merely dangerous.** A team that names no states has
   no flow: `IsCycleTimeDefinitionValid` (`WorkTrackingSystemOptionsOwner.cs:120-128`) reports every
   cycle-time definition invalid, `TeamMetricsService.cs:362, 381, 536` computes over an empty state
   order, and `MapStateToStateCategory` returns `Unknown` for everything. There is nothing to
   preserve by accepting it. `[READ]`
3. **The failure must be attributable to the person who caused it.** At query-build time the
   consequence lands on a background refresh, hours later, as a `LogDebug` per deleted item. At save
   time it lands as a 400 on the request that caused it, naming the field.
4. **A test that only pins the current query-build guard would bless the wrong layer.** It asserts
   "when the configuration is impossible, we ask a wider question than the operator configured" —
   which is a behaviour worth pinning as a fallback, but is not a behaviour worth *specifying*. The
   real hole (§5) is that the impossible configuration can be saved at all.
5. **Configuration-time validation alone is not sufficient, because of the restore path.** A restored
   database (`DatabaseManagementService.cs:89-137`) never passes through a controller. That is why
   the two lower layers stay.

The three layers, in order of load-bearing:

| | Layer | Rule | Why it is there |
|---|---|---|---|
| **L1 — the fix** | API save path (both create + both update actions, or the two `Sync*` extensions they all funnel through) | Refuse a save where all three state lists are empty, or where `WorkItemTypes` is empty **and** `DataRetrievalSchemaDto.For*(system).IsWorkItemTypesRequired` is true. Return 400 naming the field, matching `StateMappingValidator`'s existing shape (`StateMappingValidator.cs:33-36`) | Closes the reachable route; makes the failure loud and attributable; reuses the flag the backend already authors for the UI, closing the "twinned rule" gap named in `DataRetrievalSchemaDto.cs:27-29` |
| **L2 — belt** | `PrepareQuery` (`:1959`) and the ADO equivalent | Refuse to issue a query that is empty or whitespace-only. **Reuse the existing type, do not add one:** `WorkTrackingReadException` (`Services/Interfaces/WorkTrackingConnectors/WorkTrackingReadException.cs:11`) is the shared abstract base carrying a `ConnectionValidationResult`, and `JiraReadException` (`Jira/JiraReadException.cs:13`), `AzureDevOpsReadException` (`AzureDevOps/AzureDevOpsReadException.cs:14`) and `ServiceNowReadException` (`ServiceNow/ServiceNowReadException.cs:13`) all already derive from it — **no new exception type is needed for any connector**. `ValidateTeamSettings` already catches it at `:1075` and returns its verdict, so the settings screen gets a named message for free; on the refresh path it propagates and aborts before the removal | The only layer that catches the **measured** live hole (§3, §4b) and the restore path |
| **L3 — belt** | `PrepareGenericQuery` (`:2008`) and `AzureDevOpsWorkTrackingConnector.cs:1334` | Keep exactly as is; add the regression test in §9 | Unchanged behaviour, now protected |

**Not recommended:** a floor in `RemoveItemsThatLeftTheQuery` ("never delete more than N% in one
cycle"). It would mask Root Cause E rather than address it, and it would break the legitimate case of
a team whose query correctly goes to zero. If Root Cause E is to be addressed directly, the right
shape is for the connector to *say* it asked an unanswerable question, not for the remover to guess.
That is a separate item.

---

## 9. Question 5, answered: the minimal test that kills the mutant

**Public entry point:** `JiraWorkTrackingConnector.ValidateTeamSettings(Team)` (`:1052`) — it calls
`PrepareQuery` at `:1058` and then `GetIssuesByQuery`, which puts the JQL in the request URL.
`ValidatePortfolioSettings` (`:1093`) is the portfolio twin.

**Where it belongs:** `JiraBoardQueryAssemblyTest.cs`, which already owns "what the assembled query
looks like" and already has the capture helper — `TheQueryIssuedFor(Team)` at `:673-687`, which
reads the JQL back off the `rest/api/3/search/jql` URL. Adding to that fixture keeps the four
query-assembly rules (OR-joining, quote escaping, backslash escaping, emptiness) in one place.

**Confirmed reachable offline:** yes — I ran exactly this shape in this worktree against a stub
`HttpMessageHandler`, with the live-connector categories excluded, and captured the JQL (§3). No
network call. `[MEASURED]`

```csharp
/// <summary>
/// A team can reach the settings API with no states mapped, and the clause built from them is then
/// built from nothing. Emitting the bracket pair anyway writes an expression Jira cannot parse, and
/// a team whose refresh fails outright records nothing at all. Asking for the absent bracket pair,
/// rather than for the clause that replaces it, is what keeps this from passing on a query that
/// happens to contain the right substring somewhere else.
/// </summary>
[Test]
public async Task ValidateTeamSettings_NoStatesMapped_LeavesTheStateClauseOutRatherThanEmpty()
{
    var team = JiraConnectorTestSetup.ATeamOnJiraCloud();
    team.ToDoStates.Clear();
    team.DoingStates.Clear();
    team.DoneStates.Clear();

    var jql = await TheQueryIssuedFor(team);

    using (Assert.EnterMultipleScope())
    {
        Assert.That(jql, Does.Not.Contain("()"));
        Assert.That(jql, Does.Contain("AND (issuetype = \"Story\")"));
    }
}

[Test]
public async Task ValidateTeamSettings_NoWorkItemTypesMapped_LeavesTheTypeClauseOutRatherThanEmpty()
{
    var team = JiraConnectorTestSetup.ATeamOnJiraCloud();
    team.WorkItemTypes.Clear();

    var jql = await TheQueryIssuedFor(team);

    using (Assert.EnterMultipleScope())
    {
        Assert.That(jql, Does.Not.Contain("()"));
        Assert.That(jql, Does.Contain("AND (status = \"To Do\" OR status = \"In Progress\" OR status = \"Done\")"));
    }
}
```

**Why these kill the mutant.** `[MEASURED]` — today the first emits
`(project = PROJ) AND (issuetype = "Story")   `; under the mutant `string.Join` over an empty
sequence is `""`, so the true-branch yields `AND () ` and `Does.Not.Contain("()")` fails. The second
assertion in each pair is what stops the test passing for the wrong reason — a future change that
dropped *both* clauses would satisfy the negative assertion alone.

**One test is not enough, and this is the part the bug's title obscures.** The two above pin L3 only.
The measured live hole needs its own, and it must be written against L2, not L3:

```csharp
[Test]
public async Task ValidateTeamSettings_NothingConfiguredAtAll_RefusesRatherThanAskingForEverything()
{
    var team = JiraConnectorTestSetup.ATeamOnJiraCloud();
    team.WorkItemTypes.Clear();
    team.ToDoStates.Clear();
    team.DoingStates.Clear();
    team.DoneStates.Clear();
    team.DataRetrievalValue = string.Empty;

    // Today this issues `jql=` and Jira answers with either every issue in the instance or none of
    // them, depending on an instance setting - and "none" is what deletes the team's history.
    var jql = await TheQueryIssuedFor(team);

    Assert.That(jql, Is.Not.Empty);
}
```

That one **fails today** (`[MEASURED]`: the JQL is the empty string) and is the regression test for
the L2 fix.

**One implementation note the shape above hides.** If L2 is built as "`PrepareQuery` throws", then
once it lands `TheQueryIssuedFor` never returns — it propagates, and `Assert.That(jql, Is.Not.Empty)`
never runs. The test must be rewritten at that point to assert on the **verdict**
(`ValidateTeamSettings` catches `WorkTrackingReadException` at `:1075` and returns
`refusal.Verdict`), or on the thrown type from `GetWorkItemsForTeam`. Written as above it is a
*red* test that documents today's behaviour; it is not the shape it will keep. Decide which of the
two before writing it, or it will be rewritten twice.

Plus the L1 counterparts, which belong with the controller tests rather than here: a `POST`/`PUT`
with empty lists must answer 400, and must **not** answer 400 where `IsWorkItemTypesRequired` is
false.

---

## 10. Question 6, answered: risk of configuration-time validation

| # | Risk | Assessment | Evidence |
|---|---|---|---|
| R1 | **Existing rows already empty become unsaveable — the operator is locked out of their own settings screen** | **Real, and the one that must be designed for.** The settings screens already refuse to autosave an empty list and show a blocking warning naming the missing field (`ModifyTeamSettings.tsx:332-348`, `data-testid="settings-blocking-warning"`), so a user with an already-empty row **is already in this state today** and can fix it by adding an entry. Adding L1 does not change their experience. **But** the L1 rule must be scoped to *the fields being written*, not to the whole entity: a `PUT` that is otherwise valid must not be refused because some **unrelated** empty collection already existed. Validate the four collections in the incoming DTO; do not re-validate the stored row. | `[READ]` `ModifyTeamSettings.tsx:66-82`, `useModifySettings.ts:300-317` |
| R2 | **Mid-wizard users** | **None.** The wizard cannot reach its create step with an empty list (`useCreateWizard.ts:46-60`, gate at `CreateWizardShell.tsx:289`, re-checked at `:236-237`), so no in-flight wizard can produce a payload L1 would reject. L1 becomes a second, matching gate behind a gate that already holds. | `[READ]` |
| R3 | **Linear and ServiceNow portfolios legitimately have no work item types** | **Real, and it is why the types rule must be conditional.** `IsWorkItemTypesRequired = false` for Linear team (`DataRetrievalSchemaDto.cs:59`), Linear portfolio (`:124`) and ServiceNow portfolio (`:142`). An unconditional `WorkItemTypes` rule would break all three. Read the flag rather than restating it — the whole point of L1 is to stop the rule existing in two places. | `[READ]` |
| R4 | **Restored databases with empty rows** | **Real, and unaddressed by L1** — `DatabaseManagementService.cs:113` restores files wholesale. This is exactly what L2 is for. Do not ship L1 without L2. | `[READ]` |
| R5 | **The API is a public, documented surface; a new 400 is a breaking change for existing callers** | **Low but not zero.** Any caller currently sending empty lists is already producing either a silently widened query (Jira/ADO) or silent deletion (Linear/ServiceNow/CSV), so a 400 is strictly better than what they get now. Worth a release-note line. Whether the Lighthouse-Clients CLI/MCP sends these fields, and whether it reproduces the frontend's gate, is **not measured here** — that repo is not in this worktree. Check before shipping. | `[INFERRED]` |
| R6 | **L2 turns a currently-"successful" refresh into a failing one** | **Intended, and the safe direction.** A refresh that throws skips `RemoveItemsThatLeftTheQuery` entirely — the fetch is awaited at `WorkItemService.cs:140`, two statements before the removal at `:142` — so the stored history survives while the operator fixes the configuration. That is the opposite of today's behaviour, where a vacuous query succeeds and deletes. | `[READ]` — the ordering is read off the source; it was **not** exercised by the probe, which only ever answered 200 |
| R7 | **The ADO change lands in a connector whose only tests are live-category** | **CLAIM RETRACTED — I asserted this and then measured it false.** It is true that `AzureDevOpsWorkTrackingConnectorTest.cs:15-16` is `[Category("Integration")] [Category("AdoIntegration")]` and that `AzureDevOpsWriteBackTest.cs:17` is `AdoIntegration`. But **six** ADO fixtures in the same folder carry no live category at all — `AzureDevOpsDependencyRelationTest.cs`, `AzureDevOpsFetchRefusalTest.cs`, `AzureDevOpsBatchedWriteBackTest.cs`, `AzureDevOpsCancellationGranularityTest.cs`, `AzureDevOpsHistoryReadTest.cs`, `AzureDevOpsIncrementalSyncTest.cs` (their only `[Category]` attributes are epic/slice tags). `AzureDevOpsIncrementalSyncTest.cs` already records every emitted WIQL in `ado.WiqlQueries` (`:94`, `:252`, `:306`). **The ADO half needs no new fixture, and the risk is not real.** Recorded rather than deleted because the brief asked for claims measurement overturned to be visible. | `[READ]` — retraction measured by enumerating `[Category(` in every file in the folder |

**Prioritisation.**

| Priority | Item | Rationale |
|---|---|---|
| **P1** | L2 (refuse an empty assembled query) in Jira **and** ADO, with the failing test in §9 | The only **measured**, currently-live path to mass deletion in the two connectors that have a sweep |
| **P1** | Linear team-read state guard (`LinearWorkTrackingConnector.cs:611`) | Silent deletion today, zero conditions required beyond an empty state list; the correct guard already exists 470 lines above it at `:137`. **Epic #5687 slice 08 turns Linear's sweep on** (`:38-40`), after which this becomes a sweep-path defect too |
| **P2** | L1 (save-time refusal), reading `IsWorkItemTypesRequired` | Closes the reachable route; prevents recurrence rather than mitigating it |
| **P2** | L3 regression tests (§9), Jira + ADO | The bug as filed |
| **P2** | ServiceNow empty-states guard (`ServiceNowWorkTrackingConnector.cs:300-305`) | Silent deletion; requires the API-direct route, but **Epic #5687 slice 07 turns ServiceNow's sweep on** (`:115-117`) and this must be guarded before that lands |
| **P3** | CSV fetch-path guard (`CsvWorkTrackingConnector.cs:222-225`) | Silent deletion, API-direct route only, and CSV has no scheduled sweep |
| **P3** | State the empty-collection contract on `IWorkTrackingConnector` and pin it with one shared test across all five | Root Cause D; prevents the next connector from inventing a fifth answer |
| **P3** | Correct the comment at `:1975-1978` | It currently names a hazard that §4 shows is not real, and omits the one that is |

---

## 11. Root causes → solutions

| Root cause | Solution | Type |
|---|---|---|
| **A** — regression tests were gated on "can I say what the remote does?" rather than "can the output be wrong?" | §9 tests (Jira + ADO). Record the rule: a query-shaping branch is pinned by asserting on **Lighthouse's own emitted string**; what the remote does with it is a separate question | Permanent fix + prevention |
| **B** — no layer owns the "at least one type, at least one state" invariant | **L1**: refuse at the API save path, reading `DataRetrievalSchemaDto.IsWorkItemTypesRequired` so the rule exists once | Permanent fix |
| **C** — the invariant was enforced per fragment, not per whole query | **L2**: post-condition on `PrepareQuery` / the ADO equivalent — never issue an empty query | Permanent fix; also the immediate mitigation for the measured hole |
| **D** — the connector port does not state what an empty collection means | State it on `IWorkTrackingConnector`; guard Linear's team read now; ServiceNow states and CSV as follow-ups; one shared test across all five | Permanent fix (P1 for Linear, P3 for the contract) |
| **E** — removal cannot tell an empty tracker from an unanswerable question | Out of scope here. The right shape is a connector-reported "this configuration selects nothing" signal, not a percentage floor on the remover. Raise as a separate item | Early detection (separate item) |

**Early detection, independent of all five:** `RemoveItemsThatLeftTheQuery` currently logs one
`LogDebug` per deleted item (`WorkItemService.cs:219`). One `LogWarning` when a cycle removes
*every* stored record for an owner would have made all four holes visible in a log the operator
reads, at essentially no cost, without changing any behaviour.

---

## 12. Files affected by the fix

| File | Change |
|---|---|
| `.../Jira/JiraWorkTrackingConnector.cs` | L2 post-condition in `PrepareQuery` (`:1959`); correct the comment at `:1975-1978` |
| `.../AzureDevOps/AzureDevOpsWorkTrackingConnector.cs` | L2 post-condition on the WIQL assembly; `:1334` unchanged |
| `.../Linear/LinearWorkTrackingConnector.cs` | `:611` — guard the team-path state filter as `:137` already guards the portfolio path |
| `.../API/TeamsController.cs`, `TeamController.cs`, `PortfoliosController.cs`, `PortfolioController.cs` (or the `Sync*` extensions they funnel through) | L1 save-time refusal |
| `Lighthouse.Backend.Tests/.../Jira/JiraBoardQueryAssemblyTest.cs` | The three tests in §9 |
| `Lighthouse.Backend.Tests/.../AzureDevOps/AzureDevOpsIncrementalSyncTest.cs` | The ADO twins of the §9 tests. **No new fixture needed** — this one is uncategorised (offline) and already records every emitted WIQL in `ado.WiqlQueries` (`:94`, `:252`, `:306`), which is the ADO equivalent of `TheQueryIssuedFor` |
| Controller test fixtures | L1: 400 on empty lists, and **no** 400 where `IsWorkItemTypesRequired` is false |

---

## 13. What this investigation did not settle

1. **Cloud's behaviour for an explicitly empty `jql=` parameter.** The instance-setting behaviour is
   documented for Data Center ([JRA-65602][jra65602]); the Cloud enhanced-search reference could not
   be read in full from here, and the community reports are second-hand. `[INFERRED]` — but the fix
   does not depend on it, because both possible answers are unacceptable.
2. **The provenance of the guard.** `git log -L` on `:2005-2010` could not be run: this session's
   shell refuses git invocations in the worktree. Whether the guard was written with the hazard in
   mind or added later is unknown. `[NOT MEASURED]`
3. **Whether Lighthouse-Clients (CLI/MCP) writes these fields**, and whether it reproduces the
   frontend's gate. Separate repository. `[NOT MEASURED]` — a prerequisite for sizing R5.
4. **The exact Stryker mutant id and its status line.** The archived artefact in this repo is the run
   config, not the report (§2, claim 2). Survival is established by grep, not by reading a report.

---

## Sources

- [jqlparser]: Atlassian, *JqlParser (Atlassian Jira - Server 9.4.5 API)* —
  `https://docs.atlassian.com/software/jira/docs/api/9.4.5/com/atlassian/jira/jql/parser/antlr/JqlParser.html`
  (the generated `FOLLOW_*` field names from which §4a reconstructs `subClause`, `orClause`,
  `andClause`, `notClause`, `terminalClause`)
- [community-rparen]: Atlassian Community, *Error in the JQL Query: Expecting a field name but got
  ')'*, 2019-08-20 —
  `https://community.atlassian.com/forums/Jira-questions/Error-in-the-JQL-Query-Expecting-a-field-name-but-got-You-must/qaq-p/1157925`
- [jra65602]: Atlassian, *JRASERVER-65602 — As a Jira Administrator I want to make empty JQL return
  no results* — `https://jira.atlassian.com/browse/JRASERVER-65602`
  ("either returning no results at all (ON), or all existing issues (OFF)"; default OFF; Jira
  Software 7.9 and backports)
- Predecessor RCA, for method and evidence discipline:
  `docs/feature/fix-jira-field-lookup-error-message/rca.md`

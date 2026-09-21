# Feature Delta — story-6055-activity-names-the-work

**ADO**: User Story #6055 — *Activity shows Portfolios twice* · Active · tagged `Release Notes` · no
parent · reported by Benj Huser with a screenshot, 2026-09-20.

**Waves**: DISCUSS (2026-09-21).

**One line**: the Task Manager's Activity list names the entity a piece of work is *about* and never
the work itself, so a Portfolio being refreshed and the forecast that refresh triggers draw two rows
with the same text — and the second one says it is queued behind a name that is its own.

**Density**: `lean` + `ask-intelligent` (`~/.nwave/global-config.json`, resolved as
`Density(mode='lean', expansion_prompt='ask-intelligent', provenance='explicit_override')`). Tier-1
`[REF]` only.

---

## Wave: DISCUSS / [REF] Persona IDs

| Persona | Role here |
|---|---|
| `platform-operator` | **Primary and only.** The Activity list is System-Administrator-only and exists to answer "is it working or is it wedged". Both flavours — the self-hoster and the SaaS operator on Tenant Zero. |

No new persona, and no new job: this is the anxiety the Epic that built this surface wrote down and
shipped anyway (see JTBD below).

---

## Wave: DISCUSS / [REF] JTBD One-Liners

| Job ID | One-liner |
|---|---|
| `job-operator-see-what-lighthouse-is-doing-right-now` | When I am wondering whether Lighthouse is working or wedged, I want to see what is running and what is waiting without leaving the page I am on, so the question costs me a glance instead of a log download. |

Existing job, created 2026-08-23 in `epic-5511-task-manager`. Its recorded **anxiety** reads: *"That a
list of raw update keys is not actually legible — `UpdateType` has five members and two of them are
delete operations the UI has never had to name."* That anxiety materialised, in a way the Epic did not
predict: the problem is not the two delete members but that **three of the five collapse onto one
word**. The job is not re-scored; a glance that returns an ambiguous answer is the job failing, not a
different job.

---

## Wave: DISCUSS / [REF] Current-State Surface Inventory

Read from the code on 2026-09-21. The report carries a diagnosis; it was re-derived rather than taken.

| # | Fact | Evidence |
|---|---|---|
| S1 | **Five update types, two words.** The row's kind is decided by a two-arm lookup: `Team` and `TeamDelete` render the tenant's Team term, and a `default:` arm sends `Features`, `Forecasts` **and** `PortfolioDelete` to the Portfolio term. Three types, one word. | `ActivitySection.tsx:37-45` |
| S2 | **A Portfolio refresh enqueues a second, separate piece of work for the same entity.** At the end of its run it calls `forecastUpdater.TriggerUpdate(project.Id)`, which admits `UpdateKey(Forecasts, portfolioId)` — a distinct key with the same id. | `PortfolioUpdater.cs:123`; `ForecastUpdater.cs:139` |
| S3 | **A Team refresh does it too.** `TeamDataRefreshedForecastTriggerHandler` triggers a forecast for every portfolio the team feeds, so a Portfolio can appear in the list while nobody asked for that Portfolio at all. | `TeamDataRefreshedForecastTriggerHandler.cs:16` |
| S4 | **Both resolve to the same display name.** `NameOf` switches on the type only to choose a repository; `Features`, `Forecasts` and `PortfolioDelete` all read `portfolioRepository.GetById(work.Id)?.Name`. | `UpdateTaskNaming.cs` |
| S5 | ∴ **The two rows are byte-identical up to their status.** `Portfolio 'Ocean Explorer' — Running` and `Portfolio 'Ocean Explorer' — Queued behind Ocean Explorer`. The `data-testid` differs (`…-Features-3` vs `…-Forecasts-3`) — the rows are distinguishable to a test and not to a reader. | `ActivitySection.tsx:158-164` |
| S6 | **`waitingBehind` is a bare name chosen from whatever is running.** `admitted.FirstOrDefault(work => work.Status == InProgress)`, then `naming.NameOf(...)`. When the lane holder is the queued row's own entity, the row says it is waiting behind itself. | `UpdateController.cs:61-62,70` |
| S7 | **One lane, still.** `Channel.CreateUnbounded<Func<Task>>()` drained by one `await foreach`. #5877's per-type lanes are **not in the tree**, so S6's "whatever is running" is currently a correct description of the queue and an incorrect description of the row. | `UpdateQueueService.cs:11,610` |
| S8 | **The only disambiguation that exists is a suffix.** Deletes render `(removal)` after the name. Nothing else carries a marker. | `ActivitySection.tsx:163` |
| S9 | **The contract is one producer, one consumer.** `UpdateTaskResponse.WaitingBehind` is a `string?` written in one place and read in one place. Blast radius of changing its shape: `UpdateController.cs:132-138`, `UpdateSubscriptionService.ts:34`, `ActivitySection.tsx:80-81`, plus tests. | grep, 2026-09-21 |
| S10 | **The public docs describe the list as Teams, Portfolios and removals.** Forecasts are never mentioned, and the row-wording table is normative prose a reader is expected to match against their screen. | `docs/settings/taskmanager.md:35,39-46` |
| S11 | **The docs screenshot is generated and shows these rows.** `Screenshots.spec.ts:136-138` opens the Task Manager and writes `docs/assets/settings/taskmanager.png`. | `Screenshots.spec.ts:136` |
| S12 | **"Forecast" is not a configurable term.** `TERMINOLOGY_KEYS` has no forecast entry, so a verb naming it is Lighthouse's own word and needs no lookup. `Team` and `Portfolio` remain tenant-configurable and must stay so. | `TerminologyKeys.ts` |
| S13 | **#5877 shipped and was reverted.** It ran DISCUSS → DESIGN → DISTILL → DELIVER; slice 01 landed on 2026-09-19 and was backed out the same day. Cause: with a lane each, a Portfolio refresh overlaps the Team refreshes feeding it, and `WorkItemService.RefreshRemainingWork` rebuilds every Feature's team work from a snapshot another lane is rewriting — on the Dependencies demo scenario, teams holding 10/0/10/4/4 Features dropped to all but one holding none. A data-correctness regression, not a test artifact. Re-landable once the ownership question is answered; the starvation analysis still holds. | `f216ef558`, reverting `86d796171..7a4a09089` |
| S14 | **#5877's D4 was implemented, and needed no contract change.** `53aa75a1b` grouped the running work by lane and looked each queued row's own lane up — a `holdingEachLane` dictionary plus a `WhatItIsWaitingFor` helper — leaving `UpdateTaskResponse` untouched. Its own commit body: *"no change to the response shape"*. Reverted with the rest and recoverable from history; `UpdateLane` and `UpdateLaneMapping` no longer exist in the tree. | `53aa75a1b` |

∴ **S1 is the defect and S2/S3 are what expose it.** Neither is wrong on its own: one word per entity
kind is reasonable until two different pieces of work share an entity, which S2 guarantees on every
Portfolio refresh.

∴ **S6 is the second half of the same complaint.** Fixing S1 alone leaves the row reading *"Forecasting
Portfolio 'Ocean Explorer' — Queued behind Ocean Explorer"*, which still asks the reader to work out
that the name refers to a different row.

∴ **S7 dates the fix.** The self-reference is reachable today because one lane means the forecast waits
behind its own portfolio's refresh. Under #5877's per-type lanes it becomes rare rather than
impossible — a `PortfolioDelete` still shares the Portfolio lane with `Features` (#5877 D6).

---

## Wave: DISCUSS / [REF] Locked Decisions

### D1 — A row names the work first, then the entity

`Refreshing Portfolio 'Ocean Explorer'`, `Forecasting Portfolio 'Ocean Explorer'`, `Removing Portfolio
'Atlas'`, `Refreshing Team 'Voyager'`, `Removing Team 'Voyager'`. User decision, 2026-09-21, chosen
over a parenthetical suffix and over collapsing the two rows into one.

The suffix would have matched the house style already used for deletes (S8) and was rejected because a
marker appended to an entity name is read as a property of the entity; the confusion being fixed is
precisely that the row appears to be *about* the Portfolio rather than about a piece of work. Leading
with the verb makes the row a sentence about work, which is what the list is.

This replaces the `(removal)` suffix rather than coexisting with it. Two ways of saying the same thing
in one column is how they come to disagree.

### D2 — The verb is a total function over `UpdateTaskType`

Five members, five answers: `Team` and `Features` → *Refreshing*, `Forecasts` → *Forecasting*,
`TeamDelete` and `PortfolioDelete` → *Removing*. Not an extension of scope — D1 cannot be expressed by
a lookup with a `default:` arm, because the arm that currently absorbs three members is the defect.
The entity-kind lookup stays two-armed and stays terminology-driven (S12).

### D3 — The backend decides whether the holder is the row's own entity; the browser words it

Two properties, and only these two are locked:

1. **The comparison happens once, on the read path.** The backend already picks the lane holder
   (`UpdateController.cs:61-62`) and already resolves its name, so it is the only place that holds both
   sides of "is this the same entity". A second decider in the browser is the failure mode
   `ActivitySection.tsx:48-55` already warns about in its own comment.
2. **The words stay in the browser.** They are terminology-dependent, and the backend has no business
   holding them.

**The response widens by the minimum that lets the browser word it, and no more.** The exact field is
DESIGN's — see the handoff. Deliberately *not* locked as "carry the holder's full identity": S14 shows
the adjacent problem was solved with no contract change at all, and a shape chosen here without that
evidence in view would be a guess dressed as a decision.

### D4 — A row never says it is waiting behind itself

Where the lane holder is the **same entity** as the queued row, the clause names the holder's activity
instead of its name: `Queued behind its own refresh`, `… its own forecast`, `… its own removal`. Where
the holder is a different entity the clause is unchanged — `Queued behind Ocean Explorer` — because
that reading is correct, already shipped, and already documented (S10).

### D5 — This story must survive #5877 being re-landed, and must not re-land any of it

#5877 is **not** an unstarted story. It shipped and was reverted the same day for destroying Feature
ownership under concurrent refreshes (S13), and its D4 — a queued row naming the holder of its *own*
lane — was implemented and backed out with it (S14). The revert is explicit that the work can return
once the ownership question has an answer.

Three consequences, all binding:

- **Nothing here re-lands lanes**, reintroduces `UpdateLane`/`UpdateLaneMapping`, or makes any change
  whose correctness depends on lanes existing. One lane in, one lane out (S7).
- **D4's rule is orthogonal to lanes and holds under both.** Today the single lane makes a forecast wait
  behind its own portfolio's refresh. Under #5877's lanes that particular pair separates, but a
  `PortfolioDelete` still shares the Portfolio lane with `Features` (#5877 D6), so a row can still be
  queued behind its own entity. The rule does not need re-deciding when #5877 returns.
- **Whatever DESIGN picks for D3 must compose with `WhatItIsWaitingFor`**, the reverted helper #5877
  will bring back. Two passes over the same field that were written without knowledge of each other is
  how the read path ends up with two answers to one question.

Read `53aa75a1b` before designing D3. It is the nearest prior art and it argues against widening the
contract.

### D6 — Wording only on the front end; one field on the read path

No new endpoint, no schema change, no queue change, no new control, no new gate.

### D7 — Public docs and the generated screenshot are this story's work, not `/release`'s

`docs/settings/taskmanager.md` states the row wording as normative prose (S10) and
`docs/assets/settings/taskmanager.png` is regenerated from an `@screenshot` spec that opens this
popover (S11). Both go stale the moment D1 ships. They are a Definition-of-Done item here, not drift
for a later pass to find.

---

## Wave: DISCUSS / [REF] Scope Assessment

**PASS — right-sized.** Two user stories, two slices, one surface, one read-path field. Well under
every oversized signal: 2 stories (threshold >10), 2 modules (>3), no integration points, ~1 day total
(>2 weeks), and the two outcomes ship separately by design.

---

## Wave: DISCUSS / [REF] WS Strategy

**C — no walking skeleton.** Brownfield. Every mechanism on the path is in production: the popover, the
read model, the terminology lookup, the SignalR refresh. Nothing here is a mechanism nobody has run.

---

## Wave: DISCUSS / [REF] Driving Ports

| Surface | Change |
|---|---|
| Header → Task Manager popover, Activity section | Each row leads with what the work is (D1). A queued row never names itself as what it waits for (D4). |
| `GET /api/latest/update/tasks` | `WaitingBehind` carries the lane holder's identity instead of a bare name (D3). Every other field unchanged. |
| `docs/settings/taskmanager.md` | The row-wording table gains forecasts and the new phrasing (D7). |
| `docs/assets/settings/taskmanager.png` | Regenerated (D7). |
| CLI / MCP | **None.** No client reads `update/tasks` — grepped across `lighthouse-clients` on 2026-09-21, no hit for the route, `getRunningTasks` or `UpdateTask`. |
| HTTP API additions | **None.** |
| Website marketing surface | **None.** The Task Manager is an operator surface behind a System-Administrator gate; no marketing page covers it. |
| RBAC | **None.** `GET /update/tasks` already carries `[RbacGuard(RbacGuardRequirement.SystemAdmin)]` and the icon is already hidden from everyone else. No gate is added, removed or moved. |

---

## Wave: DISCUSS / [REF] Pre-requisites

- **#5511 slices 01–08 are pushed.** They built every surface this story touches. Satisfied.
- **#5877 is reverted, not unstarted** (S13, S14). Trunk carries one lane and today's global
  `waitingBehind`; the lane work sits in history behind an open data-correctness question about Feature
  ownership. This story neither waits for it nor blocks it, and must not re-land any of it — D5.
- **The revert is the live risk to this story's schedule.** If #5877 returns while #6055 is in flight,
  both touch `UpdateController`'s `waitingBehind` resolution. Whichever lands second rebases onto the
  other; they do not merge cleanly by luck. Worth checking #5877's state before slice 02 starts.
- No other in-flight item touches `ActivitySection.tsx` or `UpdateController.cs`.

---

## Wave: DISCUSS / [REF] Out of Scope

- **Collapsing a Portfolio refresh and its forecast into one row.** Considered and rejected by the user,
  2026-09-21: it would make the forecast un-stoppable on its own and would need a row to survive the
  gap between two independent queue entries.
- **The stop button's wording on a delete row.** `Stop refreshing <name>` is what the ✖️ announces even
  on a removal, which the server then refuses (`UpdateController.cs:94-97`). A pre-existing wart on a
  control D1 does not touch; named here so it is a known omission rather than an oversight.
- **The fallback name for a vanished entity.** `NameOf` falls back to `"{UpdateType} {Id}"`, so a
  Portfolio deleted mid-refresh renders `Forecasting Portfolio 'Forecasts 3'`. Reachable only in the
  window between an entity's deletion and its own update leaving the list. No worse than today's
  `Portfolio 'Forecasts 3'`; left alone rather than fixed under cover of a wording story.
- **Queue position or wait estimate.** Refused in #5511 (D15) and again in #5877.
- **Anything about the lanes themselves** (#5877).

---

## Wave: DISCUSS / [REF] User Stories

### US-01 — Every row says what the work is

`job_id`: `job-operator-see-what-lighthouse-is-doing-right-now`

As a Platform Operator, when Lighthouse is refreshing a Portfolio and forecasting it, I want the two
rows to read as two different pieces of work, so that I can tell a busy instance from a list that has
drawn the same thing twice.

#### Elevator Pitch

```
Before: a Portfolio being refreshed and the forecast that refresh triggers draw two rows with the
        same text — "Portfolio 'Ocean Explorer'" twice — and nothing in either says which is which.
After:  open the Task Manager popover in the header while a Portfolio refresh is running → sees
        "Refreshing Portfolio 'Ocean Explorer' — Running" above "Forecasting Portfolio
        'Ocean Explorer' — Queued".
Decision enabled: the operator decides whether the instance is working through a real backlog or has
        drawn one piece of work twice — which is the difference between waiting and investigating.
```

#### Acceptance Criteria

- **AC-01.1** — With `Features` and `Forecasts` admitted for the same portfolio id, the two rows render
  **different** text. Asserted as a difference between the two rendered strings, not against two
  hard-coded expectations, so the test fails for any future pair that collapses.
- **AC-01.2** — Each of the five `UpdateTaskType` members renders a distinct leading verb-and-kind
  phrase for one shared entity id: five rows, five distinct strings (D2).
- **AC-01.3** — The entity kind still comes from the tenant's Terminology. With Team renamed to *Squad*
  and Portfolio to *Programme*, the rows read `Refreshing Squad '…'` and `Forecasting Programme '…'`,
  and the seeded defaults appear nowhere.
- **AC-01.4** — A delete renders `Removing <kind> '<name>'` and **no** `(removal)` suffix. The suffix is
  gone, not carried alongside (D1).
- **AC-01.5** — The verb is not itself passed through Terminology. With every configurable term renamed,
  *Refreshing*, *Forecasting* and *Removing* are unchanged (S12).
- **AC-01.6** — The row's `data-testid` is unchanged (`task-manager-row-<type>-<id>`), so the existing
  `TaskManagerIcon` suite continues to address rows the same way.
- **AC-01.7** — **Production data.** On the dogfood instance, with a real Portfolio refresh under way
  against a real connection, the popover shows a *Refreshing* row and a *Forecasting* row for that
  Portfolio, and a screenshot of it is what `docs/assets/settings/taskmanager.png` becomes.

---

### US-02 — Nothing in the list says it is waiting behind itself

`job_id`: `job-operator-see-what-lighthouse-is-doing-right-now`

As a Platform Operator, I want a queued row to name something other than itself as what it is waiting
for, so that the one field in the list whose whole purpose is to explain a wait stops being the most
confusing thing on the row.

#### Elevator Pitch

```
Before: the queued forecast of a running Portfolio reads "Queued behind Ocean Explorer" — where
        Ocean Explorer is the Portfolio the row is already about.
After:  open the Task Manager popover in the same moment → sees "Forecasting Portfolio 'Ocean
        Explorer' — Queued behind its own refresh", and a queued Team still reading "Queued behind
        Ocean Explorer" where that names a different entity.
Decision enabled: the operator reads the wait as a sequence they understand and leaves the popover,
        instead of reading a contradiction and going to the log to resolve it.
```

#### Acceptance Criteria

- **AC-02.1** — With `Features` for portfolio 3 running and `Forecasts` for portfolio 3 queued, the
  queued row's clause names the holder's **activity**, not its name: `Queued behind its own refresh`.
- **AC-02.2** — With `Features` for portfolio 3 running and a `Team` queued, the Team's clause is
  unchanged: `Queued behind <the portfolio's name>` (D4).
- **AC-02.3** — `PortfolioDelete` for portfolio 3 queued while `Features` for portfolio 3 runs reads
  `Queued behind its own refresh`; the reverse pair reads `Queued behind its own removal`. The clause
  names what the *holder* is doing, never what the queued row is doing.
- **AC-02.4** — "Same entity" means same entity, not same id. A `Team` with id 3 queued while `Features`
  for portfolio 3 runs is **not** a self-reference and reads `Queued behind <portfolio name>`. This is
  the criterion that fails an implementation comparing ids alone.
- **AC-02.5** — Work whose lane is free reports no clause at all — `Queued`, with nothing after it.
- **AC-02.6** — Whatever the response gains to express D4, every field of `UpdateTaskResponse` that
  exists today keeps its name, type and meaning. Asserted against the serialised payload, because this
  is a shared contract and the guard belongs where a consumer would break.
- **AC-02.7** — **Production data.** On the dogfood instance, a real Portfolio refresh and its triggered
  forecast produce a queued row that does not name its own Portfolio.
- **AC-02.8** — The self-reference is decided in exactly one place. No comparison of a row against the
  lane holder exists in the browser (D3). Asserted by the read model's tests being the only ones that
  can make AC-02.1 through AC-02.4 fail.

---

## Wave: DISCUSS / [REF] Story Map

**Backbone** — one activity: *read the Activity list and believe it*.

| Slice | Story | Ships | Releasable alone |
|---|---|---|---|
| 01 — Say what the work is | US-01 | The reported defect, end to end: rows, docs prose, screenshot | **Yes.** Answers #6055 as written. |
| 02 — Nothing waits behind itself | US-02 | The residual contradiction in the one field that explains waits | **Yes.** Independent of 01, though it reads better after it. |

**Walking skeleton**: none (WS strategy C). Slice 01 *is* the thinnest end-to-end change.

---

## Wave: DISCUSS / [REF] Slice Taste Tests

| Test | Verdict |
|---|---|
| "Ship 4+ new components" → not thin | **Pass.** Slice 01 changes one component and one docs page. Slice 02 changes one record, one interface and one clause. |
| Every slice depends on a new abstraction → ship it first | **Pass.** No new abstraction. Slice 02's contract change is a widening of an existing field, and nothing in slice 01 waits on it. |
| No slice disproves a pre-commitment → decoration | **Pass.** Slice 01 disproves that the verb can be Lighthouse's own word while the kind stays tenant-configurable (AC-01.3, AC-01.5). Slice 02 disproves that "same entity" is cheaply knowable on the read path (AC-02.4). |
| Synthetic data only → proves plumbing | **Pass.** Each slice carries a production-data criterion (AC-01.7, AC-02.7). |
| 2+ slices identical except for scale → merge | **Pass.** Different surfaces, different failure modes. |

---

## Wave: DISCUSS / [REF] Prioritization

1. **Slice 01 first — highest learning leverage and the whole reported defect.** It is the only slice
   whose failure mode is a taste question ("does leading with a verb actually read better than a
   suffix?"), and that answer is cheapest to get from the dogfood screenshot before slice 02 builds a
   clause on top of it. It also touches no contract, so it can ship and be looked at the same day.
2. **Slice 02 second — dependency direction.** Its clause is worded in the same vocabulary slice 01
   establishes, and its contract change is the one #5877 will inherit (D5). Doing it first would mean
   choosing that shape before seeing the wording it has to sit inside.

**Dogfood cadence**: one per slice, on the dogfood instance, during a real Portfolio refresh. Both are
same-day.

---

## Wave: DISCUSS / [REF] Outcome KPIs

| # | KPI | Target | Measurement |
|---|---|---|---|
| KPI-1 | Distinct rendered text per update type, for one shared entity id | **5 of 5 distinct** | AC-01.2, asserted in the Vitest suite over all five `UpdateTaskType` members. A regression here is a red test, not a second bug report. |
| KPI-2 | Rows whose `waitingBehind` clause resolves to the row's own entity | **0** | AC-02.1/02.3/02.4. Asserted in the read-model tests over the same-entity pairs the single lane makes reachable. |
| KPI-3 | Row phrasings the public docs table does not list | **0** | At DELIVER: enumerate the strings the component can emit (5 verb-kind forms × 4 states) and check each against `docs/settings/taskmanager.md`'s table (D7). |
| KPI-4 | Repeat reports of duplicate-looking Activity rows, within two releases of the fix | **0** | ADO items raised against the Task Manager surface. The weakest of the four — absence of reports is not proof of comprehension — kept because it is the actual outcome and KPI-1/2 are only its proxies. |

---

## Wave: DISCUSS / [REF] Definition of Done

1. Both slices' acceptance criteria pass as automated tests (Vitest for the rows; NUnit for the read
   model and the response contract).
2. `dotnet build` zero warnings; `dotnet test` green on the non-connector filter.
3. `pnpm test` green; `pnpm build` zero errors and zero warnings; Biome clean on `./src`.
4. SonarQube Cloud introduces no new issue of any severity.
5. Frontend mutation testing (StrykerJS) scoped to `ActivitySection.tsx`, ≥80% kill rate. Backend
   Stryker.NET scoped to `UpdateController` and the naming path. Recorded under
   `docs/feature/story-6055-activity-names-the-work/mutation/`.
6. `docs/settings/taskmanager.md`: the Activity section names forecasts, and the row-wording table
   carries the new phrasing including the self-reference clause (D7).
7. Per-feature screenshot: `docs/assets/settings/taskmanager.png` regenerated from the `@screenshot`
   spec at `Screenshots.spec.ts:136-138`, showing at least one *Refreshing* and one *Forecasting* row
   (D7, AC-01.7). Delete the PNG before the run — a diff under the pixel threshold keeps the old file.
8. `ARCHITECTURE.md` / `docs/product/architecture/brief.md`: **N/A, because** no component, boundary or
   contract described there changes. `WaitingBehind` is a field on one response, not an architectural
   element.
9. ADR: **N/A, because** D1–D4 are wording and one field widening within ADR-181's existing shape
   ("update activity is a read through the status store"). An ADR is warranted only if DESIGN chooses a
   different home for the same-entity comparison than the read path.
10. Lighthouse-Clients CLI/MCP version bump: **N/A, because** no client reads `update/tasks` — verified
    by grep across `lighthouse-clients` on 2026-09-21.
11. Website marketing surface: **N/A, because** the Task Manager is an operator surface behind a
    System-Administrator gate and appears on no marketing page.
12. RBAC impact: **N/A, because** no gate is added, removed or moved. The route's existing
    `SystemAdmin` guard and the icon's existing visibility rule are untouched.
13. Demo data: **N/A, because** the Activity list is driven by live queue state, not by seeded rows.
14. Release notes drafted from the reader's confusion — "the Activity list showed the same Portfolio
    twice and there was no way to tell which was which" — not from the lookup that caused it. The story
    carries the `Release Notes` tag; the reporter is credited.
15. ADO #6055 transitioned Active → Resolved, not Closed.

---

## Wave: DISCUSS / [REF] DoR Validation

| # | Item | Evidence |
|---|---|---|
| 1 | Business value stated | Both elevator pitches. A screenshot from the maintainer's own instance showing the duplicate, plus a correct root-cause guess that the code confirms (S1–S5). |
| 2 | Job traceability | US-01 and US-02 → `job-operator-see-what-lighthouse-is-doing-right-now`, an existing validated job whose recorded anxiety is this defect. No `infrastructure-only` escape used. |
| 3 | Acceptance criteria testable | 15 ACs, each asserting a rendered string, a serialised field or a screenshot. Two are production-data criteria. AC-01.1 and AC-02.4 are written to fail the plausible wrong implementations rather than to pass the right one. |
| 4 | Dependencies known | #5511 slices 01–08 shipped. #5877 shipped and was reverted (S13/S14); the interaction is decided (D5) and the rebase risk is named in Pre-requisites. Nothing else touches the two files. |
| 5 | Sized | Two slices, ~3h and ~3h of crafter dispatch. |
| 6 | Technical feasibility | Every changed line is in one component and one controller, both under test today. The only non-obvious point is that `UpdateTaskType` distinguishes entity kinds already, so "same entity" is decidable without a new lookup (AC-02.4). |
| 7 | Non-functional constraints | None engaged. No extra query, no extra round-trip, no change to what the read path loads — `NameOf` already resolves the holder. |
| 8 | UX defined | D1 fixes the row. D4 fixes the clause. The exact strings are in the ACs, and the tenant-configurable parts are separated from Lighthouse's own words (AC-01.3, AC-01.5). |
| 9 | Testable in isolation | `ActivitySection` through the existing `TaskManagerIcon` Vitest suite, which already addresses rows by the `data-testid` AC-01.6 preserves. The read model through the existing `TaskManagerAcceptanceTest`. |

**Requirements completeness: 0.97.** The one deliberate gap is the exact serialised shape of the
widened `WaitingBehind` — and whether it needs widening at all, since `53aa75a1b` solved the adjacent
problem with no contract change and that evidence belongs in the decision (D3). Slice 01 does not
depend on it; AC-02.6 and AC-02.8 constrain every candidate.

**Per-wave peer review: skipped.** DoR surfaced no ambiguity, the JTBD rests on an existing validated
job rather than on a new assumption, and the ACs carry no vendor-neutrality risk. The consolidated
review fires at end of DISTILL.

**Expansion catalog — no trigger fired.** AC ambiguity: no; every AC names a rendered string or a
serialised field. Cross-context complexity: no — two technologies (ASP.NET Core, React/TypeScript), one
bounded context. Multi-stakeholder: no; one persona. Compliance: no regulatory language. WS strategy is
C, not D. **Strict lean output; no expansion menu offered.**

---

## Wave: DISCUSS / [REF] Wave Decisions Summary

### Key Decisions

- **[D1]** A row names the work first, then the entity — `Refreshing Portfolio 'X'` — replacing the
  `(removal)` suffix. User decision 2026-09-21, over a suffix and over collapsing the rows.
- **[D2]** The verb is a total function over all five `UpdateTaskType` members; the `default:` arm that
  absorbs three of them is the defect (S1).
- **[D3]** The backend decides whether the lane holder is the row's own entity — one decider, on the
  read path — and the browser words it. The response widens by the minimum that allows that, and the
  field itself is DESIGN's to choose.
- **[D4]** A row never says it is waiting behind itself — `Queued behind its own refresh`. The
  different-entity clause is unchanged.
- **[D5]** #5877 is reverted, not unstarted: it shipped on 2026-09-19 and was backed out the same day
  for destroying Feature ownership under concurrent refreshes, taking its own `waitingBehind` fix with
  it. Nothing here re-lands any of it; D4's rule holds under one lane and under many.
- **[D6]** Wording plus one read-path field. No endpoint, schema, gate or control.
- **[D7]** `docs/settings/taskmanager.md` and the generated `taskmanager.png` are Definition-of-Done
  items for this story.

### Requirements Summary

- **Primary job**: an operator glances at the Activity list to tell a working instance from a wedged
  one. The glance currently returns a list that appears to name the same Portfolio twice, and a wait
  explanation that names the waiting row itself. Both are wording over a read model that already holds
  the right information.
- **Walking skeleton scope**: none (strategy C, brownfield).
- **Feature type**: user-facing.

### Constraints Established

- The entity kind stays tenant-configurable; the activity verb stays Lighthouse's own word (S12).
- `data-testid` on each row is a fixed point — the existing suite addresses rows through it (AC-01.6).
- `UpdateTaskResponse` may widen `WaitingBehind` and may change nothing else (AC-02.6).
- Nothing about the update queue's structure may change here (D5/D6).

### Upstream Changes

- None. No DISCOVER or DIVERGE artifacts exist for this story, and no assumption recorded in
  `epic-5511-task-manager` is contradicted — its own recorded anxiety is confirmed, not overturned.

---

## Wave: DISCUSS / [REF] SSOT Updates

| File | Change |
|---|---|
| `docs/product/jobs.yaml` | `story-6055-activity-names-the-work` appended to `feature_context`. A dated note appended to `job-operator-see-what-lighthouse-is-doing-right-now` recording that its stated anxiety materialised and how. No new job, no re-score. |
| `docs/product/journeys/story-6055-activity-names-the-work.yaml` | Created — the read-the-list journey, its emotional arc, the shared artifacts and the error paths. |
| `docs/product/personas/platform-operator.yaml` | **Unchanged** — no new job to list. |

---

## Wave: DISCUSS / [REF] Handoff

**To**: `nw-solution-architect` (DESIGN) — full artifact set. `nw-platform-architect` (DEVOPS) — the
Outcome KPIs section only.

Open for DESIGN, in order of consequence:

1. **What the response gains so the browser can word D4 — and whether it needs to gain anything.**
   **Read `53aa75a1b` first** (S14): the adjacent problem, "which lane holds you", was solved entirely
   inside `UpdateController` with no contract change, via a `holdingEachLane` lookup and a
   `WhatItIsWaitingFor` helper. D4 is harder only because the *wording* differs in the self-reference
   case and the words live in the browser. The candidates, cheapest first: a nullable field carrying the
   holder's `UpdateType`, set only when the holder is the row's own entity; a boolean plus the existing
   name; the holder's full identity. D3 locks the two properties, not the field. Whatever is chosen must
   compose with `WhatItIsWaitingFor` returning (D5), because #5877 brings it back.
2. **Whether the verb belongs beside the kind lookup or replaces it.** `useKindOf` returns a kind; D1
   needs a phrase. One total function returning the whole phrase is the likely shape, but the kind is
   used nowhere else, so this is a free choice rather than a constrained one.
3. **Whether slice 02 should wait for #5877's ownership question.** Not a blocker — D4's rule holds
   under one lane and under many (D5) — but if #5877 is about to return, landing slice 02 first creates
   a rebase over the same lines. A scheduling call, and the user's, not DESIGN's; surfaced here so it is
   made rather than discovered.

Carried forward, not dropped: the stop-button wording on delete rows, and the `"{UpdateType} {Id}"`
fallback name — both in Out of Scope with their reasons, both still true after this story ships.

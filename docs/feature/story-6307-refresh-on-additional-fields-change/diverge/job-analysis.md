# Job Analysis — story-6307-refresh-on-additional-fields-change

DIVERGE wave · Flux (nw-diverger) · 2026-10-15 · work type: brownfield · DISCOVER skipped by the maintainer;
the problem is grounded in Bug #6306 evidence and in the code as it stands on `main` (`17428144f`).

## 1. Raw request (verbatim, ADO User Story 6307)

> Split out of Bug #6306. Adding an additional field (e.g. fixVersions) to a connection does not populate it
> on already-synced work items until each team/portfolio using that connection is updated. A rule on the new
> field then silently matches nothing. The docs say to run an update, but even the maintainer forgot.
> Proposal: when a connection's additional fields change, trigger (or offer) an update of every team and
> portfolio using it. Needs DISCUSS: automatic vs prompted, scope, load on large instances.

## 2. Evidence base (what the code does today)

| # | Fact | Where |
|---|---|---|
| E1 | Additional fields are a `List<AdditionalFieldDefinition>` owned by the connection (`Id`, `DisplayName`, `Reference`, `IsPredefined`). | `Lighthouse.Backend/Lighthouse.Backend/Models/AdditionalFieldDefinition.cs:3-16`, `Models/WorkTrackingSystemConnection.cs:21` |
| E2 | They are edited only through the connection PUT, which is **SystemAdmin**-guarded, merges the list (delete missing, update in place, add new) and saves. **Nothing is enqueued.** | `API/WorkTrackingSystemConnectionController.cs:75-122` (guard `:76`), merge `:239-276` |
| E3 | The connection edit page saves and navigates to `/`; it never calls an update. | `Lighthouse.Frontend/src/pages/Connections/Edit/EditConnection.tsx` `saveAndNavigate` |
| E4 | The only hint is an info Alert shown once a field is touched: "Additional Fields will be loaded with the next refresh of data." | `Lighthouse.Frontend/src/components/Common/Connection/ModifyConnectionSettings.tsx:674-680` |
| E5 | By contrast, a Team or Portfolio settings save **does** refresh that owner straight away, from the frontend. | `components/Common/Team/ModifyTeamSettings.tsx:122-126`, `pages/Teams/Edit/EditTeam.tsx:55`, `pages/Portfolios/Edit/EditPortfolio.tsx:77` |
| E6 | Correctness of the eventual refresh is already solved: the fetch fingerprint includes the connection's additional field definitions, so the **next** cycle of every Team and Portfolio on that connection is a full fetch. The invalidation is deliberately passive ("each entity notices at its own next cycle"). | `Services/Implementation/WorkItems/FetchFingerprint.cs:26-47, 86, 117-131`; comparison `WorkItemService.cs:117-124`; ADR-140 "A connection change fans out" |
| E7 | "Next cycle" is far away by default: the periodic loop runs every `Interval` = 60 min and refreshes an owner once `RefreshAfter` = 180 min have passed since its last update, so a just-refreshed owner can wait up to ~4 h. | `Services/Implementation/Seeding/AppSettingSeeder.cs:28-32`; `BackgroundServices/Update/UpdateServiceBase.cs:305-337`; `TeamUpdater.cs:113-121` |
| E8 | A rule reads an additional field by id from `AdditionalFieldValues`; a missing key reads as `""`. A rule on a never-fetched field therefore silently matches nothing (or, negated, everything). The rule builder offers the new field immediately, because the schema comes from the connection's definitions, not from the data. | `Services/Implementation/WorkItemRules/WorkItemFieldProvider.cs` `GetAdditionalFieldValue`; schemas `DeliveryRuleService.cs:26-47`, `Forecast/ForecastFilterRuleService.cs:15-24`, `WorkItems/BlockedItemService.cs:84-93` |
| E9 | Three rule surfaces consume additional fields: rule-based Deliveries (Features, Portfolio refresh), forecast filter (Work Items, Team refresh), blocked rules (both). | as E8 |
| E10 | Manual paths exist: per Team (`TeamController.cs:74-89`, `TeamWrite`), per Portfolio (`PortfolioController.cs:56-61`), and "Update All" for every Team / every Portfolio (`TeamsController.cs:71-84`, `PortfoliosController.cs:61-74`) — **premium + SystemAdmin**, header button `UpdateAllButton.tsx`. **No "update everything on this connection" path exists.** | as cited |
| E11 | The update queue is one sequential reader, dedups by key and coalesces a trigger raised while the same key is in flight into one follow-up; admission is fleet-wide on multi-replica. | `BackgroundServices/Update/UpdateQueueService.cs:9-80` (sequential note `:72-77`); `ARCHITECTURE.md:61`; `docs/ci-learnings.md:683-687` |
| E12 | User docs: the faster-updates section says changing "the additional fields" makes "the next update" download everything — true, but silent on *when*. The per-connector Additional Fields sections say nothing about updating. A 2024-era release note says "you must make sure to refresh your teams and portfolios". | `docs/settings/configuration.md:81`; `docs/concepts/worktrackingsystems/jira.md:162-185`, `azuredevops.md:101-124`; `docs/releasenotes/releasenotes.md:1558-1559` |
| E13 | Precedent for "a saved change needs dependent data reloaded": cheap → automatic, expensive → one in-place click, **never** an instruction to go elsewhere and refresh. The info Alert in E4 is exactly the shape that decision retired elsewhere. | `docs/product/architecture/adr-030-dependent-data-reload-after-autosave-cost-based-split.md` |
| E14 | Bug #6306 itself: a Jira Release binding was verified against `fixVersions`; the additional-field re-sync gap surfaced in the same report and was split out. | `docs/evolution/2026-10-15-bug-6306-jira-onprem-releases.md` (header, "Deferred"); `docs/feature/bug-6306-jira-onprem-releases/deliver/roadmap.json:10,32` |

## 3. Job extraction (5 Whys)

| Layer | Statement |
|---|---|
| Tactical (request) | "When a connection's additional fields change, trigger or offer an update of every Team and Portfolio using it." |
| Why 1 | So that Work Items and Features that were already synced carry the new field's value. |
| Why 2 | Because a rule built on the new field reads `""` for every item until then (E8) and silently matches nothing — the Delivery is empty, the filter excludes nothing, nothing reads as blocked. |
| Why 3 (operational) | Because the save and its effect are decoupled by up to ~4 h (E7) and nothing on screen says which Teams and Portfolios are still on the old field set; the only bridge is a sentence (E4) that the maintainer himself did not act on (E14). |
| Why 4 (strategic) | The admin is about to build decisions — which Features are in a Delivery, which Work Items count towards throughput — on what the configuration says. They need what Lighthouse shows to *be* the configuration they saved, or to say plainly that it is not yet. |
| Why 5 (physical) | Irreducible function: **close, or make visible, the gap between a configuration change and the data it governs.** A further "why" lands on a life goal ("trust the forecast"), so stop here. |

First-principles inversion: the activity is "press Update on seven Teams". Nobody wakes up wanting that. Stripped of
tools, what remains is *configuration → effect with no hidden latency*.

## 4. Job statements

- **Functional (required).** When I add or change a field on a work tracking connection, I want every Team and
  Portfolio that reads through that connection to hold that field's values before I build on it — or to be told plainly that they do not yet — so I can put a
  rule on the field and trust what it matches.
- **Emotional.** From "why is my Delivery empty — did I type the field wrong?" to saving the connection and
  moving on, sure that the field will be there (or told plainly that it is not there yet).
- **Social.** Not be the administrator whose rule-based Delivery shows zero Features in the review; for the
  maintainer, not ship a product whose correct use depends on a step only he knows — and forgot.

## 5. Disruption check

Higher-level job: *"Any configuration change that alters what Lighthouse fetches takes effect without me having
to know the sync model."* Team and Portfolio settings already serve it (E5: save → refresh that owner).
The connection is the one fetch-shaping surface that does not, because its change fans out to many owners
(ADR-140). A solution at the higher level — every fetch-shaping save, wherever it happens, enqueues the owners
whose fingerprint it moved — would make this story one instance of a rule rather than a special case. Noted
for DISCUSS as a scope question; not required to serve this job.

## 6. ODI outcome statements

| # | Outcome statement |
|---|---|
| O1 | Minimize the time it takes for a newly added field's values to be present on every already-synced Work Item and Feature on that connection. |
| O2 | Minimize the likelihood that a rule naming a field is evaluated against values that were never fetched. |
| O3 | Minimize the likelihood that an administrator has to remember a follow-up step after saving a connection. |
| O4 | Minimize the time it takes to tell whether a Team or Portfolio still holds data read under the previous field set. |
| O5 | Minimize the number of full fetches one connection edit causes against the tracker. |
| O6 | Minimize the likelihood that a connection edit that does not change what is fetched (a rename, a credential rotation) causes any fetch. |

## 7. Opportunity candidates

Importance and satisfaction are **estimated** (1–10) from the evidence above, not surveyed — DISCOVER was skipped.

| Outcome | Importance | Satisfaction | Score | Status |
|---|---|---|---|---|
| O2 rule evaluated against never-fetched values | 9 | 2 | 16 | Under-served |
| O1 time to populated | 8 | 3 | 13 | Under-served |
| O3 no follow-up step to remember | 7 | 2 | 12 | Under-served (satisfaction 2; the maintainer forgot the step) |
| O4 tell old vs new | 7 | 2 | 12 | Under-served (satisfaction 2; nothing shows the pending state) |
| O5 one connection edit → bounded full fetches | 6 | 8 | 6 | Over-served — already exactly one full fetch per owner (E6); **a constraint, not a target: do not regress** |
| O6 no fetch for a non-shaping edit | 6 | 9 | 6 | Over-served — `Options` are outside the fingerprint (ADR-140); **constraint: do not regress** |

Reading: the job is under-served on *time* (O1), *silence* (O2, O4) and *memory* (O3), and already well served on
*cost* (O5, O6). (Scores of exactly 12 sit on the band edge; with satisfaction at 2 they are read as under-served.)

Scale note: outcome-level estimates here use the ODI 1–10 scale. The SSOT job in `docs/product/jobs.yaml` uses that
file's 1–5 convention; it is mapped from the job's dominant outcomes O2/O1 (importance 9 and 8 → 4; satisfaction
2 and 3 → 1), giving importance 4, satisfaction 1, gap 3. Rounding rule: halve and round down (9 → 4.5 → 4,
3 → 1.5 → 1).
Any option that buys O1 by spending O5/O6 is a regression.

## 8. Relation to existing jobs (SSOT)

Closest existing job: `job-config-admin-know-which-settings-cost-a-refetch` (epic-5687) — it decides the
*mode* of the next cycle and explicitly scopes out an immediate refresh ("this job is about what MODE the next
scheduled cycle runs in, not about an immediate refresh"). This story is the complementary *when*. A new job,
`job-config-admin-see-a-connection-field-change-take-effect`, is added to `docs/product/jobs.yaml`, linked to
that one.

## Gate G1

- [x] Job at strategic/physical level (configuration → effect, no hidden latency); no feature reference in the job statement.
- [x] 6 ODI outcome statements (≥ 3).
- [x] Under-served outcomes identified (O2, O1, O3/O4) and constraints named (O5, O6).

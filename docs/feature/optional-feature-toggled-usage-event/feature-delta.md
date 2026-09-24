# Feature Delta — optional-feature-toggled-usage-event

> Maintainer request, 2026-09-24: *"can we add a posthog event so we know every time someone toggled a
> feature (and which one)? that will help evaluate who has toggled things on /off"*.
>
> An eleventh event in the Opt-In Usage Data vocabulary (Epic #5733). It is sent when somebody
> switches a behaviour setting under Settings → System, and it carries which setting and which way it
> went. It uses the existing pipe: the browser notices, our own backend checks consent and the
> administrator's veto, and our backend forwards. Nothing about the pipe changes.
>
> **ADO: no item yet.** Creating one needs the maintainer's confirmation. Proposed: a User Story under
> Epic #5733, titled "Usage Data :: report when a behaviour setting is switched", tagged `Release Notes`.

Combined DISCUSS + DESIGN pass (2026-09-24). Runs after story #5913, which retires the Faster Updates
switch, so the list of settings this event can name is the list after that story.

---

## Wave: DISCUSS / [REF] Persona IDs

- **product-owner (maintainer)** — reads the PostHog census to decide which behaviour settings earn
  their place, and whether a premium switch is worth what it costs to keep.
- **config-admin** — the person whose switching is counted. Their browser must have agreed, and their
  instance must not have vetoed usage data.

## Wave: DISCUSS / [REF] JTBD One-Liners

Extends the capability-use outcome that Epic #5733 slice 04 opened (`OUT-usagedata-capability-use`): when I
decide whether a behaviour setting stays, becomes the default or goes away, I want to know how often
people switch it and which way, so the decision rests on use rather than on a guess. Story #5913 made
that decision for Faster Updates on one dev instance's log. This event is how the next decision gets
real data.

## Wave: DISCUSS / [REF] What this event can and cannot answer

This belongs up front, because the request says *"who has toggled things"*.

- **It can answer:** which settings get switched, in which direction, how often, and on which version,
  deployment mode and licence tier. These are the instance facts every event already carries.
- **It cannot answer who.** By design, the pipe carries no instance identifier and no person. The only
  identity is a per-browser pseudonym minted on the server (Epic #5733, ADR-191). Two browsers of one
  installation look like two unrelated browsers, and no event can be traced to a customer. Adding an
  identity would reopen a decision the Epic closed on purpose, and the shipped consent copy promises
  that none travels. Out of scope here.
- **It counts browsers that agreed, not installations.** An administrator who never answered the
  consent question, or who declined, is never counted.

## Wave: DISCUSS / [REF] Locked Decisions

| ID | Decision | Verdict |
|----|----------|---------|
| E1 | **One event, `OptionalFeatureToggled`**, appended to `UsageDataEventName` as value 10. One name covers both directions: the direction is a property, so "switched on" and "switched off" can be compared within one event instead of across two. | Locked |
| E2 | **It carries exactly two things:** which setting (`optional_feature`), from a closed list of its own, and the state it was switched to (`enabled`: `true`/`false`). Never the setting's key string, name or description. Both are declared in `UsageDataEventShapes`, both ways round, like the connector kind: this event is refused without them, and every other event is refused with them. | Locked |
| E3 | **The list of settings is the event's own closed enum, `UsageDataOptionalFeature`,** not the product's key strings. This follows the same reasoning as `UsageDataWorkTrackingSystem`: what leaves an instance is decided by a list written for that purpose. Members: `FeatureOrder` only (amended 2026-09-24, see E5). `DeltaSync` is absent because story #5913 removes it. A future setting reaches the census only when somebody adds it to that list deliberately. | Locked |
| E4 | **Sent only after the server accepted the change.** The settings screen flips the switch before the server answers. The event fires after `updateFeature` resolves, never on the click, and never when the write is refused, for example the 403 a Community instance gets on a premium row. | Locked |
| E5 | **The veto is not reported in either direction** (amended 2026-09-24 after DISTILL's U1, orchestrator decision taken while the maintainer was away, reversible). Switching *Never send usage data* on can never be reported: the gate drops every batch from that moment on, including the one saying so. Switching it off would be dropped too, in the browser, because the browser's consent answer still reads "stopped by the administrator" until its next hourly refresh. Making it reportable needs new consent-refresh plumbing, and it would still give a one-sided count (lifts only, never engagements) that reads as "people keep lifting the veto". So `NeverSendUsageData` is not in the event's list; the browser's key mapping leaves `UsageData` out, which by E6 means nothing is sent. The disclosure row says the veto switch is never reported. | Locked (reversible) |
| E6 | **A setting the browser does not recognise is not reported.** The browser maps the setting keys it knows to the enum through a `Record`. A key it does not know sends nothing, rather than a guess or a placeholder. | Locked |
| E7 | **Terminology.** The disclosure row names the settings as the settings table does: *Let Lighthouse own the order of your Features* (default term), *Never send usage data*. | Locked |

## Wave: DISCUSS / [REF] User Stories

### US-01 — See which behaviour settings people switch, and which way

As the **product owner**, I want an event every time a behaviour setting is switched on an instance
whose browser agreed to share usage data, saying which setting and which way, so I can tell a setting
nobody touches from one people keep switching off.

`job_id: job-maintainer-know-if-a-shipped-feature-landed` (the job Epic #5733 slice 04's events serve, `docs/product/jobs.yaml`; this story feeds its outcome `OUT-usagedata-capability-use`)

#### Elevator Pitch
Before: the census shows that people open settings pages and connect trackers, but nothing about which behaviour settings they change.
After: an administrator whose browser agreed switches *Let Lighthouse own the order of your Features* on under **Settings → System**. The collector receives `OptionalFeatureToggled` with `optional_feature: FeatureOrder, enabled: true`.
Decision enabled: whether a behaviour setting stays optional, becomes the default, or goes away.

#### Acceptance Criteria
- **AC-1.1** A browser that agreed switches *Feature Order* on, and the switch is accepted: exactly one `OptionalFeatureToggled` event with `optional_feature = FeatureOrder`, `enabled = true` reaches the collector. Switching it back off sends one more event with `enabled = false`.
- **AC-1.2** (amended per E5) Switching *Never send usage data* in either direction reports nothing from the screen, and a hand-built message naming it as the setting is refused by the server, because it is not on the event's list.
- **AC-1.3** A switch the server refuses, such as a premium row on an instance without a premium licence, sends nothing.
- **AC-1.4** A browser that refused, never answered, or withdrew sends nothing when it switches a setting. The existing gate covers this; one scenario proves the new event inherits it.
- **AC-1.5** The backend refuses an `OptionalFeatureToggled` message missing either field, and any other event carrying either field. The whole batch is rejected, as for the connector kind.
- **AC-1.6** Nothing else about the setting travels: not its key string, name or description. The emit-seam field list gains exactly `optional_feature` and `enabled`.
- **AC-1.8** A setting the browser has no name for reports nothing when it is switched, and switching a setting it does name, right afterwards, still reports exactly once (E6). *(Added after the DISCUSS review; DISTILL's F5 already covers it.)*
- **AC-1.7** `docs/settings/usagedata.md` gains the event row and the two field rows, and states plainly that switching the administrator's veto (*Never send usage data*) is never reported, in either direction (E5). `UsageDataDisclosureTest` stays green, which proves the doc and the enum agree.

## Wave: DISCUSS / [REF] Out of Scope

- Any identity beyond the per-browser pseudonym: no instance id, no user, no customer.
- Reporting settings changes outside the behaviour-settings list (team, portfolio or system settings).
- Reporting from the backend, for example on writes through the API or MCP. The pipe is browser-detected by design; an API client has no consent row.
- A PostHog dashboard. Building the insight is done in PostHog, not in this repository.

## Wave: DISCUSS / [REF] Cross-cutting Impact

- **RBAC** — **N/A, because** switching a behaviour setting is already `RbacGuard(SystemAdmin)`. The event only reports a write that already succeeded, and the ingest endpoint is the existing anonymous, consent-gated one.
- **Lighthouse-Clients (CLI + MCP)** — **N/A, because** the clients never touch usage data. `grep -rli "usagedata\|usage data"` over `/storage/repos/lighthouse-clients` (excluding `node_modules`/`dist`) finds nothing. The browser-to-backend batch DTO is not a public contract.
- **Website** — **N/A, because** `/storage/repos/website` carries no copy of the event list; the product's own `docs/settings/usagedata.md` is the disclosure.
- **Privacy / consent copy** — the disclosure page is the only place the payload list exists, and every consenting person was pointed at it. A new row is a change to what they agreed to. It stays within the dialog's promises: nothing typed, no names, no addresses. Adding events was the maintainer's call when slice 04 lifted the ceiling. This is recorded rather than re-litigated.

## Wave: DISCUSS / [REF] Scope Assessment: PASS

One story, one slice. It follows a pattern slice 04 used ten times; the only new thing is two fields, and
the connector kind already shows the shape for a field. Under a day.

## Wave: DISCUSS / [REF] Outcome KPIs

| KPI | Target | Measurement |
|-----|--------|-------------|
| `OUT-usagedata-setting-switches` | The event arrives for 100 % of accepted switches from consenting browsers and 0 % of refused or vetoed ones | Integration scenarios against the recording collector (below the `HttpClient`), as slice 04's events are checked |
| `OUT-usagedata-capability-use` (existing) | Gains a second capability-use signal | PostHog insight on `OptionalFeatureToggled`, broken down by `optional_feature` and `enabled` |

## Wave: DISCUSS / [REF] Definition of Done

1. `OptionalFeatureToggled` is on the name list, its two fields are declared in `UsageDataEventShapes`, and the publisher sends them under `optional_feature` / `enabled`.
2. The browser reports it from `SystemSettingsTab` only after an accepted write, through `useUsageDataReporter`, with no consent branch at the call site.
3. The TS mirrors (`UsageData.ts`) carry the name and the enum as string values, and the key mapping is a `Record`.
4. The emit-seam field list is widened by exactly two names.
5. The disclosure page is updated, and `UsageDataDisclosureTest` is green.
6. The backend suite (live connectors excluded), `pnpm test` and `pnpm build` are green with zero warnings.
7. Stryker ≥ 80 % on changed lines, both stacks.

## Wave: DISCUSS / [REF] DoR Validation

| # | Item | Status | Evidence |
|---|------|--------|----------|
| 1 | Value clear | ✅ | Maintainer request; elevator pitch |
| 2 | Traceability | ✅ | `OUT-usagedata-capability-use` |
| 3 | ACs testable | ✅ | AC-1.1–1.6 at the ingest endpoint + recording collector; AC-1.7 by the existing disclosure test |
| 4 | Dependencies known | ✅ | Story #5913 first (fixes the enum's membership); Epic #5733 pipe on `main` |
| 5 | Scope bounded | ✅ | Out of Scope; E2–E6 |
| 6 | KPIs | ✅ | Above |
| 7 | Cross-cutting | ✅ | RBAC, Clients, Website, Privacy |
| 8 | ≤ 1 day | ✅ | Scope Assessment |
| 9 | No blocking unknowns | ✅ | The one open item, the ADO story, is administrative and does not block the code |

---

## Wave: DESIGN / [REF] Component Decisions

| Component | Path | Action | Detail |
|-----------|------|--------|--------|
| `UsageDataEventName` | `Models/UsageData/UsageDataEventName.cs` | EXTEND | `OptionalFeatureToggled = 10` |
| `UsageDataOptionalFeature` | `Models/UsageData/UsageDataOptionalFeature.cs` | **NEW** | `FeatureOrder = 0` only (E5 amended), with the same "zero is a real answer" note as its siblings |
| `UsageDataEventReported` | `Models/UsageData/UsageDataEventReported.cs` | EXTEND | Two nullable members: `UsageDataOptionalFeature? OptionalFeature`, `bool? Enabled` |
| `UsageDataEventShapes.Fits` | `Models/UsageData/UsageDataEventShapes.cs` | EXTEND | A third declaration, `EventsThatSayWhichSettingWasSwitched`, checked both ways round for **both** fields together. One is never present without the other |
| `UsageDataEventBatchDto` | `API/DTO/UsageDataEventBatchDto.cs` | EXTEND | Two nullable enum/bool fields, closed values only; still no `string` property |
| `UsageDataController` (the read) | `API/…/UsageDataController.cs` | EXTEND | Passes the two fields into `UsageDataEventReported`; the refusal path is unchanged |
| `PostHogUsageDataPublisher` | `Services/Implementation/UsageData/PostHogUsageDataPublisher.cs` | EXTEND | Two `[JsonPropertyName]` members, `optional_feature` and `enabled`, omitted when null as `work_tracking_system` is |
| `UsageDataEmitSeamArchUnitTest` | tests | EXTEND | The written field list gains `enabled` and `optional_feature` |
| TS mirrors | `Lighthouse.Frontend/src/services/Api/UsageDataService.ts` (event name, per DISTILL U2) + `src/models/UsageData/UsageData.ts` | EXTEND | The name; `UsageDataOptionalFeature` as a string-valued mirror; `NoticedEvent` and `UsageDataCapabilityUse` (`usageDataReporter.ts`) gain the two optional fields |
| Key → enum mapping | `Lighthouse.Frontend/src/services/UsageData/` | **NEW** (small) | `usageDataOptionalFeatureFor(key: string)`: a `Record` of the known keys, `undefined` for anything else |
| Call site | `Lighthouse.Frontend/src/pages/Settings/System/SystemSettingsTab.tsx` `onToggleOptionalFeature` | EXTEND | After `await updateFeature(…)` succeeds, report `{ name: OptionalFeatureToggled, optionalFeature, enabled: !toggledFeature.enabled }`. Nothing in the `catch` |
| Buffer / service serialisation | `usageDataBuffer.ts`, `UsageDataService.ts` | EXTEND | Carry the two fields through to the batch, as `workTrackingSystem` is carried |
| Disclosure | `docs/settings/usagedata.md` | EXTEND | One event row, two field rows, and the veto sentence (E5) |

No ADR. This is an instance of ADR-190's per-event shape, and the shape table is the design. No migration,
since nothing is persisted. `Program.cs` is untouched.

## Wave: DESIGN / [REF] The shape table after this change

| Event | route | work_tracking_system | optional_feature + enabled |
|-------|-------|----------------------|----------------------------|
| TeamTabOpened / PortfolioTabOpened | required | refused | refused |
| WorkTrackingSystemConnected | refused | required | refused |
| OptionalFeatureToggled | refused | refused | **both required** |
| the other six | refused | refused | refused |

## Wave: DESIGN / [REF] DEVOPS

**N/A, because** the collector, its address and the chart are untouched. A new event name needs no PostHog
configuration: events are created on first arrival.

## Wave: DESIGN / [REF] Handoff

DISTILL writes the scenarios in the slice 04 harness (`Integration/UsageData/Slice04ProductEventsTests.cs`
and its recording collector) plus the frontend call-site test. The frontend test is written first for the
`SystemSettingsTab` handler, because slice 04 found page-level call sites were the untested seam.

---

## Wave: DISTILL / [REF] Reconciliation

DISCUSS and DESIGN are both in this file, from one combined pass, and there is no separate
`wave-decisions.md`. DEVOPS is N/A because DESIGN says so. E1–E7, US-01 AC-1.1–AC-1.7, the component
table and the shape table were checked against each other.

**DISCUSS vs DESIGN: 0 contradictions.** This includes the amendments to E3, E5, AC-1.2 and the
component table.

The first pass found one defect, against the existing code rather than between waves: the veto being
lifted could not be reported (U1). It is now **RESOLVED as (b)**. The veto is not reported in either
direction, and `NeverSendUsageData` is not on the event's list (see E5). The scenarios below are
written against the amended E3, E5 and AC-1.2.

## Wave: DISTILL / [REF] Scenario list with tags

Backend: 9 scenarios, 28 cases, all `[Ignore("pending DELIVER — OptionalFeatureToggled")]`.
Frontend: 6 specs, 7 cases, all `it.skip`.

| # | Scenario | Stack | Tags | Contract shape |
|---|---|---|---|---|
| B1 | Switching the feature order setting on arrives saying which setting and that it is now on | BE | `@walking_skeleton @driving_port @real-io @AC-1.1` | bounded-change: the collector gains exactly one message |
| B2 | Switching it back off arrives as one more event saying it is now off | BE | `@driving_port @real-io @AC-1.1` | bounded-change |
| B3 | Nothing leaves a browser that did not agree (refused / never answered / withdrew) | BE | `@driving_port @real-io @error @AC-1.4` | unbounded-preservation: the collector is untouched |
| B4 | A setting switch missing which setting, which way, or both, is refused | BE | `@driving_port @real-io @error @AC-1.5` | unbounded-preservation, with an in-scenario control |
| B5 | A setting this list does not have is refused (`NeverSendUsageData`, `DeltaSync`, `FeatureOrdering`, `UsageData`) | BE | `@driving_port @real-io @error @AC-1.2 @AC-1.5` | unbounded-preservation, with an in-scenario control |
| B6 | A setting switch that says something other than on or off is refused (`"true"`, `1`, `null`) | BE | `@driving_port @real-io @error @AC-1.5` | unbounded-preservation, with an in-scenario control |
| B7 | Any other event that says a setting was switched is refused (all 10 names) | BE | `@driving_port @real-io @error @AC-1.5` | unbounded-preservation |
| B8 | Any other event carrying either half of a setting switch is refused | BE | `@driving_port @real-io @error @AC-1.5` | unbounded-preservation |
| B9 | Nothing travels with a setting switch beyond which setting and which way | BE | `@driving_port @real-io @AC-1.6` | bounded-change: the outbound field set is exactly the page's list plus two |
| F1 | Reports the ordering setting switched on once the server has accepted it | FE | `@driving_port @AC-1.1` | bounded-change: one report, after the answer |
| F2 | Reports it switched back off as one more event | FE | `@driving_port @AC-1.1` | bounded-change |
| F3 | Reports nothing when the veto is switched on, or switched off (2 cases) | FE | `@driving_port @error @AC-1.2` | unbounded-preservation, with a control |
| F4 | Reports nothing for a switch the server refused | FE | `@driving_port @error @AC-1.3` | unbounded-preservation, with a control |
| F5 | Reports nothing for a setting usage data has no name for | FE | `@driving_port @error @AC-1.6` | unbounded-preservation, with a control |
| F6 | Hands in which setting was switched and which way (click to post, real reporter and detector) | FE | `@driving_port @AC-1.1` | bounded-change |

Error and edge share: 9 of 15 scenarios (60 %). AC-1.7 (the disclosure page) needs no new scenario:
the existing `UsageDataDisclosureTest` counts page rows against the enum, and it goes red as soon as
the name is added without its row.

AC-1.2 is covered twice, on both sides. F3 shows the screen never reports the veto. B5 shows the
server refuses a hand-built message naming it.

In-scenario controls (B4–B6, F3–F5) are deliberate. Before DELIVER, every `OptionalFeatureToggled`
message is refused because the name is unknown, and the screen reports nothing at all. A pure "is
refused" or "reports nothing" scenario would therefore pass today, and would prove nothing afterwards
either. Each one also sends the complete, valid message and expects it to be accepted, or switches
Feature Order and expects that one report.

## Wave: DISTILL / [REF] Test placement

- `Lighthouse.Backend/Lighthouse.Backend.Tests/Integration/UsageData/OptionalFeatureToggledEventTests.cs`
  sits beside `Slice04ProductEventsTests.cs` and derives from the same `UsageDataCollectorObservationTest`
  base. That gives it the real host over HTTP, real SQLite, the recorder below `HttpClient`,
  `UsageData:CollectorBaseUrl` pointed at a `.invalid` address, and the live-census teardown guard.
- `Lighthouse.Frontend/src/pages/Settings/System/SystemSettingsTab.usageData.test.tsx` covers the call
  site with the reporter replaced, the way `OverviewDashboard.test.tsx` replaces it.
- `Lighthouse.Frontend/src/pages/Settings/System/SystemSettingsTab.usageDataHandIn.test.tsx` covers the
  seam from the click to `postEvents`. It uses the real consent provider, reporter and detector, and
  fakes only the HTTP services. It exists because the call-site test replaces the reporter and the
  backend scenarios post by hand, which leaves the gap between them uncovered.
- There is no separate unit spec for the key-to-name mapping (`usageDataOptionalFeatureFor`). F1–F5
  cover every branch through the call site: `FeatureOrdering` maps to `FeatureOrder`, and both
  `UsageData` and an unknown key are not reported. A spec importing the module would have needed a
  static import of a file that does not exist yet. DELIVER can add one if Stryker leaves a survivor
  there.

## Wave: DISTILL / [REF] Driving-port coverage

| Driving port | Scenarios |
|---|---|
| `POST /api/latest/usagedata/events` (ingest endpoint, anonymous, consent-gated) | B1–B9 over real HTTP |
| Settings → System, `SystemSettingsTab.onToggleOptionalFeature` via the `{key}-toggle` switch | F1–F6 via `userEvent.click` |

## Wave: DISTILL / [REF] Adapter coverage

| Adapter | @real-io | Covered by |
|---|---|---|
| `PostHogUsageDataPublisher` (outbound shape, `optional_feature` / `enabled`, left out when null) | YES | B1, B2 and B9, read from the recorder's raw JSON. Slice 04's existing `Nothing_travels_with_an_event_beyond_what_the_page_says_travels` covers the "left out when null" half. |
| Consent store (`UsageDataConsent`, SQLite) | YES | B3 (refused, never answered, withdrew through `DELETE /consent`) |
| Frontend `UsageDataService.postEvents` | NO, faked | F6 asserts what is handed to it. It posts `{ events }` unchanged, so no mapping can drop a field. |

## Wave: DISTILL / [REF] Scaffolds

**None.** No production file was touched. The backend scenarios name the event, its two parts and
the enum members as JSON text posted to the real endpoint, and read the recorder's raw JSON. This is
the idiom slice 04 already uses. The whole test assembly compiles against today's production types,
and each scenario fails on its own assertion. Before DELIVER, the unknown name is refused with a 400,
and fields on other events are silently ignored.

The frontend specs name the event and its parts as string literals passed to a mocked reporter, or
compared against `postEvents` arguments. So nothing imports a type or module that does not exist yet,
and `tsc` stays clean.

Wire names fixed by these scenarios, which DELIVER must match:
- inbound (browser to backend): `optionalFeature` (enum as string) and `enabled` (JSON boolean)
- outbound (backend to collector): `optional_feature` and `enabled`
- published setting names: `FeatureOrder` **only**. `NeverSendUsageData` is refused, following E5.

## Wave: DISTILL / [REF] Existing-test disposition

| Test | What happens in DELIVER | Action |
|---|---|---|
| `UsageDataEmitSeamArchUnitTest.EveryFieldOnTheWayOut_IsOneSomebodyWroteDown` | Goes red when the publisher gains the two `[JsonPropertyName]` members | **DELIVER widens `EveryFieldTheCollectorIsSent` by exactly `enabled` and `optional_feature`.** DISTILL did not edit it. |
| `UsageDataEmitSeamArchUnitTest.NothingOnTheWayIn_HasAFieldThatCouldHoldFreeText` | Stays green only if the new DTO and record members are `UsageDataOptionalFeature?` / `bool?`, never `string` | No edit. It is the guard. |
| `UsageDataDisclosureTest.EveryEventTheProductCanSend_HasALineOnThePage` | Goes red the moment `OptionalFeatureToggled = 10` lands unless the page gains its row in the same step. It counts event rows only, not field rows. | No edit. Add the page row in the same step as the enum member. |
| `Slice04ProductEventsTests.Nothing_travels_with_an_event_beyond_what_the_page_says_travels` (TeamCreated) | Must stay green, which proves `optional_feature` and `enabled` are left out for every other event. `bool?` must be omitted when null, not written as `false`. | No edit |
| `Slice04ProductEventsTests.EveryEventThereIs` / `ABatchOfWhateverShape` | The list of 10 does not pick up the 11th name, so its sweeps over the administrator's veto and over refusing browsers do not cover the new event | **Recommended DELIVER extension:** add `OptionalFeatureToggled`, with a `FeatureOrder` shape, to both. B3 covers refusing browsers for this event. Nothing in this file now covers the server's veto gate for it, because the veto scenarios were removed with U1. |
| Positional `new UsageDataEventReported(…)` in `UsageDataForwardingServiceTests`, `UsageDataEventQueueTests`, `UsageDataMistypedCollectorAddressTests`, `UsageDataPublishedMessageTests` (plus the controller) | Adding two positional members breaks these constructions | DELIVER either gives the new members defaults at the end of the record, or updates the four call sites |
| `UsageDataControllerTests` (positional `UsageDataEventDto(…)`) | Same, for the DTO | Same |
| `UsageDataEventShapes.Fits(name, route, system)` | Signature widens, with one production caller | No tests call it directly |
| Frontend: any test counting `UsageDataEventName` members | None found | — |

## Wave: DISTILL / [REF] Pre-requisites

- Story #5913 has landed on this branch (commits `1c7e88b71`, `1bd0e78c6`). `DeltaSync` is in the
  seeder's deprecated list, and only `FeatureOrdering` and `UsageData` are seeded.
- The Epic 5733 pipe is on `main`: ingest endpoint, consent gate, veto, queue, and a forwarding drain
  that tests can trigger.
- Backend runs with the live connectors filtered out. The UsageData filter needs no network, because
  the recorder answers in place of it.

## Wave: DISTILL / [REF] Upstream findings

**U1: RESOLVED (b), see E5.** Switching the veto off was dropped in the browser, because the consent
answer still read "stopped by the administrator" until the next hourly refresh. The veto is now not
reported in either direction.

The scenarios were reworked to match:
- The server refuses `optional_feature: NeverSendUsageData` (B5).
- The screen reports nothing for the veto either way (F3).
- The earlier "veto lifted arrives", "veto engaged sends nothing" and "hand-in of the veto lift"
  scenarios are removed.

Verified by simulating the amended DESIGN: all 7 frontend specs pass.

**U2: RESOLVED in DESIGN.** The TS event name lives in `src/services/Api/UsageDataService.ts`, and
`UsageDataCapabilityUse` in `usageDataReporter.ts` needs the two optional fields.

**U3: note on AC-1.6.** The check that the setting's key string never travels is made for
`FeatureOrdering`, the only setting that can now travel. B9's exact field-set match, together with the
inbound ArchUnit rule that allows no string fields, is what actually holds AC-1.6.

**U4: note on AC-1.3.** Its example, a premium row on an unlicensed instance, cannot be reached from
the screen, because that switch is disabled. F4 uses a generic refused write instead. The backend
cannot observe AC-1.3 at all, because a refused write never produces a post.

**U5: minor wording, for DISCUSS.** AC-1.7 still says the disclosure page "states plainly that
switching the veto on is never reported". The amended E5 says it is not reported in either direction,
and the disclosure row should say that. No test depends on the sentence.

## Wave: DISTILL / [REF] Completeness and mandate notes

- Project conventions override the skill's Python mechanics, so there is no Python `state_delta` port.
  The "exactly this changed, nothing else did" check is made through what the ports expose: the
  collector's message count, its exact outbound field set, and "the collector is empty", all read
  below `HttpClient`.
- There is no property-based testing and no Tier B. These are layer-3 scenarios with real HTTP and
  SQLite, where failure paths are listed out one by one. The input space is a one-member closed list
  and a boolean, fully enumerated in B4–B8.
- AT-completeness was reviewed by category rather than scored item by item:
  - error contracts: B3–B8, F3–F5
  - the switch in both directions: B1, B2, F1, F2, and F3 for the veto
  - consent states: B3
  - the fields each event may carry, in both directions: B4–B9
  - environment matrix: N/A, there is no DEVOPS for this feature
  - concurrency: N/A, one administrator makes one switch
  - no open specification gaps

## Wave: DISTILL / [REF] Review Gate (2026-09-24)

| Reviewer | Scope | Verdict | Disposition |
|----------|-------|---------|-------------|
| Product owner | DISCUSS | rejected (3 "blockers", 1 medium) | **Fixed:** `job_id` now names the jobs.yaml job slice 04 serves (`job-maintainer-know-if-a-shipped-feature-landed`); AC-1.8 added for E6. **Held, not defects:** the ADO item awaits the maintainer's go (creating it needs confirmation), and E5's finality awaits the maintainer's confirmation (the reversal path is written down in E5) |
| Solution architect | DESIGN | "rejected, 12 blockers" | **Overruled.** Every "blocker" is a production change the component table *specifies* and DELIVER has not made yet: the reviewer checked the code instead of the design. Its own summary says the design is "solid and handoff-ready", confirms E5 and the both-ways shape check can be built, and finds the privacy boundaries intact |
| Platform architect | DEVOPS N/A | approved (2 low) | Noted: CI and E2E cannot leak into the census. `PostHogUsageDataPublisher.WhereThisOneSends()` returns null for a build nobody published, and no workflow sets `UsageData__CollectorBaseUrl`. The PostHog insight for the capability-use KPI is the maintainer's to build after release |
| Acceptance designer | DISTILL | approved (1 low) | B7/B8 rely on slice 04's cross-file control. By design, and recorded |

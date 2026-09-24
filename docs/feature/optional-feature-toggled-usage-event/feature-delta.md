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
| E3 | **The list of settings is the event's own closed enum, `UsageDataOptionalFeature`,** not the product's key strings. This follows the same reasoning as `UsageDataWorkTrackingSystem`: what leaves an instance is decided by a list written for that purpose. Members: `FeatureOrder`, `NeverSendUsageData`. `DeltaSync` is absent because story #5913 removes it. A future setting reaches the census only when somebody adds it to that list deliberately. | Locked |
| E4 | **Sent only after the server accepted the change.** The settings screen flips the switch before the server answers. The event fires after `updateFeature` resolves, never on the click, and never when the write is refused, for example the 403 a Community instance gets on a premium row. | Locked |
| E5 | **Switching the veto on is never reported, and that is correct.** When an administrator switches on *Never send usage data*, the gate drops every batch from that moment on, including the one saying so. Switching it back off is reported, because it is sent after the veto is lifted. The disclosure row says this in plain words. No special case is written: it follows from where the gate already sits. | Locked |
| E6 | **A setting the browser does not recognise is not reported.** The browser maps the setting keys it knows to the enum through a `Record`. A key it does not know sends nothing, rather than a guess or a placeholder. | Locked |
| E7 | **Terminology.** The disclosure row names the settings as the settings table does: *Let Lighthouse own the order of your Features* (default term), *Never send usage data*. | Locked |

## Wave: DISCUSS / [REF] User Stories

### US-01 — See which behaviour settings people switch, and which way

As the **product owner**, I want an event every time a behaviour setting is switched on an instance
whose browser agreed to share usage data, saying which setting and which way, so I can tell a setting
nobody touches from one people keep switching off.

`job_id: OUT-usagedata-capability-use` (Epic #5733 slice 04 outcome; no `jobs.yaml` entry of its own)

#### Elevator Pitch
Before: the census shows that people open settings pages and connect trackers, but nothing about which behaviour settings they change.
After: an administrator whose browser agreed switches *Let Lighthouse own the order of your Features* on under **Settings → System**. The collector receives `OptionalFeatureToggled` with `optional_feature: FeatureOrder, enabled: true`.
Decision enabled: whether a behaviour setting stays optional, becomes the default, or goes away.

#### Acceptance Criteria
- **AC-1.1** A browser that agreed switches *Feature Order* on, and the switch is accepted: exactly one `OptionalFeatureToggled` event with `optional_feature = FeatureOrder`, `enabled = true` reaches the collector. Switching it back off sends one more event with `enabled = false`.
- **AC-1.2** Switching *Never send usage data* **off** sends `optional_feature = NeverSendUsageData`, `enabled = false`. Switching it **on** sends nothing to the collector.
- **AC-1.3** A switch the server refuses, such as a premium row on an instance without a premium licence, sends nothing.
- **AC-1.4** A browser that refused, never answered, or withdrew sends nothing when it switches a setting. The existing gate covers this; one scenario proves the new event inherits it.
- **AC-1.5** The backend refuses an `OptionalFeatureToggled` message missing either field, and any other event carrying either field. The whole batch is rejected, as for the connector kind.
- **AC-1.6** Nothing else about the setting travels: not its key string, name or description. The emit-seam field list gains exactly `optional_feature` and `enabled`.
- **AC-1.7** `docs/settings/usagedata.md` gains the event row and the two field rows, and states plainly that switching the veto on is never reported (E5). `UsageDataDisclosureTest` stays green, which proves the doc and the enum agree.

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
| `UsageDataOptionalFeature` | `Models/UsageData/UsageDataOptionalFeature.cs` | **NEW** | `FeatureOrder = 0`, `NeverSendUsageData = 1`, with the same "zero is a real answer" note as its siblings |
| `UsageDataEventReported` | `Models/UsageData/UsageDataEventReported.cs` | EXTEND | Two nullable members: `UsageDataOptionalFeature? OptionalFeature`, `bool? Enabled` |
| `UsageDataEventShapes.Fits` | `Models/UsageData/UsageDataEventShapes.cs` | EXTEND | A third declaration, `EventsThatSayWhichSettingWasSwitched`, checked both ways round for **both** fields together. One is never present without the other |
| `UsageDataEventBatchDto` | `API/DTO/UsageDataEventBatchDto.cs` | EXTEND | Two nullable enum/bool fields, closed values only; still no `string` property |
| `UsageDataController` (the read) | `API/…/UsageDataController.cs` | EXTEND | Passes the two fields into `UsageDataEventReported`; the refusal path is unchanged |
| `PostHogUsageDataPublisher` | `Services/Implementation/UsageData/PostHogUsageDataPublisher.cs` | EXTEND | Two `[JsonPropertyName]` members, `optional_feature` and `enabled`, omitted when null as `work_tracking_system` is |
| `UsageDataEmitSeamArchUnitTest` | tests | EXTEND | The written field list gains `enabled` and `optional_feature` |
| TS mirrors | `Lighthouse.Frontend/src/models/UsageData/UsageData.ts` | EXTEND | The name; `UsageDataOptionalFeature` as a string-valued mirror; `NoticedEvent` gains the two optional fields |
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

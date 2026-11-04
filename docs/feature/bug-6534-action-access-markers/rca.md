# RCA: Bug #6534 - single-connection GET has no RBAC guard, and nothing makes an action declare its access

Date: 2026-11-04 · Investigator: Rex (nw-troubleshooter) · Read-only: no production or test code changed, nothing built or run.
Method: every claim below was read in the code or in `git`; claims that were inferred are marked **(inferred)**, claims not
checked are marked **(not verified)**. Census numbers come from a script (`census.py` in the session scratchpad) that parses
`Lighthouse.Backend/Lighthouse.Backend/API/*.cs`, cross-checked against two independent `grep` counts (251 `[Http*]`
attributes, 78 `[RbacGuard` attributes). It is a careful text parse, not a reflection run (no built assembly exists in the
checkout).

## 0. Verdict in five lines

1. `GET /api/{v1,latest}/worktrackingsystemconnections/{id}` is open to every signed-in user. The guard was removed
   deliberately in `e0aef89bb` (class-level `[RbacGuard]` replaced by guards on PUT and DELETE only) and never restored.
2. It is one of **3** actions that need a real guard. The other two are `POST /teams/validate` and `POST /portfolios/validate`,
   which carry **no** `RbacGuard` (the session notes saying they carry `AnyScopedAdmin` are wrong), and which a signed-in
   Viewer can use to run attacker-chosen queries against the tracker with the stored credential.
3. True census: **251** actions = **171** guarded (65 at action level, 106 at class level only) + **14** `AllowAnonymous`
   + **66** with no declaration of the three kinds the Bug asks for. Those 66 = **28** with no marker at all + **38** that
   carry only `[Authorize]`.
4. Of the 66: 3 need a real guard, 16 are legitimately any-signed-in-user, 45 are checked inside the handler or service
   (cited), 2 are suspected holes (low). All "check first" endpoints from the Bug except the two validates are
   checked in-handler and are not holes; two adjacent weaknesses were found inside them.
5. An unmarked action requires sign-in only when `Auth.Enabled` is true (fallback policy); with auth off it is open to the
   network, and with RBAC off every guard passes. The reflection test can therefore be exact, with no allowlist.

## 1. Root cause chain (Toyota 5 Whys)

PROBLEM: any signed-in user can `GET` one connection and receive the full DTO (tracker URL, user name, authentication
method, additional-field definitions, write-back mappings, defaults, concurrency token). No test or rule notices which
actions have no access declaration.

### Branch A - the single-connection GET

- **WHY 1A: the action has no access attribute.** Evidence: `API/WorkTrackingSystemConnectionController.cs:33-41` (`[HttpGet]`
  only); class (lines 16-23) carries no `[RbacGuard]`; only PUT (`:84-85`) and DELETE (`:150-151`) carry
  `[RbacGuard(RbacGuardRequirement.SystemAdmin)]`.
- **WHY 2A: the class-level guard was removed on purpose to unblock scoped admins, and the GET was left open.** Evidence:
  `git show e0aef89bb` (2026-05-12 12:54): `-    [RbacGuard]` on the class, `+[RbacGuard(SystemAdmin)]` added to exactly two
  methods (PUT, DELETE). Its message: "Any authenticated user can safely list / read connections" because secrets are redacted
  (`WorkTrackingSystemConnectionOptionDto.cs:28`: `Value = IsSecret ? string.Empty : option.Value`). From `77c88bf1b`
  (2026-05-09) until then the GET was SystemAdmin.
- **WHY 3A: five hours later the list was re-guarded, but only on the plural controller; the singular GET was never revisited
  and no test pinned its access.** Evidence: `c389e80be` (2026-05-12 18:05) touches only
  `WorkTrackingSystemConnectionsController.cs` (`[RbacGuard(SystemAdmin)]` on the list, new `/summary` with `AnyScopedAdmin`).
  ADR-006's decision names the list endpoint only. `WorkTrackingSystemConnectionControllerTest.cs:67,90` calls the action
  method directly, so no filter runs. Nobody had an owner for "the other endpoint on the same resource".
- **WHY 4A: access is opt-in per action and the default is open, so removing a class-level guard silently opens every
  action that did not get a method-level one.** Evidence: `RbacGuardAttribute.cs` is an additive filter; the only default is
  the fallback policy, which asks for sign-in only (`Program.cs:833-836`); `[RbacGuard]` is not `IAuthorizeData`, so nothing
  tells ASP.NET an action was left undecided. The Bug's own 66-of-251 figure is the same fact at scale.
- **WHY 5A (ROOT CAUSE A): the codebase has no rule, in code or in tests, that every controller action states who may call
  it.** `Tests/Architecture` holds 60-odd architecture tests and none enumerates actions. The only access pins are
  per-endpoint tests that were rewritten together with the change they were meant to guard (`e0aef89bb` "Reflection-based
  pin tests updated to assert the new shape").

### Branch B - the two validate endpoints (same failure, same 32-minute window)

- **WHY 1B: `POST /teams/validate` and `POST /portfolios/validate` have no `RbacGuard`.** Evidence:
  `TeamsController.cs:182-183` and `PortfoliosController.cs:123-124` carry `[LicenseGuard(...)]` only. `AnyScopedAdmin` appears
  in exactly one controller: `WorkTrackingSystemConnectionsController.cs:80` (`/summary`). The notes' claim is false.
- **WHY 2B: the guard was dropped, not narrowed, to fix a scoped-admin 403.** Evidence: `6e23f31b6` (2026-05-12 13:26,
  32 minutes after `e0aef89bb`): `-[RbacGuard(CanCreateTeam)]`; message "reachability by any authenticated user is
  acceptable ... Frontend page-level gating prevents unauthorised users".
- **WHY 3B: a test now pins the absence of the guard as correct.** Evidence: `CreateRightsControllerGuardTest.cs:35-56`
  (`ValidateTeamSettings_HasNoRbacGuardAttribute`, `ValidatePortfolioSettings_HasNoRbacGuardAttribute`).
- **WHY 4B: no requirement fitted at the time; the one that fits arrived four and a half hours later and was applied to one
  endpoint.** `AnyScopedAdmin` (SystemAdmin or any Team/Portfolio admin grant) was added in `c389e80be` for `/summary` only.
  The route has no scope id (the body carries `Id`), so `TeamWrite` cannot be declared on it.
- **WHY 5B (ROOT CAUSE B): a trade-off made under time pressure ("keep the scoped admin's Save working") was recorded as a
  permanent expectation by a test, and the frontend-gating argument stood in for a server-side check.** Same enabler as A: no
  rule forces the question to be asked again.

### Branch C - why the same mistake repeated across 66 actions

- **WHY 1C: 66 actions carry none of RbacGuard / AllowAnonymous / a marker.** Evidence: census (section 3).
- **WHY 2C: they are a mix of three things that look the same in the source:** actions open on purpose (16), actions whose
  access is decided in the handler (45), and actions nobody decided (5 incl. the 3 holes and 2 suspects).
- **WHY 3C: there is no vocabulary for the first two kinds.** The only way to say "this is open on purpose" is a code
  comment (`AppSettingsController.cs:38` "Deliberately NOT guarded"; `SystemInfoController.cs:20-26`), which no tool reads.
  `[Authorize]` is used for it on 12 controllers but means only "somebody" (`SystemInfoController.cs:94-96` says so).
- **WHY 4C: the declarative guard cannot express the handler checks** (scope resolved from a delivery id, a body, or the
  result set: `DeliveriesController.cs:411-413` "the declarative guard has no scope to resolve"), so authors reasonably wrote
  them inline, and inline checks are invisible to anything but a human reader.
- **ROOT CAUSE C (= A): no machine-checked declaration, no marker with a reason.** This is what the Bug's fix adds.

### Backward validation

- A + `e0aef89bb` content: GET unguarded, PUT/DELETE guarded, list re-guarded later -> exactly today's state. Yes.
- B + `6e23f31b6` + the pin test -> validate open, test green. Yes.
- C -> any unannotated or `[Authorize]`-only action passes review and CI. Yes (66 of them).
- No contradictions: A and B are the same enabler (C) at two sites.

## 2. Contributing factors

- Security fix bundled with three other stories in one commit (`c389e80be` message: "ship as one commit rather than
  hunk-split"), so the singular controller was outside any story's checklist.
- ADR-006's frontend half never shipped for six months (documented in the 6533 write-up), which kept "just open it" as the
  live workaround; both holes are workarounds for the same 403 on the Settings tab.
- Redaction was treated as sufficient: "secrets are redacted, so reading is safe". ADR-006 itself lists the non-secret
  metadata as sensitive; that did not reach the singular GET.
- `GET /worktrackingsystemconnections/{id}` also **writes**: `EnsurePredefinedAdditionalFieldsRegistered`
  (`WorkTrackingSystemConnectionController.cs:38,43-82`) calls `repository.Update` and `Save()` on a read. Any signed-in
  user's GET can change the stored connection. (verified by reading; side effect only when a predefined field drifted.)
- The CLI and MCP clients (outside this repo) call this GET, which kept the endpoint "in use" in people's heads.
- Test hosts run with RBAC off, so integration tests that GET the connection pass either way
  (`Slice05PredefinedFieldSpecifications.cs:93`, `ConnectionSecretStatePayloadTests.cs:253`,
  `WriteBackMappingForNewAdditionalFieldIntegrationTest.cs:185`).

## 3. Census (section B)

### 3.1 What an unmarked action means today

| Mode | Unmarked / `[Authorize]`-only action | `[RbacGuard]` | Evidence |
|---|---|---|---|
| `Auth.Enabled=true` | Sign-in required: fallback policy `RequireAuthenticatedUser` | Fallback still applies, then the filter runs | `Program.cs:833-836`; `RbacGuardAttribute : Attribute, IAsyncAuthorizationFilter`, not `IAuthorizeData`, so it does not replace the fallback |
| `Auth.Enabled=true`, `[AllowAnonymous]` | Open to the public | Filter would still run if present | `Program.cs:831-832` comment |
| `Auth.Enabled=false` | Open. `DisabledAuthenticationHandler` signs every request in as `lighthouse\|auth-disabled`; no fallback policy | Passes (see next row) | `Program.cs:820-828`; `Auth/DisabledAuthenticationHandler.cs:19-29` |
| RBAC off (`Authorization:Enabled=false`) | n/a | Every requirement returns true except scope ones with no id (`SystemAdmin` is `!IsRbacEnforced || CanManageRbac`; `TeamRead` etc. return true via `CanReadTeamAsync`) | `RbacAdministrationService.cs:46-49,342-368` |
| RBAC on | n/a | Gate: premium licence **and** a system admin must exist, else every non-`SystemAdmin` requirement is false | `RbacAdministrationService.cs:IsEnforcementGateSatisfiedAsync` |

Other facts: a guard whose scope key is not a route value answers 500 (`RbacGuardAttribute.cs:36-41`), `Check=false` is used
nowhere (grep). A missing `IRbacAdministrationService` lets the call through (`:30-33`). Whether RBAC can be switched on
with auth off is **(not verified)**; the code shows no validation.

### 3.2 Totals (verified by script, cross-checked by grep)

| Bucket | Actions |
|---|---|
| Controller actions (`[Http*]` attributes in `API/*.cs`) | **251** (matches the Bug) |
| `[RbacGuard]` at action level | **65** (6 of them also sit under a class-level guard: Team/PortfolioReports create/edit/delete) |
| `[RbacGuard]` at class level only | **106** (13 controllers) |
| Guarded in total | **171** (78 `[RbacGuard` attributes = 65 + 13 class-level) |
| `[AllowAnonymous]` | **14** (matches the Bug) |
| No marker of the three kinds | **66** (matches the Bug) |
| ...of which carry nothing at all | 28 |
| ...of which carry only `[Authorize]` | 38 |

No inherited actions today (`ReportsControllerBase` declares none), so the test must still handle them but nothing
depends on it yet. Not controller actions, outside the Bug's scope: `/health/*` anonymous, Prometheus scrape anonymous when
telemetry is on (`TelemetryConfigurator.cs:31`), `/.well-known/security.txt` (`Program.cs:232`, no `AllowAnonymous`, so
under the fallback it needs sign-in **(inferred)**).

### 3.3 The 14 `AllowAnonymous` actions

| Action | Verdict |
|---|---|
| `GET /auth/mode`, `GET /auth/login`, `POST /auth/logout`, `GET /auth/session` (`AuthController.cs:26,35,52,69`) | Must be anonymous (sign-in bootstrapping). OK |
| `GET /embed/enter`, `GET /embed/start`, `GET /api/.../embed/handshake/{nonce}` | Anonymous by design (ADR-137 hops, rate limited, `EmbedEntryController.cs:13-18`, `EmbedHandshakeController.cs:13-17`). OK |
| `GET /api/oauth/callback` (`OAuthController.cs:29`) | Anonymous by necessity; protected by the OAuth state token (`OAuthStateTokenInvalidException` handling `:56-66`). OK |
| `GET/POST/DELETE /usagedata/*` x5 | Anonymous by documented design, class comment `UsageDataController.cs:12-20`; rate limited; per-browser token. Anonymous `POST /usagedata/consent` mints a row per call, bounded only by the rate limiter. Acceptable, mention in the marker review |
| `GET /version/current` | Anonymous by documented need (`VersionController.cs:25-26`, Jira app and platform probe). OK |

None should change. Nothing signed-in-only is wrongly anonymous.

### 3.4 The 66 unmarked actions

Categories: **(1)** needs a real guard · **(2)** legitimately any signed-in user · **(3)** checked inside the handler or service
(check cited) · **(4)** suspected hole. "N" = no marker, "A" = `[Authorize]` only. FE = frontend, file under
`Lighthouse.Frontend/src`.

| # | Controller | Verb + route | Has | Cat | Evidence | Callers |
|---|---|---|---|---|---|---|
| 1 | WorkTrackingSystemConnection | GET `/worktrackingsystemconnections/{id}` | N | **1** | `WorkTrackingSystemConnectionController.cs:33`; full DTO; GET writes (`:38`) | FE: none. E2E: none (only POST/DELETE/list). CLI `connection get`, MCP get-connection (external repo `lighthouse-clients/packages/client/src/index.ts:2343`). Integration tests with RBAC off |
| 2 | Teams | POST `/teams/validate` | N | **1** | `TeamsController.cs:182-204`; runs the Team's query on the tracker with the stored credential | FE `TeamService.ts:101`: `EditTeam`/`ModifyTeamSettings` (post-save, `useModifySettings.ts:216,486`, gated by `canSave`), `CreateTeamWizard`. Roles: SystemAdmin creators, TeamAdmin editors |
| 3 | Portfolios | POST `/portfolios/validate` | N | **1** | `PortfoliosController.cs:123-145` | FE `PortfolioService.ts:147`: `EditPortfolio`, `ModifyProjectSettings`, `CreatePortfolioWizard`. Same roles |
| 4 | AppSettings | GET `/appsettings/FeatureOrdering` | N | 2 | `AppSettingsController.cs:38-44` comment "Deliberately NOT guarded" | `SettingsService.ts:53`, every feature list |
| 5 | BlackoutPeriods | GET `/blackout-periods` | N | 2 | Instance calendar; writes are SystemAdmin | `BlackoutPeriodService.ts:21` |
| 6 | RecurringBlackoutRules | GET `/recurring-blackout-rules` | N | 2 | same | `RecurringBlackoutRuleService.ts:26` |
| 7 | OptionalFeatures | GET `/optionalfeatures`, GET `/optionalfeatures/{key}` | N | 2 | Flags that gate UI for everybody; write is SystemAdmin (`:39-40`) | `OptionalFeatureService.ts:20,32` |
| 8 | Version | GET `/version/hasupdate`, `/new`, `/updateSupported`, `/distribution` (4) | N | 2 | Public release data; `/new` calls GitHub (shared unauthenticated quota, low) | `VersionService.ts:28,36,46,64` |
| 9 | WorkTrackingSystemConnections | GET `/worktrackingsystemconnections/supported` | N | 2 | Default templates per tracker, no stored data (`:44-54`) | `WorkTrackingSystemService.ts:102` (wizards) |
| 10 | Deliveries | GET `/deliveries/{id}/metrics-history` | N | 3 | `DeliveriesController.cs:114-121` `PortfolioRead` on the delivery's portfolio, else 403 | `DeliveryService.ts:252` |
| 11 | Deliveries | PUT `/deliveries/{id}` | N | 3 | `:182-189` `PortfolioWrite` | FE delivery edit |
| 12 | Deliveries | DELETE `/deliveries/{id}` | N | 3 | `CheckScopeAsync :312,415-433` `PortfolioWrite` | FE |
| 13 | Deliveries | POST `/deliveries/{id}/archive`, `/unarchive` (2) | N | 3 | `:327,356` via `CheckScopeAsync` | `DeliveryService.ts:208,216` |
| 14 | DeliveryNotes | GET, POST, PUT, DELETE `/deliveries/{id}/notes[/{noteId}]` (4) | N | 3 | `DeliveryNotesController.cs:28,48,82,118` via `CheckScopeAsync :204-221` (Read for GET, Write otherwise) plus author rule `MayModify` | `DeliveryService.ts:261-293` |
| 15 | Portfolios | GET `/portfolios` | N | 3 | `PortfoliosController.cs:36-60` returns only readable ids | list pages |
| 16 | Teams | GET `/teams` | N | 3 | `TeamsController.cs:36-50` readable ids only | list pages |
| 17 | Reports | GET `/reports`, GET `/reports/creation-options` (2) | N | 3 | `ReportsController.cs:24-32` `ReadableOwners()` | FE reports page |
| 18 | Reports | POST `/reports/bulk`, POST `/reports/bulk-removal` (2) | N | 3 | per-owner `MayWriteAsync` -> `CanWriteTeam/PortfolioAsync` (`ReportOwnerWriteAccess.cs:12-17`) called in `ReportBulkCommands.cs CreateFor` and `ReportBulkRemover.cs Remove`; refused owners return `NotAdmin` | `ReportService.ts:131,148` |
| 19 | ApiKey | POST, GET, DELETE `/apikeys` (3) | A | 3 | Owner-scoped by `sub` (`ApiKeyController.cs:35-46,94-103,111-125`); create refuses scope broader than the caller's own (`:52-66`) | FE API-key settings, any user |
| 20 | Auth | GET `/auth/me` | A | 2 | Returns the caller's own profile (`AuthController.cs:90-115`) | shell |
| 21 | Authorization | GET `/authorization/status` | A | 2 | `RbacStatus`: enabled, licence gate, has-admin, emergency-admin configured, unassigned user count, group claim name (`:16-22`). Low sensitivity, the shell needs it | `RbacService.ts:49` |
| 22 | Authorization | GET `/authorization/my-summary` | A | 2 | Caller's own roles (`:24-30`) | `useRbac()` only |
| 23 | Authorization | POST `/authorization/bootstrap/system-admin` | A | 3 | Service refuses unless no admin exists (`:37-52`, `AlreadyBootstrapped`) | bootstrap page |
| 24 | Authorization | GET users; POST/DELETE system-admins; DELETE users; GET/POST/PUT/DELETE group-mappings (8) | A | 3 | `CanManageRbacAsync` then `Forbid()`: `:63,79,105,131,308,356,403,439` | RBAC settings page (SystemAdmin) |
| 25 | Authorization | GET/PUT/DELETE team members; GET group-mappings of a team (4) | A | 3 | `CanManageTeamMembershipAsync` `:156,177,212,323` | Team access tab |
| 26 | Authorization | GET/PUT/DELETE portfolio members; GET group-mappings of a portfolio (4) | A | 3 | `CanManagePortfolioMembershipAsync` `:232,253,288,338` | Portfolio access tab |
| 27 | Features | GET `/features`, `/features/ids`, `/features/references`, `/features/{id}/workitems` (4) | A | 3 | Result filtered by readable portfolios (`FeaturesController.cs:80-86,258-296`; `{id}/workitems` answers 404 for unreadable `:112-117`) | feature lists |
| 28 | Features | PATCH `/features/{id}/rank` | A | 3 | `:131-158`: premium licence, `ManualOrder` policy, then `CanMove` = write on **every** portfolio of the Feature (`FeatureMoveAuthorization.cs VerdictFor`), else 403 | `FeatureService.ts:84`; UI shows move only when the DTO's `canMove` is true |
| 29 | Suggestions | GET workitemtypes/teams, /projects; states/teams, /projects (4) | A | 3 | `GetReadableTeams/GetReadablePortfolios` (`SuggestionsController.cs:96-122`) | settings forms |
| 30 | SystemInfo | GET `/systeminfo` | A | 3 | Two fields withheld from non-admins (`SystemInfoController.cs:44-53,69-90`) | shell |
| 31 | SurveyNudge | GET `/surveynudge` | A | 2 | Next-eligible date only | `SurveyNudgeService.ts:21` |
| 32 | Terminology | GET `/terminology/all` | A | 2 | Every screen renders it | `TerminologyService.ts:16` |
| 33 | Update | GET `/update/status` | A | 2 | Counts only (`UpdateController.cs:29-39`); task list is SystemAdmin | `UpdateSubscriptionService.ts:288` |
| 34 | License | GET `/license` | A | **4** (low) | `LicenseStatusDto.cs` returns name, **email**, organisation, licence number to every signed-in user, Viewers and Jira-embed viewers included. The shell needs only `canUsePremiumFeatures`/`isValid` (`useLicenseRestrictions.ts:40`); the dialog shows the rest (`LicenseStatusDialog.tsx:211-239`). A maintainer call: redact for non-admins as `SystemInfo` does | `LicensingService.ts:19`, `SurveyNudge.tsx:49`, `SystemSettingsTab.tsx:47` |
| 35 | SurveyNudge | POST `/surveynudge` | A | **4** (low) | `SurveyNudgeController.cs:28-37` writes an instance-wide app setting (`RecordSurveyNudgeAction`) for any signed-in user, so one Viewer dismissing it snoozes it for everybody. Perhaps intended; state it in the marker or restrict | `SurveyNudgeService.ts:28` |

Tally by category, counting the actions inside the multi-action rows: **(1) 3, (2) 16, (3) 45, (4) 2 = 66.** Rows 1-18 hold
the 28 no-marker actions; rows 19-35 hold the 38 `[Authorize]`-only actions (ApiKey 3, Auth 1, Authorization 19, Features 5,
Suggestions 4, SystemInfo 1, SurveyNudge 2, Terminology 1, Update 1, License 1).

## 4. Verdicts on the "check first" list

| Endpoint | Verdict | What a low-privilege signed-in user can do | Callers and roles |
|---|---|---|---|
| `PATCH /features/{id}/rank` | **Not a hole.** Cat 3. Class `[Authorize]`, `LicenseGuard(RequirePremium)`, handler requires write on every portfolio of the Feature (`FeaturesController.cs:131-158`, `FeatureMoveAuthorization.cs`). **Adjacent weakness (low, verified by reading):** the *target* Feature in `beforeFeatureId`/`afterFeatureId` is not authorised (`FeatureRankingService.cs:45-53`): `Placed`->200 vs `TargetNotFound`->404 tells a caller whether any Feature id exists, and `WriteThePlaces` rewrites `ManualRank` of the whole instance order. No cross-scope data is returned | Viewer: 403. Portfolio admin: moves only Features whose portfolios they all write | `FeatureService.ts:84`; UI gates on `canMove` from the DTO |
| `PUT/DELETE /deliveries/{id}`, `POST .../archive`, `.../unarchive` | **Not holes.** Cat 3: `PortfolioWrite` on the delivery's portfolio, in the handler (`DeliveriesController.cs:182-189,312,327,356`), because the route has no portfolio id (`:411-413`). Archive also needs premium (`:333`). **Adjacent weakness (suspected, medium-low, not exercised):** manual selection accepts Feature ids from any portfolio (`CreateManualFeatureSelectionDelivery :608-624`, `DeliveryRepository.GetFeaturesByIds :92-99` has no portfolio filter), and the delivery DTO returns those ids plus their likelihoods (`DeliveryWithLikelihoodDto.cs:43,45,95-96`). A Portfolio admin of P1 can pull a Feature of P2 into a P1 delivery and read its forecast. Same on create (`:129-169`) | Viewer: 403 on writes | `DeliveryService.ts`; role: PortfolioAdmin, SystemAdmin |
| `POST/PUT/DELETE .../notes` (and GET) | **Not holes.** Cat 3 `CheckScopeAsync` (`DeliveryNotesController.cs:204-221`), refuses archived deliveries, author-only edit | Viewer reads notes of readable portfolios only; cannot write | `DeliveryService.ts:261-293` |
| `POST /reports/bulk`, `/reports/bulk-removal` | **Not holes.** Cat 3, per-owner write check in the services (`ReportBulkCommands.cs CreateFor`, `ReportBulkRemover.cs Remove`); a refused owner gets `NotAdmin`. Low: removal answers `AlreadyGone` for unknown ids and `NotAdmin` for known ones, which lets a caller probe report ids | Viewer: every owner refused | `ReportService.ts:131,148` |
| `POST /teams/validate`, `POST /portfolios/validate` | **Real holes, cat 1.** No `RbacGuard` (only `LicenseGuard`). The notes saying they carry `AnyScopedAdmin` are wrong; so is "confirmed hole carrying" if read as "has a guard". What a Viewer can do: pick any stored connection id and send any query and state mapping; the server runs it against Jira/ADO/etc. with the stored credential (`JiraWorkTrackingConnector.cs:1255-1293`: `GetIssuesByQuery(..., 10)`), and learns whether it matched (`no_work_items_found`) plus `exception.Message` on failure. That is a blind boolean oracle over the tracker with the admin's credential, and an error-text leak. Does not return issue content and cannot change the connection's URL. Other connectors **(not verified)** | Callers listed in rows 2 and 3. All save paths of SystemAdmin creators and Team/Portfolio admins. No Viewer path in the UI (`canSave` gate). Server-side check absent |
| `GET /worktrackingsystemconnections/{id}` | **Real hole, cat 1** (see 5.1) | Reads the full connection (non-secret options such as Jira `Url` and `Username`, auth method, field definitions, write-back mappings, defaults, concurrency token) and triggers a write | See 5.1 |

### The two session-note claims

1. "`POST /teams/validate` and `/portfolios/validate` carry `AnyScopedAdmin`": **false**. They carry no RBAC guard
   (`TeamsController.cs:182`, `PortfoliosController.cs:123`; `grep AnyScopedAdmin` finds one use, the `/summary` action). The
   "hole" half is right.
2. "The PUT response of the connection endpoint should be filtered": **moot**. PUT is `SystemAdmin` (`:85`) and returns
   `new WorkTrackingSystemConnectionDto(existingConnection)` (`:147`), the same secret-blanked shape; only a system admin (or
   anybody, with RBAC off, as for every write) can receive it. Nothing for a low-privilege caller to see.

## 5. Proposed fix

### 5.1 The Bug itself: guard the single GET

Add to `WorkTrackingSystemConnectionController.GetWorkTrackingSystemConnection` (`:33`):
`[RbacGuard(RbacGuardRequirement.SystemAdmin)]`, same as the list (`WorkTrackingSystemConnectionsController.cs:56`), the PUT
and the DELETE. Non-read requirement, so a denied caller gets 403, as on the list.

Caller check (the lesson of #6533), all verified by grep:
- Frontend: **no caller** of the by-id GET (`WorkTrackingSystemService.ts` has list, summary, supported, validate, POST, PUT,
  DELETE only). Edit connection reads the list (`EditConnection.tsx:53`), which is already SystemAdmin.
- E2E: no GET by id (`LighthouseFixture.ts:56-61` list + DELETE; `OAuthConnection.spec.ts` DELETE).
- Backend tests: unit tests call the action directly (no filter). Integration tests GET it with RBAC off (guard passes);
  they should stay green **(not run)**.
- `lighthouse-clients` (separate repo, `packages/client/src/index.ts:2343`, CLI and MCP `get connection`): a non-admin key
  that could read a connection by id now gets 403, consistent with the list it could not read already. No change needed
  there; mention in release note if the maintainer wants.
- Settings tabs read `/summary` (`TeamDetail.tsx`, `PortfolioDetail.tsx` call `getConnectionSummaries()`), not this GET.

Optional, same change or separate: the GET should not write. Move `EnsurePredefinedAdditionalFieldsRegistered` out of the
read path (or accept it now that only a system admin can trigger it). Not required by the Bug; call it out.

Add tests: a reflection pin on the action (the architecture test covers it), plus one HTTP test per role (Viewer 403,
Team Admin 403, System Admin 200) beside `S2_ConnectionListPayloadShapeTests.cs`.

### 5.2 The marker

New file `Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/Authorization/SignedInOnlyAttribute.cs`, next to
`RbacGuardAttribute.cs`:

```csharp
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public sealed class SignedInOnlyAttribute : Attribute
{
    public SignedInOnlyAttribute(string reason)
    {
        Reason = string.IsNullOrWhiteSpace(reason)
            ? throw new ArgumentException("Say why a signed-in user may call this.", nameof(reason))
            : reason;
    }

    public string Reason { get; }
}
```

Pure marker. It must **not** implement `IAuthorizeData` or a filter: the fallback policy already asks for sign-in when auth is
on, and adding behaviour here would change 66 actions at once. Name stays as the Bug proposes; see open question 1 on the 45
handler-checked actions, for which "SignedInOnly" reads as a claim that nothing more is checked.

Reason convention (enforced only by length and non-blank, no reflection into prose): the sentence names either "any signed-in
user may" or the check that decides, e.g. `"Returns only the Teams the caller may read (GetReadableTeamIdsAsync)."`.

### 5.3 The reflection test

New `Lighthouse.Backend.Tests/Architecture/EveryActionDeclaresItsAccessArchitectureTest.cs`, NUnit, plain reflection (as
`API/CreateRightsControllerGuardTest.cs` does; the other Architecture tests use ArchUnitNET or Cecil, neither is needed).

Discovery, no allowlist:
- Controllers: `typeof(RbacGuardAttribute).Assembly.GetTypes()` where `!IsAbstract && typeof(ControllerBase).IsAssignableFrom`.
- Actions: `controller.GetMethods(Public | Instance)` where `DeclaringType` is not `object`/`ControllerBase` and the method is
  not `[NonAction]` and carries an `IActionHttpMethodProvider` attribute (covers `HttpGet/Post/Put/Delete/Patch/Head/Options` and
  `AcceptVerbs`). `GetMethods` on the concrete controller returns inherited public methods, so an action declared on a base is
  evaluated once per derived controller, with the **derived** class's attributes.
- Attributes: `method.GetCustomAttributes(inherit: true)` plus `controller.GetCustomAttributes(inherit: true)` (includes base
  classes). `RbacGuardAttribute` and `SignedInOnlyAttribute` are `Inherited = true`; `AllowAnonymousAttribute` is too.

Assertions:
1. Every action has at least one of `RbacGuardAttribute`, `AllowAnonymousAttribute`, `SignedInOnlyAttribute` at action or class
   level. `[Authorize]` does **not** count (this is what makes the 38 visible). Failure message lists `Controller.Action`.
2. No action is both `AllowAnonymous` and (`RbacGuard` or `SignedInOnly`), and none has both `RbacGuard` and `SignedInOnly`
   **at the same level**; an action-level declaration may override a class-level one of a different kind (the 6 report
   actions already stack class + action guards of the same kind).
3. Every `SignedInOnly.Reason` has at least ~25 characters of non-whitespace.
4. Every `RbacGuard` whose requirement needs a scope (`TeamRead/Write/Contribute`, `PortfolioRead/Write`) names a
   `ScopeIdRouteKey` that is a `{parameter}` in one of the action's route templates; otherwise the guard answers 500 at run
   time (`RbacGuardAttribute.cs:36-41`). Optional, small, catches a real class of error.
5. Sanity: the scan finds at least 40 controllers and 200 actions, so a broken filter cannot pass empty.

Also flip `CreateRightsControllerGuardTest.cs:35-56` (it pins "no guard" on both validates) to assert `AnyScopedAdmin`, or
delete it in favour of the architecture test plus the HTTP tests below.

A stronger variant, if the maintainer wants it: resolve actions through `IActionDescriptorCollectionProvider` from a test host,
which also sees conventions and minimal endpoints. Reflection is enough for the Bug and cheaper.

### 5.4 Exact changes per action

**Real guards (cat 1, 3 actions)**

| Action | Change | Why this requirement |
|---|---|---|
| `GET /worktrackingsystemconnections/{id}` | `[RbacGuard(RbacGuardRequirement.SystemAdmin)]` | Same as list/PUT/DELETE; no frontend caller |
| `POST /teams/validate` | `[RbacGuard(RbacGuardRequirement.AnyScopedAdmin)]` | Callers are SystemAdmin creators (`CanCreateTeam` resolves to SystemAdmin) and Team/Portfolio admins saving; route has no scope id; `AnyScopedAdmin` is true for exactly those (`RbacAdministrationService.cs:372-398`) and is what `/summary` already uses for the same population |
| `POST /portfolios/validate` | same | same |

A tighter in-handler check (`dto.Id == 0 ? CanCreate : CanWrite(dto.Id)`) buys little: a Team admin may pick any connection
(maintainer decision), so they can send any query anyway. Do not use `TeamWrite`/`CanCreateTeam`: that is what `6e23f31b6`
removed because it broke Save for scoped admins.

Frontend: no gating change needed. All validate calls sit behind `canSave` or the create wizards
(`EditTeam.tsx:44`, `ModifyProjectSettings.tsx:73`), which already derive from `useRbac()`. Re-check by hand with a Team Admin:
open Settings, change a field, Save, confirm the post-save validation still reports (`useModifySettings.ts:216,486`).

**Marker-only edits (cat 2, 3, 4): 63 actions in 21 controllers**, class-level where every action shares the reason,
action-level otherwise. Proposed reasons:

| Controller (actions) | Marker level | Reason (gist) |
|---|---|---|
| AppSettings.GetFeatureOrdering | action | Every feature list names its position column from it |
| BlackoutPeriods / RecurringBlackoutRules GetAll | action | Instance calendar every forecast view reads |
| OptionalFeatures GetAll, GetByKey | action | Flags the UI gates on for everybody |
| Version hasupdate/new/updateSupported/distribution | action | Public release data |
| WorkTrackingSystemConnections.supported | action | Default connection templates, no stored data |
| Deliveries x5, DeliveryNotes x4 | action/class | Scope resolved from the delivery id in the handler (`CheckScopeAsync`) |
| Portfolios.GetPortfolios, Teams.GetTeams | action | Returns only what the caller may read |
| Reports x4 | action | Readable owners (reads), per-owner write check (bulk) |
| ApiKey (3) | class | Owner-scoped by subject; scope limited to caller's own rights |
| Authorization (19) | class | Each action asks `IRbacAdministrationService` who may manage what it touches; status and my-summary answer for the caller |
| Features (5), Suggestions (4) | class | Filtered by readable portfolios / move verdict |
| SystemInfo.GetSystemInfo | action | Two fields withheld from non-admins |
| Auth.me, SurveyNudge.GetState, Terminology.all, Update.status | action | Caller's own or instance-wide non-sensitive data |
| License.GetLicenseStatus, SurveyNudge.RecordAction | action | Pending decision (cat 4, section 5.5) |

Keep `[Authorize]` where present (no behaviour change) or drop it where it is class-level and redundant with the marker; that
is the maintainer's call (open question 2).

### 5.5 What becomes a separate Bug, and what is fixed here

| Item | Severity | Where |
|---|---|---|
| `POST /teams/validate` and `POST /portfolios/validate` unguarded: any signed-in user runs queries with the stored tracker credential | **Medium** | **Separate Bug** (two endpoints, same fix and callers; one Bug is enough unless the maintainer wants one per endpoint) |
| Delivery manual Feature selection is not limited to the delivery's portfolio (cross-portfolio read of forecast through a delivery) | Medium-low **(suspected, not exercised end to end)** | **Separate Bug** after a failing test confirms it |
| `PATCH /features/{id}/rank` target Feature not authorised (existence oracle) | Low | Fold into the Delivery Bug or a Low Bug; maintainer's call |
| `GET /license` shows licensee name, email, organisation, number to every signed-in user | Low (suspected) | Decide first (design); Bug only if redaction is wanted |
| `POST /surveynudge` lets any signed-in user snooze an instance-wide prompt | Low (suspected) | Decide first; may be fine, then marker with that reason |
| `GET /worktrackingsystemconnections/{id}` | **Medium** | **This Bug (#6534)** |
| `RemoveEach` distinguishes unknown from forbidden report ids | Low | Mention in the Reports finding, no Bug unless wanted |

The Bug asks for "a separate Bug for each real hole found": the validates are the only new real hole besides the one being
fixed. #6534 itself fixes the GET, adds the marker, test and the 66 markers. Whether the validate guard rides along in #6534
or in its own Bug is open question 3.

## 6. Files affected (proposed)

Production:
- `API/WorkTrackingSystemConnectionController.cs` (guard on GET; optionally move the write out of the read)
- `API/TeamsController.cs`, `API/PortfoliosController.cs` (validate guard, if fixed here)
- New `Services/Implementation/Authorization/SignedInOnlyAttribute.cs`
- Markers on: `AppSettingsController`, `BlackoutPeriodsController`, `RecurringBlackoutRulesController`, `OptionalFeaturesController`,
  `VersionController`, `WorkTrackingSystemConnectionsController`, `DeliveriesController`, `DeliveryNotesController`,
  `PortfoliosController`, `TeamsController`, `ReportsController`, `ApiKeyController`, `AuthController`, `AuthorizationController`,
  `FeaturesController`, `SuggestionsController`, `SystemInfoController`, `SurveyNudgeController`, `TerminologyController`,
  `UpdateController`, `LicenseController`
- `docs/ci-learnings.md` only if the maintainer wants a ledger line (the test is the automated check, so probably none)

Tests (`Lighthouse.Backend.Tests`):
- New `Architecture/EveryActionDeclaresItsAccessArchitectureTest.cs`
- `API/CreateRightsControllerGuardTest.cs` (flip or remove lines 35-56)
- HTTP role tests beside `API/Security/S2_ConnectionListPayloadShapeTests.cs` (GET by id, both validates)

Docs: `docs/product/architecture/adr-006-connection-list-payload-shape.md` (amendment: the by-id GET) and `docs/settings/rbac.md`
if it lists endpoint access **(not read)**. ADR-270/271 are not touched (no new context or surface).

Frontend: none expected. `lighthouse-clients`: none.

## 7. Risk assessment and what could not be verified

Risks of the fix
- **GET guard:** low. No frontend or E2E caller. Behaviour change for non-admin CLI/MCP keys on RBAC-on instances only.
- **Validate guard:** medium, because it is exactly the shape that caused #6533 and `6e23f31b6`. `AnyScopedAdmin` is chosen
  because every caller is an admin of some kind. Residual: `AnyScopedAdmin` is false when the licence gate fails or no system
  admin exists (`:372-387`), where the same user could not save a Team either. Test by hand with a Team Admin and a
  Portfolio Admin on an RBAC-on instance before pushing; the `@rbac` E2E check was never run locally in #6533 either.
- **Test breadth:** the new test fails on first run until all 66 markers exist; that is intended and it must land in the same
  change. A `reason` check by length cannot judge honesty, so review of the 63 reasons is a human task.
- **Misleading marker name** on handler-checked actions (see question 1): someone later copies `SignedInOnly` onto an action
  that needs a guard because the name looks like the sanctioned default.
- **Reading 45 actions as "checked in handler" rests on the cited lines**; none was exercised with a Viewer principal.

Not verified
- No reflection run: the census is a text parse. Cross-checks: 251 `[Http*]` lines = 251 rows; 78 `[RbacGuard` lines = 65 + 13;
  14 anonymous and 66 unmarked match the Bug. A reflection run would also catch attributes added by conventions; none found.
- Nothing was built, no test run, no live-connector test.
- Whether RBAC can be enabled with auth disabled.
- Connector behaviour of `ValidateTeamSettings` other than Jira (what Linear/ADO/ServiceNow/CSV echo back).
- Whether the Delivery cross-portfolio Feature selection actually discloses data (read from DTO shape only).
- Which docs list endpoint access.
- The peer review by `nw-troubleshooter-reviewer` was not run (the brief limited this to a read-only investigation).

## 8. Open questions for the maintainer

1. **Marker name for handler-checked actions.** Keep one `SignedInOnly(reason)` for both "anyone signed in" (16) and "checked
   in the handler" (45), or add a second name (for example `AccessDecidedInHandler(reason)`) so the name does not understate
   what the action does? One marker is simpler; two is more honest.
2. **`[Authorize]` fate.** Keep it next to the marker, or replace it (the marker plus the fallback policy do the same job)?
3. **Validate guard in #6534 or its own Bug?** The Bug text says one Bug per real hole; they are two lines of code with an
   interacting test change.
4. **`GET /license` and `POST /surveynudge`:** intended for every signed-in user, or restrict/redact?
5. **GET with a side effect:** move `EnsurePredefinedAdditionalFieldsRegistered` off the read path now, or leave it behind the
   new guard?
6. **Class-level vs action-level markers** for `AuthorizationController` (19 actions, two reasons): one class-level reason,
   or per-action reasons so the two self-answering actions are not hidden behind the admin ones?

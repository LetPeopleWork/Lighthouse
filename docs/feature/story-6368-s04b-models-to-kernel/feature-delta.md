# Feature: story-6368-s04b-models-to-kernel
Story #6368 · S04b Reduce `Models` to the shared kernel + kernel-size rule · Epic #6510 (Modules: package the bounded
contexts) · catalogue Epic #6264

- Catalogue: [`docs/feature/epic-6264-modular-monolith/feature-delta.md`](../epic-6264-modular-monolith/feature-delta.md)
  (S04b block line 215, KPI 3 line 618, G4 line 72, the "Later, not this Story: … S04b" notes of S02, S03a-S03h).
- Brief: [`slices/slice-12-s04b-reduce-models-to-kernel.md`](../epic-6264-modular-monolith/slices/slice-12-s04b-reduce-models-to-kernel.md)
  (Size M; "Update after S03b (#6360, 2026-11-16)").
- Authority: [ADR-270](../../product/architecture/adr-270-bounded-contexts-context-map-and-module-layout.md)
  Decision 7 ("what the shared kernel may hold": shared ids, truly shared value objects, the domain-event base type;
  nothing else), Decision 3 (each event type lives with its publisher), Decision 8 (who owns which data), the context
  table (owners), the Platform lane row; `ARCHITECTURE.md` §4 line 109 ("`Models` — the shared kernel").
- Predecessor ratchet: [stories 6367/6369](../stories-6367-6369-kernel-and-repository-ratchets/feature-delta.md)
  (S04a: `Models ↛ Services`, `KernelRules`, `InlineBaseline`, IL dependencies via Mono.Cecil).
- Precedents copied, same shape: [S03h](../story-6366-s03h-package-reports/feature-delta.md) (closest format),
  [S03i](../story-6468-s03i-package-organisation-set-up/feature-delta.md) (open question 8: `ManualRankComparer` left
  for this Story), S03g's shared `NamespaceMoveLaw` (in delivery in worktree `wise-sleeping-firefly`, commit
  `1e25f5918`).

DESIGN and DEVOPS run 2026-11-24, **AFK, PROPOSE mode**: every engineering choice takes the recommended option and is
recorded as such; the open questions at the end are confirmations with the default taken, not blockers. Nothing a
user sees changes. Nothing was built or run with `dotnet` or `pnpm`; every count is a **text measurement on `main` at
`952040471`** (`git ls-files`, a comment-stripping word-boundary scan written for this design, kept in the session
scratchpad, not committed), confidence `noisy` (same short name in two namespaces is conflated; "on the wire" is
approximated by "named in `API/DTO`"; nested types are counted with their parent). **Every count below is re-measured
from the IL in S1**; a difference is recorded, not argued with. Counts the text scan could not settle are marked
"to re-measure at DoR re-run".

**Start condition.** All predecessors are delivered and merged into `main` before S1 starts: S03g (#6365, in delivery
now), S03h (#6366), S03i (#6468), the write-back round (#6657) and S09a context contracts (#6382). S09b (#6383) comes
after this Story. At `952040471` only S03g is in delivery; S03h and S03i are designed; #6657 and #6382 are being
designed and have no workspace on `main` yet (`docs/feature/story-6657-write-back-round/`,
`docs/feature/story-6382-s09a-context-contracts/`: absent). S09a in particular may already move event types and
two-context types into `Modules/<Context>/Contracts`, and #6657 decides where the write-back round (and with it
`Models/WriteBack`) lives. **Both decide rows of this Story's per-type list; S1 re-measures them.**

**What "merged" means, checked by S1 for each of #6365, #6366, #6468, #6657, #6382; all three checks must hold:**
1. ADO: the Story's state is `Closed` (`az boards work-item show --id <id> --query 'fields."System.State"'`).
2. Git: `git log main -1 --format="%h %s" --grep="story <id>"` returns that Story's final commit; S1 records the hash
   and runs `git merge-base --is-ancestor <hash> HEAD` (exit 0). No hit is "not merged".
3. Content (the property): `git ls-files` shows `Lighthouse.Backend/Lighthouse.Backend/Modules/{Refinement, Reports,
   OrganisationSetup}/`; no `Services/{Implementation,Interfaces}/{Refinement,Reports}/`; for #6382 at least one
   `Modules/*/Contracts/` folder and its contracts-only rule test in `Tests/Architecture`; for #6657 no loose
   `Services/Implementation/WriteBack*.cs` left outside the place its design names.
A failed check stops S1 and is reported with the Story, the check and its output; S1 never starts on a partial merge.

## Wave: DESIGN / [REF] Inherited
- ADR-270 Decision 7: the kernel holds shared ids, truly shared value objects and the domain-event base type. Entities,
  DTOs, settings and event types live with their owner. "Today's `Models` folder is far wider and shrinks one packaged
  context at a time."
- ADR-270 Decision 3: each event type moves to the context that publishes it; the kernel keeps only the base type.
- S03a D-3 (repeated by S03b-S03i): a `Models` type moves only if it is not an EF entity, not on the wire, not reached
  by a kernel type and not used outside its context. Every S03 Story applied it and handed the rest to S04b.
- S02 E1 / catalogue line 441: entity CLR names are baked into the EF model snapshots of both migration assemblies;
  entities move with S12c/S13, not here (brief update). Measured today: **44** `Lighthouse.Backend.Models.*` entity
  names, identical in `Lighthouse.Migrations.Postgres` and `Lighthouse.Migrations.Sqlite` snapshots.
- Catalogue line 183 and line 70: context-owned DTOs move into the context's contract **after S12c** made them pure
  mapping; S09a creates the `Contracts` namespaces. Contracts namespaces are OUT for S04b (brief).
- S04a: `Models ↛ Services|Modules|Platform` (`KernelRules.ModelsDoNotDependOnServices`, empty list). Consequence: a
  type reached by any type that stays in `Models` cannot leave `Models`.
- D-6264-16: Platform is composition-root infrastructure (`Lighthouse.Backend.Platform.<Area>`), not a context; Platform
  owned types may live there.
- Expand-only migrations; D-0 style: no schema change, no behaviour change.

## Wave: DESIGN / [REF] DoR re-run (2026-11-24)
| DoR item | Status | Evidence |
|---|---|---|
| 1 Problem statement | PASS | `Models` holds 260 files / ~360 top-level types; ADR-270 D7 allows three kinds. No rule says which `Models` type is kernel and which is a context's, so a new context type lands in `Models` with nothing failing (`SharedKernel_*` rules only forbid upward edges). |
| 2 Users / value | PASS | Maintainers: every `Models` type carries its owner and the reason it still sits there; a new context type in `Models` fails and names itself. No user-visible change. |
| 3 Hypothesis | **Disproved as written, restated** | Brief: "After S03*, the residue is small and clearly shared. Disproved if many types are shared by exactly two contexts." Measured: the residue is **large and mostly blocked**, not shared: 44 entities, ~86 wire types, 36 held by a staying `Models` type, 15 event types; only **24** types are shared by exactly two contexts (E3), and each sits on an edge ADR-270 already draws (Tracker integration → Work Items translation, the delivery-source port, write-back). That is published language for S09a's contracts, not a missing context, so it does not go back to S01. Restated: "S04b classifies every `Models` type (kernel, or owner + blocker), moves the free ones, and a rule keeps the list true and shrinking". |
| 4 Size | PASS | **M, about 4 days**, one nwdes feature, 5 slices (one conditional, S4). |
| 5 AC from UAT | PASS, AC 1 made precise | AC 1 "Every type in `Models` is on the ADR's allowed list" cannot hold while entities and wire types wait for S12c/S13/S09a. Restated (D-2): every type in `Models` is on the catalogue, either as `Kernel` (ADR-270 D7) or as owned by a named context with a blocker that is **measured true**. AC 2 holds as written. AC 3 (`ARCHITECTURE.md` §4) in S5. Behaviour-free: D-0. Open question 1. |
| 6 Dependencies | PASS, extended | Hard: all S03* (incl. S03i), S04a. **Added**: #6657 (owner of `Models/WriteBack`) and #6382 (S09a: `Contracts`, events) merged first (task order). Soft: S12c, S13, S05*, S10 (they free blocked rows later). |
| 7 Technical notes | PASS | E5: two non-test consumers read `Models` paths: `Scripts/test-selection/path-classifier.sh` (`SHARED_REGEX`, `JIRA_REGEX`) and the **frontend** test `Lighthouse.Frontend/src/services/UsageData/usageDataRouteKeys.test.ts` (reads `Models/UsageData/UsageDataRouteKey.cs` and `UsageDataRoutePatterns.cs` by relative path). No `GlobalSuppressions.cs` target, `.editorconfig`, Stryker config or `appsettings*.json` names a `Models` type. No serializer keys on type names (no `JsonDerivedType`, `Type.GetType`, `TypeNameHandling`); Swashbuckle uses short schema ids (no `CustomSchemaIds`), so a namespace move changes no API document. |

## Wave: DESIGN / [REF] DoR re-run at DELIVER start (what S1 re-measures once the predecessors merge)
| # | Re-measure | Why it can change | Changes |
|---|---|---|---|
| R1 | Every `Models` type (IL, top-level and nested), file, namespace | S03g/h/i moved no `Models` type by design, but S09a may move events and DTOs into `Contracts`; #6657 may move `Models/WriteBack` | catalogue rows |
| R2 | The 44 entity names in both snapshots (`modelBuilder.Entity("Lighthouse.Backend.Models…")`) and `LighthouseAppContext`'s runtime model | a predecessor that moved an entity would have changed snapshots (none planned) | `Entity` rows |
| R3 | Wire reachability from the IL: types reachable from a controller action's parameters/return (incl. `ActionResult<T>`, `IEnumerable<T>`, generic arguments) or from an `API.DTO` type's public properties, transitively | S09a/S12c may move or reshape DTOs | `Wire` rows; the movers |
| R4 | Held-by closure: a type referenced by any type that stays in `Models` | depends on R1-R3 | `HeldBy` rows; the movers |
| R5 | Users per context from the IL, contexts = `Modules.<X>`; non-contexts = `API`, `Data`, `Platform.*`, repositories, composition root, loose `Services` | S03g/h/i turn `Services.*.Refinement/Reports` and the loose Organisation set-up files into `Modules.*` | `Shared`, `Kernel` rows; destination module |
| R6 | Where S09a put `Contracts` and whether event types already moved | #6382 in design now | S4 runs or is dropped (D-6) |
| R7 | Where #6657 put the write-back round | #6657 in design now | owner of the 9 `Models/WriteBack` types |
| R8 | Rule-file literals naming `Models` types/namespaces in `Tests/Architecture` (text today: 40 files, e.g. `AccessApplicationLayoutArchUnitTest` 35, `ApplicationCoreRulesReachModulesArchUnitTest` 13, `PlatformLaneMembership` 11, `ModuleBoundariesArchUnitTest.ParentPlacementKernelTypes`, `ReportsModuleArchUnitTest.ReportModelsPattern`) | predecessors add rule files | the S1 rename list |
| R9 | `NamespaceMoveLaw` exists on `main` and its signature (S03g) | S03g in delivery | EXTEND vs lift (D-5) |
| R10 | E5 non-test consumers (classifier, frontend path test, suppressions, Stryker, log overrides) | any predecessor | the "stays because pinned" rows |
| R11 | S12c (#6388) and S13 (#6395) ADO state | if either landed, rows become free | `Wire` / `Entity` rows |

## Wave: DESIGN / [REF] Baseline (`main` at `952040471`, text search, `noisy`)
260 files under `Models` (KPI 3 was 258 after S03e). ~360 top-level types. Classification by the design rule (D-2),
first matching standing wins:

| Standing (text) | Types | Meaning | Moves in S04b? |
|---|---|---|---|
| Kernel base | 3 | `IDomainEvent`, `IEntity`, `IConcurrencyTokenEntity` | stays, `Kernel` |
| Entity | 44 | in both EF snapshots | no (S12c/S13) |
| Event | 15 | `Models/Events/*` except `IDomainEvent` | only into an existing publisher `Contracts` (D-6, S4) |
| Wire (named in `API/DTO`) | ~86 | plus more found by R3 among the "single" rows | no (S09a/S12c) |
| Held by a staying `Models` type | 36 | e.g. `ThroughputSettings` (by `Team`), `SimulationResult` (by `WhenForecast`), `Parent*Placement` (by `ParentResolution`) | no; moves with its holder |
| Reached by Data / repositories / composition root | 9 | e.g. `AppSettingKeys`, `ConnectionHealthState`, `ReportOwner`, `ForecastSimulationLimits` | no (S05*/S10); `ForecastSimulationLimits` see E7 |
| Shared by 2 contexts | 24 | E3 | no (S09a contracts) |
| Shared by 3 contexts | 8 | `WorkItemRules/*` (4), `RefreshSettings`, `ThroughputFilterMode`, `SyncOutcome`, `WriteBackFieldUpdate` | no (owner's contract, S09a) |
| Single context, otherwise free | 106 | candidates | yes **if** R3 finds them off the wire and R4 free |
| Platform lane / API only | 29 | `UsageData*`, `Encryption/SecretReadResult`, `Logging/*`, `SystemInfo`, `DistributionInfo`, `LighthouseRelease*`, request records | yes **if** free (Platform lane) |

File level (a file moves whole; D-3): **67** files hold only "single" or "platform" types. Most of them are controller
bodies (Metrics `*Dto`, `Rbac*Summary`, `*Request`, `RealityCheckResultDto`), which R3 will mark `Wire`. **Expected
movers: about 20 files** (to re-measure at DoR re-run), the ones with no controller signature edge:

| Expected mover (file) | Owner → destination |
|---|---|
| `Auth/EmbedSessionTokenRedemption.cs`, `Authorization/RbacGuardRequirement.cs`, `Authorization/RbacOperationResult.cs` (2 types) | Access → `Modules/Access/{Domain,Application}` |
| `DeliverySources/{DeliverySourceBindability, DeliverySourceDescriptor, DeliverySourceOption}.cs`, `OAuth/OAuthCompleteResult.cs` | Tracker integration → `Modules/TrackerIntegration/…` |
| `Forecast/RealityCheckVerdictTypes.cs` (6 enums) **if** R3 finds `RealityCheckResultDto` does not expose them | Forecasting |
| `ManualRankComparer.cs`, `OwnersWhoseFetchMoved.cs`, `AdditionalFieldInUse.cs` (4 types, if off the wire) | Organisation set-up |
| `Reports/ReportOwnerGoneException.cs`, `Reports/ReportRefusalCodes.cs` (if R3 keeps it off the wire: string constants read by `ReportsControllerBase`) | Reports |
| `Encryption/SecretReadResult.cs` | Platform → `Platform/Encryption` |
| `UsageData/{UsageDataEmitPermit, UsageDataSuppressionReason}.cs` | Platform → `Platform/UsageData` |
| `Logging/RefreshSubject.cs` (if R4 free) | Platform → `Platform/Logging` |
| `WorkItemRules/RuleSetSemantics.cs`, `Metrics/SpecialCauseType.cs` (if not held) | Work Items / Flow metrics |
| `BlackoutDaysExtensions.cs` | **stays**: held by `Delivery`, `Feature` (S04a moved it in for that reason; the text scan misses extension-method use) |
| `UsageData/UsageDataRoutePatterns.cs`, `UsageDataRouteKey.cs` | **stay**: the frontend test reads them by path (E5, D-3) |

### Per-type list for the Work Items types (the brief's update after S03b)
| Type(s) | Standing | Owner | Blocker (measured in S1) | Moves with |
|---|---|---|---|---|
| `WorkItem`, `Feature`, `FeatureWork`, `WorkItemStateTransition`, `WorkItemBlockedTransition`, `FeatureStateTransition`, `FeatureBlockedTransition`, `RefreshLog`, `FeatureDependencyReference` | Owned | Work Items (ADR-270 D8; `Feature.Forecasts`/`StartForecasts` are Forecasting's, S13) | Entity | S12c/S13 |
| `WorkItemBase` | Owned | Work Items | HeldBy `WorkItem`, `Feature`; Wire | with the entities |
| `WorkItemRules/*`: `WorkItemRuleSet`, `WorkItemRuleSetJson`, `WorkItemRuleCondition`, `WorkItemRuleSchema`, `WorkItemRuleFieldDefinition`, `RuleOperators`, `AdditionalFieldRuleHealing` | Owned | Work Items (the item-selection rule language) | Shared (Forecasting, Promises, Work Items, Refinement); Wire; HeldBy `Models.Refinement` | Work Items `Contracts` (S09a) or S12c |
| `RuleSetSemantics` | Owned | Work Items | none expected (no user found) | **S04b** (S3), if R4 free |
| `ParentPlacement`, `ParentPlacementOutcome`, `ParentPlacementRule`, `ParentNotPlacedReason` | Owned | Work Items | HeldBy `ParentResolution` | with `ParentResolution` |
| `ParentResolution`, `ParentCandidate`, `ParentChoice`, `ParentNarrowing`, `ParentReadings`, `StoredFeatures` | Owned | Work Items (`ParentReadings`: Tracker integration) | Shared (Tracker integration ↔ Work Items); `StoredFeatures` reached by repositories | S09a contracts |
| `ParentReading`, `ParentReadingAt`, `ParentReadingKind` | Owned | Tracker integration | same file as shared `ParentReadings` (D-3) | with `ParentReadings` |
| `ParentSources`, `ParentSourceEntry`, `ParentSourceKind`, `ParentSourcesOutcome`, `ParentSourcesViolation` | Owned | Tracker integration (the parent source selector's vocabulary) | Wire; HeldBy `WorkTrackingSystemOptionsOwner` | S12c / S09a |
| `SyncMode`, `RefreshType` | Owned | Work Items | Wire | S12c |
| `SyncOutcome` | Owned | Work Items | Shared (Forecasting, update code) | S09a |
| `RemoteRecordStamp`, `RefreshFinding`, `RefreshFindingKind`, `RefreshFindingSetting` | Owned | Tracker integration (stamp), Work Items (findings) | Shared; `RefreshFinding` Wire | S09a / S12c |
| `Staleness/*`: `StalenessPolicy`, `StalenessCandidate`, `StaleReason`, `StaleReasonKind`, `StalenessVerdict` | Owned | Work Items | Wire; `StalenessVerdict` HeldBy | S12c |
| `Dependencies/*`: `DependencyVerdict`, `HonouredDependencies`, `NotHonouredReason`, `ForecastWaits`; `DependencySource` | Owned | Work Items; `DependencySource` Tracker integration | Wire / HeldBy / Shared (Forecasting) | S12c / S09a |
| `IWorkItemQueryOwner` | Owned | Work Items | Shared; HeldBy entities | S09a |
| Events `WorkItemTransitioned`, `WorkItemBlocked`, `WorkItemUnblocked`, `WorkItemBecameStale`, `FeatureBlocked`, `FeatureUnblocked`, `TeamDataRefreshed`, `PortfolioFeaturesRefreshed` | Owned | Work Items (publisher) | Event | S4 if Work Items has `Contracts` (D-6) |

R-W6 (Work Items' 6-entry list): `IAppSettingService` and `ILighthouseClock` leave it at S03i/S03f, not here; S1
records the count it finds.

### Owners of the other groups (design decision; S1 writes one catalogue row per type)
| Group | Owner |
|---|---|
| `Team`, `Portfolio`, `WorkTrackingSystemOptionsOwner`, `IWorkTrackingSystemOptionsOwner`, settings (`AppSetting`, `AppSettings/*`, `SettingSource`, `EffectiveSetting`, `IncomingSetting`, `StoredSetting`), `TerminologyEntry`, `OwnerTerms`, `BlackoutPeriod`, `RecurringBlackoutRule`, `*Dto` of both, `BlackoutDaysExtensions`, `InstanceCalendar`, `StateCategories`, `StateCategoryLists`, `StateMapping`, `CycleTimeDefinition`, `EstimationBundle`, `ConnectionFieldDefaults*`, `ConnectionDefaultSetting`, `AdditionalField*`, `FeatureOrderingPolicy`, `FeatureOrderKey`, `FeatureComparer`, `ManualRankComparer`, `ThroughputSettings`, `PortfolioSize`, `OptionalFeatures/*`, `LegacyListShape`, `ListEntryKind`, `OwnersWhoseFetchMoved`; events `TeamDeleted`, `PortfolioDeleted`, `BlackoutConfigurationChanged`, `FeatureOrderingPolicyChanged`, `FeatureRankChanged` | Organisation set-up |
| `WorkTrackingSystemConnection`, `…Option`, `WorkTrackingSystems`, `ConnectionHealth/*`, `OAuth/*`, `Validation/*`, `DeliverySources/*`, `DependencySource`, `ParentSource*`, `ParentReading*`, `RemoteRecordStamp`, `AdditionalFieldDefinition`; event `ConnectionFetchShapeChanged` | Tracker integration |
| `Metrics/*`, `BlockedCountSnapshot`, `PercentilesOverTimeSnapshot`, `ProcessBehaviorSnapshot`, `ProcessBehaviorMetricType`, `MetricType`, `OwnerType`, `ForecastInputCandidatesDto`, `FeatureCandidateDto`, `ForecastPredictabilityScore` | Flow metrics |
| `Forecast/*` (except below), `SimulationResult`, `UpdateType`'s forecast use; event `PortfolioForecastsUpdated` | Forecasting |
| `Delivery*`, `DeliveryCompletionForecast`, `DeliveryForecastRow`, `FeatureForecastWindow`, `DeliverySelectionMode`, `DeliveryWhenPercentile` | Promises |
| `Refinement/*` | Refinement |
| `Reports/*` | Reports |
| `Auth/*`, `Authorization/*`, `FeatureMoveVerdict` | Access (`AuthMode` shared with Refinement) |
| `LicenseInformation` | Licensing |
| `WriteBack/*` | per #6657 (R7); default Tracker integration for the port vocabulary (`WriteBackResult`, `WriteBackItemResult`, `NotificationSuppression`), the round's owner for the rest |
| `UsageData/*`, `Encryption/*`, `Logging/*`, `Distribution/*`, `DemoData/*`, `SystemInfo`, `LighthouseRelease*`, `UpdateType` | Platform lane |

## Wave: DESIGN / [REF] Evidence
| # | Finding | Consequence |
|---|---|---|
| E1 | 44 entity CLR names in both model snapshots, identical lists; 4 discriminator lines (the forecast hierarchy). | No entity moves (D-0, D-2). Moving one rewrites both snapshots. |
| E2 | `KernelRules.ModelsDoNotDependOnServices` forbids `Models → Services|Modules|Platform` (empty list). | Anything a staying `Models` type reaches stays (held-by closure, R4). This, not ownership, is why most context types cannot leave yet. |
| E3 | 24 types shared by exactly two contexts: Tracker integration ↔ Work Items 11 (`Parent*`, `RefreshFinding*`, `RemoteRecordStamp`, `IWorkItemQueryOwner`), Promises ↔ Tracker integration 4 (`DeliverySources/*`), write-back ↔ Tracker integration/Forecasting 4, others 5 (`AuthMode`, `FeatureComparer`, `FeatureMoveVerdict`, `FeatureOrderKey`, `UpdateType`). Each lies on an edge in ADR-270's map. | Published language for S09a's contracts, not a new context; no return to S01 (DoR 3). |
| E4 | `Tests/Architecture`: 40 files name `Models` types or namespaces by literal; `ReportModelsPattern` treats `Models.Reports` as part of Reports; `ParentPlacementKernelTypes` lists four `Models` full names. | S1 sweep lists each literal for the movers; staying rows need no rename. |
| E5 | Non-test consumers of `Models` paths: `path-classifier.sh` `SHARED_REGEX` (`Models/WorkTrackingSystemConnection*`, `Models/WorkTrackingSystems.cs`, `Models/OAuth/`) and `JIRA_REGEX` (`Models/ParentSource*`, `ParentReadings`, `ParentChoice`, `ParentResolution`); `test-path-classifier.sh` lines 66, 70, 130-134, 148-149; the frontend test `usageDataRouteKeys.test.ts` lines 21, 29. | Of these only `Models/OAuth/OAuthCompleteResult.cs` is an expected mover; its new place `Modules/TrackerIntegration/(Application|Domain)/` is already in `SHARED_REGEX`, so the trigger is kept (S3 checks with `test-selection-show`). The two UsageData files stay (D-3). |
| E6 | Swashbuckle with default (short-name) schema ids; enums serialise by name; System.Text.Json stores no type names; no type-name reflection. | A namespace move changes no JSON and no API document; wire types still stay (D-2) because their move belongs with S12c/S09a. |
| E7 | `ForecastSimulationLimits` and `ForecastHorizon` were handed here by S03c. Text: `ForecastSimulationLimits` is read by the composition root (start-up banner, platform) → listed edge; `ForecastHorizon` is on the wire. | Both stay with a measured blocker; S03c's note is answered. |
| E8 | `ReportOwnerGoneException` (S03h OQ 8) and `ManualRankComparer` (S03i OQ 8) were handed here. Both expected free. | Expected movers (S3). |
| E9 | Many `Models` files hold several types (`InfoWidgetDtos.cs` 12, `RealityCheckResultDto.cs` 9, `ReportReadModels.cs` 15). | A file moves whole or not at all (D-3); no file split in this Story. |

## Wave: DESIGN / [REF] Decisions
All taken as recommended (AFK, 2026-11-24).

### D-0 No schema change, no behaviour change — RECOMMENDED
No entity, `DbSet`, migration, snapshot, route, DTO shape, payload, usage-data event or configuration key changes.
Checked by `ef-model-unchanged` and an empty diff under `Lighthouse.Migrations.*`, `Lighthouse.Frontend`,
`Lighthouse.EndToEndTests`.

### D-1 The kernel — RECOMMENDED: exactly ADR-270 D7, today three base types
`Kernel` rows: `IDomainEvent` (event base type), `IEntity`, `IConcurrencyTokenEntity` (entity markers, kernel since
S04a). Lighthouse has no shared id or value-object type today (ids are `int`); none is created here. Every other type
has an owner context (or the Platform lane).
| Option | Verdict |
|---|---|
| **a (recommended)** Kernel = D7 shapes only; every other type owned | Matches the ADR literally; the kernel rule then measures progress toward it. |
| b Call widely used types (`StateCategories`, `InstanceCalendar`, `WorkTrackingSystems`, `RunChartData`) kernel | Rejected: each has an owner in ADR-270's table (Organisation set-up, Tracker integration, Flow metrics); calling them kernel hides an upstream's published language. |

### D-2 The catalogue — RECOMMENDED: one row per `Models` type, standing `Kernel` or `Owned(owner, blocker)`, every blocker measured
Blockers, a closed set, each a property the test measures from the IL or the EF model (never a name):
`Entity` (in `LighthouseAppContext`'s model) · `Wire` (R3 reachability) · `HeldBy(type)` (a staying `Models` type
references it) · `Shared` (IL users in ≥ 2 contexts) · `InfraReached` (named by `Data`, a repository or the composition
root) · `PathPinned(consumer)` (a non-test consumer reads its file by path, E5) · `Event` (implements `IDomainEvent`,
waits for its publisher's `Contracts`). A row with no blocker is not allowed after S3: such a type must have moved.
Each row also names the Story expected to free it (S12c, S13, S09a, S05*, S10, Epic 6474). The brief's AC 1 is
restated accordingly (open question 1).

### D-3 Unit of move — RECOMMENDED: the file
A file moves only if all its top-level types are free and share one owner. No file split, no rename. Keeps every
commit a pure move (`refactor(kernel): move …`).

### D-4 Destination — RECOMMENDED: `Modules/<Owner>/Domain` iff its IL edges reach only the kernel, the BCL and that module's `Domain`; else `Application`; Platform-owned types to `Platform/<Area>`
Same rule as S03h D-1. No `Contracts` folder is created here (brief OUT; S09a). No new module folder is created: if an
owner has no module yet (write-back, R7), its rows stay `Owned` with the blocker it has.

### D-5 Rules first — RECOMMENDED: EXTEND S03g's `NamespaceMoveLaw` with a `Models` map
Movers' old namespaces are `Models.*` (role `Place` for `KernelRules.TheModels`, `ReportModelsPattern`), new ones a
module or `Platform.<Area>` (role `Context`). Law: every regex field keeps (Context) or drops (Place) each mover as
declared. Literal sweep with a rename list fixed in S1 from the pre-move grep, only shrinks. If S03g's law is still
Refinement-only, S1 lifts it first (one owner).

### D-6 Event types — RECOMMENDED: move only into a `Contracts` folder S09a already created; never create one
| Option | Verdict |
|---|---|
| **a (recommended)** S4 moves each event still in `Models/Events` into its publisher's existing `Modules/<Publisher>/Contracts`; S4 is dropped if S09a moved them already or made no `Contracts` | Follows ADR-270 D3 without taking S09a's scope. |
| b Keep all events in the kernel | Rejected: contradicts D3; leaves 15 non-kernel rows the rule must carry. |
| c Move events to the publisher's `Domain` | Rejected: subscribers in other contexts would then reach another context's `Domain`, which every module's R2/R3 forbids. |
Publishers: Work Items 8, Organisation set-up 5 (`TeamDeleted`, `PortfolioDeleted` are published from controllers
today, ADR-270 gap 5: owner still Organisation set-up), Tracker integration 1 (`ConnectionFetchShapeChanged`; published
by the connection save, which S03i places in Organisation set-up — open question 5), Forecasting 1.

### D-7 Wire types and entities — RECOMMENDED: stay, owned, blocker measured
Wire types wait for S12c/S09a (catalogue line 183), entities for S12c/S13 (brief). Their rows say so.

### D-8 The kernel-size rule — RECOMMENDED: `KernelCatalogueArchUnitTest` on `InlineBaseline` + `KernelRules`
See Ratchet design. Inline list, compared both ways, only shrinks; new `Models` types fail naming themselves.

### D-9 Tests move with production — RECOMMENDED
Unit tests of moved types (`Tests/Models/**` today, e.g. `ManualRankComparerTest`) move to `Tests/Modules/<Owner>/…`
(namespace = folder), kept there by each module's existing `…UnitTestPlacementTest`. Port, host and wire tests stay;
`using` lines change only.

### D-10 CI and pushes — RECOMMENDED: commit per slice, push S1-S5 together
No slice touches `Program.cs` unless S1 finds a mover named there (none expected); one push, one CI run.

### D-11 Behaviour-free proof and EXAMINE — RECOMMENDED: reuse probes
X1 `architecture-rules` (all) · X2 `ef-model-unchanged` · X3 `backend-tests` unchanged-assertion suites on both
revisions · X4 `test-selection-show` for the moved `OAuth` file. No new compare command.

### D-12 Mutation bar — RECOMMENDED
Last, on frozen code: Stryker.NET over the moved production files; no new survivor or no-coverage mutant against the
same files on the pre-move revision. Enums and constants yield no meaningful mutants (N/A, stated).

### D-13 Formal verification — RECOMMENDED: no Quint / TLA+ / Agda model; finite exhaustive checks
The claims are relations over a closed finite set (types × catalogue × measured properties), no state, time or
concurrency. The catalogue test and the law are exhaustive NUnit checks over the IL and the EF model, which is their
own executable projection in the default suite. PBT N/A (closed finite domain). No proof obligation, so no checker
install or waiver.

## Wave: DESIGN / [REF] Slices
One nwdes feature `story-6368-s04b-models-to-kernel`, 5 slices (S4 conditional), each ≤ 1 day, moves in commits apart
from edits. Size **M, about 4 days**. No split into several nwdes features.

| # | Delivers | Oracle (native NUnit, `Lighthouse.Backend.Tests/Architecture/`) | Production targets | Est. | Rigor |
|---|---|---|---|---|---|
| S1 | **First action: the five merge checks, then R1-R11.** The catalogue: one row per `Models` type (owner, blocker, freeing Story), measured from the IL and the EF model; the movers named with old and new namespace; the `Models` map for the move law; the rename list | `KernelCatalogue.cs` + `KernelCatalogueArchUnitTest.cs`: closed both ways (every `Models` type has a row, every row exists); every blocker claim **true** by measurement. **RED today** for the movers: their rows carry no blocker, which the test refuses ("free, move it to <destination>"); allowed until S3 by a shrink-only movers list. Law of D-5 RED on `TheModels`/`ReportModelsPattern` until widened | none | 1 d | `thorough` |
| S2 | Platform-owned free files in `Platform/<Area>` (expected `SecretReadResult`, `UsageDataEmitPermit`, `UsageDataSuppressionReason`, `RefreshSubject`) | catalogue rows tightened to the new namespace for these movers — RED before, GREEN after; lane rules (`PlatformLaneMembership`) green | ~4 files moved, `using`s | 0.5 d | `standard` |
| S3 | Context-owned free files in `Modules/<Owner>/{Domain,Application}` (expected ~16: Access, Tracker integration, Forecasting, Organisation set-up, Reports, Work Items, Flow metrics) and their unit tests | same, for these movers; every module's boundary and placement rules green | ~16 files moved, `using`s, E4 literals | 1 d | `standard` |
| S4 | *(conditional, D-6)* event types into existing publisher `Contracts` | catalogue `Event` rows: an event whose publisher has `Contracts` and still sits in `Models` fails; GREEN after the move; `DomainEventHandlerOrderPinTest` unchanged | ≤ 15 files moved, handler `using`s | 0.5 d | `standard` |
| S5 | Rules by namespace only and docs: movers list empty, `Place` patterns narrowed; `KernelRules` gains "every `Kernel` row is used by ≥ 2 contexts"; `ARCHITECTURE.md` §4 line 109 rewritten; ADR-270 D7 status line; catalogue KPI 3; brief AC 1 | **RED oracle**: the sweep with an **empty** rename/allowance list (excluding its own files and fixtures by exact path, the list asserted equal to those paths) — RED while S1's widened alternatives remain, GREEN once narrowed. Planted-type falsifiers (`KernelRulesCatchANewOffenderArchUnitTest` style): a made-up `Models` type not on the catalogue fails naming itself; a row claiming `Entity` for a non-entity fails | none (test side and docs) | 0.75 d | `standard` |

Rigor (CLAUDE.md profiles, proposed): S1 `thorough` (opus / sonnet): the measured blockers and the law are where
earlier reviews found misses; S2-S5 `standard` (sonnet / haiku). Open question 2.

Commands (host runs; nothing run in DESIGN):
```
pwsh -NoProfile -File Lighthouse.Backend/Test-Fast.ps1
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter "FullyQualifiedName~Architecture"
dotnet build Lighthouse.Backend                                            # 0 warnings
dotnet test Lighthouse.Backend --filter "TestCategory!=JiraIntegration&TestCategory!=LinearIntegration&TestCategory!=AdoIntegration&TestCategory!=ServiceNowIntegration&TestCategory!=GithubIntegration"
bash Scripts/test-selection/test-path-classifier.sh
```
Driving port for every oracle: the compiled production assembly read through `IlDependencies` and the EF model of
`LighthouseAppContext` (in-process; nearest argv the `~Architecture` line). Behaviour is observed through the unchanged
acceptance tests at the application-service port.

## Wave: DESIGN / [REF] Ratchet design
| # | Rule | Baseline |
|---|---|---|
| K1 | Every top-level type in `Lighthouse.Backend.Models(\.|$)` has exactly one catalogue row; every row names an existing `Models` type | catalogue (~360 rows, IL count in S1) |
| K2 | Each row's blocker is true now (Entity / Wire / HeldBy / Shared / InfraReached / PathPinned / Event); a false blocker fails with "the type is free of <blocker>: give it its true blocker or move it to <owner>" | all rows |
| K3 | No row is free after S5 (movers list empty) | S1: ~20 movers → 0 |
| K4 | `Kernel` rows: interfaces/base types used by ≥ 2 contexts (IL) | 3 |
| K5 | (kept) `Models ↛ Services|Modules|Platform`, `Models ↛ update queue` | empty |
Failure messages carry WHAT (type, claim), WHY (ADR-270 D7: the kernel holds only shared ids, value objects and the
event base type) and HOW (move it to `Modules/<Owner>`; or put its row with the measured blocker). Contexts are read
from `Modules.<X>` namespaces only; `API`, `Data`, `Platform.*`, repositories and the composition root are not
contexts.

## Wave: DESIGN / [REF] Component decomposition
| Type(s) | From | To | Change | Slice |
|---|---|---|---|---|
| Platform-owned free files (~4) | `Models/{Encryption,UsageData,Logging}` | `Platform/<Area>` | MOVE | S2 |
| Context-owned free files (~16) | `Models/**` | `Modules/<Owner>/{Domain,Application}` | MOVE | S3 |
| Event types (≤ 15) | `Models/Events` | `Modules/<Publisher>/Contracts` (if it exists) | MOVE (conditional) | S4 |
| Entities, wire, held, shared, infra-reached, path-pinned types | `Models` | — | UNCHANGED, rows with blocker | — |
| `KernelCatalogue`, its test, the `Models` move map | `Tests/Architecture` | same | CREATE_NEW / EXTEND | S1, S5 |

## Wave: DESIGN / [REF] Driving and driven ports
Driving: unchanged (all controllers, the update queue, the event dispatcher). Driven: unchanged. No new port, no new
external call. The only "port" this Story adds is test-side: the catalogue read by the kernel rule.

## Wave: DESIGN / [REF] Reuse analysis
| Existing | Overlap | Decision |
|---|---|---|
| `KernelRules`, `KernelRuleVerdict` | kernel rules defined once, run on real and planted types | **EXTEND** (K4, falsifiers) |
| `InlineBaseline` | both-ways list compare | **REUSE** for K1, K3 |
| `IlDependencies` | IL edges incl. async bodies | **REUSE** for HeldBy, Shared, Wire |
| `NamespaceMoveLaw` (S03g) | rules see a move | **EXTEND** with the `Models` map (D-5) |
| `*Membership` classes (Tracker integration, Promises, Access/Licensing, Platform lane) | namespace-agnostic membership | **REUSE** shape for `KernelCatalogue` |
| `ExpandOnlyMigrationGuard`, `ef-model-unchanged` | schema unchanged | **REUSE** |
| Each module's `…UnitTestPlacementTest` | tests mirror layout | **REUSE** |
No CREATE_NEW production type. Semantic-duplicate check: S09a's contracts-only rule decides cross-context references;
the catalogue decides only `Models` standing. They do not decide the same fact; S1 checks the S09a rule's actual scope
(R6) and records any overlap.

## Wave: DESIGN / [REF] Impact closure
| Locator | Kind | Decision | Reason |
|---|---|---|---|
| ~20 free `Models` files | production | MIGRATED | D-3, D-4 |
| Event files (conditional) | production | MIGRATED or UNCHANGED | D-6 |
| Consumers of movers (`using` lines; text: < 40 files) | production / tests | CHANGED (`using`s) | |
| Entities, wire and blocked types | production | UNCHANGED | D-0, D-7 |
| `Tests/Architecture` rule files naming movers (E4) | architecture tests | CHANGED | S1, S3, S5 |
| `path-classifier.sh`, `test-path-classifier.sh` | CI | UNCHANGED_COMPATIBLE (E5; checked in S3) | |
| `Lighthouse.Frontend/.../usageDataRouteKeys.test.ts` | frontend test | UNCHANGED (its two files stay) | D-3, E5 |
| Migrations, snapshots, `Data`, frontend, E2E, Lighthouse-Clients | — | UNCHANGED | D-0, E6 |
| `ARCHITECTURE.md` §4 line 109 + diagram node "Models — shared kernel + domain events"; ADR-270 D7 ("Today: three base types; the rest listed with owner and blocker by Story 6368"); catalogue KPI 3; slice-12 brief AC 1 and hypothesis | docs | CHANGED in S5 | |
| `GlobalSuppressions.cs`, `.editorconfig`, Stryker config, `appsettings*.json` | build / runtime | UNCHANGED_COMPATIBLE (none names a mover; re-grep R10) | |

Agreement analysis: no shared wire, schema or config contract changes (D-0, E6): `not-applicable`. Contract tests:
`not-applicable` (single deployable, same commit). Architectural tests: applicable (K1-K5, the law). Prefactoring: S1
is the prefactoring (rules see the move; production untouched; suite GREEN before and after S1).

## Wave: DESIGN / [REF] Constructive chain and residuality
- Chain: compiled assembly + EF model (input) → `KernelCatalogue` measured by `IlDependencies` (producer) → K1-K5
  (consumer) → `dotnet test … ~Architecture` exit and message (public observation). Falsifier: a planted
  `Lighthouse.Backend.Models.Fixtures.AContextOnlyType` used by one module fails K1 naming itself; a row claiming
  `Wire` for a type no controller reaches fails K2. DESIGN `PASS`, runtime `DESIGNED_NOT_BUILT`, proof N/A (D-13).
- Stressors (negated assumptions): (1) "`API/DTO` mention = on the wire" — false (controllers return `Models` types
  directly) → R3 measures reachability; (2) "a single-context type is free" — false when a staying type holds it
  (`BlackoutDaysExtensions` via extension calls the text scan misses) → IL closure; (3) "only tests read `Models`
  paths" — false (frontend test, CI classifier) → `PathPinned`; (4) "predecessors left `Models` as measured" — false by
  construction (S09a, #6657) → R1-R11; (5) "a blocker stays true" — false once S12c/S13 land → K2 fails and names the
  freed type, which is the intended pressure.
  Walked afterwards with a second set (a moved enum persisted as int: no entity moves, values unchanged; Swagger schema
  clash: short names unchanged; an event handler order shift on S4: registrations untouched, order pin unchanged): all
  survive. Walkthrough, not execution.
- Residual: K2 makes a later Story (S12c, S13) update or move rows it frees; that friction is accepted and stated in
  the failure message.

## Wave: DESIGN / [REF] Context-map placement
- Contexts touched: none in behaviour. The kernel shrinks by ~20 files (to re-measure); every remaining `Models` type
  gets an owner in ADR-270's terms.
- New terms / concepts: none for users. "Kernel catalogue" is a test-side term only; not added to the glossary.
- Domain events: none published or handled newly; event types may move to their publishers' `Contracts` (S4).
- UI surface: none.
- ADR-270 / `ARCHITECTURE.md` §4 updates: S5 tasks in Impact closure.

## Wave: DESIGN / [REF] Checklist (ticked through DELIVER)
- [ ] The five predecessors merged by the three checks; R1-R11 re-measured in S1 and recorded here.
- [ ] Behaviour-free: no entity, snapshot, migration, DTO shape, route or usage-data change; frontend and E2E untouched.
- [ ] Moves in `refactor(kernel): move …` commits apart from edits; files move whole.
- [ ] Every catalogue blocker measured, none designated.
- [ ] Classifier and the frontend path test unaffected (`test-selection-show`, `test-path-classifier.sh`).
- [ ] Architecture docs updated in S5; finalize checklist run before the push.

## Wave: DEVOPS
- **Platform lens**: none material. No deployment, configuration, persistence, pipeline or runtime change. No
  `Program.cs` edit expected (D-10). Log categories of moved types: none of the expected movers takes an `ILogger<T>`
  (re-grep R10).
- **Usage data**: **N/A, because** the Story changes nothing a user can use; no outcome exists to count.
- KPI: KPI 3 (`Models` files) 260 → about 240 at S5 (to re-measure); the catalogue's row count by blocker is the new
  progress measure toward ADR-270 D7.

## Open questions (default taken)
1. Restate AC 1 as "every `Models` type is `Kernel` (ADR-270 D7) or owned by a named context with a measured blocker
   and the Story that frees it", since entities and wire types wait for S12c/S13/S09a? **Default: yes; S5 amends the
   brief.**
2. Rigor: S1 `thorough` (opus / sonnet), S2-S5 `standard` (sonnet / haiku)? **Default: yes.**
3. Kernel = the three base types only (D-1 a), no widely used type promoted to kernel? **Default: yes.**
4. Events move only into a `Contracts` folder S09a created (D-6 a); S4 dropped otherwise? **Default: yes.**
5. `ConnectionFetchShapeChanged`: owner Tracker integration (ADR-270 D3 list) although S03i places its publisher (the
   connection save) in Organisation set-up? **Default: follow the publisher as measured in S1 (Organisation set-up) and
   amend ADR-270 D3's list in S5.**
6. `Models/WriteBack` owner: whatever #6657 decided (R7); without a home, rows stay owned by Tracker integration with
   their blocker? **Default: yes.**
7. Files with mixed standing (E9) are not split here (D-3)? **Default: no split; split with S12c.**
8. K2 fails a later Story that frees a row (for example S12c) until it updates or moves that row? **Default: yes — the
   failure names the freed type and the fix.**

## Architecture summary for the maintainer
**Shape (hexagonal).** Nothing new is built. About twenty small types leave the kernel for their owners; every type
that stays gets an owner and a measured reason.
```
 Driving (unchanged)          Modules/<Context> (some types MOVED in)       Platform/<Area> (a few MOVED in)
 controllers, update queue,   Access, TrackerIntegration, Forecasting,      Encryption, UsageData, Logging
 event dispatcher             OrganisationSetup, Reports, WorkItems,
                              FlowMetrics: Domain / Application
                              (+ Contracts from S09a: events, S4)
              \                         |                                   /
               \                        v                                  /
                Models — shared kernel (SHRINKS, rule-guarded)
                  Kernel: IDomainEvent, IEntity, IConcurrencyTokenEntity
                  Owned, waiting with a measured blocker:
                    44 entities ............ S12c / S13
                    wire types ............. S12c / S09a
                    held by an entity ...... with their entity
                    shared by 2-3 contexts . S09a contracts
                              |
 Driven (unchanged): LighthouseAppContext, repositories, trackers — schema and snapshots untouched
```
- Driving adapters: unchanged. Application services and ports: unchanged; some moved types land in a module's
  `Application`. Domain: pure enums and comparers land in a module's `Domain` where their IL edges allow. Driven
  adapters: unchanged; no entity moves, so no EF snapshot changes. No new external call.

**Patterns weighed.**
- Shared kernel kept tiny (chosen, ADR-270 D7) vs a "convenience kernel" of popular types (rejected: hides each
  upstream's published language).
- Ratchet list with **measured** reasons (chosen) vs a plain allow-list of names (rejected: a name cannot say whether the
  reason still holds) vs "no type used by one context" alone (rejected: misses held and wire types, and fails on
  entities that cannot move yet).
- Rules first, then move (chosen): the rules must see the move, or they check an empty set.
- Move whole files (chosen) vs split multi-type files (rejected: an edit, not a move; belongs with S12c).
- Domain events in the publisher's `Contracts` (chosen, if S09a made them) vs the publisher's `Domain` (rejected:
  subscribers would reach another context's Domain) vs staying in the kernel (rejected: contradicts ADR-270 D3).
- Moving entities now (rejected): both EF snapshots carry their CLR names; expand-only migrations and S13 own that.
- Dependency inversion / stable dependencies: the kernel stays the most stable layer and depends on nothing above it
  (`Models ↛ Services|Modules|Platform` kept).

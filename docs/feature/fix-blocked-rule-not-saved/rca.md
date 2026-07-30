# RCA — Bug #5613: blocked-item rule on a team is not saved and no error is shown

- **Reported**: 2026-07-29, ServiceNow slice-02 dogfood (Epic #5513), first-hand by the maintainer.
- **Method**: Toyota 5 Whys, multi-causal, evidence required at every level. Investigation depth 5.
- **Status of the defect**: **diagnosed by code trace, not yet reproduced on the instance.** Every
  link in the chain is pinned to `file:line`; the two facts that are inferred rather than observed
  are marked **[INFERRED]** with a 30-second confirmation step.
- **Verdict**: the *trigger* is ServiceNow-specific and belongs to Epic #5513. The *silence* is
  generic and pre-existing and should be split into its own item. Blocked rules are innocent.

---

## 1. Scope and problem statement

**Problem.** On the team settings page of a ServiceNow-backed team, a blocked-item rule created by a
config admin was not persisted, and the UI reported nothing — no error, no "saving", no "saved".

**In scope**: the team-settings save path (frontend autosave gate → PUT `/api/v1/teams/{id}` →
`Team.BlockedRuleSetJson` → GET `/settings` → frontend parse), and the connector-schema surface that
feeds the form's validity predicate.

**Out of scope / excluded as coincidence**: the other three dogfood findings (#5610 query authoring,
#5611 several tables as work item types, #5612 record click-through). #5611 is *correlated* — it is
the same missing "work item types for ServiceNow" decision seen from the product side — and is
called out below as a coupling, not as a cause.

**Answer to the reporter's first question — was a request sent at all?**
**No request was ever sent.** The defect is in the form, and specifically in a validity gate that
disables autosave for the entire page. See Branch A.

---

## 2. Root cause chain

### Branch A — why nothing was sent (the trigger; ServiceNow-specific)

```
WHY 1A: The rule change never left the browser — no PUT was issued.
  [Evidence] The team settings page has no Save button and never calls handleSave: the ONLY save
  trigger is the autosave effect. ModifyTeamSettings.tsx:84 passes
  `autoSave: { enabled: true, canSave: !disableSave, refreshOnSave: true }`; the component
  destructures the hook at ModifyTeamSettings.tsx:59-75 and does not take `handleSave`.
  The blocked-rule editor only mutates React state:
  FlowMetricsConfigurationComponent.tsx:272-290 `persistBlockedRuleSet` →
  `onSettingsChange("blockedRuleSetJson", …)` → useModifySettings.ts:352-360 `updateSettings`,
  which is a `setSettings` and nothing else.

  WHY 2A: The autosave effect early-returned because the form was considered invalid.
    [Evidence] useModifySettings.ts:265 — `if (!autoSaveEnabled || !autoSaveCanSave || !formValid) { return; }`
    — before the debounce timer at useModifySettings.ts:277 is ever armed. `formValid` is the
    single boolean produced by `validateForm` (useModifySettings.ts:130, :146-152).

    WHY 3A: `validateForm` for a team requires at least one Work Item Type unless the schema says
    otherwise — and this team has none.
      [Evidence] ModifyTeamSettings.tsx:95-96 —
      `(schema?.isWorkItemTypesRequired === false || s.workItemTypes.length > 0)`.
      `schema` is `settings.dataRetrievalSchema`, i.e. whatever the backend returned on GET; the
      hook never recomputes it on load (useModifySettings.ts:301-330 sets settings straight from
      `getSettings()`), and TeamService.ts:53-83 passes `dataRetrievalSchema` through untouched.

      WHY 4A: The backend tells the frontend that Work Item Types ARE required for a ServiceNow
      team, while the create wizard — driven by the frontend's own table — hid the field, so the
      team was created with an empty list.
        [Evidence, backend side] TeamSettingDto.cs:29 sets
        `DataRetrievalSchema = DataRetrievalSchemaDto.ForTeam(connection.WorkTrackingSystem)`.
        DataRetrievalSchemaDto.cs:21-69 switches over `WorkTrackingSystems` with arms for
        AzureDevOps / Jira / Linear / Csv **and no ServiceNow arm** → ServiceNow falls into the
        catch-all `_ =>` at DataRetrievalSchemaDto.cs:61-68, which returns
        `Key = "query", DisplayLabel = "Query", IsRequired = true, IsWorkItemTypesRequired = true`.
        [Evidence, frontend side] DataRetrievalSchemaDefaults.ts:46-55 declares ServiceNow with
        `isWorkItemTypesRequired: false`. CreateTeamWizard.tsx:74 renders the Work Item Types step
        only when `wizard.schema?.isWorkItemTypesRequired !== false`, and the wizard's schema comes
        from that frontend table (CreateTeamWizard.tsx:25). The wizard's own list starts empty
        (useCreateWizard.ts:64) and is passed straight into the create DTO
        (CreateTeamWizard.tsx:32), so a ServiceNow team is created with `workItemTypes: []`.
        The wizard's own can-proceed check agrees with the frontend table
        (useCreateWizard.ts:128), which is why creation succeeded and only *editing* is dead.

        WHY 5A: The per-connector data-retrieval schema is duplicated — a TypeScript
        `Record<WorkTrackingSystemType, …>` on the frontend and a C# `switch` with a `_` default on
        the backend — and Epic 5513 slice-01 extended only the frontend copy. The TS Record is
        exhaustive, so the compiler forced the frontend entry; the C# default arm swallowed the new
        enum member without a warning.
          [Evidence] `git log -- Lighthouse.Backend/.../DataRetrievalSchemaDto.cs` → last touched by
          `2f6503270` ("Rework Linear integration"). `git log -- .../DataRetrievalSchemaDefaults.ts`
          → `1b71a04ef` "feat(servicenow): ask for a ServiceNow query and decline portfolios (#5574)".
          The slice-01 roadmap step lists only the frontend file
          (deliver/roadmap.json:222) with test `DataRetrievalSchemaDefaults.serviceNow.test.ts`.
          The DESIGN's own definition of the work — feature-delta.md:27-31, "Adding a system is a
          known, bounded shape [verified]: `WorkTrackingSystems` enum + `AuthenticationMethodKeys`
          entry + `IWorkTrackingAuthStrategy` + connector class + **frontend**
          `DataRetrievalSchemaDefaults` entry + optional wizard" — does not mention the backend
          `DataRetrievalSchemaDto` at all.
          `DataRetrievalSchemaDtoTest.cs:10-13` and `:36-40` parametrise Linear/ADO/Jira/Csv and
          have no ServiceNow case, so the divergence is untested on both stacks.

→ **ROOT CAUSE A**: two unsynchronised copies of the per-connector data-retrieval schema, with the
backend copy missing ServiceNow and falling back to a "work item types required" default. Every
ServiceNow team is therefore permanently form-invalid on its settings page, which silently disables
autosave for the whole page.
```

**Corroborating evidence that the schema question was knowingly left half-answered.** The epic's own
open call C-3 (feature-delta.md:828-850) ends with: *"**Check before committing to it**: whether
`isWorkItemTypesRequired: false` merely skips validation or actually hides the field in the UI."*
That check was recorded as "ruled non-blocking; asserted in the FE schema test"
(feature-delta.md:942) and never performed. The answer is: **it does both, and the two stacks
disagree about it.**

**Corollary / falsifiable prediction (see §7).** If Branch A is right, *nothing at all* saves on
that team's settings page — the blocked rule is just what the maintainer happened to try. Toggling
the System WIP Limit will be lost the same way, and adding a single Work Item Type will make the
page start saving immediately (including the blocked rule), because `updateSettings` flips
`hasInteractedRef` and the next `validateForm` run returns true.

### Branch B — why it was silent (the amplifier; generic, pre-existing)

```
WHY 1B: The user saw no error, no spinner, no "saved".
  [Evidence] SaveStateIndicator.tsx:34 — `if (!canSave || saveState === "idle") { return null; }`.
  `saveState` only leaves "idle" inside `dispatchSave` (useModifySettings.ts:180 `setSaveState("saving")`),
  which the effect never reached. Rendering an error would have required a rejected request
  (useModifySettings.ts:213-219) that never happened.

  WHY 2B: `formValid` is never rendered anywhere.
    [Evidence] The hook returns it (useModifySettings.ts:409), but neither consumer destructures it:
    ModifyTeamSettings.tsx:59-75, ModifyProjectSettings.tsx:70-101. Nothing in either page reacts
    to "this form is not going to save".

    WHY 3B: The design deliberately made the gate silent, on the assumption that a field-level
    inline error is always present to explain it.
      [Evidence] useModifySettings.autosave.test.ts, test
      "@US-01 @error holds back an invalid edit so the inline error is the only feedback" — asserts
      `saveSettings` not called, `formValid === false`, `saveState === "idle"`. The assumption is in
      the test name.

      WHY 4B: That assumption does not hold for every predicate in `validateForm`. The Work Item
      Types editor has no required/empty error state at all.
        [Evidence] WorkItemTypesComponent.tsx:48-59 renders an `InputGroup` + `ItemListManager` with
        no validation prop and no error path; an empty list looks exactly like a list the user has
        not opened. Same for empty To Do / Doing / Done lists (ModifyTeamSettings.tsx:91-94 requires
        them; StatesList renders no required-state error).

        WHY 5B: Form validity is modelled as one opaque boolean over the entire page, so the system
        structurally cannot say *which* field blocked the save, and no component owns the message.
          [Evidence] The `validateForm` signature in useModifySettings.ts:38-42 returns `boolean`;
          both call sites are single boolean expressions (ModifyTeamSettings.tsx:85-101,
          ModifyProjectSettings.tsx:102-119).

→ **ROOT CAUSE B**: a whole-form boolean validity gate with no form-level "not saving, because…"
surface. Any invalid field — including ones with no inline error owner — silently disables autosave
for the entire settings page. This is the "validation silent no-op" item already on the Epic 5074
enhancement backlog; #5613 is its first field report.
```

### Cross-validation

| Pair | Consistent? |
|---|---|
| A + B | Yes, and complementary: A explains *why the form was invalid*, B explains *why the user could not tell*. Neither alone produces the report — A without B would have shown a message; B without A would need some other invalid field. |
| A + "the E2E walking skeleton is green" | Yes. `BlockedItems.spec.ts` drives the identical flow (enable → clear → add rule → save → assert the widget) on the CSV demo team "Team Zenith", whose FE and BE schemas agree (`isWorkItemTypesRequired: true` in both, and the demo team has work item types). The generic path is proven to work, which is what forces the cause to be ServiceNow-shaped. |
| All observed symptoms explained | Yes: (1) rule gone after reload — never sent; (2) no error — `saveState` stayed `idle` and the indicator renders `null`; (3) team itself exists and metrics render — the team was created by the wizard's own POST, which uses the frontend schema and validates fine. |

### Alternative hypotheses — explicitly ruled out

| Hypothesis (incl. all four raised by the reporter) | Verdict | Evidence |
|---|---|---|
| Request sent and rejected 4xx/5xx | **Ruled out** as *this* symptom | A rejection sets `saveState = "error"` (useModifySettings.ts:220) and SaveStateIndicator.tsx:74-86 renders "Couldn't save" + Retry. Not silent. |
| 200 but not persisted server-side | **Ruled out** | TeamController.cs:185 assigns `team.BlockedRuleSetJson = teamSetting.BlockedRuleSetJson` verbatim, then `teamRepository.Update` + `Save` (:187-195). No normalisation, no round-trip through a deserializer on the write path. `TeamExtensions.cs:89` does the same inside `SyncBlockedItems`. |
| Validation deserialises camelCase case-sensitively and nulls the ruleset | **Ruled out as the cause of the loss** — but the deserialisation defect is real, see CF-1 | `ValidateBlockedRuleSet` (TeamController.cs:269-292) only ever *returns a string*; it never writes to the entity. Its practical effect is the opposite of dropping: it makes validation a no-op that accepts anything. |
| Epic 5074 "Bug 3" rerun — persisted but invisible because GET serves PascalCase | **Ruled out** | GET `/settings` serves `blockedItemService.GetEffectiveRuleSetJson(team)` (TeamController.cs:209), which serialises with `PropertyNamingPolicy = JsonNamingPolicy.CamelCase` (BlockedItemService.cs:21-25, :72-75). That matches `blockedRuleSetSchema` (BaseSettings.ts:19-23) exactly, and read-back deserialisation is case-insensitive (BlockedItemService.cs:22, :57). The rule is **not** sitting invisible in the database — it is genuinely absent. |
| Rule attached to a never-saved parent team | **Ruled out** | The team was created through the wizard's own POST (`EditTeam.tsx:118-121` → `teamService.createTeam`) before the settings page was ever opened, and the dogfood report confirms work items, ages and states rendered — impossible without a persisted team. |
| Concurrency conflict (409) swallowed | **Ruled out** | 409 sets `saveState = "conflict"` (useModifySettings.ts:215-218) and renders a visible warning + Reload button (SaveStateIndicator.tsx:37-51). |
| RBAC hid or disabled the editor | **Ruled out** | The rule builder only renders for a config admin (`canEditBlockedRules`, FlowMetricsConfigurationComponent.tsx:81-83, :540) — the maintainer *saw and used* it, so the check passed. |
| Licence gate silently disabled autosave | **Not the cause here, but a second silent path — confirm** | `EditTeam.tsx:33` sets `disableSave = !canUpdateTeamData`; `useLicenseRestrictions.ts:81` makes that `teamCount <= 3` for a non-premium instance. If it bites, `autoSaveCanSave` is false (useModifySettings.ts:265) **and** `SaveStateIndicator` renders `null` (`!canSave`, SaveStateIndicator.tsx:34) — a byte-identical symptom. The dogfood instance is expected to be premium; confirm `licenseStatus.canUsePremiumFeatures` when reproducing. |
| Connector-specific gating of blocked rules | **Ruled out** | Nothing in the blocked-rule path branches on `WorkTrackingSystems`. `BlockedItemService.GetSchema` (BlockedItemService.cs:81-101) builds fields from the owner's fixed fields plus the connection's additional field definitions, with no connector switch. ServiceNow contributes no predefined additional fields (AdditionalFieldsEditor.tsx:161-167), which shrinks the field list but does not disable anything. |

---

## 3. Contributing factors

| # | Factor | Evidence |
|---|---|---|
| CF-1 | **Blocked-rule validation is a permanent no-op on both controllers.** The incoming rule JSON is camelCase; `WorkItemRuleSet` is PascalCase with no `[JsonPropertyName]`; both controllers deserialise with default (case-*sensitive*) options. `Conditions` therefore always comes back empty, hits the `Conditions.Count == 0` early return, and every field/operator/length/count check is skipped. Already logged in `docs/feature/epic-5074-blocked-items/deliver/review-slices-01-04.md`, still unfixed. It did not cause #5613, but it removes the one affordance that would have told the maintainer *something* about their rule. | TeamController.cs:279 and PortfolioController.cs:185 (`JsonSerializer.Deserialize<WorkItemRuleSet>(ruleSetJson)`, no options) vs WorkItemRuleSet.cs:15-19 (PascalCase, no attributes). Contrast the correct call: BlockedItemService.cs:57 passes `JsonSerializerOptions`. Note the same file gets it right for the *forecast filter* service (ForecastFilterRuleService.cs:16). |
| CF-2 | No backend test asserts a schema for ServiceNow, so the fallback arm is invisible. | DataRetrievalSchemaDtoTest.cs:10-13, :36-40. |
| CF-3 | No contract test ties the two schema tables together. The frontend has `DataRetrievalSchemaDefaults.serviceNow.test.ts`; the backend has `DataRetrievalSchemaDtoTest.cs`; nothing compares them. | Both files exist independently; no shared fixture anywhere in the repo. |
| CF-4 | The `_ =>` default arm on a switch over a closed enum converts "new connector forgotten" from a compile error into a silent wrong answer. The team knew this trap — the roadmap called it out for `workTrackingSystemGetDataRetrievalDisplayName` ("the ONE frontend touch point the compiler cannot enforce because the switch has a `default:` arm", deliver/roadmap.json:228) — and applied the lesson to that one switch only. | DataRetrievalSchemaDto.cs:61, :111. |
| CF-5 | The only end-to-end coverage of the blocked-rule save flow runs against a CSV demo team, whose schemas happen to agree across stacks. | BlockedItems.spec.ts:12-13, :22-44. |
| CF-6 | The Work Item Types decision for ServiceNow is genuinely unfinished (dogfood finding #5611 wants tables surfaced *as* work item types). The fix below must not pre-empt that; it only makes the backend agree with the frontend's current answer. | feature-delta.md:836-850. |

---

## 4. Proposed fix

### P0 — workaround for the maintainer, right now (no code)

Open the ServiceNow team's settings page and add any single Work Item Type. The form becomes valid,
autosave resumes, and the blocked rule (and everything else) starts saving. This is safe: the
ServiceNow connector never reads `WorkItemTypes` — there is no reference to it anywhere under
`Services/Implementation/WorkTrackingConnectors/ServiceNow/`.

### P1a — close the trigger (Root Cause A)

`Lighthouse.Backend/Lighthouse.Backend/API/DTO/DataRetrievalSchemaDto.cs` — add the two missing arms,
mirroring `DataRetrievalSchemaDefaults.ts:46-55` and `:93-99` value for value:

```csharp
// in ForTeam, before the `_` arm (line 61)
WorkTrackingSystems.ServiceNow => new DataRetrievalSchemaDto
{
    Key = "servicenow.query",
    DisplayLabel = "ServiceNow Query (Encoded Query)",
    InputKind = FreeTextInput,
    IsRequired = true,
    IsWorkItemTypesRequired = false,
    WizardHint = null,
},

// in ForPortfolio, before the `_` arm (line 111)
WorkTrackingSystems.ServiceNow => new DataRetrievalSchemaDto
{
    Key = "servicenow.query",
    DisplayLabel = "Not supported for ServiceNow",
    InputKind = "none",
    IsRequired = false,
    IsWorkItemTypesRequired = false,
    WizardHint = null,
},
```

### P1b — make the class of defect impossible (CF-2/CF-3/CF-4)

`DataRetrievalSchemaDtoTest.cs` — add a test that iterates `Enum.GetValues<WorkTrackingSystems>()`
and asserts that neither `ForTeam` nor `ForPortfolio` returns the fallback (`Key == "query"` /
`DisplayLabel == "Query"`) for any declared system. That turns "next connector forgets the backend
schema" into a red test on the day the enum member is added, which is exactly what the frontend's
exhaustive `Record` already does for the other stack. Add explicit ServiceNow `TestCase` rows too,
so the expected values are readable.

### P1c — close the silence (Root Cause B)

Minimum viable, in both `ModifyTeamSettings.tsx` and `ModifyProjectSettings.tsx`: destructure the
`formValid` the hook already returns (useModifySettings.ts:409) and render a persistent
`Alert severity="warning"` near the SaveStateIndicator — "Changes on this page are not being saved:
some required settings are incomplete." — whenever `!formValid`. This is ~6 lines per page and needs
no signature change.

Better, as the follow-up item: change `validateForm` to return the list of unmet requirement labels
instead of a boolean (useModifySettings.ts:38-42), have `formValid` derive from `length === 0`, and
name the offending fields in the alert. That removes the structural cause at WHY 5B rather than
papering over it, and it retires the deferred "validation silent no-op" backlog entry.

### P2 — fix the no-op validation (CF-1)

Give both controllers the case-insensitive options they need — the smallest correct change is to
reuse the same options object shape already proven at BlockedItemService.cs:21-25 (or add
`[JsonPropertyName]` to `WorkItemRuleSet`/`WorkItemRuleCondition`, which fixes every call site at
once). Ship as its own commit with its own test: this flips validation from "accepts anything" to
"actually enforces", so it is a behaviour change, not a cleanup.

### P3 — product decision, not a fix

Feed the ServiceNow work-item-types question into #5611. If the answer becomes "tables are the work
item types", `IsWorkItemTypesRequired` becomes conditional on the configured table in *both* tables —
which is precisely why P1b's exhaustiveness guard should land first.

---

## 5. Files affected

**By the fix**

| File | Change |
|---|---|
| `Lighthouse.Backend/Lighthouse.Backend/API/DTO/DataRetrievalSchemaDto.cs` | P1a — two arms (before :61 and before :111) |
| `Lighthouse.Backend/Lighthouse.Backend.Tests/API/DTO/DataRetrievalSchemaDtoTest.cs` | P1b — ServiceNow cases + enum-exhaustiveness guard |
| `Lighthouse.Frontend/src/components/Common/Team/ModifyTeamSettings.tsx` | P1c — surface `formValid` |
| `Lighthouse.Frontend/src/components/Common/ProjectSettings/ModifyProjectSettings.tsx` | P1c — same |
| `Lighthouse.Frontend/src/hooks/useModifySettings.ts` | P1c follow-up only, if `validateForm` returns reasons |
| `Lighthouse.Backend/Lighthouse.Backend/API/TeamController.cs:279`, `PortfolioController.cs:185` | P2 — case-insensitive deserialisation |

**Read during the investigation, unchanged** — `FlowMetricsConfigurationComponent.tsx`,
`DeliveryRuleBuilder.tsx`, `BaseSettings.ts`, `BlockedItemService.cs`, `TeamSettingDto.cs`,
`TeamExtensions.cs`, `CreateTeamWizard.tsx`, `useCreateWizard.ts`, `SaveStateIndicator.tsx`,
`WorkItemTypesComponent.tsx`, `TeamService.ts`, `useLicenseRestrictions.ts`, `BlockedItems.spec.ts`.

**No migration.** `BlockedRuleSetJson` is untouched; nothing was written wrongly, so there is no data
to repair. Any ServiceNow team that has been edited since creation simply has the pre-edit values.

---

## 6. Risk assessment

| Change | Risk | Reasoning / watch-outs |
|---|---|---|
| P1a ServiceNow team arm | **Low** | Purely additive data for one enum member; no other connector's behaviour can change. Visible effect: the Work Item Types editor disappears from ServiceNow team settings (ModifyTeamSettings.tsx:145) and stops being required, so those forms become valid and autosave starts working. Any ServiceNow team that already has types typed in keeps them — the value is preserved, just no longer required, and the connector never reads it. |
| P1a ServiceNow portfolio arm | **Low** | ServiceNow portfolios are declined at the frontend (`inputKind: "none"`), so the arm only makes an unreachable-by-design surface consistent. Check `PortfolioSettingDto.cs:36` consumers for anything keying off `DisplayLabel`. |
| P1b exhaustiveness guard | **Low**, one-time cost | It will go red immediately for any *other* enum member currently relying on the fallback. That is the point; verify no legitimate use of the `_` arm exists before merging. Do **not** replace `_` with a throw — a runtime `ArgumentOutOfRangeException` in a settings GET would be worse than a wrong label. |
| P1c surface `formValid` | **Medium-low** | It will start showing a warning on pages that have been quietly invalid all along — desirable, but expect it in screenshot/E2E baselines. Guard against firing on first paint before settings load (`settings === null`) and consider gating on `hasInteracted` so a freshly opened, never-touched page does not scold the user. Regenerate `@screenshot` PNGs with an `rm` first (known pixel-threshold trap). |
| P2 case-insensitive validation | **Medium** | Validation goes from "accepts everything" to "actually enforces". Rule sets already stored with an unknown `fieldKey`, an unsupported operator, an over-long value, or more than `MaxRules` conditions will start returning 400 on the next settings save — a user who has never had a rule rejected could suddenly be unable to save at all. Audit existing `BlockedRuleSetJson` and `ForecastFilterRuleSetJson` values (including demo seeds) before shipping, and ship it as its own commit so it can be reverted independently. |
| Doing nothing | **High** | Every ServiceNow team's settings page silently discards every edit, and the next connector added to Lighthouse inherits the same trap. |

---

## 7. Cheapest deterministic reproduction

**Confirm the two [INFERRED] facts first — 30 seconds, no code.** On the dogfood instance,
`GET /api/v1/teams/{id}/settings` for the ServiceNow team and check that
`workItemTypes` is `[]` and `dataRetrievalSchema.isWorkItemTypesRequired` is `true`. Then, in the UI,
toggle the System WIP Limit on that team's settings page and reload: **it will also be lost**, which
proves this is not a blocked-rules bug. Then add one Work Item Type and repeat: everything saves.
(Also confirm `canUsePremiumFeatures` is true, to eliminate the licence path from §2.)

**Then, by layer — cheapest first:**

| Layer | Test | Cost | What it pins |
|---|---|---|---|
| **1. Backend NUnit (unit)** — *start here* | `DataRetrievalSchemaDtoTest`: `ForTeam(WorkTrackingSystems.ServiceNow).IsWorkItemTypesRequired` is `false`, plus the enum-exhaustiveness guard. | ~2 min, no host, no DB | The proximate defect and the recurrence guard. This is the regression test that matters most. |
| **2. Frontend vitest (component)** — *this is the bug as reported* | Render `ModifyTeamSettings` with settings shaped like the real API response for a ServiceNow team (`workItemTypes: []`, `dataRetrievalSchema: { isRequired: true, isWorkItemTypesRequired: true }`, states populated), drive the blocked-rule editor, advance the 300 ms debounce, assert `saveTeamSettings` was **not** called and that a "not being saved" warning **is** visible. Cheaper variant if the provider setup is heavy: `renderHook(useModifySettings)` with `validateForm` copied from ModifyTeamSettings.tsx:85-101 and a ServiceNow-shaped `getSettings` — the existing `useModifySettings.autosave.test.ts` fixtures (`atlasSettings`, `makeArgs`) already give you everything but the settings shape. | ~15 min | The full user-visible chain: bad schema → invalid form → no request → no message. Covers Root Cause B as well. |
| 3. Backend NUnit integration (WebApplicationFactory) | Not needed for this bug — nothing is wrong on the server for #5613. **Do** use this layer for the P2 fix (CF-1), where a PUT with a camelCase rule set carrying an unknown `fieldKey` must return 400. | — | CF-1 only. |
| 4. Playwright E2E | **Not worth it.** A ServiceNow E2E needs a live instance and the epic already deferred its own ServiceNow walking skeleton for that reason. The existing `BlockedItems.spec.ts` on the CSV demo team already covers the happy path and would not have caught this. | — | — |

---

## 8. Verdict: ServiceNow-specific or generic?

**Both, and the split decides the filing.**

- **The trigger is ServiceNow-specific and was introduced by Epic #5513.** The missing
  `WorkTrackingSystems.ServiceNow` arm in `DataRetrievalSchemaDto` (DataRetrievalSchemaDto.cs:21-69)
  is a gap in slice-01's own work (commit `1b71a04ef` extended only the frontend twin). An ADO, Jira,
  Linear or CSV team does **not** fail this way: their backend and frontend schemas agree, their
  settings forms are valid, and `BlockedItems.spec.ts` proves the flow green on a CSV team.
  → **Bug #5613 stays under Epic #5513.**
- **The amplifier is generic and pre-existing.** The silent whole-form autosave gate
  (useModifySettings.ts:265 + SaveStateIndicator.tsx:34 + neither page rendering `formValid`) belongs
  to the autosave work under Epic 5074 and will keep producing "it saved nothing and said nothing"
  reports for every connector until it is fixed. It is already known — the deferred "validation
  silent no-op" backlog item — and #5613 is its first field report.
  → **File the silence as its own item** (Epic 5074 / settings-UX), cross-linked from #5613, and do
  not let it ride along on the ServiceNow fix.
- **Blocked rules are not implicated at all.** The rule editor, the serialiser, the column, and the
  read-back are all correct. The maintainer would have lost any other setting changed on that page in
  exactly the same way. The one real blocked-rules defect found on the way (CF-1, validation is a
  no-op) is separate, pre-existing, and still unfixed.

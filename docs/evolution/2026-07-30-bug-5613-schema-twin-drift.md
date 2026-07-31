# A Blocked Rule That Was Never About Blocked Rules — Evolution

**Feature:** fix-blocked-rule-not-saved | **ADO:** Bug #5613 (parent Epic #5513) | **Shipped:** 2026-07-30 | **Commits:** `cb5f0efb0..017988cdb`

## What shipped

The report was first-hand from the ServiceNow slice-02 dogfood: a blocked-item rule created on a team was gone afterwards, with no error. The blocked-rules code turned out to be innocent — the maintainer would have lost *any* setting changed on that page, in exactly the same way, and would have been told nothing either time.

Two independent defects, fixed separately because they fail for different reasons and belong to different owners.

| Step | Outcome |
|---|---|
| 01-01 | `DataRetrievalSchemaDto.ForTeam`/`ForPortfolio` gain their missing `ServiceNow` arms, plus a guard test that iterates `Enum.GetValues<WorkTrackingSystems>()` and fails if either factory ever returns the `query` fallback. |
| 01-02 | `validateForm` returns the *reasons* a settings form is blocked instead of one opaque boolean; both settings pages render them as a persistent warning. |

## Root cause

**The data-retrieval schema table is duplicated across the stacks, and only one copy is exhaustive.**

The frontend copy (`DataRetrievalSchemaDefaults.ts`) is a `Record<WorkTrackingSystemType, IDataRetrievalSchema>` — adding a member to the union is a compile error until every entry exists. The backend copy (`DataRetrievalSchemaDto.cs`) is a `switch` with a `_ =>` fallback, which compiles perfectly while silently answering `IsWorkItemTypesRequired = true`, `Key = "query"` for anything unmapped.

Epic 5513 slice-01 (`1b71a04ef`) extended only the frontend copy. `DataRetrievalSchemaDto.cs` was last touched by the Linear rework (`2f6503270`) and nobody noticed, because nothing could notice.

The two copies then feed different screens, which is what turned a stale table into an unsaveable team:

- The **create wizard** reads the frontend defaults (`getDefaultTeamSchema` → `CreateTeamWizard.tsx:74`), saw `isWorkItemTypesRequired: false`, and hid the Work Item Types step. The team was created with `workItemTypes: []`.
- The **settings page** reads `settings.dataRetrievalSchema` — the backend DTO — which said work item types *were* required. `ModifyTeamSettings.tsx:95-96` therefore judged the form invalid, permanently.

Three things then conspired to make it silent:

- **The settings pages have no Save button.** The autosave effect is the only save trigger, and it returns at `useModifySettings.ts:265` *before* arming its debounce timer. No request was ever sent — the answer to the reporter's own first question.
- **`SaveStateIndicator` renders `null` for `idle`**, and `saveState` never left `idle`, because no save was attempted. Correct behaviour; wrong outcome.
- **`useModifySettings` already returned `formValid`, and neither page destructured it.** The gate was *designed* silent — the existing test is literally named "holds back an invalid edit so **the inline error is the only feedback**" — but `WorkItemTypesComponent` has no required/empty error state, so there was no inline error to be the feedback.

Underneath the silence sits a shape worth naming: `validateForm` collapsed seven independent predicates into one boolean, so the system was *structurally incapable* of saying which one failed. That is why the fix is a signature change and not a banner.

## Key decisions & design anchors

- **The `_` fallback arms stay.** `WorkTrackingSystems` is a persisted database value, so an unmapped future member must still yield *something* rather than throw on read. Deleting the fallback would trade a silent wrong answer for a crash on legacy rows. The enum-iteration test is what makes a *declared* member landing there loud — it supplies to the C# switch the exhaustiveness the TypeScript `Record` gets from its type system.
- **The tables were NOT de-duplicated.** Collapsing them into one source of truth is a design change, not a fix, and it was explicitly left out of scope. What this bug needed was for the drift to become impossible to miss.
- **`formValid` had to become a derived value, not `useState` + `useEffect`.** This was forced, not cosmetic: `validateForm` is an inline arrow at both call sites, so its identity changes every render and it sits in the effect's dependency array. Storing a fresh *array* in state from that effect sets state on every render with a new object identity — an infinite render loop. The old boolean escaped only because React bails out on an identical primitive. The autosave gate is untouched and still reads `formValid`; it now sees the correct value one render *earlier* rather than one render late.
- **The reason strings go through `useTerminology()`.** A tenant who renamed "Work Item" to "Ticket" reads "Add at least one Ticket Type" — the warning has to name the field *as it appears on their screen*, or it sends them looking for something that isn't there. The data-retrieval reason uses `schema.displayLabel` with the connection's own display name as fallback.
- **The warning is gated on the same can-save flag as the save indicator** (`!disableSave` / `canUpdatePortfolioData`), so a user with no save permission at all is not told to go fix fields.
- **The predicates moved to module-level `teamAutoSaveBlockers`/`portfolioAutoSaveBlockers`.** Inlining the reasons pushed each component's cognitive complexity from 19 to 20, and Sonar embeds the number in its message — a changed count on a changed line risks being reported as a *new* issue.

## Gotchas worth remembering

- **Adding a work-tracking connector means editing BOTH schema tables.** `DataRetrievalSchemaDefaults.ts` and `DataRetrievalSchemaDto.cs`. The TS side will refuse to compile; the C# side will not. `SchemaFactories_EveryDeclaredWorkTrackingSystem_DoesNotUseTheQueryFallback` is now the thing that tells you.
- **Which schema copy wins depends on the screen.** The create wizard reads the frontend defaults; the settings page reads whatever the backend DTO returned. A disagreement between them produces a team that can be created but never saved — and the two screens will each look entirely reasonable in isolation.
- **This class of failure looks identical to Epic 5074's "Bug 3"** (a PascalCase/camelCase mismatch that made a *persisted* rule render as absent). Both present as "my setting vanished". They were told apart here by checking `GetEffectiveRuleSetJson`, which serialises camelCase and matches the zod schema exactly — so this time the rule was genuinely never stored, not hiding in the database.
- **One live alternative was never fully excluded**: the licence gate (`EditTeam.tsx:33` → `useLicenseRestrictions.ts:81`, non-premium `teamCount <= 3`) produces a byte-identical silent symptom. It is worth checking `canUsePremiumFeatures` first the next time a save silently does nothing.
- **The 30-second confirmation for any future "my settings vanished" report**: toggle an unrelated field on the same page and reload. If *that* is lost too, the bug is the form-validity gate, not the feature the user happened to be editing.

## Still open

**Blocked-rule validation is a permanent no-op on both controllers.** `TeamController.cs:279` and `PortfolioController.cs:185` deserialize the incoming camelCase rule JSON with default case-**sensitive** `System.Text.Json` options against the PascalCase `WorkItemRuleSet` (no `[JsonPropertyName]` attributes), so `Conditions` comes back empty and every check — `MaxRules`, field, operator, length — is bypassed.

This is real, pre-existing, and did **not** cause #5613. It was excluded from this fix by the maintainer for a concrete reason: the change flips validation from "accepts anything" to enforcing, so rule sets already stored in the wild would start returning 400. It was first logged in `docs/feature/epic-5074-blocked-items/deliver/review-slices-01-04.md` and still needs its own work item. Note the irony — it is the reason the maintainer got no feedback about the *rule* either.

## Quality gates

| Gate | Result |
|---|---|
| `dotnet build` | zero warnings, zero errors |
| `dotnet test` | 4146 / 4146 green |
| `pnpm test` | 285 files / 3810 tests green |
| `pnpm build` | `tsc -b` + vite + Biome, zero errors, zero warnings |
| `des-verify-integrity` | both steps, complete DES traces |
| CI on `017988cdb` | see below |

Two `LicensingIntegrationTest.ValidLicenseLoaded_*` failures during the run were environmental, not regressions: `valid_not_expired_license.json` is gitignored (`.gitignore:425`) and absent from a fresh worktree. Copying the fixture in from the main checkout turned both green, which is where the 4146 figure comes from.

## DELIVER checklist

- **Public docs prose** — N/A for the ServiceNow half: no user-facing page documents the ServiceNow connector yet, only ADRs 114-117 and the epic workspace. The settings warning is user-visible on a released surface, but it only appears on an invalid form and adds no capability to describe.
- **Per-feature screenshots** — N/A. The warning renders only when a form is blocked; every screenshot test drives valid demo data, so regenerating would produce byte-identical images.
- **Demo data** — N/A. No new entity, field or seeding path.
- **Lighthouse-Clients CLI/MCP versioning** — N/A. `DataRetrievalSchemaDto` is settings-form metadata consumed by the web UI; no CLI or MCP surface reads it, and no endpoint, route or DTO shape changed — only the values one arm of a switch returns.
- **Website marketing surface** — N/A. A defect fix in unreleased connector work plus an error-reporting improvement; no new capability to market.
- **RBAC impact** — N/A. No new endpoint and no permission-gated surface. The new warning is gated on the *existing* can-save flag, so it does not tell a read-only user to go change things.
- **EF migrations** — N/A. No persisted model changed; `WorkTrackingSystems` already carried its `ServiceNow` member.
- **Mutation testing** — deferred by agreement rather than skipped. The backend change is two arms of a data table whose every field is asserted individually, and whose exhaustiveness guard is itself the mutation-resistant part; the frontend change is a predicate list with one test per predicate. Both would score on assertions already written. Recorded here so the omission is visible rather than assumed.

## Retrospective note

The epic's own open call **C-3** said, in writing: *"Check before committing to it: whether `isWorkItemTypesRequired: false` merely skips validation or actually hides the field in the UI."* It was marked non-blocking and never done. It does both — and the two stacks disagreed about which.

So the check that would have caught this was not missing. It was written down, correctly scoped, correctly aimed, and then filed under "low risk". The cost of that judgement was a dogfood session where a maintainer configured something, watched it disappear, and had no way to tell whether the feature was broken or they were.

Worth noting how the diagnosis went sideways: every hypothesis in the bug report was about blocked rules, because that is what the reporter was doing when it happened. Four of them were ruled out with evidence before the actual cause — a schema table two directories away — came into view. The reproduction that would have skipped all of it is the cheapest one available and nobody thought to run it: change *something else* on the same page and see whether that survives a reload too.

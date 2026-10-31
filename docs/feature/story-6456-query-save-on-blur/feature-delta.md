# Feature Delta — story-6456-query-save-on-blur

ADO **User Story #6456**: *Team/Portfolio Query should only save after leaving of tab*. Active,
unparented.

> Right now, every change in the query for teams/portfolios (when using jira/ADO) will relatively
> quickly trigger an update. This is expensive, and often people type, think, type more, leading to
> consecutive updates. For this specific field, we should only save (and thus trigger an update) if
> we "tab out" of the query field.

**Waves run:** DISCUSS → DISTILL → DELIVER. On 2026-10-31 the maintainer explicitly skipped DISCOVER,
DIVERGE, DESIGN and DEVOPS ("skip discover, diverge, design and devops"). There are no artifacts for
those waves. What DESIGN and DEVOPS would have answered is recorded below as explicit N/As.

---

## Wave: DISCUSS / [REF] Persona IDs

| Persona | File | Role here |
|---|---|---|
| `config-admin` | `docs/product/personas/config-admin.yaml` | Edits a Team's or Portfolio's query on its settings page. The only actor. |

---

## Wave: DISCUSS / [REF] JTBD One-Liners

- `job-commit-intent-no-button` (existing). **When** I have just finished expressing a valid change,
  **I want** Lighthouse to act on my intent, **so I can** stay in the flow. This story sharpens
  *finished*: someone typing a query is not finished at every 300 ms pause, so the field commits only
  when they leave it. The job's anxiety, "What if it auto-saves something half-typed?", is the one
  this story removes for the query field.

No new job is added to `docs/product/jobs.yaml`.

---

## Wave: DISCUSS / [REF] Current-State Surface Inventory

| S | Fact | Evidence |
|---|---|---|
| S1 | Autosave saves the **whole** settings object 300 ms after any field changes, once the form is valid and the user has interacted with it. | `hooks/useModifySettings.ts:50`, `:305-331` |
| S2 | Team and Portfolio settings both turn on `refreshOnSave: true`, so every save also refreshes from the tracker. That refresh is the expensive part. | `ModifyTeamSettings.tsx:161`, `ModifyProjectSettings.tsx:175-178` |
| S3 | The query field is a 4-row multiline `TextField` that calls `onSettingsChange` on every keystroke. It is read-only when `isDataRetrievalReadOnly` is set. | `GeneralSettingsComponent.tsx:167-187` |
| S4 | The board wizard writes a complete query into the same setting in one step, through `handleWizardComplete`. | `GeneralSettingsComponent.tsx:63-70` |
| S5 | Only the two Modify pages use `GeneralSettingsComponent`. The create wizards do not, so they are unaffected. | grep, 2026-10-31 |
| S6 | Nothing handles `beforeunload` today. An edit inside the 300 ms window is already lost if the tab is closed. | `useModifySettings.ts` |
| S7 | Another field's change still sends the whole settings object, so until the query draft is committed it is not part of those saves. The committed (old) query is sent instead. | follows from S1 |

---

## Wave: DISCUSS / [REF] Locked Decisions

| # | Decision | Verdict |
|---|---|---|
| D1 | **Scope: only the query (data-retrieval) field.** Every other Team/Portfolio settings field keeps the 300 ms debounce. | Maintainer, 2026-10-31 |
| D2 | **Commit on blur.** While the field has focus, the typed text is a local draft. On blur the draft is passed to `onSettingsChange` **only if it differs** from the committed value. Autosave then runs as it does today: debounce, save, refresh. | Maintainer, 2026-10-31 |
| D3 | **Closing or reloading the tab while the field has focus loses the draft.** No `beforeunload` save and no warning. Navigating inside the app blurs the field first, so that edit is saved. | Maintainer, 2026-10-31 |
| D4 | **Nothing visible changes.** No helper text, no new save-state value. The save indicator moves to saving/saved after blur, as it does today after the debounce. | Maintainer, 2026-10-31 |
| D5 | **A complete value written from outside replaces the draft.** The board wizard (S4) and a settings reload (load, conflict reload) set the field's value. That value becomes the draft, and a wizard-written query saves immediately, as it does today. | Derived from S4; no UI impact |
| D6 | **A read-only field never commits.** `isDataRetrievalReadOnly` stays as it is. Blurring a read-only field sends nothing. | Derived from S3 |

---

## Wave: DISCUSS / [REF] Scope Assessment

**PASS.** One story, one component (`GeneralSettingsComponent`) and its tests, frontend only. Under a day.

---

## Wave: DISCUSS / [REF] WS Strategy

No walking skeleton. This changes behaviour on an existing, shipped surface. The one slice is
end-to-end by itself.

---

## Wave: DISCUSS / [REF] Driving Ports

- UI: the query field on **Team settings** and **Portfolio settings** (Modify pages), via
  `GeneralSettingsComponent`.

No HTTP, CLI or MCP surface changes. The `PUT` settings endpoints receive the same payload, just
less often.

---

## Wave: DISCUSS / [REF] Pre-requisites

None.

---

## Wave: DISCUSS / [REF] Out of Scope

- Changing the timing of any other settings field (D1).
- `beforeunload` save or warning (D3).
- Any visible "unsaved" signal (D4).
- The create wizards (S5).
- The backend, and the refresh itself: a single refresh still costs what it costs.

---

## Wave: DISCUSS / [REF] User Stories

### US-01: The query saves once, when I leave the field

**As a** `config-admin` editing a Team's or Portfolio's query, **I want** the query to save only
when I leave the field, **so that** typing, pausing to think and typing more does not set off a
save and a tracker refresh at every pause.

`job_id: job-commit-intent-no-button`

### Elevator Pitch
Before: every pause while typing a query saves and refreshes from Jira/ADO, so one edit costs several refreshes against half-typed queries.
After: type a query in *Team settings → query field*, pause, type more, then Tab out → sees the save indicator go to saving/saved **once**, after leaving the field, and not while typing.
Decision enabled: the admin can work a query out in place, trying and adjusting it, without paying a tracker refresh for every half-typed version.

#### Acceptance Criteria

1. **Typing does not save.** Changing the query and waiting past the 300 ms debounce, with the field
   still focused, sends no settings save.
2. **Leaving the field saves once.** Blurring the field after an edit sends exactly one save, and
   that save carries the edited query.
3. **Leaving an unchanged field sends nothing.** Focusing and blurring with no edit, or editing back
   to the committed value, sends no save.
4. **Other fields keep their debounce.** Editing any other settings field still saves after the
   debounce, without a blur. While a query edit is uncommitted, that save carries the committed
   query, not the draft (S7).
5. **The board wizard still saves at once.** A query written by the board wizard is saved without
   any focus or blur on the field (D5).
6. **Read-only stays read-only.** With `isDataRetrievalReadOnly`, focusing and blurring the field
   sends nothing (D6).
7. **Both pages behave the same.** AC 1-3 hold on Team settings and on Portfolio settings.

---

## Wave: DISCUSS / [REF] Story Map

| Activity | Edit the query |
|---|---|
| Slice 01 | US-01: commit the query on blur (`slices/slice-01-query-commits-on-blur.md`) |

---

## Wave: DISCUSS / [REF] Slice Taste Tests

- Thin? One component. ✓
- New abstraction first? None needed: a local draft inside the existing field. ✓
- Disproves something? *"Commit on blur is enough to stop the repeated refreshes"*. If people
  routinely close the tab mid-edit (D3), the edits they lose would show it is not. ✓
- Production data? Checked by hand on the dev instance against a real ADO/Jira connection. ✓
- Duplicate slices? None. ✓

---

## Wave: DISCUSS / [REF] Prioritization

One slice. No ordering question.

---

## Wave: DISCUSS / [REF] Outcome KPIs

| KPI | Target | Measured by |
|---|---|---|
| Saves per query edit | Exactly 1 per blur with a change, 0 while typing | Vitest (AC 1-3); a manual check in the browser network tab on the dev instance |

There is no usage-data event (see DEVOPS N/A below).

---

## Wave: DISCUSS / [REF] Project Checklist

| Item | Answer |
|---|---|
| RBAC impact | **N/A**, because no endpoint, permission or gate changes. The field keeps its read-only rule (D6), and autosave keeps `canSave: !disableSave`. |
| Lighthouse-Clients CLI/MCP | **N/A**, because the payload and endpoints are unchanged. Clients write settings directly and never had a debounce. |
| Website marketing surface | **N/A**, because this is an internal efficiency change with nothing to market. |
| Terminology | **N/A**, because no copy is added (D4). |
| Visualization skill | **N/A**, because nothing that shows data changes. |
| UI sketch | **N/A**, because nothing visible changes (D4). Layout, copy and controls are identical. DISTILL has no sketch to walk through. |

---

## Wave: DISCUSS / [REF] Skipped-Wave N/As

| Wave | Question it would answer | Answer |
|---|---|---|
| DESIGN | Hexagonal shape | Frontend driving adapter only (`GeneralSettingsComponent`, modified). No port, service, domain or driven adapter is touched. |
| DESIGN | Loading and failure looks | Unchanged. They are the existing save-state indicator's saving, saved and failed states. |
| DEVOPS | Usage-data event | **N/A**, because this changes when an existing action fires, not whether a feature is used. No count would tell us anything new. |
| DEVOPS | Pipeline / infra | **N/A**, frontend unit tests only. |

---

## Wave: DISCUSS / [REF] Definition of Done

1. AC 1-7 pinned by Vitest + RTL tests on `GeneralSettingsComponent` (and the page tests where they
   need the autosave hook).
2. `pnpm test` green.
3. `pnpm build` clean (Biome + `tsc`), `git status` checked afterwards.
4. No E2E change unless an existing spec relied on per-keystroke saving of the query. If one did, fix
   its page object to blur (watch the debounced-autosave navigation race).
5. StrykerJS on the changed component, kill rate ≥ 80%.
6. Checked by hand on the dev instance: one save in the network tab per Tab-out.
7. Docs: check `docs/` for any text saying the query saves as you type. Update it only if such text
   exists. Otherwise record N/A at finalize.
8. ADO #6456 moved Active → Resolved on push and → Closed once CI is green.

---

## Wave: DISCUSS / [REF] DoR Validation

| # | Item | Evidence |
|---|---|---|
| 1 | Clear value | Elevator pitch; S2 |
| 2 | Job traced | `job-commit-intent-no-button` |
| 3 | Testable ACs | AC 1-7, each observable as "a save was / was not sent" |
| 4 | Sized ≤ 1 day | Scope assessment |
| 5 | Dependencies known | None |
| 6 | Scope edges explicit | Out of Scope; D1-D6 |
| 7 | UX agreed | D4, nothing visible changes |
| 8 | Non-functional | No beforeunload (D3); no new requests |
| 9 | Maintainer sign-off | Decisions D1-D4, 2026-10-31 |

---

## Wave: DISCUSS / [REF] Wave Decisions Summary

- Primary need: stop a save and tracker refresh at every typing pause in the Team/Portfolio query field.
- Feature type: user-facing behaviour, frontend only, with no visible change.
- Constraints: query field only; commit on blur when changed; a tab close loses the draft; values
  written from outside replace the draft; read-only never commits.
- Upstream changes: none (no DISCOVER or DIVERGE).

---

## Wave: DISCUSS / [REF] Handoff

Next: **DISTILL**, as DESIGN and DEVOPS were explicitly skipped. Nothing visible changes, so DISTILL has no
sketch walkthrough. It writes AC 1-7 as Vitest + RTL tests, with no nWave scenario tags in the code.

---

## Wave: DELIVER / [REF] Rigor Profile

`standard` for slice 01 (maintainer, 2026-10-31): crafters and designers on `sonnet`, reviewers on
`haiku`. Run in AFK mode with refactor + review per slice, a per-slice push, and mutation testing once in
the final gates.

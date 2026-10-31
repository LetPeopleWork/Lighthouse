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
| D3 | **Closing or reloading the tab while the field has focus loses the draft.** No `beforeunload` save and no warning. Navigating inside the app blurs the field first, which commits the draft and starts the usual 300 ms autosave timer. If the page unmounts inside that window, the save is dropped. That race already exists for every settings field today; this story neither fixes nor worsens it (see Follow-ups). | Maintainer, 2026-10-31; the navigation sentence was corrected at the DISTILL review |
| D4 | **Nothing visible changes.** No helper text, no new save-state value. The save indicator moves to saving/saved after blur, as it does today after the debounce. | Maintainer, 2026-10-31 |
| D5 | **A complete value written from outside replaces the draft.** The board wizard (S4) and a settings reload (load, conflict reload) set the field's value. That value becomes the draft, and a wizard-written query saves immediately, as it does today. | Derived from S4; no UI impact |
| D6 | **A read-only field never commits.** `isDataRetrievalReadOnly` stays as it is. Blurring a read-only field sends nothing. | Derived from S3; no UI impact |
| D7 | **Once committed, the query follows today's autosave rules unchanged.** That covers an invalid form (not saved until valid), a conflict (autosave off until reload), no save permission (never saved) and a failed save (existing failed state and retry). The committed value stays in the field in every case. | Derived from S1; no UI impact |

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
- The create wizards (S5). The board wizard on the Modify pages **is** in scope (AC 5).
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
- Disproves something? *"Commit on blur is enough to stop the repeated refreshes"*. The
  dev-instance check (one save per Tab-out in the network tab) disproves it if typing still saves.
  Lost drafts from tab closes are not observable without a usage-data event, and none is planned. ✓
- Production data? To be checked by hand on the dev instance against a real ADO/Jira connection (DoD 6). Pending.
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


---

## Wave: DISTILL / [REF] Inherited commitments

| Origin | Commitment | DDR | Impact |
|--------|------------|-----|--------|
| DISCUSS#D1 | Only the query field changes; every other settings field keeps the 300 ms debounce | n/a | A name edit must still save after the debounce with no blur, pinned by an active test on both layers, so the change cannot spread to other fields |
| DISCUSS#D2 | Typing is a local draft; blur hands the draft over only when it differs from the committed value | n/a | Tests fail until the field stops handing over every keystroke, and they pin both "no save while typing" and "exactly one save on leaving" |
| DISCUSS#D3 | A tab close with the field focused loses the draft; no `beforeunload` | n/a | No test is written for it, because nothing may be built; the absence is the commitment |
| DISCUSS#D4 | Nothing visible changes | n/a | No sketch walkthrough and no copy or layout assertions are added; only save calls are observed |
| DISCUSS#D5 | A value written from outside (wizard, reload) replaces the draft and a wizard query saves at once | n/a | Two active guard tests pin the wizard hand-over without blur and the draft replacement, so the draft state cannot swallow outside values |
| DISCUSS#D6 | A read-only field never commits | n/a | A skipped RED test forces a change into a wizard-select query field and pins that leaving it hands over nothing |
| DISCUSS#AC1-7 | Each AC is observable as "a save was / was not sent" | n/a | Tests count saves on the real Team and Portfolio pages, and the field's hand-overs on the component, so the autosave hook is exercised end to end |
| Skipped DESIGN / DEVOPS | Frontend driving adapter only, no usage event | n/a | No port or adapter work is scaffolded, because the component and hook already exist and only the behaviour is missing |

---

## Wave: DISTILL / [REF] Scenario list

Tests are Vitest + RTL with plain names and no scenario tags in code. The classification lives here only.
"Skipped" tests fail today (RED, see below) and are un-skipped one at a time in DELIVER.

**GeneralSettingsComponent** (`Lighthouse.Frontend/src/components/Common/BaseSettings/GeneralSettingsComponent.test.tsx`, describe "the query is committed when the field is left"):

| Test name | AC | Status |
|---|---|---|
| hands over no change while the query is being typed | 1 | skipped (RED) |
| hands over the edited query once, when the field is left | 2 | skipped (RED) |
| hands over nothing when the query is edited back to what it was | 3 | skipped (RED) |
| hands over nothing when the query is focused and left without an edit | 3 | active, green today (regression guard) |
| hands over a changed name at once, without waiting for the field to be left | 4 (first half) | active, green today |
| hands over a query written by the board wizard at once, without the field being focused or left | 5 | active, green today (guard) |
| hands over nothing from a read-only query that is focused and left | 6 | skipped (RED); the change is forced past `readOnly` with `fireEvent`, because a user cannot type into it |
| shows a query that arrives from outside in place of what was being typed | 5 (draft replaced) | active, green today (guard) |

**Team page** (`Lighthouse.Frontend/src/components/Common/Team/ModifyTeamSettings.queryCommit.test.tsx`, real `GeneralSettingsComponent`, real autosave hook, saves counted):

| Test name | AC | Status |
|---|---|---|
| saves a changed name after the debounce without anyone leaving a field | 4 (first half) | active, green today (end-to-end proof) |
| saves nothing when the query is focused and left without a change | 3, 7 | active, green today |
| saves nothing while the query is being typed and the field is still focused | 1, 7 | skipped (RED) |
| saves the edited query once, after the field is left | 2, 7 | skipped (RED) |
| saves nothing when the query is edited back to what was saved before the field is left | 3, 7 | skipped (RED) |

**Portfolio page** (`Lighthouse.Frontend/src/components/Common/ProjectSettings/ModifyProjectSettings.queryCommit.test.tsx`, same shape):

| Test name | AC | Status |
|---|---|---|
| saves nothing when the query is focused and left without a change | 3, 7 | active, green today |
| saves nothing while the query is being typed and the field is still focused | 1, 7 | skipped (RED) |
| saves the edited query once, after the field is left | 2, 7 | skipped (RED) |
| saves nothing when the query is edited back to what was saved before the field is left | 3, 7 | skipped (RED) |

Findings that shape DELIVER:

- **AC 4 second half (an uncommitted draft stays out of another field's save) has no page-level test.**
  A user cannot reach it: clicking or tabbing to another field blurs the query first, which commits the
  draft, so both reach the save together. It holds by construction once the draft stays inside the field
  (AC 1), which the component test pins.
- **An existing test goes red when the behaviour lands.** `calls onSettingsChange with correct arguments
  when work item query changes` (same component test file, `fireEvent.change` with no blur) expects a
  hand-over per change. The crafter must make it leave the field (`fireEvent.blur`) in the same commit
  that un-skips the AC 1 and AC 2 tests.
- Polarity: 3 component tests and 6 page tests are RED; 5 component, 1 Team-page and 1 Portfolio-page
  guards are green today and must stay green.

---

## Wave: DISTILL / [REF] Test placement

Component behaviour goes into the existing `GeneralSettingsComponent.test.tsx`. The page tests are new
sibling files named after the page and the concern (`ModifyTeamSettings.queryCommit.test.tsx`,
`ModifyProjectSettings.queryCommit.test.tsx`), following the existing `ModifyTeamSettings.refinement.test.tsx`
and `.readiness.test.tsx` precedent. The existing page tests mock `GeneralSettingsComponent`, so they
cannot count saves caused by the real field. The new files render it for real and mock only `useRbac`
(the Portfolio file also provides the licensing, suggestion, team and portfolio services the page reads).
They wait past the debounce in real time, as `ModifyTeamSettings.test.tsx` already does, and raise the
per-file test timeout to 20 s because each test does a full page render plus a wait.

---

## Wave: DISTILL / [REF] Driving-adapter coverage

The only driving adapter is the query field UI. It is exercised through the Team page and the Portfolio
page (real component, real `useModifySettings` autosave, save mock counted) and through the component
alone. No HTTP, CLI or MCP surface changes.

## Wave: DISTILL / [REF] Adapter coverage

None. No driven adapter is added or changed; the settings services stay mocked, as in every existing page
test.

---

## Wave: DISTILL / [REF] RED classification

Every skipped test was run un-skipped once against unmodified production code. Each fails on an
assertion about saves or hand-overs, never on an import, render or setup error.

| Test | Failure |
|---|---|
| component: no change while typing | `AssertionError: expected [ …(15) ] to have a length of +0 but got 15` (one hand-over per keystroke). MISSING_FUNCTIONALITY |
| component: edited query once on leave | `AssertionError: expected [ …(15) ] to deeply equal [ [ 'dataRetrievalValue', … ] ]`. MISSING_FUNCTIONALITY |
| component: edited back to what it was | `AssertionError: expected [ Array(2) ] to have a length of +0 but got 2`. MISSING_FUNCTIONALITY |
| Team page: nothing saved while typing | `AssertionError: expected "vi.fn()" to not be called at all, but actually been called 1 times` (the save carries the half-typed query). MISSING_FUNCTIONALITY |
| Team page: edited query saved once on leave | same assertion, at the "not saved before leaving" step. MISSING_FUNCTIONALITY |
| Team page: edited back, nothing saved | same assertion (the round trip still saves). MISSING_FUNCTIONALITY |
| Portfolio page: the same three tests | identical three assertion failures. MISSING_FUNCTIONALITY |
| component: read-only query, focused and left | `AssertionError: expected [ [ 'dataRetrievalValue', …(1) ] ] to have a length of +0 but got 1`. MISSING_FUNCTIONALITY |

Verdict: 10 of 10 skipped tests fail for the right reason. The typing test now waits 500 ms past the debounce before asserting. No IMPORT_ERROR, FIXTURE_BROKEN or
OBSERVABLE_NOT_AT_PORT. A first parallel run showed two 5 s timeouts on green guards while three files
shared the worker pool; the per-file 20 s timeout removed that, and the second run was clean.

---

## Wave: DISTILL / [REF] Wave-decision reconciliation

DISCUSS holds the only wave decisions (D1-D6). DESIGN and DEVOPS were skipped by the maintainer and have
no artifacts, only the N/As recorded in DISCUSS. Reconciliation passed: 0 contradictions.
Warnings: DESIGN directory missing (the driving port is named by DISCUSS and by the maintainer's dispatch),
DEVOPS directory missing (default infra; frontend unit tests only).

E2E check: no Playwright spec under `Lighthouse.EndToEndTests` relies on the query saving while typing.
The only helpers that touch the field, `setDataRetrievalValue` and `getDataRetrievalValue` in
`tests/models/common/BaseSettingsPage.ts`, have no callers. The specs create teams and portfolios through
the API helpers. No E2E change is needed.

---

## Wave: DISTILL / [REF] Handoff

Next: **DELIVER** slice 01. Order: un-skip the component tests one at a time (AC 1, then 2, then 3),
update the old `fireEvent.change` test as noted, then un-skip the page tests, which need no further code.

---

## Wave: DISTILL / [REF] Review Gate

Reviewers on `haiku` (standard profile). The architect and platform reviewers were not dispatched:
DESIGN and DEVOPS were skipped by the maintainer and have no sections to review.

| Reviewer | Verdict | Outcome |
|---|---|---|
| Acceptance designer reviewer | conditionally approved, 2 high, 1 medium, 5 low | Fixed: the component typing test waits past the debounce; the read-only test forces the change past `readOnly` and is now skipped RED; the name assertion is exact; the wizard button matcher is anchored; three comments citing internal references were rewritten. Deferred to the DELIVER refactor: derive the page tests' 500 ms wait from the production debounce constant and share one helper. Accepted as is: the wizard and reload paths are pinned at the component's hand-over port. |
| Product owner reviewer | needs revision, 4 critical, 4 high | **F4 accepted:** D3's navigation sentence was wrong (no unmount flush; verified in `useModifySettings.ts`) and was corrected; the race is pre-existing and is listed under Follow-ups. **F6/F11 accepted** as D7. **F8:** D5 and D6 are marked as derived, with no UI impact. **F9, F10, F16 fixed.** **Rejected:** F1-F3 (Gherkin UAT and example tables; this project pins ACs as Vitest tests, which DISTILL has written) and F7 (slice briefs are a declared nWave machine artifact, separate by design). **F5 noted:** the job's persona is `delivery-forecaster`; the job is about committing intent without ceremony, which this story refines for free-text fields. |

## Follow-ups (not in this story)

- **Debounced-autosave navigation race.** Any settings edit made less than 300 ms before an in-app
  navigation is dropped, because unmount clears the pending timer. This affects every field and predates
  this story. A candidate fix is to flush the pending save on unmount in `useModifySettings`. It is for
  the maintainer to raise as its own Bug.

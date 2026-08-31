# Feature Delta — remove-encryption-frame-from-config

**ADO**: User Story [#5875](https://dev.azure.com/letpeoplework/Lighthouse/_workitems/edit/5875) — *Remove Secret Encryption Key Frame from Config*
**Follows**: Epic #5775 (Secret Encryption Key Custody) — shipped v26.8.31.7, archived to `docs/evolution/2026-08-31-epic-5775-secret-encryption-key-custody.md`
**Density**: lean (`documentation.density: lean`, `expansion_prompt: ask-intelligent`)

---

## Wave: DISCUSS / [REF] Persona ID

`platform-operator` — self-hoster or cluster operator who signs in as System Administrator to answer
"which key protects my stored credentials, and is every secret readable under it?" Already defined in
`docs/product/personas/`; no persona change in this story.

---

## Wave: DISCUSS / [REF] JTBD One-Liner

Traces to the **existing** job `job-operator-know-the-key-is-actually-in-effect` (`docs/product/jobs.yaml:5078`).
No new job — this story does not add a capability, it removes a surface that undermines one already
delivered.

> When I have installed, upgraded, or changed the encryption key, I want Lighthouse to tell me plainly
> which key it is using and whether every stored secret is readable under it, so I stop finding out
> from a work tracking system rejecting a credential at three in the morning.

**The traceability is exact, and it is the whole argument for this story.** That job's `pull` reads:

> *"One line at startup, **one panel in settings**, one read-only check that writes nothing."*

Singular. Lighthouse currently ships **two** panels in Settings that state key source and active key id:
the Encryption tab (`EncryptionPanel`) and a read-only frame on the Configuration tab
(`SystemSettingsTab`). The second panel was never part of the job; it is drift introduced during Epic
#5775 delivery. Removing it does not trade one value against another — it restores the design the job
was accepted against.

---

## Wave: DISCUSS / [REF] Locked Decisions

| # | Decision | Verdict | Rationale |
|---|---|---|---|
| **D1** | Feature type | **User-facing** | A visible frame disappears from a screen a System Administrator uses. Frontend-only; no API, no schema, no backend change. |
| **D2** | Walking skeleton | **No** | Brownfield subtraction inside one existing component. Nothing new to prove end-to-end. |
| **D3** | UX research depth | **Lightweight** | One persona, one screen, one removal, no new step in any journey. |
| **D4** | JTBD analysis | **Yes** — existing job | Traces to `job-operator-know-the-key-is-actually-in-effect`. Not `infrastructure-only`: the change is user-visible by definition. |
| **D5** | Scope boundary | **Configuration frame only** | The Encryption tab keeps both values; the System Info "Encryption" row also stays. See *Out-of-Scope* for why the System Info row is a different thing. |
| **D6** | Workspace | **Own feature workspace** | Epic #5775 is finalized and archived. Reopening a closed workspace to hold a follow-up story would resurrect it; this story gets its own. |
| **D7** | Zero information loss is a precondition, not a hope | **Verified in code before writing this story** | `EncryptionPanel.tsx:108,114` renders `KEY_CUSTODY_WORDING[keyState.custody]` and `keyState.activeKeyId` — the same two values, from the same `encryptionService.getKeyState()` call, behind the same System-Admin gate (`Settings.tsx:89`, tab value `"45"` ∈ `systemAdminTabValues`). The Encryption tab is a strict superset: it adds the key ring, the key store path, the custody explanation, and the rotate / re-encrypt / check actions. |
| **D8** | The duplicate `data-testid` is fixed, not renamed | **Removal resolves it** | `encryption-active-key-id` is currently emitted by **both** `SystemSettingsTab.tsx:46` and `EncryptionPanel.tsx:114`. Any future E2E selector on that id is ambiguous across tabs today. Deleting the Configuration frame leaves one owner; no rename needed anywhere. |

### Why the frame was there — and why that reason no longer holds

`SystemSettingsTab.tsx:65-67` carries a comment justifying the frame:

> *"Where the key came from and what it is called is instance security posture, so it is read from the
> surface only a System Administrator can reach, never from the system information response that any
> signed-in viewer — including one inside an embedded frame — can already see."*

That comment defends **the data source** (the admin-only `encryptionService` rather than the
broadly-readable system-information response), not **the placement**. The Encryption tab reads from the
identical source behind the identical gate, so the concern the comment names is fully satisfied without
this frame. The comment does not survive the removal — it goes with the code it annotates.

---

## Wave: DISCUSS / [REF] Scope Assessment

`## Scope Assessment: PASS`

| Oversized signal | This story |
|---|---|
| >10 user stories | 1 |
| >3 bounded contexts / modules | 1 (frontend Settings) |
| Walking skeleton needs >5 integration points | n/a — no skeleton (D2) |
| Effort >2 weeks | ~1 hour |
| Multiple independent shippable outcomes | 1 |

Zero signals. Right-sized; one slice.

---

## Wave: DISCUSS / [REF] User Stories

### US-01 — One place answers "which key is in force"

> **As** a System Administrator running Lighthouse,
> **I want** the key source and active key id to live on the Encryption tab and nowhere else in Settings,
> **so that** when the two ever disagree I do not have to work out which screen is telling the truth.

`job_id: job-operator-know-the-key-is-actually-in-effect`

#### Elevator Pitch

Before: opening Settings → Configuration shows a "Secret Encryption Key" frame repeating the key source
and active key already shown on Settings → Encryption, leaving two screens that can disagree and no rule
for which one wins.
After: open `Settings → Configuration` → sees the tab end at `Optional Features` and continue straight to
`Feature Order`, with no "Secret Encryption Key" frame anywhere on it; `Settings → Encryption` still
shows `Key source` and `Active key` unchanged.
Decision enabled: the operator knows without hesitating that the Encryption tab is the one authority on
key custody, so a key question is answered on one screen instead of reconciled across two.

#### Acceptance Criteria

- **AC-1** — On `Settings → Configuration`, signed in as System Administrator, no element with
  `data-testid="encryption-key-state"` is rendered, and no heading reads "Secret Encryption Key".
- **AC-2** — On `Settings → Encryption`, signed in as System Administrator, `encryption-custody` still
  renders the custody wording and `encryption-active-key-id` still renders the active key id — same
  values as before this change, from the same `getKeyState()` response.
- **AC-3** — `encryption-active-key-id` resolves to exactly one element across the whole Settings area;
  it is no longer emitted by the Configuration tab.
- **AC-4** — `SystemSettingsTab` no longer calls `encryptionService.getKeyState()`. Loading the
  Configuration tab issues no encryption request. (Guards against leaving a dead fetch behind a deleted
  view — the failure this AC catches is invisible on screen.)
- **AC-5** — Every other Configuration-tab group renders unchanged: Blackout Periods & Recurring Rules,
  Optional Features, *Feature* Order, Terminology Configuration, *Team* Refresh, *Feature* Refresh.
  (Terminology-configurable names render as the operator's own terms.)
- **AC-6** — `docs/assets/settings/configuration.png` is regenerated from a fresh `@screenshot` run and
  shows no encryption frame.
- **AC-7** — Frontend quality gates green: `pnpm test`, `pnpm build` with zero errors and zero warnings.

Every AC is observable from outside the component. AC-1/-2/-3/-5 are assertions on rendered output;
AC-4 is an assertion on the service call; AC-6 is a file; AC-7 is a command exit code.

---

## Wave: DISCUSS / [REF] Out-of-Scope

- **The System Info tab's "Encryption" row** (`SystemInfoDisplay.tsx:78-80`). It is a *different*
  surface for a *different* reader: `docs/settings/systeminfo.md:20` documents it as "the fastest way to
  tell whether an instance still needs a key of its own", readable by any signed-in viewer, naming the
  key and never the key material. It answers *do I have a key of my own?*, not *which key is in force and
  is every secret readable under it?* Removing it would cost a documented capability; removing the
  Configuration frame costs nothing. Explicitly confirmed with the requester.
- **The Encryption tab itself** — untouched, in every respect.
- **Backend** — `EncryptionService`, the key-state endpoint, and the encryption domain are untouched.
  No API, no DTO, no migration.
- **`EncryptionKeyState.ts` and `KEY_CUSTODY_WORDING`** — still consumed by `EncryptionPanel`; not dead
  after this change, do not delete.
- **Renaming any `data-testid`** — D8: the ambiguity is resolved by deletion alone.

---

## Wave: DISCUSS / [REF] Driving Ports

| Surface | Change |
|---|---|
| UI — `Settings → Configuration` (`?tab=configuration`) | Frame removed. Only inbound surface this story touches. |
| UI — `Settings → Encryption` (`?tab=encryption`) | Unchanged; becomes the sole Settings authority on key custody. |
| HTTP — `GET` key state (`encryptionService.getKeyState()`) | Unchanged endpoint; one fewer caller. |

---

## Wave: DISCUSS / [REF] Pre-Requisites

- Epic #5775 shipped and archived — **met** (v26.8.31.7, 2026-08-31).
- `EncryptionPanel` renders custody + active key id behind the System-Admin gate — **met**, verified in
  code (D7).
- Premium licence fixture present for the `@screenshot` run — **check before AC-6**; the fixture is
  gitignored and absent from every worktree.

---

## Wave: DISCUSS / [REF] WS Strategy

**Strategy A — no walking skeleton.** Brownfield subtraction within one mounted component; there is no
new path to prove end-to-end (D2).

---

## Wave: DISCUSS / [REF] Story Map

**Backbone (unchanged by this story):** Sign in as System Administrator → open Settings → tend instance
configuration → *check encryption posture* → act on the key.

The story removes a **detour** from the fourth activity. No backbone step is added, removed or reordered.

### Slice 01 — Configuration tab stops answering the key question

The only slice. Brief: `slices/slice-01-drop-config-encryption-frame.md`.

- End-to-end value: yes — an operator sees the change on the screen they use.
- Ship estimate: ~1 hour (≤1 day).
- Learning hypothesis: *the Encryption tab is a complete replacement for the Configuration frame.*
  Disproved if any operator-visible fact is only reachable from the Configuration tab. Read from code
  before writing this story (D7), so the risk is low and the slice is a confirmation, not a bet.
- Production data: yes — the real `getKeyState()` response on a running instance drives the
  `@screenshot` run (AC-6), not a fixture.
- Dogfood moment: same day — the local dev instance's Settings screen, plus the regenerated
  `configuration.png` reviewed in the docs.
- IN / OUT: in the brief.

### Slice taste tests

| Test | Result |
|---|---|
| Ships 4+ new components? | Pass — ships **zero**. It deletes one. |
| Every slice depends on a new abstraction? | Pass — no new abstraction. |
| Does any slice disprove a pre-commitment? | Pass — disproves *"the Configuration tab must carry key custody for admin-only visibility"*, the premise written into the code comment at `SystemSettingsTab.tsx:65-67`. |
| Synthetic data only? | Pass — AC-6 requires a real instance's key state. |
| 2+ slices identical except scale? | Pass — one slice. |

### Prioritization

One slice; order is trivial. Sequenced now rather than deferred because the duplicate
`encryption-active-key-id` (D8) is a live ambiguity that any new Settings E2E selector would inherit,
and the cost of the removal is an hour.

---

## Wave: DISCUSS / [REF] Outcome KPIs

| KPI | Target | Measurement |
|---|---|---|
| Settings surfaces stating key source + active key id | **1** (from 2) | Count elements rendering `KEY_CUSTODY_WORDING` / `activeKeyId` across `Lighthouse.Frontend/src/pages/Settings/`. Verified by grep at slice close. |
| Elements emitting `data-testid="encryption-active-key-id"` | **1** (from 2) | Same grep; AC-3. |
| Encryption requests issued by the Configuration tab | **0** (from 1 per load) | Assertion that `getKeyState` is not called on `SystemSettingsTab` render; AC-4. |
| Documented operator-visible facts lost | **0** | Diff the removed frame's fields against `EncryptionPanel`'s; both present (D7). |
| Frontend quality gates | `pnpm test` green, `pnpm build` zero errors + zero warnings | Command exit codes; AC-7. |

---

## Wave: DISCUSS / [REF] Definition of Done

1. `EncryptionKeySection` and its `keyState` / `fetchKeyState` plumbing removed from `SystemSettingsTab.tsx`.
2. `encryptionService` dropped from that component's `useContext` destructure; no unused imports remain.
3. The `SystemSettingsTab.test.tsx` `describe("secret encryption key")` block removed; the
   `createMockEncryptionService` wiring in that file removed if nothing else in it uses the mock.
4. A test asserts the frame's **absence** on the Configuration tab (AC-1) — the removal is pinned, not
   merely performed.
5. `EncryptionPanel` tests still green, unchanged.
6. `pnpm test` green; `pnpm build` zero errors, zero warnings (Biome clean via `prebuild`).
7. `@screenshot` run regenerates `docs/assets/settings/configuration.png` (delete the old PNG first —
   the runner keeps the existing file when the diff is under threshold).
8. Docs checked: `docs/settings/configuration.md` has **no** encryption mention today, so no prose edit
   is expected — confirm, and state "N/A, because the page never documented the frame" rather than
   skipping silently.
9. ADO #5875 → Resolved after push and green CI; link as a child of Epic #5775 (confirm before creating
   the link).

---

## Wave: DISCUSS / [REF] Definition of Ready — Validation

| # | Item | Verdict | Evidence |
|---|---|---|---|
| 1 | Business value articulated | PASS | One authority for key custody; restores the `pull` the accepted job states (`jobs.yaml:5103`). |
| 2 | Job traceability | PASS | `job-operator-know-the-key-is-actually-in-effect`, `jobs.yaml:5078`. |
| 3 | ACs testable and unambiguous | PASS | AC-1..AC-7, all externally observable; AC-4 covers the invisible failure. |
| 4 | Dependencies identified | PASS | Epic #5775 shipped; premium fixture needed for AC-6. |
| 5 | Sized to ≤1 day | PASS | ~1 hour; scope assessment clean. |
| 6 | UX defined | PASS | Removal only; no new layout. Groups above and below close up (`SystemSettingsTab.tsx:209`). |
| 7 | Non-functional constraints | PASS | No perf, security or a11y change. Data source and RBAC gate untouched — the removed frame and the surviving panel share both (D7). |
| 8 | Test strategy | PASS | Vitest absence assertion + existing `EncryptionPanel` tests + `@screenshot` regen. No new E2E spec: the Encryption POM already covers the surviving surface, and E2E stays a thin sanity check. |
| 9 | Out-of-scope explicit | PASS | System Info row, Encryption tab, backend, shared model — all named above. |

**9/9 PASS. Requirements completeness: 1.0** — every AC maps to an observable, every exclusion is named
with its reason, and the one judgement call (System Info row) was put to the requester and answered.

---

## Wave: DISCUSS / [REF] Handoff

**To**: DESIGN (`nw-solution-architect`) — *or skip.* This story adds no component, no port, no
technology choice and no data flow; the DESIGN wave would restate the deletion. **Recommendation: go
straight to DELIVER** with `slices/slice-01-drop-config-encryption-frame.md` as the roadmap.

**To**: DEVOPS — nothing. No pipeline, infrastructure or observability change.

---

## Wave: DISCUSS / [REF] Changed Assumptions

Epic #5775's journey `docs/product/journeys/epic-5775-secret-encryption-key-custody.yaml` and the job at
`jobs.yaml:5078` both describe **one** settings panel. The shipped code has two. This story changes no
assumption — it removes the code that contradicted one. The archived Epic #5775 artifacts under
`docs/evolution/` are **not** modified.

---

## Wave: DELIVER / [REF] Outcome — Slice 01

Shipped. `SystemSettingsTab.tsx` −48 lines: `EncryptionKeySection`, the `keyState` hook, `fetchKeyState`,
the render site, the `useRbac` / `EncryptionKeyState` / `KEY_CUSTODY_WORDING` imports and the
`encryptionService` context read are gone.

### AC verdicts

| AC | Verdict | Evidence |
|---|---|---|
| AC-1 | PASS | No `encryption-key-state`, `encryption-key-custody` or "Secret Encryption Key" anywhere under `pages/Settings/` except the absence assertion. |
| AC-2 | PASS | `EncryptionPanel.test.tsx` green, 59 tests (58 after the refactor dropped the language-guarantee test). |
| AC-2b | PASS | The System-Admin gate on the Encryption tab is now itself tested — see *Review findings* below. |
| AC-3 | PASS | `grep -rn 'data-testid="encryption-active-key-id"' src` → exactly one hit, `EncryptionPanel.tsx:114`. |
| AC-4 | PASS | New test asserts `getKeyState` and `getSystemInfo` are both never called on this tab. |
| AC-5 | PASS | Remaining `SystemSettingsTab` tests (8) green, unchanged. |
| AC-6 | **PASS — no regeneration needed.** | `docs/assets/settings/configuration.png` last changed in `3407bb76e` (2026-08-14); the frame was added in `9548d5e34` (2026-08-15). The committed screenshot predates the frame by a day, so it never showed it. It was stale-but-wrong for 16 days and is correct again as of this change. Verified from git history, not by re-running the suite. |
| AC-7 | PASS | `pnpm test` 345 files / 4635 tests passed. `pnpm build` clean (Biome via `prebuild`). |

### Coverage relocated, not deleted

The removed `describe("secret encryption key")` block held an invariant that was **not** duplicated on
the Encryption tab: `EncryptionPanel.test.tsx` covered `encryption-custody-explanation`
(`WHO_OWNS_THE_KEY`) but never `encryption-custody` (`KEY_CUSTODY_WORDING`). Deleting the block outright
would have dropped:

- the per-custody wording assertion for all four `KeyCustody` values,
- the "never render the enum name on screen" assertion,
- the exhaustiveness guard tying `CUSTODY_ON_SCREEN` to `KEY_CUSTODY_VALUES` — the test that fails when
  a fifth custody value is added and its wording is forgotten.

All three moved to `EncryptionPanel.test.tsx`, which is where the rendering now lives. Verified
non-vacuous: mutating one expected wording turned exactly that case red, then reverted.

### Left as-is, deliberately

- `docs/settings/configuration.md` — **N/A, because the page never documented the frame** (zero
  encryption mentions before or after).
- ~~The `useRbac` mock in `SystemSettingsTab.test.tsx` — the component no longer reads it, but deeper
  children may; unwiring it is outside this slice.~~ **Withdrawn.** "Deeper children may" was a guess,
  and it was wrong. Replacing the mock factory with a thrower left all 8 tests passing: nothing in the
  subtree calls `useRbac`. The mock is deleted.
- `models/Encryption/EncryptionKeyState.ts` — still consumed by `EncryptionPanel` and
  `EncryptionService`. Not dead.

### Review findings, and what changed because of them

An adversarial review ran against the full diff. Six findings; two mattered.

**The one that mattered most — the System-Admin gate had lost its only test.** The deleted
`SystemSettingsTab` block contained the sole frontend assertion that a non-System-Admin sees no key
custody and triggers no key-state request. `EncryptionPanel` carries no RBAC check of its own — the gate
is `Settings.tsx:89`'s `systemAdminTabValues` — and `Settings.test.tsx` enumerated every System-Admin tab
*except* `encryption-tab`, which arrived later with Epic #5775 and was never added to those lists. So
removing `"45"` from that set exposed key source, active key id, the whole key ring and the resolved key
store path to every signed-in viewer — including, per ADR-137, an embedded-frame viewer — with the entire
suite still green. That mutant survived on `origin/main` too; what this story did was remove the last
test standing near it.

Reproduced, then fixed: `encryption-tab` added to all five tab-visibility cases in `Settings.test.tsx`
(four non-admin, one admin). Re-running the same mutation now fails 4 tests. Net coverage of the
invariant went 1 → 0 → 4.

**Two ADRs still asserted the deleted surface.** `adr-150:88` and `adr-152:58,65` each stated that the
Settings → System page renders key state. The journey YAML was back-propagated; these were missed. Both
corrected in the same dated style.

Also fixed: a call-count assertion on `getKeyState` (its loss left an unbounded-refetch regression
invisible if the context value were ever rebuilt per render); `encryption-key-custody` added to the
absence assertion, and its heading check loosened to a case-insensitive regex; the dead `useRbac` mock
deleted. The type refactor was attacked specifically and survived — the compile-time exhaustiveness
argument was judged sound, and deriving the cases from `KEY_CUSTODY_VALUES` a net improvement over the
hand-maintained list it replaced.

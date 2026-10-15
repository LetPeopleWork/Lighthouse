# Taste Evaluation — story-6307-refresh-on-additional-fields-change

DIVERGE wave · 2026-10-15 · revision 2 (after peer review round 1). Options as curated in `options-raw.md`
(revision 2). Evidence ids (E1–E14) refer to `job-analysis.md` §2; outcome ids (O1–O6) to §6.

## 0. Constraint screen (applied to every option before scoring)

Three accepted decisions bear on this job. Each option is checked against all three; a conflict lowers its
Viability and is carried into the recommendation as a gate, because a weighted score cannot encode "this needs
the maintainer to overturn an accepted decision".

- **ADR-030** (dependent-data reload after auto-save). Its decision: "The expensive one-click path never
  auto-fires (OQ-2 resolved 2026-05-29, user) … This is the 'expensive ⇒ intentional' rule: the user controls
  when the heavy recompute runs while they are still tuning rules." Option A of that ADR cites "no surprise long
  operation". The operation it ruled on is a Team update: the forecast filter's "Reload throughput now" calls
  `teamService.updateTeamData` (`ForecastSettingsComponent.tsx:81-82`).
  *Counter-precedent in the same code:* every other Team and Portfolio settings save already fires that same
  update automatically after it saves (`useModifySettings.ts:252-254`, `autoRefreshOnSave`; `ModifyTeamSettings.tsx:122-126`).
  Lighthouse therefore carries both rulings today; which one a connection save follows is the maintainer's call.
  The rule that would be amended is ADR-030's Decision as a whole, "Split by recompute cost: cheap recompute →
  silent auto-refresh; expensive recompute → one-click", not only its "expensive ⇒ intentional" sentence. Its
  closest rejected alternative to Option 1 here is **Option B, "Always auto-refresh both"**, rejected because "a
  silent expensive throughput recompute on every keystroke-debounced filter edit would trigger repeated heavy
  backend work and a flickering forecast surface". *Does that reason transfer?* Partly. The repetition does not: a
  connection is saved with an explicit Save button, once per edit, and the cost is one full fetch per owner whose
  fingerprint moved, which ADR-140 has already committed. The size does: a whole connection's owners is a larger
  single operation than one Team's throughput, and "no surprise long operation" (ADR-030 Option A) applies to it
  in full. The maintainer needs both halves before ruling.
- **ADR-140** (fetch fingerprint). Connection-level invalidation is passive: "each entity notices at its own
  next cycle … rather than an N-entity write at connection-save time … the passive form keeps the
  connection-save path free of a fan-out write and of the partial-failure question that comes with one." It also
  rejects "Explicit admin choice on save — a 'refetch everything? [Y/N]' prompt" because the *mode* is
  derivable from the diff.
- **ARCHITECTURE.md:61** — "there is no partial mode": a fetch is full or two-phase delta, never per field.

| # | Option | ADR-030 | ADR-140 | No partial mode |
|---|---|---|---|---|
| 1 | Update on save | **conflicts** (an expensive update fires without the user choosing the moment); consistent with the Team/Portfolio auto-refresh precedent | **engages** the partial-failure concern — answered below; the fingerprint stays passive, only the timing changes. The "ask first" variant asks about *timing*, not the *mode* ADR-140 ruled on | ok |
| 2 | Waiting marker + one click | consistent (in-place, one click, user picks the moment) | consistent (reads the same passive fingerprint) | ok |
| 3 | Rules tell the truth | n/a | ok | ok; but **widens the rule-result contract** (ADR-013 leaves include/exclude to the caller over a two-valued match; a third "not loaded" outcome has to be handled by every caller) |
| 4 | The rule that needs the field asks for it | **conflicts** on the forecast filter surface, which is exactly the surface ADR-030 kept one-click | ok (single owner, like a Team settings save) | ok |
| 5 | Field-only backfill | n/a | contradicts its rejected "per-field granularity" | **violates** |
| 6 | Re-derive from stored payloads | n/a | ok | ok, but a second source of truth for item data |

**Option 1 and partial failure.** No fan-out *write* happens at save: the connection save commits on its own, and
each owner is then a separate queue item. A refresh that throws reaches the queue as a failure and is recorded
for that owner (`UpdateServiceBase.cs:25-49`). An owner's fingerprint is written only after its own fetch
completes (`WorkItemService.cs:61-62`, `:91-92`), so an owner whose update fails stays mismatched and its next
scheduled cycle is full again. A partial failure therefore leaves the failed owners exactly where they are today,
and shows each failure as its own row. This answers the question ADR-140 avoided, with machinery that exists; it
does not remove the need for DISCUSS to accept a save-time fan-out explicitly.

## 1. DVF filter

Scores 1–5. A lens scored ≤ 2 counts as failed (a convention adopted for this evaluation: the skill names "failing
two or more lenses" but sets no per-lens bar); an option failing two or more lenses, or totalling < 6, is
eliminated.

| # | Option | D | F | V | Total | Verdict |
|---|---|---|---|---|---|---|
| 1 | Update the connection's Teams and Portfolios on save | 5 | 4 | 3 | 12 | survives |
| 2 | "Waiting for an update" marker with one-click remedy | 4 | 4 | 4 | 12 | survives |
| 3 | Rules tell the truth about unfetched fields | 4 | 3 | 3 | 10 | survives |
| 4 | The rule that needs the field asks for it | 3 | 4 | 3 | 10 | survives |
| 5 | Field-only backfill | 3 | 1 | 2 | 6 | **eliminated** — fails F and V |
| 6 | Re-derive from what is already stored | 3 | 1 | 1 | 5 | **eliminated** — fails F and V, total < 6 |

Rationale per score:

- **1 · D5** removes the step the maintainer forgot (O3) and closes O1 to "as long as the queue takes".
  **F4** the parts exist — `TriggerUpdate` (`UpdateServiceBase.cs:35-49`), owners by connection id (a one-predicate
  repository question; the delete path asks the same predicate for existence at
  `WorkTrackingSystemConnectionController.cs:133-134`), the pure `FetchFingerprint.For` (`FetchFingerprint.cs:68-91`)
  to tell a field edit from a credential edit, the coalescing queue (E11) — but it is a new fan-out site at save
  time, which ADR-140 deliberately avoided. **V3** it needs the maintainer to set ADR-030 aside for this surface.
- **2 · D4** closes O4 fully; O1 only when someone clicks; O3 becomes a visible prompt instead of a memory.
  **F4** derived from the fingerprint, no new column, but rendered on Team, Portfolio and connection surfaces, each
  needing a loading and a failure look (project rule). **V4** fully consistent with ADR-030 and ADR-140.
- **3 · D4** closes O2, the highest-scoring outcome, but not O1. **F3** three rule surfaces (E9), match semantics
  (ADR-013) and a reliable "was this field fetched for this owner" signal. **V3** (revised from V4 in review
  round 2): a third rule outcome changes the contract every caller of the evaluator relies on — rule-based Delivery
  membership, the forecast filter's include/exclude, blocked detection — so it needs ADR-013 widened, a gate of
  the same kind as Option 1's.
- **4 · D3** closes O2 only for owners whose rule is saved after the field is added; fields read elsewhere (a
  Portfolio's owner, size or dependency field reference, write-back mappings) still wait. **F4** the rule save
  paths exist and a single-owner update after a save is already done for Team settings (E5). **V3** conflicts with
  ADR-030 on the forecast filter surface.
- **5 · D3** fixes values, invisibly. **F1** a new "read these fields for these ids" capability in every remote
  connector plus a partial write path the architecture rules out. **V2** ADR-140 rejected per-field granularity
  "for a saving that only appears on a rare edit", and the full fetch is already committed by the fingerprint.
- **6 · D3** would be instant. **F1** a raw payload stored per item on every connector; and a Jira search sends back
  only the fields it was asked for, so the stored payload would not contain the new field anyway. **V1** a second
  copy of all item data for one rare edit.

## 2. Weights (locked before scoring)

Preset: **Developer Tool**, taken unchanged from the `nw-taste-evaluation` skill, Phase 3 table, column
"Developer Tool": DVF 25%, Subtraction 15%, Concept Count 20%, Progressive Disclosure 15%, Speed-as-Trust 25%.
(The same table's "Default Weight" column, 30/20/20/15/15, is used below only as a sensitivity check.)
Reason for this column: the user is a technical configuration administrator (persona `config-admin`), and the
failure is a latency (speed-as-trust) and a piece of tribal knowledge (concept count).

| Criterion | Weight |
|---|---|
| DVF (average) | 25% |
| T1 Subtraction | 15% |
| T2 Concept count | 20% |
| T3 Progressive disclosure | 15% |
| T4 Speed-as-trust | 25% |

## 3. Taste scores

| # | Option | T1 Sub | T2 Concept | T3 Prog | T4 Speed |
|---|---|---|---|---|---|
| 1 | Update on save | 5 | 5 | 5 | 3 |
| 2 | Waiting marker + one click | 3 | 3 | 4 | 3 |
| 3 | Rules tell the truth | 3 | 3 | 4 | 3 |
| 4 | The rule asks for its field | 4 | 4 | 5 | 3 |

Breakdown:

- **1.** T1 5: adds one line of confirmation copy and removes the "next refresh" Alert (E4). T2 5: no new
  concept; "I saved, it updates" is what Team and Portfolio saves already do. T3 5: the only action is Save.
  T4 3 (revised from 4 in review round 2): every owner is queued at once and visible in the Task Manager, but on a
  large connection N sequential full fetches add up to a noticeable wait, possibly hours — "noticeable latency
  justified by a clear payoff", not "minor latency well masked". Scored on the same reading as Option 4.
- **2.** T1 3: a marker on three surfaces plus an action. T2 3: two new concepts ("waiting for an update", and
  why). T3 4: ambient marker, action one step away. T4 3: the state shows at once, the remedy waits for a human.
- **3.** T1 3: labels on three rule editors and on results. T2 3: "not loaded yet for this owner" per field.
  T3 4. T4 3: tells the truth at once, speeds nothing up.
- **4.** T1 4: no new screen; one check added to three save paths. T2 4: one implicit concept (a rule fetches what it
  reads). T3 5: no extra action. T4 3: the owner with the rule updates within minutes; every other owner still
  waits hours, silently.

## 4. Weighted matrix

| Option | DVF avg | T1 | T2 | T3 | T4 | Weighted total | Constraint gate |
|---|---|---|---|---|---|---|---|
| **1 Update on save** | 4.00 | 5 | 5 | 5 | 3 | **4.25** | needs ADR-030 amended for connection saves |
| 4 The rule asks for its field | 3.33 | 4 | 4 | 5 | 3 | 3.73 | needs ADR-030 amended for the forecast filter |
| 2 Waiting marker + one click | 4.00 | 3 | 3 | 4 | 3 | 3.40 | none |
| 3 Rules tell the truth | 3.33 | 3 | 3 | 4 | 3 | 3.23 | needs the rule-result contract (ADR-013) widened |

Arithmetic:
- Option 1: 4.00×0.25 + 5×0.15 + 5×0.20 + 5×0.15 + 3×0.25 = 1.00 + 0.75 + 1.00 + 0.75 + 0.75 = 4.25.
- Option 4: 3.33×0.25 + 4×0.15 + 4×0.20 + 5×0.15 + 3×0.25 = 0.83 + 0.60 + 0.80 + 0.75 + 0.75 = 3.73.
- Option 2: 4.00×0.25 + 3×0.15 + 3×0.20 + 4×0.15 + 3×0.25 = 1.00 + 0.45 + 0.60 + 0.60 + 0.75 = 3.40.
- Option 3: 3.33×0.25 + 3×0.15 + 3×0.20 + 4×0.15 + 3×0.25 = 0.83 + 0.45 + 0.60 + 0.60 + 0.75 = 3.23.

**Highest-scoring option with no constraint gate: Option 2 (3.40).**

### Sensitivity

- **Default preset** (30/20/20/15/15): 1 = 4.40, 4 = 3.80, 2 = 3.45, 3 = 3.25. Same order.
- **Option 1 at its first-round T4 reading (4):** 4.50. The T4 revision moves the margin from 0.77 to 0.52, not the order.
- **Leader downgraded:** Option 1 with D4, F3, V2 (ADR-030 fully held against it; DVF avg 3.00) = 0.75 + 0.75 +
  1.00 + 0.75 + 0.75 = 4.00 — still first. The matrix cannot express the ADR conflict; that is why it is a gate.
- **Runner-up upgraded:** Option 4 with D4 (DVF avg 3.67) = 0.92 + 0.60 + 0.80 + 0.75 + 0.75 = 3.82; with D4 and V4
  (DVF avg 4.00) = 3.90 — still second either way.
- **Option 2 at its best:** if the connection page's "Update all of them" makes the remedy one click from where the
  edit happened, T4 → 4: 1.00 + 0.45 + 0.60 + 0.60 + 1.00 = 3.65 — still behind Option 1 and Option 4.

## Gate G4

- [x] Constraint screen applied to every option.
- [x] DVF filter applied; two eliminations documented.
- [x] Weights locked before scoring; preset source quoted.
- [x] All four surviving options scored on all four criteria, each score with a rationale.
- [x] Ranking computed; sensitivity run on the leader, the runner-up and the best constraint-free option.

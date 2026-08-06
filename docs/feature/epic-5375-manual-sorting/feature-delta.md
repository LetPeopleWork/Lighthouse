# Feature Delta — epic-5375-manual-sorting

**ADO**: Epic #5375 "Manual Sorting" (Premium, Planned, reported by Lorenzo, created 2026-06-30, no
children at DISCUSS start) · **Feature type**: cross-cutting (forecasting + settings + authorization +
UI) · **Density**: lean (Tier-1 [REF] only) · **DISCUSS run**: 2026-08-06

The epic's description is a four-line sketch. This wave turns it into locked decisions, and the sketch
survives largely intact — the three things it did not say are what the wave is worth: ordering already
drives forecasts, `Order` is overwritten on every sync, and `Feature ↔ Portfolio` is many-to-many, which
is what makes "scoped to the portfolio" a decision rather than a detail.

**Revised 2026-08-06 after user input** (second pass, same day). Four changes, all narrowing risk:
the sorting page becomes a **general Features view** shared with Epic #4365 "Dependencies" (D17);
reordering ships as **discrete move actions**, not drag-and-drop (D18), which removes the epic's only
dependency blocker; the Portfolio surface reuses the **existing** Feature list rather than gaining a new
one (D10); and Done Features are treated as **irrelevant to ranking** rather than merely hideable (D15).

---

## Wave: DISCUSS / [REF] Prior-Wave Reading Confirmation

- ⊘ `docs/feature/epic-5375-manual-sorting/discover/` (not found — no DISCOVER wave ran)
- ⊘ `docs/feature/epic-5375-manual-sorting/diverge/` (not found — no DIVERGE wave ran)
- ✓ `docs/product/jobs.yaml` (schema_version 1) — no existing job covers feature ordering or priority;
  nearest neighbours are `job-forecast-throughput-tune` and `job-po-scope-cut-from-delivery-trend`, both
  about *what feeds* a forecast, not *what order* it runs in.
- ✓ `docs/product/journeys/` (37 journeys) — none touches ordering. `epic-5459-multi-team-forecasts.yaml`
  and `delivery-joint-likelihood.yaml` are the closest, and neither assumes anything about sequence.
- ✓ `docs/product/personas/` (9 personas) — `product-owner`, `config-admin`, `delivery-forecaster` all
  exist and are reused verbatim; no new persona needed.
- ✓ `docs/product/architecture/` — 131 ADRs read by index; none constrains feature ordering. ADR-110/113
  (joint forecasting) consume the forecast output, not its sequencing, so nothing is re-litigated here.
- ⊘ `docs/project-brief.md`, `docs/stakeholders.yaml` (not found — this repo carries product SSOT under
  `docs/product/` instead)
- ✓ `CLAUDE.md`, `docs/ci-learnings.md` — project standing rules applied (terminology, expand-only
  migrations, per-feature docs, quality gates).
- ✓ **ADO Epic #4365 "Dependencies"** (Premium, New, description: *"Set dependencies on Features"*) —
  read during the second pass because D17 makes this feature build the surface #4365 will land on. It is
  as sketchy as #5375 was, so nothing is assumed about its requirements beyond "it needs a per-Feature
  place to live and a cross-Portfolio view to be useful".

No DISCOVER evidence exists to contradict, so no contradiction check was possible and none is claimed.
Everything in the Current-State Surface Inventory below was read from code during this wave, not recalled.

---

## Wave: DISCUSS / [REF] Persona IDs

| Persona | Role in this feature |
|---|---|
| `product-owner` | Primary. Owns what gets built next; today cannot make Lighthouse agree with that. |
| `config-admin` | Turns the instance switch on and off, and carries the reversibility anxiety. |
| `delivery-forecaster` | Secondary. Consumes the forecast dates that ordering moves; does not reorder. |

---

## Wave: DISCUSS / [REF] JTBD One-Liners

| Job ID | One-liner |
|---|---|
| `job-po-own-the-order-the-forecast-uses` | When my tracker's rank does not match what we agreed to build next, I want to set the order Lighthouse forecasts against, so the dates I show stakeholders come from our real priority. |
| `job-po-order-features-when-the-connector-has-no-rank` | When my connector hands Lighthouse no priority signal at all, I want to supply one, so forecasts are sequenced by intent rather than by record number or import order. |
| `job-po-reorder-inside-my-own-portfolio` | When I own one portfolio on a shared instance, I want to reprioritise my own Features without needing rights over — or silently moving — anyone else's. |
| `job-config-admin-switch-ordering-ownership` | When I decide Lighthouse should own priority, I want one instance switch that starts from today's order and can be undone, so nobody's forecast jumps the moment I flip it. |
| `job-po-see-the-order-that-drives-the-forecast` | When two Features have very different forecast dates, I want to see where each sits in the queue Lighthouse simulates, so I can tell a priority effect from a throughput effect. |

### Opportunity Scores

Scored on the 1-5 scale `docs/product/jobs.yaml` already uses.

| Job | Importance | Satisfaction | Gap | Note |
|---|---|---|---|---|
| `job-po-order-features-when-the-connector-has-no-rank` | 5 | 0 | **5** | ServiceNow instances forecast in record-number order today, with no workaround short of editing records in the tracker. **Widened by the premise check**: any *multi-connector* instance is a second, far more common case — ints outrank all non-ints (S17), so ADO Features segregate ahead of Jira and Linear ones regardless of priority. Highest gap in the set. |
| `job-config-admin-switch-ordering-ownership` | 4 | 1 | **3** | Enabling ownership is the whole premise; a one-way door would sink adoption on its own. |
| `job-po-own-the-order-the-forecast-uses` | 5 | 2 | **3** | A workaround exists — go re-rank the backlog in the tracker — but it costs a round trip and is often not the PO's call to make. |
| `job-po-reorder-inside-my-own-portfolio` | 3 | 0 | **3** | Only bites on multi-Portfolio instances, but there it is a hard blocker: without it the feature is admin-only. |
| `job-po-see-the-order-that-drives-the-forecast` | 3 | 1 | **2** | The list *is* ordered today, but nothing says the order is load-bearing, so nobody reads it as information. Ranked lowest deliberately — it explains a problem rather than solving one. |

---

## Wave: DISCUSS / [REF] Current-State Surface Inventory

Read from code during this wave. Cited so DESIGN does not re-derive.

| # | Fact | Location |
|---|---|---|
| S1 | Ordering rides on exactly one field: `WorkItemBase.Order`, a **string**. There is no numeric rank anywhere. | `Models/WorkItemBase.cs:36` |
| S2 | `WorkItemBase.Update` copies `Order` from the source system on **every** refresh. Any manual value written into this field is destroyed on the next sync. | `Models/WorkItemBase.cs:142` |
| S3 | `FeatureComparer` reconciles the string: int-parse first (ints sort ahead of everything), then double-parse with the comparison **inverted** for Linear, then `string.Compare`. | `Models/FeatureComparer.cs:8-44` |
| S4 | Five call sites apply it — four read paths and one write path. Any new ordering must be honoured by all five or the UI and the forecast disagree. | `FeatureRepository.cs:18`, `:23`; `PortfolioDto.cs:15`; `FeaturesController.cs:93`; `WorkItemService.cs:535` |
| S5 | **Ordering drives forecasts, not just display.** Each simulated day a team draws from the first `FeatureWIP` remaining Features *in `Order` sequence*; reorder and the throughput lands on different Features, producing different 50/70/85/95% dates. | `ForecastService.cs:201-209` (+ `:180-189`) |
| S6 | Per-connector `Order` quality: ADO = `Microsoft.VSTS.Common.StackRank` (fallback `BacklogPriority`); Jira = `issue.Rank` (LexoRank, default field `customfield_10019`); Linear = `SortOrder` double, **lower means higher**; CSV = optional `order` column, frequently absent; ServiceNow = **`recordNumber`** — not a rank at all. | `WorkItemExtensions.cs:42-51`, `JiraWorkTrackingConnector.cs:1036`, `IssueFactory.cs:14`, `LinearWorkTrackingConnector.cs:177/338/415`, `CsvWorkTrackingConnector.cs:202`, `ServiceNowWorkItemMapper.cs:123` |
| S7 | `Feature ↔ Portfolio` is **many-to-many**. A Feature can belong to several Portfolios simultaneously. | `Data/LighthouseAppContext.cs:217-219`; `Feature.cs:41`; `Portfolio.cs:11` |
| S8 | The Portfolio Feature list renders through MUI-X DataGrid with **no initial sort model**, so the backend order shows through — but a user clicking any column header replaces it. | `PortfolioFeatureList.tsx:98-130`, `FeatureListDataGrid.tsx:19-60` |
| S9 | A per-portfolio "hide completed" toggle already exists and filters Done Features with no remaining work. | `FeatureListDataGrid.tsx:31-43` |
| S10 | The top navigation has exactly **two** entries: `{ path: "/", text: "Overview" }` and `{ path: "/settings", text: "System Settings" }`. Routes are `/`, `/connections`, `/teams/:id/:tab?`, `/portfolios/:id/:tab?`, `/settings`. There is no instance-wide Feature surface of any kind. | `App/Header/Header.tsx:59-60`; `App.tsx:202-224` |
| S11 | Premium gating exists and is attribute-driven: `[LicenseGuard(RequirePremium = true)]` → `ILicenseService.CanUsePremiumFeatures()`. | `LicenseGuardAttribute.cs:17,36-40`; `LicenseService.cs:49` |
| S12 | RBAC guard requirements include `PortfolioRead` and `PortfolioWrite`. | `Models/Authorization/RbacGuardRequirement.cs` |
| S13 | `FeaturesController` is mounted at `api/v1/features` + `api/latest/features` and today exposes **only GETs** (`ids`, `references`, `{id}/workitems`). No write surface. | `FeaturesController.cs:13-17,42,55,68` |
| S14 | **No drag-and-drop library is installed** in the frontend. MUI-X DataGrid's own row-reorder is a Pro/Premium licensed feature. *Neutralised by D18 — no longer a blocker.* | `Lighthouse.Frontend/package.json` (grep for dnd/sortable/drag → no match) |
| S15 | The Portfolio list already resolves the configurable term via `getTerm(TERMINOLOGY_KEYS.FEATURE)`. | `PortfolioFeatureList.tsx:44` |
| S16 | `FeatureListDataGrid` + its `columns.tsx` factory are **shared components**, already parameterised by a caller-supplied column list and storage key — so one column and one row-action menu added there serve every Feature list at once. | `components/Common/FeatureListDataGrid/` |
| S17 | **`FeatureComparer` segregates by connector.** Int-parseable values sort ahead of *everything* non-int (`:14-25`), so on a multi-connector instance every ADO Feature precedes every Jira and Linear Feature unconditionally, whatever their priority. Measured on the dev instance below. | `FeatureComparer.cs:14-25` + premise check |

---

## Wave: DISCUSS / [REF] Premise Check Results (slice 01, run early 2026-08-06)

Slice 01's premise check was pulled forward and run before DESIGN, because both of its failure modes
would have reshaped the epic. Source: the dev instance's SQLite database
(`Lighthouse.Backend/LighthouseAppContext.db`, real recorded history, 94 Features, 3 Portfolios, mixed
ADO + Jira + Linear connectors). The dev API on `:5169` was not running, so this was read directly from a
copy of the database rather than over HTTP.

**Hypothesis 1 — "the source order is noise" — DID NOT FIRE.**

| Measure | Value |
|---|---|
| Features | 94 |
| Blank or null `Order` | **0** |
| Distinct `Order` values | 91 of 94 |
| Duplicate values | 3 pairs — and all six are `StateCategory = Done`, so no tie touches the not-Done set ranking is *for* |

D6 can seed from the current order. US-02's "nothing moves" promise stands. Note the tie evidence cuts
**for** slice 02's own hypothesis surviving too: ties exist and `FeatureComparer` returns 0 for them, so
subset-vs-full-set sort stability is still worth the 30-minute check that brief calls for — it simply is
not a Done-set problem.

**Hypothesis 2 — "the flat list is unusable at size" — INCONCLUSIVE, not passed.** 38 not-Done Features
is comfortable, but this is a dev instance and proves nothing about a customer's. Slice 01 keeps the
hypothesis and re-checks it against the dogfood instance.

**Unanticipated finding, and the most useful thing the probe produced.** The dev instance carries three
`Order` shapes at once — 86 int-parseable (ADO StackRank), 4 LexoRank strings (Jira), 4 doubles (Linear).
Combined with S17, that means **a multi-connector instance has no meaningful cross-connector order at
all**: every ADO Feature outranks every Jira and Linear Feature by construction, not by anyone's
decision. Two consequences:

1. This is a **second** "no meaningful order exists" class alongside ServiceNow, and a much more common
   one. It widens `job-po-order-features-when-the-connector-has-no-rank` from a ServiceNow/CSV story to
   *any instance wired to more than one tracker*, which is a materially stronger case for the epic.
2. D6 still holds — seeding preserves the segregation, so nothing moves on enable — but the seeded
   starting point on such an instance is **garbage from day one**, and the first thing that user will do
   is a large re-sort. That argues directly for slice 04's picker (D18) rather than relative moves alone,
   and it is a second input to K7 beyond raw usage counts.

**Pre-requisite gap — now confirmed, not merely suspected.** All 90 Portfolio-linked Features sit in
**exactly one** Portfolio. There is no shared Feature anywhere on this instance, so D11's
write-on-every-Portfolio rule and AC-3.8 have no real-data case here. Demo data must seed one (slice 01),
and D11 ships field-unvalidated until an instance with a genuine shared Feature exists.

**Orphans — noted, deliberately not built for.** 4 Features belong to no Portfolio at all, so the
Features view's `PortfolioRead` filter (D11) would hide them from everyone including admins. All four
are Linear-sourced, `StateCategory = Unknown`, with **zero** `FeatureWork` rows and zero remaining work —
inert leftovers that enter no forecast (`InitializeSimulationResults` needs `RemainingWorkItems > 0`).
So nothing that matters is hidden. If an orphan ever *did* carry work it would be forecast while
invisible, since team membership flows through `FeatureWork` and not through Portfolio — recorded here
for DESIGN as a known edge, not as scope.

---

## Wave: DISCUSS / [REF] Locked Decisions

| # | Decision | Rationale / source |
|---|---|---|
| **D1** | **Features only.** Work items inside a Feature are not manually orderable. | User, 2026-08-06. Matches the epic ("ALL features"), and Feature order is the only ordering any consumer reads (S5). Work-item order has no consumer at all today, so ordering it would ship plumbing. |
| **D2** | **One instance-wide switch, all-or-nothing.** When manual sorting is ON, every Feature is manually ranked and the source `Order` is ignored entirely. No mixed mode. | Epic text ("It's either or"). A per-portfolio or per-Feature opt-in would leave the forecast with two incomparable orderings for one team (S5, S7). |
| **D3** | **One global total order.** Not one order per Portfolio. | Forced by S5+S7: a team spans Portfolios, so per-Portfolio orderings give its simulation no unambiguous next Feature. The epic already says it — "they are all in relation to each other". |
| **D4** | **Insert-at-target** is the translation rule for a move made through any filtered view. The moved Feature lands at the **global rank of the row it was placed against**; the block it jumped shifts by one. | User, 2026-08-06, after comparing against slot permutation. It is one rule for the Features view, the Portfolio list and every RBAC scope — and since S12 means most users see a filtered list anyway, a rule defined against "the visible slot set" would give two users different results from the same gesture. It also causes strictly fewer relative-order changes than slot permutation, and relative order is the only thing `ForecastService` reads. D18 makes it more literal, not less: "Move above Feature X" *is* insert-at-target, spelled out. |
| **D5** | **Manual rank lives in a new persisted field, never in `Order`.** `Order` continues to carry the source value untouched. | Forced by S2 — a value written to `Order` is destroyed on the next sync. Keeping `Order` intact is also what makes D9 (turn-off) a genuine revert rather than a re-import. |
| **D6** | **Enabling seeds from the current order.** Every Feature gets a rank derived from today's `FeatureComparer` result, so the visible order is byte-identical the instant the switch flips. | `job-config-admin-switch-ordering-ownership`. A switch that reshuffles the board on activation would be indistinguishable from a bug. |
| **D7** | **New Features append silently to the end.** No badge, no notification, no "needs triage" state. | User, 2026-08-06, choosing the epic's original wording over the badge alternative offered. Accepted cost, eyes open: a Feature arriving after the last sort is invisible until someone scrolls, and it forecasts last. Revisit if the tail turns out to be where real work hides. |
| **D8** | **No write-back.** The manual order is never pushed to ADO StackRank, Jira Rank or Linear SortOrder. | User, 2026-08-06. Lighthouse owns its own ordering; the tracker is untouched. Rank write-back (LexoRank generation in particular) is its own epic if it is ever wanted. |
| **D9** | **The switch is bidirectional.** Turning it off restores the source order immediately, and manual ranks are **retained**, so turning it back on restores the manual order rather than re-seeding. | `job-config-admin-switch-ordering-ownership` anxiety force. Retention is nearly free (D5 keeps the two fields separate) and converts a one-way door into an experiment. |
| **D10** | **Two surfaces, one order — and neither is new UI built for sorting.** (a) The Features view (D17); (b) the **existing** Portfolio detail Feature list, which gains a row-action menu and nothing else. | User, 2026-08-06: *"For sorting/viewing on Portfolio, we should use the existing Features View."* `FeatureListDataGrid` and its `columns.tsx` factory are already shared (S16), so the column and the actions are written once and appear on both. |
| **D11** | **RBAC: everyone may open the Features view; content is filtered to Portfolios the user can read; actions require `PortfolioWrite`.** A Feature may be moved only if the user holds `PortfolioWrite` on **every** Portfolio it belongs to; a shared Feature the user only partly owns renders its move actions disabled, naming the blocking Portfolio. | User, 2026-08-06: *"This is viewable by all, what they see and if they can do any actions is governed by their role."* The all-Portfolios mutation rule is this wave's addition: S7 means one move can re-sequence a Feature another Portfolio forecasts against, and "write on at least one" would let a PO reorder someone else's delivery. Open to DESIGN revision if it proves too strict. |
| **D12** | **The view is not premium. The manual order is.** The Features view, its position column and its help text ship to every instance, licensed or not, and stay visible when manual sorting is OFF. The switch and every move action require premium (S11). | Follows from D17 — the view is general infrastructure that later hosts dependencies, not a sorting page wearing a hat. And the position column reports a fact that is already true and already driving every forecast (S5); withholding it would be withholding an explanation. |
| **D13** | **Rank is a dense contiguous integer, renumbered across the affected block on each move.** | Simplest correct thing at Lighthouse's scale (hundreds to low thousands of Features). A sparse or LexoRank-style key would avoid the block UPDATE; noted for DESIGN, not chosen here, because it buys nothing at this size and costs a rebalancing path. |
| **D14** | **Relative move actions are disabled while a column sort is active; absolute ones are not.** "Move Up/Down/Top/Bottom" have no predictable meaning when the grid is sorted by Name (S8), so they grey out with a tooltip. "Move above/below Feature X" names its target explicitly, so it stays available under any sort. | D18 improves on what drag could offer here — a drag while sorted is simply meaningless, whereas an explicitly-targeted move is not. Filtering (including `hideCompleted`, S9) stays fully compatible under both: D4 translates through the *target row's* global rank, which is well-defined under any filter. |
| **D15** | **Done Features are irrelevant to ranking and are hidden by default on both ordering surfaces**, and are excluded from the "move above/below Feature X" target picker. They keep whatever rank they hold — no special storage rule, no rank reclamation. | User, 2026-08-06: *"while done features also have a rank, they are not really relevant (as the main thing for ranking is forecasting and done items dont matter)."* Confirmed in code: `InitializeSimulationResults` only admits `FeatureWork` with `RemainingWorkItems > 0`, so a Done Feature already contributes nothing to a forecast. Hiding rather than un-ranking keeps D7 simple if a Feature reopens. |
| **D16** | **The UI calls them whatever the instance calls them** — `getTerm(TERMINOLOGY_KEYS.FEATURE/FEATURES)` throughout, including the new nav entry, page title and switch label. | Project standing rule; S15 shows the Portfolio list already does it. The nav entry in particular: an instance that renames Features to "Deliverables" gets a "Deliverables" tab. |
| **D17** | **A general Features view is added as the third top-level navigation entry**, beside Overview and System Settings (S10), at `/features`. It lists every Feature the user can see across all Portfolios, and it is the surface that will later host **Epic #4365 "Dependencies"**. It is not a sorting page. | User, 2026-08-06. This reframes the largest piece of the epic from *a page for a premium feature* into *a shared surface two epics land on*, which is why D12 makes it free and always-visible. Nothing of #4365's requirements is designed here — only the obligation not to build a surface that would have to be rebuilt for it. |
| **D18** | **Reordering ships as discrete move actions, not drag-and-drop**: Move to Top, Move Up, Move Down, Move to Bottom, and Move above/below a named Feature. Drag-and-drop is explicitly deferred, not planned. | User, 2026-08-06: *"Drag and Drop is nice, but we can also work with arrows … That may be enough for the beginning."* Three things this buys: it neutralises S14 (no DnD dependency decision, no MUI-X Pro licence question) and removes the epic's only true blocker; it is keyboard- and screen-reader-operable by construction, which drag never is; and long-range moves become *possible* — on a 300-Feature list, "Move above Feature X" is the only usable gesture, since dragging across 200 rows is not a real interaction. Every action reduces to the same insert-at-target primitive (D4), so the rule is unchanged and the endpoint is a single shape. |

---

## Wave: DISCUSS / [REF] User Stories

### US-01 — See every Feature I have rights to, in the order that drives the forecast

**As a** product owner **I want** one place listing all Features across my Portfolios, in the order
Lighthouse simulates **so that** I can tell a priority effect from a throughput effect instead of
guessing why a date is late. · `job_id: job-po-see-the-order-that-drives-the-forecast`,
`job-po-own-the-order-the-forecast-uses` · **not** premium (D12)

#### Elevator Pitch
Before: Features can only be seen one Portfolio at a time, the list happens to be in forecast order but
nothing says so, and the order that decides every date is invisible.
After: click **Features** in the top nav (third entry, after Overview and System Settings) → sees every
Feature across every Portfolio you can read, in one ranked list, with a `#` column and help text reading
"Lighthouse forecasts Features in this order — the top of the list gets your teams' throughput first."
Decision enabled: whether a late date is because the Feature sits low in the queue (fix the order) or
because throughput is low (fix the flow).

**Acceptance criteria**
- AC-1.1 A third top-level nav entry appears beside Overview and System Settings (`Header.tsx:59-60`),
  labelled with `getTerm(TERMINOLOGY_KEYS.FEATURES)` (D16), routing to `/features`.
- AC-1.2 The view lists Features from every Portfolio the user holds `PortfolioRead` on, in global rank
  order, and lists nothing else (D11).
- AC-1.3 It is reachable on a **non-premium** instance and while manual sorting is **off** (D12) — in
  both cases read-only, with no move actions rendered.
- AC-1.4 Each row shows Portfolio membership; a Feature in several Portfolios shows all of them and
  appears exactly once.
- AC-1.5 The position column shows the Feature's rank **across the whole instance**, not its index in the
  visible rows — two rows shown consecutively may read `4` and `17`. The same column appears on the
  existing Portfolio Feature list, from the same shared factory (D10, S16).
- AC-1.6 Sorting the grid by another column leaves every position value unchanged.
- AC-1.7 Done Features are hidden by default on this view (D15) and revealed by the existing toggle;
  positions of the remaining rows are unchanged by the toggle.
- AC-1.8 A Feature whose source `Order` is empty still renders a position; no blank cell, no `NaN`.
- AC-1.9 The view renders and stays interactive for an instance with 500 Features.

---

### US-02 — Stop the tracker reshuffling my forecast every sync

**As a** config admin **I want** to hand ordering ownership to Lighthouse with one switch **so that**
the forecast stops moving for reasons nobody on my team decided. · `job_id:
job-config-admin-switch-ordering-ownership` · premium

#### Elevator Pitch
Before: every refresh re-imports the tracker's rank, so a rank change made by anyone in ADO or Jira
silently re-sequences the forecast; on ServiceNow the sequence was never meaningful to begin with.
After: open **Settings → System → Manual Sorting**, toggle it on → the Features view is unchanged, and
the `#` column header now reads "Manual"; a full refresh leaves every position exactly where it was.
Decision enabled: whether Lighthouse's forecast reflects the tracker's opinion or this team's.

**Acceptance criteria**
- AC-2.1 Flipping the switch on assigns every Feature a rank matching the pre-flip `FeatureComparer`
  order; the rendered list is identical before and after, for every connector's `Order` shape —
  int (ADO), LexoRank string (Jira), inverted double (Linear), record number (ServiceNow), empty (CSV).
- AC-2.2 With the switch on, running a full work-item refresh changes no Feature's rank, while
  `WorkItemBase.Order` is still updated from the source (D5 — the two fields are independent).
- AC-2.3 With the switch on, all five ordering call sites (S4) return the manual order. An integration
  test asserts the Portfolio DTO, the features endpoint and the forecast input agree.
- AC-2.4 Flipping the switch off restores the source order everywhere immediately; flipping it on again
  restores the previous manual order rather than re-seeding (D9).
- AC-2.5 A non-premium instance receives 403 on the enable endpoint and the switch renders disabled
  with the standard premium affordance. The Features view itself stays reachable (AC-1.3).
- AC-2.6 A Feature that arrives while the switch is on receives a rank at the end of the list and
  produces no notification, badge or log entry (D7).
- AC-2.7 The switch requires `SystemAdmin`; a `PortfolioWrite`-only user gets 403 and no control.

---

### US-03 — Move a Feature up the order

**As a** product owner **I want** Move to Top / Up / Down / Bottom on any Feature I own **so that** the
forecast sequences my delivery the way we actually agreed. · `job_id:
job-po-reorder-inside-my-own-portfolio`, `job-po-own-the-order-the-forecast-uses` · premium

#### Elevator Pitch
Before: the only way to change what Lighthouse forecasts first is to go and re-rank the backlog in the
tracker — a round trip that is often not the PO's to make, and impossible on ServiceNow.
After: on the **Features** view or on **Portfolio → detail → Features**, open a row's action menu and
choose **Move to Top** → the row jumps to the top of the list you are looking at, the `#` column
renumbers, and the Feature's forecast dates move on the next forecast run.
Decision enabled: which Feature the team's throughput lands on first, and therefore which delivery date
is credible.

**Acceptance criteria**
- AC-3.1 Moving Feature X to the position of Feature Y gives X the global rank Y held; every Feature
  between the old and new position shifts by exactly one; no other Feature's relative order changes (D4).
- AC-3.2 The rule holds when the visible Features are non-contiguous in the global order: with global
  `F1 F2 F3 F4 F5 F6` and Portfolio A = `{F2, F4, F5}`, **Move to Top** on F5 from A's list yields
  `F1 F5 F2 F3 F4 F6` — F5 lands above A's first Feature, **not** at global rank 1 (D10, D4).
- AC-3.3 **Move Up** targets the previous **visible** row, so hidden Done Features (D15) and filtered-out
  rows are jumped, not landed on. **Move to Bottom** sends the Feature to the end of the global order.
- AC-3.4 Features outside the visible set are never reordered relative to each other.
- AC-3.5 The move persists: a page reload and a full work-item refresh both show the new order.
- AC-3.6 The forecast changes accordingly — an integration test moving a Feature into the top
  `FeatureWIP` positions asserts its 85% date improves and the displaced Feature's worsens.
- AC-3.7 A user without `PortfolioWrite` sees the action menu without move entries; the endpoint returns
  403.
- AC-3.8 A Feature belonging to a Portfolio the user cannot write renders its move actions disabled with
  a tooltip naming that Portfolio; the endpoint returns 403 for it (D11).
- AC-3.9 Relative moves are disabled with an explanatory tooltip while the grid is sorted by a column
  (D14).
- AC-3.10 With manual sorting off, or on a non-premium instance, no move actions render at all.
- AC-3.11 The actions are reachable and operable by keyboard alone, and each announces its outcome to a
  screen reader (D18 — this is a stated reason for choosing buttons, so it is asserted, not assumed).

---

### US-04 — Move a Feature next to a specific other Feature

**As a** product owner **I want** to move a Feature directly above or below a named Feature **so that**
I can reprioritise across a long list without clicking Move Up two hundred times. · `job_id:
job-po-own-the-order-the-forecast-uses`, `job-po-order-features-when-the-connector-has-no-rank` · premium

#### Elevator Pitch
Before: relative moves only travel one row at a time, so on a real backlog the top and the bottom of the
list are effectively unreachable from each other.
After: choose **Move above…** from a row's action menu → a searchable picker of Features you can move
against → pick one → the Feature lands immediately above it.
Decision enabled: long-range reprioritisation — "this belongs ahead of the thing we start in Q3" —
which is the only form the decision actually takes on a backlog of any size.

**Acceptance criteria**
- AC-4.1 The picker lists Features from Portfolios the user can read, searchable by name and reference
  id, excluding Done Features (D15) and the Feature being moved.
- AC-4.2 Choosing a target places the moved Feature immediately above (or below) it, by the same
  insert-at-target rule as US-03 — asserted by a test that runs the same intended outcome through both a
  relative move and a targeted move and compares the resulting global order (D4).
- AC-4.3 Available regardless of the grid's current column sort (D14), unlike the relative moves.
- AC-4.4 Available from both surfaces (D10) with identical behaviour.
- AC-4.5 Choosing a target the user may not move *against* is allowed — the permission that matters is
  write on the **moved** Feature's Portfolios (D11), not on the target's. A test pins this, because it is
  the case a reviewer will otherwise assume was overlooked.
- AC-4.6 Same premium and RBAC gating as US-03 (AC-3.7, AC-3.8, AC-3.10).

---

### US-05 — Try it without a one-way door

**As a** config admin **I want** turning manual sorting off to give me my tracker's order back, with my
manual order still there if I turn it on again **so that** I can evaluate the feature on a real instance
without risking the ordering my forecasts already depend on. · `job_id:
job-config-admin-switch-ordering-ownership` · premium

#### Elevator Pitch
Before: "everything will be manually maintained" reads as irreversible, so the switch is a decision
nobody makes on a live instance.
After: on **Settings → System**, toggle Manual Sorting off → the Features view immediately shows the
tracker's order again, and the switch's help text says the manual order is kept; toggle it back on → the
manual order returns exactly as it was.
Decision enabled: whether to adopt manual ordering at all, evaluated by trying it rather than by
committing to it.

**Acceptance criteria**
- AC-5.1 Turning the switch off makes all five ordering call sites (S4) return the source order again,
  with no refresh required.
- AC-5.2 Manual ranks survive the off state — the column is not cleared.
- AC-5.3 Turning it on again restores the retained manual order and does **not** re-seed from source
  (D9), including for Features that arrived while it was off, which take end-of-list ranks (D7).
- AC-5.4 The off state removes every move action (AC-3.10) and reverts the `#` column header from
  "Manual" to the source label. The Features view itself remains reachable and read-only (D12).
- AC-5.5 The switch's help text states, in the instance's own terminology, what turning it off does and
  that the manual order is retained.

---

## Wave: DISCUSS / [REF] Story Map & Slices

**Backbone**: *see every Feature and the order that already drives my forecast* → *take ownership of that
order* → *nudge a Feature up it* → *move a Feature a long way in one step* → *hand ownership back*.

**Walking skeleton**: slice 02 is the skeleton — the first slice touching the whole spine (migration →
setting → comparer → all five read paths → UI label). Slice 01 precedes it because it needs none of that
and de-risks the epic's premise.

| Slice | Ships | Story | Est. | Learning hypothesis |
|---|---|---|---|---|
| **01** | The Features view: nav entry, route, RBAC-filtered list, position column on **both** surfaces, help text. Read-only, non-premium | US-01 | ~6h | Disproves "a global ranked list of Features is worth showing" — two ways it can fail: the source `Order` turns out to be mostly ties or blanks on real instances, so the column reports noise; or the flat list is unusable at real size, so the view needs grouping or search before it needs actions. Either failure reshapes the rest of the epic *and* Epic #4365's surface. |
| **02** | `ManualRank` + instance switch (on **and** off) + seeding + all five call sites honour it | US-02, US-05 | ~6h | Disproves "seeding from the current order is invisible to the user" if the seeded order differs from what was on screen — `FeatureComparer`'s int-before-string rule and `string.Compare` tie-breaking are applied at five call sites, two of which sort a *subset*. If it fails, D6 needs an explicit ordering snapshot rather than a re-derivation. |
| **03** | Relative move actions (Top / Up / Down / Bottom) + reorder endpoint + RBAC, on both surfaces | US-03 | ~6h | Disproves **D4** if a move made from a Portfolio's filtered list produces a global order the user finds surprising — the fallback is slot permutation, which changes only the rank service's body, so the endpoint, RBAC and menu all survive. |
| **04** | "Move above/below Feature X" picker | US-04 | ~4h | Disproves "relative moves are enough for the beginning" — if a fortnight of dogfooding shows people only ever use Move to Top, the picker is unnecessary and this slice is **dropped, not deferred**. It is the one slice designed to be cancellable. |

Full briefs: `docs/feature/epic-5375-manual-sorting/slices/slice-0{1..4}-*.md`.

**Prioritisation rationale** — 01 first because it carries zero dependencies, ships to every instance
licensed or not, and it is now also the surface Epic #4365 will land on (D17), so getting it wrong is
expensive twice over; its premise check is the cheapest way to find out whether the rest of the epic is
aimed correctly. 02 second because every later slice reads the rank it introduces, and because it is the
only slice that can ship and be left on safely by itself — an instance can run on frozen-but-uneditable
order indefinitely. 03 before 04 because it carries the epic's one genuine design unknown (D4 read
through a filtered view), and because 04's whole purpose is to be evaluated *after* people have lived
with 03. 04 last and optional by design.

### Slice taste tests

| Test | Verdict |
|---|---|
| Any slice shipping 4+ new components? | **Pass** — 01 adds a page, a nav entry and a shared column; 02 adds a switch panel; 03 adds a row-action menu plus one endpoint; 04 adds one dialog. |
| Every slice depending on a new abstraction? | **Pass** — the one shared abstraction (the insert-at-target rank service) ships **inside** slice 03, its first consumer, and slice 04 reuses it unchanged. Nothing is built ahead of its first consumer. |
| Does any slice disprove a pre-commitment? | **Pass** — 01 disproves the ranked-list premise, 02 seeding invisibility, 03 D4 itself, 04 the sufficiency of relative moves. Each failure changes the plan, and 04's failure deletes the slice. |
| Synthetic-data-only slices? | **Pass** — 01 and 02 are premise-checked against the dev instance's recorded history (`reference_dev_db_backup_restore`); 03 and 04 are dogfooded on a real multi-Portfolio instance the same day. Demo data is extended for the E2E, but no slice is *accepted* on demo data alone. |
| Two slices identical except for scale? | **Pass** — the earlier draft had this failure (a Portfolio drag slice and an instance-page drag slice). D10 and D18 dissolved it: both surfaces share one grid and one action menu, so 03 ships the gesture once for both, and 04 is a different gesture rather than the same one at larger scale. |

---

## Wave: DISCUSS / [REF] Scope Assessment

**PASS — right-sized.** 5 stories (≤10); ~22h total (<2 weeks); 4 slices, each ≤6h; 0 new external
integration points.

Honest note on the oversize heuristics: this feature touches **4 areas** — work-item sync/forecasting,
licensing + instance settings, authorization, and frontend — which trips the "≥3 bounded contexts"
signal on its own. It is not split, because only one of those four is where the behaviour lives
(ordering, inside the Features/forecast context); the other three are gates and surfaces it passes
through, each with an existing mechanism to reuse (S11, S12, S16). One signal firing is below the
"any 2+" split threshold.

Second-pass note: D17 adds a new top-level surface, which normally *grows* scope. Here it shrank the
plan — 4 slices instead of 5, and one fewer implementation of the same gesture — because D10 folded the
Portfolio surface into the shared grid and D18 removed the dependency decision that gated the largest
slice. The new view is also amortised across two epics rather than charged entirely to this one.

---

## Wave: DISCUSS / [REF] Outcome KPIs

| # | KPI | Target | Measurement |
|---|---|---|---|
| K1 | Seeding is order-preserving across every connector's `Order` shape | 100% of Features identical position pre/post enable, for int, LexoRank, inverted double, record number, and empty | Backend integration test parameterised over the five shapes (AC-2.1) + one dogfood enable on the dev instance's recorded data |
| K2 | Rank churn across sync with the switch on | **0** rank changes over 5 consecutive full refreshes | Backend integration assertion (AC-2.2) + dogfood over one week |
| K3 | A reorder actually moves the forecast | Moving a Feature into the top `FeatureWIP` positions changes its 85% date by ≥1 day on an instance with non-degenerate throughput | Integration test (AC-3.6). Note: constant-throughput test data cannot demonstrate this — the fixture must carry variable throughput, per the Epic 5459 lesson |
| K4 | Cross-surface agreement | All five ordering call sites (S4) return the same sequence in every state (on/off/mid-refresh) | Integration test asserting Portfolio DTO, features endpoint and forecast input agree (AC-2.3) |
| K5 | RBAC containment | A `PortfolioWrite`-on-A user cannot change the rank of any Feature outside A, and cannot move a Feature shared with a Portfolio they lack write on | 403 assertions per AC-3.7/3.8; no move affordance rendered |
| K6 | Move round trip on a realistic instance | ≤500ms p95 from action to persisted-and-rerendered, at 500 Features | Backend timing assertion on the block renumber (D13) + manual profile on the Features view (AC-1.9) |
| K7 | Are relative moves enough? | Over 2 weeks of dogfooding, the share of moves that are Move-to-Top vs multi-step Move-Up sequences | Direct observation. This is slice 04's go/no-go signal, not a pass/fail target — a high Move-to-Top share means the picker can be dropped |
| K8 | Adoption verdict | ≥1 pilot instance whose connector has no meaningful rank (ServiceNow or CSV) turns it on and is still on after 2 weeks | Benjamin's customer conversations. Cross-instance telemetry remains blocked on Epic 5015 (opt-in telemetry, no timeline), so this is qualitative by necessity, not by choice |

---

## Wave: DISCUSS / [REF] Out of Scope

- **Drag-and-drop reordering** (D18). Deferred, not planned. Revisit only if K7 shows the button actions
  are genuinely insufficient — and note it would then also need an accessible equivalent, which the
  buttons already are.
- **Anything from Epic #4365 "Dependencies".** This feature builds the surface #4365 will land on (D17)
  and nothing else. No dependency model, no dependency column, no graph. The only obligation carried here
  is not to build a Features view that would have to be rebuilt.
- **No write-back to the tracker** (D8). ADO StackRank, Jira Rank and Linear SortOrder are never written.
- **No work-item-level ordering** inside a Feature (D1).
- **No per-Portfolio independent orderings** (D3). "Scoped to the Portfolio" constrains what a move may
  *touch*, not how many orders exist.
- **No mixed mode** — no per-Feature or per-Portfolio opt-in while the instance switch is off (D2).
- **No badge, banner, count or digest for newly arrived Features** (D7), and no digest of what landed at
  the end since last time.
- **No rank reclamation for Done Features** (D15) — they are hidden, not un-ranked.
- **No CLI or MCP surface for reordering.** A set-rank command would be plumbing with no decision
  attached to its output.
- **No bulk operations** — no multi-select move, no CSV import of an order.
- **No ordering history or audit trail** of who moved what when.
- **No search, grouping, saved views or virtualised scrolling** on the Features view beyond what
  `FeatureListDataGrid` already provides. If slice 01's hypothesis fires, that changes — but as a
  re-plan, not as silent scope growth.
- **No change to `FeatureComparer`'s source-order semantics.** Its int/double/string rules stay exactly
  as they are for the switch-off path; the manual path is a separate comparison, not a rewrite.

---

## Wave: DISCUSS / [REF] WS Strategy

**Type B — parallel field behind a switch.** The manual rank is a new persisted field coexisting with
the untouched source `Order` (D5), selected by the instance switch (D2). Not Type A (a new column changes
the ordering five existing call sites depend on, so it is not purely additive) and not Type D (the switch
is a user-facing product setting, not an env-var implementation toggle — no `alternatives-considered`
trigger from this row).

Degradation is graceful at every partial state, and D12 improves this over the first draft: with slice 01
alone the instance gains a genuinely useful read-only view; with 02 and not 03 it runs on a frozen order,
which is a coherent product. There is no state in which the list and the forecast can disagree, because
D2 makes the selection instance-global and AC-2.3/K4 assert all five call sites together.

---

## Wave: DISCUSS / [REF] Driving Ports

| Port | Surface | Change |
|---|---|---|
| UI | **Top navigation** (`Header.tsx:59-60`) | Third entry, after Overview and System Settings, labelled by `getTerm` |
| UI | **`/features`** (new route) | The Features view: RBAC-filtered ranked list, read-only for everyone, actions by role + licence |
| UI | Portfolio → detail → **Features** tab | Position column + the same row-action menu, via the shared `FeatureListDataGrid` (S16). No new component |
| UI | **Settings → System** | Manual Sorting switch, premium-gated, `SystemAdmin`-guarded |
| HTTP (inbound) | `PATCH api/v1\|latest/features/{featureId}/rank` — body carries exactly one of `beforeFeatureId` / `afterFeatureId`; `beforeFeatureId: null` means "to the end" | **New write surface on a controller that has only ever exposed GETs (S13).** `[LicenseGuard(RequirePremium = true)]` + `PortfolioWrite` on every Portfolio the Feature belongs to (D11). Every move action in D18 reduces to this one shape |
| HTTP (inbound) | `GET api/v1\|latest/features` | New RBAC-filtered ranked list backing the Features view. Not premium-gated (D12) |
| HTTP (inbound) | The instance setting's existing settings endpoint | Gains the manual-sorting flag; enable path premium-guarded |
| CLI / MCP | — | **No change.** Explicitly out of scope; the clients call none of these routes |

---

## Wave: DISCUSS / [REF] Pre-requisites

- Premium licence infrastructure — **met** (S11); dev seed available (`reference_premium_license_dev_seed`).
- RBAC `PortfolioWrite` guard — **met** (S12).
- Shared Feature grid to extend rather than duplicate — **met** (S16).
- ~~A drag-and-drop mechanism~~ — **removed by D18.** This was the epic's only hard blocker; discrete
  actions need no new dependency and no MUI-X Pro licence.
- An instance with several Portfolios and at least one Feature shared between two of them, to dogfood D4
  and D11 — **confirmed absent** by the premise check: all 90 Portfolio-linked Features on the dev
  instance sit in exactly one Portfolio. AC-3.8 is reachable only through an integration test plus seeded
  demo data (owed in slice 01), and D11 ships field-unvalidated until a real shared Feature exists.
- An EF migration for the new column — **owed**, generated via the existing `CreateMigration` PowerShell
  script across all providers, expand-only (additive nullable column, nothing dropped).
- No dependency on Epic 5015 telemetry (K8 is qualitative by necessity).

---

## Wave: DISCUSS / [REF] DISCUSS Checklist (project standing rules)

**No silent N/A** — every item answered.

| Item | Answer |
|---|---|
| **RBAC impact** | **Substantial and new.** This is the first write surface on `FeaturesController` (S13), the first instance-wide Feature *read* surface, and the first case where one user's action moves an object another user's Portfolio forecasts against (S7). D11 sets the rule: the view opens for everyone, content is filtered by `PortfolioRead`, and mutation requires `PortfolioWrite` on **all** of a Feature's Portfolios. All gating flows through the existing `RbacGuardRequirement` mechanism server-side and `useRbac()` client-side — no component fetches `/authorization/my-summary` directly, and the disabled-action state derives from the same summary the guard enforces. Note for DESIGN: `GET /features` is the first endpoint whose *result set* is RBAC-filtered rather than RBAC-gated, which is a different shape from every existing guard. |
| **Lighthouse-Clients CLI/MCP versioning** | **Not owed.** The clients expose no ordering surface and call none of the new or changed routes; reordering is out of scope for them by decision, and no existing client response shape changes. If DESIGN adds a `rank` field to `FeatureDto` for the frontend's convenience, that is additive per `docs/concepts/api-versioning.md` and still needs no client bump — but it needs saying out loud at that point rather than discovered at release. |
| **Website marketing surface** | **In scope, deferred to DELIVER.** Premium-tagged epic, so `letpeople.work`'s premium/pricing surface is a candidate for a line item. Not verified during DISCUSS — the website lives in a separate repo (`project_website_hotlinks_docs_assets`) and no page was read. Owed check at DELIVER: does the premium feature list enumerate features individually, and if so does Manual Sorting belong on it? Confirm with the user before editing marketing copy. |
| **Per-feature docs + screenshots** | Owed at DELIVER, per feature, not batched into `/release`: a new docs page for the Features view (which Epic #4365 will extend rather than replace), a section on the move actions, a Settings note for the switch, and `@screenshot` E2Es for the Features view and the Portfolio list with actions. Watch the pixel-threshold trap — `rm` any regenerated PNG first. Docs wait for the user's confirmation in their own environment before being written. |
| **Demo data** | **Owed in slice 01.** The demo seeder must produce enough Features across enough Portfolios that the Features view is not trivially short, and — for AC-3.8 — at least one Feature shared between two Portfolios. Without the shared Feature, D11's most interesting case has no demo or screenshot representation. |
| **EF migrations** | **Owed in slice 02.** One additive nullable column on `Feature`, generated with the `CreateMigration` script across all supported providers. Expand-only: nothing dropped, nothing renamed. |
| **Terminology** | Every user-visible string resolves through `getTerm` (D16) — including the **nav entry**, which is the most visible instance of the rule in the product so far. No literal "Epic", "Initiative" or "Story" anywhere. |

---

## Wave: DISCUSS / [REF] Definition of Done (feature level)

1. All five stories' ACs green in CI (backend NUnit + frontend Vitest).
2. `pnpm test`, `pnpm build` (zero warnings), `dotnet build` (zero warnings), `dotnet test` green
   locally before push.
3. SonarQube Cloud: no new issues of any severity.
4. Mutation testing per feature: backend ≥80% kill on the rank service and comparer selection, frontend
   ≥80% on the Features view and the action menu.
5. One Playwright walking-skeleton assertion per surface — load the Features view, and perform one move
   from the Portfolio list — driven from demo data, run locally before commit, no re-seed to reach a
   second page.
6. Keyboard-only operation of every move action verified manually (AC-3.11).
7. Docs pages + regenerated screenshots, after the user confirms the behaviour in their environment.
8. Demo data seeds a multi-Portfolio set including one shared Feature.
9. EF migration generated by `CreateMigration` and verified against every provider.
10. ADO: Epic 5375 carries one Story per slice, states transitioned, "Release Notes" tag confirmed with
    the user before it is applied.
11. Evolution doc written and the feature workspace archived at finalize.

---

## Wave: DISCUSS / [REF] DoR Validation

| # | DoR item | Evidence |
|---|---|---|
| 1 | Business value articulated | 5 JTBD jobs with opportunity scores 2-5; the top gap (5) is a connector class — ServiceNow — that currently forecasts in record-number order with no workaround. Epic reported by a real user (Lorenzo). |
| 2 | Stories in LeanUX form with elevator pitches | US-01…US-05, each Before/After/Decision-enabled, each naming a real user-invocable entry point. **No `@infrastructure`-only slice**: slice 02 pairs the migration and the comparer change with US-02's observable outcome (the order stops moving across sync) and US-05's reversibility. |
| 3 | Acceptance criteria testable | 37 ACs, each asserting an observable — a rendered column value, a nav entry, a persisted rank, an HTTP status, a forecast date delta. |
| 4 | Dependencies identified | Pre-requisites section. The drag-mechanism blocker is **gone** (D18); one open item remains (a shared Feature to dogfood D11 against) and is flagged rather than assumed. |
| 5 | Job traceability | Every story carries a `job_id`; all five jobs are in `docs/product/jobs.yaml`. |
| 6 | Sized / sliceable | 4 slices, each ≤6h, each with a named learning hypothesis; all taste tests pass, including the two-slices-alike test that the first draft failed. |
| 7 | Technical feasibility grounded | S1-S16 read from code during this wave with file:line citations. The one real remaining unknown (D4 read through a filtered view) is isolated into slice 03 with a documented fallback. |
| 8 | Outcome KPIs measurable | K1-K8 with targets and methods; K7 is explicitly a decision signal rather than a pass/fail bar, and the telemetry limit on K8 is stated rather than hidden. |
| 9 | Out-of-scope explicit | 13 named non-goals, including the whole of Epic #4365. |

**Requirements completeness: 0.97** — up from 0.96 in the first pass, because D18 removed the dependency
decision and D10 removed a duplicate surface. The residual gap is whether D11's write-on-every-Portfolio
rule is too strict in real multi-team instances, which only field use can answer.

---

## Wave: DISCUSS / [REF] Expansion Menu Evaluation

`expansion_prompt = "ask-intelligent"` → all five triggers evaluated:

| Trigger | Fires? |
|---|---|
| AC ambiguity (≥2 stories with a contestable AC) | **No** — the one genuinely contestable rule (D4) is pinned by a worked numeric example in AC-3.2. |
| Cross-context complexity (≥3 contexts or technologies) | **YES** — forecasting, licensing/settings, authorization and frontend. Suggests `alternatives-considered`. |
| Multi-stakeholder (≥3 personas) | **YES** — `product-owner`, `config-admin`, `delivery-forecaster` carry distinct requirements (reorder / switch / consume). Suggests `persona-narrative`. |
| Compliance / regulatory terms in ACs | **No.** |
| WS strategy = D (configurable) | **No** — Type B. |

Two triggers fired; the scoped menu is offered to the user at wave end rather than auto-expanded (lean
mode). Telemetry: the nWave `scripts/shared/telemetry.py` helper is not present in this repo's install
(`~/.claude/skills/nw-discuss/` ships `SKILL.md` only), so the density event is recorded here rather
than hand-rolled into JSONL.

---

## Wave: DISCUSS / [REF] Handoff

**To**: `nw-solution-architect` (DESIGN) — full artifact set.
**And**: `nw-platform-architect` (DEVOPS) — KPI section only. No new infrastructure; K2, K4 and K6 are
the instrumentable ones.

**Open questions for DESIGN**:

1. **Where the switch is stored.** `OptionalFeature` (`OptionalFeatureKeys` — has a Settings UI already,
   but is framed as *preview/optional capability*, not *licensed setting*) versus an `AppSetting`.
   DISCUSS proposes `AppSetting` + `[LicenseGuard(RequirePremium = true)]` on the enable path, because
   the premium gate is the distinguishing property; ratify or overturn.
2. **Where the manual comparison lives.** `FeatureComparer` takes a mode, or a second
   `ManualRankComparer` is selected at each of the five call sites (S4). Whichever is chosen, K4 requires
   a **single** selection point — five independent `if` statements is the failure mode.
3. **Rank storage and renumbering (D13).** Dense int with a block UPDATE is proposed; confirm it holds
   at 500+ Features under K6's 500ms budget, and decide the transaction boundary so a concurrent refresh
   cannot interleave with a renumber.
4. **`GET /features` result-set filtering.** This is the first endpoint whose *rows* are RBAC-filtered
   rather than whose *access* is RBAC-gated. Decide where that filter lives so it cannot be bypassed by
   a future caller, and whether it belongs in the repository or the controller.
5. **D11's strictness.** "Write on every Portfolio the Feature belongs to" is this wave's proposal, not
   the user's words. Confirm it, and decide how the disabled-action tooltip names a blocking Portfolio
   the user may not have read access to.
6. **Seeding transaction.** Enabling the switch writes a rank for every Feature in the instance. Decide
   whether that is synchronous or queued, and what the UI shows while it runs on a large instance.
7. **Forward-compatibility with Epic #4365.** The Features view is being built as the surface
   dependencies will land on (D17). Without designing #4365, decide only this: does the row model and the
   view's layout leave room for a dependency indicator and a per-Feature detail affordance, so #4365 is
   an extension rather than a rebuild?

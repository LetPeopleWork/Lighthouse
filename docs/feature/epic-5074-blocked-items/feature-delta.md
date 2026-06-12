<!-- markdownlint-disable MD024 -->
# Epic 5074 — Blocked Items Improvements — DISCUSS Feature Delta

Feature-id: `epic-5074-blocked-items` | Wave: DISCUSS (lean mode, Tier-1 [REF] only) | Date: 2026-06-11
Persona grounding: `config-admin` (foundation), `flow-coach`, `delivery-lead-rte`, `product-owner` (all reused).

The carry-over seed `README.md` is retained as-is. This file holds all DISCUSS findings.

## Wave: DISCUSS / [REF] Reading Confirmation

- 9 mandatory skills loaded.
- Read: `README.md` (L1 locked), `jobs.yaml` (full), journeys `wait-states-flow-efficiency.yaml` +
  `time-in-state-and-staleness.yaml` (house style), personas `config-admin`/`flow-coach`/
  `delivery-lead-rte`, architecture `brief.md` (blocked mechanism, WaitStates-twin precedent,
  client version-gate rule).
- Verified: `BlockedOverviewWidget.tsx` is **non-premium** (no premium gate) → whole Epic non-premium.
- **Reconciliation**: No contradiction. README L1 (Lighthouse-side per-sync capture, own entity)
  governs slice 02; the user's scope steer (rule-based blocked = slice 1 foundation) governs ordering.
  Consistent.

## Wave: DISCUSS / [REF] Wave Decisions

- **D-SCOPE**: All five sub-capabilities IN scope for the Epic; delivered as 5 thin end-to-end slices,
  slice 1 (rule-based blocked) the foundation and walking skeleton. (User-locked.)
- **D-ENGINE**: Blocked reuses the existing `WorkItemRuleSet`/`RuleEvaluator<WorkItem>` (ADR-013) with
  **Include semantics** (matched = blocked) — third consumer after DeliveryRule (Include) and
  ForecastFilter (Exclude). No new engine, no new operators. (Brownfield-verified.)
- **D-MIGRATE**: Existing `BlockedStates`/`BlockedTags` auto-migrate to OR'd `State equals X` /
  `Tags contains Y` conditions. Hard AC + learning hypothesis on slice 1.
- **D-CAPTURE**: Blocked-time history is Lighthouse-side per-sync (README L1), new
  `WorkItemBlockedTransition` entity off the existing `WorkItemBlocked` event + NEW leave-detection.
  Not `WorkItemStateTransition`.
- **D-CHART**: Blocked-over-time uses the forward-only delivery-metrics history pattern (count snapshot),
  not per-item reconstruction.
- **D-STALE**: Blocked→stale is a DISTINCT trigger keyed on blocked DURATION, OR'd with time-in-state
  staleness, distinct reasons. New `blockedStalenessThresholdDays` (0 = disabled).
- **D-FLAGGED**: Jira "Is Flagged" synthetic-label HACK removed; flag expressed through a **net-new
  predefined (system-owned) additional field** that Lighthouse auto-registers for Jira connections —
  referenceable as a rule field key, fetched on sync, but excluded from the user-editable additional-fields
  list + slot limits, and not user-deletable/editable. No custom/special-case connector logic; the flag
  becomes "just another additional field," only system-managed. (Enabled by D-ENGINE.) Last slice. User
  decision 2026-06-11: predefined field, NOT a hand-configured user additional field.
- **D-PREMIUM**: Non-premium (twin of non-premium blocked-states config and time-in-state-and-staleness).
- **DIVERGE absent**: No DIVERGE artifacts (`diverge/recommendation.md`/`job-analysis.md`) exist —
  noted as a minor risk; JTBD run fresh here, grounded in the brownfield findings and README seed.
- **ADR numbers**: deferred to DESIGN (ADR-013 referenced only as the existing rule-engine precedent).

## Wave: DISCUSS / [REF] Scope Assessment (Elephant Carpaccio Gate)

Oversized-signal check: 5 user stories (≤10 ✓) | 1 bounded context — the rule engine + the
team/portfolio settings aggregate + the work-item read surfaces, all existing (≤3 ✓) | walking skeleton
(slice 1) needs the existing settings endpoint + rule evaluator + per-item badge + overview widget,
all in place (≤5 integration points ✓) | estimated ~5 days total, ≤1 day/slice (≤2 weeks ✓) | the
sub-capabilities ARE independent shippable outcomes, which is why they slice cleanly rather than a
single fat feature.

**Scope Assessment: PASS — 5 stories, 1 (existing) bounded context, estimated ~5 days.** Right-sized;
no split needed beyond the 5 user-intended slices. The slices are themselves the carpaccio.

## Wave: DISCUSS / [REF] Journey & Story Map

- Journey (SSOT): `docs/product/journeys/epic-5074-blocked-items.yaml` — two linked journeys
  (`define-blocked-rules` foundation + `read-blocked-signals` payoff), comprehensive depth (emotional
  arc + shared-artifact registry + error/failure paths + integration validation per step).
- Shared artifacts: `blockedRuleSet` (HIGH risk — single blocked definition; migration must DELETE old
  read paths), `blockedSince` (MEDIUM — sync-cadence approximate), `blockedCountSnapshot` (MEDIUM —
  forward-only). Registry embedded in the journey YAML.

### Story Map Backbone

| Define blocked | Capture blocked time | Trend over time | Escalate aged | Clean up flagged |
|---|---|---|---|---|
| Rule-based definition (S1) ← skeleton | Per-item duration (S2) | Blocked-count chart (S3) | Blocked→stale (S4) | Predefined flagged field (S5) |
| Auto-migrate old config (S1) | Enter/leave capture (S2) | Forward-only snapshot (S3) | Blocked-staleness threshold (S4) | Remove synthetic label (S5) |

**Walking skeleton** = S1: the thinnest slice connecting config-write → persistence → `IsBlocked` →
existing badge + overview widget. Confirmed (not refined) — it is the minimum end-to-end slice that
proves the rule-engine-reuse hypothesis and is the foundation every other slice derives from.

### Priority Rationale

Outcome-impact + dependency order. S1 first: walking skeleton AND riskiest assumption (does the rule
model replace the hardcoded mechanism without losing config?). S2 next: highest standalone value
(aged-blocker surfacing, a daily flow-coach pain) and the capture other slices read. S3 then S4 (both
build on S2's capture; S3's trend is systemic/lower-frequency, S4 refines an existing signal). S5 last:
config debt-retirement, only meaningful once the rule engine is the single definition. Value×Urgency/Effort
ties broken by Walking-Skeleton > Riskiest-Assumption > Highest-Value.

## Wave: DISCUSS / [REF] Coherence Validation

- **CLI/domain vocabulary**: "blocked", "blocked rule set", "blocked for N days", "blocked-stale",
  "rule condition", "Include semantics" — consistent with the existing delivery-rule / forecast-filter
  vocabulary (deliberate reuse). No new competing terms.
- **Emotional arc**: config journey Constrained→Familiar→Expressive (problem-relief into mastery);
  reading journey Blind→Informed→Decisive. No jarring transitions; each reading signal builds on the
  prior (duration → trend → stale).
- **Single source of truth**: ONE `IsBlocked` (RuleEvaluator over `blockedRuleSet`). Every downstream
  signal (badge, widget, count snapshot, blocked-staleness) derives from it. Integration risk: a
  leftover `BlockedStates`/`BlockedTags` read path → flagged as a slice-1 AC (grep-verifiable).
- **Horizontal integration**: S2/S3/S4 all consume S1's definition; S4 consumes S2's `blockedSince`;
  S5's predefined flagged field is referenced as an S1 `additionalField` rule key. No circular deps.

## Wave: DISCUSS / [REF] System Constraints (cross-cutting, apply to all stories)

- **RBAC**: All blocked CONFIG writes (rule set, blocked-staleness threshold) flow through the EXISTING
  team/portfolio settings authorization via `IRbacAdministrationService`; UI gating derived from
  `useRbac()` (`isTeamAdmin(id)` / `isPortfolioAdmin(id)`) — identical to today's blocked-states and
  delivery-rules config. No new authorization surface. All READ surfaces (per-item duration, over-time
  chart, blocked-stale) inherit the existing metric/work-item read gating. No component fetches
  authorization directly.
- **Lighthouse-Clients (CLI + MCP)**: The blocked config contract CHANGES (`blockedRuleSet` replaces
  `blockedStates`/`blockedTags` arrays on the team/portfolio settings DTO; S4 adds
  `blockedStalenessThresholdDays`; S2 adds a per-item blocked-duration read field; S3 adds a new
  over-time read endpoint). **Clients follow**: wrap the changed settings contract and the new endpoint;
  **version-gate** the new read endpoint and the changed contract (old server returns opaque 404 / lacks
  the field) — pre-check server version, fail with a clear "upgrade Lighthouse" error, pin
  `FEATURE_REQUIRES_SERVER_NEWER_THAN` strictly-newer-than the last released version (record the baseline
  at client-wrap time). Dev/unparseable versions never blocked. The clients already wrap delivery-rules,
  so the rule-set shape is familiar. **S5 additionally changes the additional-field/settings contract**:
  predefined (system-owned) additional fields now surface on the settings DTO as read-only entries
  distinct from user-managed ones (a net-new `IsPredefined`/`IsSystem` distinction). Clients reading or
  wrapping additional-field config must distinguish predefined from user-editable fields and must NOT
  treat predefined fields as user-deletable/editable; **version-gate** this contract change the same way.
- **Website**: **N/A — because** blocked items is non-premium (verified: `BlockedOverviewWidget` and
  blocked-states config have no premium gate; sibling time-in-state-and-staleness shipped non-premium).
  No premium-marketing surface to update. If DESIGN later premium-gates any blocked sub-capability, this
  N/A must be revisited.

## Wave: DISCUSS / [REF] User Stories

### US-01: Rule-based blocked definition (FOUNDATION / Walking Skeleton)

`job_id: job-config-admin-define-blocked-rules` | Slice 01 | MoSCoW: Must

#### Elevator Pitch
- **Before**: Carlos can only define "blocked" as a hardcoded state list + tag list; his real blocked
  concept (a Jira flag custom field) needs a synthetic-label hack.
- **After**: In Team settings → Flow Metrics Configuration, Carlos sees a **Blocked rule builder**
  (the same one as rule-based deliveries) pre-filled with his migrated config; he adds
  `Flagged isnotempty`, ORs it in, saves, and the overview Blocked widget count updates to include
  flag-only items.
- **Decision enabled**: define exactly what counts as blocked for this team — the definition every
  blocked signal downstream uses.

#### Problem
Carlos Mendez, Team Phoenix's config-admin, finds it impossible to express "blocked = our Blocked state
OR the Jira flag field" with the two hardcoded `BlockedStates`/`BlockedTags` lists, so he hacks it by
having Lighthouse inject a synthetic "Flagged" label he then adds to `BlockedTags`.

#### Who
- config-admin (team-admin / portfolio-admin) | editing Team/Portfolio Flow Metrics Configuration |
  motivated to make "blocked" match the team's real workflow without losing existing config.

#### Solution
Replace `BlockedStates`+`BlockedTags` with a `BlockedRuleSet` (existing `WorkItemRuleSet`, Include
semantics, reusing `RuleEvaluator<WorkItem>` and `DeliveryRuleBuilder`); auto-migrate existing config to
equivalent OR'd rule conditions; `IsBlocked` computes from the rule set.

#### Domain Examples
1. **Happy path** — Team Phoenix has `BlockedStates=["Blocked"]`, `BlockedTags=["impediment"]`. After
   migration the builder shows `State equals Blocked` OR `Tags contains impediment`; the same items read
   blocked as before.
2. **Custom field** — Carlos adds `additionalField.10001 (Flagged) isnotempty` ORed in; PHX-204, which
   has the flag set but no blocked state/tag, now reads blocked.
3. **Empty/boundary** — Team Atlas never configured blocked states/tags; migration yields an empty rule
   set; nothing is blocked (unchanged from before). Atlas later adds `State equals On Hold` and PHX… no,
   ATL-88 in "On Hold" reads blocked.

#### UAT Scenarios (BDD)
##### Scenario: Existing blocked config is preserved as rules
Given Team Phoenix has BlockedStates ["Blocked"] and BlockedTags ["impediment"]
When the blocked configuration is migrated to a rule set
Then the Blocked rule builder shows `State equals Blocked` OR `Tags contains impediment`
And every item that was blocked before the migration is still blocked after it

##### Scenario: A custom field defines blocked
Given Carlos has added the rule `Flagged isnotempty` to Team Phoenix's blocked rule set
And item PHX-204 has the Flagged field set and is in state "In Progress" with no blocked tag
When Lighthouse evaluates whether PHX-204 is blocked
Then PHX-204 reads as blocked on the team view and counts in the Blocked overview widget

##### Scenario: Blocked rules persist across reload
Given Carlos has saved a blocked rule set on Team Phoenix
When Carlos reloads the settings page
Then the same rule conditions are shown
And no part of the system still reads the old BlockedStates or BlockedTags lists

##### Scenario: A team with no blocked config blocks nothing
Given Team Atlas never configured blocked states or tags
When the blocked configuration is migrated
Then Team Atlas's blocked rule set is empty
And no Atlas item reads as blocked

#### Acceptance Criteria
- [ ] Existing `BlockedStates`/`BlockedTags` auto-migrate to an equivalent OR'd rule set; no item changes
      blocked status across the migration.
- [ ] `IsBlocked` is computed via `RuleEvaluator<WorkItem>` over `BlockedRuleSet` (Include semantics);
      no code path reads `BlockedStates`/`BlockedTags` after migration.
- [ ] The blocked rule builder reuses `DeliveryRuleBuilder`; field picker offers fixed fields +
      `additionalField.{id}`; the six existing operators; `MaxRules`/`MaxValueLength` enforced.
- [ ] Read-your-writes: saved rule conditions persist on reload.

#### Outcome KPIs
- **Who**: config-admins of teams whose blocked concept isn't a bare state/tag.
- **Does what**: define a blocked rule set using a custom field or compound condition.
- **By how much**: ≥30% of active teams that had blocked config defined adopt a non-trivial blocked rule
  (uses a field condition or AND/OR) within 8 weeks; **0 reported config losses** from migration.
- **Measured by**: settings telemetry on blocked-rule shape (where available) + support/issue reports.
- **Baseline**: 0 — rule-based blocked does not exist today; 100% of blocked config is state/tag lists.

#### Technical Notes
- Reuse ADR-013 engine; Include semantics (matched = blocked). Settings DTO contract change →
  version-gate clients (System Constraints). Lighthouse-side definition only (L1). RBAC: existing
  settings gate. Non-premium.

---

### US-02: See how long each item has been blocked

`job_id: job-flow-coach-see-how-long-blocked` | Slice 02 | MoSCoW: Must

#### Elevator Pitch
- **Before**: Priya sees WHICH items are blocked but not for how long; an aged blocker rots unnoticed.
- **After**: On /teams/{teamId} each blocked item shows **"blocked 9d"** (with an "Approximate — based
  on sync cadence" tooltip); first-observation items show "—".
- **Decision enabled**: which blockers to escalate in standup today, by age.

#### Problem
Priya Nair, Team Phoenix's flow coach, finds it slow and unreliable to judge how long an item has been
blocked — the indicator is a duration-less snapshot, so she must open each item in Jira to read its
history, and aged blockers get forgotten until a missed forecast.

#### Who
- flow-coach running standups/flow reviews | scanning the team work-item table | motivated to escalate
  the oldest blockers early.

#### Solution
Capture enter/leave-blocked per sync (`WorkItemBlockedTransition` off the existing `WorkItemBlocked`
event + new leave-detection, L1) and surface "blocked \<N>d" for the current spell.

#### Domain Examples
1. **Happy path** — PHX-204 becomes blocked on Monday's sync; by Wednesday the badge reads "blocked 2d".
2. **Unblock** — PHX-204 stops matching the blocked rule on Friday's sync; the badge clears.
3. **First-observation** — PHX-110 was already blocked when the feature shipped; it shows "—" until the
   next sync sets a baseline, then "blocked 0d".

#### UAT Scenarios (BDD)
##### Scenario: A newly blocked item shows its blocked duration
Given item PHX-204 was not blocked at the previous sync
And PHX-204 matches Team Phoenix's blocked rule set at the current sync
When two days pass and Priya opens the team view
Then PHX-204 shows "blocked 2d"
And the duration tooltip reads "Approximate — based on sync cadence"

##### Scenario: An unblocked item clears its blocked duration
Given PHX-204 has been showing "blocked 5d"
When PHX-204 no longer matches the blocked rule set at the next sync
Then PHX-204 no longer shows a blocked duration

##### Scenario: A first-observation blocked item establishes a baseline
Given PHX-110 is already blocked when the feature is first released
When the next sync runs
Then PHX-110 shows "blocked 0d" from that sync's timestamp rather than a fabricated history

#### Acceptance Criteria
- [ ] A blocked item shows "blocked \<N>d" for its current spell, derived from the captured `EnteredAt`.
- [ ] On unblock the spell closes (`LeftAt`) and the badge clears.
- [ ] First-observation items show "—" until the next sync; tooltip states sync-cadence approximation.
- [ ] `blockedSince` derives only from items `IsBlocked` per US-01's rule set (single definition).

#### Outcome KPIs
- **Who**: flow-coaches running flow reviews.
- **Does what**: escalate aged blockers from the duration read instead of discovering them late.
- **By how much**: median age of blockers AT escalation drops (target: aged blockers — blocked >7d —
  are surfaced in the next flow review rather than weeks later); proxy: blocked-duration tooltip/badge
  viewed in ≥50% of flow-review sessions on teams with blocked items.
- **Measured by**: view telemetry where available + qualitative coach feedback.
- **Baseline**: 0 — no blocked duration is surfaced today.

#### Technical Notes
- New capture entity, NOT `WorkItemStateTransition` (L1). Enter seam = existing `WorkItemBlocked` event;
  leave-detection is new. Sync-cadence resolution (approximate). Per-item read field → version-gate
  clients. Non-premium.

---

### US-03: See whether blocked count is trending up or down

`job_id: job-delivery-lead-see-blocked-trend` | Slice 03 | MoSCoW: Should

#### Elevator Pitch
- **Before**: The lead sees today's blocked count but not whether it's improving or worsening.
- **After**: A **Blocked Items over-time chart** in the **Flow Metrics chart area** (alongside the
  other flow-metrics charts) plots the in-scope blocked count per day; the lead reads "climbed from 3 to
  9 over three weeks"; empty state says "builds forward from today".
- **Decision enabled**: whether to invest in clearing blockers (are we accumulating faster than we
  clear?).

#### Problem
A Team Phoenix delivery lead finds there is no way to tell whether the team is getting better or worse
at clearing blockers — Lighthouse shows only the current count — so the improvement conversation runs on
a single snapshot or a hand-built spreadsheet.

#### Who
- delivery-lead-rte preparing a retro / leadership update | reading the team/portfolio flow surfaces |
  motivated to frame improvement around clear-rate trend.

#### Solution
A forward-only daily blocked-count snapshot (delivery-metrics history pattern) plotted as a time series,
composing with the existing filters.

#### Domain Examples
1. **Rising trend** — Team Phoenix's blocked count went 3 → 6 → 9 over three weeks; the chart shows the
   upward trend.
2. **Empty/new** — A team one day after release has no snapshots; the chart shows the forward-only empty
   state, not a flat zero.
3. **Filtered scope** — Filtering to work-item type "Bug" shows the blocked-count trend for bugs only.

#### UAT Scenarios (BDD)
##### Scenario: The blocked-count trend is visible over time
Given Team Phoenix's in-scope blocked count was recorded as 3, then 6, then 9 over three weeks
When the delivery lead opens the Blocked Items over-time chart for that window
Then the chart shows the blocked count rising from 3 to 9

##### Scenario: A new chart is honest about having no history yet
Given no blocked-count snapshots have been recorded yet for Team Atlas
When the delivery lead opens the Blocked Items over-time chart
Then the chart shows "blocked trend builds forward from today — no snapshots yet"
And it does not show a flat zero line

##### Scenario: The trend respects the active filter
Given Team Phoenix has blocked bugs and blocked stories
When the delivery lead filters the chart to work-item type "Bug"
Then the trend reflects only the blocked-bug count over the window

#### Acceptance Criteria
- [ ] The chart is presented in the Flow Metrics chart area (alongside the other flow-metrics charts),
      not folded into the overview widget.
- [ ] With snapshots present, the chart plots the daily in-scope blocked count over the window.
- [ ] With no snapshots, the forward-only empty state is shown (never a flat zero).
- [ ] The chart composes with the existing team/portfolio/type/date-range filter.
- [ ] The snapshot count equals the count of items `IsBlocked` per US-01's rule set at snapshot time.

#### Outcome KPIs
- **Who**: delivery-leads/RTEs.
- **Does what**: read the blocked-count trend to judge clear-rate.
- **By how much**: ≥25% of teams with recurring blocked items view the over-time chart within 8 weeks of
  the chart accumulating ≥14 days of snapshots.
- **Measured by**: chart view telemetry where available.
- **Baseline**: 0 — no over-time blocked view exists.

#### Technical Notes
- Forward-only (delivery-metrics pattern); NO retroactive reconstruction. New read endpoint →
  version-gate client wrapper. Non-premium.

---

### US-04: Treat an item as stale when it's been blocked too long

`job_id: job-flow-coach-stale-when-blocked-too-long` | Slice 04 | MoSCoW: Should

#### Elevator Pitch
- **Before**: Staleness fires only on time-in-state; a long-blocked item can slip past it.
- **After**: An item blocked longer than `blockedStalenessThresholdDays` shows the **stale** treatment
  with the reason **"stale: blocked 12 days"**, distinct from "stale: 11 days in Review".
- **Decision enabled**: which long-blocked items need a conversation now — caught by the same stale
  signal the coach already uses.

#### Problem
Priya finds that a long-blocked item can avoid her staleness signal entirely, because today's staleness
only watches time-in-current-state, so blocked rot escapes the one "needs a conversation" signal she
relies on.

#### Who
- flow-coach (+ config-admin setting the threshold) | reading the stale signal on team/portfolio views |
  motivated to catch blocked rot in the existing stale list.

#### Solution
A distinct blocked-duration staleness trigger (new `blockedStalenessThresholdDays`, 0 = disabled),
OR'd with time-in-state staleness, with distinct reasons; derives from US-02's `blockedSince`.

#### Domain Examples
1. **Blocked-stale** — With threshold 10, PHX-204 blocked 12 days reads stale, reason "stale: blocked
   12 days".
2. **Both reasons** — PHX-77 is blocked 12 days AND has been 13 days in Review; it reads stale once,
   listing both reasons.
3. **Disabled** — Team Atlas sets the threshold to 0; no item is blocked-stale; only time-in-state
   staleness applies.

#### UAT Scenarios (BDD)
##### Scenario: A long-blocked item becomes stale
Given Team Phoenix's blocked-staleness threshold is 10 days
And PHX-204 has been blocked for 12 days
When Priya reviews the team's stale items
Then PHX-204 reads as stale with the reason "stale: blocked 12 days"

##### Scenario: An item stale for two reasons is not double-counted
Given PHX-77 has been blocked 12 days and has spent 13 days in Review
And both staleness thresholds are exceeded
When Priya reviews the stale items
Then PHX-77 appears once as stale
And both reasons ("blocked 12 days" and "13 days in Review") are listed

##### Scenario: Blocked-staleness can be disabled
Given Team Atlas's blocked-staleness threshold is 0
And ATL-5 has been blocked for 30 days
When Priya reviews Team Atlas's stale items
Then ATL-5 is not flagged stale by blocked duration

#### Acceptance Criteria
- [ ] An item blocked longer than `blockedStalenessThresholdDays` (>0) reads stale with a blocked reason.
- [ ] Blocked-staleness OR'd with time-in-state staleness into ONE stale state; both reasons listed when
      both fire; never double-counted.
- [ ] Threshold 0 disables blocked-staleness.
- [ ] Blocked duration derives from US-02's `blockedSince` (single capture).

#### Outcome KPIs
- **Who**: flow-coaches.
- **Does what**: catch long-blocked items in the stale signal.
- **By how much**: on teams enabling the threshold, ≥1 net-new stale item per review is flagged by
  blocked-duration that time-in-state staleness missed (validates distinctness).
- **Measured by**: stale-reason breakdown telemetry where available + coach feedback.
- **Baseline**: 0 — blocked-duration staleness does not exist.

#### Technical Notes
- Distinct trigger from time-in-state staleness; reuses staleness treatment + threshold model. Ties into
  Epic #4144. Threshold is a settings contract change → version-gate clients. Non-premium.

---

### US-05: Jira flagged via a predefined (system) additional field

`job_id: job-config-admin-define-blocked-rules` | Slice 05 | MoSCoW: Could
(Same foundation job — this is config debt-retirement enabled by US-01, not a new value job.)

#### Elevator Pitch
- **Before**: Jira "Is Flagged" is a HACK — Lighthouse injects a synthetic "Flagged" label so it can be
  added to `BlockedTags`. It is opaque, special-cased, and unlike every other field.
- **After**: The Jira flag flows through a **predefined (system-owned) additional field** that Lighthouse
  **auto-registers** for Jira connections. Flagging an item in Jira makes it read blocked through a normal
  blocked-rule condition referencing that field — with **no synthetic label** and **no custom connector
  logic**. The predefined field appears in the rule field picker but NOT in the user's editable
  additional-fields list, and never consumes a user field slot.
- **Decision enabled**: mark items blocked from Jira by flagging them, using the same generic additional-
  field mechanism as everything else — the flag is "just another additional field," only system-managed.

#### Problem
Carlos finds the Jira "Is Flagged" handling opaque and special-cased: Lighthouse fabricates a "Flagged"
tag he must then wire into `BlockedTags`, a code path that exists only because the old mechanism couldn't
read a custom field directly. Asking him to instead hand-configure the flag as one of his own additional
fields would just move the hack — he'd have to know the connection-specific custom-field id and it would
eat one of his additional-field slots.

#### Who
- config-admin on a Jira-connected team | configuring blocked rules | motivated to express flagged the
  same generic way as every other blocked condition, without having to hand-wire Jira's internal flag
  field or spend an additional-field slot on it.

#### Solution
Introduce a **net-new predefined / system additional-field concept** (no `IsPredefined`/`IsSystem` flag
exists today; additional fields are a user-managed `List<AdditionalFieldDefinition>` on
`WorkTrackingSystemConnection`). Lighthouse **auto-registers** the Jira "Flagged" field
(`JiraFieldNames.FlaggedName` + the connection-specific flagged custom-field reference the connector
already resolves) as a predefined additional field for Jira connections — fetched on sync and offered as
a rule field key like any additional field, but **excluded from the user-editable list and from user
slot/limits, and not user-deletable/editable**. Remove the synthetic `"Flagged"` label injection
(`IssueFactory` / `JiraFieldNames.FlaggedName` label wiring) entirely; the flag now flows only through the
predefined field.

#### Domain Examples
1. **Predefined-field flagging** — Lighthouse auto-registers the predefined "Flagged" field on Team
   Phoenix's Jira connection. Carlos's blocked rule references it (e.g. `Flagged isnotempty`); Jira item
   PHX-300, flagged in Jira, reads blocked in Lighthouse with no "Flagged" label on its tags.
2. **Not in the user list** — In additional-fields settings, the predefined "Flagged" field does not
   appear among Carlos's addable/editable fields and cannot be deleted; his own 3 user fields are
   unaffected and he still has his full slot allowance.
3. **Cleanup** — After the slice, no code injects a synthetic "Flagged" label; the
   `JiraFieldNames.FlaggedName`-based label injection is gone; the flag is consumed only as the predefined
   additional field.

#### UAT Scenarios (BDD)
##### Scenario: A flagged Jira item reads blocked via the predefined flagged field
Given Lighthouse has auto-registered the predefined "Flagged" additional field on Team Phoenix's Jira connection
And Team Phoenix's blocked rule set references the predefined flagged field
And Jira item PHX-300 has the Flagged field set in Jira
When Lighthouse evaluates PHX-300
Then PHX-300 reads as blocked
And no synthetic "Flagged" label is present on PHX-300's tags

##### Scenario: The predefined flagged field is not user-customizable
Given the predefined "Flagged" additional field is registered on Team Phoenix's Jira connection
When Carlos opens the additional-fields settings
Then the predefined "Flagged" field is not listed among the fields he can add, edit, or delete
And it does not consume any of his additional-field slots
And it is still available as a selectable field key when building a blocked rule

##### Scenario: The synthetic flagged label is removed
Given the predefined-flagged slice has shipped
When the codebase is inspected
Then no code path injects a synthetic "Flagged" label
And the Jira flag is consumed only as the predefined additional field

#### Acceptance Criteria
- [ ] A Jira flagged item reads blocked via a blocked rule referencing the **predefined flagged additional
      field**, with no synthetic "Flagged" label on its tags.
- [ ] The predefined flagged field is **NOT listed among the user-addable/editable additional fields**,
      cannot be user-deleted or user-edited, and does not consume a user additional-field slot/limit.
- [ ] The predefined flagged field is fetched on sync like any additional field and is offered as a
      selectable rule field key.
- [ ] No code injects a synthetic "Flagged" label after the slice; grep confirms the
      `JiraFieldNames.FlaggedName`-based label injection is gone.

#### Outcome KPIs
- **Who**: config-admins on Jira-connected teams.
- **Does what**: express flagged through a system-managed predefined additional field (and flag items in
  Jira to mark them blocked) instead of via a synthetic-label hack.
- **By how much**: synthetic-label code path fully removed (binary); 100% of Jira connections expose the
  predefined flagged field with no per-connection hand-configuration and zero user-slot consumption.
- **Measured by**: code inspection + settings/contract verification.
- **Baseline**: synthetic "Flagged" label HACK exists today; no predefined/system additional-field concept
  exists.

#### Technical Notes
- Depends on US-01 (`additionalField` conditions as first-class rule keys). **Net-new predefined/system
  additional-field concept** (additive `IsPredefined`/`IsSystem` flag on the field-definition model);
  auto-registered for Jira connections; excluded from user-editable list + slot limits. Jira-only
  registration; the predefined-field concept itself is connector-agnostic. **Settings/additional-field
  contract change** (predefined fields surface read-only) → version-gate clients (see System Constraints).
  Slightly heavier than the other slices — SPIKE candidate if the additional-field list proves deeply
  user-CRUD-coupled (see slice-05 weight note). Non-premium.

## Wave: DISCUSS / [REF] Definition of Ready

| DoR Item | US-01 | US-02 | US-03 | US-04 | US-05 |
|---|---|---|---|---|---|
| 1. Problem in domain language | PASS | PASS | PASS | PASS | PASS |
| 2. Persona with characteristics | PASS (config-admin/Carlos) | PASS (flow-coach/Priya) | PASS (delivery-lead) | PASS (flow-coach/Priya) | PASS (config-admin/Carlos) |
| 3. 3+ domain examples, real data | PASS | PASS | PASS | PASS | PASS |
| 4. UAT G/W/T (3–7) | PASS (4) | PASS (3) | PASS (3) | PASS (3) | PASS (3) |
| 5. AC derived from UAT | PASS | PASS | PASS | PASS | PASS |
| 6. Right-sized (≤1d, ≤7 scen) | PASS | PASS | PASS | PASS | PASS |
| 7. Technical notes + 3 cross-cutting | PASS | PASS | PASS | PASS | PASS |
| 8. Dependencies tracked | PASS (none) | PASS (US-01) | PASS (US-01) | PASS (US-01,02) | PASS (US-01) |
| 9. Outcome KPIs measurable | PASS | PASS | PASS | PASS | PASS |

Plus mandatory gates: every story has a `job_id` (all trace to a `jobs.yaml` entry — none
`infrastructure-only`); every story has an Elevator Pitch (Before/After/Decision-enabled) referencing a
real user entry point. Cross-cutting RBAC / Clients / Website addressed for all (System Constraints +
per-slice).

### DoR Status: PASSED (9/9 + JTBD traceability + Elevator Pitch + cross-cutting, all 5 stories)

## Wave: DISCUSS / [REF] Risks & Open Questions

- **R1 (MEDIUM)**: No DIVERGE wave ran — JTBD derived fresh from brownfield + README. Mitigation: jobs
  grounded in verified code reality and the locked L1/scope decisions; reviewer to sanity-check job framing.
- **R2 (LOW)**: Sync-cadence resolution (L1) makes blocked duration approximate; short re-block spells
  collapse. Mitigation: tooltip discloses; accepted limitation.
- **R3 (MEDIUM, slice-05)**: US-05 introduces a **net-new predefined/system additional-field concept**
  (auto-registered, excluded from the user-editable list + slot limits). This raises slice-05's weight
  above the other four. Mitigation: it is plausibly ≤1 day IF the predefined flag is a thin additive
  field-definition flag and auto-registration is a single Jira-connection hook. **If brownfield shows the
  additional-field list is deeply user-CRUD-coupled (limits, ordering, persistence, settings surfaces),
  pull a pre-slice SPIKE** — "can a predefined additional field be excluded from the user-editable list +
  limits without per-connector special logic, and do the flag value semantics map to the existing
  operators?" — before committing slice 05. See slice-05 weight note.
- **OQ1 (for DESIGN, not blocking)**: Boundary semantics for blocked-staleness threshold (`>` vs `≥`) —
  proposed `≥` to mirror time-in-state's `>` consistency check; DESIGN confirms.
- **OQ2 — RESOLVED (user decision 2026-06-11): Flow Metrics chart.** The blocked-over-time chart lives
  in the **Flow Metrics chart area** (alongside the other flow-metrics charts), NOT folded into the
  overview widget area. Reflected in US-03 (elevator pitch + AC) and `slice-03`.

## Wave: DESIGN / [REF] Summary

Date: 2026-06-12 | Decider: Morgan (PROPOSE) | Density: LEAN (Tier-1 only). Full ADRs are the SSOT.

**Architecture summary**: No new bounded context — every capability EXTENDS an existing mechanism.
Blocked becomes the **third Include consumer** of the existing rule engine (ADR-012/013): a
`WorkItemRuleSet` stored as a JSON column on the shared `WorkTrackingSystemOptionsOwner` aggregate
(Team + Portfolio), evaluated once via `RuleEvaluator<T>` (matched = blocked). Per-item duration is a
new owned `WorkItemBlockedTransition` capture (enter via the existing `WorkItemBlocked` event, leave
via a new `WorkItemUnblocked` event). The over-time trend reuses the forward-only delivery-metrics
snapshot PATTERN as a new owner-grained sibling store. Blocked→stale AMENDS ADR-026 (blocked-exclusion
narrowed to the time-in-state trigger; a distinct blocked-duration trigger OR's in). The Jira flag
becomes a predefined (system-owned) additional field via the generic id-keyed path (SPIKE gated).
Default = modular monolith + ports-and-adapters (unchanged); no style change; no premium gate.

### [REF] DDD Decisions (verdicts + one-line rationale)

| ID | Decision | Verdict | Rationale | ADR |
|---|---|---|---|---|
| DDD-1 | Blocked definition + storage placement | `BlockedRuleSetJson` (WorkItemRuleSet) JSON column on `WorkTrackingSystemOptionsOwner` (Team+Portfolio); single `IsBlocked` via `IBlockedItemService`→`RuleEvaluator<T>` (Include); legacy `BlockedStates`/`BlockedTags` REMOVED | **DECIDED** | JSON column is the EXISTING rule-set idiom (`ForecastFilterRuleSetJson`/`RuleDefinitionJson`), not owned-collection (ADR-064 is for structured non-rule config); one definition by construction once legacy columns dropped | ADR-067 |
| DDD-2 | Auto-migration of existing config | App-layer + EF backfill-before-drop; each `BlockedStates`→`State equals X`, each `BlockedTags`→`Tags contains Y`, OR'd; idempotent (null-guard, no `=true` sentinel) | **DECIDED** | Loss-free proven by real-provider pre/post blocked-status equality test; pure SQL can't reuse the C# rule synthesis | ADR-067 |
| DDD-3 | Blocked-transition capture entity | New owned `WorkItemBlockedTransition {WorkItemId, EnteredAt, LeftAt?}`; enter=existing `WorkItemBlocked`, leave=NEW `WorkItemUnblocked` domain event; `blockedSince` = open spell's `EnteredAt`; first-observation = null = "—" | **DECIDED** | Symmetric enter/leave on the bus (memory: prefer domain events); orthogonal to state (not `WorkItemStateTransition`); current-spell (D6); honest no-fabrication | ADR-068 |
| DDD-4 | Blocked-over-time count snapshot | NEW owner-grained sibling `BlockedCountSnapshot {OwnerId, OwnerType, RecordedAt, BlockedCount}`; forward recorder post-sync; date-keyed upsert; NEW `blockedCountHistory` read endpoint | **DECIDED** | Grain differs from delivery-keyed `DeliveryMetricSnapshot` (owner not delivery); forward-only PATTERN reused; honest empty state | ADR-069 |
| DDD-5 | Blocked→stale vs ADR-026 (CRITICAL) | `deriveStaleness` returns `StalenessResult {isStale, reasons[]}`; time-in-state KEEPS `!isBlocked` guard; NEW blocked-duration trigger (`≥ blockedStalenessThresholdDays`, 0=off) OR's in with distinct reason; stale-once | **DECIDED** | AMENDS ADR-026 (exclusion narrowed to time-in-state); single-selector invariant upheld + extended; OQ1 resolved (`≥` blocked, `>` time-in-state) | ADR-070 |
| DDD-6 | Predefined/system additional field | Additive `IsPredefined` flag in the SAME list; auto-registered for Jira; excluded from user CRUD + slot count; generic id-keyed value/rule path; synthetic-label hack deleted | **DECIDED + SPIKE** | Value/fetch/rule paths already generic; exclusion threads 4 surfaces + no system-field precedent ⇒ pre-slice-05 SPIKE | ADR-071 |
| DDD-7 | Settings/contract + client version-gate | Matrix: changed settings contract (#1) GATE, `blockedSince` (#2) no gate, new endpoint (#3) GATE, `blockedStalenessThresholdDays` (#4) no gate, `IsPredefined` write (#5) GATE; baseline strictly `> v26.6.7.1` | **DECIDED** | Additive ⇒ no gate (ADR-062); changed/new ⇒ gate (loud "upgrade" beats silent divergence); last released = v26.6.7.1 | ADR-072 |

### [REF] Component Decomposition

| Component | EXTEND / CREATE | Surface | Owner |
|---|---|---|---|
| `IBlockedItemService` (`IsBlocked(WorkItem,Team)` / `IsBlocked(Feature,Portfolio)`) | CREATE NEW (thin delegator, mirrors `ForecastFilterRuleService`) | backend service | ADR-067 |
| `RuleEvaluator<T>`, `WorkItemFieldProvider`, `FeatureFieldProvider`, `WorkItemRuleSet`, schema | EXTEND (reused unchanged) | backend rule engine | ADR-067 |
| `BlockedRuleSetJson` column on `WorkTrackingSystemOptionsOwner` | EXTEND (new column on existing aggregate) | persistence | ADR-067 |
| `WorkItemBlockedTransition` owned entity + 2 handlers | CREATE NEW (mirrors `WorkItemStateTransition`) | capture | ADR-068 |
| `WorkItemUnblocked` domain event + dispatch seam | CREATE NEW (symmetric to `WorkItemBlocked`) | event bus | ADR-068 |
| `BlockedCountSnapshot` store + repo + recorder + endpoint | CREATE NEW (forward-only pattern reused) | snapshot/read | ADR-069 |
| `deriveStaleness` selector (boolean → `StalenessResult`) | EXTEND (return type widened; 3 call sites) | FE staleness | ADR-070 |
| `blockedStalenessThresholdDays` settings field | EXTEND (twin of `StalenessThresholdDays`) | settings | ADR-070 |
| `IsPredefined` flag on `AdditionalFieldDefinition` + Jira auto-reg + CRUD/slot exclusion | EXTEND (additive flag) / CREATE (registration hook) | additional fields | ADR-071 |
| `DeliveryRuleBuilder` (blocked rule builder UI) | EXTEND (third consumer; ForecastFilterEditor is the precedent) | FE config | ADR-067 |
| `BlockedOverviewWidget`, blocked badge, `WorkItemDto.IsBlocked` | EXTEND (consume rule-derived `IsBlocked` unchanged) | FE read | ADR-067 |

### [REF] Driving / Driven Ports

- **Driving** (inbound): team/portfolio settings PUT (blocked rule validation, blocked-staleness threshold) via `TeamController`/`PortfolioController` (existing, extended); `GET blockedCountHistory` (new); the per-sync work-item pipeline (existing) emitting `WorkItemBlocked`/`WorkItemUnblocked`.
- **Driven** (outbound): `IBlockedCountSnapshotRepository : IRepository<BlockedCountSnapshot>` (new); existing `IRepository<Team/Portfolio/WorkItem>`; the domain-event dispatcher (existing); Jira/ADO connectors (existing additional-field fetch, generic).
- **Core (no I/O)**: `RuleEvaluator<T>` (pure, ADR-012 purity invariant), `IBlockedItemService` (composes evaluator + provider), `deriveStaleness` (pure FE selector, ADR-026/070).

### [REF] Technology Choices

All existing — zero new dependencies (OSS-first satisfied by reuse). Backend C# .NET 8, EF (migrations via the `CreateMigration` PowerShell script, all providers; real-provider read-your-writes tests — InMemory misses migrations). FE React 18 + TS strict; **Zod at the changed settings boundary + the new `blockedCountHistory` boundary** (rolling-adoption rule — DELIVER constraint). Architectural enforcement: ArchUnitNET (backend: single `IsBlocked` path, evaluator purity, `WorkItemBlockedTransition` distinctness); grep/Vitest (FE single-selector, no method-presence enforcement in TS — same limitation noted in ADR-026).

### [REF] Reuse Analysis (HARD GATE)

| Existing component | Path | Verdict | Justification |
|---|---|---|---|
| Rule engine (`RuleEvaluator<T>`, providers, `WorkItemRuleSet`, schema, 6 operators) | `Backend/.../Models/WorkItemRules/`, `.../Services/.../WorkItemRules/` | **EXTEND** | Third Include consumer; no new engine/operators (D-ENGINE) |
| Rule-set JSON persistence idiom | `Team.ForecastFilterRuleSetJson` (L19), `Delivery.RuleDefinitionJson` (L47) | **EXTEND** | `BlockedRuleSetJson` reuses the exact JSON-column idiom; ADR-013 canary protects shape |
| `WorkTrackingSystemOptionsOwner` settings aggregate (Team+Portfolio base) | `Backend/.../Models/WorkTrackingSystemOptionsOwner.cs` | **EXTEND** | New columns (`BlockedRuleSetJson`, `blockedStalenessThresholdDays`); removed columns (`BlockedStates`/`BlockedTags`) |
| `WorkItemBlocked` event + `WasBlocked` seam | `Backend/.../Events/WorkItemBlocked.cs`, `WorkItemService.cs` L103/L121 | **EXTEND** | Enter seam reused; symmetric `WorkItemUnblocked` added |
| `WorkItemStateTransition` capture idiom | ADR-015/016/017 | **EXTEND (idiom)** / **CREATE (entity)** | New `WorkItemBlockedTransition` mirrors the owned-collection + capture-dispatch idiom; distinct entity (blocked ⊥ state, README L1) |
| Delivery-metrics forward-snapshot pattern | ADR-048/049/050 | **EXTEND (pattern)** / **CREATE (store)** | Forward recorder + date-keyed upsert + honest-empty PATTERN reused; new owner-grained store (grain differs from delivery) |
| `deriveStaleness` selector + 3 surfaces | `Frontend/src/utils/staleness/deriveStaleness.ts` (+ TimeInStateBadge, StaleOverviewWidget, WorkItemAgingChart) | **EXTEND** | Return type widened to `StalenessResult`; ADR-026 single-selector invariant upheld |
| `DeliveryRuleBuilder` + `ForecastFilterEditor` reuse precedent | `Frontend/src/components/Common/DeliveryRuleBuilder/`, `.../Teams/ForecastFilterEditor/` | **EXTEND** | Third UI consumer (fetch schema → pass to builder), exact ForecastFilterEditor pattern; replaces the two `ItemListManager` blocked lists |
| `BlockedOverviewWidget`, blocked badge | `Frontend/src/pages/Common/MetricsView/BlockedOverviewWidget.tsx`, `BaseMetricsView.tsx` | **EXTEND** | Consume rule-derived `IsBlocked` (unchanged bool); add per-item `blockedSince` badge |
| Additional-field mechanism (generic id-keyed fetch/value/schema/provider) | `AdditionalFieldDefinition`, connectors, `WorkItemFieldProvider`, `AdditionalFieldsHelper` | **EXTEND** | `IsPredefined` additive flag rides the generic path; Jira auto-registration hook (new, single) |
| RBAC (`IRbacAdministrationService`, `useRbac()`) | existing settings authorization | **EXTEND (reuse)** | All blocked config writes ride the existing team/portfolio settings gate; no new authorization surface |

No CREATE-NEW without an EXTEND attempt first. The two genuinely-new entities (`WorkItemBlockedTransition`, `BlockedCountSnapshot`) and the new event (`WorkItemUnblocked`) each reuse an established idiom/pattern; extending an existing entity was rejected with evidence in their ADRs.

### [REF] ADR-026 Reconciliation (the critical one)

ADR-026 locked "a blocked item must NOT also be flagged stale" via `&& !item.isBlocked` in `deriveStaleness`. ADR-070 AMENDS this: the rule is NARROWED from "staleness" to "TIME-IN-STATE staleness" (the original premise had only one staleness source). PRESERVED: blocked excludes time-in-state stale (clock paused). ADDED: a distinct blocked-DURATION trigger that fires *because* the item is blocked too long, OR'd into one `StalenessResult` with a distinct reason, stale-once. The single-selector architecture is upheld and extended; only the exclusion's scope changes. ADR-070 carries a `## Changed Assumptions` section quoting ADR-026 verbatim.

### [REF] Slice-05 SPIKE Verdict

**VERDICT: pre-slice-05 SPIKE REQUIRED (timeboxed ~half-day), NOT a thin additive slice.** Evidence: the value/fetch/rule paths are fully GENERIC (favourable, cheap) BUT the predefined-field exclusion threads through FOUR surfaces (CRUD reconcile `UpdateAdditionalFieldDefinitions`, license slot gate `SupportsAdditionalFields`, DTO user-list/rule-picker split, connector auto-registration) and there is NO existing system-registered-field precedent. SPIKE must answer: reconcile-merge (no silent delete on user PUT), slot-count split (user vs total), `WriteBackMappingDefinition` compatibility + `Reference` immutability, single idempotent Jira hook, FE DTO split. Does NOT block slices 01–04 (slice 05 = MoSCoW Could, last). Detail: ADR-071 §5, upstream-changes UC-3.

### [REF] Decisions Table

| DDD | ADR | Status |
|---|---|---|
| DDD-1 rule-based blocked + storage + migration | ADR-067 | Accepted |
| DDD-3 blocked-transition capture + `WorkItemUnblocked` | ADR-068 | Accepted |
| DDD-4 blocked-count snapshot + endpoint | ADR-069 | Accepted |
| DDD-5 blocked→stale AMENDS ADR-026 | ADR-070 | Accepted |
| DDD-6 predefined/system additional field + SPIKE | ADR-071 | Accepted |
| DDD-7 contract changes + client version-gate matrix | ADR-072 | Accepted |
| DDD-2 (migration) | folded into ADR-067 | Accepted |

### [REF] Constraints (carried to DELIVER)

- **RBAC** (unchanged): all blocked CONFIG writes ride the existing team/portfolio settings gate (`IRbacAdministrationService`, UI via `useRbac()` `isTeamAdmin`/`isPortfolioAdmin`); reads inherit existing metric/work-item read gating. No new authorization surface.
- **Non-premium** (verified): no premium gate anywhere in this epic. **Website N/A** (non-premium).
- **EF migrations** via the `CreateMigration` PowerShell script (all providers); real-provider read-your-writes tests required (InMemory misses migrations); mind the `--no-incremental` stale-DLL trap.
- **Zod** at the changed settings boundary + the new `blockedCountHistory` boundary (rolling-adoption rule).
- **CI**: immutability (records/ImmutableList), Nullable enable + TreatWarningsAsErrors, TS strict; consult `docs/ci-learnings.md` rule families (CA1859, S2325, NUnit4002…) when coding.
- **Architectural enforcement**: ArchUnitNET (single `IsBlocked` path, evaluator purity, transition-entity distinctness); grep/Vitest for FE single-selector.

### [REF] Open Questions (deferred to DISTILL / DELIVER)

- **UC-1** (slice-04 AC2): RESOLVED at DESIGN (ADR-070) — `blocked-duration` is the stale driver, days-in-state is a `context-time-in-state` entry; stale-once. Only the AC2 Gherkin wording is for the acceptance-designer at DISTILL. (Not a product decision.)
- **UC-2** (slice-03 AC3): historical per-TYPE blocked-count filtering deferred (total-count snapshot; per-type is an additive follow-up). (DISTILL — delivery-lead.)
- **UC-3** (slice-05): pre-slice-05 SPIKE scheduled (does not block slices 01–04). (DELIVER — software-crafter.)

All three recorded in `design/upstream-changes.md`. None block the DESIGN handoff.

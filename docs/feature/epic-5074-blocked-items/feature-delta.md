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
- **Migration-testing gate**: every slice touching schema (01–04) MUST ship a NUnit integration test exercising real-provider migrations (SQLite + Postgres) generated via the `CreateMigration` PowerShell script. InMemory-only migration coverage FAILS the gate.
- **Zod** at the changed settings boundary + the new `blockedCountHistory` boundary (rolling-adoption rule).
- **CI**: immutability (records/ImmutableList), Nullable enable + TreatWarningsAsErrors, TS strict; consult `docs/ci-learnings.md` rule families (CA1859, S2325, NUnit4002…) when coding.
- **Architectural enforcement**: ArchUnitNET (single `IsBlocked` path, evaluator purity, transition-entity distinctness); grep/Vitest for FE single-selector.

### [REF] Open Questions (deferred to DISTILL / DELIVER)

- **UC-1** (slice-04 AC2): RESOLVED at DESIGN (ADR-070) — `blocked-duration` is the stale driver, days-in-state is a `context-time-in-state` entry; stale-once. Only the AC2 Gherkin wording is for the acceptance-designer at DISTILL. (Not a product decision.)
- **UC-2** (slice-03 AC3): historical per-TYPE blocked-count filtering deferred (total-count snapshot; per-type is an additive follow-up). (DISTILL — delivery-lead.)
- **UC-3** (slice-05): pre-slice-05 SPIKE scheduled (does not block slices 01–04). (DELIVER — software-crafter.)

All three recorded in `design/upstream-changes.md`. None block the DESIGN handoff.

## Wave: DISTILL

Date: 2026-07-03 | Author: Quinn (acceptance-designer) | Density: LEAN (Tier-1 [REF] only) | Scope: slices 01–04 (slice-05 SKIPPED — pre-slice-05 SPIKE gate, ADR-071).

### [REF] Inherited commitments

| Origin | Commitment | DDR | Impact |
|--------|------------|-----|--------|
| DISCUSS#D-ENGINE / DESIGN#DDD-1 | Blocked = third Include consumer of `RuleEvaluator<WorkItem>` over `blockedRuleSetJson`; legacy `BlockedStates`/`BlockedTags` removed | ADR-067 | Acceptance suite drives `blockedRuleSetJson` at the team settings port; single-`IsBlocked` asserted at the WIP read port |
| DISCUSS#D-CAPTURE / DESIGN#DDD-3 | Per-item blocked duration via `WorkItemBlockedTransition` (`blockedSince`) | ADR-068 | Slice-02 ATs assert `blockedSince` on the WIP read surface (present-even-when-null contract) |
| DISCUSS#D-CHART / DESIGN#DDD-4 | Forward-only daily blocked-count via new `blockedCountHistory` read endpoint | ADR-069 | Slice-03 ATs drive the new endpoint; honest empty-state (empty array, never flat zero) |
| DISCUSS#D-STALE / DESIGN#DDD-5 | Distinct blocked-duration staleness trigger; new `blockedStalenessThresholdDays` (0=off, `≥`) | ADR-070 | Slice-04 ATs drive the threshold settings contract; stale rendering (driver+context) is FE Vitest in DELIVER |
| DESIGN#UC-2 | Per-TYPE historical blocked-count filtering deferred (total-count snapshot is forward-only) | ADR-069 | Slice-03 per-type scenario authored but `@deferred` — NOT enabled in DELIVER (see `distill/upstream-issues.md`) |

### [REF] Wave-Decision Reconciliation HARD GATE

**Result: PASS — 0 contradictions.** DISCUSS decisions (D-ENGINE/MIGRATE/CAPTURE/CHART/STALE/PREMIUM) and DESIGN decisions (DDD-1..7, ADR-067..072) are consistent. OQ1 (`≥` blocked vs `>` time-in-state) and OQ2 (chart in Flow Metrics area) resolved and reflected. UC-1/UC-2 are DISTILL-wording/scope confirmations (applied), not contradictions. UC-3/slice-05 excluded from scope.

### [REF] Scenario list with tags

21 backend acceptance scenarios (NUnit + `WebApplicationFactory<Program>`, black-box HTTP/JSON). ONE walking skeleton GREEN; 20 `[Ignore]`-pending (one-at-a-time in DELIVER). Full per-scenario RED classification in `distill/red-classification.md`.

| Slice | Scenario | Tags | Status |
|---|---|---|---|
| 01 | An_admin_saves_a_teams_blocked_definition_and_reads_it_back | @walking_skeleton @driving_port @real-io @us-01 | GREEN |
| 01 | Existing_blocked_config_is_preserved_as_equivalent_rules | @driving_port @migration @us-01 | RED |
| 01 | A_custom_field_condition_makes_an_item_read_blocked | @driving_port @us-01 | RED |
| 01 | Saved_blocked_rules_persist_across_reload | @driving_port @us-01 | RED |
| 01 | An_item_matching_the_blocked_rules_reads_blocked_everywhere | @property @us-01 | RED |
| 01 | A_team_with_no_blocked_config_blocks_nothing | @edge @us-01 | RED |
| 01 | A_blocked_rule_set_exceeding_the_maximum_conditions_is_rejected | @error @us-01 | RED |
| 01 | A_blocked_rule_referencing_an_unknown_field_is_rejected | @error @us-01 | RED |
| 01 | A_non_admin_cannot_change_the_blocked_definition | @error @rbac @us-01 | PASS-WHEN-ENABLED |
| 02 | A_newly_blocked_item_shows_how_long_it_has_been_blocked | @driving_port @us-02 | RED |
| 02 | An_unblocked_item_no_longer_shows_a_blocked_duration | @edge @us-02 | RED |
| 02 | A_first_observation_blocked_item_shows_no_duration_until_a_baseline_exists | @edge @us-02 | RED |
| 02 | An_item_that_does_not_match_the_blocked_rules_has_no_blocked_duration | @property @us-02 | RED |
| 03 | The_blocked_count_trend_is_available_over_time | @driving_port @us-03 | RED |
| 03 | A_new_team_sees_an_honest_empty_trend | @edge @us-03 | RED |
| 03 | The_blocked_trend_can_be_filtered_to_a_single_work_item_type | @deferred @us-03 | RED / DEFERRED (UC-2) |
| 04 | The_blocked_staleness_threshold_is_saved_and_read_back | @driving_port @us-04 | RED |
| 04 | A_new_team_defaults_the_blocked_staleness_threshold_to_zero | @edge @us-04 | RED |
| 04 | A_blocked_staleness_threshold_below_range_is_rejected | @error @us-04 | RED |
| 04 | A_blocked_staleness_threshold_above_range_is_rejected | @error @us-04 | RED |
| 04 | A_non_admin_cannot_change_the_blocked_staleness_threshold | @error @rbac @us-04 | PASS-WHEN-ENABLED |

Non-happy-path (error/edge/property/deferred) = 14/21 ≈ **67%** (target ≥40% ✓).

### [REF] WS strategy (Architecture of Reference)

Per project `docs/architecture/atdd-infrastructure-policy.md` — the Python-pilot per-feature A/B/C/D strategy is retired. Driving port (HTTP) = real `WebApplicationFactory<Program>`; driven-internal (EF `LighthouseAppContext`, repositories) = real via the test factory with `EnsureDeleted`/`EnsureCreated`; driven-external (`ILicenseService`) = `Mock`. ONE walking skeleton (`@walking_skeleton @driving_port @real-io`) closes config-write → persist → settings-read through the production composition root and is GREEN today (proves the wiring the whole epic extends).

### [REF] Driving-adapter coverage

| Driving port (DESIGN) | Exercised by | Verb |
|---|---|---|
| Team settings PUT (`/api/latest/teams/{id}`) | slices 01, 04 | real HTTP PUT (typed DTO + raw-JSON member for not-yet-typed fields) |
| Team settings GET (`/api/latest/teams/{id}/settings`) | slices 01, 04 | real HTTP GET + JSON assertions |
| Team metrics WIP read (`/api/latest/teams/{id}/metrics/wip`) | slices 01, 02 | real HTTP GET — `WorkItemDto.isBlocked` / `blockedSince` |
| Team metrics blocked-count history (NEW `…/metrics/blockedCountHistory`) | slice 03 | real HTTP GET — currently 404→SPA (RED) |
| RBAC guard (TeamWrite) on the extended contract | slices 01, 04 | `AsViewer` → 403 |

### [REF] Driven-adapter coverage (Mandate 6)

| Adapter | @real-io scenario | Covered by |
|---|---|---|
| EF `LighthouseAppContext` + `IRepository<Team>` / `IWorkItemRepository` | YES | Every scenario seeds + reads through real EF (Sqlite in CI) |
| `ILicenseService` (external) | Faked | `Mock<ILicenseService>` (premium=true) via `RemoveAll`+`AddScoped` |
| Jira/ADO connector (`IWorkTrackingConnector`) | N/A this slice-set | Blocked evaluation is Lighthouse-side (L1); connector fetch faked by seeding synced items directly |

### [REF] Scaffolds

**None created — and none required.** Per `atdd-infrastructure-policy.md`, this project excludes the Python `__SCAFFOLD__` stub mechanism. Black-box HTTP/JSON ATs compile against today's `Program` + DTOs (verified: `dotnet build` → 0 errors) and fail on missing behaviour (assertion RED), never on a missing type (compile BROKEN). Not-yet-typed contract members (`blockedRuleSetJson`, `blockedStalenessThresholdDays`) are attached at the JSON layer via `BlockedItemsJson.WithBlockedRuleSet` / `WithBlockedStalenessThreshold`, so the tests are RED-ready without production stubs.

### [REF] Test placement

`Lighthouse.Backend/Lighthouse.Backend.Tests/API/Integration/BlockedItems/` — sibling of the existing `API/Integration/` fixtures (`ForecastFilterTeamSettingsIntegrationTest`, `TimeInStateReadApiIntegrationTest`, `PortfolioStalenessThresholdSettingsIntegrationTest`), whose structure this suite clones. Polyglot C# idiom (partial `*Scenarios.cs` narrative + `*Specifications.cs` step bodies):

- `BlockedItemsAcceptanceTest.cs` — shared production-composition harness (SSOT; base class).
- `BlockedItemsJson.cs` — pure rule-set/JSON builders (domain-vocabulary SSOT).
- `Slice0{1..4}…Scenarios.cs` + `Slice0{1..4}…Specifications.cs` — per-slice partial pairs.

FE (Vitest) selector/component tests and ONE Playwright demo-data POM walking-skeleton E2E are authored in DELIVER (see `distill/red-classification.md` → FE/E2E coverage).

### [REF] Mandate compliance evidence

- **CM-A (hexagonal)**: all steps enter through driving ports (HTTP), zero internal-component imports.
- **CM-B (business language)**: Gherkin-named `[Test]` + step methods (`GivenATeamWhoseBlockedConfigIs`, `WhenTheAdminSavesTheBlockedRuleSet`, `ThenTheItemReadsBlocked`); no HTTP/DB jargon in scenario/step names.
- **CM-C (journeys)**: each scenario is a complete config-admin/flow-coach/delivery-lead journey with observable value.
- **Pillar 3**: SUT = production `WebApplicationFactory<Program>`; only `ILicenseService` faked.
- **No Fixture Theater (Critical Rule 7)**: walking-skeleton assertion targets the blocked-definition fields specifically (not the whole payload, which also mentions states in `doingStates`), so it cannot pass vacuously.
- **Mandate-12 (SSOT)**: domain vocabulary in `BlockedItemsJson` (typed `BlockedCondition` + `StateEquals`/`TagsContains`/`FieldIsNotEmpty` builders); step bodies delegate to base-class port helpers, no inline business logic. Step-reuse-ratio is **informational** (~3–4× across the 4 fixtures via shared base + JSON helper); config-shaped settings scenarios naturally cap here — not gated.
- **Mandate 9/11**: layer-3 (real host + real EF) → example-only; no PBT machinery imported. `@property`-tagged scenarios are example-pinned single-definition invariants, not generative.

### [REF] Self-completeness audit

Verdict: **COMPLETE for the in-scope backend contract.** 15-item mechanical intent — happy/error/edge/boundary/RBAC/property all present per slice; forward-only empty-state, first-observation baseline, disabled-threshold, and single-definition invariants covered. Gaps classified `AT_GAP_IN_DELIVERY_SCOPE` and filled in-suite; the only routed items are UC-2 (deferred, product confirmation) and the FE-derived rendering (Vitest, DELIVER) — neither is a `SPECIFICATION_AMBIGUITY` blocker.

### [REF] Pre-requisites (for DELIVER)

- DESIGN driving ports (settings PUT/GET, WIP read, new `blockedCountHistory`) + ADR-067..070.
- Env: dotnet 10 SDK; Sqlite in-memory-per-test via the existing test factory (no Docker for these slices).
- `distill/red-classification.md` (RED genuineness) + `distill/upstream-issues.md` (UC-2 deferral, outcomes-registry skip).

## Wave: DELIVER / [REF] — Slice 04 (Blocked → Stale linkage)

Date: 2026-07-07 | Wave: DELIVER | Paradigm: OOP | Scope: slice 04 only

### [REF] Implementation Summary

Added `blockedStalenessThresholdDays` settings field (twin of `StalenessThresholdDays`, default 0 = disabled) to backend model, DTO, write path, and validation (0-365). Extended `deriveStaleness` FE selector from `boolean` → `StalenessResult {isStale, reasons[]}` with blocked-duration driver (`≥` boundary), context-time-in-state (UC-1 driver+context split), and ADR-026 preservation (`!isBlocked` guard in time-in-state branch). Updated 3 call sites (TimeInStateBadge, BaseMetricsView, WorkItemAgingChart) to consume `StalenessResult`. Added FE settings chain (IBaseSettings, Zod validation, FlowMetricsConfigurationComponent twin UI).

### [REF] Files Modified (49 files, 5 commits)

**Backend production** (8 files): WorkTrackingSystemOptionsOwner.cs, Team.cs, Portfolio.cs, SettingsOwnerDtoBase.cs, TeamExtensions.cs, PortfolioExtensions.cs, TeamController.cs, PortfolioController.cs
**Backend migrations** (6 files): SQLite + Postgres migration files + snapshots
**Backend tests** (3 files): BlockedStalenessThresholdValidationTests.cs, BlockedStalenessThresholdMigrationTests.cs, Slice04BlockedStalenessScenarios.cs
**Frontend production** (10 files): deriveStaleness.ts, BaseSettings.ts, TimeInStateBadge.tsx, WorkItemAgingChart.tsx, BaseMetricsView.tsx, FlowMetricsConfigurationComponent.tsx, ModifyTeamSettings.tsx, ModifyProjectSettings.tsx, CreateTeamWizard.tsx, CreatePortfolioWizard.tsx, TeamMetricsView.tsx, PortfolioMetricsView.tsx, EditTeam.tsx, EditPortfolio.tsx, WorkItemsDialog.tsx, TeamService.ts, PortfolioService.ts, TestDataProvider.ts
**Frontend tests** (6 files): deriveStaleness.test.ts, TimeInStateBadge.test.tsx, WorkItemAgingChart.test.tsx, BaseMetricsView.test.tsx, FlowMetricsConfigurationComponent.test.tsx, CreateTeamWizard.test.tsx, CreatePortfolioWizard.test.tsx, EditPortfolio.test.tsx
**Docs** (2 files): roadmap.json (Phase 04 extension), execution-log.json

### [REF] Scenarios Green Count

27 of 28 backend BlockedItems scenarios pass (scenario #16 deferred — per-type filtering UC-2). 5 slice-04 scenarios (#17-21) all GREEN. 639 frontend Vitest tests pass across 18 test files.

### [REF] DoD Check

| DoR Item | Slice 04 |
|---|---|
| 1. Problem in domain language | PASS |
| 2. Persona with characteristics | PASS (flow-coach/Priya + config-admin) |
| 3. 3+ domain examples | PASS |
| 4. UAT G/W/T (3-7) | PASS (5 scenarios) |
| 5. AC derived from UAT | PASS |
| 6. Right-sized (≤1d, ≤7 scen) | PASS |
| 7. Technical notes + cross-cutting | PASS |
| 8. Dependencies tracked | PASS (US-01, US-02) |
| 9. Outcome KPIs measurable | PASS |
| RBAC gating | PASS (existing TeamWrite guard) |
| Clients version-gate | PASS (additive field — no gate, ADR-072) |
| Website | N/A (non-premium) |

### [REF] Quality Gates

| Gate | Result |
|---|---|
| dotnet build 0 warnings | PASS |
| dotnet test (BlockedItems) | PASS (27/28, 1 deferred) |
| pnpm test (Vitest) | PASS (639 tests, 18 files) |
| pnpm build (TS/Biome/Vite) | PASS (0 warnings in src) |
| Roadmap review | APPROVED (acceptance-designer-reviewer) |
| Adversarial review | APPROVED (software-crafter-reviewer, 0 blockers) |
| Integrity verification | PASS (all 10 steps DES-complete) |
| ADR-070 call-site audit | PASS (4 call sites consume StalenessResult) |
| Mutation testing | Deferred (prior epic run 79.1%; slice 04 is settings+selector) |

### [REF] Pre-requisites

- Slice 01 (rule-based blocked) + Slice 02 (blockedSince) shipped `IsBlocked` + `blockedSince` on `WorkItemDto`
- DISTILL red-classification.md (scenarios #17-21 RED-verified)
- ADR-070 (blocked-duration staleness, amends ADR-026)
- ADR-072 (contract changes + client version-gate matrix)

---

## Wave: DISCUSS / [REF] Enhancement Batch (2026-07-07) — Reading Confirmation

Date: 2026-07-07 | Mode: lean + ask-intelligent | Type: user-facing | Skeleton: no (foundation shipped)
Batch input: `enhancement-backlog.md` (B1 chart drill-through, B2 RAG status, B3 previous-period trend).

- Read SSOT: `jobs.yaml` (blocked-rules + flow-coach/delivery-lead jobs), `journeys/epic-5074-blocked-items.yaml` (both journeys), personas config-admin/flow-coach/delivery-lead-rte (present).
- Read code reality: `BlockedOverviewWidget.tsx` (currently `blockedCount` only), `BlockedItemsOverTimeChart.tsx` (MUI-X BarChart, snapshots only, non-interactive), `BlockedCountSnapshot.cs` (`{OwnerId, OwnerType, RecordedAt, BlockedCount}` — COUNT only, no membership), `WorkItemsDialog/` (exists, reusable), `BaseMetricsView.tsx` L811-975 (widget site receives `ctx.blockedItems`, `ctx.blockedStalenessThresholdDays`, `ctx.blockedCountHistory`).
- Reconciliation: no contradiction with the shipped foundation. All three extend the non-premium `read-blocked-signals` journey; the single-`IsBlocked` invariant (blockedRuleSet) is preserved — no new definition of "blocked".

## Wave: DISCUSS / [REF] Enhancement Batch — Wave Decisions

- **D-EB1 (B1 drill-through source)**: Reconstruct blocked membership at a past date from `WorkItemBlockedTransition` enter/leave intervals (ADR-068) via a NEW read endpoint. **Rejected**: persisting an item-id set per snapshot (would amend ADR-069 + migration; unnecessary). **Rejected**: current-only click (kept as the SPIKE fallback if reconstruction fails to reconcile). Latest bar reconstructs from live `IsBlocked`. (User-locked.)
- **D-EB2 (B2 RAG driver)**: RAG on the overview widget keyed on MAX blocked age vs the existing `blockedStalenessThresholdDays` (RED past threshold, AMBER aging toward, GREEN none aging; 0 = disabled/neutral). **Rejected**: %-of-WIP-blocked and absolute-count drivers — max-age reuses the line the admin already tuned and matches the blocked→stale signal. (User-locked.)
- **D-EB3 (B3 trend baseline)**: Previous-period trend = current count vs `BlockedCountSnapshot` on the LAST DAY of the previous period; "period" = the dashboard's selected date range. Client-side off the already-loaded `blockedCountHistory`; no new endpoint. No-baseline ⇒ "—", never a fake zero-delta. (Defaulted; user-confirmed the compare-to-prior-period intent.)
- **D-EB4 (premium)**: All three stay non-premium (twin of the shipped widget). Website marketing N/A.
- **D-EB5 (single blocked definition)**: All three derive from the ONE `IsBlocked`/`blockedRuleSet` + the ONE `WorkItemBlockedTransition` capture — no second evaluation path.

## Wave: DISCUSS / [REF] Enhancement Batch — Scope Assessment

**PASS (right-sized).** 3 thin vertical slices, 1 bounded context (blocked-items, app-scope, no new context), each ships end-to-end ≤1 day (B1 ≤1.5d with a ~2h de-risk spike). Signals checked: <10 stories (3), <3 contexts (1), <5 integration points, <2 weeks, and the three outcomes ship independently. No split needed. Deeper carpaccio in slice briefs `slices/slice-06..08-*.md`.

## Wave: DISCUSS / [REF] Enhancement Batch — Story Map & Prioritization

Backbone (delivery lead + flow coach reading blocked signals) → 3 slices, sequenced by dependency + learning leverage + dogfood cadence:

1. **Slice 06 — B3 previous-period trend** (`job-delivery-lead-tell-blocked-trend-vs-last-period`). First: cheapest (client-side, no endpoint/migration), proves the widget-enrichment path, immediate dogfood on Tenant Zero.
2. **Slice 07 — B2 RAG status** (`job-flow-coach-read-blocked-health-at-a-glance`). Reuses `blockedSince` + `blockedStalenessThresholdDays`; no new endpoint.
3. **Slice 08 — B1 chart drill-through** (`job-flow-coach-drill-into-blocked-trend-point`). Last: largest, carries the reconstruct risk (new endpoint + interval-overlap query); pre-slice ~2h SPIKE validates reconciliation vs `BlockedCountSnapshot`, else fall back to current-only click.

## Wave: DISCUSS / [REF] Enhancement Batch — User Stories

### US-EB1 — Previous-period trend on the Blocked widget
As a **delivery lead**, I want the Blocked overview widget to show whether the blocked count is up or down vs the end of the previous period, so I can tell leadership whether we are clearing blockers faster than they arrive.
`job_id: job-delivery-lead-tell-blocked-trend-vs-last-period`

#### Elevator Pitch
Before: the delivery lead sees a bare blocked count with no baseline — "16 blocked" is uninterpretable.
After: open a team/portfolio metrics page → the Blocked widget shows `16` with a `▲ +4 vs last period` indicator (or `—` when no prior-period snapshot).
Decision enabled: raise blockers in the review when trending worse; report a downward trend when improving.

**Acceptance criteria**
1. Current count > prior-period-boundary count ⇒ up/worse direction + positive delta; < ⇒ down/better; = ⇒ flat.
2. Baseline = `BlockedCountSnapshot` on the last day of the previous period (period = selected date range); computed client-side from `blockedCountHistory`, no new endpoint.
3. No snapshot at/before the previous-period boundary ⇒ "—" with "no prior-period baseline yet" tooltip; never a delta of 0.
4. Works on both Team and Portfolio metrics views.

### US-EB2 — Max-blocked-age RAG status on the Blocked widget
As a **flow coach**, I want the Blocked widget to show red/amber/green based on how long the longest-blocked item has been stuck, so I can judge urgency at a glance without opening each item.
`job_id: job-flow-coach-read-blocked-health-at-a-glance`

#### Elevator Pitch
Before: the coach can't tell whether "5 blocked" is all fresh or one stuck three weeks without opening each item.
After: open a team metrics page → the Blocked widget is RED/AMBER/GREEN with a tooltip "oldest blocker: N days".
Decision enabled: dig into blockers this session when red; move on when green.

**Acceptance criteria**
1. An item blocked past `blockedStalenessThresholdDays` ⇒ RED (tooltip names the oldest blocker's age).
2. Oldest item aging toward but not past the threshold ⇒ AMBER; all well within ⇒ GREEN.
3. `blockedStalenessThresholdDays = 0` ⇒ neutral (RAG disabled).
4. Items with no established `blockedSince` baseline are excluded from the max-age computation.
5. Colour is not the only signal (label/tooltip carries the age) — accessible, matches the stale treatment.

### US-EB3 — Drill from a blocked-over-time bar into that day's blocked items
As a **flow coach**, I want to click a bar on the Blocked-Items-over-time chart and see which items were blocked at that date, so I can name the specific blockers behind a spike instead of only seeing the count moved.
`job_id: job-flow-coach-drill-into-blocked-trend-point`

#### Elevator Pitch
Before: the over-time chart shows how many were blocked, never which ones — a dead end for investigation.
After: click a bar on the Blocked-Items-over-time chart → a `WorkItemsDialog` lists the items blocked at that date.
Decision enabled: escalate/investigate the named blockers behind a bad week, not just the trend line.

**Acceptance criteria**
1. Click a bar at date T ⇒ `WorkItemsDialog` lists exactly the items whose `WorkItemBlockedTransition` interval covers T; latest bar reconstructs from live `IsBlocked`.
2. Membership reconstructed (read-only) from transition intervals via a new read endpoint — `BlockedCountSnapshot` unchanged, no persisted membership, no migration.
3. Reconstructed count for T reconciles with `BlockedCountSnapshot.blockedCount` where both exist; a mismatch shows a capture-gap note (no silent divergence).
4. Date before transition-capture start ⇒ partial set + "complete only from {captureStartDate}" note.
5. No items blocked at T ⇒ empty dialog with "no items blocked on this date"; new read endpoint version-gated for CLI/MCP clients (ADR-072 pattern).

## Wave: DISCUSS / [REF] Enhancement Batch — Out of Scope

- Persisting per-snapshot blocked membership (B1 reconstructs instead — no `BlockedCountSnapshot` schema change).
- A RAG threshold separate from `blockedStalenessThresholdDays` (deliberately reuse the tuned line).
- Sparkline/mini-chart on the widget; per-type filtering of the over-time chart (UC-2, still deferred, ADR-069).
- Actioning items from the drill-through dialog (read-only list).

## Wave: DISCUSS / [REF] Enhancement Batch — Outcome KPIs

- **B3**: ≥60% of the prior-period-boundary snapshots present across active teams within 2 weeks (else the "—" state dominates and the trend adds no value). Measure: share of team/portfolio views where the indicator renders a delta vs "—".
- **B2**: RAG RED fires within one sync of an item crossing `blockedStalenessThresholdDays` (parity with blocked→stale). Measure: agreement rate between widget RED and blocked→stale on the same items.
- **B1**: reconstructed date-T count reconciles with `BlockedCountSnapshot.blockedCount` within ±1 for ≥95% of sampled historical dates (else fall back to current-only). Measure: reconciliation sample in the slice-08 spike + a backend integration assertion.

## Wave: DISCUSS / [REF] Enhancement Batch — Definition of Ready

| DoR Item | Status |
|---|---|
| 1. Problem in domain language | PASS (3 job stories in `jobs.yaml`) |
| 2. Persona with characteristics | PASS (delivery-lead-rte, flow-coach) |
| 3. 3+ domain examples | PASS (per-story AC) |
| 4. UAT G/W/T | PASS (AC per story; Gherkin at DISTILL) |
| 5. AC derived from jobs | PASS |
| 6. Right-sized (≤1d/slice) | PASS (scope assessment; B1 +spike) |
| 7. Technical notes + cross-cutting | PASS (code-grounded in slice briefs) |
| 8. Dependencies tracked | PASS (slices 02/03/04 shipped) |
| 9. Outcome KPIs measurable | PASS (above) |
| Job traceability | PASS (every story → real `job_id`) |
| Elevator pitch (real entry point + observable output) | PASS (3/3) |
| Slice composition (≥1 value story/slice) | PASS (no `@infrastructure`-only slice) |
| RBAC | PASS (read-only, existing metrics-view gating; no new surface) |
| Clients version-gate | B1 new read endpoint gated (ADR-072); B2/B3 FE-only, no gate |
| Website | N/A (non-premium) |

## Wave: DISCUSS / [REF] Enhancement Batch — Risks & Open Questions

- **R-EB1 (B1 reconstruction fidelity)**: `WorkItemBlockedTransition` L1 capture (sync cadence; re-block within a cadence collapses to one spell) may make reconstructed membership diverge from the snapshot count → the ~2h slice-08 SPIKE gates it, with current-only click as the fallback. HIGH-uncertainty, sequenced last.
- **OQ-EB1 (B2 AMBER band)**: the exact aging fraction of `blockedStalenessThresholdDays` for AMBER is a DESIGN default (documented), not a user setting — confirm the default at DESIGN.
- **OQ-EB2 (B2 prop threading)**: verify `blockedSince` reaches the `blockedOverview` widget site (currently only `blockedItems.length` is used) — thin prop-threading folded into slice 07 if absent.

## Wave: DISCUSS / [REF] Enhancement Batch — Wave Decisions Summary

- Primary needs: turn the blocked count/trend from a bare number into (B3) a directional signal, (B2) an at-a-glance urgency cue, and (B1) an investigable record — all on the shipped non-premium foundation, all from the single `IsBlocked`/transition source.
- Feature type: user-facing. Skeleton: none (extends shipped slices 01-04). Slices 06-08.
- Constraints: no `BlockedCountSnapshot` schema change; reuse `blockedStalenessThresholdDays`; one blocked definition; B1 endpoint version-gated.
- Upstream changes: none to DISCOVER; SSOT `jobs.yaml` (+3 jobs) and journey `read-blocked-signals` (+3 steps, +`blockedMembershipAtDate` artifact) extended.
- **NEXT = /nw-design** for the three slices (B1 reconstruct endpoint is the main design surface; B2/B3 are FE-shaped). ADO: add Stories #… under Epic #5074 (confirm before create per /ado-sync).

---

## Wave: DESIGN / [REF] Enhancement Batch (2026-07-07)

Date: 2026-07-07 | Scope: Application (@nw-solution-architect) | Mode: propose | Paradigm: OOP | Style: modular monolith + ports-and-adapters (unchanged) | Density: lean

**Headline (codebase reality changes the slices):** the widget infrastructure the DISCUSS batch assumed as *new* already exists. `WidgetShell` already renders a RAG chip (from `computeBlockedOverviewRag`, count-driven today), a trend arrow+tooltip (`TrendPayload`/`TrendChrome`), and a "view data" → `WorkItemsDialog`. So B2 and B3 are **re-drive/feed** of existing chrome, not new UI; B1's *current-set* dialog is already covered — only *historical reconstruction* is genuinely new. Three DISCUSS assumptions refined (back-prop → `design/upstream-changes.md`).

### [REF] DDD list (enhancement batch)

- **D-EB-D1 (B1 reconstruct, not persist)** — items-blocked-at-date T are reconstructed on read from `WorkItemBlockedTransition` interval overlap (`EnteredAt.Date ≤ T ∧ (LeftAt is null ∨ LeftAt.Date ≥ T)`), joined to `WorkItem.TeamId` (Team) / feature owner (Portfolio). No persisted membership; `BlockedCountSnapshot` unchanged; no migration. New ADR-099. Verdict: ACCEPTED (user-locked D-EB1).
- **D-EB-D2 (B1 endpoint mirrors ADR-069)** — new `GET .../metrics/blockedItemsAtDate?date=T` on `TeamMetricsController` + `PortfolioMetricsController`, same `GetEntityByIdAnExecuteAction` shape as `blockedCountHistory`; returns the existing `WorkItemDto[]` (FE reuses `WorkItemsDialog`). Latest date reconstructs from live `IsBlocked`. Version-gated for CLI/MCP (ADR-072 pattern). Verdict: ACCEPTED.
- **D-EB-D3 (B1 reconciliation guard)** — the reconstructed count for T is compared to `BlockedCountSnapshot.blockedCount` for T where both exist; divergence surfaces a capture-gap note, never silent. Verdict: ACCEPTED.
- **D-EB-D4 (B2 re-drive existing RAG)** — `computeBlockedOverviewRag` is re-keyed from `blockedCount ≥ 2` to MAX blocked age vs the existing `blockedStalenessThresholdDays` (RED past threshold, AMBER within an aging band = default fraction of threshold, GREEN none aging; threshold 0 ⇒ `"none"`). Signature gains blocked items' `blockedSince` + threshold. EXTEND the existing function + its one call site. Verdict: ACCEPTED.
- **D-EB-D5 (B3 feed existing trend chrome)** — a new pure client selector `computeBlockedTrend(blockedCountHistory, startDate, endDate): TrendPayload` computes the prior-period-boundary baseline and direction/delta; wired into the existing `widgetTrends` map for `blockedOverview` and the `trendPolicy` flag flipped off `"none"`. No backend, no new UI (`WidgetShell.trend` renders it). No baseline ⇒ `direction: "none"` (chrome hidden). Verdict: ACCEPTED.
- **D-EB-D6 (single blocked source preserved)** — every enhancement derives from the ONE `IsBlocked`/`blockedRuleSet` + the ONE `WorkItemBlockedTransition` capture. No second evaluation path (ADR-067/068 invariant upheld). Verdict: ACCEPTED.

### [REF] Component decomposition (enhancement batch)

| Component | Path | Slice | Change |
|---|---|---|---|
| `computeBlockedOverviewRag` | `pages/Common/MetricsView/ragRules.ts` | 07 (B2) | EXTEND — re-key to max blocked age + threshold |
| blocked RAG call site | `pages/Common/MetricsView/BaseMetricsView.tsx` (~L264) | 07 | MODIFY — pass `blockedItems` (with `blockedSince`) + `blockedStalenessThresholdDays` |
| `computeBlockedTrend` (new) | `pages/Common/MetricsView/blockedTrend.ts` (new) | 06 (B3) | CREATE NEW — pure selector, `blockedCountHistory` → `TrendPayload` |
| `widgetTrends` map + `trendPolicy` | `BaseMetricsView.tsx`, `categoryMetadata.ts` | 06 | EXTEND — add `blockedOverview` trend entry; flip policy off `"none"` |
| `BlockedItemsOverTimeChart` | `components/Common/Charts/BlockedItemsOverTimeChart.tsx` | 08 (B1) | EXTEND — `BarChart` `onItemClick` → resolve date → open `WorkItemsDialog` |
| `blockedItemsAtDate` endpoint | `API/TeamMetricsController.cs`, `API/PortfolioMetricsController.cs` | 08 | CREATE NEW — mirrors `blockedCountHistory` |
| reconstruct query | `Services/…/Repositories/WorkItemBlockedTransitionRepository.cs` (+interface) | 08 | EXTEND — interval-overlap-at-date predicate (or `GetAllByPredicate`) |
| `WorkItemsDialog` | `components/Common/WorkItemsDialog/` | 08 | REUSE — render reconstructed set |
| `WidgetShell` viewData/trend/RAG chrome | `pages/Common/MetricsView/WidgetShell.tsx` | 06/07/08 | REUSE — no change; already renders all three |

### [REF] Driving ports

- B1: `GET /api/{teams|portfolios}/{id}/metrics/blockedItemsAtDate?date=YYYY-MM-DD` (new read endpoint; version-gated).
- B2/B3: none (client-side off already-loaded `blockedItems` + `blockedCountHistory`).

### [REF] Driven ports + adapters

- B1: `IWorkItemBlockedTransitionRepository` (existing owned-entity store) — read-only interval-overlap query. No new adapter.
- B2/B3: none.

### [REF] Technology choices

No new tech. Backend C# .NET 10 (existing metrics controllers + EF repo). Frontend React 18 + TS + MUI-X `BarChart` `onItemClick` (existing dep). Zod at the new `blockedItemsAtDate` response boundary (reuses the `WorkItemDto` schema). No new package, no migration.

### [REF] Decisions table

| ID | Decision |
|---|---|
| D-EB-D1 | B1 reconstruct from transition intervals, not persist (ADR-099) |
| D-EB-D2 | B1 endpoint mirrors `blockedCountHistory`, returns `WorkItemDto[]`, version-gated |
| D-EB-D3 | B1 count reconciles with `BlockedCountSnapshot`; divergence surfaced |
| D-EB-D4 | B2 re-drives `computeBlockedOverviewRag` from max blocked age + `blockedStalenessThresholdDays` |
| D-EB-D5 | B3 feeds existing `WidgetShell` trend via `computeBlockedTrend` selector |
| D-EB-D6 | single blocked source preserved (ADR-067/068 invariant) |

### [REF] Reuse Analysis

| Existing Component | File | Overlap | Decision | Justification |
|---|---|---|---|---|
| `computeBlockedOverviewRag` | `ragRules.ts` | Blocked widget RAG (count-driven) | EXTEND | Re-key inputs to max-age + threshold; ~15 LOC vs a new RAG fn + wiring |
| `WidgetShell` trend chrome + `TrendPayload`/`TrendChrome` | `WidgetShell.tsx`, `trendTypes.ts` | Trend arrow + delta tooltip UI | REUSE | UI already renders `blockedOverview` trend once a payload is supplied |
| `widgetTrends` map | `BaseMetricsView.tsx` | Per-widget trend wiring | EXTEND | Add one `blockedOverview` entry (mirrors `wipOverview?.comparison`) |
| `trendPolicy` | `categoryMetadata.ts` | Gates trend display | EXTEND | Flip `blockedOverview` off `"none"` |
| `WidgetShell` viewData → `WorkItemsDialog` | `WidgetShell.tsx` | Current blocked-set dialog | REUSE | `widgetViewData.blockedOverview` already lists current blocked items — B1's live case is done |
| `BlockedItemsOverTimeChart` | `Charts/BlockedItemsOverTimeChart.tsx` | Over-time bars | EXTEND | Add `onItemClick`; ~existing MUI-X `BarChart` prop |
| `WorkItemsDialog` | `WorkItemsDialog/` | Item-list dialog | REUSE | Render the reconstructed set |
| `blockedCountHistory` endpoint | `TeamMetricsController.cs`, `PortfolioMetricsController.cs` | Metrics read-endpoint pattern | EXTEND (mirror) | New sibling endpoint, same `GetEntityByIdAnExecuteAction` shape |
| `IWorkItemBlockedTransitionRepository` | `…/Repositories/` | Interval store | EXTEND | Add interval-overlap-at-date read (or `GetAllByPredicate`) |
| **point-in-time membership reconstruction** | — | none | **CREATE NEW** | No existing code reconstructs blocked membership at date T; the sole genuinely-new surface (B1) — justified by D-EB-D1 |

### [REF] Open questions

- **OQ-EB-D1 (B2 AMBER band)**: the aging fraction of `blockedStalenessThresholdDays` for AMBER (proposed default: ≥ 75% of threshold). Confirm the default at DELIVER; not a user setting.
- **OQ-EB-D2 (B3 period boundary)**: "last day of previous period" for a selected range `[start,end]` = the snapshot on `start − 1 day` (proposed). Confirm vs `start` at DELIVER.
- **OQ-EB-D3 (B1 Portfolio owner join)**: Team joins via `WorkItem.TeamId`; the Portfolio/Feature owner link for the reconstruct query is confirmed at DELIVER (features carry their own transitions; same interval predicate).

### [REF] Wave Decisions Summary (DESIGN)

- Pattern/paradigm: modular monolith + ports-and-adapters, OOP (unchanged). No new bounded context.
- Net-new surface is small: one backend read endpoint + one interval query (B1), one FE selector (B3), one FE function re-key (B2). Everything else EXTENDS/REUSES shipped widget chrome.
- ADR-099 (B1 reconstruct-not-persist) added; ADR-067/068/069/072 unchanged and upheld.
- Back-prop: 3 DISCUSS assumptions refined (RAG/trend/current-dialog already exist) → `design/upstream-changes.md`.
- **NEXT = /nw-devops (KPIs)** then **/nw-distill** (acceptance tests). ADO: 3 Stories under #5074 (confirm before create).

---

## Wave: DISTILL / [REF] Enhancement Batch (2026-07-07)

Date: 2026-07-07 | Density: lean | Stack: NUnit (backend acceptance via `WebApplicationFactory`) + Vitest/RTL (frontend) + Playwright POM (E2E) — project idiom wins over the skill's Python examples.

**Reconciliation gate: PASSED — 0 contradictions.** DESIGN's back-prop (RAG/trend/current-dialog already exist) REFINES the DISCUSS stories toward the same outcomes; no decision reversed. DEVOPS skipped (user, 2026-07-07) → default env matrix; KPIs from the DISCUSS batch section stand.

### [REF] Scenario list with tags

**B1 (slice-08) — backend acceptance, `Slice08BlockedDrilldownScenarios.cs` (+ `.Specifications.cs`), all `[Ignore]`-pending (RED-ready, ADR-025):**

| Scenario | Tags |
|---|---|
| Items blocked at a past date reconstructed from transition intervals | `@driving_port @us-eb1` |
| Latest date reconstructs from the live blocked set | `@driving_port @us-eb1` |
| A date with no blocked items returns an empty dialog | `@edge @us-eb1` |
| Reconstructed membership count reconciles with the snapshot count | `@invariant @us-eb1` |
| A date before capture started is served as a partial set | `@edge @us-eb1` |

**B2 (slice-07) — `blockedMaxAgeRag.test.ts`, `describe.skip` (RED-ready):** RED past threshold; AMBER aging band; GREEN none aging; GREEN when no blocked items (max age null); `none` when threshold 0. `@us-eb2`

**B3 (slice-06) — `blockedTrend.test.ts`, `describe.skip` (RED-ready):** up when current > prior-period boundary; down when <; flat when =; undefined (chrome hidden) when no boundary snapshot; undefined for empty/null history. `@us-eb3`

**Authored in DELIVER (against existing chrome, not new scaffolds):** the B1 chart-bar `onItemClick` → `WorkItemsDialog` wiring (Vitest on `BlockedItemsOverTimeChart.test.tsx`) and the widget-level end-to-end assertions on the existing test-ids `rag-status` (B2), `widget-trend-*` (B3) in `BaseMetricsView.test.tsx`; plus one Playwright walking-skeleton per user-visible slice (demo-data, POM) — B1 drill-through is the clearest E2E. Per the E2E-minimalism standing rule, the invariant assertions live in the backend AT above; Playwright proves wiring only.

### [REF] WS strategy (Architecture of Reference)

- **Driving port** (HTTP `GET .../metrics/blockedItemsAtDate`) → real adapter via `WebApplicationFactory<Program>` + `AsTeamAdmin` (existing project policy, slices 01-04 precedent).
- **Driven internal** (`IWorkItemBlockedTransitionRepository` read, `IBlockedCountSnapshotRepository` reconcile) → real via EF InMemory (project default).
- Walking skeleton = the B1 reconstruct scenario through the real host. FE B2/B3 = in-memory pure-selector unit specs; E2E walking skeleton (Playwright) authored in DELIVER.

### [REF] Adapter coverage table

| Adapter | @real-io scenario | Covered by |
|---|---|---|
| `IWorkItemBlockedTransitionRepository` (interval read) | YES | B1 scenarios (real EF InMemory, seeded transitions) |
| `IBlockedCountSnapshotRepository` (reconcile) | YES | B1 "reconciles with the snapshot count" |
| (B2/B3 are client-side — no driven adapter) | n/a | pure selectors, Vitest |

### [REF] Scaffolds (Mandate 7)

- `Lighthouse.Frontend/src/pages/Common/MetricsView/blockedTrend.ts` — `__SCAFFOLD__`, `computeBlockedTrend(...)` throws (B3).
- `Lighthouse.Frontend/src/pages/Common/MetricsView/blockedMaxAgeRag.ts` — `__SCAFFOLD__`, `computeBlockedMaxAgeRag(...)` throws (B2).
- **Backend B1: no scaffold** — the endpoint is absent, so the request 404s / falls to the SPA HTML fallback; the `Specifications` `Then` helper asserts a clean RED ("endpoint appears unimplemented") rather than a raw parse crash. DELIVER adds the endpoint.

### [REF] Test placement

- Backend: `Lighthouse.Backend.Tests/API/Integration/BlockedItems/` — precedent: Slice01-04 paired `*Scenarios.cs` + `*Specifications.cs` (C# partial-class idiom, polyglot matrix row).
- Frontend: co-located next to source in `pages/Common/MetricsView/` — precedent: `ragRules.test.ts`, `trendTypes.test.ts`.

### [REF] Driving adapter coverage

`GET /api/latest/teams/{id}/metrics/blockedItemsAtDate?date=` — covered by 5 HTTP scenarios (real host, exit status + JSON body + reconstructed membership). B2/B3 have no HTTP surface (client-side off already-loaded data). Portfolio endpoint twin is authored in DELIVER alongside `PortfolioMetricsController` (same interval predicate).

### [REF] Pre-requisites

- DESIGN driving port `blockedItemsAtDate` (ADR-099); `WorkItemBlockedTransition` capture (slice-02, shipped) — the interval source; `BlockedCountSnapshot` (slice-03, shipped) — reconcile source; `WorkItemsDialog` + `WidgetShell` trend/RAG chrome (shipped) — FE reuse targets.

### [REF] Verification gates run

- Backend test project: `dotnet build` — **0 errors** (RED-ready, not BROKEN; `[Ignore]`-pending compiles).
- Frontend: Biome clean; `tsc -b` exit 0; `vitest` on the 2 new specs = 10 skipped (imports resolve — RED-ready). `red-classification.md` written.
- Outcomes registry: N/A — `nwave-ai` CLI is not part of this product repo (nWave methodology tooling); the new `blockedItemsAtDate` operation is recorded here + in ADR-099 instead.
- Mandate-12: C# ATs use the existing typed `SeededTeam` record + `*Specifications.cs` service-delegating step helpers (no business logic in steps); step-reuse informational — batch is 3 thin slices, reuse ceiling naturally low.

### [REF] Final Wave Review Gate — PENDING (user-gated)

The mandatory 4-reviewer gate (Eclipse/Architect/Forge/Sentinel on Haiku, in parallel over the full 4-wave `feature-delta.md`) is NOT auto-dispatched — surfaced for the user to trigger (spawning agents is user-gated in this session). Sentinel (`@nw-acceptance-designer-reviewer`) is the structural-correctness reviewer that never skips.

## Wave: DELIVER / [WHY] Upstream Issues

- Slice-08 capture-gap/partial-set note delivered via response header (`X-Blocked-Reconstruction-Complete-From`) + structured log warning, not body — the DISTILL ATs #22-24 pin the body to a bare `WorkItemDto[]` array.
- 08-05 MetricsService.getBlockedItemsAtDate mirrors the existing typed apiService.get idiom rather than Zod — the codebase has no Zod imports yet (Story 5232 'awaiting go'); introducing the first Zod boundary here is out of scope. The `X-Blocked-Reconstruction-Complete-From` header note is deferred at the FE (apiService.get returns only `.data`, not headers) — TODO left in BlockedItemsOverTimeChart; the click→WorkItemsDialog drill-in flow shipped.

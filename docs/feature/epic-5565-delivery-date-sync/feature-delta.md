<!-- markdownlint-disable MD024 -->
# Feature Delta — epic-5565-delivery-date-sync

ADO Epic **#5565 "Sync Delivery Dates with the Work Tracking System"** (Planned, Size 1, tags
`Premium; Productboard`, reported by Chris Graves, forecasted delivery 2026-08-23).
Child **#4463 "Delivery Write-Back"** (New, empty description, tags `Delivery; Documentation; Release Notes`)
is **absorbed into this Epic** by user decision — see D0.
Predecessor: **#5698 Deliveries as Durable Records** (Deliveries are records with notes and archive).

Wave DISCUSS run 2026-08-22. Cold DISCUSS — no DISCOVER or DIVERGE artifacts existed. Grounded in an
ADO read plus a code reality check (see Current-State Surface Inventory).

Density: `lean` + `ask-intelligent` — Tier-1 [REF] only. Two Tier-2 triggers fired; see the expansion
menu at the foot of this file.

---

## Wave: DISCUSS / [REF] Persona IDs

| Persona | Role in this Epic |
|---|---|
| `delivery-forecaster` | **Primary.** Maintains the Delivery in Lighthouse and today re-types a date that already exists in Jira. |
| `config-admin` | **Primary for degradation.** Owns the work tracking connection; must understand why the Jira Release tab exists on one Portfolio and not another, and why an outbound write was refused. |
| `product-owner` | **Primary for outbound.** Lives in Jira, never opens Lighthouse, and today has no way to see the forecast. |
| `delivery-lead-rte` | Secondary. Reads the Delivery in the review; benefits from the date being unarguable. |

---

## Wave: DISCUSS / [REF] JTBD One-Liners

| Job ID | One-liner |
|---|---|
| `job-forecaster-bind-a-delivery-to-the-release-it-tracks` | When the thing I am forecasting already exists as a Release in Jira, I want to point a Delivery at it instead of re-describing it, so I stop maintaining the same date and the same scope in two systems. |
| `job-forecaster-trust-the-delivery-date-without-rechecking-jira` | When a Release date moves in Jira, I want Lighthouse to already know, so I can present a forecast without first checking whether the target it is measured against is still current. |
| `job-po-see-the-forecast-without-opening-lighthouse` | When I plan against a Release in Jira, I want Lighthouse's forecast date on the Release itself, so I can see whether the plan is credible without being asked to learn another tool. |
| `job-config-admin-know-when-a-delivery-source-degrades` | When something on the far side changes underneath a Delivery that was syncing, I want Lighthouse to say so where I would look, so I fix the cause instead of discovering months later that a number stopped updating. |

Full JTBD narrative (dimensions, four forces, opportunity scores) lives in `docs/product/jobs.yaml`.

---

## Wave: DISCUSS / [REF] Current-State Surface Inventory

Established by reading the code before writing requirements. Every decision below rests on these.

| # | Fact | Evidence |
|---|---|---|
| S1 | `Delivery` carries Name, Date, PortfolioId, `List<Feature>`, SelectionMode, RuleDefinitionJson. **No remote identity, no provenance, no last-synced value.** There is nothing to sync *against*. | `Models/Delivery.cs` |
| S2 | The ctor **rejects a date that is not in the future** (`ArgumentException`). A Release whose date has passed cannot be represented today. | `Models/Delivery.cs:16-19` |
| S3 | `DeliverySelectionMode` has exactly two members, `Manual = 0` and `RuleBased = 1`. EF persists the enum as an int with no `HasConversion`. | `Models/DeliverySelectionMode.cs` |
| S4 | The binary Manual/RuleBased assumption is **hardcoded in ~8 places in one frontend file** — a two-button `ButtonGroup`, then `if (mode === Manual) … else …` in `SelectionModeContent`, the validity helper, the payload builder, the edit-hydration effect and the reset. | `DeliveryGrid/DeliveryCreateModal.tsx:242,305,357,427,463,545,589,676-706` |
| S5 | The Jira connector has **zero** Release/`fixVersion` code. This is greenfield capability, not a connector parity gap. | grep over `WorkTrackingConnectors/Jira/*.cs` |
| S6 | The full Jira fetch requests `AllFields = "*all"`, so **`fixVersions` already arrives in the payload today**. Only the identity sweep is narrowed (`SweepFields = "key,updated"`), and it must stay narrow. | `JiraWorkTrackingConnector.cs:37,42,1559` |
| S7 | Optional capability has an established shape on the port: `SupportsTransitionHistory(connection)` and `SupportsIncrementalSync(connection)` — **per connection, not per connector**, because Jira Cloud and Jira Data Center are one connector class that does not answer alike. | `IWorkTrackingConnector.cs:9,16` |
| S8 | Rule-based Deliveries are recomputed **inside the Portfolio refresh cycle**, at `PortfolioUpdater`, between the Feature fetch and the forecast run. Inbound sync needs no new scheduler — this is the seam. | `BackgroundServices/Update/PortfolioUpdater.cs:73-79` |
| S9 | Outbound write-back **already ships**: `WriteFieldsToWorkItems(connection, updates)` with a collect-and-flush collector staged twice in the same refresh method, plus `notifyUsers=false` with an optimistic retry on 403 (ADR-142). It writes **work item fields only** — `WriteBackFieldUpdate` names an issue and a field. | `IWorkTrackingConnector.cs:71`, `PortfolioUpdater.cs:82-95`, `docs/feature/quiet-jira-writeback/` |
| S10 | The rule schema already exposes `additionalField.{id}` conditions, so an admin *could* today configure `fixVersions` as an additional field and write `equals "2026 Q4"`. | `DeliveryRuleService.cs:25-45` |
| S11 | …but rule matching is **case-insensitive string comparison against a single stored value**, with operators `equals / notEquals / contains / notContains / isEmpty / isNotEmpty` only. A Jira issue's `fixVersions` is an **array**, and the rule carries the version **name**, not its id. | `RuleEvaluator.cs:105-151`, `FeatureFieldProvider.cs` |
| S12 | `DeliveryMetricSnapshot` already records `TargetDateAtSnapshot` per day. A date that moves because Jira moved it is **already historically visible for free** — no new history store. | `Models/DeliveryMetricSnapshot.cs`, `docs/feature/delivery-target-date-tracking/` |
| S13 | `WorkTrackingSystems` is an append-only int-persisted enum: `AzureDevOps, Jira, Linear, Csv, ServiceNow`. Four of the five have no Release concept. | `WorkTrackingConnectors/WorkTrackingSystems.cs` |
| S14 | "Delivery"/"Deliveries" and "Feature"/"Features" are **configurable Terminology keys**. Every UI string and doc line below renders the tenant's word. Jira's "Release" is a literal remote value and stays as written. | `Seeding/TerminologySeeder.cs` |
| S15 | **Archiving does not exist yet.** `Delivery` has no archive field of any kind (`grep -ric "archiv" Models/Delivery.cs` = 0), and no backend file references `IsArchived`. #5698 puts archiving in its slices 04-05, which have **not shipped**. AC-03.7 and AC-05.5 assume it, on the user's stated sequencing that archiving ships first — see Pre-requisites. | `Models/Delivery.cs`, `docs/feature/epic-5698-deliveries-as-durable-records/slices/` |

∴ **S1 + S2 are the gap** — a Delivery has no remote identity and cannot even hold a past date.
**S8 + S9 are the answer** — both directions plug into one existing method, and outbound reuses a shipped,
permission-hardened path. **S10 + S11 are the reason a rule is not enough** (D3).

---

## Wave: DISCUSS / [REF] Locked Decisions

### D0 — Both directions live in this Epic; #4463 is absorbed

**Decision** (user, 2026-08-22). Inbound (Release → Delivery) and outbound (forecast → Release) are
DISCUSSed together and slice out of one backlog. #4463 does not get its own DISCUSS pass.

**Why**: conflict policy (D4) is the one decision both directions depend on, and settling it with only
one direction visible is how the two ends drift. #4463 had no description to DISCUSS from anyway.

**Consequence**: #4463 becomes the ADO Story for slice 04, not a sibling Epic. Its `Release Notes` tag
carries over.

### D1 — A Delivery source is a registered handler, and it is a third selection mode

**Decision** (user, 2026-08-22). Beside **Manual** and **Rules**, the Delivery modal shows **0..n**
further tabs, one per source handler that the Portfolio's connected work tracking system registers.
The first and only handler is **Jira Release**.

A handler owns **both** things a Delivery needs: **which Features are in it** and **what date it is
measured against**. Picking Release *"2026 Q4"* sets the Delivery's Features to the Portfolio Features
carrying that `fixVersion`, and its Date to the Release's `releaseDate`. Both are read-only in
Lighthouse from that moment.

**Why a handler and not a rule** — see D3.

**Why the registry ships in slice 01a rather than later**: the tab must be **absent** on Azure DevOps,
Linear, ServiceNow and CSV Portfolios (S13). "Ask the server which sources this Portfolio offers" is
what produces that absence. A hardcoded third tab would have to be hidden by a client-side
`if (system === Jira)`, which is the thing the port exists to avoid. The registry is therefore load-bearing
on day one, not speculative generality.

**Consequence for the frontend**: S4 is a precursor. The binary branch has to become a list-driven
render **inside slice 01a**, as its opening move — not as a separately shipped refactor slice, which
would be a slice with no user-visible value (slice composition hard gate).

### D2 — Handler capability is declared per connection, not per connector

**Decision.** Availability answers on the same shape as S7 — a per-connection question asked of the
connector, not a static per-connector table.

**Why**: Jira Cloud and Jira Data Center are one connector class and are already known to disagree about
things this Epic touches (`quiet-jira-writeback` SPIKE-03 found Cloud 403s where DC behaviour is still
unverified). A per-connector answer cannot express "this Jira can, that Jira cannot".

**Consequence**: a Portfolio whose connection loses the capability (credential downgraded, project
changed) must degrade the same way a deleted Release does (D6), not crash.

### D3 — A Release handler is not a rule with a nicer label

The reviewer question this pre-empts: S10 says an admin can already configure `fixVersions` as an
additional field and write `equals "2026 Q4"`. Four reasons that is not this feature:

1. **A rule yields no date.** The date is the entire point of the Epic. A rule selects Features and
   stops.
2. **A rule matches one string against an array.** `fixVersions` is a list; the evaluator compares a
   single stored value case-insensitively (S11). An issue in two Releases matches unpredictably.
3. **A rule carries a name, not an id.** Rename the Release in Jira and the Delivery silently empties —
   the exact class of quiet wrongness the Epic exists to remove. A handler stores the Version **id**.
4. **A rule cannot enumerate.** There is no picker; the user hand-types a version string with no
   feedback until validate. A handler lists the Releases that actually exist, with their dates.

### D4 — Remote always wins; the bound date is read-only in Lighthouse

**Decision** (user, 2026-08-22). For a source-bound Delivery, the work tracking system is the single
source of truth for the date **and** the membership. Lighthouse never edits either; the fields render
read-only with their provenance shown. To edit by hand, unbind the Delivery back to Manual.

**Why**: it removes the conflict class by construction rather than managing it. There is no last-synced
value to store, no divergence state, no "which side changed more recently" comparison, and no silent
overwrite. Every alternative buys a conflict UI with it.

**Consequence**: membership is **not** hand-overridable while bound. A user who wants "the Release plus
one extra Feature" unbinds, or uses Rules.

### D5 — A synced date in the past is accepted and shown as overdue

**Decision** (user, 2026-08-22). S2's future-date invariant is a **hand-entry** rule and stays one for
Manual and Rules. A date arriving from the remote system is accepted whatever it says, including today
and the past, and renders as overdue.

**Why**: rejecting it would mean Lighthouse quietly disagrees with Jira about the date — the precise
failure this Epic exists to remove. A Release that slipped past its date is a normal, common state.

**Consequence for DESIGN**: the invariant moves off the constructor onto the hand-entry path. Any
factory or copy-constructor that reconstitutes a `Delivery` from storage must not re-run it (see the
`[NotMapped]` init-only class of bug already recorded against work-item sync).

### D6 — A vanished Release keeps the Delivery, freezes the date, and says so

**Decision** (user, 2026-08-22). When the bound Release no longer answers — deleted, or the credential
lost sight of the project — the Delivery is **kept**, its last known date and Feature set are **frozen**,
and the binding is flagged as broken on the Delivery itself. Nothing auto-unbinds and nothing is deleted.

**Why not auto-unbind**: it silently converts a synced Delivery into a hand-maintained one whose date
nobody is updating, and the user is never told the source went away.

**Why not delete**: #5698 established that a Delivery is a durable record. Deleting one because a remote
object vanished contradicts that outright.

### D7 — Outbound writes the forecast onto the Jira Release, not onto the work items

**Decision** (user, 2026-08-22). Lighthouse pushes the Delivery's forecast — the 85% date and the
likelihood — onto the **Version object** in Jira, so a Jira-native reader sees it on the Release they
already plan against.

**Why not the member issues** (the cheaper option, S9): the per-issue write path already carries
per-Feature numbers. Putting a Delivery-level forecast on every member issue repeats one number N times,
on the objects that suffer the notification problem `quiet-jira-writeback` spent an Epic fixing. The
Release is the object the number is *about*.

**Consequence — this is a new write class, not a new `WriteBackFieldUpdate`.** S9's collector names an
issue and a field. A Version write is `PUT rest/api/3/version/{id}`, a different endpoint with a
different permission bar (project-admin) and a different failure shape. DESIGN must decide whether it
joins the existing collector as a second staged type or sits beside it. Slice 00 measures the
permission bar before either is designed.

### D8 — Outbound only: publishing is opt-in and writes the description, not the Release date

**Scope of this decision — read it before D9.** D8 constrains only what Lighthouse *writes into* Jira.
It says nothing about the inbound direction: a bound Delivery's name and date **are** set from the
Release automatically and **are** re-applied on every Portfolio update, exactly the way a rule-based
Delivery re-matches its Features. That is D1 + D4 + D9, and it is the point of the Epic — nothing about
a bound Delivery is maintained by hand in Lighthouse. D8 exists so that publishing the forecast back
cannot quietly overwrite the target the forecast is measured against.

**Decision.** Outbound is off until switched on. The default write target is the Release **description**
(append/replace a delimited Lighthouse block), **not** `releaseDate`.

**Why**: writing `releaseDate` would make Lighthouse overwrite the very field D4 declares the remote
system owns — a sync loop where Lighthouse's forecast becomes the target it is then measured against.
Writing the *forecast* into the field the *target* lives in destroys the distinction the whole product
rests on.

**Consequence**: "push the forecast **date**" from the Epic description means "push it where a Jira reader
sees it", not "into `releaseDate`". Slice 00 confirms the description field is writable and how it
renders; if it is not usable, the fallback is a Jira comment on the Release's issues, decided in DESIGN.

**Observed 2026-08-22 (live, Releases page).** `description` is a **column in the Releases list**, not
just a field on a detail view — so the published text reaches the reader on the page they already open.
The column truncates, which shapes the block's ordering (AC-05.3c) but does not shrink its content: the
block carries attribution, the write date, the three forecasts and the target likelihood (AC-05.3b), and
is read in full on the Release itself. `Elixir Project`'s past date renders in red: Jira computes overdue
itself.

**Raised and dropped — "override the release date with the forecast" (user, 2026-08-22).** A possible
opt-in where the user chooses a percentile and Lighthouse writes *that date* into `releaseDate`.
**Dropped by the user the same day** — not scheduled, not a backlog item. One constraint is recorded with
it in case it ever returns, because it is not obvious and decides whether the option is coherent at all:

> **Writing `releaseDate` while also reading it creates a closed loop.** Inbound takes the target from
> `releaseDate`; outbound would write the forecast into the same field. The Delivery's target then *is*
> Lighthouse's own forecast, so "likelihood of hitting the target" converges on the chosen percentile by
> construction and stops carrying information.

That is not an argument that the idea is bad — "publish our forecast as the plan" is a legitimate mode
some teams want. But it is a **different mode**, not a checkbox on this one: such a Delivery has no
independent target, so the likelihood reading must be suppressed or relabelled rather than left showing a
number that means nothing. Its own feature, with that as its first decision.

### D9 — Inbound sync runs on the existing Portfolio refresh, on the existing cadence

**Decision.** Source-bound Deliveries recompute at S8's seam, immediately beside
`RecomputeRuleBasedDeliveries`, on every Portfolio refresh. No new background service, no new schedule,
no new setting.

**Consequence**: sync latency equals the Portfolio's refresh interval. That is the same latency every
other number on the Delivery already has, so it needs no separate explanation to the user.

### D10 — Premium

**Decision.** The capability is Premium, consistent with the Epic's `Premium` tag and with rule-based
Delivery selection, which is already `LicenseGuard(RequirePremium = true)`.

**Consequence**: the source tabs are visible-but-gated on a Community instance, matching how rule-based
selection presents today (`PremiumFeatureNotice`), not hidden.

### D11 — A Release with no date is listed but cannot be bound

**Decision** (user, 2026-08-22), prompted by real data: of the three Releases on the demo instance,
**two carry no `releaseDate` at all** — the field is simply absent from the API response. A Jira version's
release date is optional, so this is the normal case, not an edge case.

A dateless Release appears in the picker, labelled as having no date set in Jira, and **cannot be
selected**. The user sets the date in Jira, where the date belongs.

**Why not make it bindable with no target**: `Delivery.Date` is non-nullable and every consumer assumes
it — the likelihood is a statement *about* a target, the overdue rendering needs one, and
`TargetDateAtSnapshot` records one daily. Making it nullable to represent "the remote has not decided
yet" is a domain change with a blast radius far larger than the empty field it models.

**Why not let the user type one**: it splits ownership of a bound Delivery across both sides, which is
exactly what D4 removes, and it needs a further rule for what happens when Jira later gets a date.

**Consequence**: the picker needs the dateless state as a first-class rendering, not a blank cell — and
this is the one place the feature asks the user to go and fix something in Jira, so it has to say so
plainly.

### D12 — A bound Release that loses its date becomes a broken source

**Decision** (user, 2026-08-22), closing a gap found in review. When a Release that a Delivery is bound to
has its `releaseDate` cleared in Jira, the Delivery enters the **same broken-source state D6 defines for a
deleted Release**: last known date, name and Features frozen, the state flagged with when it last synced
successfully, and Unbind offered.

**Why this needed deciding at all**: it falls through every other rule. The Version still resolves, so D6
does not fire. The read succeeded, so AC-03.6 does not fire. The date did not move into the past, so D5
does not fire. And D11 makes it *likely* rather than exotic — D11's remedy is "the user sets the date in
Jira", so the feature actively teaches the gesture that produces this state.

**Why the D6 state rather than a new one**: the frozen-and-flagged machinery already ships in slice 03, so
this costs a predicate rather than a mechanism. The **message differs** — "this Release no longer has a
date" is a different instruction to the reader than "this Release is gone" — but the behaviour is
identical, and one behaviour is easier to reason about than two.

**Why not keep the last date silently**: it produces a Delivery showing a date nothing maintains,
indistinguishable from a live one. That is the quiet wrongness the Epic exists to remove.

**Why not freeze membership too**: the Features are still perfectly syncable. Only the date is missing, so
only the date is stale.

---

## Wave: DISCUSS / [REF] User Stories

### US-01 — See what a Jira Release would give me

`job_id: job-forecaster-bind-a-delivery-to-the-release-it-tracks` · persona `delivery-forecaster` · slice 01a

As a delivery forecaster on a Jira-connected Portfolio, I want to browse the Releases that exist in the
connected project and see, for one of them, the date and the Features it would bring, so I can tell
whether a Release is a usable Delivery before committing to it.

#### Elevator Pitch

Before: I cannot see my Jira Releases in Lighthouse at all; to build a Delivery for one I re-type its
name, re-type its date, and hand-pick the Features I believe are in it.
After: open **Create Delivery** on a Jira Portfolio → a **Jira Release** tab sits beside Manual and Rules
→ pick *"2026 Q4"* → sees `Date: 2026-12-19 (from Jira)` and the 7 matching Features listed.
Decision enabled: whether the Release is ready to become a Delivery — and if it brings back
zero, that its Features have not been tagged with the version yet, which is a Jira-side fix, not a
Lighthouse one.

**AC-01.1** — On a Portfolio whose connection is Jira, the Delivery modal shows a third tab labelled
**Jira Release**. On an Azure DevOps, Linear, ServiceNow or CSV Portfolio, exactly two tabs show and no
Jira Release tab appears anywhere in the DOM.
**AC-01.2** — The tab list is rendered from what the server reports for that Portfolio, not from a
client-side test of the system type. Adding a second handler server-side adds a tab with no frontend change.
**AC-01.3** — Selecting the Jira Release tab lists the connected project's Releases with each one's date;
a Release with no `releaseDate` (the common case — two of three on the demo instance) is listed,
labelled as having no date set in Jira, and **not selectable** (D11). The label says where to fix it.
**AC-01.4** — Selecting one Release previews its date and the Portfolio Features matching it, using the
same Feature grid the Rules tab's validate step already uses.
**AC-01.5** — A Release matching zero Features previews an explicit empty state naming the reason
(no Feature in this Portfolio carries this Release), not a blank grid.
**AC-01.6** — On a Community licence the tab is visible and gated with the existing premium notice.
**AC-01.7** — Nothing is persisted by this story. Closing the modal leaves no Delivery and no Delivery change.

### US-02 — Create a Delivery from a Jira Release

`job_id: job-forecaster-bind-a-delivery-to-the-release-it-tracks` · persona `delivery-forecaster` · slice 01b

As a delivery forecaster, I want to save the previewed Release as a Delivery, so the Delivery is
identified by the Release rather than by a name I typed.

#### Elevator Pitch

Before: I create a Delivery by typing a name and a date that already exist in Jira, and the two copies
start diverging the same afternoon.
After: on the **Jira Release** tab, pick *"2026 Q4"* → **Save** → the Portfolio's Delivery grid shows a
Delivery named *2026 Q4*, dated `2026-12-19`, marked **from Jira Release**, with its name, date and
Feature list not editable.
Decision enabled: I stop maintaining the date, and anyone reading the Delivery can tell at a glance that
the target is Jira's number and not someone's guess.

**AC-02.1** — Saving persists the Delivery with a source-bound selection mode, the handler key, and the
Release's **id** (not its name — a Jira-side rename must not break the binding, D3.3).
**AC-02.2** — Name, date and Feature selection render read-only on a bound Delivery, each showing the
source it came from.
**AC-02.3** — Editing an existing Manual or Rule-based Delivery is unchanged: same tabs, same payload,
same behaviour as before this Epic.
**AC-02.4** — Unbinding a bound Delivery returns it to Manual with its last synced name, date and
Features retained and editable.
**AC-02.5** — The stored reference is the Version id, so a Jira-side rename leaves the binding intact
and resolvable. (What the rename does to the displayed *name* is AC-03.8, which needs a refresh path
this slice does not have.)

### US-03 — Keep the Delivery date in step with Jira

`job_id: job-forecaster-trust-the-delivery-date-without-rechecking-jira` · persona `delivery-forecaster` · slice 02

As a delivery forecaster, I want a bound Delivery's date and membership to follow Jira without my doing
anything, so I never present a forecast measured against a target that moved last week.

#### Elevator Pitch

Before: when a release date moves in Jira, Lighthouse keeps showing the old one until somebody notices
and re-types it — and nobody knows how long it has been wrong.
After: the date moves in Jira → within one Portfolio refresh the Delivery header shows the new date, and
the Delivery's history chart shows the target stepping on the day it moved.
Decision enabled: I can quote the likelihood in a review without first opening Jira to check the target
is still the target.

**AC-03.1** — On each Portfolio refresh, every source-bound Delivery re-reads its Release's date, name
and membership, and persists any change.
**AC-03.2** — A Release date that has moved into the past is accepted and the Delivery renders as
overdue (D5). Hand-entry on a Manual Delivery still rejects a past date, unchanged.
**AC-03.3** — A Feature gaining or losing the Release in Jira joins or leaves the Delivery on the next
refresh.
**AC-03.4** — A refresh in which nothing changed remotely persists no write and produces no history entry.
**AC-03.5** — The moved target is visible in the Delivery's existing metric history (`TargetDateAtSnapshot`,
S12) with no new storage.
**AC-03.6** — A Jira read failure during refresh leaves the Delivery on its last known values and does
not fail the Portfolio refresh.
**AC-03.7** — An archived Delivery (#5698 D1) is **not** re-synced; its pinned closure snapshot stands.
**AC-03.8** — A Release renamed in Jira updates the bound Delivery's displayed name on the next refresh;
the binding survives, because it was never keyed on the name (AC-02.1).

### US-04 — Be told when the Release is gone

`job_id: job-config-admin-know-when-a-delivery-source-degrades` · persona `config-admin` · slice 03

As the admin of a Lighthouse instance, I want a Delivery whose Release has been deleted to say so, so a
stale date is never mistaken for a synced one.

#### Elevator Pitch

Before: if the Release disappears there is no mechanism at all — the Delivery would keep displaying a
date that nothing is maintaining, indistinguishable from a live one.
After: delete the Release in Jira → the Delivery shows **Source unavailable — showing last synced values
from 2026-08-20**, with an **Unbind** action.
Decision enabled: whether to re-point the Delivery at a different Release or unbind it and take the date
back by hand — instead of trusting a number that stopped updating.

**AC-04.1** — When the bound Release cannot be resolved, the Delivery keeps its last synced date, name
and Features; nothing is cleared and nothing is deleted (D6).
**AC-04.2** — The Delivery displays a broken-source state naming when it last synced successfully.
**AC-04.3** — An **Unbind** action returns it to Manual with those values, editable.
**AC-04.4** — A connection that stops offering the capability at all (D2) degrades identically to a
deleted Release; it does not error and does not silently unbind.
**AC-04.5** — A transient read failure (AC-03.6) does **not** raise the broken-source state; only a
resolved "this Release does not exist" does.
**AC-04.6** — A bound Release whose `releaseDate` is **cleared** in Jira raises the broken-source state
(D12), with a message naming the actual cause — the Release lost its date — not the deleted-Release
message. Behaviour is identical: freeze, flag, offer Unbind.

### US-05 — See the Lighthouse forecast on the Release in Jira

`job_id: job-po-see-the-forecast-without-opening-lighthouse` · persona `product-owner` · slice 04

As a product owner who works in Jira, I want the Lighthouse forecast to appear on the Release itself, so
I can judge whether the plan is credible without opening a tool I do not use.

#### Elevator Pitch

Before: the forecast exists only in Lighthouse; everyone who plans in Jira either asks someone or plans
against the target date as if it were a forecast.
After: switch **Publish forecast to Jira** on for the Portfolio → open Release *"2026 Q4"* → its
description reads:
```
--- Lighthouse forecast · updated 22 Aug 2026 ---
70%: 4 Dec 2026 · 85%: 11 Dec 2026 · 95%: 19 Dec 2026
Likelihood of hitting 19 Dec 2026: 72%
--- end Lighthouse forecast ---
```
Decision enabled: whether to cut scope or move the Release date, taken by the people who own that call,
in the tool they already have open — and the spread across the three percentiles tells them how much
room there is to argue about it.

**AC-05.1** — Outbound is off by default and switched on per Portfolio (D8).
**AC-05.2** — When on, each bound Delivery's forecast is written to its Release on the same refresh
cycle that produces the forecast.
**AC-05.3** — The write targets a delimited block in the Release description and never `releaseDate` (D8).
**AC-05.3b** — The block carries, and is verifiable to carry, exactly four things (user, 2026-08-22):
  1. an unmistakable statement that **Lighthouse** wrote it,
  2. the **date it was written**,
  3. the Delivery's **three forecasts — 70%, 85%, 95%** — the same three
     `DeliveryWithLikelihoodDto` renders in the product (`CalculateMetrics(today, blackoutPeriods, 70, 85, 95)`),
     so the Release and the Lighthouse screen never show different sets,
  4. the **likelihood of hitting the target date**, with the target named.
**AC-05.3c** — The first line of the block leads with the attribution and the headline number, because
`description` is a column in the Releases list (observed 2026-08-22) and the column truncates. The full
block is read on the Release itself; the truncated column preview must still say something true and
attributable rather than a dangling fragment.
**AC-05.4** — The block is bounded by a **stable machine-detectable marker**, which is how a later write
finds the previous one. Re-writing replaces the block in place: never a second block, never any change to
text outside the markers. A description already holding the team's own text keeps it intact.
**AC-05.4b** — A Release with **no** description (the observed default — all three on the demo instance
had none) gets one created, not appended to.
**AC-05.4c** — A user hand-editing the text *inside* the block does not break the next write: it is
replaced wholesale, never merged, never treated as foreign text to preserve. **Conditional on slice 00**
**Q4** — if the markers do not survive a round trip through the Jira UI, DESIGN needs another way to
find its own previous write, and this AC is rewritten rather than met.
**AC-05.5** — Only Deliveries bound to a *live* Release are published. Three exclusions, and the last two
are the ones that bite:
  - Manual and Rule-based Deliveries — no Release to write to, skipped silently.
  - **Archived** Deliveries — an archived Delivery's numbers are a frozen closure snapshot, so publishing
    it would keep pushing a dead forecast into a live Jira Release forever. Archiving stops the
    machinery; it does not merely hide the row. Assumes #5698 archiving has shipped — see Pre-requisites.
  - **Broken-source** Deliveries (D6) — the Version id no longer resolves, so the write would PUT to
    something that is not there.
**AC-05.6** — A write that 404s (the Release was deleted between the read and the write) raises the same
broken-source state as a failed read (D6). It is not reported as a refusal — US-06 is about permission,
and conflating a missing target with a denied one sends the admin to fix the wrong thing.
**AC-05.7** — The write reuses the existing suppression posture (`notifyUsers=false` where the endpoint
accepts it, ADR-142) so publishing does not reintroduce the noise `quiet-jira-writeback` removed.

### US-06 — Be told when Jira refuses the write

`job_id: job-config-admin-know-when-a-delivery-source-degrades` · persona `config-admin` · slice 05

As the admin who owns the Jira connection, I want to be told that the credential cannot write Releases,
so I fix the permission instead of assuming the feature is broken.

#### Elevator Pitch

Before: a 403 on a Version write would be invisible — the switch is on, nothing appears in Jira, and
there is nothing anywhere that says why.
After: open the work tracking connection → sees **Cannot publish to Releases — the credential lacks
Administer Projects on PROJ. Last refused 2026-08-22 14:02.**
Decision enabled: grant the permission, or switch publishing off and stop expecting it to work.

**AC-06.1** — A refused Version write is surfaced against the connection, naming the project and the time.
**AC-06.2** — A refusal does not disable publishing, does not fail the Portfolio refresh, and does not
retry in a tight loop.
**AC-06.3** — A subsequent successful write clears the state.
**AC-06.4** — Inbound continues to work while outbound is refused — reading Releases and writing them are
separate capabilities (D2, and the Epic's own "an adapter may read without being allowed to write").

---

## Wave: DISCUSS / [REF] Definition of Done

1. - [ ] Jira Release tab appears only where the connection offers it; four other connectors show two tabs.
2. - [ ] A Delivery can be created from a Release with date and membership from Jira, both read-only.
3. - [ ] Date, name and membership re-sync on the Portfolio refresh cycle; past dates accepted; archived Deliveries skipped.
4. - [ ] A deleted Release freezes the Delivery and says so; unbind returns it to Manual intact.
5. - [ ] Forecast publishes to the bound Release's description behind a per-Portfolio switch.
6. - [ ] A refused write is reported against the connection and does not break inbound or the refresh.
7. - [ ] Backend `dotnet build` zero warnings, `dotnet test` green; frontend `pnpm test`, `pnpm build`, Biome clean.
8. - [ ] Mutation testing at or above 80% on the new backend and frontend surfaces.
9. - [ ] Docs updated per-feature with screenshots, using the configurable Terminology defaults (S14).

---

## Wave: DISCUSS / [REF] Out of scope

- **Azure DevOps, Linear, ServiceNow and CSV handlers.** The port and registry accommodate them; no second
  adapter ships here. ADO has no first-class Release — mapping one onto Iterations or a tag is its own
  modelling decision, taken on real demand (D1, S13).
- **Creating Deliveries in bulk from Releases (import).** Slice 01b binds one Delivery to one Release.
  Import inherits that binding and can follow later.
- **Hand-overriding membership on a bound Delivery.** Excluded by D4; the escape hatch is unbind or Rules.
- **Writing `releaseDate` in Jira.** Excluded by D8 — that is the field the remote system owns.
- **Overriding `releaseDate` with a chosen forecast percentile.** Raised and then dropped by the user on
  2026-08-22 — not scheduled, not a backlog item. The note under D8 records why it is not a checkbox on
  this feature if it ever comes back: reading and writing the same field closes a loop that makes
  "likelihood of hitting the target" self-referential.
- **Creating or deleting Releases from Lighthouse.**
- **Jira Data Center verification of the outbound permission bar.** Same posture `quiet-jira-writeback`
  took: design for Cloud, verify DC post-release, and if DC differs that is a dedicated feature.
- **A new sync schedule or a manual "sync now" button.** D9 rides the existing refresh.

---

## Wave: DISCUSS / [REF] WS strategy

**Type A (additive), with one in-slice precursor.** No existing endpoint changes contract, no existing
Delivery behaviour changes, and Manual/Rules are untouched throughout. Two additive columns on `Delivery`,
one appended enum member (S3 — append only, EF stores the int), new endpoints only.

The one non-additive move is S4: the frontend's binary Manual/RuleBased branch must become list-driven.
That lands **as the opening commit of slice 01a**, not as its own slice — a refactor-only slice would
contain no user-visible value story and fails the slice composition hard gate.

Walking skeleton = **slice 01a**: server reports the Portfolio's available sources → the tab renders from
that list → a real Jira Release is read and previewed with its real date and its real matching Features.
That is the whole port exercised end to end, against production data, with nothing persisted.

---

## Wave: DISCUSS / [REF] Driving ports

Every controller here carries **both** `api/v1/...` and `api/latest/...` (`DeliveriesController.cs:14-15`,
`DeliveryRulesController.cs:14-15`). The `v1` form is written below for brevity; every route is served on
both.

| Method | Route | Auth | Status | Change |
|---|---|---|---|---|
| GET | `api/v1/portfolios/{portfolioId:int}/delivery-sources` | `PortfolioRead` | **New** | `[{ key, displayName }]` — the source handlers this Portfolio's connection offers. `[]` for AzureDevOps, Linear, ServiceNow, CSV. Answered per connection (D2), not per connector. Not Premium-gated, mirroring `delivery-rules/schema`, so the tab can render its gated state. |
| GET | `api/v1/portfolios/{portfolioId:int}/delivery-sources/{sourceKey}/options` | `PortfolioWrite` | **New** | `[{ id, name, date? }]` — the Releases available to bind; `date` absent where Jira has none (D11). `404` for an unknown or unoffered key. |
| POST | `api/v1/portfolios/{portfolioId:int}/delivery-sources/{sourceKey}/preview` | `PortfolioWrite` + Premium | **New** | Given a source reference, returns the date and matching Features. Mirrors `delivery-rules/validate` (`DeliveryRulesController.cs:41`) so the preview grid is the one that already exists. |
| POST | `api/v1/deliveries/portfolio/{portfolioId:int}` | `PortfolioWrite` | Existing | **Extended** — accepts a source-bound selection mode with `sourceKey` + `sourceReference`. Manual and rule payloads unchanged. Route verified at `DeliveriesController.cs:14,75`. |
| PUT | `api/v1/deliveries/{deliveryId:int}` | `PortfolioWrite` (enforced in the body, `DeliveriesController.cs:157`) | Existing | **Extended** — same, plus unbind (switch back to Manual retaining values). Route verified at `DeliveriesController.cs:134`. |

The two **New** `delivery-sources` routes are written above as Portfolio-nested because that is how
`DeliveryRulesController` already nests (`api/v1/portfolios/{portfolioId:int}/delivery-rules`). Note the
deliveries controller itself does **not** nest that way — it is `api/v1/deliveries/portfolio/{id}`. DESIGN
should pick one and say so rather than inherit the inconsistency by accident.

Port additions on `IWorkTrackingConnector` (shape follows S7):

- `bool SupportsDeliverySources(WorkTrackingSystemConnection connection)` — inbound capability.
- `bool SupportsDeliveryForecastPublishing(WorkTrackingSystemConnection connection)` — outbound capability,
  **separate** from inbound (D2, US-06 AC-06.4).
- a read for the available sources and their dates, and a resolve for one bound source.
- a publish for the forecast, which is **not** a `WriteBackFieldUpdate` (D7).

UI surfaces: `DeliveryCreateModal` (tab list becomes data-driven), `DeliverySection` / `DeliveryHeader`
(read-only + provenance + broken-source states), the Portfolio settings surface (outbound switch), the
work tracking connection surface (refusal report).

---

## Wave: DISCUSS / [REF] Pre-requisites

- **#5698 archiving ships before this Epic starts** (user, 2026-08-22 — "archiving will be shipped once we
  start the work on this epic, assume it's there"). It is **not in the codebase today** (S15): `Delivery`
  has no archive field and #5698's slices 04-05 are outstanding. Two requirements here assume it: AC-03.7
  (an archived Delivery is not re-synced, or the pin is not a pin) and AC-05.5 (an archived Delivery does
  not keep publishing a frozen forecast into a live Jira Release). Both are written as normal
  requirements on that basis. **If the sequencing changes, they are the first things to re-open** — and
  slice 02 is where the assumption first has to hold.
- **Slice 00 SPIKE must complete before slice 04 is designed.** D7 and D8 rest on
  unverified Jira behaviour; `quiet-jira-writeback` lost a slice and three pre-commitments to exactly
  this omission.
- A Jira Cloud project with Releases and at least one Feature carrying a `fixVersion`
  (`letpeoplework.atlassian.net`).
- Premium licence fixture (gitignored; import it from the main checkout before any screenshot run).

---

## Wave: DISCUSS / [REF] Scope Assessment: PASS with a caveat

6 stories, 7 slices (one a SPIKE), 3 bounded contexts (Delivery/Portfolio, WorkTracking connector,
WriteBack). Two of the five oversized signals are borderline: three contexts, and two independently
shippable outcomes (inbound is releasable without outbound). **Right-sized as one Epic** because the
conflict policy D4 must be decided once for both — that is the reason #4463 was absorbed (D0), not a
scope grab. If slice 00 shows the Version write permission bar is prohibitive, slices 04-05 detach
cleanly and inbound still ships whole.

---

## Wave: DISCUSS / [REF] Outcome KPIs

| KPI | Target | Measurement |
|---|---|---|
| Hand-maintained dates removed | at least 1 Portfolio on the dogfood instance has a source-bound Delivery within a day of slice 01b | Count Deliveries with a non-null source key |
| Date drift eliminated | 0 occurrences of a bound Delivery's date differing from its Release's date after one completed refresh | Compare stored date vs remote date in the slice-02 acceptance run |
| Sync is not a cost | Portfolio refresh duration with 5 bound Deliveries within 5% of the same Portfolio unbound | `RefreshLogService` duration, before/after |
| Outbound reaches its reader | The forecast block is present and current on the bound Release in Jira within one refresh of the switch going on | Read the Version description in the slice-04 acceptance run |
| Refusal is legible | 100% of refused writes produce a connection-level report naming project and time | Slice-05 acceptance run against a credential without Administer Projects |

---

## Wave: DISCUSS / [REF] Slices

| # | Slice | Ships | Learning hypothesis |
|---|---|---|---|
| 00 | `slice-00-spike-jira-release-reality-check` | Findings only (timeboxed) | Disproves that Jira Releases are reachable, Feature-level, and writable on the terms D1/D7/D8 assume |
| 01a | `slice-01a-see-what-a-release-would-give-you` | Registry-driven tabs + Release list + preview | Disproves that a server-reported source list degrades correctly — the property that makes every later handler cheap — and, secondarily, the version-matching code if `fixVersions` is not shaped as assumed |
| 01b | `slice-01b-create-a-delivery-from-a-release` | Persisted binding, read-only fields | Disproves that read-only date and membership is acceptable if the first user immediately wants to override |
| 02 | `slice-02-keep-the-date-in-step` | Refresh-cycle re-sync, past dates | Disproves D9's "no new scheduler" if refresh-interval latency is too slow to be trusted |
| 03 | `slice-03-say-so-when-the-release-is-gone` | Broken-source state + unbind | Disproves D6 if freezing reads as "working" and users prefer an auto-unbind |
| 04 | `slice-04-publish-the-forecast-to-the-release` | Outbound to the Version description | Disproves D7/D8 if the description is unusable as a surface or the permission bar is prohibitive |
| 05 | `slice-05-say-so-when-jira-refuses` | Connection-level refusal report | Disproves that refusal reporting is needed at all if slice 04's permission bar turns out to be low |

Briefs: `docs/feature/epic-5565-delivery-date-sync/slices/`.

**ADO Stories (created 2026-08-22, all `New`, all children of #5565).**

| Slice | Story | Title |
|---|---|---|
| 00 | #5827 | Jira Release reality check (SPIKE) |
| 01a | #5828 | See what a Jira Release would give a Delivery |
| 01b | #5829 | Create a Delivery from a Jira Release |
| 02 | #5830 | Keep the Delivery date in step with Jira |
| 03 | #5831 | Say so when the bound Jira Release is gone |
| 04 | #4463 | Delivery Write-Back (pre-existing; description filled, absorbed per D0) |
| 05 | #5832 | Say so when Jira refuses the Release write |

**Membership convention (user, 2026-08-22).** Tagging the Portfolio's Features with the version is a
convention the customer owns, not a discovery problem for Lighthouse. The handler matches at the level
Lighthouse tracks and does **not** roll up from children; an item without the version is simply not in
the Release, exactly as a Feature failing a rule is not in a rule-based Delivery. This retires what was
the Epic's largest open risk: slice 00 Q1 drops from a gate to a shape confirmation, and slice 01a's
empty-match case becomes a Jira-side gap to name rather than a design threat.

### Carpaccio taste tests

| Test | Verdict |
|---|---|
| Any slice shipping 4 or more new components? | 01a is the fattest — registry endpoint, options endpoint, connector read, tab refactor. **Documented exception**: the refactor is a precursor commit inside the slice (WS strategy), and splitting it out would produce a value-free slice. |
| Every slice depends on a new abstraction? | No. The abstraction (registry + capability flags) ships **inside** slice 01a, justified by the degradation requirement, not ahead of it as speculation. |
| Any slice disproves a pre-commitment? | Yes, all seven — see the table. 00 targets the outbound permission bar and write surface, the two remaining unknowns. |
| Synthetic data only? | No. 00 and 01a run against `letpeoplework.atlassian.net` with real Releases; every later slice inherits that. |
| Two slices identical but for scale? | No. |
| Any slice with only `@infrastructure` stories? | No. 00 is a SPIKE, which is findings-not-release and outside the gate; every shipping slice carries a value story. |

### Prioritisation rationale

- **00 first** on learning leverage. Two decisions (D7 write target, D8 permission bar) rest on
  unverified Jira behaviour. `quiet-jira-writeback` shipped a slice built on an unverified premise and
  had to delete it. A timeboxed probe is the cheapest place for those to be wrong. D1's membership
  question came off this list on 2026-08-22 — see the note under the slice table.
- **01a before 01b** because 01a shows a real Release's real date and Features with nothing persisted,
  so the first look at production data costs nothing to throw away.
- **02 before 03** because the broken-source state is only meaningful once something is actually syncing.
- **Inbound (01a-03) entirely before outbound (04-05)** because D4 — the conflict policy — is established
  by inbound, and outbound's whole justification (D8: never write the field the remote owns) is a
  consequence of it.
- **05 last** and possibly unnecessary: if slice 00 finds the write needs only a permission most
  credentials already hold, 05 shrinks to a log line.

---

## Wave: DISCUSS / [REF] Definition of Ready

| # | Item | Evidence |
|---|---|---|
| 1 | Business value articulated | Epic description plus four job stories, all traced to `docs/product/jobs.yaml` |
| 2 | User stories with job traceability | US-01 to US-06, each carrying a real `job_id`; no `infrastructure-only` used |
| 3 | Acceptance criteria testable | 41 ACs, each naming an observable outcome; none says "works correctly" |
| 4 | Elevator pitch per story | Six, each naming a real entry point and real observable output |
| 5 | Dependencies identified | Pre-requisites section — the #5698 archive interaction is the load-bearing one |
| 6 | Slices sized at or under 1 day | Seven briefs, each with an estimate; 01a's exception documented in the taste tests |
| 7 | Outcome KPIs measurable | Five, each with a numeric target and a named measurement source |
| 8 | Out-of-scope explicit | Eight exclusions, each with a reason |
| 9 | Technical feasibility | Verified against code (S1-S15). The two unverified Jira behaviours are quarantined into slice 00 rather than assumed. S15 (archiving absent today) is covered by a stated sequencing commitment, not by hope — see Pre-requisites. |

**Score: 0.94**, revised from a self-assessed 0.96 after review, then partly restored once the two gaps
review found were closed by decision (D12) and by an explicit sequencing commitment on #5698 archiving.
The remaining 0.06 is slice 00 Q3 and Q4 — the outbound permission bar, and whether a marker survives a
round trip through the Jira UI. Both sit in a SPIKE rather than in a requirement, which is the point.

---

## Wave: DISCUSS / [REF] Wave Decisions Summary

### Key Decisions

- **[D0]** Both directions in one Epic; #4463 absorbed as slice 04's Story — conflict policy must be
  decided once with both ends visible.
- **[D1]** A Delivery source is a registered handler and a third selection mode, owning membership **and**
  date; the registry ships in slice 01a because it is what makes the tab absent elsewhere.
- **[D2]** Capability declared per connection, following `SupportsIncrementalSync` (S7); inbound and
  outbound are separate flags.
- **[D3]** A Release handler is not a rule — no date, array-vs-string matching, name-not-id, no enumeration.
- **[D4]** Remote always wins; bound date and membership are read-only. The conflict class is removed, not managed.
- **[D5]** A synced past date is accepted and shown overdue; the future-date invariant moves to hand entry.
- **[D6]** A vanished Release freezes the Delivery and says so; never auto-unbind, never delete.
- **[D7]** Outbound writes the Version, not the member issues.
- **[D8]** Outbound is opt-in and writes the description, never `releaseDate`.
- **[D9]** Inbound rides the existing Portfolio refresh at `PortfolioUpdater.cs:73-79`.
- **[D10]** Premium, gated the way rule-based selection already is.
- **[D11]** A Release with no `releaseDate` is listed but not selectable — the remote owns the date, and
  if the remote has not set one there is nothing to take. Two of three Releases on the demo instance are
  in this state, so it is the common path.
- **[D12]** A bound Release whose date is cleared becomes a broken source — the same freeze-and-flag state
  as a deleted Release (D6), with a different message. Closes a gap review found: the case fell through
  D5, D6 and AC-03.6, and D11 makes it likely by telling users to go set the date in Jira.

### Requirements Summary

- **Primary jobs**: stop maintaining a date twice (inbound); stop presenting a forecast against a target
  that may have moved (sync); let Jira-native readers see the forecast where they work (outbound); and be told when any of it stops working (degradation).
- **Walking skeleton**: slice 01a — server-reported source list, data-driven tabs, real Jira Releases
  previewed with real dates and real matching Features, nothing persisted.
- **Feature type**: cross-cutting (domain model + connector port + frontend + write path).

### Constraints Established

- `DeliverySelectionMode` is int-persisted with no conversion — **append only** (S3, the same append-only
  rule already pinned on `WorkTrackingSystems`).
- The identity sweep stays `key,updated` (S6). The full fetch is `*all`, so `fixVersions` needs **no**
  `fields=` widening — the same trap already recorded for Jira issue links.
- Registering a handler in `Program.cs` pulls in the full backend Integration suite; expect the longer
  run and its wider flake exposure.
- Delivery/Deliveries and Feature/Features are configurable Terminology; Jira's "Release" is a literal
  remote value and stays as written (S14).

### Upstream Changes

None. No DISCOVER or DIVERGE artifacts existed for this Epic. #5698's archive decision is **consumed
unchanged** and constrains slice 02 — see Pre-requisites and AC-03.7.

---

## Tier-2 expansion menu

Two triggers fired under `ask-intelligent`:

- **Cross-context complexity** — 3 bounded contexts (Delivery/Portfolio, WorkTracking, WriteBack) and 3
  technologies (C# domain, React modal, Jira REST) → suggests **`alternatives-considered`**: the rejected
  alternatives behind D1 (rule-with-a-label), D4 (last-write-wins, flag-and-ask), D7 (write the issues)
  and D8 (write `releaseDate`).
- **Multi-stakeholder need** — 4 personas across the stories, 3 of them primary → suggests
  **`persona-narrative`**: the Jira-native `product-owner` who never opens Lighthouse is the least
  documented and the one US-05 exists for.

Ask for either by name to render it.

---

## Wave: DISCUSS / [WHY] Persona narrative

Rendered on request (user, 2026-08-22). Tier-2 expansion `persona-narrative`, triggered by
multi-stakeholder need — four personas across six stories, three of them primary.

Three of the four are documented personas doing a recognisable version of what they already do here. The
fourth is not, and is the reason this expansion is worth reading.

### The persona this Epic adds: the Jira-native planner

`product-owner` already exists in `docs/product/personas/product-owner.yaml`, described entirely as a
Lighthouse user — someone who opens the tool to deep-dive an outlier item. US-05 is addressed to a
**variant of that persona who never opens Lighthouse at all**, and every other persona in the product's
SSOT is defined by something they do inside the product. That makes this one structurally different, and
easy to get wrong.

**Where they are.** In Jira, on the Releases page, planning against `2026 Q4`. That page shows them a
name, a date, and a progress bar. They have no account on the Lighthouse instance and no reason to want
one.

**Their mental model, and the specific error in it.** They read the Release date as a *prediction*. It is
a **commitment** — a date somebody agreed to, which says nothing about whether it will be met. Nothing in
their tool distinguishes the two, so the distinction does not exist for them. This is not ignorance; it
is the only model the available information supports.

This matters more than it first looks. The product's whole thesis is that a target and a forecast are
different kinds of statement. The person making the most consequential scope decisions is, today,
structurally unable to hold that distinction — not because they disagree, but because they have never
been shown two numbers at once.

**Their vocabulary.** "Release", "fix version", "the date", "are we going to make it". Not: Delivery,
likelihood, percentile, Monte Carlo, throughput. The published block has to survive being read by someone
who has never seen the word "percentile" — which is a constraint on **how the three forecasts are
written**, not an argument for publishing only one.

`70%: 4 Dec · 85%: 11 Dec · 95%: 19 Dec` is three **dates**, each with a confidence attached. That reads
as "the later we promise, the safer we are", which is a sentence anyone can follow without knowing what a
percentile is. The same information as a statistical table would not be. So the rule the vocabulary
imposes is: **label dates with confidence, never present confidence as the subject.**

Three of them rather than one earns its place with this reader specifically, because the spread *is* the
message. A single date invites "so it's the 11th then" — the exact false precision the product exists to
argue against. Three dates make the uncertainty visible without ever naming it, and the distance between
the 70th and the 95th is what tells them how much room there is to negotiate. That is the decision this
persona is there to make.

**What they will actually do with it.** Nothing, most of the time — which is the correct outcome and
worth stating, because it is easy to mistake for failure. The value is concentrated in the minority of
Releases where the forecast and the target visibly disagree. On those, the decision is cut scope or move
the date, and it is theirs to make. The block's job is to put that disagreement in front of them at the
moment they are already looking, not to be read every day.

**Why they are reachable only by outbound.** Every other persona here can be served by a screen. This one
can only be served by Lighthouse writing into somewhere else. That is the entire justification for
slices 04-05 existing at all, and it is why D7 puts the number on the Release rather than on the member
issues: the Release is the object they have open.

**What would make it worse than nothing.** Three things, each already a locked decision:

- Overwriting `releaseDate` (excluded by D8). They would lose the commitment they were planning against,
  and the two kinds of statement would collapse into one — the exact confusion the block exists to fix,
  now caused by us.
- An unlabelled or stale number. A date with no "as of" reading as current when it is weeks old is worse
  than no date, because it is trusted. Hence the write date required by AC-05.3b and the replace-not-append
  rule in AC-05.4.
- Notification noise. A watcher emailed on every refresh will mute the Release, and then they see nothing
  at all. Hence AC-05.6 inheriting the suppression posture.

### The three documented personas, and what is different here

**`delivery-forecaster`** (primary, US-01 to US-03) is doing what they already do — maintaining a
Delivery — with one behavioural change: they stop being the integration. Today they are the mechanism by
which a date moves from Jira to Lighthouse. The Epic removes the job, not just the effort. The residual
worry is control, which is why unbind exists and returns everything intact; a forecaster who cannot get
the date back will not hand it over in the first place.

**`config-admin`** (primary, US-04 and US-06) has an established pattern in this codebase: they are the
persona who gets told when a capability is not available, and `job-config-admin-know-writeback-is-quiet`
is the direct precedent. Their question is never "what happened" but "where do I fix it", so a message
that names the project and the time is worth more than one that explains the failure. Their standing
anxiety is alarm fatigue, which is why AC-04.5 draws a hard line between a transient read failure and a
resolved deletion.

**`delivery-lead-rte`** (secondary) does not appear in a story and needs no new capability. They are the
person in the review being told a likelihood, and their benefit is entirely second-order: the target
stops being arguable, so the conversation moves to the forecast. Worth naming because if a future slice
proposal claims to serve them directly, that claim should be checked — nothing in this Epic does.

### The one that is not here

There is no persona for "the person who set this up and left". The Epic assumes the admin who binds a
Delivery is around when it breaks. `job-config-admin-know-when-a-delivery-source-degrades` is written to
be legible to a successor — the state names when it last synced, not just that it failed — but nothing
proactively reaches someone who is not looking. That is an accepted gap, not an oversight, and the place
it would be closed is instance-level health reporting, not this Epic.

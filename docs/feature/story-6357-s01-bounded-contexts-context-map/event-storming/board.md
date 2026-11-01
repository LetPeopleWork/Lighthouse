# Big Picture Event Storming — session board

Session with the maintainer, started 2026-11-01. This board merges two walls:

- the maintainer's own chaotic-exploration list (written before seeing the code), and
- the code seed in [`board-seed.md`](board-seed.md) (status quo, every sticky cited there).

Provenance marks: **[M]** maintainer only (the code has no name for it), **[M+C]** both, **[C]** code only
(listed here only when it matters for the discussion; the full code wall stays in the seed).
Sticky types follow the seed: EVENT, IMPLICIT-EVENT (happens, no event type), COMMAND, POLICY, EXTERNAL,
READ-MODEL, ACTOR, HOTSPOT. A `?` marks an open question for the walk.

## Phase 1 — the maintainer's list, sorted into the seed's sections (raw, before the walk)

### A. Install & platform
- [M+C] Lighthouse was opened and nothing is configured yet — code: *Instance Installed* (seeded install timestamp), no event
- [M+C] A licence was loaded — code: *Licence Imported*, no event
- [M] Licence has expired — code: derived on read, no event, nobody reacts
- [M] Licence entered the last 30 days of validity — code: nothing
- [M+C] An admin changed some system settings — code: refresh cadence, log level, optional features; silent except Feature ordering
- [M+C] Terminology has changed — code: only *Default Terminology Seeded*; a change is silent

### C. Connect a tracker
- [M+C] A connection to a work tracking system was created — code: implicit, usage data has `WorkTrackingSystemConnected`
- [M+C] A connection could not be created because something was off — code: *Connection Validation Failed*, answered in the response only
- [M+C] A tracking connection stopped working (invalid/expired PAT, …) — code: *Connection Health Verdict Recorded*, *Credential Became Unreadable*, no event
- [M+C] A new "additional field" was added — code: part of *Change Connection*; may emit *Connection Fetch Shape Changed*

### D. Configure Teams & Portfolios
- [M+C] A Team was created / a Portfolio was created — code: implicit, usage-data names only
- [M+C] A Team was updated / Team settings changed successfully — `?` one event or two
- [M+C] Team settings could not be validated/changed — code: *Validate Team Settings* answer, or a 409 on a lost race
- [M+C] Portfolio settings changed successfully / could not be validated/changed
- [M+C] Features got re-ordered — code: **EVENT** *Feature Rank Changed*, *Feature Ordering Policy Changed*
- [M] A new "hierarchy level" was introduced — Epic 6455, does not exist yet

### E. Sync
- [M+C] Manual refresh was triggered / scheduled refresh was triggered — code: commands and timer policies
- [M+C] A Team refreshed its data from the tracker — **EVENT** *Team Data Refreshed*
- [M+C] Portfolio data was refreshed — **EVENT** *Portfolio Features Refreshed*
- [M+C] A refresh of data failed (Teams or Portfolios) — code: health verdict + refresh log, no event
- [M+C] An item was going stale — **EVENT** *Work Item Became Stale*, **nobody listens**
- [M+C] An item was blocked — **EVENT** *Work Item Blocked* / *Feature Blocked*
- [M+C] Data was written back to the original tracker — code: *Write-back Staged*, no event

### F. Metrics
- [M] A coach was analysing the flow metrics — activity, not a fact: becomes ACTOR *Coach* + READ-MODEL metrics
- [M] The full Team was analysing the flow metrics — ACTOR *Team* + same READ-MODEL; `?` is "metrics were reviewed" a fact anyone reacts to
- [M] A signal changed state (red, amber, green) — code: computed on read (SLE risk, likelihood colours), no event, no history

### G. Forecasting
- [M+C] A forecast for a Team was run — code: *Manual Forecast Run*, answered and forgotten
- [M+C] A Portfolio was (re)forecast — **EVENT** *Portfolio Forecasts Updated*
- [M] A Feature's date changed — code: none; forecasts are replaced whole, nobody compares old and new

### H. Deliveries
- [M+C] A Delivery was created
- [M+C] A Delivery's target date changed — code: *Change Delivery* (reschedule)
- [M+C] A Delivery's Features changed — code: *Change Delivery* (re-select), *Rule-based Delivery Recomputed*, *Synced From Its Source*
- [M] A Delivery's date/likelihood changed — code: none (same as the Feature date)
- [M] A Delivery ran past its target date — code: none
- [M] A PO planned their PI using Deliveries — activity: ACTOR *PO*, READ-MODEL Deliveries
- [M] A project manager wanted to know when the release is ready — question: ACTOR *Project manager*, READ-MODEL Delivery forecast
- [M] A customer looked at the promised Delivery — ACTOR *Customer* (outside the org), READ-MODEL; `?` through what (embed, a shared link, a screenshot)

### J. Reports
- [M+C] A report was created — code: implicit, usage data `ReportCreated`
- [M] A report was exported — `?` check against the bulk-report export
- [M+C] A consultant proved their effect by comparing metrics at two points in time — this is the Then & Now report's job

### Hot spots raised by the maintainer
- [M] HOTSPOT `?` Rethink Team / Portfolio as generic levels: one "team level" where the work is done and forecast,
  and 1..n levels above it?
- [M] HOTSPOT Someone connected Initiatives (C-level) directly to Teams (the forecast level) — the level jump

## Phase 1 — first observations (for the walk)

1. **The maintainer thinks in changes, the code thinks in recomputation.** Five of the maintainer's events are
   "X changed" or "X crossed a line": a Feature date changed, a Delivery date/likelihood changed, a signal went
   red/amber/green, a Delivery ran past its target, a licence entered its last 30 days. The code has none of these. It
   publishes "forecasts updated" (a fresh snapshot) and computes signals on read. A change is something a user
   reacts to; a recomputation is not. This is a candidate language shift.
2. **Activities point to actors and read models.** "A coach was analysing…", "a PO planned their PI…", "a customer
   looked at…" are not facts the system can record, but they name who decides with which view: coach, Team, PO,
   project manager, customer, consultant. The code seed has none of the last four.
3. **The code has a lot of plumbing no user would name.** Key ring, forecast fingerprints, orphan clean-up,
   update-queue statuses. That is a hint (not yet a decision) that part of section A is a generic/supporting
   area, not core domain.

## Phase 2 — enforce the timeline (walk, section by section)

Decisions from the walk are recorded here, section by section.

### A. Install & platform — walked 2026-11-01

**Licensing is an upstream that others react to** (maintainer). It is not just a rule checked on read:

- EVENT *Licence Loaded* → Premium features become available.
- EVENT *Licence Entered Its Last 30 Days* → POLICY: the licence icon turns orange, with a message and a renew link.
- EVENT *Licence Expired* → POLICY: Premium features go away; if more Teams/Portfolios exist than the free tier
  allows, **all** data refreshing stops; the icon turns red, with a message and a renew link.
- HOTSPOT: in code all three facts are derived on read, and the limits are checked in three places with three
  numbers (create guard 2, refresh guard, updaters skip above 3 Teams / 1 Portfolio). Does "all refreshing stops"
  hold in every one of those paths? Verify at the ADR, do not fix here (behaviour).

**Platform plumbing is not part of the story** (maintainer). Key ring, backups/restore, demo data, version update,
log level, usage-data consent and start-up housekeeping move to a separate **platform lane** below the timeline.
Input for the open question "Platform/Persistence: a module or composition-root infrastructure?": the storming
puts it outside the domain timeline.

**Terminology and hierarchy levels are "how the organisation is set up", per installation** (maintainer). An
organisation that says "Initiative" says it across the whole instance, and it is fair to support one structure per
installation. Terminology therefore moves from section A to section D, next to "a new hierarchy level was
introduced", under a new heading *Organisation set-up*.

- HOTSPOT [6455]: this **reverses the DIVERGE recommendation** for Epic 6455, which declares levels per Portfolio
  (`../../epic-6455-deeper-hierarchies/recommendation.md`). It matches the recorded dissent there (the
  instance-wide level model) and answers its open question 5 ("levels per Portfolio now, or once?") with "once,
  per installation". To be carried into 6455's DISCUSS.
- Candidate language cluster: *organisation structure* (levels, their names, terminology). The first concept the
  storming found that has no home in today's nine modules.

### C. Connect a tracker — walked 2026-11-01

**A broken connection is one fact, and someone must be told** (maintainer). Causes vary (Jira DC down for
maintenance, expired PAT, other), so the fact is about the connection, not about each failed refresh.

- EVENT *Connection Stopped Working* (with a reason) → POLICY: tell whoever owns the connection so it can be fixed.
  Also *Connection Started Working Again* as its pair.
- HOTSPOT: today this is a health verdict recorded per refresh plus failures Team by Team; no single fact exists.
- HOTSPOT `?`: who *owns* a connection? Today connections are administered instance-wide; the Teams/Portfolios
  that use one are called its "owners" in code (they inherit its defaults). Owner-of-the-credential and
  user-of-the-connection are two different words in the making.

**Mapping and translation are two different things** (maintainer, a new distinction):

- **Mapping** (which tracker types, states and fields count as Work Item, Feature, In Progress, Done, blocked…)
  needs a lot of user involvement and describes how the organisation works. It joins **Organisation set-up**,
  next to Terminology and the hierarchy levels.
- **Translation** (connectors turning each tracker issue into a Work Item or Feature during a fetch) is plumbing
  that "just works" once the mapping is known. It is the **anti-corruption layer** in front of Jira, ADO, Linear,
  CSV and ServiceNow.
- Consequence for the context map: the ACL is *downstream* of Organisation set-up for its configuration (it
  applies the mapping) and *upstream* of Sync for its data. Today both live together in Team/Portfolio/Connection
  settings and the connectors.
- `?` for D: state mapping is set **per Team** today, while the maintainer places Organisation set-up **per
  installation**. Which parts of the mapping are org-wide and which stay per Team? *Answered in the interlude below.*

### Interlude (before D) — who decides which setting — walked 2026-11-01

The maintainer: Teams and Portfolios (old names, may change) keep flexibility, because ways of working differ
inside one organisation. Settings sorted into three buckets:

| Bucket | Settings | Note |
|---|---|---|
| **1. Org-wide, fixed** (one per installation) | Terminology · hierarchy levels and their names · blackout periods · Feature ordering policy · refresh cadence | |
| **2. Org default, local override** | Work Item types · Feature types · state mapping · blocked rules · staleness threshold · SLE · default Feature size · Parent Override Field | Most settings land here. Work Item types: "pre-selection is neat, but Teams must be able to ignore things". Feature types: with 6455's levels, each level lists the types that may appear on it, so set-up is guided and unfitting types cannot be added at random. |
| **3. Local only** | Feature WIP · throughput window | "Highly specific to Teams. Interesting to *see* across Teams/Portfolios, but setting it is local." |

What the sorting shows:

1. **Bucket 2 is the dominant pattern: a way-of-working default that a unit may deviate from.** Lighthouse
   already has this mechanism, but at the **connection** (Parent Override Field, estimation:
   `SettingSource.ConnectionDefault`, `Team.FollowsConnectionDefault`). The sorting moves the default's home up to
   the organisation. HOTSPOT `?` (maintainer): org default or per-tracker default? Usually there is one tracker, so
   they coincide; with two trackers the type and state names differ, so a per-connection layer may still be needed.
2. **Bucket 3 holds exactly the forecasting inputs of the unit that has throughput** (how many Features at once,
   how much history). Not org set-up, not way-of-working: they describe the Team as a *capacity* for the
   simulation. Candidate: these belong to Forecasting's view of a Team.
3. **Bucket 1 beyond the names is calendar and cadence** (blackouts, refresh cadence, ordering policy). Org-wide
   rules about time and priority.
4. Context-map edge: **Organisation set-up (upstream) → Team / Portfolio way of working (downstream, may
   override)**. Today, settings in buckets 1 and 2 are split across instance settings, connection defaults and
   per-Team/Portfolio settings.
5. **This is the target language, not a behaviour change inside S01** (the note closing section D says how).

### D. Configure Teams & Portfolios — core questions walked 2026-11-01

**Q6 — levels are paired: a unit owns a unit of work** (maintainer, "somewhat Flight Levels"). Each level is a
pair of an *owning unit* (a group of people, a board) and the *unit of work* assigned to it:

```text
  level        owning unit (people)          unit of work
  ─────        ────────────────────          ────────────
  n  strategic  Program / portfolio board  ← Strategic Initiative / Project / Business Epic
  2  coordination  ART / value stream      ← Change / Feature (SAFe) / Capability
  1  operational   Team                     ← Story → rolls up to Epic (today's Lighthouse Feature)
```

- The Team is the lowest owning unit and the only one with throughput (bucket 3's forecasting inputs).
- HOTSPOT [6455]: the DIVERGE recommendation stacks levels of *work* only and keeps Teams flat. Pairing adds an
  owning unit per level. To be carried into 6455's DISCUSS together with "levels per installation".

**Q7 — "Portfolio" is overloaded today** (maintainer). Users model with it a *Product* with release tracking, an
*ART*, or a *value stream*, partly because the tool gives no guidance. HOTSPOT: one word, at least three meanings,
a textbook sign that the word must be split in the glossary.

- **Deliveries become a property of a level, not of a Portfolio**: they make sense on levels above the Team (not
  sure about the Team level itself).
- Candidate reading: the old Portfolio dissolves into (a) an owning unit at a level above the Team and (b) the
  Deliveries hanging off that level. To be tested in the next round.

**Note for the interlude:** this is the target language, not a behaviour change inside S01. Moving SLE, staleness etc. from
   per-unit to org-default-plus-override is a feature (likely part of Epic 6455 or its own), not refactoring.

**Closing D — the Team stays special** (maintainer). The lowest layer is defined by what it does: it **generates
the throughput**. Everything above it can be generic units on levels; the old Portfolio becomes one of those.
The name "Team" is up for debate, the role is not. HOTSPOT: how to model "the unit that generates throughput"
generically is open (maintainer: "unsure yet how to best solve this").

### B. Access — walked 2026-11-01

- **Org roles plus unit members** (maintainer): organisation-wide roles (admin, broad viewers) plus membership
  per unit for working on it. Matches today's shape (System Admin + Team/Portfolio members) lifted to units.
- HOTSPOT `?`: "a C-level person should not see all the Team-level things, as they don't care". Two different
  needs hide in that sentence: *not allowed* (permission, Access) and *not relevant* (which level a person looks
  at by default, a read-model / navigation concern). Keep them apart in the model.
- **Someone outside the organisation looking at a promise** has been asked for; not planned. Hosted instances run
  with auth, so it needs an account; a future dedicated *Delivery viewer* role is conceivable. For Standalone and
  the rest, the outward channel is **screenshots today, Reports in future** (PDF export, scheduled delivery).
  Candidate context-map edge: **Reports as the published language to people outside Lighthouse**.
- The code announces no access change at all (every RBAC change goes through `IRbacAdministrationService`
  silently). No maintainer event was raised here, so Access stays a supporting area: it follows the units that
  exist and has no story of its own beyond that.

### E. Sync — walked 2026-11-01

- **Stale, blocked and transitioned items feed a signal** (maintainer). Today *Work Item Became Stale* and
  *Work Item Transitioned* are published to nobody, and blocked only feeds a chart. In the target, they are inputs
  to the red/amber/green **signals** from section F, with history. The events exist; their consumer does not.
- **What "one refresh" is does not matter to users** (maintainer). They care that data is fresh and that a failure
  is visible. The refresh shape (per unit, per level, per connection) is plumbing: Sync is a supporting area whose
  outward facts are *X refreshed* and *refresh failed*, not its internal steps.
- **The default-size guess belongs to Sync** (maintainer): rolling up work, including the guess for a Feature
  with no children, is part of getting the data right. The rule it applies comes from Organisation set-up
  (default Feature size = org default, local override). Forecasting only consumes the result.
- Code hot spots kept: one 1,445-line `WorkItemService` runs both the Team and the Portfolio sync; a query change
  discards items silently; Feature state changes are recorded with no event.

### F. Metrics — walked 2026-11-01

- **Signals live on two levels** (maintainer): **unit/level health** (a Team or level turns amber/red, e.g. by how
  many of its items are blocked or stale; item-level facts roll into it) and **promise** (a Feature or Delivery's
  likelihood of hitting its date). Each signal has a history of when it changed. None of this exists in code
  today; the inputs (stale, blocked, transitioned events; forecasts) do.
- **Flow metrics are one calculation at every level; granularity differs** (maintainer). Teams look per day
  (perhaps per hour in future); strategic levels per week or month. Candidate: granularity is a property of the
  level, set in Organisation set-up.
- **The real distinction is leaf vs non-leaf level** (maintainer), not Team vs Portfolio:
  - **Feature WIP** belongs to forecasting and only matters at the leaf, where throughput is generated.
  - **Refinement** exists only at the leaf today; it may come to non-leaf levels in a different form, and must then
    work for any number of upper levels.
  - So "special" things are tied to *leaf* or *non-leaf*, never to a numbered level.

### G. Forecasting — walked 2026-11-01

- **A Feature's forecast is a planning aid, not a tracked promise** (maintainer). People use it to plan: "what can
  I deliver in the next 3 weeks?", scrolling down to the cut line where a Feature won't make it. That its date
  moves matters little on the Feature itself. Trends already exist in the Delivery metrics.
- **Change matters where a target exists**: on non-leaf items that carry Deliveries, *signals* react, e.g.
  approaching the target date, or N Features forecast to finish after the target. So the maintainer's
  "date changed" events are promise signals, not forecast events.
- **Language shift (candidate pivotal): Forecast → Promise.** A *forecast* is an answer ("when, with what
  likelihood") that is recomputed and replaced. A *promise* is a target someone committed to, which a forecast is
  compared against, with history and signals. Forecasting owns the first; Deliveries own the second.
- **Upper levels are composed upward** (maintainer): Forecasting forecasts the items directly above the leaf from
  throughput, then composes "all children done" level by level (the joint maths Deliveries already use). So
  Forecasting owns "when" at every level.

### H. Deliveries — walked 2026-11-01

- **A promise ends kept, missed, or renegotiated, and all three are recorded with history** (maintainer).
  Renegotiation takes two forms: **target moved** or **scope cut**. Both are normal ("just life happening") and
  useful for retrospectives; nothing about them is a failure. **Signals indicate when a renegotiation should be
  considered**: the promise signal's job is to prompt that conversation, not to blame.
  - EVENTS: *Promise Made* (Delivery created with a target) · *Target Moved* · *Scope Cut* (or added) ·
    *Renegotiation Suggested* (signal) · *Promise Kept* · *Promise Missed*. Today only the archive's frozen pin
    exists; the rest is lost or derived.
- **A Delivery is a release of items on its own level** (maintainer). A Delivery on the Change level holds Changes;
  today's Deliveries are this rule on the Feature level. So Deliveries hang off any non-leaf level and hold that
  level's items.
- **Planning is core** (maintainer). Lighthouse helps decide what fits ("what can we promise by the end of the
  PI?"), not just checks the result. The Feature forecast's cut-line view from G is the planning read model.

### I–K. Refinement, Reports, Clients — walked 2026-11-01

- **Refinement is mainly way of working** (maintainer), at the leaf. On non-leaf levels a different form would
  **feed Forecasting**: reference-class forecasting, setting the approximate size of non-leaf items. So it is
  supporting at the leaf, and an upstream of Forecasting above it.
- **Reports are their own context** (maintainer). Then & Now is only the first report type; more follow (metrics
  charts in a report, signals as a report type). They conform to Metrics for numbers and are the outbound channel
  (PDF, scheduled delivery). HOTSPOT `?`: Signals as a report type, or Signals as their own concept that a report
  can include; open.
- **Clients**: the public API is the published language for the CLI and the MCP server. Kept hot spot: saving a
  connection from the CLI always starts updates; the browser asks first.

## Phase 3 — pivotal events, language shifts and candidate contexts (draft for the maintainer)

### Pivotal events (where the story hands over)

| # | Pivotal event | What changes meaning there |
|---|---|---|
| P1 | *Organisation Set Up* / *Level Introduced* | Structure, names and mapping exist; everything downstream reads them |
| P2 | *Items Fetched* (connection works) | Tracker issues become Lighthouse items (translation) |
| P3 | *Unit Data Refreshed* (today Team Data Refreshed, Portfolio Features Refreshed) | Items become flow history |
| P4 | *Forecasts Updated* | Throughput becomes dates ("when") |
| P5 | *Promise Made / Kept / Missed / Renegotiated* | A date becomes a commitment |
| P6 | *Signal Changed* | State becomes attention |
| P7 | *Report Created* | Live numbers become a frozen record for others |
| — | *Licence Expired* | Upstream gate on everything |

### Candidate contexts (draft, to be confirmed)

Maintainer decisions, 2026-11-01:

- **Units live in Organisation set-up**: one context holds the levels, their names (Terminology), the units on each
  level (the leaf unit that generates throughput, today's Team; upper units, today's Portfolios), the mapping
  defaults and each unit's overrides.
- **Signals is its own context** (maintainer leaning, Claude agreeing): it listens to Sync (stale, blocked,
  transitioned), Metrics, Forecasting and Promises, and owns thresholds, current state and the history of
  changes. Splitting it between Metrics and Promises would build the same threshold-and-history mechanism twice.
  **Target only**: nothing in code to package today; SLE risk stays in Metrics until the first Signals feature.
  Reports include signals as conformist.
- **The delivery side is called Promises** in the model; *Delivery* stays the user-facing, configurable term for
  one promise.
- **Licensing is its own small, generic context** publishing *Licence Loaded / Nearing Expiry / Expired*, which
  others react to.
- **Platform is not a context** (answers the brief's open question): keys, backups, updates, consent and start-up
  housekeeping are the composition root's infrastructure lane.

| Context | Type | Today's module | Packaged by Epic 6264 (behaviour-free) or target only |
|---|---|---|---|
| Organisation set-up (levels, names, mapping defaults, units + overrides, calendar, cadence, ordering) | supporting, upstream of nearly all | none; spread over instance settings, connection defaults, Team/Portfolio | packaged from today's settings + Team/Portfolio; levels are target (6455) |
| Tracker integration (ACL) | supporting | WorkTracking-Integration | packaged |
| Sync | supporting | WorkItems/Sync | packaged |
| Flow metrics | **core** | Metrics | packaged (S02 pilot) |
| Forecasting | **core** | Forecasting | packaged |
| Promises | **core** | Delivery half of Portfolio/Delivery | packaged (today's Deliveries); lifecycle events are target |
| Signals | supporting (target) | none | target only |
| Refinement | supporting | Refinement | packaged |
| Reports | supporting | Reports | packaged |
| Access | generic | RBAC/Identity | packaged |
| Licensing | generic | inside Platform | packaged (small) |
| *(Platform lane)* | not a context | Platform/Persistence | composition root |

### Context map (draft)

```text
  Jira · ADO · Linear · CSV · ServiceNow                    OIDC IdP           PostHog
          │ (upstream, external)                               │                  ▲
          ▼                                                    ▼                  │ platform lane
  ┌────────────────────┐  mapping (conformist)   ┌─────────────────────────┐   ┌────────┐
  │ Tracker integration │◄────────────────────────│ Organisation set-up      │   │ Access │ generic, guards all
  │   (ACL)             │                         │ levels·names·units·      │   └────────┘
  └─────────┬──────────┘                         │ defaults·overrides       │   ┌───────────┐
            │ customer/supplier                   └───────────┬─────────────┘   │ Licensing │ generic,
            ▼                                                 │ open host:      └───────────┘ events → all
  ┌───────────────────┐  domain events                         │ everyone conforms to its structure
  │ Sync              │──────────────┬─────────────┬───────────┼──────────────┐
  └───────────────────┘              ▼             ▼           ▼              ▼
                            ┌──────────────┐ ┌─────────────┐ ┌────────────┐ ┌─────────┐
                            │ Flow metrics │ │ Forecasting │◄│ Refinement │ │ Signals │ (target)
                            └──────┬───────┘ └──────┬──────┘ └────────────┘ └────▲────┘
                                   │ conformist     │ customer/supplier          │ events from Sync,
                                   ▼                ▼                            │ Metrics, Forecasting,
                            ┌──────────────┐ ┌─────────────┐                     │ Promises
                            │ Reports      │ │ Promises    │─────────────────────┘
                            └──────┬───────┘ └─────────────┘
                                   │ published language (PDF, scheduled) → people outside Lighthouse
  Public HTTP API = open host / published language → SPA, Lighthouse-Clients (CLI, MCP)
```

### Layout decisions (maintainer, 2026-11-01)

- **Backend**: `Modules/<Context>/{Domain, Application, Adapters}`; ports live in `Application` next to the use
  cases that need them; `Adapters/{Api, Persistence, Connectors…}`. Chosen over flat role-named folders (layers not
  visible) and vertical slices (conflict with ports-and-adapters rules). Lets ArchUnit enforce "Domain ↛ Adapters"
  per context.
- **Frontend**: `src/modules/<context>/` with one public `index.ts`. Not `features/` (collides with the domain word
  Feature), not `contexts/` (collides with React Context and `src/context/`).
- **Shared kernel** may hold only: identifiers used across contexts, truly shared value objects (e.g. date range,
  likelihood), and the domain-event base type. Each event type lives with the context that publishes it.

### Status of this board

**This is the target, not today's code.** The context map, the glossary and the layout are the goal the
modular-monolith work moves towards over the coming weeks, one behaviour-free Story at a time. Concepts that do not
exist in code yet (Signals, the promise lifecycle, levels above the Feature) are target language for the features
that will build them (Epic 6455 first), not refactoring work.

### Data ownership (maintainer question, 2026-11-01)

The maintainer asked whether there is one context for the data from the source (leaf and non-leaf), with
calculated data (forecasts, flow metrics …) kept in separate "extension" tables. Recorded:

- **Maintainer**: source data separate from what each context calculates; this also helps refreshes and forecast
  runs write in parallel without the conflicts seen before (parallel writes on shared object graphs).
- **Claude's proposals, confirm at the ADR review**: (a) the Sync context owns the mirrored items as **one item
  model with a level**, not separate contexts for leaf and non-leaf items; (b) every other context owns what it
  calculates in its own tables, referencing items by ID only, and reads others through their contracts; (c) rename
  Sync to **Work Items**, since it owns the data, not only the process; (d) Feature WIP and the throughput window
  (bucket 3) are stored as Forecasting's extension keyed by unit id.
- Consistent with the Epic's logical-ownership decision (one database, FKs and cascades stay). The forecast pilot
  (moving forecasts off the Feature) gets an explicit goal at close-out: measure parallel refresh + forecast
  behaviour before and after. On SQLite, writes still queue; they become short and conflict-free.
- Correction: Licensing comes from RBAC/Identity today (ARCHITECTURE.md §4, `Services.Implementation.Licensing`),
  not from Platform as written in the Phase 3 table.

### Hold 1 — ADR-270 accepted (maintainer, 2026-11-01)

- **One item model with a level**: accepted. "If we move forecasts and flow metrics out, we can probably work with
  a single model (Work Item)." Today `Feature` differs from `WorkItem` mainly by data other contexts own. Open for
  6455: "Work Item" then names an item on any level, so the leaf's item needs its own name.
- **Context name "Work Items"**: accepted (owns the items; sync is the process).
- **Feature WIP and throughput window** stored as Forecasting's extension keyed by unit id: accepted.
- **Pipelines inside a context, events across contexts**, with event chains where order matters across contexts:
  accepted after weighing the trade-off.
- ADR-270 status: **Accepted**.

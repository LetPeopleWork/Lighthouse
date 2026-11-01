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

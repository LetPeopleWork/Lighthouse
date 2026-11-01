# ADR-271: UI surfaces follow units and levels; settings split by audience

**Status**: Accepted as a direction (2026-11-01, maintainer). No Story implements it on its own; features move the UI
one step towards it as they touch a surface (see "How we get there").
**Date**: 2026-11-01
**Builds on**: [ADR-270](adr-270-bounded-contexts-context-map-and-module-layout.md) (bounded contexts, context map,
the language distinctions of its Decision 4 and the three setting buckets).

> **Target, not today.** Like ADR-270, this describes where the UI is going. Each section names today's state and the
> target, so a feature can tell which step it is taking.

## Context

ADR-270 cut the domain into eleven contexts. The question after the storming was whether the UI should follow the
same cut. It should not, literally: a "Forecasting" or "Flow metrics" menu would make users learn our architecture.
People navigate by **where they are** (a unit of their organisation, on a level) and then **what they look at**
there. But several of ADR-270's language decisions expose real muddle in today's UI:

- **System Settings mixes three audiences.** Its Configuration tab holds Terminology, blackout periods, behaviour
  settings and refresh cadence (how the organisation works) next to Demo Data, Database, Encryption, Log level and
  System Info (running the instance) and API Keys and System Admins (who may do what). The person who decides what
  a Team is called is often not the person who restores a backup.
- **"Org default, local override" has no single home or control.** The default sits on the connection
  (ADR-241, `ConnectionDefault`), the overrides on each Team's and Portfolio's settings, each with its own look.
- **Mapping and Translation share the connection page.** Which states count as Done describes how the organisation
  works; credentials and health are plumbing.
- **Team and Portfolio are two hard-coded kinds**: two routes, two detail pages with near-parallel tabs, two lists
  on the Overview. The target model is a unit on a level, where only leaf versus non-leaf matters.
- **"Not allowed" and "not relevant" get conflated.** Which level a person looks at by default is navigation, not
  RBAC.

## Decision 1: the unit on a level is the navigation axis; contexts are lenses on it

The primary navigation is the organisation's structure: units, grouped by level. A unit's page shows tabs, and the
tabs are the contexts' views of that unit. Which tabs appear depends on whether the unit is on the leaf level, never
on a numbered level or on the unit being a "Team" or a "Portfolio".

| Tab | Context | Shown on |
|---|---|---|
| Items (today: Features) | Work Items | every level |
| Metrics | Flow metrics | every level; granularity is a property of the level |
| Forecasts (manual, backtest, reality check) | Forecasting | leaf today |
| Deliveries | Promises | where Deliveries hang off the level (today: Portfolio) |
| Refinement | Refinement | leaf only |
| Reports | Reports | every level |
| Settings | Organisation set-up (the unit's overrides) | every level |

Today's Team and Portfolio pages already follow this shape. The target unifies them into one unit page once units on
levels exist (Epic 6455); until then a change to one page keeps the two pages parallel rather than diverging.

## Decision 2: settings are split by who decides, not by what screen they were added to

| Surface | Holds | Context | Today |
|---|---|---|---|
| **Organisation** | Terminology; levels and their names; calendar (blackout periods); Feature ordering policy; refresh cadence; the organisation's defaults for bucket 2 (types, state mapping, blocked rules, staleness, SLE, default Feature size, Parent Override Field) | Organisation set-up | System Settings → Configuration, connection defaults, per-unit settings |
| **Access** | System Admins, roles, members per unit, IdP group mappings, API keys | Access | System Settings → System Admins, API Keys; unit settings |
| **System** | Connections (credentials, health), database and backups, encryption, demo data, log level, usage data, system info, licence | Tracker integration (connections), Licensing, platform lane | System Settings tabs |

A unit's own Settings tab holds only its overrides and its local-only settings (Feature WIP, throughput window).

## Decision 3: one "follows the default / set here" control for every bucket-2 setting

Every setting with an organisation default and a local override uses the same control everywhere: it shows the
inherited value and where it comes from, and lets the unit take it over or hand it back. Bucket-3 settings (local
only) get a read-only comparison across units where seeing them side by side helps; they are not turned into
organisation settings.

## Decision 4: a connection is credentials and health

The connection page holds how to reach a tracker and whether that still works. Mapping (types, states, fields onto
Lighthouse's concepts) moves to Organisation, with per-unit overrides on the unit. Translation stays invisible.

## Decision 5: a person lands on their level

The Overview opens at the level a person works on, with other levels one step away. Hiding what someone may not see
is Access; choosing what someone sees first is navigation. They stay separate.

## Decision 6: surfaces that come later

- **Promises across units.** Once Deliveries carry a lifecycle (renegotiations, kept or missed), a cross-unit view of
  Deliveries may earn a place in the top navigation, as Features have today.
- **Signals as the Overview.** When the first Signals feature lands, "what needs attention" is the strongest candidate
  for what the Overview becomes, rather than another tab.
- **Copy rule, now.** A committed date is never labelled a forecast, and a forecast never a promise.

## Decision 7: onboarding asks how the organisation works

Once Organisation exists as one surface, first-run set-up becomes a short interview about how the organisation works
instead of a sequence of technical forms: which tracker, which levels exist and what they are called, what a Team
works on and what that rolls up into, which states mean "started" and "done", what counts as blocked. The answers
fill the Organisation surface's defaults, after which a unit is created mostly by picking from them. Every answer
stays editable later in the same place, so onboarding writes nothing that cannot be found again. It reuses
`OnboardingStepper` and the `CreateWizards` shell rather than a new frame. Target only; it depends on Decision 2 and
on levels from Epic 6455.

## How we get there

No big-bang reorganisation. Each feature that touches a surface:

1. names the surface it lives on, at DISCUSS (or DESIGN), next to its place on the context map;
2. puts anything new in its target surface, or as close as today's UI allows;
3. moves what it touches one step towards the target where that is cheap, and says so in its UI sketch.

Steps that are worth doing as their own Story, in a sensible order:

| Step | What | Depends on |
|---|---|---|
| U1 | Split System Settings into Organisation, Access and System | nothing; planned inside Epic 6455 |
| U2 | One "follows the default / set here" control, used by every bucket-2 setting | nothing; easier after U1 |
| U3 | Mapping moves from the connection to Organisation, with per-unit overrides | U1, U2; the org-or-per-tracker default question in ADR-270 |
| U4 | One unit page and per-level navigation; Overview lists units by level and lands on the person's level | Epic 6455 |
| U5 | Onboarding as an interview that fills Organisation | U1–U3; U4 for levels |
| U6 | Deliveries across units; Signals on the Overview | Promise lifecycle; first Signals feature |

Every one of these changes what users see, so each still goes through a sketch and the maintainer's yes, and the
docs and screenshots follow at its finalisation.

## Consequences

- Features have one more question at DISCUSS: which surface. The answer is cheap and stops organisation settings
  from landing next to encryption again.
- The UI drifts towards the target unevenly; for a while some organisation settings live in Organisation and some
  still in System. That is accepted over a frozen UI waiting for a big move.
- Docs and screenshots under `docs/` that show Settings change with each step, at that feature's finalisation.

## Alternatives considered

- **One menu per bounded context.** Rejected: it exposes the architecture and splits what a person does at one unit
  across several places.
- **A dedicated UI reorganisation Epic up front.** Rejected for now: most steps depend on Epic 6455 and on ADR-270's
  open questions, and a big move freezes the UI meanwhile. U1 is planned inside Epic 6455 (maintainer, 2026-11-01),
  scoped at its DISCUSS.
- **Keep Team and Portfolio as separate kinds forever.** Rejected: ADR-270 makes leaf versus non-leaf the only
  distinction that matters, and deeper hierarchies need more than two kinds.

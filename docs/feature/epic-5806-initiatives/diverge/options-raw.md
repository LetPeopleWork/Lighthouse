# Options (raw) — Epic 5806 "Visualize Initiatives"

Wave: DIVERGE, phase 3 · 2026-09-29 · generation only; scoring is in `taste-evaluation.md`.

## HMW

> How might we let a Delivery Lead answer "when will each strategic Initiative land, and is it on
> track?" for every Initiative at once, in a form they can show an executive as it is?

## SCAMPER

| Lens | Idea |
|---|---|
| **S**ubstitute | Replace "one Delivery per Initiative, built by hand" with the Initiative itself as a computed row: no stored object, derived from each Feature's parent |
| **C**ombine | Combine Initiatives with Deliveries: Lighthouse creates and maintains a rule-based Delivery for each parent automatically |
| **A**dapt | Borrow the heatmap (BigPicture) and health-coloured timeline (Linear): Initiatives down, months across, each cell coloured by the chance all Features are done by then |
| **M**agnify | Magnify the existing instance-wide Features page: a "group by parent" mode with a roll-up in each group header |
| **P**ut to other use | Put the Portfolio page to other use: a new tab listing that Portfolio's Initiatives, plus a strip on the Overview dashboard linking to each |
| **E**liminate | Eliminate the page: a weekly Initiative brief (PDF/email) pushed to the lead or the executive |
| **R**everse | Reverse the direction: the lead declares the promise first (Initiative + target date), and Lighthouse reports against it; Initiatives without a promise are not shown |

Crazy 8s: **Tracker-owned promise** (read the target date from the tracker's parent); **Slip
meter** (on track = the 85% date has not moved later in four weeks); **Feature order** (parents join
the instance-wide ranking); **Unparented work** (a pseudo-row for Features with no parent).

## Naming dimension (applies to every option)

The level above Feature is a SAFe Epic in one organisation, an Initiative, Objective, OKR or Theme
in another, and possibly a different work-item type in each Portfolio of the same instance.
Maintainer rule: never a fixed "Epic" or "Initiative" in the UI.

| Variant | Mechanism |
|---|---|
| N1 New terminology key, neutral default | A configurable term for the level above Feature, seeded with a methodology-neutral word; each organisation renames it (Epic, Initiative, Objective, …) |
| N2 Tracker's own word | Show each parent's work-item type as the tracker names it; mixed types show mixed words, and a page title cannot be derived |
| N3 Neutral fixed word | Reuse the word the UI already uses, "Parent", with no way to rename it |
| N4 Methodology word fixed | A SAFe or Flight Levels word hardcoded ("Epic", "Initiative"). Recorded to be rejected: it breaks the maintainer rule |

Every direction below uses a type-agnostic unit: whatever each Feature's parent is, of any type.

## Curated directions (5)

### Option A: Initiatives page (computed rows, one table)

- **Core idea**: A new top-level Premium page lists every Initiative, grouped by Portfolio: Features
  done/total, the date by which all are 85% likely done, target date, likelihood by target, a status
  word. A row expands to its Features.
- **Key mechanism**: Initiative = distinct parent reference over all Features; forecast = the
  existing joint composition over its Features. Target date is the only new stored fact.
- **Key assumption**: A table is one-glance enough; the lead will set a target date per Initiative.
- **Origin / closest competitor**: Substitute (+ Reverse) · Allstacks Portfolio Report, with Lighthouse maths.

### Option B: Auto-Deliveries per parent

- **Core idea**: A Portfolio setting "track every parent as a Delivery". Lighthouse keeps one
  rule-based Delivery per parent up to date; the Deliveries tab (and a cross-Portfolio list) is the
  Initiative view.
- **Key mechanism**: Existing rule-based Deliveries on Parent Reference ID, created and archived
  automatically; target date, likelihood, snapshots, timeline, notes, export all inherited.
- **Key assumption**: Users accept Initiatives as a kind of Delivery; each one gets a target date.
- **Origin / closest competitor**: Combine · 55 Degrees Portfolio Forecaster (named scope; Jira-only).

### Option C: Features page grouped by parent

- **Core idea**: The existing Features page gains a "Group by {parent}" mode; each group header shows
  Features done/total and the joint 85% date, with an optional target.
- **Key mechanism**: Grouping in an existing grid; header roll-up from the joint composition.
- **Key assumption**: Leads already live on the Features page; an Initiative is a grouping, not a thing.
- **Origin / closest competitor**: Magnify · ADO backlog roll-up; the grouping Lighthouse had until 2025-11.

### Option D: Portfolio "Initiatives" tab + Overview strip

- **Core idea**: Each Portfolio gets an Initiatives tab (its parents, roll-up, target); the Overview
  dashboard shows one line per Portfolio ("4 Initiatives, 1 at risk") linking in.
- **Key mechanism**: Scope is the Portfolio, matching "mostly one Portfolio"; multi-Portfolio
  Initiatives appear in each, marked.
- **Key assumption**: The lead thinks Portfolio-first; two clicks to any Initiative is fast enough.
- **Origin / closest competitor**: Put to other use · ADO Delivery Plans (per project roll-up).

### Option E: Initiative heatmap

- **Core idea**: One board: Initiatives as rows, the next 6-12 months as columns, each cell shaded by
  the chance all Features are done by that month's end; a marker shows the target month.
- **Key mechanism**: The same joint distribution sampled at month ends; colour carries the answer.
- **Key assumption**: A colour gradient is read correctly as a cumulative likelihood, by executives.
- **Origin / closest competitor**: Adapt · BigPicture risk heatmap; Linear health-coloured timeline.

## Diversity test (all five differ on every axis)

| | Mechanism | Assumption about the user | Cost profile | Structure it assumes about the org |
|---|---|---|---|---|
| A | Computed entity, new page | Wants a dedicated place; sets targets | New page + one small stored fact | None beyond "Features may have a parent" |
| B | Stored managed objects | Accepts Initiative = Delivery | Lifecycle automation, no new page | Every strategic item is a dated deliverable (poor fit for OKRs, themes) |
| C | Grouping in existing grid | Initiative is a lens, not a thing | Smallest; touches a free page | None |
| D | Per-Portfolio scope | Thinks Portfolio-first | New tab + dashboard strip | The level nests inside a Portfolio (SAFe-portfolio shaped) |
| E | Visual encoding over time | Reads colour, not numbers | New chart component | A months-ahead planning horizon |

## Merged or removed

Weekly brief: a channel, not a view, and "Reports" is another Epic (deferred). Reverse: merged into
A as the target-date source. Tracker-owned promise and Slip meter: sources for "on track" that fit
every direction, so an open decision. Feature order: prioritisation is out of scope. Unparented
work: carried into A's v1 as a count.

# Manual verification — what a dependency does to a date

**Run this after slice 01, and again after slice 02.** The setup is identical both times; six of the
expectations change, and those six are the point. A case whose expected result is the same in both
columns is a non-regression check; a case whose result differs is the slice's actual claim.

Nothing here is automated on purpose. The suite already proves each rule in isolation against a pinned
starting number. What it cannot show is a person opening a Portfolio and finding a date that moved for
a reason they can read on the row — which is the only evidence that the feature was worth building.

---

## Before anything: measure the noise, or the whole exercise is worthless

The production draw source is unseeded by deliberate choice (ADR-154). **Two refreshes over identical
data return different percentiles.** With several Features under a waiting one, at least one of them
reads earlier by chance most runs, so "a date moved" on its own proves nothing at all.

So, first, with **no dependency honoured anywhere** (easiest: unlicensed, or every Portfolio set to
ignore dependencies):

1. Refresh the Portfolio three times.
2. Record each Feature's 85 % date each time.
3. **The spread is the largest difference any one Feature shows across those three runs.**

Every claim below is then read as *moved by more than the spread*, and reproduced across three runs.
A movement inside the spread is not a result. Write the spread down in the run log — it is the
measuring stick, and it is instance-specific.

---

## Setup

### Data — already in the Jira demo project

`LGHTHSDMO`, four Epics, links already created. Confirmed by `JiraDependencyDogfoodTest`, which pins
exactly this shape:

```
LGHTHSDMO-10  TrendSpotter Insights   waits on nothing (blocks only)
      ^
LGHTHSDMO-9   BlinkList Directory     waits on 10
      ^
LGHTHSDMO-7   Spotlight Finder        waits on 8 and 9
      ^
LGHTHSDMO-8   SnapShare Hub           waits on nothing (blocks only)
```

A DAG: `10 -> 9 -> 7`, and `8 -> 7`. Note that **8 and 10 must read empty** in the Dependencies column.
They carry only the far end of somebody else's link, and a mapper that walked both ends would double
every dependency in the instance while still looking believable.

### Teams

Lighthouse decides which Team works an Epic from the Team's own query, not from anything in Jira. That
matters more than it sounds: **slice 01 honours a wait only when at most one Team has work across both
ends.** Split these four Epics across Teams by accident and every link reads "crosses a Team", no date
moves, and the screen is indistinguishable from a build where the feature does not work.

So configure deliberately:

- **One Team** whose query covers the children of 7, 8, 9 and 10 — this is what cases A, B, C and E run
  against.
- **A second Team** with a slightly different query, so that one end of one link sits on it — this is
  case D. Two teams with near-identical queries is the cheapest way to manufacture a cross-Team edge
  without touching Jira.

### Licence

Premium for cases A-E. The premium licence fixture is gitignored and absent from a fresh worktree —
import it from the main checkout before starting, or every case silently becomes case F.

---

## The cases

| # | Setup | After slice 01 | After slice 02 |
| --- | --- | --- | --- |
| **A** | A Feature with no dependency, on the one Team | Date unchanged beyond the spread. Dependencies column empty, no warning | **Same** — this is the non-regression check, and it must not move in either slice |
| **B** | The `10 -> 9 -> 7` chain, one Team, blockers ranked above their dependents | 7 moves **out**. At least one Feature ranked below 7 moves **in** by more than the spread. No warning on any row — nothing is wrong with these links | **Same** |
| **C** | As B, then **drag 7 above 9** so a blocker sits below its dependent | Dates still move — where a Feature sits is not a reason to leave a wait out. Row reads *"…which sits below it in the order"*, as a note, not a warning | **Same** |
| **D** | Move one end of a link onto the second Team | **No date moves.** Row warns the wait is not the same Team's work and is not in the forecast | **Inverted.** The wait is honoured, dates move, and **that warning is gone** |
| **E** | Create a cycle in Jira by hand (see below) | Both rows warn they are waiting on each other. Neither holds the other back. **The forecast completes in normal time** | **Same** |
| **F** | Any of the above, licence removed | Counts and the dependency list still render. A wait nothing else stands against says a premium licence accounts for it. **Dates identical to a run with no dependency recorded.** A cross-Team or circular wait keeps saying *that*, not the licence | **Same**, except D's wait now reads as the licence rather than as crossing a Team |

### Case E — the cycle, and why it comes last

Lighthouse never authors a dependency, so this link has to be made by hand in Jira. **Azure DevOps
refuses to create one** (`TF201035`, and it enforces transitively), so Jira is the only place a cycle
can be built at all.

Keep it out of the standing demo data — a permanent cycle is a permanent warning on two rows that
everyone learns to ignore. Add it, verify, remove it.

What it proves is not cosmetic: honoured waits form a DAG *because* the one decision drops every edge
inside a circle, and the run's termination rests on that. If the forecast hangs here, that argument is
wrong. If it completes, the guard held.

---

## What to record, each run

Per case, in the run log below:

- The spread, measured first, on this instance, this session.
- Each Feature's 85 % date before and after, across three runs.
- Which rows carried which warning, in the instance's own words.
- Wall clock of the refresh for case E, against a refresh with no cycle present.

Also worth a look for case F: **Settings → Logs**. An unlicensed instance forecasting a Portfolio
whose waits would otherwise have been honoured writes one line naming the Teams whose dates read as
though nothing waited on anything. It is silent on a licensed instance, and silent for a Portfolio
that has set its dependencies aside — both of those are an empty set, and a warning about an empty set
teaches people to skip lines.

---

## Traps that will waste your time

- **Reading a single refresh.** Covered above, and it is the one mistake that makes the whole exercise
  produce a confident wrong answer.
- **Expecting case D to do something after slice 01.** It is meant to sit still. The warning is the
  result.
- **Forgetting 8 and 10 read empty.** If they show a count, dependencies are being read from both ends
  and every number in the instance is doubled.
- **A Portfolio set to ignore dependencies.** Silently produces case A everywhere, with no warning on
  any row by design — that reason is the one that deliberately says nothing.
- **Aiming E2E at this instance.** Don't. This is a hand-run walkthrough against real history.

---

## Run log

### Slice 01 — 2026-08-23

Run on the local dev instance against the real `LGHTHSDMO` Jira demo project. Cases A-D done; E and F
still owed.

**Setup as actually used** — the plan above said to split the two Teams on `parent`, and that was wrong.
Two lessons, both cost real time:

- **`labels in (Brownies)` holds two populations**: ~98 parentless auto-generated items that carry all
  the throughput, and 15 items that are children of the Epics. Splitting on `parent != LGHTHSDMO-10`
  dropped every parentless item, because JQL `!=` excludes nulls - the Team ended up with 12 items,
  none of them done, and **zero throughput**. Every dependency then read `BlockerCannotBeForecast`,
  which is correct and masks everything the walkthrough is trying to show.
- **Epic `LGHTHSDMO-10` has no children at all**, so it falls back to the default Feature size and its
  progress reads 0/10. It is not a candidate for a Team split.

What worked - both Teams keep recent throughput *and* own different Epics:

```
Team A  labels in (Brownies) and key not in (LGHTHSDMO-76, LGHTHSDMO-75, LGHTHSDMO-72)
        and ((summary ~ "Story 1" and parent is EMPTY and updated >= -10d)
             or parent in (LGHTHSDMO-7, LGHTHSDMO-8))

Team B  labels in (Brownies) and key not in (LGHTHSDMO-76, LGHTHSDMO-75, LGHTHSDMO-72)
        and ((summary !~ "Story 1" and parent is EMPTY and updated >= -10d)
             or parent = LGHTHSDMO-9)
```

Feature WIP 3 on both. `updated >= -10d` is a workaround, not a preference: a query matching more than
one page of results makes the Jira identity sweep fail with **410 Gone** on its second page. That is a
connector bug unrelated to this Epic and it is what forced the window down to ten days, which is why
every date below sits in 2027.

**The spread, measured first.** Three refreshes, nothing changed between them: TrendSpotter 3 days,
BlinkList 2, Spotlight 1, SnapShare 1. Nothing below is called a movement unless it beats that.

| case | result | evidence |
| --- | --- | --- |
| **A** | PASS | SnapShare Hub and TrendSpotter both read **empty** in the Dependencies column - they carry only the far end of somebody else's link, so a count there would mean every dependency in the instance was doubled. SnapShare shows a green tick. TrendSpotter shows a warning, and it is **not** a dependency warning: `isUsingDefaultFeatureSize=true`, no children in Jira. |
| **B** | PASS | Spotlight Finder waits on SnapShare Hub, same Team, honoured. It is listed on the row and is **absent from the warnings tooltip** - having a dependency is not a problem, only one with something wrong with it is. Spotlight 85% = 24.3.2027 lands after SnapShare's 2.12.2026, as an honoured wait must. |
| **C** | PASS | Dragged Spotlight above SnapShare so the blocker sits below its dependent. Verdict stayed honoured - where a Feature sits is not a reason to leave a wait out - and the row gained *"This Feature depends on SnapShare Hub, which sits below it in the order."* Dates moved -2/+2/-2/-3 days, inside the measured spread. |
| **D** | PASS | BlinkList Directory waits on TrendSpotter Insights across a Team. Row warns *"…which is not the same Team's work. That dependency is not included in the forecast."* **BlinkList 85% = 23.3.2027 finishes two months before TrendSpotter's 19.5.2027** - only possible because the wait is being ignored, which is exactly slice 01's claim. |
| **E** | PASS | Two-Feature cycle between Spotlight Finder and SnapShare Hub, **both on Team A** - the one shape that could make two Features wait on each other forever. Both edges flipped `HONOURED` to `InALoop`; Spotlight's cross-Team edge kept `CrossesATeam`, so a circle does not swallow the reasons around it. Forecast completed in 2394/2059/2279 ms against a 1934-1980 ms baseline, and **the abandoned-run guard never fired** - the cycle was dropped at the decision and the simulation never saw it. Dates landed where they do when nothing is honoured, because a circle is a data problem, not a constraint. |
| **F** | PASS | Licence cleared. `Spotlight -> SnapShare` flipped `HONOURED` to `NotLicensed`; **both cross-Team edges stayed `CrossesATeam`** - an unlicensed reader is never told a purchase would move a date that would not move (the AC-6.1 fix, holding live). Counts and the dependency list stayed visible throughout. One aggregated log line named the affected Team. |

**Also verified, unplanned:** setting the Portfolio to ignore its dependencies with a valid licence turns
**every** edge into `IgnoredByPortfolio` - including the cross-Team ones - and removes every dependency
warning from every row, while the list still names the reason per entry. Somebody made a choice rather
than breaking a link, and a warning on every Feature in the Portfolio is how a column stops being read.

**A date movement nobody predicted, and it is the mechanic seen from the other side.** Removing the one
honoured wait (by clearing the licence) moved Spotlight Finder **in** 16 days - expected - and moved
SnapShare Hub **out 43 days**, fourteen times the noise floor. Nobody predicted the second one. With the
wait honoured, Team A's work-in-progress window covers two Features and SnapShare takes half the
throughput; without it the window covers three and SnapShare takes a third. Honouring a dependency does
not merely delay the Feature that waits, it concentrates capacity on the Features that can actually be
started. That is KPI-2's claim observed in reverse, and it is the strongest single piece of evidence
this walkthrough produced. BlinkList sat perfectly still throughout, alone on Team B, which is what made
it readable.

**Two things worth carrying into the slice 02 re-run:**

- **D's assertion is a date ordering that must flip, not a vague "dates should move".** After slice 02,
  BlinkList must finish *after* TrendSpotter, and its warning must be gone. That is the sharpest
  before/after this walkthrough produces - write the two dates down before starting.
- **C's dates did not move, and the reason is narrower than it looks.** The forecast is *not* blind to
  order - it iterates Features in rank order and that decides which fall inside the work-in-progress
  window. It did not move here because for this particular drag the eligible set is unchanged: Spotlight
  is held back by SnapShare either way, so both orders leave `[TrendSpotter, SnapShare]` eligible. Move
  a Feature that is *not* blocked and dates will move, correctly. Do not record "reordering never moves
  a date" as the lesson.

### Slice 02 — <date>

_Not yet run._

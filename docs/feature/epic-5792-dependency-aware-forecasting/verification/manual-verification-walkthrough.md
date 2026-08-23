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

### Slice 01 — <date>

_Not yet run._

### Slice 02 — <date>

_Not yet run._

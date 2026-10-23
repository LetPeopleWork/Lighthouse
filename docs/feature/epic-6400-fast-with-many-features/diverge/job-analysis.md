# Job Analysis — epic-6400-fast-with-many-features (DIVERGE, Phase 1)

**Agent**: Flux (`nw-diverger`) · **Date**: 2026-10-23 (**revision 1**, after peer review round 1) · **Inputs**:
- `../feature-delta.md` (DISCOVER, committed `eed46aa7c`)
- `docs/product/jobs.yaml` (`job-operator-keep-lighthouse-usable-while-it-forecasts`)
- Epic 6311 and Story 6354 context
- Epic 6264 slices S05a–S05e and S13

**Evidence grade**: one reporting instance.
- The stalls are **observed behaviour**: a log, transcribed by the maintainer.
- The slow list is **one second-hand reading** (≈50 s).
- The wish for a scaffold that appears at once is **stated intent**.
- The mechanism is **desk/codebase fact** (W1–W10).

The opportunity scores below are **desk estimates by the diverger** on a 1–10 scale, not survey data. The
measurement slice re-scores them (Handoff waiver).

**Scale mapping**: `jobs.yaml` scores importance and satisfaction on 1–5. A 1–10 desk estimate here maps to it
halved and rounded (8 → 4, 2 → 1).

**Revision 1**:
- The new job statement carries the strategic clause and names no UI.
- The physical function is now solution-neutral.
- The scale mapping is stated above.
- OS2 now states how likely a random reading is to overlap a forecast.

## 1. Raw request

> ADO Epic #6400 *Lighthouse stays fast with many Features*. On Portfolios of 400–920 Features across 8 Teams:
> - the Overview shows a loading wheel when the user only wants to get into Settings;
> - a Portfolio's Features list takes about 50 s;
> - while forecasts run (4–8 minutes, several times an hour), the server stalls.
>
> Named suspects: the whole dependency graph, move permissions, the global order. Also asked for: a warning for
> Portfolios that are too big.

The request names suspects and one remedy, staggered loading. DISCOVER re-ranked the suspects on the code (X1, X2,
D3) and reframed the remedy from "load heavy data later" to "do not read it" (X4, D4).

## 2. Job extraction (5 Whys)

| Layer | Statement | Why go higher? |
|---|---|---|
| Tactical (the request) | "Make the Features list and the Overview load faster; stop the stalls during forecasts." | Nobody wants a fast list for its own sake. |
| Operational | "Read a large Portfolio's dates, or get into Settings, without waiting, also while Lighthouse forecasts." | Why does the wait matter? |
| Operational | "Because the wait comes on every visit and every hour, and a stall looks like a broken instance; the operator pressed Cancel all." | Why does that matter beyond annoyance? |
| **Strategic** | "So an instance that holds a whole organisation's portfolio stays as quick to consult as a small one, and people keep using Lighthouse for delivery decisions instead of falling back to the tracker or to a spreadsheet." | Higher is "deliver predictably", a life-goal answer. **Stop.** |
| **Physical (irreducible function)** | Deliver the answer a person asked for within the time they will wait, whatever else the instance holds or is doing. | — |

**First-principles inversion**

1. The activity: opening the Overview, opening the Features tab, waiting, pressing Cancel all.
2. Nobody wants to open a list. The reader wants a Feature's date, and the operator wants the instance to look
   alive.
3. The irreducible function has two halves: **the answer within the wait a person tolerates**, regardless of how
   much the instance holds and regardless of what it is doing in the background. Both halves matter. A cheap read
   still stalls behind a starved server, and an isolated server still reads the whole Feature table.

## 3. Job statements

**Reused (primary for the stalls, `platform-operator`)**: `job-operator-keep-lighthouse-usable-while-it-forecasts`

> When Lighthouse is recalculating a Portfolio's forecasts in the background, I want every page to keep answering,
> so a forecast reads as "busy for a moment" rather than "the instance is dead", and I never restart the container
> to get my screens back.

The wording is unchanged. New evidence: v26.10.19.10 carries Epic 6311's half-the-cores limit, and on it nine
heartbeat warnings (1.4–49.3 s) appear only during forecast rounds (X5). The job is still under-served after 6311.

**New**: `job-reach-and-read-what-i-came-for-on-a-large-instance`. It is primary for the screens
(`delivery-lead-rte`, `delivery-forecaster`, `product-owner`) and covers `platform-operator` on the way to
Settings.

> When I open Lighthouse on an instance with hundreds of Features per Portfolio, to see where a Portfolio's or a
> Team's Features stand or to change a setting,
> I want to get what I came for without waiting on data I did not ask for,
> so I keep using Lighthouse for delivery decisions instead of falling back to the tracker or a spreadsheet.

**One job, two circumstances.** Both come from DISCOVER's candidates: the large Features list (V1b), and the way to
Settings (Maintainer answers, 3). They share one success measure (time to what I came for) and one cause in the
code: reads that scale with the whole instance (W2–W5).

**Distinct from the reused job.** It holds **even with no forecast running**. The independence is weaker than it
looks, though (OS2, below).

**Narrowing rule.** The list half fails G1 if the idle 920-Feature list loads in under about 10 s on the scaled data
set; that is the **falsifier**. The list half is unmet but stands if the idle list takes between the K2 target (2 s)
and about 10 s. If the falsifier fires, the job narrows to the way to Settings.

**Emotional**: from "every visit is a wait, and a stall looks like a crash" to "it is just there; when it is busy,
it says so".

**Social**:
- The delivery lead opens a large Portfolio in front of a steering group without apologising for the tool.
- The operator does not have to tell colleagues that Lighthouse "hangs sometimes".

## 4. Disruption check

**Is there a higher job that makes this one unnecessary?**
- The higher job is "know when the work will be done" (delivery forecasting).
- Speed is a hygiene factor of that job, not a competitor to it.

**One disruption is real**: if the answer **came to the reader**, through a digest or the Lighthouse-Clients
CLI/MCP reading one Feature, nobody would open the 920-Feature list. It is set aside as an option during curation
(`options-raw.md` §4) because:
- it serves a different circumstance, asking about one Feature;
- it does nothing for the operator's stalls.

## 5. Outcome statements (ODI)

Each outcome is tied to a DISCOVER target. The scores are desk estimates on 1–10, with
Score = I + max(0, I − S).

| # | Outcome statement | Target | I | S | Score | Status |
|---|---|---|---|---|---|---|
| OS1 | Minimise the time a page waits for an answer while a forecast runs | K3 | 9 | 2 | **16** | Under-served |
| OS2 | Minimise the time from opening a large Portfolio or Team to reading its first Feature's dates | K2 | 8 | 2 | **14** | Under-served |
| OS3 | Minimise the time from opening Lighthouse to reaching the screen I came for | K1 | 7 | 4 | 10 | Served by the header (W1); the server's part is unproven (A12) |
| OS4 | Minimise the time the instance spends on one forecast round, computed or skipped | K4 | 6 | 2 | 10 | A lever for OS1 rather than an outcome users name |
| OS5 | Minimise the likelihood that a slow or failed load reads as an empty or finished list | C5 | 6 | 2 | 10 | Mandatory under CLAUDE.md for any view a slice changes |
| OS6 | Minimise the likelihood that a user builds a Portfolio far larger than they need without knowing its cost | O7 | 4 | 3 | 5 | Over-served by the job; a nudge (V7) |

Why these numbers:
- **OS1** carries the only symptom observed on the current release. An instance that looks dead is the most
  expensive failure the product has (6311's rationale).
- **OS2** rests on one reading, which is **not independent of OS1**. The log shows several rounds an hour, each 4–8
  minutes long. That puts a forecast in progress for roughly 12–48 minutes of every hour, so a reading taken at a
  random moment overlaps a round **about 20–80% of the time** (A13). That is why I is 8, not 9.
- **OS3** is lower because the header already renders without waiting (W1). The reporter's wait is real but
  unexplained; it may be the stall itself (A12).
- **OS4** matters through OS1. The operator's Cancel all shows that the length of a round is itself felt.

## 6. Opportunity candidates

**OS1** and **OS2** are the most under-served. Every option in `options-raw.md` is judged by how much of these two
it moves, both idle and during a forecast. OS3 and OS5 come with whichever slices touch those screens. OS6 stays a
nudge, last.

## 7. Gate G1

- **PASS**: the job is at strategic level (§2), and the physical function is stated without a mechanism.
- **PASS**: the job statements name no list, row, spinner, cache or worker.
- **PASS**: six ODI outcome statements, each tied to K1–K4 or to a constraint.

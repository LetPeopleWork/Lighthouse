# Job Analysis — epic-5510-5881-refinement

**Wave**: DIVERGE, Phase 1 · **Agent**: Flux (`nw-diverger`) · **Date**: 2026-10-02
**Input**: `feature-delta.md` (DISCOVER, light, desk research only) and
`docs/product/journeys/epic-5510-5881-refinement.yaml`.
Maintainer-settled decisions D1-D19 and constraints C1-C10 are treated as fixed. They bound the
solution space here and are not re-opened.

---

## 1. Raw request (verbatim intent of the two ADO items)

- **#5510 Sizing Poker**: let a Team say per Work Item whether it is "doable within our SLE?" — Yes / Yes, but… /
  No, with comments. Async first, live later.
- **#5881 Refinement Need Chart**: before the next replenishment, show how many Work Items the Team is likely
  to finish against how many are already refined. The band needs an upper bound that says "stop refining".

Both are solutions. Below, they are used as evidence of the job and not as the job itself.

## 2. Job extraction (5 Whys)

| Layer | Statement | Question that moved it up |
|---|---|---|
| Tactical | "We want a poker tool and a chart." | Why a poker tool? |
| Tactical | "To find out, before the meeting, which Work Items fit our SLE." | Why before the meeting? |
| Operational | "So the refinement meeting only discusses the Work Items someone doubts." | Why does the chart belong with it? |
| Operational | "Because the meeting also runs long at the other end: nobody knows how many Work Items need refining, so the top ten get refined again 'just in case'." | Why does the count matter? |
| Strategic | "Refinement is an investment that decays. Too little and the Team runs dry at the commitment point. Too much and the prepared options rot in a growing ready queue, which LPW teaches against (C6)." | Why does that matter? |
| **Physical** | **Match upstream preparation to the downstream pull rate, and spend preparation effort only where there is uncertainty.** | A further "why" gives "deliver value predictably", which is a life goal. Stop. |

**First-principles check.** The visible activity is "a refinement meeting". Nobody wakes up wanting
to refine. Take away the meeting and the tools, and two functions are left: (1) **count**: how
much will be pulled, and how much is ready; (2) **triage**: which candidates carry doubt. Voting is one
way to triage, and a chart is one way to count. Neither one is the job.

## 3. Job statements

**Functional (primary, `flow-coach`)**
> When the next replenishment is coming up, I want to know how many right-sized Work Items the Team will
> likely pull and which candidates someone doubts fit our SLE, so I can spend refinement effort only where
> it changes the outcome — enough to keep the Team supplied, and no more.

**Emotional**: move from *weary and guessing* ("another hour through every ticket, and let's refine ten
more to be safe") to *restrained confidence* ("we discussed three, and we stop here because we are at the
top of the range").

**Social**: the coach is seen as someone who protects the Team's time, not someone who runs rituals.
For the voter, it means being able to raise a doubt without being anchored or talked over. For the
product owner, it means pointing to evidence when declining to refine more.

**Candidate voter job (`team-member-voter`, not yet a persona)**
> When I am asked whether a Work Item fits our SLE, I want to give my view in seconds, from where I already
> am, so I don't have to sit through discussion of Work Items I have no doubt about.

DISCOVER deferred this persona until the voter's job could be shown to differ from the coach's. The
research in Phase 2 shows that it does. The voter's success measure is *cost per vote and where the
request reaches them*, while the coach's is *the meeting's length and the size of the ready queue*. Every
async competitor that works is built around the voter's measure (Phase 2, I1). **Recommendation to
DISCUSS: create `team-member-voter`.**

## 4. Disruption check

The job one level up is *"keep a steady flow of valuable work at the commitment point"*. Two higher-level
answers would make this whole job smaller:

1. **Right-size at creation** (ProKanban's FIRST/right-sizing practice). If Work Items are written to fit the SLE
   from the start, the sizing vote becomes a confirmation, not a triage. The job stays because the doubt
   remains, but it shrinks.
2. **Just-in-time refinement driven by a pull signal.** Refine only when the ready count falls below a
   lower bound. That makes the "how many" question continuous instead of a per-meeting one. This is the
   Kanban Method's own answer (replenishment on demand), and it is why options with an upper bound and
   a gauge (see options-raw.md, Option 4) are in scope.

Neither removes the job. Both argue that the **count** is the senior function and the **vote** the junior
one: the count decides whether any voting is needed at all.

## 5. ODI outcome statements

Importance and satisfaction are **desk estimates by Flux (1-10)**. There is no customer survey (DISCOVER
G2 is PARTIAL), so treat the scores as a ranking hypothesis to test, not as measurements.
Score = Importance + max(0, Importance − Satisfaction).

| # | Outcome statement | Imp. | Sat. | Score | Status | Basis for the estimate |
|---|---|---|---|---|---|---|
| O1 | Minimize the time spent discussing Work Items nobody doubts fit the SLE | 8 | 3 | **13** | Under-served | #5510 ICE 90; every poker tool still walks every item in a meeting |
| O2 | Minimize the likelihood of refining more Work Items than the Team will pull before the next replenishment | 8 | 2 | **14** | Under-served | r/agile 500+ backlog thread (V3); no tool found gives a "stop" signal (competitive-research I3) |
| O3 | Minimize the likelihood of the Team running out of refined Work Items before the next replenishment | 7 | 6 | **8** | Over-served | Industry heuristics ("ready for the next two sprints", readiness rate >80%) already push towards more |
| O4 | Minimize the likelihood that a Team member's doubt about a Work Item goes unvoiced | 6 | 5 | **7** | Over-served | Hidden-until-reveal is standard in every poker tool researched |
| O5 | Minimize the time it takes a Team member to give a sizing view on one Work Item | 8 | 3 | **13** | Under-served | D19: MVP votes arrived in one meeting window, so async is unproven; competitors that do async push into inboxes |
| O6 | Minimize the likelihood that the refinement-need count includes Work Items that never pass through refinement | 7 | 1 | **13** | Under-served | DISCOVER X5/R2; no researched tool corrects for it |
| O7 | Minimize the time it takes to set up refinement for a Team | 7 | 4 | **10** | Appropriately served | R1; competitors are zero-setup, but Lighthouse already holds the Work Items |

## 6. Opportunity candidates

1. **O2 (14): the stop signal.** This is the highest-scoring outcome, and it is the constraint the maintainer made
   mandatory (C5). The research found no product that serves it, and its mirror O3 is over-served. An
   option that leads with "stop" goes where the gap is.
2. **O1 / O5 / O6 (13 each).** O1 and O5 are the two ends of async voting (the meeting gets shorter only if
   people vote cheaply beforehand). O6 is an honesty requirement on the count, not a feature.
3. **O3 and O4 are over-served.** Do not compete on "refine more" or on anti-anchoring mechanics. Treat
   them as table stakes.

## Gate G1

| Check | Result |
|---|---|
| Job at strategic or physical level | **PASS**: physical ("match preparation to pull rate; spend effort where there is uncertainty") |
| No feature reference in the job statement | **PASS**: no poker, chart, vote or session in the statement |
| At least 3 ODI outcome statements | **PASS**: 7 |
| Functional, emotional and social dimensions | **PASS** |

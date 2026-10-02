# Job Analysis — epic-5878-baseline (DIVERGE, Phase 1)

**Agent**: Flux (`nw-diverger`) · **Date**: 2026-10-02 · **Inputs**: `../feature-delta.md` (DISCOVER),
`../discover/wave-decisions.md`, `docs/product/journeys/epic-5878-baseline.yaml`, maintainer decisions of
2026-10-02 (D8-D16 in `../wave-decisions.md`).

Evidence grade: DISCOVER failed G1-G3 on evidence and the maintainer accepted the risk (D2). Everything below is
derived from second-hand past behaviour (Thrivve built and uses ValueFlow), one stated interest (consultant #2)
and desk/codebase facts. The opportunity scores are **desk estimates by the diverger**, not survey data.

## 1. Raw request

> ADO Epic #5878 *Baseline*: "pick a baseline window, such as the 90 days before an engagement or improvement
> effort started, freeze the flow metrics for that window, and show today's values against them."

Since 2026-10-02 the maintainer has fixed the surface: a **Reports** tab on Team and Portfolio, **Create Report**
→ pick a Template, the before/after comparison is the first Template. The user **selects comparison items from
a list**, and each item carries a rule that judges it good or not (D16).

## 2. Job extraction (5 Whys)

| Layer | Statement | Why go higher? |
|---|---|---|
| Tactical (the request) | "Freeze the flow metrics of a past window and show today's next to them." | Nobody wants a frozen table for its own sake. |
| Operational | "Show the client or management what changed since the engagement or experiment started." | Why do they need to see it? |
| Operational | "So the people paying for (or sponsoring) the change can see whether it worked." | Why does that matter? |
| **Strategic** | "So a decision can be made on evidence: keep the new way of working, extend the engagement, or stop." | Higher is "keep the business healthy", a life-goal answer. **Stop.** |
| **Physical (irreducible function)** | Compare two states of one process against a yardstick that was fixed before looking, and separate a real change from routine variation. | — |

**First-principles inversion**

1. Activity: building a before/after slide or report.
2. Nobody wakes up wanting to build a report. The coach wants the sponsor to believe a true statement about the
   process, and the sponsor wants to decide.
3. Irreducible function: **a fixed "before" + a pre-agreed rule of what counts as better + a judgement that
   resists noise.** The selectable rules (D16) are the maintainer's own move toward the middle term.

## 3. Job statements

**Functional (primary, `flow-coach`, consulting and internal shapes)**

> When I (or the Team) have changed how work flows and someone asks whether it helped,
> I want to set today's flow against a "before" that cannot move, judged by rules agreed before looking,
> so I can say with evidence whether flow changed — and not present noise as progress.

**Emotional**: from exposed ("they are paying me; I have to show it worked") to vindicated or honestly informed
("Cycle Time shifted down; WIP held"). A "no change yet" must feel safe to show, not like failure.

**Social**: the coach is seen as rigorous, not as cherry-picking numbers; the sponsor (`delivery-lead-rte`) can
pass the result upward without having to defend the method.

**Reader job (candidate, `delivery-lead-rte`)**: "When a coach or Team shows me a before/after, I want to know in
one look which items got better and whether I can trust it, so I can decide without learning flow metrics."
Recorded for DISCUSS; not a separate jobs.yaml entry yet (no evidence it differs in success measure).

## 4. Disruption check

Is there a higher-level job that would make this one unnecessary? **"Know continuously whether the process is
improving"** — a live signal stream (the prospect counter-signal D6: "signals and email reports") would make a
dedicated before/after rarer. It does not remove it: an engagement has a start date and a sponsor, and
the "before" must be fixed to that date. The before/after is the bounded, sponsor-facing case of the same
physical function. Consequence for options: the rule machinery (item + direction + rule) should be reusable
by a future signal/alert surface, which argues for a catalog seam that is not report-specific.

## 5. ODI outcome statements

| # | Outcome statement |
|---|---|
| OS1 | Minimize the time it takes to produce a before/after of flow for a chosen start date |
| OS2 | Minimize the likelihood that the "before" values change after they were shown |
| OS3 | Minimize the likelihood of presenting routine variation as a change |
| OS4 | Minimize the time it takes a sponsor to tell whether each compared item got better |
| OS5 | Minimize the likelihood that what counts as "better" is chosen after seeing the numbers |
| OS6 | Minimize the likelihood that a "before" cannot be built because the engagement started in the past |

## 6. Opportunity candidates (desk estimates)

Score = Importance + max(0, Importance − Satisfaction), 1-10 scale.

| Outcome | Imp. | Sat. | Score | Status | Basis |
|---|---|---|---|---|---|
| OS1 time to produce | 8 | 4 | 12 | Served | Thrivve does it by hand/in ValueFlow; Nave's Executive Report does a rolling version |
| OS2 before does not move | 8 | 2 | 14 | **Under-served** | No researched product freezes; Lighthouse recompute drifts (X2) |
| OS3 noise as change | 8 | 3 | 13 | **Under-served** | Nave flags "significant shift" by user threshold; ActionableAgile has a PBC but no before/after judgement |
| OS4 sponsor reads it fast | 7 | 4 | 10 | Served | RAG indicators exist (Nave) |
| OS5 pre-agreed "better" | 7 | 2 | 12 | Borderline | Nobody asks for the rule before showing the result |
| OS6 retroactive before | 7 | 3 | 11 | Served (in Lighthouse by history) | V5; maintainer D12 |

Under-served: **OS2, OS3**, with OS5 close behind. OS2 is settled by D10 (frozen at creation). The open design
space is therefore **OS3 + OS5 + OS4**: how the rules are defined, how they resist noise, and how the verdict reads.

## Gate G1

- [x] Job at strategic level (decide keep/extend/stop) and physical level (fixed yardstick, signal vs noise).
- [x] No feature reference in the job statement ("report", "tab", "template" absent).
- [x] 6 ODI outcome statements (minimum 3).
- [x] Under-served outcomes identified (OS2, OS3; OS5 borderline). Scores flagged as desk estimates.

**G1: PASS.**

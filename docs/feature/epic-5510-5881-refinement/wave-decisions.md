# Wave Decisions — epic-5510-5881-refinement

ADO **#5510** Sizing Poker (Community, ICE 90) and **#5881** Refinement Need Chart (ValueFlow, ICE 60). One
workspace, to be split at DISCUSS (D1). DISCOVER decisions live in `feature-delta.md`.

---

## DIVERGE

**Agent**: Flux (`nw-diverger`) · **Date**: 2026-10-02 · **Interaction mode**: autonomous (subagent)
**Predecessor**: DISCOVER (light, desk research; D1-D19 maintainer-settled and not re-opened).
**Successor**: DISCUSS (`nw-product-owner`). Peer review (`nw-diverger-reviewer`) is to be dispatched by
the coordinator.

**Structure.** The six options are whole directions, and each answers sub-questions (a) need number,
(b) async voting and (c) live modes as one coherent bundle. Premium restrictions (d) were generated and
scored as a separate pool with their own locked rubric. A gate can sit on any direction, so scoring it
inside the taste matrix would mix concerns.

### Artifacts produced

| Path (relative to the feature workspace) | What it holds |
|---|---|
| `diverge/job-analysis.md` | 5-Why chain to a physical job (count plus triage), job statements, candidate voter job, disruption check, 7 ODI outcomes with desk-estimate scores, G1 |
| `diverge/competitive-research.md` | 2 unresolved identifications (flagged), 7 products/practices, 7 non-obvious alternatives, free-tier gating survey, local Premium-gating and feasibility evidence, 6 insights, G2 |
| `diverge/options-raw.md` | HMW, answer pools a1-a4 / b1-b6 / c1-c3, 7 SCAMPER directions, 4 Crazy 8s, curation to 6 with diversity test, Premium pool P1-P11, G3 |
| `diverge/taste-evaluation.md` | Locked weights, DVF, 6×5 matrix, breakdown, 4 sensitivity tests, Premium rubric, G4 |
| `recommendation.md` | Decision, top 3, per-sub-question answers, dissent with a pre-committed trigger, maintainer-only decisions |
| `wave-decisions.md` | This section |

### SSOT updates

`docs/product/jobs.yaml`:
- `updated:` → 2026-10-02;
- `feature_context:` gains `epic-5510-5881-refinement`;
- new job **`job-flow-coach-refine-just-enough`** (persona `flow-coach`, created 2026-10-02, importance 4 / satisfaction 2 / gap 2).

Changelog convention as in earlier waves: `jobs.yaml` has no changelog block, so the `updated:` date, the
`feature_context:` entry and the job's `created:` and `note:` fields are the record. The journey file
references two proposed job ids that do not exist. Both are ends of this one job, and DISCUSS should
re-point the journey (the journey file was not edited here).

### Decisions taken in this wave

- **DV-1. The job is count plus triage, and count is senior.** The physical job is "match preparation to the pull rate; spend
  effort where there is doubt". The count decides whether any voting is needed.
- **DV-2. Recommended direction: Option 4, "Stop sign first" (4.30).** It is a gauge-led Refinement tab. Votes sit on the Work Item,
  scoped to the gap and dated by the D17 cadence. There is no round entity. The in-person presenter view
  comes first among live modes. It is robust under all four sensitivity tests.
- **DV-3. The need number uses total Team Throughput** (maintainer override 2026-10-02). The existing `HowMany`
  Monte Carlo is used unchanged; expedites and bugs are refined too and share the same flow, so R2 does not
  hold. Both bounds come from one run.
- **DV-4. Async is pull, not push** (maintainer override 2026-10-02). Votes live in Lighthouse and people browse
  there; the readiness rule's "x votes needed" is what moves a Work Item on, and the missing votes are raised
  in the Team's own rituals (e.g. the daily). No push channel and no pre-committed push trigger. The
  research finding that async tools rely on push (competitive-research I1) is noted, not adopted.
- **DV-5. Community is always auth-off.** Authentication is Premium, so every Community voter is self-declared and per-browser.
  Named, accountable votes are already Premium by platform design.
- **DV-6. Premium shortlist.**
  - Lead lever: P5, sizing calibration.
  - P2: stated in the copy.
  - P9: a candidate.
  - Rejected: P8 (live sessions are the D8 gateway), P6 (goes against C10), P10 (cannot be enforced).
- **DV-7. Recommend persona `team-member-voter` at DISCUSS.** The voter's job measure, cost per vote and where the request
  reaches them, differs from the coach's.
- **DV-8. Usage data (D18, corrected by the coordinator).** Events are wanted, and DEVOPS designs them. The only
  distinction flagged here is the one R4 needs: whether a vote is cast inside or outside a live session.

### Flagged, not resolved

- The ProKanban-endorsed sizing tool **could not be identified** from public sources.
- Thrivve's ValueFlow chart is **not publicly verifiable**; the evidence is internal (#5881 demo, 2026-08-31).
- Opportunity scores are desk estimates; G2 from DISCOVER remains PARTIAL.

### Open for the maintainer

1. ~~Accept the R4 trigger, or fund push up front.~~ Resolved: pull only, no push (DV-4).
2. ~~Approve the Premium shortlist.~~ Resolved: D22, D25 in feature-delta.md (only named votes and sizing calibration are Premium).
3. ~~"Replenishment" wording.~~ Resolved: "Refinement" is a Terminology term; cadence = how often the Team refines (D26, D27).
4. ~~Minimum Yes = 0?~~ Resolved: minimum Yes is at least 1 (D30).
5. ~~Name the ProKanban-endorsed tool.~~ Resolved: SLE Poker Planning (D31).

# Taste Evaluation — epic-5510-5881-refinement

**Wave**: DIVERGE, Phase 4 · **Agent**: Flux (`nw-diverger`) · **Date**: 2026-10-02
**Inputs**: the six curated directions and the Premium pool from `options-raw.md`; outcome scores from
`job-analysis.md` §5; evidence from `competitive-research.md`.

---

## 1. Weights, locked before any scoring

| Criterion | Weight | Why |
|---|---|---|
| DVF (average) | 30% | Default |
| T1 Subtraction | 20% | Default. Constraint C6 and the "not thin, not big" stance in DISCOVER both reward an option that adds nothing removable |
| T2 Concept count | 20% | Default. The voter is a new kind of user, and every concept costs async adoption |
| T3 Progressive disclosure | 15% | Default |
| T4 Speed-as-trust | 15% | Default. Voter speed matters, but it is already counted in Desirability via O5. Raising T4 as well would count it twice |

The **default** profile was chosen over the "developer tool" profile on purpose. The primary persona is a
flow coach reading a forecast, not a developer in CI. §4 shows that the developer-tool weights do not
change the ranking.

## 2. DVF filter

| # | Option | D | F | V | Total | Avg | Evidence for the scores |
|---|---|---|---|---|---|---|---|
| 1 | Need sets the agenda | 4 | 4 | 4 | 12 | 4.00 | **D**: serves O1, O2 and O6 together. Async depends on the coach sharing a link, which no evidence supports or refutes. **F**: `HowMany` and the transition history exist, but a round entity, links and a schedule are new. **V**: ties both ADO items into one Community acquisition story |
| 2 | Always-open votes | 2 | 5 | 3 | 10 | 3.33 | **D 2**: no deadline and no request. Every working async product combines a request, a closing time and a cheap response (I1), and tracker-native voting (N1) is exactly the always-open pattern, used for priority rather than decisions. **F 5**: smallest build. **V 3**: little that a competitor lacks |
| 3 | Pushed rounds | 4 | 2 | 3 | 9 | 3.00 | **D 4**: the strongest external evidence on R4 (Async Poker, Agile Poker, GitHub review requests). **F 2**: Lighthouse has **no** outbound channel at all (0 matches for SMTP, webhook or Slack). It would need per-deployment secrets for hosted, standalone and Kubernetes, and a push hub voters can reach. **V 3**: infrastructure cost, though a possible Premium lever |
| 4 | Stop sign first | 4 | 4 | 4 | 12 | 4.00 | **D**: leads with O2 (14, the top outcome, which no competitor serves: I3). a3 removes R2 (O6) by construction. The async ask is small and dated but not pushed, so R4 is unproven and this is a 4, not a 5. **F**: the gauge is `HowMany` plus `GetLikelihood` on existing data. Votes, readiness and the cadence date are needed anyway. **V**: the one claim no competitor can make |
| 5 | Vote where you already type | 2 | 3 | 3 | 8 | 2.67 | **D 2**: no evidence that voters have the Lighthouse MCP configured, and web voters would come second. **F 3**: needs a Lighthouse-Clients release (D12 expects one anyway) |
| 6 | Objection window | 3 | 4 | 3 | 10 | 3.33 | **D 3**: the cheapest possible voter action, but silence counts as Yes, which works against O4 and needs D16 to allow minimum Yes = 0. **F 4** |

**Eliminations (DVF total < 6)**: none. Every option survives to taste scoring.

## 3. Scoring matrix

| # | Option | DVF | T1 Sub | T2 Concept | T3 Prog | T4 Speed | **Weighted** |
|---|---|---|---|---|---|---|---|
| 4 | **Stop sign first** | 4.00 | 4 | 4 | 5 | 5 | **4.30** |
| 2 | Always-open votes | 3.33 | 5 | 4 | 3 | 5 | **4.00** |
| 6 | Objection window | 3.33 | 4 | 3 | 4 | 5 | **3.75** |
| 1 | Need sets the agenda | 4.00 | 3 | 3 | 4 | 4 | **3.60** |
| 5 | Vote where you already type | 2.67 | 3 | 3 | 3 | 3 | **2.90** |
| 3 | Pushed rounds | 3.00 | 2 | 3 | 2 | 3 | **2.65** |

Arithmetic, top row: 4.00×0.30 + 4×0.20 + 4×0.20 + 5×0.15 + 5×0.15 = 1.20 + 0.80 + 0.80 + 0.75 + 0.75 = 4.30.

### Per-criterion breakdown

**T1 Subtraction**
- **2 (5)**: there is nothing left to remove; it has no rounds and no deadline.
- **4 (4)** and **6 (4)**: one element could go (4's gauge gating, 6's objection window).
- **1 (3)**: the round entity is removable, and Option 2 proves the job survives without it.
- **5 (3)**: two voter surfaces.
- **3 (2)**: channels, invitations, reminders, rounds and a hub.

**T2 Concept count**
- **4 (4)**: one new concept, the band, anchored to a fuel gauge. Votes on a Work Item and the readiness rule read as required approvals (I5).
- **2 (4)**: one new concept, readiness.
- **1 (3)**: round plus band.
- **6 (3)**: "silence counts as Yes" goes against expectation.
- **5 (3)**: MCP tools plus web.
- **3 (3)**: round plus channel configuration.

**T3 Progressive disclosure**
- **4 (5)**: the first interaction is one sentence ("Nothing more needs refining before Thu 8 Oct", or "3 Work Items need your view by Thursday"). Votes appear only when the gauge asks for them.
- **1 (4)**: read the number, press one button.
- **6 (4)**.
- **2 (3)**: vote controls on every refinement Work Item and the band, all at once.
- **5 (3)**.
- **3 (2)**: a channel must be configured before the first round.

**T4 Speed-as-trust**
- **4, 2, 6 (5)**: one click per vote, and the gauge is computed in the request.
- **1 (4)**: an extra step to create a round.
- **5 (3)**: assistant round-trip latency.
- **3 (3)**: delivery and reminder latency, and setup before first value.

## 4. Sensitivity

| Test | Change | Result |
|---|---|---|
| S1: the riskiest bet fails | Option 4's Desirability 4 → 3 (the dated, unpushed ask draws no async votes) | 4 = **4.20**, still first |
| S2: cheapest possible push | Option 3's Feasibility 2 → 4 (one incoming-webhook URL per Team, no email) and T3 2 → 3 (setup optional) | 3 = **3.00**, still last |
| S3: developer-tool weights (25/15/20/15/25) | — | 4 **4.40** · 2 4.08 · 6 3.88 · 1 3.65 · 5 2.92 · 3 2.70. Ranking unchanged |
| S4: what flips first place | Option 4's T1 4 → 3 *and* T3 5 → 4 | 4 = 3.95 < 2's 4.00. It takes a two-point swing on 4's strongest criteria |

The ranking is robust. **What the matrix cannot measure is R4.** Every direction except 3 bets that async
votes arrive without a push. That is the subject of the dissent in `recommendation.md`.

## 5. Premium-restriction proposals (question d): separate rubric

These are scored separately because a gate sits on top of any direction. The four criteria are equally weighted (25% each) and were locked before scoring.

- **CP, conversion pull**: who meets the gate, and whether they meet it at a moment of value.
- **CI, Community integrity**: the core job (count plus triage plus readiness) stays whole without a licence.
- **PF, pattern fit**: it reuses a gating mechanism Lighthouse already has (competitive-research §5).
- **EN, enforceability**: it holds in hosted, standalone and auth-off, where names are self-declared.

| ID | Proposal | CP | CI | PF | EN | **Score** | Verdict |
|---|---|---|---|---|---|---|---|
| P5 | **Sizing calibration**: votes compared with outcomes against the SLE | 4 | 5 | 4 | 5 | **4.50** | **Lead lever.** It is new value built on the C10 log and cycle times that already exist, and it is met by Teams that already use the free feature |
| P2 | Accountable, named votes come with authentication (already Premium) | 3 | 5 | 5 | 5 | **4.50** | **Adopt.** Nothing to build; only the copy needs to say it |
| P1 | No new scale gate; the free 3-Team cap already bounds it | 2 | 5 | 5 | 5 | 4.25 | Baseline, adopt |
| P9 | Cross-Team refinement overview | 3 | 5 | 4 | 5 | **4.25** | **Candidate.** It mirrors "Update All" being Premium. Not Portfolio-level sizing, so C1/D2 stand |
| P3 | Need number uses the Premium forecast filter when one is set | 1 | 5 | 5 | 5 | 4.00 | Consistency rather than a lever; it follows the existing `GetEffectiveRuleSet` behaviour |
| P11 | Renaming "replenishment" is Premium if it becomes a Terminology term | 1 | 5 | 5 | 5 | 4.00 | Incidental, and only if that term is added |
| P7 | Outbound reminders are Premium | 4 | 3 | 4 | 5 | 4.00 | **Conditional.** If async turns out to need a push, gating the push gates the core bet (CI 3). Decide only when push is built |
| P4 | Veto and rule override are Premium | 2 | 3 | 4 | 5 | 3.50 | Hold. It splits a D16 setting the maintainer framed as one Team setting |
| P8 | Live remote sessions are Premium | 3 | 2 | 4 | 4 | 3.25 | **Reject.** D8 makes live sessions the *gateway* for Planning Poker teams, and gating the acquisition path defeats it |
| P6 | Vote history retention capped in Community | 3 | 2 | 2 | 4 | 2.75 | **Reject.** Deleting a self-hoster's log goes against C10; Lighthouse has no retention gate today |
| P10 | Voter cap per Team in Community | 3 | 2 | 3 | 1 | 2.25 | **Reject.** It cannot be enforced against self-declared auth-off names |

## Gate G4

| Check | Result |
|---|---|
| DVF filter applied and eliminations documented | **PASS** (none eliminated) |
| Weights locked and documented before scoring | **PASS** (§1) |
| All surviving options scored on all criteria | **PASS** (§3) |
| Recommendation derivable from scores | **PASS**: see `recommendation.md`; it follows the top score, with the dissent made explicit |

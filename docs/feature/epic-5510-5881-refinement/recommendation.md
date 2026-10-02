# Recommendation — epic-5510-5881-refinement

**Wave**: DIVERGE (complete, pending peer review) · **For**: `nw-product-owner`, DISCUSS wave
**Agent**: Flux (`nw-diverger`) · **Date**: 2026-10-02
**ADO**: #5510 Sizing Poker (Community, ICE 90) · #5881 Refinement Need Chart (ValueFlow, ICE 60)

Supporting artifacts, all under `docs/feature/epic-5510-5881-refinement/`:
`diverge/job-analysis.md` · `diverge/competitive-research.md` · `diverge/options-raw.md` ·
`diverge/taste-evaluation.md` · `wave-decisions.md`.
SSOT: `docs/product/jobs.yaml`, job `job-flow-coach-refine-just-enough`.

---

## Maintainer overrides, 2026-10-02 (these win over the text below)

1. **The need number runs on total Team Throughput** — the existing `HowMany` Monte Carlo, unchanged.
   Expedites and bugs are refined too and go through the same flow, so R2 does not hold; the
   "Work Items leaving refinement" basis in §0, §1 finding 3 and DV-3 is withdrawn.
2. **Async is pull, not push.** Votes live in Lighthouse; people browse the Refinement view when they
   choose to. What moves a Work Item on is the readiness rule (D16, "x votes needed"), and the missing
   votes become a talking point in the Team's own rituals ("we need two more votes on this one" at the
   daily). No requests, reminders or inbox are needed; the push channel from Option 3 and the
   pre-committed push trigger in §0 and §5 are dropped. Usage data still shows whether votes arrive
   outside meetings (DV-8), but a low share does not by itself commit us to building push.

---

## 0. The decision, up front

> **Proceed with Option 4, "Stop sign first".** The Team's Refinement tab opens on one verdict about the next
> replenishment: *below range*, *in range* or *above range*. Then:
>
> - **In or above range**: it says plainly that nothing more needs refining, and asks for no votes.
> - **Below range**: it asks the Team for views on exactly the gap's worth of Work Items, in backlog
>   order, by the replenishment date.
>
> Votes (Yes / Yes, but… / No, with a comment) sit on the Work Item itself. There are no rounds to
> create, and the Team's replenishment cadence (D17) is the deadline. The count comes from the rate at
> which Work Items **leave** the Team's refinement state(s), not from total Throughput.
>
> **Assuming**: a small, dated ask that is reachable through one stable per-Team link gets votes outside a
> meeting **without** Lighthouse pushing into anyone's inbox. This is R4, still the riskiest bet. The
> research points against it (§1, I1). The recommendation therefore carries a **pre-committed trigger**:
> if the usage data shows votes still arriving only inside meetings, the next increment is the push
> channel from Option 3 (§5).

This is one direction. The runner-up and the evidence-based dissent are argued in full in §5.

## 1. Four findings that change the brief

1. **Async works only when the request lands in an inbox that has a deadline.** Every async product that works
   pairs three things: a request in a channel people already watch, a closing time, and a cheap answer.
   Examples: Async Poker for Jira (Slack/email at start, on new items, before the deadline), Agile Poker
   (due date plus scheduled reminders), GitHub review requests (notification inbox, merge blocked), Rust
   FCP (one-week window). **Lighthouse has no outbound channel at all**: there is no SMTP, webhook or Slack
   code. This is the most likely reason MVP votes arrived in one meeting window (D19). It is new evidence
   on R4, and it is evidence *against* the async bet as currently framed.
2. **Community is always auth-off.** Authentication is itself a Premium feature (`BlockedPage.tsx`: "Authentication is a
   Premium feature… or disable authentication… to restore anonymous access"). So every Community voter
   is in the D11 regime: anyone may vote, with a self-declared name held per browser (ADR-191 is the
   precedent). D14's account-required voting only ever occurs on Premium instances. This matters for (c)
   and (d).
3. **R2 can be designed out rather than corrected.** Run `HowMany` over the **commitment-point Throughput**: the number of
   Work Items leaving the last refinement state per day, which is the rate the ready queue is actually
   drained. A Work Item that never passed through refinement is then not counted at all. The data
   already exists: `WorkItemStateTransition` comes from the source for Jira, ADO and Linear (ADR-017), with
   sync-delta capture for CSV. A ratio correction or the Premium forecast filter would only reduce the
   bias, not remove it.
4. **No product says "stop".** The incumbent answer to "how many" is a fixed buffer ("ready for the next two
   sprints", "readiness rate above 80%"), and it only ever says "more". O3 (do not run dry) scores 8,
   over-served. O2 (do not over-refine) scores **14**, the top outcome. C5 is the differentiator, not just
   a guardrail.

The ProKanban-endorsed sizing tool **could not be identified** from public sources, and ValueFlow's chart is
**not publicly verifiable** (competitive-research §1). Neither identification is guessed.

## 2. Top 3

Full matrix: `diverge/taste-evaluation.md` §3.

### 1st — Option 4 · Stop sign first — **4.30**
**Why it scores well**: top on progressive disclosure (5) and speed (5). The first interaction is one
sentence, and voting appears only when the gauge asks for it. It has one new concept (the band), and the
readiness rule reads as "required approvals", which developers already know (I5). DVF 4.0, because it
leads with the highest-scoring outcome (O2, 14), which no competitor serves.
**Core trade-off**: nothing pushes the ask. It relies on the coach's one stable link and on people opening
Lighthouse.
**Key risk**: R4. If dated, in-app asks draw no async votes, this direction still delivers the count and a
shorter in-person meeting (c1), but not the async half of #5510.
**Hire criteria**: a coach whose Team over-refines, or who wants to say "stop" with evidence.

### 2nd — Option 2 · Always-open votes — **4.00**
**Why it scores well**: it is the purest subtraction (T1 5): no rounds, no deadline, no gating.
**Core trade-off**: nobody is ever *asked* to vote. This is the tracker-native voting pattern, which in Jira
and ADO is used for priority, not decisions.
**Key risk**: Desirability 2 is the lowest of the top three, for exactly the I1 reason.
**Hire criteria**: a Team that already lives in Lighthouse every day.

### 3rd — Option 6 · Objection window — **3.75**
**Why it scores well**: the cheapest voter action of all (speak only to object), modelled on Rust's FCP.
**Core trade-off**: silence counts as Yes, which works against O4 (unvoiced doubt). It also needs D16 to
allow minimum Yes = 0.
**Hire criteria**: a mature Team with high trust and a small, well-understood backlog.

## 3. Recommendation and what it absorbs

**Option 4**, because it has the top score and is robust under all four sensitivity tests. Flipping first
place takes a two-point swing on its two strongest criteria (taste-evaluation §4).

Two pieces from runners-up are folded in, as wording rather than mechanism:
- **From Option 2**: votes live on the Work Item, with no round entity.
- **From the merged "review requests" idea**: D16 is phrased to the voter as required approvals.

**Option 1 (Need sets the agenda, 3.60) is not adopted.** Its round entity is the one removable element
that Option 4 avoids by using the cadence date as the deadline.

### Answers per sub-question

| Q | Answer in the recommended direction |
|---|---|
| **(a) Need number** | `HowMany` over commitment-point Throughput, horizon = days until the next replenishment (D17), minus Work Items currently ready. The band's lower and upper bounds come from one run (`GetProbability`). The sentence is phrased as likelihood (`GetLikelihood(readyCount)`), e.g. "about 1 in 5 chance the ready Work Items run out before Thu 8 Oct". **Above the upper bound the tab says "stop" with the same weight as "refine more" (C5).** The existing minimum-data guard applies to the new series. For a CSV Team whose sync-delta history is too short, the tab says so in words rather than falling back silently to total Throughput |
| **(b) Async** | Votes on the Work Item. The ask is scoped to the gap and dated by the replenishment. There is one stable per-Team link that the coach pins in the Team's own chat once, rather than one per round. Others' votes stay hidden until the voter has cast their own: anchoring protection is table stakes (O4), not a differentiator. D16 readiness is evaluated continuously. No push in the first increment; see the trigger in §5 |
| **(c) Live** | **c1 presenter view first**: the same page on one shared screen, where the coach records votes called out in the room. It needs no push channel, so it works standalone and auth-off (C2, D3). **c2 remote facilitated later**: under D14 every auth-on voter has an account, so `[Authorize]` on the hub is not the blocker there. DESIGN must check whether the auth-off shared subject passes `[Authorize]`, and must use a per-browser self-declared name for identity (C3, ADR-191 precedent) |
| **(d) Premium** | Lead lever **P5, sizing calibration** ("of the Work Items you voted Yes, 82% finished within the SLE"). It is new value on top of the C10 vote log and existing cycle times, and Teams meet it after the free feature has paid off. **P2**: named, accountable votes come with authentication, which is already Premium; nothing to build. **P9 candidate**: a cross-Team refinement overview, which mirrors "Update All" being Premium. **P3/P11** follow automatically from existing gates. **Rejected**: live sessions as Premium (P8; they are the gateway, per D8), capped vote history (P6; goes against C10), voter caps (P10; cannot be enforced against self-declared names) |

### Usage data (D18): what this direction makes measurable

The events themselves are designed in DEVOPS. To test R4, the one distinction the catalogue needs is
**whether a vote was cast inside a live session or outside one**. With that, Option 4 lets the catalogue
show:
- the Refinement tab opened;
- a vote cast;
- live versus not;
- a live session run.

Option 3 would additionally make "reminder sent → vote" measurable, but only once push exists.

## 4. Wording note: "replenishment" is not a configurable term

Teams call this moment replenishment, planning, sprint planning or refinement-to-commit. Two ways forward,
for DISCUSS or the maintainer:
- **Speak in dates** ("before Thu 8 Oct"). No new term is needed. Flux prefers this.
- **Add "Replenishment" to Settings → Terminology.** Renaming then becomes Premium automatically (P11),
  because Terminology editing is Premium.

Every other user-facing word is already configurable (Work Item, Team, SLE, Throughput) and must render as
the instance's own term.

## 5. Dissenting case

**Runner-up the scoring almost chose: Option 2 (4.00).** It is simpler (T1 5), and its only weakness is the
async ask. If DISCUSS concludes that gauge-gated solicitation adds more than it saves (S4), Option 2 is the
fallback. It loses the stop sign's ability to *close* the ask, which is the C5 behaviour.

**Evidence-based dissent: Option 3, Pushed rounds (2.65, last).** The matrix ranks it last for build cost
and setup friction, but it is the **only** direction backed by the external evidence on the riskiest bet
(I1). The honest reading is that the matrix scores *taste and feasibility*, while R4 is an *empirical*
question it cannot settle. So:

> **Pre-committed trigger.** After the first increment has been in use for a few replenishment cycles, read the
> usage data. If votes cast outside live sessions are a small minority, build the push channel next. That
> means webhooks first (the cheapest variant: one incoming-webhook URL per Team, S2) and email later, and
> it would be a candidate Premium lever (P7, decided then, not now). If async votes do arrive, Option 4
> stands as built.

## 6. Decisions only the maintainer can make

1. **Accept the R4 trigger as framed.** Push is deferred and decided by the usage data, and its tier is decided only when it is built.
   Or fund push up front despite the cost.
2. **Premium shortlist.** P5 as the lead lever; P2 stated in the copy; P9 as a candidate; P8, P6 and P10 rejected.
3. **"Replenishment" in the UI**: dates only, or a new Terminology entry (§4).
4. **Whether D16 may be set to minimum Yes = 0.** That would allow the objection-window behaviour (Option 6) as a Team configuration.
5. **Name the ProKanban-endorsed tool**, so DISCUSS can position against it on evidence.

## 7. For DISCUSS

- **Create persona `team-member-voter`.** The voter's success measure is cost per vote and where the request reaches
  them, which differs from the coach's (job-analysis §3).
- **Decide the band's bound percentiles** and the exact sentence. Read both bounds from one `HowMany` run.
- **Define "leaving refinement"** under D15: the transition out of the last configured refinement state,
  or out of the "ready" split when rules define one. This includes states mapped to Doing (C8).
- **Split proposal** for D1:
  - (i) the count and gauge, #5881-shaped, which is useful on its own and tests R2's fix;
  - (ii) votes and readiness, #5510-shaped, which consume the gap from (i);
  - (iii) live remote, later.
- **The cheapest R4 evidence is still free**: read the letpeople.work/sizing-poker vote timestamps against
  invitation times before DISCUSS commits to the async copy.

## Decision for DISCUSS

> **Proceed with Option 4, "Stop sign first"**: a gauge-led Refinement tab whose count comes from commitment-point
> Throughput and whose band says "stop" as loudly as "refine more". Votes sit on the Work Item, scoped to
> the gap and dated by the replenishment cadence. The in-person presenter view comes first among live
> modes. **This assumes** async votes arrive without a push, which is unproven and contradicted by the
> competitor evidence. That risk is accepted only together with the pre-committed trigger in §5.

# Competitive Research — epic-5510-5881-refinement

**Wave**: DIVERGE, Phase 2 · **Agent**: Flux (`nw-diverger`) · **Date**: 2026-10-02
**Method**: web research (WebSearch/WebFetch) plus a local code read. Each claim is cited below. Where a
claim could not be verified, the entry says so rather than guessing.

---

## 1. Identification flags (read these first)

| Asked to identify | Result | Confidence |
|---|---|---|
| The free SLE-based sizing tool endorsed in ProKanban Slack (#5510) | **Not identified.** Searches for SLE/right-sizing poker tools, "sizing poker", "flow poker" and ProKanban-linked tools all returned nothing that matches. ProKanban's own right-sizing article names the SLE as the yardstick but links no tool [P1]. ProKanban's free "AI skill" (Liz Rettig) is a flow-metrics assistant, not a sizing tool [P2]. Slack content is not indexed. | **Identified by the maintainer, 2026-10-02:** see the note below the table. |
| Thrivve Partners "ValueFlow" refinement-need chart (#5881) | **Not publicly verifiable.** Thrivve's public products are FlowViz (free Power BI template for Jira/ADO) [T1] and their Medium blog [T2]. No "ValueFlow" product or refinement chart appears on thrivve.partners/insights [T3]. The only evidence is internal: the demo by Paul Brown on 2026-08-31, recorded on ADO #5881. | **Low** (internal evidence only, not re-verified here) |

## 2. Products and practices that serve the job

The job (job-analysis.md §3) has two functions: **count** (how many to prepare) and **triage** (which
ones carry doubt). Each entry below says which function it serves.

| # | Product / practice | Category | Serves | What it does well | Where it fails the job | Assumption it makes about users |
|---|---|---|---|---|---|---|
| 1 | **Lunar Logic "No Bullshit Estimation" cards** [L1] | Sizing method (physical cards) | Triage | Three categories instead of numbers: **1** (normal), **TFB** ("too f… big", split it), **NFC** ("no f… clue"). Discussion is about granularity and uncertainty, and "the most reliable productivity metric is simply counting completed work items". This is the conceptual parent of D5 (Yes / Yes, but… / No). | No SLE anchor ("1" is relative to the group's gut). Synchronous only. No count of how many are needed. | The whole Team is in one room, and "too big" is self-evident without a yardstick |
| 2 | **Parabol Sprint Poker** [PA1, PA2] | Planning Poker app (open source, SaaS) | Triage | Pulls items from Jira/GitHub/GitLab/ADO and writes estimates back. Described as async-friendly: the facilitator scopes ahead of the meeting. Also supports WSJF. | Story points or effort scales, not SLE fit. No count. Free tier capped at **2 teams, 10 meetings/month, 30-day history** [PA3]. | Estimation is a meeting, and the scale is numeric |
| 3 | **PlanITpoker** [PI1] | Planning Poker app (SaaS) | Triage | "Quick Play" needs no registration, rooms are unlimited, items can be imported from CSV. | Numeric decks. Synchronous. Free plan stated as **7 participants**, $20/month for unlimited [PI1] (a third-party review says no documented limit on quick play [S1]; the sources conflict). | Zero setup beats integration |
| 4 | **Async Poker: Planning Poker & Estimation for Jira** [AP1] | Jira Marketplace app | Triage | **The clearest async design found.** A game is created from a board, sprint, JQL or selection. Estimates and **written reasoning** stay **hidden until reveal**. **Notifications go to Slack and email when a game starts, when items are added and when a deadline approaches**, with per-participant status. The Review step focuses discussion on outliers. Writes estimates back to Jira. | Points, not SLE fit. Jira only. Paid. | **Async happens only when the request is pushed into an inbox, with a deadline** |
| 5 | **Agile Poker for Jira (Appfire)**: async session [AG1, AG2] | Jira Marketplace app | Triage | The moderator picks the issues and the participants and shares a link via Slack or email. Participants estimate "whenever they find it convenient". A **due date** is shown in the session and in invitations. **Reminders are scheduled relative to the due date** ("1 day before"). There is a Slack integration for invitations. | Points. Jira only. | Same as #4: invite, deadline, reminder |
| 6 | **Thrivve Partners ValueFlow refinement-need chart** (internal evidence, see §1) | Flow-metrics product | Count | Per #5881: throughput against the ready queue, shown as a recognised practice (V4). | Not verifiable publicly, so its upper-bound behaviour is unknown. | — |
| 7 | **Focused Objective (Troy Magennis)** Monte Carlo spreadsheets, Story Count Forecaster [FO1, FO2] | Free forecasting spreadsheets | Count (indirect) | Monte Carlo over historical throughput, and "story count instead of size estimation" once items are split small. | Answers "when will the backlog be done", not "how many to refine before the next replenishment". No ready-queue concept. Manual. | The user can frame the question themselves |

## 3. Non-obvious alternatives (different category, same job)

| # | Alternative | Serves | Why it matters here |
|---|---|---|---|
| N1 | **Jira native issue voting** [J1] and the **ADO Work Item Voting extension** [J2] | Triage (attempted) | Voting already lives inside the tracker, but it measures *priority* (a vote count), not *fit*. Jira voting must be switched on by an admin. The ADO extension stores votes in two custom fields. Lesson: a vote on the Work Item itself, without a round, is a familiar gesture. It also shows the risk of an always-open vote: nobody is ever asked to cast it. |
| N2 | **Slack polls (Simple Poll)** [SL1] | Triage | Anonymous voting, **recurring polls, automatic closing**, an API. Teams already make small async decisions where they talk. Slack has no robust native poll [SL2]. Lesson: the async channel that already works is the chat the Team lives in. |
| N3 | **GitHub pull-request reviews** [GH1, GH2] | Triage (structural analogue) | Three review outcomes, **Approve / Request changes / Comment**, plus **"required approving reviews"** enforced by branch protection. This maps one-to-one onto D5 and D16: Yes ≈ Approve, No ≈ Request changes, Yes-but ≈ comment with a condition, "minimum Yes votes" ≈ required approvals, veto ≈ blocking change request. Developers already run this async loop daily. **It works because a review request lands in a notification inbox, and because the merge is blocked until it is answered.** |
| N4 | **Rust RFC Final Comment Period** [RF1] | Triage (governance) | A time-boxed (one week), async, **lazy-consensus** decision ("does not require consensus amongst all participants"), with a disposition announced up front. Lesson: a closing time plus "speak now or it proceeds" makes async decisions finish without everyone voting. |
| N5 | **Kanban Method replenishment meeting** [K1, K2, K3] | Count + triage (practice) | Replenishment means "reviewing the requested work items that fulfil the 'ready for delivery' criteria and selecting the ones to be pulled… based on available capacity" [K1]. Typical cadence is weekly or on demand, 20-30 minutes [K2]. The commitment point sits at replenishment [K1]. Lesson: the practice already defines the cadence (D17) and the capacity question. Lighthouse adds the probabilistic count. |
| N6 | **"Refine two sprints ahead" heuristic / Backlog Readiness Rate** [BR1] | Count (rule of thumb) | "Percentage of top-sprint stories that meet your Definition of Ready… healthy benchmark above 80% for the next two sprints. Below 60% signals a broken refinement process." The incumbent answer to "how many" is a fixed buffer that **only ever says "more"**. This is the habit that O2 and C5 push against. |
| N7 | **ProKanban right-sizing practice** [P1] | Triage (method) | "If your SLE says 80% of items complete within six days or less, then a right-sized story is one the team would expect to complete within six days or less." It happens at work entry, before items flow. This is the method D5 implements. No tool is attached. |

## 4. How the competitor field gates its free tier (input to question (d))

| Tool | Where "free" stops | Source |
|---|---|---|
| Parabol | 2 teams, 10 meetings/month, 30-day history, AI features paid | [PA3] |
| PlanITpoker | 7 participants (sources conflict) | [PI1], [S1] |
| Planning Poker Online | 9 votings per game, 5 issues per game, games kept 6 weeks | [S1] |
| Kollabe | 10 members per room, 7-day history, monthly meeting cap | [S1] |
| Pointing Poker / Scrum Poker Online | Ad-funded; paid tier removes ads and adds timer and statistics | [S1] |

The pattern: competitors gate **scale** (teams, participants), **memory** (history retention), **extras**
(AI, statistics) and **integrations**. None gates the core act of voting.

## 5. Local evidence: how Lighthouse gates Premium today

Read with `ctx_search` and `ctx_read` on 2026-10-02.

| Mechanism | Where | Notes |
|---|---|---|
| `[LicenseGuard(RequirePremium = true)]` returns 403 "Premium Features Required" on write endpoints | `LicenseGuardAttribute.cs`; used on blackout periods, recurring blackout rules, OAuth connect/disconnect, terminology update, feature ordering, feature move, delivery-rule validation, delivery sources, Update All Teams/Portfolios | The standard gate is on the **write**, with the read left open |
| Entity caps: free licence allows **3 Teams** (`MaxAllowedTeams`) and **1 Portfolio** | `LicenseGuardAttribute.cs` L13-15; `TeamsController.CreateTeam` | Community refinement is therefore already bounded at ≤3 Teams, with no new gate |
| Configuration kept but **effect switched off** without a licence | `ForecastFilterRuleService.GetEffectiveRuleSet` returns null if not Premium; `GetStoredRuleSetJsonForEditing` still shows the rules. Cycle-time definitions are preserved on a free save (`TeamController` L162-178) | "Downgrade keeps your setup; Premium makes it act" |
| View but not edit | `TerminologyConfiguration.tsx` ("You can view the current terminology below, but editing is restricted to premium") | Read-only UI with `LicenseTooltip` (`canUseFeature`, `premiumExtraInfo`) |
| **Authentication itself is Premium** | `BlockedPage.tsx` L67: "Authentication is a Premium feature… or disable authentication… to restore anonymous access." | **So Community instances always run auth-off.** Every Community voter is in the D11 regime (anyone may vote, identity self-declared). D14's account-required voting only exists on Premium instances |
| Throughput noise filter (Premium) | `Team.ForecastFilterRuleSetJson` and `ForecastFilterRuleService` | A Premium Team may already exclude bugs and expedites from forecast throughput, which partly mitigates R2 for Premium only |

Other local facts that change option feasibility:

- **There is no outbound notification channel at all**: no SMTP, email sender, webhook or Slack client in
  `Lighthouse.Backend` (`ctx_search` for `Smtp|EmailSender|Webhook|SlackClient|IncomingWebhook` returned 0
  matches). Any "push into the inbox" mechanism (#4, #5, N3) means new infrastructure for both hosted and
  standalone.
- **State-transition history exists**: `WorkItemStateTransition` is captured from the source for Jira, ADO
  and Linear, with a sync-delta fallback for CSV (ADR-017). So "how many Work Items leave the refined
  state(s) per day" (the rate the ready queue is drained) can be computed from stored data. That gives
  an R2-free count for all connectors except a young CSV Team.
- `ForecastBase.GetProbability(percentile)` takes any percentile, and `GetLikelihood(threshold)` returns
  the likelihood of reaching a count. Both an upper and a lower bound, and a sentence like "x% chance the
  ready Work Items run out", can be read from one `HowMany` run.
- `UpdateNotificationHub` is `[Authorize]`. Under D14 (auth on → account required, no guest links), every
  auth-on voter has an account, so the attribute is **not** the blocker for auth-on live sessions. Whether
  `[Authorize]` admits the shared auth-off subject must be checked in DESIGN.
- A per-browser pseudonym precedent exists (ADR-191, analytics identity). It is the obvious model for the
  auth-off self-declared voter.

## 6. Insights that change the brief

- **I1. Async works only when the request is pushed into an inbox that has a deadline.** Every async product
  that works (#4, #5, N2, N3, N4) combines **(a) a request in a channel people already watch, (b) a closing
  time, and (c) a cheap response**. Lighthouse has none of (a) today. That is the most likely mechanism
  behind D19, where MVP votes came in one meeting window. It is evidence on R4 that the maintainer has
  not seen before.
- **I2. Nobody combines count and triage.** Poker tools triage and never count. Forecasting tools count and
  never triage. Lighthouse holds the Work Items and the throughput, so it is the only product in this list
  that can do both.
- **I3. Nobody says "stop".** The incumbent count is a fixed buffer (N6) that only says "more". An upper bound
  (C5) has no competitor.
- **I4. Free tiers gate scale, memory, extras and integrations, never the core verb.** This matches Lighthouse's
  own pattern (write-gates, entity caps, config kept with the effect off).
- **I5. Developers already know D16's readiness rule as "required approvals".** Using that frame costs zero new
  concepts for the voter (T2).
- **I6. Community is always auth-off.** Proposals that rely on per-user accountability are already Premium
  by platform design, without a new gate.

## Gate G2

| Check | Result |
|---|---|
| 3+ real products named | **PASS**: 7 products and practices (§2) plus 7 alternatives (§3) |
| At least one non-obvious alternative | **PASS**: GitHub reviews, Rust FCP, Slack polls, tracker-native voting, the replenishment practice, the readiness heuristic |
| No generic market claims | **PASS**: each claim is cited. Two identifications are explicitly unresolved (§1) |

## Sources

- [P1] ProKanban, "Right Sizing Is Not the Same as Same Sizing" — https://prokanban.org/blog/right-sizing-is-not-the-same-as-same-sizing
- [P2] ProKanban, AI Tools (Professional Kanban skill) — https://prokanban.org/ai
- [T1] FlowViz for Jira (Cloud), Thrivve Partners — https://marketplace.microsoft.com/en-us/product/web-apps/thrivvepartners1747925071876.flowviz_jira
- [T2] Thrivve Partners on Medium — https://medium.com/thrivve-partners/avoiding-workflow-wreckage-three-levels-of-prioritization-to-keep-your-team-on-course-f61d4be6480e
- [T3] Thrivve Partners Insights — https://thrivve.partners/insights
- [L1] Lunar Logic, No Bullshit Estimation — https://estimation.lunarlogic.io/
- [PA1] Parabol Sprint Poker — https://www.parabol.co/agile/sprint-poker/
- [PA2] Parabol, Planning Poker guide (remote and async) — https://www.parabol.co/resources/planning-poker-guide/
- [PA3] Parabol pricing — https://www.parabol.co/pricing/ ; TeamRetro comparison — https://www.teamretro.com/compare/parabol-alternative/
- [PI1] PlanITpoker pricing — https://planitpoker.com/pricing/
- [S1] Scrumpy, "Best free planning poker tools… where each one stops being free" — https://scrumpy.it/blog/best-free-planning-poker-tools
- [AP1] Async Poker for Jira — https://marketplace.atlassian.com/apps/1221281/async-poker-planning-poker-estimation-for-jira
- [AG1] Appfire, Agile Poker async session — https://appfire.atlassian.net/wiki/spaces/JPP/pages/148570624/Asynchronous+Session
- [AG2] Appfire, Slack integration with Agile Poker — https://hub.appfire.com/popular-topics/software-development-and-devops/slack-integration-with-agile-poker/
- [FO1] Focused Objective — https://focusedobjective.com/
- [FO2] Troy Magennis, Monte Carlo introduction — https://www.slideshare.net/troymagennis/introduction-to-monte-carlo-analysis-for-software-development-troy-magennis-focused-objective
- [J1] Atlassian, watch and vote on work items — https://support.atlassian.com/jira-service-management-cloud/docs/watch-and-share-issues-from-the-new-issue-view/
- [J2] Azure DevOps Work Item Voting extension — https://github.com/samartzidis/azure-devops-extension-voting
- [SL1] Simple Poll — https://simplepoll.rocks/
- [SL2] Geekbot, Slack poll apps — https://geekbot.com/blog/slack-poll/
- [GH1] GitHub Docs, approving a PR with required reviews — https://docs.github.com/en/pull-requests/how-tos/review-pull-requests/approving-a-pull-request-with-required-reviews
- [GH2] GitHub Docs, managing PR reviews in a repository — https://docs.github.com/en/repositories/managing-your-repositorys-settings-and-features/managing-repository-settings/managing-pull-request-reviews-in-your-repository
- [RF1] The Rust RFC Book — https://rust-lang.github.io/rfcs/
- [K1] Kanban University glossary — https://kanban.university/glossary/
- [K2] Businessmap, Kanban replenishment — https://businessmap.io/kanban-resources/getting-started/kanban-replenishment
- [K3] Nave, Kanban meetings — https://getnave.com/blog/kanban-meetings/
- [BR1] NextAgile, Product backlog health metrics — https://nextagile.ai/blogs/agile/product-backlog-health-metrics/
- Local: `Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/Licensing/LicenseGuardAttribute.cs`,
  `…/Services/Implementation/Forecast/ForecastFilterRuleService.cs`, `…/API/TeamController.cs`,
  `…/Models/Forecast/ForecastBase.cs`, `…/Services/Implementation/BackgroundServices/Update/UpdateNotificationHub.cs`,
  `Lighthouse.Frontend/src/components/App/Auth/BlockedPage.tsx`, `…/components/TerminologyConfiguration.tsx`,
  `docs/product/architecture/adr-017-transition-capture-dispatch.md`, ADR-191.


## Addendum, 2026-10-02 — the ProKanban-endorsed tool

**SLE Poker Planning** ("Planning Poker Revisited"), https://planning-poker-revisited.web.app/, by José
Coignard ("The Flowmizer"), v1.0.1. Three cards tied to the SLE: *comfortably fits within our SLE goal*
(green), *too big or risky, likely to blow the SLE* (red), *unclear, cannot estimate*. A facilitator sets
the SLE (timebox and confidence %), reveals the cards and resets rounds; Teams join a table by a shared
session name, with no accounts shown. Synchronous. Nothing on the page indicates tracker import — Work
Items appear to be discussed without the tool knowing them. Pricing not stated.

**Read-across**: it already proves the SLE-based three-answer question (D5) lands with this audience, and
its "unclear" card maps onto our "Yes, but…" only loosely (theirs is "can't tell", ours is "yes under
conditions"). Lighthouse's difference is that it already knows the Team's Work Items, its SLE and its
Throughput — nothing is entered by hand — and votes persist between meetings.

# Content Spec — Flow & Forecasting Readiness Assessment (ADO #5123)

Status: content LOCKED 2026-06-01 (refinement session). This is the SSOT for the assessment
questions, band copy, results email, and email-gate copy. Implementation lands in the **website**
repo (`/storage/repos/website`). Sibling: [lighthouse-user-survey content-spec](../lighthouse-user-survey/content-spec.md).

House rules for all product copy below: no em-dashes; "We …" / "you" voice; metric names title-cased.

---

## 1. The six questions

Pillars unchanged (measure = Q2/Q3/Q5, forecast = Q1/Q4/Q6). Internal question `id`s stay the same to
avoid churn in scoring/session order even where the prompt meaning shifted.

### Q1 · `forecasting-method` (forecast)
Prompt: **How do you decide when work will be done?**
- 0 — We rely on gut feeling or an expert's best guess.
- 1 — We add up estimates.
- 2 — We use past delivery data to forecast.
- 3 — We use probabilistic forecasting and continuously re-check it.

### Q2 · `metrics-reviewed` (measure)
Prompt: **Which delivery metrics do you review?**
- 0 — We don't track or review metrics.
- 1 — Velocity, Story Points, Planned vs. Actual.
- 2 — Throughput and Cycle Time.
- 3 — Work Item Age and WIP (on top of Throughput and Cycle Time).

### Q3 · `wip-management` (measure)
Prompt: **How do you manage Work In Progress (WIP)?**
- 0 — We don't actively review or act on WIP.
- 1 — We set WIP limits but struggle to act on them.
- 2 — We actively keep WIP from getting too high.
- 3 — We control WIP so we're neither overloaded nor starved for work.

### Q4 · `date-likelihood` (forecast) — reframed to reliability
Prompt: **When you commit to a date, how reliably do you hit it?**
- 0 — We regularly miss, and usually find out late.
- 1 — We sometimes hit it, often after a late scramble.
- 2 — We're usually accurate, but can't say how likely up front.
- 3 — We commit to a likelihood and land within it as expected.

### Q5 · `data-activation` (measure)
Prompt: **How do you use delivery data to make decisions?**
- 0 — We don't, because we have no data or can't find it.
- 1 — We have the data, but rarely base decisions on it.
- 2 — We pull reports now and then to inform a discussion.
- 3 — Live flow data actively steers our planning and expectations.

### Q6 · `deadline-at-risk` (forecast)
Prompt: **What happens when a target date starts to look at risk?**
- 0 — We only notice once it's too late to act.
- 1 — We react late with overtime or scope-cutting.
- 2 — We spot the risk early and discuss options with the team and stakeholders.
- 3 — Our forecast continuously flags the risk, and we re-steer scope or expectations with data.

> The "0 — / 1 —" markers above are list formatting in this doc only; the actual option `label`
> strings carry NO leading "N —" prefix and NO em-dash.

---

## 2. Scoring (unchanged)

- `rawSum` = sum of six 0-3 answers (0-18).
- `score = round(rawSum / 18 * 100)` (0-100).
- Bands exhaustive / non-overlapping: 0-25, 26-50, 51-75, 76-100.
- `credibilityAnchor` (unchanged): "Based on Kanban flow metrics and probabilistic forecasting principles (Vacanti / ProKanban)."

---

## 3. The four bands

Band NAMES changed: `Output-focused` → **Drifting**, `Probabilistic` → **Predictable**
(=> `BandName` type in `scoring.ts` + every reference/test/feature file).

Content model is now **3 fields** (the old `nextRung` field is DROPPED; its content folded into the
reads). Principle: the band NAME says where you are; the `tagline` says how to move up; each read
gives "where you are + your next move" on one pillar.

### Band 1 · Flying blind (0-25)
- tagline: Start measuring flow and run your first forecast.
- measureRead: You're not measuring flow yet, so you can't see how work moves. Add your team in Lighthouse and start watching your flow metrics. It's easy to get the data flowing and learn from it.
- forecastRead: Forecasting is gut feel today. Run a forecast for your next iteration and see how it performs. The goal right now is to experiment, not to overhaul how you work.
- on-page CTAs: ① Start with Lighthouse Community (free) → `https://letpeople.work/lighthouse`

### Band 2 · Drifting (26-50)
- tagline: Swap guesses for real data and start steering.
- measureRead: You're tracking output like Velocity or Story Points, not the flow metrics that show how predictably work moves. Start watching Throughput and Cycle Time, and set improvement goals like a Service Level Expectation (SLE) or WIP limits.
- forecastRead: You forecast by adding up estimates, betting on what you thought would happen rather than what actually does, and a single date leaves no room to manage risk. Compare your estimates against what really happened (Cycle Time), and backtest a Monte Carlo forecast to see how it would have performed.
- on-page CTAs: ① Start with Lighthouse Community (free)

### Band 3 · Flow-aware (51-75) — sweet spot
- tagline: Start acting to get more predictable.
- measureRead: You're already watching flow metrics. Now sharpen predictability: act on leading indicators like Work Item Age, use your Service Level Expectation to drive improvements, and analyse how stable your delivery is with Process Behaviour Charts.
- forecastRead: Your forecasting works at team level. Now extend it to the portfolio, and keep checking the trend of your deliveries over time rather than a single date.
- on-page CTAs: ① Use Community to operationalize forecasting (free) ② Book a workshop → `https://letpeople.work/#workshops`

### Band 4 · Predictable (76-100)
- tagline: Fine-tune the details and scale across your portfolio.
- measureRead: Flow metrics are second nature. Now fine-tune: study your workflow to find bottlenecks, set more ambitious Service Level Expectations and bring your percentiles closer together, and use Process Behaviour Charts to keep improving.
- forecastRead: You forecast probabilistically and steer by it. Sharpen it further by right-sizing your features and accounting for feature WIP, then scale forecasting across teams and your portfolio.
- on-page CTAs: ① Use Community to scale your forecasting (free) ② Book a workshop → `https://letpeople.work/#workshops`

CTA invariant PRESERVED: the free Community CTA appears in all four bands. Consulting/coaching/premium
CTAs are removed from the on-page breakdown (they live in the email kit instead).

---

## 4. Results email (band-specific "starter kit")

The on-page breakdown stays LEAN (just the reads + Community CTA). The rich kit lives ONLY in the
email — that is the payoff that justifies the email gate. Sent via the `capture-lead` edge function
after a valid email; copy duplicated band copy must stay in sync with section 3.

### Email structure
1. Subject: `Your Flow & Forecasting Readiness results`
2. Greeting: `Hi there,`
3. Intro: `Here's where you stand on flow and forecasting. Keep this for yourself, or forward it to your team.`
4. Result block: `Your result: {score} / 100 · {band}` + the band tagline (note: middot separator, NOT an em-dash).
5. **How you measure flow** → band `measureRead`
6. **How you forecast** → band `forecastRead`
7. **Your starter kit** → band-specific basics links (below)
8. **Workshops for where you are** → 2 named workshops (below) linking to `https://letpeople.work/#workshops`
9. **Want hands-on help?** → the 3 consulting offers (same every band) linking to `https://letpeople.work/#services`
10. Community CTA (from section 3)
11. `Just reply to this email and we'll point you to the right next step.`
12. Footer: logo, `LetPeopleWork`, `contact@letpeople.work`, `https://letpeople.work`. Reply-to `contact@letpeople.work`.

### Starter kit — basics, per band

**Band 1 · Flying blind — "Get your flow visible"**
- Docs — Connect your work tracking system: `https://docs.lighthouse.letpeople.work/concepts/worktrackingsystems/worktrackingsystems.html`
- Docs — Create a team & open the Metrics Dashboard: `https://docs.lighthouse.letpeople.work/metrics/dashboard.html`
- Blog — How Lighthouse forecasts: `https://blog.letpeople.work/p/how-lighthouse-forecasts`
- YouTube — LetPeopleWork channel: `https://www.youtube.com/@LetPeopleWork`

**Band 2 · Drifting — "Swap guesses for data"**
- Blog — The hidden cost of noise in your predictions: `https://blog.letpeople.work/p/the-hidden-cost-of-noise-in-your`
- Blog — Dear stakeholder: a range instead of a date: `https://blog.letpeople.work/p/dear-stakeholder-heres-why-im-giving`
- Docs — How Lighthouse forecasts (run one, then backtest): `https://docs.lighthouse.letpeople.work/concepts/howlighthouseforecasts.html`
- Docs — Metrics & widgets (Cycle Time, Throughput): `https://docs.lighthouse.letpeople.work/metrics/widgets.html`

**Band 3 · Flow-aware — "Operationalize forecasting"**
- Blog — A diner breakfast on Flow, Kanban & SLEs: `https://blog.letpeople.work/p/what-breakfast-at-a-diner-taught`
- Blog — How Lighthouse changed the way I work: `https://blog.letpeople.work/p/how-lighthouse-changed-the-way-i`
- Docs — Portfolios: forecast across teams: `https://docs.lighthouse.letpeople.work/portfolios/portfolios.html`

**Band 4 · Predictable — "Fine-tune & scale"**
- Blog — Low WIP, small features, high freedom: `https://blog.letpeople.work/p/low-wip-small-features-high-freedom`
- Blog — Lighthouse advanced features: `https://blog.letpeople.work/p/lighthouse-advanced-features`
- Docs — Scale & edit your portfolios: `https://docs.lighthouse.letpeople.work/portfolios/edit.html`
- Docs — AI & automation: `https://docs.lighthouse.letpeople.work/aiintegration.html`

> On the bench (unused, easy swap): Blog — How to build realistic roadmaps as a PO:
> `https://blog.letpeople.work/p/how-to-build-realistic-roadmaps-as`

### Starter kit — workshops, per band (all link to `https://letpeople.work/#workshops`)

| Band | Workshops |
|------|-----------|
| Flying blind | Flow Metrics & Little's Law · Introduction to Probabilistic Forecasting |
| Drifting | Visualization & Interpretation of Flow Metrics · Actively Manage Items in a Workflow |
| Flow-aware | SLE & Right Sizing · Workflow Definition & Visualization |
| Predictable | Epic Right Sizing & Slicing · Signal & Noise |

Short descriptions (from the offerings page):
- Flow Metrics & Little's Law — the physics of your delivery system: WIP, throughput, cycle time.
- Introduction to Probabilistic Forecasting — how Monte Carlo turns historical throughput into reliable forecasts.
- Visualization & Interpretation of Flow Metrics — reading the charts is easy; knowing what to do is the hard part.
- Actively Manage Items in a Workflow — the daily practices that keep work flowing.
- SLE & Right Sizing — meaningful Service Level Expectations and slicing work into predictable sizes.
- Workflow Definition & Visualization — map your actual process, not the idealized version.
- Epic Right Sizing & Slicing — decompose Epics into pieces that flow and can be forecasted.
- Signal & Noise — tell meaningful signals from random variation using Process Behaviour Charts.

### Starter kit — consulting, every band (all link to `https://letpeople.work/#services`)
- Flow Clarity Assessment — a data-driven diagnostic of how work really flows (includes a Lighthouse Premium license).
- Flow Health Check — a fast snapshot of your team or portfolio delivery health.
- Lighthouse Setup & Introduction — we configure Lighthouse and onboard your team.

---

## 5. Email-gate copy (on-page, after the teaser)

Replaces the current "Enter your email to see your full breakdown and your recommended next step.":

> Want the full picture? Leave your email and we'll send your Flow & Forecasting starter kit: your
> complete breakdown across both pillars, plus hand-picked articles, Lighthouse how-tos, and the
> workshops that match where you are right now. No spam, just the kit.

---

## 6. Trial decision

The one-month trial is **survey-only** (see the 5124 spec). For 5123, **drop the `wantsTrial`** flag
from the gate UI and the `capture-lead` payload. No `organization` field on 5123. The assessment gate
is purely "get your starter kit".

---

## 7. Implementation ripples (website repo)

- `src/features/assessment/content/assessmentContent.ts` — all six questions; band names + 3-field
  copy (drop `nextRung`); CTA changes. Update the zod schema (`bandSchema` drops `nextRung`) + tests.
- `src/features/assessment/core/scoring.ts` — `BandName` union (`Drifting`, `Predictable`) + boundary
  tests, and every reference to the old names.
- `supabase/_shared/resultsEmail.ts` — new band copy, drop `nextRung`, de-em-dash, add a per-band
  `starterKit` (basics links + workshops) + the shared consulting block; render the new sections.
- `supabase/_shared/bands.ts` — band-name constants/ranges if they encode the names.
- `supabase/functions/capture-lead/index.ts` — drop `wantsTrial`; keep degrade-open.
- Email-gate component — new gate copy (section 5).
- Tests: scoring boundary tests, content schema tests, email render tests, any band-CTA invariant test.

## 8. Docs / SSOT ripples (Lighthouse repo)

- `docs/product/journeys/flow-forecasting-readiness-assessment.yaml` — band names, dropped nextRung,
  CTA mapping (Community + workshop; no consulting on-page), email-as-payoff, survey-only trial.
- `docs/feature/flow-forecasting-readiness-assessment/feature-delta.md` & `distill/scoring.feature` —
  band names in the boundary tables.
- ADO #5123 sync per `/ado-sync`.

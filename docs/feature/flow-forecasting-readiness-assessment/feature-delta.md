<!-- markdownlint-disable MD024 -->
# Feature Delta — Flow & Forecasting Readiness Assessment (ADO #5123)

A FREE, ~5-minute self-assessment on the LetPeopleWork **website** (repo `/storage/repos/website`,
NOT the Lighthouse product). Six 0-3 questions → one memorable 0-100 "Forecasting Readiness Score"
→ one of four named bands → a band-specific next step. Anonymous to start; a teaser score shows on
completion; the full breakdown is gated behind a volunteered email. Modeled on ai-readiness.dev.
This feature SHIPS FIRST and establishes a shared Supabase capture + minimal admin platform that
sibling epic 5124 (User Survey) will reuse.

> nWave note: the DISCUSS docs live in the Lighthouse repo (here); the implementation lands in the
> **website** repo. Driving ports and pre-requisites reflect that cross-repo reality.

## Wave: DISCUSS / [REF] Persona

**`forecasting-prospect`** (new top-of-funnel persona; full spec at
`docs/product/personas/forecasting-prospect.yaml`) — a delivery lead / engineering manager / flow
coach who does NOT yet use Lighthouse, lands on the website, and wants an honest, fast, framework-backed
read on their delivery maturity across both what they measure and how they forecast. This is the
pre-adoption version of the in-tool `flow-coach` / `delivery-forecaster` personas.

## Wave: DISCUSS / [REF] JTBD One-Liner

**Job (`job-assess-forecasting-flow-maturity`)**: *"When I want an honest, fast read on how mature
our delivery really is, I want a credible self-assessment covering BOTH what we measure AND how we
forecast, so I can see where we stand on both axes and what concrete step comes next."*

**Business-primary objective (D1)**: this is honestly a **lead-generation instrument** — its business
job is to qualify the visitor and route them to Lighthouse Community (free), a consulting call, or a
paid tier. It must serve a genuine visitor purpose to earn the email and the click. **Funnel metrics
LEAD the outcome KPIs**, with a "served-a-purpose" value proxy guarding against a hollow funnel.

## Wave: DISCUSS / [REF] Locked Decisions (D1-D7)

| ID | Verdict |
|----|---------|
| **D1** | Honest framing: a LEAD-GEN instrument, business-primary (qualify + route to Community / consulting / paid). BUT it must serve a genuine visitor purpose. Funnel metrics LEAD the outcome KPIs. |
| **D2** | Two pillars, explicit: flow-metrics maturity (cycle time, throughput, WIP, work-item age, flow health, data activation) AND forecasting maturity (deterministic vs probabilistic/Monte Carlo, predictability, steering). Q2/Q3/Q5 lean metrics & flow-health; Q1/Q4/Q6 lean forecasting/predictability/steering. Band & CTA copy speak to BOTH. |
| **D3** | Genuine visitor job per the JTBD one-liner above; emotional arc curiosity/mild concern → a named, defensible two-axis read → "that's our gap, here's the next rung"; social = a credible framework-backed number to bring to the team/leadership. |
| **D4** | Results granularity v1 = SINGLE 0-100 score + band + band-specific CTA; band copy speaks to BOTH pillars. Per-pillar sub-scores are an explicit FAST-FOLLOW, NOT v1. No benchmark/compare line in v1 (no data source yet). |
| **D5** | UX research depth = COMPREHENSIVE (full emotional arc per step + key error paths). |
| **D6** | This feature SHIPS FIRST and establishes the shared platform on the EXISTING website Supabase: (a) response/lead capture tables, (b) email-capture handling, (c) a minimal internal results/admin dashboard. Generalize via a `source`/`kind` discriminator so epic 5124 reuses it; do NOT build 5124's needs now. Store NO personal info beyond the opt-in email. |
| **D7** | Work against `main`. `alt/redesign-2026` is style-only and touches no quiz/dashboard files — no collision. The assessment pages must ADOPT the new visual language (scroll-reveal idiom, restyled Navigation + a new assessment nav entry, consistent section styling). Recorded as a DESIGN-wave styling constraint, not a blocker. |

## Wave: DISCUSS / [REF] JTBD Analysis

### Dimensions

- **Functional**: get a defensible, framework-grounded read on delivery maturity across two axes
  (what we measure, how we forecast) without installing anything or booking a call, and learn the
  concrete next rung to climb.
- **Emotional**: relieve the nagging "I think we're output-focused but have no honest yardstick"
  feeling; replace vague self-doubt with a named, defensible position.
- **Social**: walk away with one memorable number and a named band, anchored in Vacanti / ProKanban,
  that the visitor can repeat to their team and leadership to start a credible conversation.

### Four Forces (framed honestly per D1 — opportunity is conversion-weighted, not deep-pain-weighted)

- **Push**: low-grade unease about delivery predictability; generic agile-maturity quizzes that
  ignore the estimate-vs-probabilistic distinction; missed dates with no honest explanation.
- **Pull**: one memorable, framework-backed number in ~5 minutes; a two-axis read; a concrete free
  next step. Anchored in a named body of work so it feels credible, not salesy.
- **Anxiety**: "Is this just a sales funnel?"; "Will I have to give an email before seeing anything?";
  "Will the result be a generic platitude?" Mitigations: teaser BEFORE email; named framework anchor;
  band copy that speaks specifically to both pillars.
- **Habit**: doing nothing / relying on gut-feel dates; using a spreadsheet; trusting a generic
  agile-maturity model they already half-remember.

**Honest opportunity note**: the *visitor* pain here is mild-to-moderate (curiosity + low-grade
unease), not acute. The opportunity is **conversion-weighted** — the value to LetPeopleWork is in
qualifying and routing warm leads, and the value to the visitor is a credible orientation. The design
deliberately front-loads genuine value (teaser before gate, two-pillar specificity, framework anchor)
so the lead-gen intent does not hollow out the visitor benefit.

### Proposed jobs.yaml addition

> Do NOT edit `docs/product/jobs.yaml` directly — the orchestrator integrates this to avoid a write
> race. The complete entry to add:

```yaml
- id: job-assess-forecasting-flow-maturity
  persona: forecasting-prospect
  job_story: >
    When I want an honest, fast read on how mature our delivery really is, I want a credible
    self-assessment covering BOTH what we measure (flow metrics) AND how we forecast (deterministic
    vs probabilistic), so I can see where we stand on both axes and what concrete step comes next.
  dimensions:
    functional: >
      Obtain a defensible, framework-grounded 0-100 read on delivery maturity across two axes without
      installing anything or booking a call, and learn the concrete next rung to climb.
    emotional: >
      Replace the vague "we're probably output-focused but I have no honest yardstick" unease with a
      named, defensible position.
    social: >
      Carry one memorable number and a named band, anchored in Vacanti / ProKanban, into a team /
      leadership conversation.
  forces:
    push: >
      Low-grade unease about delivery predictability; generic agile-maturity quizzes that ignore the
      estimate-vs-probabilistic distinction; missed dates with no honest explanation.
    pull: >
      One memorable, framework-backed number in ~5 minutes; a two-axis read; a concrete free next step.
    anxiety: >
      Fear this is a thin sales funnel; reluctance to give an email before seeing value; fear of a
      generic platitude result.
    habit: >
      Relying on gut-feel dates, a spreadsheet, or a half-remembered generic agile-maturity model.
  opportunity_score:
    importance: 3
    current_satisfaction: 2
    gap: 1
    rationale: >
      HONESTLY conversion-weighted. Visitor pain is mild-to-moderate (curiosity + low-grade unease),
      so importance is moderate, not high. Satisfaction with existing options (generic quizzes) is low
      because none separate flow-metrics maturity from forecasting maturity or anchor in probabilistic
      forecasting. The business value is in qualifying and routing warm leads; the gap that justifies
      building is the absence of a credible, two-axis, framework-backed self-read on the market — not
      an acute unmet visitor need. Scored to reflect that this is a top-of-funnel lead instrument that
      must still earn trust, not a deep-pain product job.
```

## Wave: DISCUSS / [REF] System Constraints (cross-cutting)

- **No PII beyond opt-in email** (D6): the only personal data stored is the email the visitor
  volunteers at the gate. Answers/score/band are non-personal.
- **Deterministic, client-evaluable scoring**: `score = round(rawSum/18*100)`; bands are exhaustive
  and non-overlapping over 0-100. Computation must not require a server round-trip.
- **Degrade open on persistence failure**: a Supabase write failure must never block the visitor's
  result or the breakdown; retry once, surface a non-blocking notice.
- **Generalized capture schema**: a `source`/`kind` discriminator so epic 5124 reuses the platform.
- **Redesign styling adoption** (D7): adopt scroll-reveal + restyled Navigation; do not edit
  `alt/redesign-2026` files; no merge collision.
- **Cross-repo**: implementation lands in `/storage/repos/website`; these docs live in the Lighthouse repo.

## Wave: DISCUSS / [REF] User Stories

All stories trace to **`job-assess-forecasting-flow-maturity`**. Entry points are website routes/UI
actions in the website repo.

### US-01 — Take the six-question assessment one at a time

`job_id: job-assess-forecasting-flow-maturity`

#### Elevator Pitch

- **Before**: Maria Santos (delivery lead, does not use Lighthouse) suspects her team is "output-focused"
  but has no honest yardstick and no time for a consultant.
- **After**: she visits `/assessment`, reads a ~5-min framed intro, and answers six 0-3 questions one
  at a time with a "Question N of 6" progress bar — seeing each answer ladder name a real practice rung.
- **Decision enabled**: she forms an honest opinion of where her team sits before any number lands,
  and decides whether to keep going (she does — the questions feel diagnostic, not salesy).

#### Problem

Maria is a delivery lead who suspects her team manages by gut-feel dates. She finds it hard to get an
honest read without booking a sales call or trusting a generic quiz that ignores probabilistic forecasting.

#### Who

- Forecasting-prospect | lands on the website, ~5 min, mobile or desktop | wants a credible self-read, not a pitch.

#### Solution

A `/assessment` route presenting an intro then the six questions (Q1 forecasting method; Q2 metrics
reviewed; Q3 WIP management; Q4 likelihood of hitting a committed date; Q5 where data lives & is it
used; Q6 deadline-at-risk response), each with its exact 0-3 ladder, one at a time, with progress and
back navigation, mobile-friendly.

#### Domain Examples

1. **Happy path** — Maria Santos on desktop answers Q1=1, Q2=2, Q3=1, Q4=2, Q5=2, Q6=1 across six screens; progress reads "6 of 6" at the end.
2. **Edge case** — Tomas (EM) on a phone (375px) taps through all six in one column; back-navigates from Q4 to fix Q3, his Q3 answer preserved.
3. **Error/boundary** — Priya refreshes the browser on Q5; her prior answers are restored from session storage and she resumes at Q5, not Q1.

#### UAT Scenarios (BDD)

```gherkin
Scenario: Visitor answers all six questions one at a time
  Given Maria opens /assessment and starts the assessment
  When she answers each of the six questions on its own screen
  Then a progress indicator advances from "1 of 6" to "6 of 6"
  And the assessment is ready to produce a result

Scenario: Visitor corrects an earlier answer without losing progress
  Given Tomas has answered questions 1 through 3
  When he navigates back to question 3 and changes his answer
  Then his answers to questions 1 and 2 are still intact

Scenario: Visitor refreshes mid-assessment and resumes
  Given Priya has answered the first four questions
  When she refreshes the browser
  Then her four answers are restored and she resumes at question 5
```

#### Acceptance Criteria

- [ ] `/assessment` presents the intro then the six questions one at a time with a "N of 6" indicator.
- [ ] Back navigation preserves previously selected answers.
- [ ] A mid-assessment refresh restores answers from session storage and resumes at the right question.
- [ ] Layout is usable at 375px width.

#### Outcome KPIs

- **Who**: forecasting-prospect visitors who start the assessment
- **Does what**: complete all six questions
- **By how much**: ≥60% start→complete rate
- **Measured by**: Supabase response rows (completions) ÷ assessment-start events
- **Baseline**: 0 (new feature)

---

### US-02 — Compute and normalize the score, assign the correct band

`job_id: job-assess-forecasting-flow-maturity`

#### Elevator Pitch

- **Before**: Maria has six answers but no single, comparable number — and no named position.
- **After**: on submitting the sixth answer, the results surface shows a single 0-100 score and one
  of four named bands (Flying blind / Drifting / Flow-aware / Predictable).
- **Decision enabled**: she now has a defensible, named position ("we're Flow-aware") to anchor a
  team conversation, instead of a vague hunch.

#### Problem

Six separate answers are not shareable or comparable. Maria needs one memorable number and a named
band that maps deterministically to her answers, with no surprises at the boundaries.

#### Who

- Forecasting-prospect | just answered six questions | wants one number + a named band.

#### Solution

A pure scoring module: `rawSum` = sum of six 0-3 answers (0-18); `score = round(rawSum/18*100)`
(0-100); `band` = the band whose range contains the score (0-25 Flying blind, 26-50 Drifting,
51-75 Flow-aware, 76-100 Predictable). Deterministic, client-evaluable.

#### Domain Examples

1. **Happy path** — Maria's answers sum to rawSum 9 → score 50 → "Drifting".
2. **Boundary** — answers summing to a score of 51 → "Flow-aware" (not Drifting); a score of 75 → "Flow-aware"; 76 → "Predictable".
3. **Extremes** — all 0s → rawSum 0 → score 0 → "Flying blind"; all 3s → rawSum 18 → score 100 → "Predictable".

#### UAT Scenarios (BDD)

```gherkin
Scenario: Score is normalized to 0-100 and mapped to the correct band
  Given Maria's six answers sum to 9
  When the result is computed
  Then her score is 50
  And her band is "Drifting"

Scenario: Boundary scores land in the documented band
  Given a set of answers that normalizes to a score of 51
  When the result is computed
  Then the band is "Flow-aware"

Scenario: The lowest and highest possible answers map to the extreme bands
  Given all six answers are 0
  Then the score is 0 and the band is "Flying blind"
  And given all six answers are 3, the score is 100 and the band is "Predictable"
```

#### Acceptance Criteria

- [ ] `rawSum` is the sum of the six 0-3 answers (range 0-18).
- [ ] `score = round(rawSum/18*100)`, range 0-100.
- [ ] Bands are exhaustive and non-overlapping; boundary scores 25/26, 50/51, 75/76 map per the locked table.
- [ ] All-0 → score 0 → "Flying blind"; all-3 → score 100 → "Predictable".

#### Outcome KPIs

- **Who**: visitors who complete the assessment
- **Does what**: receive a band that matches their actual answers (no boundary misclassification)
- **By how much**: 100% of computed bands correct per the locked thresholds (correctness, not adoption)
- **Measured by**: automated boundary tests + spot-check of captured rows (answers vs band)
- **Baseline**: 0

---

### US-03 — See the teaser score immediately on completion

`job_id: job-assess-forecasting-flow-maturity`

#### Elevator Pitch

- **Before**: Maria fears she will have to surrender an email before seeing anything of value.
- **After**: the instant she finishes, the results page shows her score (e.g. "58 / 100") and band
  ("Flow-aware") prominently, with the framework anchor — and NO email asked yet.
- **Decision enabled**: she sees the instrument delivered real value first, lowering her resistance to
  the email gate she now sees below.

#### Problem

If the score is hidden behind an email, the anxiety force ("just a sales funnel") wins and the visitor
bounces. The teaser must prove value before any ask.

#### Who

- Forecasting-prospect | just completed the assessment | skeptical of email walls.

#### Solution

A teaser surface showing `score`/100 and `band` name prominently with the credibility anchor, no
email required; the full breakdown is visibly gated below.

#### Domain Examples

1. **Happy path** — Maria sees "58 / 100 — Flow-aware" and the anchor line, with the breakdown gated below.
2. **Low band** — Tomas sees "20 / 100 — Flying blind" — still shown in full teaser, no email.
3. **Top band** — Priya sees "88 / 100 — Predictable"; teaser identical in structure.

#### UAT Scenarios (BDD)

```gherkin
Scenario: Teaser shows the number and band without requiring an email
  Given Maria has completed all six questions with a result of 58
  When the results page loads
  Then she sees "58 / 100" and the band "Flow-aware" prominently
  And no email is required to see the teaser
  And the full breakdown is shown as gated below the teaser

Scenario: The credibility anchor is visible on the teaser
  Given any completed assessment
  When the teaser renders
  Then the Vacanti / ProKanban framework attribution is visible
```

#### Acceptance Criteria

- [ ] On completion, score/100 and band name show without any email.
- [ ] The full breakdown is visibly gated below the teaser.
- [ ] The framework attribution (Vacanti / ProKanban) is present on the teaser.

#### Outcome KPIs

- **Who**: visitors who complete the assessment
- **Does what**: view the teaser result (proxy that value was delivered before any ask)
- **By how much**: ≥95% of completers reach the teaser (i.e. computation/teaser does not fail)
- **Measured by**: teaser-view events ÷ completion events
- **Baseline**: 0

---

### US-04 — Unlock the band-specific breakdown via email capture

`job_id: job-assess-forecasting-flow-maturity`

#### Elevator Pitch

- **Before**: Maria has her number but not the *why* or the *next step* — and the business has no lead.
- **After**: she enters her email in one field and the full band breakdown unlocks in place; her email
  and full response are captured to Supabase.
- **Decision enabled**: she gets the two-pillar explanation and next rung; the business gets a qualified,
  routable lead.

#### Problem

The breakdown (the genuine payoff) and the lead capture (the business payoff) must be exchanged fairly:
one field, instant unlock, no spam, and never lose the result to a write failure.

#### Who

- Forecasting-prospect | motivated by the teaser | mildly reluctant to share an email.

#### Solution

An email-gate form (zod-validated) below the teaser; on valid email, persist `email` + the full
response (`answers`, `rawSum`, `score`, `band`, source/kind discriminator) to Supabase and unlock the
breakdown in place. Invalid email → inline error, no write. Write failure → degrade open (unlock anyway,
retry once, non-blocking notice).

#### Domain Examples

1. **Happy path** — Maria enters maria.santos@acme.example; the row is written and the breakdown unlocks.
2. **Invalid email** — Tomas types "tomas@" → inline error, breakdown stays gated, no write.
3. **Write failure** — Priya submits a valid email during a Supabase outage; the breakdown unlocks anyway, a non-blocking notice shows, and the write is retried once.

#### UAT Scenarios (BDD)

```gherkin
Scenario: Valid email unlocks the breakdown and captures the lead
  Given Maria is viewing her teaser result of "Flow-aware"
  When she submits a valid email address
  Then the full band breakdown unlocks in place
  And her email and full response are captured with a readiness-assessment marker

Scenario: Invalid email is rejected without capturing anything
  Given Tomas is at the email gate
  When he submits "tomas@"
  Then an inline validation error is shown
  And the breakdown stays gated
  And nothing is written

Scenario: A capture failure never costs the visitor the breakdown
  Given Priya submits a valid email while the capture service is unavailable
  When the write fails
  Then the breakdown still unlocks
  And a non-blocking notice is shown
  And the capture is retried once
```

#### Acceptance Criteria

- [ ] A valid email unlocks the breakdown in place and persists email + full response with the source/kind marker.
- [ ] An invalid email produces an inline error, keeps the breakdown gated, and writes nothing.
- [ ] A capture failure still unlocks the breakdown (degrade open), shows a non-blocking notice, and retries once.
- [ ] No personal data beyond the volunteered email is stored.

#### Outcome KPIs

- **Who**: visitors who view the teaser
- **Does what**: trade an email to unlock the breakdown
- **By how much**: ≥35% teaser→email-capture rate
- **Measured by**: rows with an email ÷ teaser-view events
- **Baseline**: 0

---

### US-05 — Get a band-specific next step with the right CTA mix

`job_id: job-assess-forecasting-flow-maturity`

#### Elevator Pitch

- **Before**: even with a number, Maria does not know the concrete next rung for *her* band.
- **After**: the breakdown explains her band across BOTH pillars and presents the band-specific CTAs —
  Community always; consulting on low bands; paid on the top band.
- **Decision enabled**: she clicks the credible free next step (Community) she can bring to her team —
  routing her into the funnel honestly.

#### Problem

A score with no next step is a dead end for the visitor and a missed route for the business. The next
step must match the band and always include the free Community option.

#### Who

- Forecasting-prospect | has unlocked the breakdown | wants a concrete next rung.

#### Solution

Band-specific breakdown content (both-pillars explanation + named next rung) and CTA set keyed by band:
Flying blind → Community (free) + book a consulting call; Drifting → Community (shift output→flow,
first probabilistic forecast) + light coaching; Flow-aware (sweet-spot lead) → Community (operationalize
forecasting); Predictable → Lighthouse paid/portfolio + Community (validate) / advanced consulting.
The free Community CTA appears in EVERY band.

#### Domain Examples

1. **Sweet-spot** — Maria (Flow-aware) sees a both-pillars explanation and a primary "Use Community to operationalize forecasting" CTA.
2. **Low band** — Tomas (Flying blind) sees a Community CTA AND a "Book a consulting call" CTA.
3. **Top band** — Priya (Predictable) sees a paid/portfolio primary CTA plus a Community/advanced-consulting secondary; Community still present.

#### UAT Scenarios (BDD)

```gherkin
Scenario: Breakdown explains the band across both pillars
  Given Maria has unlocked her "Flow-aware" breakdown
  When she reads the explanation
  Then it speaks to both what her team measures and how it forecasts

Scenario: The CTA mix matches the band
  Given Tomas is in the "Flying blind" band
  Then he sees a free Lighthouse Community CTA and a consulting-call CTA
  And given Priya is in the "Predictable" band, she sees a paid/portfolio CTA and a Community CTA

Scenario: The free Community CTA appears in every band
  Given any of the four bands
  When the breakdown renders
  Then a free Lighthouse Community CTA is present
```

#### Acceptance Criteria

- [ ] Breakdown content exists for all four bands and speaks to BOTH pillars.
- [ ] The CTA set matches the band per the locked mapping.
- [ ] The free Community CTA is present in all four bands.

#### Outcome KPIs

- **Who**: visitors who unlock the breakdown
- **Does what**: click a band-specific CTA
- **By how much**: ≥25% breakdown→CTA click-through (≥30% for the Flow-aware sweet-spot band)
- **Measured by**: CTA click events ÷ breakdown-unlock events, segmented by band
- **Baseline**: 0

---

### US-06 — Capture responses + leads to a generalized Supabase platform with a minimal admin dashboard

`job_id: job-assess-forecasting-flow-maturity`

> Platform-establishing story. It is NOT infrastructure-only: it produces a user-visible, internal
> **results/admin dashboard** that the LetPeopleWork team uses to see and act on leads.

#### Elevator Pitch

- **Before**: the LetPeopleWork team has no way to see who took the assessment, how they scored, or
  which leads to follow up — and there is no reusable capture platform for the upcoming survey (5124).
- **After**: every completed assessment is captured to a generalized Supabase table (source/kind
  discriminator), and a protected internal dashboard page shows total responses, band distribution,
  and the lead list (email, score, band, date).
- **Decision enabled**: the team decides which leads to follow up and in what order; and epic 5124 can
  reuse the same tables and dashboard without a redesign.

#### Problem

Without persistence and a view over it, completions are invisible and leads are lost; without a
generalized schema, the sibling survey epic would require a parallel build.

#### Who

- LetPeopleWork team (internal) | reviews captured leads | wants to act on them and reuse the platform for 5124.

#### Solution

A generalized Supabase responses table (answers, rawSum, score, band, optional email, `source`/`kind`
discriminator, created_at) with anon-INSERT RLS; and a protected internal dashboard showing counts,
band distribution, and the lead table, parameterized by the discriminator.

#### Domain Examples

1. **Happy path** — Maria's completed assessment appears as one row; the dashboard count increments and her email/score/band show in the lead table.
2. **No-email completion** — Tomas finishes but never gives an email; his row is captured (answers/score/band) and counts toward completions but not toward captured leads.
3. **Access control** — an unauthenticated visitor hitting the dashboard route is denied; no lead data is exposed.

#### UAT Scenarios (BDD)

```gherkin
Scenario: A completed assessment is captured as a single response row
  Given Maria completes the assessment
  When her result is computed
  Then one response row is captured with her answers, score, band and a readiness-assessment marker

Scenario: The internal dashboard summarizes captured results
  Given several assessments have been completed and some emails captured
  When an authenticated team member opens the results dashboard
  Then they see total responses, the email-capture count, the band distribution, and a lead table

Scenario: The dashboard is not publicly accessible
  Given an unauthenticated visitor
  When they navigate to the results dashboard route
  Then access is denied and no lead data is shown
```

#### Acceptance Criteria

- [ ] Each completion writes exactly one row with answers, rawSum, score, band, source/kind marker, created_at.
- [ ] The anon client can INSERT but cannot SELECT other rows (RLS verified).
- [ ] The dashboard requires authentication and shows totals, capture count, band distribution, and the lead table.
- [ ] The schema and dashboard are parameterized by the source/kind discriminator (5124-ready, none built).
- [ ] No PII beyond the volunteered email is stored.

#### Outcome KPIs

- **Who**: the LetPeopleWork team
- **Does what**: review and act on captured leads from a single dashboard
- **By how much**: 100% of completions captured; band distribution visible within the dashboard
- **Measured by**: dashboard row counts vs assessment-completion events (reconciliation)
- **Baseline**: 0

## Wave: DISCUSS / [REF] Definition of Done (9-item)

1. All UAT scenarios across US-01..US-06 pass (green) in the website test stack.
2. Scoring is deterministic with boundary tests covering 25/26, 50/51, 75/76, 0, 100.
3. The teaser shows before any email; the email gate unlocks the breakdown; capture degrades open.
4. Every band has authored both-pillars breakdown content and the correct CTA mix; Community present in all four.
5. Responses persist to the generalized Supabase table with the source/kind discriminator; RLS verified.
6. The internal dashboard is auth-protected and shows totals/distribution/lead table.
7. No PII beyond the opt-in email is stored anywhere.
8. Assessment pages adopt the redesign visual language + a Navigation entry; no `alt/redesign-2026` collision.
9. Feature merged to website `main`; website `pnpm build` + Biome clean; demoable end-to-end on mobile and desktop.

## Wave: DISCUSS / [REF] Out of Scope

- Per-pillar (metrics vs forecasting) sub-scores — explicit FAST-FOLLOW, not v1 (D4).
- Benchmark / "how you compare" line — no data source yet (D4).
- Epic 5124's User Survey content and survey-specific dashboard views (only the discriminator is reserved).
- Auto-issued licenses / CRM automation / email nurture sequences — leads are acted on manually in v1.
- Editing the `alt/redesign-2026` branch files (style-only, owned by a colleague).

## Wave: DISCUSS / [REF] Walking Skeleton Strategy

Brownfield website. The thinnest end-to-end slice that delivers real value is the **full 6-question →
score → band → CTA flow WITHOUT the email gate or dashboard** (Slice 01). One question → teaser is NOT
enough value (no band, no next step, nothing to validate). The skeleton proves the questions+bands feel
honest (the riskiest assumption) before any platform investment. Persistence (Slice 02), the email gate
(Slice 03), the dashboard (Slice 04), and redesign adoption (Slice 05) then layer on.

## Wave: DISCUSS / [REF] Driving Ports (inbound surfaces — website repo)

- **Route `/assessment`** (react-router-dom v7, registered in `src/App.tsx` above the catch-all `*`):
  intro → six questions → teaser → email gate → breakdown. Primary visitor-invocable entry point.
- **Navigation entry** (`src/components/Navigation.tsx` `navItems`): a "Forecasting Readiness" item routing to `/assessment`.
- **Email-gate form**: react-hook-form + zod, submitting into the capture path.
- **Supabase capture** (`@supabase/supabase-js` via `src/integrations/supabase/client.ts`): anon-INSERT
  into the generalized responses table guarded by RLS (an edge function is optional, not required — a
  scoped RLS insert from the anon client matches the existing wiring; revisit in DESIGN if validation
  needs to be server-side).
- **Protected dashboard route** (e.g. `/assessment/results` or `/admin/assessment`): Supabase-auth-gated
  read surface over the responses table.

## Wave: DISCUSS / [REF] Pre-requisites

- Work in `/storage/repos/website` on `main` (D7).
- Existing Supabase project (`tkkghzcpwefwrgacgvdv`) reachable; anon client already wired.
- Decide (DESIGN) whether capture is a direct RLS-guarded anon INSERT or a small edge function (the
  existing `create-payment` / `stripe-webhook` functions show the pattern if server-side validation is wanted).
- Final question/answer-ladder and band copy text confirmed (ladders reproduced faithfully from the epic).
- Coordinate Slice 05 timing with the `alt/redesign-2026` merge (non-blocking; no file collision).

## Wave: DISCUSS / [REF] Comprehensive Journey

Full step-by-step journey, emotional arc, shared-artifact registry, and error paths are authored in
`docs/product/journeys/flow-forecasting-readiness-assessment.yaml` (house-style journey schema with
embedded per-step Gherkin, emotional states, shared artifacts, and failure modes).

### Mental model (summary)

The visitor thinks of "maturity" as two axes — *what we measure* and *how we forecast* — and wants one
memorable, framework-anchored number plus a named position they can repeat to leadership.

### Happy path with outputs per step

1. **Discover** (`/assessment`) → framed intro + credibility anchor; output: a "Start" action, no email.
2. **Answer** → six questions one at a time; output: `${answers}` (6 ordinals 0-3) + progress.
3. **Compute** → output: `${rawSum}` (0-18), `${score}` (0-100), `${band}` (one of four).
4. **Teaser** → output: `${score}`/100 + `${band}` prominently, breakdown gated, no email.
5. **Capture** → output: `${email}` + full response persisted; breakdown unlocks.
6. **Breakdown + act** → output: `${breakdownContent}` (both-pillars + next rung) + band-specific CTAs.

### Emotional arc (upward trajectory)

Curious/mildly skeptical → engaged/self-aware → anticipation → satisfied (number) → motivated tension
(want the why) → reassured (one-field unlock) → oriented/motivated (named gap + free next step).

### Shared-artifacts registry

| Variable | Source of truth | Consumers | Integration risk |
|----------|-----------------|-----------|------------------|
| `${credibilityAnchor}` | band/question content module (website repo) | intro, teaser, breakdown, footer | LOW — static text, single module |
| `${answers}` | client session state (sessionStorage), authoritative until submit | progress, scoring, Supabase row | MEDIUM — must be exactly 6×(0-3) before scoring |
| `${rawSum}` | pure fn of `${answers}` (scoring module) | scoring, Supabase row | LOW — derived |
| `${score}` | `round(rawSum/18*100)` (scoring module) | teaser, breakdown, row, dashboard | HIGH — the headline number; one source only |
| `${band}` | band-mapping table (scoring module) | teaser, breakdown, CTA selection, row, dashboard | HIGH — drives content + CTA mix; exhaustive/non-overlapping |
| `${email}` | email-gate input (zod-validated) | Supabase lead row, dashboard | HIGH — the only PII; validation at trust boundary |
| `${breakdownContent}` | band content module keyed by `${band}` | breakdown page | MEDIUM — every band must have content |

### Error paths

- Refresh mid-quiz → restore `${answers}` from sessionStorage; empty → restart at Q1 with notice.
- Partial completion / deep link without 6 answers → block scoring, redirect to first unanswered question.
- Invalid email → inline error, no write, breakdown stays gated.
- Supabase write failure → degrade open: unlock breakdown, retry once, non-blocking notice.
- Boundary scores → deterministic mapping, asserted by boundary ACs.
- Unauthenticated dashboard access → denied; no PII beyond opt-in email exists to leak.

## Wave: DISCUSS / [REF] Story Map

### Backbone (activities, left→right)

| Discover | Answer | Get number | Unlock why | Act |
|----------|--------|------------|-----------|-----|
| Land on `/assessment`, see framed intro + anchor | Answer 6 questions one at a time | Compute score + band | Teaser → email gate → breakdown | Click band-specific CTA |
| Find it via restyled Navigation | Back-nav / resume on refresh | (deterministic, boundary-safe) | Capture lead to Supabase | Team reviews leads on dashboard |

### Walking skeleton

Slice 01: `/assessment` → 6 questions → score → band → CTA shown directly (no gate, no persistence).
Touches every backbone activity at its thinnest.

### Release slices (elephant carpaccio)

Each slice is end-to-end, ≤6h crafter dispatch, with a learning hypothesis and a user-visible value story.
Briefs at `docs/feature/flow-forecasting-readiness-assessment/slices/slice-0N-*.md`.

| Slice | Goal (1 line) | Learning hypothesis (disproved if…) | Dogfood moment | IN / OUT |
|-------|---------------|-------------------------------------|----------------|----------|
| 01 — WS quiz→band→CTA | 6Q → score → band → CTA, client-only | Questions/bands feel honest for known teams; disproved if dogfood scores feel wrong | Team takes it for their own teams | IN: quiz+scoring+CTA. OUT: gate, persistence, dashboard, restyle |
| 02 — Supabase capture | Persist each completion to a generalized table | Discriminator absorbs 5124 later w/o breaking v1; disproved if survey shape forces restructuring | Real completions land as rows | IN: table+RLS+write+degrade-open. OUT: email, dashboard |
| 03 — email gate | Teaser + email unlocks breakdown | Number-first earns the email; disproved if capture rate ≈ 0 | Watch first capture rate | IN: teaser/gate/breakdown/email persist. OUT: dashboard, restyle |
| 04 — admin dashboard | Internal protected results/lead view | One generalized dashboard suffices + 5124-ready; disproved if a lead can't be acted on | Team reviews real leads | IN: auth read view over discriminator. OUT: per-pillar, export |
| 05 — redesign adoption | Adopt scroll-reveal + Nav entry | No collision with redesign branch; disproved if it must touch redesign files | Pages look native to the redesign | IN: Nav entry + styling. OUT: behavior changes |

### Slice taste-tests

- **End-to-end value?** Every slice delivers something a visitor or the team can observe (01 a full result; 02 captured data confirmable; 03 the gated breakdown; 04 a usable dashboard; 05 native styling). PASS.
- **≤1 day?** Each ≤6h. PASS.
- **At least one user-visible story per slice?** Yes — no infra-only slice (02's value = confirmable captured data feeding 04; it pairs with US-06's dashboard value). PASS.
- **Production-not-synthetic data?** 01 dogfooded with the team's own teams; 02-04 use real completions. PASS.
- **Independent demo?** Each slice is demoable on its own. PASS.

### Suggested execution order (learning leverage + dependency + dogfood cadence)

1. **Slice 01** — highest learning leverage (validates the riskiest assumption: do the questions/bands feel honest?) and unblocks everything. Dogfood immediately.
2. **Slice 03** — next-riskiest business assumption (will number-first earn the email?). Depends on 01.
   *(Sequencing note: 03 needs a place to persist the email; ship Slice 02 immediately before 03 — they pair. 02 alone has no visitor-facing change, so it never ships as a standalone release; it lands attached to 03's value.)*
3. **Slice 02** — paired with/just before 03 (persistence the gate writes into).
4. **Slice 04** — once real leads exist, give the team the dashboard to act on them and confirm the platform generalizes.
5. **Slice 05** — styling adoption last; non-blocking, coordinate with the redesign merge.

> Practical order: 01 → (02+03 together) → 04 → 05.

## Wave: DISCUSS / [REF] Outcome KPIs (funnel-leading per D1)

### Objective

Within 8 weeks of launch, the assessment qualifies and routes a meaningful stream of warm leads into
Lighthouse Community / consulting / paid, while genuinely orienting the visitor on their two-axis maturity.

### Outcome KPIs

| # | Who | Does what | By how much (target) | Baseline | Measured by | Type |
|---|-----|-----------|----------------------|----------|-------------|------|
| 1 | Assessment starters | Complete all six questions | ≥60% start→complete | 0 | completions ÷ start events | Leading (Activation) |
| 2 | Completers | Finish quickly | median completion ≤5 min | 0 | timestamp delta first-answer→completion | Leading (Secondary) |
| 3 | Teaser-viewers | Trade an email to unlock | ≥35% teaser→email-capture | 0 | rows-with-email ÷ teaser-views | Leading (Acquisition) — **North Star** |
| 4 | Completers | Distribute across bands | no single band >60% (sanity, not optimize) | 0 | band counts ÷ completions | Guardrail |
| 5 | Breakdown-unlockers | Click a band-specific CTA | ≥25% overall, ≥30% Flow-aware | 0 | CTA clicks ÷ unlocks, by band | Leading (Acquisition) |
| 6 | Captured leads | Sign up for Lighthouse Community | ≥10% lead→Community-signup | 0 | Community signups attributed to assessment ÷ leads | Leading→Lagging bridge |
| 7 | Captured leads (low/top bands) | Book a consulting call | ≥3% lead→consulting-call | 0 | consulting bookings attributed ÷ leads | Lagging (Revenue) |
| 8 | Breakdown-unlockers | Engage with the breakdown (served-a-purpose value proxy) | median breakdown dwell ≥30s OR ≥40% scroll-to-CTA | 0 | dwell time / scroll-depth on breakdown | Guardrail (D1 "serve a purpose") |

### Metric hierarchy

- **North Star**: teaser→email-capture rate (KPI 3) — the pivot where genuine value converts to a lead.
- **Leading indicators**: start→complete (1), CTA click-through (5).
- **Guardrails**: band-distribution sanity (4), breakdown engagement value-proxy (8) — KPI 8 directly
  honors D1's "must serve a genuine visitor purpose" so the funnel is not hollow.

### Measurement plan

| KPI | Data source | Collection | Frequency | Owner |
|-----|-------------|-----------|-----------|-------|
| 1,2,4 | website analytics + Supabase rows | event + row reconciliation | weekly | website owner |
| 3,5,8 | website analytics (events) + Supabase | event ratios | weekly | website owner |
| 6,7 | Community signup + consulting CRM, attributed | manual attribution v1 | monthly | LetPeopleWork team |

> Self-hosted Lighthouse instances do not phone home (MEMORY: telemetry gap, Epic 5015), so KPIs 6-7
> rely on website-side attribution + manual CRM reconciliation in v1, not in-product telemetry.

## Wave: DISCUSS / [REF] DoR Validation

| DoR Item | Status | Evidence |
|----------|--------|----------|
| 1. Problem statement clear, domain language | PASS | Per-story Problem sections in prospect's language ("flying blind", "output-focused", probabilistic forecasting). |
| 2. User/persona with specific characteristics | PASS | `forecasting-prospect` persona authored; top-of-funnel delivery lead/EM/coach, not yet a Lighthouse user. |
| 3. 3+ domain examples with real data | PASS | Each US has 3 examples with real names (Maria Santos, Tomas, Priya) and real answer/score data. |
| 4. UAT in Given/When/Then (3-7 scenarios) | PASS | Each US has 2-3 scenarios; 16 total across 6 stories; journey YAML embeds per-step Gherkin. |
| 5. AC derived from UAT | PASS | Each US's AC list maps to its scenarios. |
| 6. Right-sized (1-3 days, 3-7 scenarios) | PASS | 6 stories, each ≤6h; mapped 1:1-ish to 5 slices each ≤6h. |
| 7. Technical notes: constraints/dependencies + RBAC/Clients/Website checklist | PASS | System Constraints section + Pre-requisites + cross-cutting checklist below all answered with evidence. |
| 8. Dependencies resolved or tracked | PASS | Slice dependencies recorded; 02↔03 pairing noted; redesign-branch coordination noted (non-blocking). |
| 9. Outcome KPIs defined with measurable targets | PASS | 8 KPIs with numeric targets + measurement methods; North Star identified; value-proxy guardrail per D1. |

### DoR Status: PASSED

## Wave: DISCUSS / [REF] Cross-Cutting Impact Checklist (CLAUDE.md — all three explicit)

- **RBAC** — **N/A, because** this feature is website-only and does NOT touch the Lighthouse product,
  so it has zero interaction with `IRbacAdministrationService` or the `useRbac()` hook (those gate the
  Lighthouse backend/frontend, a different codebase). The only authorization concern is the *internal
  results/admin dashboard*, which is a **website/Supabase** concern: it is protected by Supabase auth /
  a gated route in the website repo (verified as an AC in US-06 and Slice 04), not by Lighthouse RBAC.
- **Lighthouse-Clients (CLI + MCP)** — **N/A, because** there are NO Lighthouse server API contract
  changes: all new endpoints/tables live on the website's Supabase, not the Lighthouse server. The CLI
  and MCP clients wrap the Lighthouse server API only, so there is nothing for them to follow and no
  version gate to add.
- **Website** — **IN SCOPE** — this IS a website feature (new `/assessment` route, Navigation entry,
  Supabase capture, internal dashboard, all in `/storage/repos/website`). DESIGN-wave styling
  constraint (D7): the assessment pages must adopt the `alt/redesign-2026` visual language (scroll-reveal
  via `useScrollReveal`, restyled Navigation + new nav entry, consistent section styling). No file
  collision with that branch (it touches no quiz/dashboard files); coordinate Slice 05 timing with its
  merge. Recorded as a constraint, not a blocker.

## Wave: DISCUSS / [REF] Shared Platform (establishes; reused by 5124)

This feature establishes the reusable capture + admin platform on the existing website Supabase (D6).

### Supabase schema sketch (generalized — two tables for structural anonymity)

PII (email) is kept in a SEPARATE table from anonymous answers, with NO foreign key between them, so
5124's "anonymous survey" guarantee is structural rather than a promise. 5123 still gets its lead (the
email-band pair) because the lead row carries its own copy of score/band — it just isn't joinable back
to a specific anonymous answer row.

```yaml
table: responses           # anonymous; NEVER holds PII
columns:
  id: uuid (pk, default gen_random_uuid)
  source: text not null    # discriminator: "readiness-assessment" | (5124) "user-survey"
  kind: text               # optional sub-type within a source
  answers: jsonb not null  # six 0-3 ordinals (assessment); survey single-selects reuse the shape
  raw_sum: int             # assessment 0-18; null for non-scored sources
  score: int               # assessment 0-100; null for non-scored sources
  band: text               # assessment band name; null for non-scored sources
  created_at: timestamptz not null default now()

table: leads               # the ONLY table with PII (email)
columns:
  id: uuid (pk, default gen_random_uuid)
  source: text not null    # "readiness-assessment" | (5124) "user-survey-trial"
  email: text not null     # volunteered; validated server-side
  score: int               # assessment lead carries its own score/band copy (NOT an FK to responses)
  band: text
  wants_trial: bool not null default false   # 5124 trial opt-in signal
  created_at: timestamptz not null default now()
notes:
  - No names, IPs, or tracking identifiers persisted in either table.
  - source/kind lets 5124 add survey rows without restructuring assessment rows.
  - responses and leads are deliberately NOT joined → survey answers stay anonymous.
```

### Supabase security model (DESIGN seed — guidance for first-time setup)

The website is a **public client-side app**: the Supabase `anon` key ships in the browser bundle and
is **not a secret**. Security therefore rests entirely on **Row Level Security (RLS) policies**, never
on the key being hidden. The rules:

1. **Enable RLS on every table.** RLS is OFF by default, and with RLS off the `anon` key has full
   read/write. With RLS on and no policy, access is denied by default (fail-closed) — that is what we
   want.
2. **`anon` gets INSERT-only, never SELECT/UPDATE/DELETE.** A policy like `INSERT … WITH CHECK (...)`
   lets the public form write one row; the absence of a SELECT policy means a visitor can never read
   back other people's responses or harvest emails. This is the single most important rule.
3. **Write the PII (email) through an Edge Function, not a direct anon INSERT.** The `leads` table
   should have NO anon policy at all; the browser POSTs the email to a Supabase Edge Function (Deno —
   same pattern as the existing `create-payment` / `stripe-webhook` functions) which validates the
   payload (zod) and inserts using the privileged `service_role`. This keeps the email table fully
   sealed from the public key and centralizes validation/rate-limiting. The anonymous `responses`
   table can stay on the simpler direct anon-INSERT path (rule 2).
4. **The `service_role` key NEVER touches the browser.** It bypasses RLS entirely and lives only in
   Edge Function / server env vars. Putting it in client code would void every protection above.
5. **Dashboard reads use a privileged path, not `anon`.** Either (a) Supabase Auth — an admin logs in
   and a `SELECT` policy gates rows to `authenticated` users, or (b) an Edge Function / server route
   using `service_role`. Never grant `anon` SELECT just to power the dashboard.
6. **Spam / bot mitigation (optional v1, plan for it):** a public INSERT endpoint invites bot writes.
   Acceptable to launch without it at community volume, but the Edge-Function write path (rule 3) is
   where a Cloudflare Turnstile / hCaptcha token check or per-IP rate limit slots in if abuse appears.

### Capture mechanism

Anonymous `responses` → direct anon-INSERT guarded by RLS (matches the already-wired `client.ts` +
`config.toml`). Email/lead `leads` → Edge Function write with `service_role` (rule 3). Both capture
paths degrade open: a write failure must never block the visitor's result.

### Minimal dashboard

A privileged (Supabase-auth) internal route showing total responses, email-capture count, band
distribution, and a lead table (email, score, band, created_at), filtered by `source`/`kind` so 5124's
survey responses + trial requests slot into the same view later without a redesign.

## Wave: DISCUSS / [REF] wave-decisions Summary

- **Scope Assessment: PASS** — 6 right-sized stories, 1 bounded context (website + its Supabase),
  estimated ~5 days across 5 slices each ≤6h. Not oversized.
- **DIVERGE artifacts**: none present for this feature (`docs/feature/.../diverge/` absent). Discovery
  decisions D1-D7 were supplied pre-locked by the user and embedded above; JTBD grounded in D3.
  **Risk**: no formal DIVERGE recommendation/job-analysis — mitigated by the locked decisions, but the
  opportunity score is self-asserted (honestly conversion-weighted) rather than ODI-validated.
- **Cross-repo**: docs in Lighthouse repo; code in website repo — reflected in driving ports/prereqs.
- **Platform-first**: this feature establishes shared Supabase capture + dashboard for sibling 5124.

### Risks / open questions for the orchestrator/user

1. **Question/band copy is reproduced from the epic** — confirm the exact answer-ladder wording and
   band explanation copy before DELIVER (content is load-bearing for credibility).
2. **Capture transport** — RECOMMENDED model now recorded (see Supabase security model): anonymous
   `responses` via RLS anon-INSERT (no SELECT); email/`leads` via an Edge Function using
   `service_role`. Confirm in DESIGN / security review; add Turnstile only if spam appears.
3. **Dashboard auth mechanism** — privileged read only (Supabase Auth `authenticated` SELECT policy,
   or Edge Function with `service_role`); never `anon` SELECT. Confirm who on the team authenticates.
4. **KPI 6/7 attribution** — Community signup and consulting bookings are attributed manually in v1
   (no in-product telemetry per the Epic 5015 gap); confirm a UTM/source-tagging convention for the CTAs.
5. **opportunity_score is self-asserted** (no DIVERGE/ODI) — accept the conversion-weighted rationale
   or commission a quick validation if a harder number is wanted.

---

# Wave: DESIGN (Application / component scope — Morgan, solution-architect)

> **Scope note (avoid the category error)**: every architecture decision below is for the **WEBSITE app**
> (`/storage/repos/website`). The Lighthouse product SSOT (`docs/product/architecture/brief.md`) is NOT
> touched. Feature-scoped ADRs are `adr-031`..`adr-037` (each headed "Scope: WEBSITE repo"). C4 diagrams
> are in the sibling `c4-design.md`. Interaction mode: **propose**. Documentation density: **lean** (Tier-1
> [REF] only).

## Wave: DESIGN / [REF] Decisions (DDD-N)

| ID | Decision | ADR |
|----|----------|-----|
| **DDD-1** | Adopt Vitest 3 + React Testing Library + jsdom in the website repo (no test framework exists today); pure scoring → unit boundary tests, surfaces → component tests; `test` + `test:watch` scripts. | adr-031 |
| **DDD-2** | Capture transport is asymmetric: anonymous `responses` via direct anon-INSERT (RLS INSERT-only, no SELECT); PII `leads` via a `capture-lead` Supabase Edge Function using `service_role`. Both degrade open. | adr-032 |
| **DDD-3** | Internal dashboard auth = Supabase Auth (email/password) + `authenticated` SELECT RLS; team accounts provisioned manually; RLS is the real boundary, the route guard is UX. | adr-033 |
| **DDD-4** | Two-table structural-anonymity schema (`responses` anon, `leads` PII), NO FK between them, `source`/`kind` discriminator generalizing both for epic 5124; RLS fail-closed on both. | adr-034 |
| **DDD-5** | Functional-core / imperative-shell = hexagonal here: pure `scoring` + pure `quizMachine` reducer as the domain core; sessionStorage / Supabase / Edge / analytics as driven ports; route/Nav/form/dashboard as driving ports. No OOP forced into the functional-React repo. | adr-035 |
| **DDD-6** | Single typed, zod-validated `assessmentContent` module is the one source for questions/ladders/band copy/CTAs/anchor; load-time invariants enforce 4 contiguous bands + Community CTA in every band; band ranges shared with the scoring table. | adr-036 |
| **DDD-7** | Funnel analytics behind a swappable `AnalyticsSink` driven port; Supabase rows are the durable funnel backbone; outbound CTAs carry `utm_source=assessment&...&band=<slug>` for manual KPI 6/7 attribution. | adr-037 |
| **DDD-8** | Default architecture confirmed: a feature module inside the existing modular website SPA (no new service, no microservice, no backend) — simplest solution that meets every quality attribute; Supabase Edge Functions are the only server surface and only for the PII write. | (rationale here) |

## Wave: DESIGN / [REF] Reuse Analysis (HARD GATE — searched the website repo before designing)

| Existing component | File | Overlap | Decision | Justification |
|--------------------|------|---------|----------|---------------|
| Navigation | `src/components/Navigation.tsx` | Needs the "Forecasting Readiness" nav entry | **EXTEND** | Add one item to `navItems` (+ the mobile list). Rebuilding nav would fork the menu. |
| App routing | `src/App.tsx` | Needs `/assessment` + `/admin/assessment` routes | **EXTEND** | Add two `<Route>`s above the catch-all (the file literally instructs this). |
| Supabase client | `src/integrations/supabase/client.ts` | Anon client + auth already wired (`persistSession`, `autoRefreshToken`) | **EXTEND/REUSE** | Reuse the singleton for anon-INSERT, Edge invoke, and dashboard auth — no new client. |
| Supabase types | `src/integrations/supabase/types.ts` | Currently empty (`Tables: never`) | **EXTEND** | Regenerate/author `responses`+`leads` Row/Insert types after the migration. |
| Edge Function pattern | `supabase/functions/create-payment/index.ts` | Deno `serve` + CORS + `Deno.env` + validate + privileged op | **REUSE pattern → CREATE NEW `capture-lead`** | The pattern is the template; a *new* function is required (different table/validation), no existing function fits. |
| Edge invoke pattern | `src/pages/Lighthouse.tsx:489` (`supabase.functions.invoke("create-payment")`) | Client→function call shape | **REUSE pattern** | The lead adapter invokes identically. |
| shadcn `progress` | `src/components/ui/progress.tsx` | "N of 6" progress bar | **REUSE** | Radix progress already present — do not rebuild. |
| shadcn `radio-group` | `src/components/ui/radio-group.tsx` | 0-3 answer ladder selection | **REUSE** | Present. |
| shadcn `form` + RHF + zod | `src/components/ui/form.tsx`, `react-hook-form`, `zod` | Email-gate form + validation | **REUSE** | All present (`@hookform/resolvers` too); matches Lighthouse.tsx form usage. |
| shadcn `input`/`button`/`card`/`badge`/`alert` | `src/components/ui/*` | Quiz/teaser/breakdown/dashboard primitives | **REUSE** | All present — do not rebuild any primitive. |
| shadcn `table`/`tabs` | `src/components/ui/table.tsx`, `tabs.tsx` | Dashboard lead table + source/kind tabs | **REUSE** | Present. |
| `use-toast` + sonner | `src/hooks/use-toast.ts`, `src/components/ui/sonner.tsx` | Degrade-open non-blocking notice | **REUSE** | Already mounted in `App.tsx` (`<Toaster/>`,`<Sonner/>`). |
| SEO component | `src/components/SEO.tsx` (used in `Index.tsx`) | Assessment page `<title>`/meta | **REUSE** | Compose like Index/Lighthouse pages. |
| Page layout idiom | `src/pages/Index.tsx`, `Lighthouse.tsx` | Nav + sections + footer composition | **REUSE pattern** | New pages follow the same shell composition. |
| `useScrollReveal` hook | NOT on `main` (lives on `alt/redesign-2026`) | Slice 05 scroll-reveal idiom | **CONSUME WHEN AVAILABLE** | Hook is absent on `main`; Slice 05 depends on the redesign merge landing it. See Open Questions / Changed Assumptions. |

**CREATE NEW (justified — no existing alternative):** the `scoring` module, the `quizMachine` reducer, the `assessmentContent` module, the four driven-adapter implementations, the `capture-lead` Edge Function, the `/assessment` flow surfaces (intro/question/teaser/gate/breakdown), and the `/admin/assessment` dashboard. None exist; each is feature-specific.

## Wave: DESIGN / [REF] Component Decomposition

| Component | Path (website repo) | Change |
|-----------|---------------------|--------|
| `assessmentContent` (questions/ladders/bands/CTAs/anchor + zod) | `src/features/assessment/content/` | NEW |
| `scoring` (`score()`, band table) | `src/features/assessment/core/scoring.ts` | NEW |
| `quizMachine` (pure reducer + guards) | `src/features/assessment/core/quizMachine.ts` | NEW |
| Driven port interfaces (`ResponseRepository`, `LeadCapture`, `QuizPersistence`, `AnalyticsSink`) | `src/features/assessment/ports/` | NEW |
| `SupabaseResponseRepository` | `src/features/assessment/adapters/` | NEW |
| `EdgeFunctionLeadCapture` | `src/features/assessment/adapters/` | NEW |
| `SessionStoragePersistence` | `src/features/assessment/adapters/` | NEW |
| `AnalyticsSink` adapter (vendor or no-op) | `src/features/assessment/adapters/` | NEW |
| Assessment flow surfaces (intro, question, teaser, gate, breakdown) | `src/features/assessment/components/` | NEW |
| `AssessmentPage` (route host + composition root) | `src/pages/Assessment.tsx` | NEW |
| `AdminAssessmentPage` (dashboard + `useAdminSession`) | `src/pages/AdminAssessment.tsx` | NEW |
| `capture-lead` Edge Function | `supabase/functions/capture-lead/index.ts` | NEW |
| Supabase migration (two tables + RLS) | `supabase/migrations/*.sql` | NEW |
| Routes | `src/App.tsx` | MODIFIED |
| Nav entry | `src/components/Navigation.tsx` | MODIFIED |
| Supabase types | `src/integrations/supabase/types.ts` | MODIFIED |
| Test config | `vitest.config.ts`, `vitest.setup.ts`, `package.json` | NEW/MODIFIED |
| Scroll-reveal adoption (Slice 05) | assessment surfaces consume `useScrollReveal` | MODIFIED (Slice 05) |

## Wave: DESIGN / [REF] Ports

**Driving ports (inbound):** Route `/assessment` (`App.tsx`); Navigation entry (`Navigation.tsx`); email-gate form (RHF + zod); dashboard route `/admin/assessment` (`App.tsx`, Supabase-auth-gated).

**Driven ports (outbound) + adapters:** `ResponseRepository.save` → `SupabaseResponseRepository` (anon-INSERT into `responses`); `LeadCapture.capture` → `EdgeFunctionLeadCapture` (`functions.invoke("capture-lead")`); `QuizPersistence` → `SessionStoragePersistence`; `AnalyticsSink.track` → analytics adapter (or no-op). Adapters injected at the `/assessment` composition root; the pure core never imports `@supabase/*` or touches `window`.

## Wave: DESIGN / [REF] Technology Choices

| Technology | Version (repo-pinned) | License | Role | Notes |
|------------|----------------------|---------|------|-------|
| React | ^19.2.4 | MIT | SPA framework | existing |
| Vite | ^7.3.1 | MIT | build/dev | existing |
| react-router-dom | ^7.13.0 | MIT | routing | existing; add 2 routes |
| @supabase/supabase-js | ^2.95.3 | MIT | anon-INSERT, Edge invoke, auth | existing |
| react-hook-form | ^7.71.1 | MIT | email-gate form | existing |
| zod | ^4.3.6 | MIT | trust-boundary validation (email, content, Edge payload) | existing |
| @hookform/resolvers | ^5.2.2 | MIT | RHF↔zod bridge | existing |
| shadcn/ui (Radix) | per `package.json` | MIT | progress/radio-group/form/input/button/card/table/tabs/badge/alert | existing — reuse |
| sonner / toast | ^2.0.7 | MIT | degrade-open notice | existing |
| Supabase Edge Functions (Deno) | platform | — | `capture-lead` (service_role write) | existing pattern (`create-payment`) |
| Supabase Postgres + RLS + Auth | platform | — | persistence, structural anonymity, dashboard auth | existing project `tkkghzcpwefwrgacgvdv` |
| **Vitest** | **^3.x (to pin)** | MIT | test runner (NEW — DDD-1) | Vite-native; reuses `@/*` alias |
| **@testing-library/react** | **^16.x (to pin)** | MIT | component tests (NEW) | RTL for React 19 |
| **@testing-library/jest-dom** | **^6.x (to pin)** | MIT | DOM matchers (NEW) | via setupFiles |
| **jsdom** | **^25.x (to pin)** | MIT | test DOM env (NEW) | Vitest `environment: "jsdom"` |

All choices OSS, MIT-licensed. No proprietary technology. Exact NEW-dep minor versions pinned at DELIVER against the React 19 peer matrix.

## Wave: DESIGN / [REF] Quality Validation (self-check)

- Requirements→components traced (US-01..06 map to the surfaces/core/adapters/tables above).
- Dependency-inversion: pure core depends on port interfaces only; adapters depend inward; verified by an import convention (core must not import `@supabase`/`window`).
- Simplest-solution: a feature module in the existing SPA; 2 simpler-alternatives rejected in ADR-035 (component-local state) and ADR-032 (no Edge Function at all) — neither met testability/PII-sealing. No microservice/backend introduced (DDD-8).
- C4 L1+L2 produced (`c4-design.md`); L3 deliberately omitted (no 5+-component subsystem).
- External integrations: Supabase (first-party) — no consumer-driven contract test required; the `capture-lead` request shape is covered by shared zod + a degrade-open component test. Analytics vendor (if any, DELIVER) is fire-and-forget, degrade-silent.
- Enforcement tooling (website-appropriate): the existing `eslint` flat config + an import-restriction lint rule keeping `core/` free of `@supabase`/DOM (the website analogue of ArchUnit/import-linter); plus Vitest as the behavioural net. (Lighthouse's ArchUnitNET/Biome do not apply to this repo.)
- Performance: no server-side performance architecture is warranted — scoring is O(1) client-side with no round-trip (constraint), the only "perf" KPI (median completion ≤5 min, KPI 2) is a UX-pacing target met by the one-question-at-a-time flow, and the two write paths are off the critical render path (degrade-open). The dashboard reads a low-volume table (community-scale rows); a `created_at` index on `responses`/`leads` covers the date-ordered lead table and is the only indexing need (a composite `(source, created_at)` index is the recommended upgrade only if per-`source` volume grows enough to make the discriminator-filtered, date-ordered query scan-bound).

## Wave: DESIGN / [REF] Open Questions (deferred to DISTILL/DELIVER)

1. **Final content copy** — ✅ **RESOLVED (PO, 2026-05-30): the answer-ladder + band copy reproduced from the epic is confirmed "fair" as the v1 baseline.** DISTILL treats the band/ladder text as **locked fixtures** (no longer pending). Copy may still be polished later, but it is no longer a DELIVER blocker.
2. **Dashboard account holders** — ✅ **RESOLVED (PO, 2026-05-30): `benjamin@letpeople.work` and `peter@letpeople.work`** get the Supabase Auth accounts (ADR-033 updated).
3. **Analytics vendor** — is a website analytics tool already wired? If not, v1 ships the no-op sink + Supabase-row funnel reconstruction (ADR-037). Resolve at DELIVER.
4. **`useScrollReveal` availability** — ✅ **RESOLVED (PO, 2026-05-30): the assessment feature merges FIRST (before `alt/redesign-2026`).** Therefore `useScrollReveal` will NOT exist on `main` when Slice 05 ships → **Slice 05 implements the scroll-reveal idiom with a small local hook** and reconciles to the shared `alt/redesign-2026` hook when that branch later merges. See Changed Assumptions item 1.
5. **CI for the website** — the website repo CI is build-only; adding a `pnpm test` job is a platform-architect/website-owner follow-up (DDD-1 consequence).
6. **Edge Function spam mitigation** — Turnstile/hCaptcha/rate-limit on `capture-lead` deferred (community volume); the function is the chokepoint if abuse appears (ADR-032).

## Wave: DESIGN / [REF] DELIVER-handoff Gates (crafter/DISTILL MUST honor)

These are not surprises — they are explicit gates surfaced by the security peer review. Each must be satisfied during DELIVER (or sequenced as noted).

1. **Anti-forgery acceptance gate (security-boundary property — ADR-032/036)**: because scoring is client-evaluable, `capture-lead` MUST re-validate the submitted `score`/`band` against the shared `assessmentContent.bands` ranges (one band table, shared via `supabase/_shared/bands.ts` or a CI parity assertion). DISTILL writes the acceptance test: *"a POST with a forged `band` (band name not matching the submitted `score`'s range), or a `score` outside 0-100, is rejected with no `leads` row written."* This is a DESIGN security property, not optional hardening.
2. **RLS migration review gate**: the `supabase/migrations/*.sql` for the two tables must be peer-reviewed to confirm (a) `responses` has **NO `anon` SELECT policy**; (b) the `responses` INSERT `WITH CHECK` bounds `source`/`kind` to known literals and the migration enables **no UPDATE/DELETE for `anon`**; (c) `leads` has **NO `anon` policy at all** (written only by the Edge Function via `service_role`).
3. **Dashboard fail-closed test gate (Slice 04)**: Slice 04 must assert RLS fail-closed directly — an **unauthenticated SELECT on BOTH `responses` and `leads` returns 0 rows** — independent of the route guard. The route guard (ADR-033) is defense-in-depth / UX only; RLS is the real boundary and must be tested as such.
4. **Website CI gate (DDD-1 consequence — sequencing prerequisite, not a late surprise)**: confirm the website repo CI runs `pnpm test` **before** Slice 01's feature code is gated on it. If CI is not yet wired for tests, split Slice 01: **01a** lands the Vitest/RTL framework + the scoring boundary tests (the test gate becomes real), then **01b** lands the feature code that the gate protects.
5. **Slice 05 hook-availability gate** — ✅ **RESOLVED (PO, 2026-05-30): we merge the assessment FIRST**, so `useScrollReveal` will not be on `main`. Slice 05 therefore **implements a small local scroll-reveal hook** (own the idiom locally) and reconciles to the shared `alt/redesign-2026` hook when that branch merges later. No file collision either way. See Changed Assumptions item 1.

## Wave: DESIGN / [REF] Changed Assumptions (back-propagation)

**1. `useScrollReveal` is NOT on `main`.** DISCUSS assumed (Pre-requisites + Slice 05) the scroll-reveal hook is consumable. Verbatim original (`feature-delta.md` Cross-Cutting / Website): *"the assessment pages must adopt the `alt/redesign-2026` visual language (scroll-reveal via `useScrollReveal`...)"* and (Slice 05) *"this slice consumes the shared tokens/hook, does not modify them."* 

- **DESIGN finding**: a repo search (`Grep useScrollReveal` across `/storage/repos/website`) returns **no file on `main`** — the hook lives only on the `alt/redesign-2026` branch.
- **Resolution (PO, 2026-05-30): the assessment feature merges FIRST**, ahead of `alt/redesign-2026`. So the hook will not exist on `main` when Slice 05 ships → **Slice 05 implements the scroll-reveal idiom with a small local hook** (e.g. an `IntersectionObserver`-based `useScrollReveal` inside the assessment module) rather than importing a non-existent shared hook, and reconciles to the shared hook when the redesign branch later lands. This does **not** change any AC.
- **Rationale**: DISCUSS recorded the coordination as non-blocking; DESIGN confirmed no *file collision* but surfaced a *consumption* dependency. The PO's merge-first decision settles it toward the local-implementation path. No story/AC text changes → **no `design/upstream-changes.md` required**; recorded here for DISTILL/DELIVER sequencing.

**2. Website tsconfig is loose (`strict:false`).** DISCUSS/CLAUDE.md frame TDD + type-safety as non-negotiable. DESIGN finding: the website `tsconfig.app.json` sets `strict:false`, `noImplicitAny:false`, `strictNullChecks:false` (shadcn/Lovable default). New assumption: tests (DDD-1) + zod at trust boundaries are the correctness backstop here, not the compiler; the pure core is written type-safe by discipline regardless of the loose project flags. No AC change; recorded for DELIVER so the crafter does not assume `strict` narrowing is available.

No DISCUSS story or AC is invalidated by any DESIGN decision; the two items above are sequencing/implementation clarifications, so `design/upstream-changes.md` is intentionally not created.

---

# Wave: DISTILL (Acceptance design — Quinn, acceptance-designer)

> **Placement: SPEC-FIRST (PO-locked).** This is a cross-repo website feature: code lands in
> `/storage/repos/website` (React 19 + Vite + Vitest, per DDD-1 / ADR-031), but that repo has no test
> runner yet — the Vitest + RTL bootstrap is DELIVER **Slice 01a**'s job. Therefore DISTILL writes NO
> files in the website repo. The binding scenario SSOT is the portable `.feature` set under
> `distill/` (this repo); DELIVER Slice 01a translates them into Vitest specs. Documentation density:
> **lean** (Tier-1 [REF] only). Wave-decision reconciliation gate: already passed (0 contradictions);
> not re-run.

## Wave: DISTILL / [REF] Mode banner (Phase-0 detection)

- `[lang-mode] typescript` — target runtime is the **website** repo. DELIVER stack (DDD-1 / ADR-031 /
  polyglot-matrix TS row): **Vitest 3 + React Testing Library + jsdom**, property-based via **fast-check**,
  skip marker `it.skip(...)`, scenario/spec split `*.scenarios.ts` + `*.specifications.ts`.
- `[policy-mode] inherit` — `docs/architecture/atdd-infrastructure-policy.md` exists. It records that this
  is a C#/.NET + React/TS + Playwright project and that the **Python-pilot artifacts are N/A**:
  `tests/common/state_delta.<ext>`, `assert_state_delta` Universe assertions, Hypothesis harnesses, and
  `__SCAFFOLD__` stubs do **not** apply. Consequence for this wave: **Mandate 8** (universe-bound
  `assert_state_delta` at layers 1-3) is **N/A** — layer-1/2 specs use traditional Vitest assertions with
  state-delta *in spirit* (assert the changed slot AND that adjacent observable slots are unchanged).
  **Mandate 9** still holds via the TS analogue: fast-check `@property` at the pure layer (scoring,
  quizMachine); example-only at component/adapter layers.
- `[port-mode] N/A` — no `tests/common/state_delta.ts` bootstrap (disavowed by the existing policy). No
  policy rows were appended (the website driving/driven ports are feature-local and the policy is the
  Lighthouse-project mechanism ledger; website mechanisms are recorded in this section instead).
- `[outcomes-registry] N/A` — methodology/cross-repo; the `nwave-ai outcomes` CLI is the DES Python
  project's tooling, not available/relevant for a website feature. Skipped deliberately.
- `[telemetry] N/A` — DocumentationDensityEvent emission is DES-project tooling; not run for this feature.

## Wave: DISTILL / [REF] Inherited commitments

| Origin | Commitment | DDD | Impact |
|--------|------------|-----|--------|
| DESIGN#DDD-1 | Vitest + RTL + jsdom is the website test stack; pure core → boundary/property tests, surfaces → component tests | DDD-1 | Every scenario below is authored for that stack; fast-check carries the `@property` scoring contract |
| DESIGN#DDD-2 | Lead capture re-validates score/band server-side; both capture paths degrade open | DDD-2 | Drives the anti-forgery gate scenario and the degrade-open scenario |
| DESIGN#DDD-3 | Dashboard auth via real per-user sign-in; RLS is the real boundary, route guard is UX | DDD-3 | Drives the fail-closed gate tested at the data boundary, not just the page |
| DESIGN#DDD-4 | Two-table structural anonymity, no join between answers and email | DDD-4 | Drives the "answers stay anonymous" and "no PII beyond email" assertions |
| DESIGN#DDD-5 | Pure scoring + pure quizMachine core behind four driven ports | DDD-5 | Lets layer-1 scoring be fast-check property tests with no DOM; degrade-open/resume use faked ports |
| DESIGN#DDD-6 | One typed content module; load-time invariants: 4 contiguous bands + Community CTA in every band | DDD-6 | Drives the band-coverage outline and the "Community in every band" property |
| DESIGN-handoff#gate-4 | Website CI must run the test job before feature code is gated; split Slice 01 into 01a (framework+tests) and 01b (feature) | DDD-1 | Mandate-7 scaffold materialization + the fail-for-right-reason RED gate are **deferred to Slice 01a** |

## Wave: DISTILL / [REF] Scenario list with tags

`.feature` SSOT files live in `docs/feature/flow-forecasting-readiness-assessment/distill/`.
Layer column uses the layered-discipline table (1=pure unit, 2=in-memory acceptance/component,
3=real-adapter/integration). 32 scenarios total; error+edge+security ≈ 44%.

| # | Scenario | Tags | US | Driving port | Layer |
|---|----------|------|----|--------------|-------|
| 1 | Visitor completes the assessment and sees a number, a band, and a next step | `@walking_skeleton @driving_port @real-io` | 01,02,03,05 | route `/assessment` (full flow) | 3 (WS) |
| 2 | Visitor advances through all six questions one at a time | `@driving_port @in-memory` | 01 | route `/assessment` | 2 |
| 3 | Visitor corrects an earlier answer without losing progress | `@driving_port @in-memory` | 01 | route `/assessment` | 2 |
| 4 | Visitor refreshes mid-assessment and resumes | `@error @driving_port @in-memory` | 01 | route `/assessment` + QuizPersistence | 2 |
| 5 | Visitor returns to a cleared assessment and is restarted gently | `@error @driving_port @in-memory` | 01 | route `/assessment` + QuizPersistence | 2 |
| 6 | A result cannot be produced before all six are answered (partial guard) | `@error @driving_port @in-memory` | 01 | route `/assessment` (quizMachine guard) | 2 |
| 7 | The assessment is usable on a 375px phone screen | `@driving_port @in-memory` | 01 | route `/assessment` | 2 |
| 8 | Every answer vector yields a deterministic score and exactly one band | `@property @pure` | 02 | `scoring()` pure fn | 1 |
| 9 | Higher answers never produce a lower score (monotonic) | `@property @pure` | 02 | `scoring()` pure fn | 1 |
| 10 | Boundary scores land in the documented band (outline: 0/25/26/50/51/75/76/100) | `@pure` | 02 | `scoring()` pure fn | 1 |
| 11 | Lowest answers map to lowest band (all-0 → 0 → Flying blind) | `@pure` | 02 | `scoring()` pure fn | 1 |
| 12 | Highest answers map to highest band (all-3 → 100 → Predictable) | `@pure` | 02 | `scoring()` pure fn | 1 |
| 13 | Representative middling result lands in the middle band (sum 9 → 50) | `@pure` | 02 | `scoring()` pure fn | 1 |
| 14 | Teaser shows the number and band without asking for an email | `@driving_port @in-memory` | 03 | route `/assessment` teaser surface | 2 |
| 15 | The credibility anchor is visible on the teaser | `@driving_port @in-memory` | 03 | teaser surface | 2 |
| 16 | A valid email unlocks the breakdown and records the lead | `@driving_port @in-memory` | 04 | email-gate form → LeadCapture | 2 |
| 17 | An invalid email is rejected and nothing is recorded | `@error @driving_port @in-memory` | 04 | email-gate form (zod) | 2 |
| 18 | A capture failure never costs the visitor the breakdown (degrade-open, retry-once) | `@error @driving_port @in-memory` | 04 | email-gate form → rejecting LeadCapture fake | 2 |
| 19 | The breakdown explains the band across both pillars | `@driving_port @in-memory` | 05 | breakdown surface ← content module | 2 |
| 20 | The recommended next steps match the band (outline: 3 bands) | `@driving_port @in-memory` | 05 | breakdown surface | 2 |
| 21 | The free Community next step appears in every band | `@property @driving_port @in-memory` | 05 | content module (load-time invariant) | 2 |
| 22 | A completer who never gives an email still leaves a captured completion | `@driving_port @in-memory` | 05,06 | route `/assessment` → ResponseRepository | 2 |
| 23 | A completed assessment is captured as a single response | `@driving_port @real-io @adapter-integration` | 06 | ResponseRepository.save | 3 |
| 24 | The internal dashboard summarizes captured results | `@driving_port @in-memory` | 06 | dashboard route `/admin/assessment` | 2 |
| 25 | The dashboard filters by the kind of response it shows | `@driving_port @in-memory` | 06 | dashboard route | 2 |
| 26 | The results dashboard is not visible to a visitor not signed in | `@error @security @driving_port` | 06 | dashboard route guard | 2 |
| 27 | A result claiming a band it did not earn is refused and recorded nowhere (anti-forgery) | `@error @security @real-io @adapter-integration` | 04,06 | LeadCapture (`capture-lead`) | 3 |
| 28 | A result with an impossible score is refused and recorded nowhere | `@error @security @real-io @adapter-integration` | 06 | LeadCapture (`capture-lead`) | 3 |
| 29 | An honest result whose band matches its score is recorded | `@security @real-io @adapter-integration` | 04 | LeadCapture (`capture-lead`) | 3 |
| 30 | Captured responses are not readable by anyone not signed in (RLS fail-closed) | `@error @security @real-io @adapter-integration` | 06 | ResponseRepository (RLS) | 3 |
| 31 | Captured leads are not readable by anyone not signed in (RLS fail-closed) | `@error @security @real-io @adapter-integration` | 06 | leads table (RLS, no anon policy) | 3 |
| 32 | A named team member can read the dashboard once signed in (outline: 2 accounts) | `@security @real-io @adapter-integration` | 06 | dashboard auth + RLS | 3 |

**The four mandatory DELIVER-handoff gate scenarios are all present:** gate-1 anti-forgery = #27 (+ #28
out-of-range, #29 honest-accept); gate-2 RLS fail-closed = #30 + #31 (+ #32 signed-in read); gate-3
degrade-open = #18; gate-4 scoring boundaries = #8 (property) + #10/#11/#12 (boundary examples).

## Wave: DISTILL / [REF] Walking Skeleton strategy (Architecture-of-Reference framing)

- **Exactly one** `@walking_skeleton @driving_port @real-io` scenario (#1): the Slice-01 client-only path —
  open `/assessment` → answer 6 → see score + band + band-specific next step. No gate, no persistence. It
  closes the end-to-end loop through the **production composition root** (the real `/assessment` route host
  wiring the real `scoring`, `quizMachine`, and `assessmentContent`). Litmus test: a non-technical
  stakeholder confirms "yes — answer six questions, get an honest number and a next step" is what visitors
  need. (Dim-5 user-centric: title = user goal, Then = user observations, no "touches all layers" framing.)
- **Port treatment (Architecture of Reference applied to the website):**
  - *Driving* (route `/assessment`, Navigation entry, email-gate form, dashboard route) → real adapter:
    the real route host / RTL `render` of the production surface (Slice 01 WS) — production composition root.
  - *Driven internal* (Supabase `responses` read/write, dashboard read) → **real-io at DELIVER** for the
    `@real-io @adapter-integration` scenarios (#23, #27-32) against the real Supabase project + RLS; faked
    at the component layer (#16, #22, #24-26) for fast deterministic feedback.
  - *Driven external / non-deterministic* → **faked**: the clock/analytics (`AnalyticsSink` no-op or spy),
    and the `capture-lead` Edge service-role write at the component layer. **Degrade-open (#18) uses a
    rejecting `LeadCapture` fake** that throws on first call so the breakdown-still-unlocks + retry-once
    behaviour is provable without a real outage.
- **What the in-memory doubles CANNOT model** (recorded per self-review item 4): a faked `LeadCapture` /
  `ResponseRepository` cannot prove RLS fail-closed or the server-side anti-forgery re-validation — those
  are properties of the real Supabase policies + the real Edge Function, which is exactly why #27-32 are
  `@real-io @adapter-integration` at layer 3 (DELIVER Slice 03/04), not in-memory.

## Wave: DISTILL / [REF] Driven Adapter coverage (Mandate 6 — every driven adapter ≥1 real-io / contract)

| Driven adapter (ADR-035) | `@real-io` scenario | Covered by |
|--------------------------|---------------------|------------|
| `SupabaseResponseRepository` (anon-INSERT into `responses`) | YES | #23 (single response captured, real write) + #30 (RLS: anon cannot read responses) |
| `EdgeFunctionLeadCapture` (`capture-lead`, service_role) | YES (contract) | #27/#28 anti-forgery refusal + #29 honest accept — exercise the real Edge Function contract (forged band / out-of-range / honest); #31 confirms `leads` sealed from anon |
| `SessionStoragePersistence` (sessionStorage) | Covered via component layer | #4 (resume), #5 (cleared → restart) drive the real persistence port through the route host (jsdom sessionStorage is real in the Vitest env) |
| `AnalyticsSink.track` (no-op/vendor) | N/A — fire-and-forget, degrade-silent | DESIGN: analytics is fire-and-forget; no observable `Then` beyond a spy. Funnel is reconstructed from Supabase rows (ADR-037). A spy assertion is OPTIONAL at DELIVER; no dedicated `@real-io` row required. |

Zero "NO — MISSING" rows. `EdgeFunctionLeadCapture` is a costly/first-party external surface; per Mandate
6 a contract-style real exercise (the anti-forgery refusal at the function boundary) satisfies the rule.

## Wave: DISTILL / [REF] Driving Adapter coverage (each driving port ≥1 scenario via its protocol)

| Driving port (ADR-035) | Protocol exercised | Scenario |
|------------------------|--------------------|----------|
| Route `/assessment` (full flow host) | Render the route + drive the user flow | #1 (WS, real-io), #2-7, #14-16, #22 |
| Navigation entry → `/assessment` | Nav item routes to the assessment | Covered by the WS entry (#1 "opens the assessment"); Slice 05 adds the restyled Nav entry (styling-only, no behaviour change → no separate behavioural scenario) |
| Email-gate form (RHF + zod) | Submit valid / invalid email | #16 (valid), #17 (invalid), #18 (degrade-open) |
| Dashboard route `/admin/assessment` (auth-gated) | Open the dashboard signed-in / signed-out | #24, #25 (signed-in summary), #26 (signed-out denied), #32 (named accounts) |

## Wave: DISTILL / [REF] Test placement

- **SSOT**: portable `.feature` files in `docs/feature/flow-forecasting-readiness-assessment/distill/`
  (`walking-skeleton.feature`, `assessment-flow.feature`, `scoring.feature`, `teaser-gate-breakdown.feature`,
  `capture-and-dashboard.feature`, `security-gates.feature`). These are the binding spec DELIVER Slice 01a
  translates into website Vitest specs.
- **Materialization (DELIVER, website repo)**: per the polyglot-matrix TS row — `*.scenarios.ts` +
  `*.specifications.ts`, `it.skip(...)` skip markers, fast-check for `@property`. Suggested website paths
  (NOT created now): `src/features/assessment/core/scoring.specifications.ts` (fast-check, no jsdom),
  `…/core/quizMachine.specifications.ts`, `…/components/*.scenarios.ts` (RTL + faked ports),
  `…/adapters/*.integration.ts` (real Supabase / Edge for `@real-io`).
- **Why spec-first**: the website repo has no Vitest runner yet (DDD-1); writing specs there now would not
  run. Slice 01a bootstraps the runner + the scoring boundary tests, making the test gate real before 01b's
  feature code is gated on it (DESIGN-handoff gate-4).

## Wave: DISTILL / [REF] Pre-requisites

- **DESIGN driving ports** (ADR-035): route `/assessment`, Navigation entry, email-gate form, dashboard
  route `/admin/assessment` — all in the website repo.
- **Slice 01a Vitest bootstrap** (DDD-1 / DESIGN-handoff gate-4): Vitest 3 + RTL + jsdom + fast-check
  installed and a `pnpm test` CI job wired BEFORE feature code is gated. This is Slice 01a's entry work and
  the prerequisite for every scenario above becoming executable.
- **RLS migration** (ADR-032/034, DESIGN-handoff gate-2): the two-table migration with `responses`
  INSERT-only-no-SELECT for anon, `leads` no-anon-policy, and `authenticated` SELECT on both — required
  before #30/#31/#32 can pass.
- **Shared band table** (`supabase/_shared/bands.ts`, ADR-036, DESIGN-handoff gate-1): ONE band-range
  definition imported by both the browser scoring core and the `capture-lead` Edge Function (or a CI parity
  assertion) — required before the anti-forgery scenarios #27-29 can pass against a single source of truth.
- **Locked content fixtures**: the six answer ladders + four band copies are PO-confirmed (DESIGN Open
  Question 1 RESOLVED) — DISTILL treats band/ladder text as locked fixtures.
- **Two dashboard accounts**: `benjamin@letpeople.work`, `peter@letpeople.work` provisioned in Supabase
  Auth (ADR-033) — required before #32.

## Wave: DISTILL / [REF] Scaffolds — DEFERRED to DELIVER Slice 01a

Per the spec-first placement decision, the Mandate-7 RED-scaffold step (materializing stub modules in the
website `src/` so the first spec is RED-not-BROKEN, and the **fail-for-the-right-reason RED gate**) is
**deferred to DELIVER Slice 01a**. DISTILL creates NO scaffold stubs and NO files in `/storage/repos/website`
in this wave. Slice 01a's entry gate is therefore: (a) bootstrap Vitest/RTL/jsdom/fast-check; (b) materialize
the `scoring` / `quizMachine` / port-interface scaffolds; (c) translate `scoring.feature` first and confirm it
fails for a missing-implementation reason (not an import/setup error) before unskipping further scenarios
one at a time.

## Wave: DISTILL / [REF] Slice → scenario mapping (one-at-a-time DELIVER order)

Every non-WS scenario is `@pending` so each maps to one DELIVER TDD cycle. Practical order (DISCUSS story
map): **01 → (02+03 together) → 04 → 05**.

| Slice | Scenarios |
|-------|-----------|
| **01 — WS quiz→band→CTA** (client-only) | #1 (WS), #2-7 (flow), #8-13 (scoring), #19-21 (breakdown content + CTA invariant — content/CTA rendered client-only in 01) |
| **01a — test-infra bootstrap** | Vitest/RTL/jsdom/fast-check + scaffolds + RED gate (no new scenarios; enables #8-13 first) |
| **02 — Supabase capture** | #23 (single response captured, real-io), #22 (no-email completion still captured) |
| **03 — email gate** | #14-18 (teaser/gate/breakdown/degrade-open), #27-29 (anti-forgery at `capture-lead`) |
| **04 — admin dashboard** | #24-26 (dashboard summary/filter/denied), #30-32 (RLS fail-closed + named-account read) |
| **05 — redesign adoption** | styling-only (Nav entry + scroll-reveal); no new behavioural scenario — existing scenarios must stay green through the restyle |

## Wave: DISTILL / [REF] Mandate compliance evidence

- **CM-A (hexagonal boundary)**: every scenario enters through a driving port (route `/assessment`,
  email-gate form, dashboard route) or the pure `scoring()` driving signature; no internal-component entry.
- **CM-B (business language)**: Gherkin uses domain terms only — "result", "band", "next step", "captured",
  "signed-in", "recorded". No API/endpoint/RLS/Supabase/jsonb/HTTP/SELECT in any scenario title or
  Given/When/Then. Technical contracts (RLS, service_role, Edge Function) live in these [REF] tables only.
- **CM-C (journey completeness + WS user-centricity)**: 1 WS (user goal, observable outcomes) + 31 focused;
  every story US-01..06 has ≥1 scenario; error+edge+security ≈ 44% (≥40%).
- **CM-D / CM-E (pure-fn extraction / Universe)**: scoring + quizMachine are pure (layer-1 fast-check, no
  fixtures); Mandate-8 `assert_state_delta` is **N/A for this repo** (policy: Python-pilot artifact) — TS
  specs assert the changed slot + adjacent-unchanged in spirit.
- **CM-F (PBT layer mode, Mandate 9)**: `@property` only at layer 1-2 (#8, #9 fast-check pure; #21 content
  invariant). Layer-3 `@real-io` sad paths (#27-31) are **example-only**, never PBT-generated (Mandate 11).
- **CM-G (Tier B, Mandate 10)**: **Tier B is NOT emitted.** The journey is config/linear-shaped — the rich
  input space (the 4^6 answer vectors) is already covered by the layer-1 fast-check property (#8/#9) at the
  pure `scoring`/`quizMachine` boundary, and the flow has a single guard, not a multi-command state space
  worth a `RuleBasedStateMachine`. Per Mandate-10 "skip when config-shaped / journey covered by Tier-A
  example + layer-1 property", Tier A + the fast-check pure layer suffices.
- **CM-H (Mandate 11)**: layer-3 sad paths (#17 invalid email is layer-2; #27/#28/#30/#31) are named
  example-based scenarios; no PBT machinery at layer 3.

# Content Spec — Lighthouse User Survey (ADO #5124)

Status: content LOCKED 2026-06-01 (refinement session). SSOT for the survey questions, the survey
start hook, and the trial opt-in (incl. the new `organization` field). Implementation lands in the
**website** repo (`/storage/repos/website`) plus a small Lighthouse FE/BE nudge part. Sibling:
[flow-forecasting-readiness-assessment content-spec](../flow-forecasting-readiness-assessment/content-spec.md).

House rules for all product copy: no em-dashes; keep questions as data/config (stable `/survey` route).

---

## 1. The survey questions (6, feedback-first then demographics)

S4 `assessment-interest` is REMOVED ("would you use our other product" does not belong in a feedback
survey). Three feedback questions added. Order is feedback-first, demographics-last.

Schema changes from today's 4-question set:
- `surveyContent.ts` content schema `.length(4)` → `.length(6)`.
- Question schema gains a **`kind`** discriminator: `"choice"` (default) | `"text"` (free text).
- `answers.ts`: drop `ASSESSMENT_INTEREST_OPTIONS` + the `assessmentInterest` key; add option-id
  constants + schema keys for the new questions; `primaryUse` is an **array** (multiselect);
  `improvement` is an **optional string** (free text).
- `summarizeSurvey.ts`: tally must count **array membership** for the multiselect; free-text answers
  are NOT tallied — show them as a separate plain list in the dashboard.

### Q1 · `recommend` (choice, single)
Prompt: **How likely are you to recommend Lighthouse to a colleague?**
- `very-likely` — Very likely
- `likely` — Likely
- `neutral` — Neutral
- `unlikely` — Unlikely

### Q2 · `primary-use` (choice, MULTISELECT — "select all that apply")
Prompt: **What do you use Lighthouse for?**
- `forecasting` — Forecasting
- `flow-metrics` — Flow Metrics
- `portfolio-overview` — Portfolio Overview
- `stakeholder-reporting` — Stakeholder Reporting
- `still-exploring` — Still Exploring

### Q3 · `improvement` (TEXT, free text, optional)
Prompt: **What would most improve Lighthouse for you?**
- Free-text input, optional, ~500-char cap. NO helper text. NOT tallied (dashboard lists responses).
- PII note: free text can contain PII; it is stored with the anonymous response. This was a conscious
  override of the closed-choice default. Keep it in the anonymous-response store, never joined to the
  trial table.

### Q4 · `team-count` (choice, single) — unchanged
Prompt: **How many teams are using Lighthouse in your organisation?**
- `just-mine` — Just mine (1)
- `two-to-five` — 2-5
- `six-to-ten` — 6-10
- `more-than-ten` — More than 10

### Q5 · `role` (choice, single) — added Consultant/Trainer
Prompt: **What's your role?**
- `scrum-master-agile-coach` — Scrum Master / Agile Coach
- `engineering-manager-delivery-lead` — Engineering Manager / Delivery Lead
- `product-manager-owner` — Product Manager / Product Owner
- `engineering-developer` — Engineering / Developer
- `consultant-trainer` — Consultant / Trainer
- `leadership-director` — Leadership / Director+
- `other` — Other

### Q6 · `discovery-channel` (choice, single) — added YouTube/Podcast, Blog/Article
Prompt: **How did you hear about Lighthouse?**
- `linkedin` — LinkedIn
- `conference-meetup` — Conference / Meetup
- `colleague-word-of-mouth` — Colleague / Word of mouth
- `google-search` — Google / Search
- `github` — GitHub
- `youtube-podcast` — YouTube / Podcast
- `blog-article` — Blog / Article
- `other` — Other

> The "id — Label" rows above map option `id` → display `label`. Old responses under retired option
> ids remain readable (the summarizer already buckets unknown ids as "historical").

---

## 2. Survey start hook (survey page intro + in-app nudge)

> Lighthouse never tracks how you use it, by design. That privacy is the point, but it also means
> we're flying blind on what to improve next, unless you tell us. This short survey is how we learn
> what's working and what's missing. It's completely anonymous and takes about two minutes. At the
> end you can opt in to a free one-month Premium trial as a thank-you, the only step where we'd ask
> for your email and organization.

---

## 3. Trial opt-in (after submitting the survey)

> Thanks, that's genuinely useful. Your feedback shapes where Lighthouse goes next. Want a free
> one-month Premium trial as a thank-you? Leave your email and organization and we'll set it up for
> you by hand. It's completely optional, and it's the only thing we store that isn't anonymous.

### Fields
- `email` (required when opting in; zod-validated).
- `organization` (required when opting in) — **NEW**. Needed to issue the license.
- SHIPPED refinement: the email + organization inputs are **hidden until the trial checkbox is
  ticked** (they otherwise read as required). Validation enforces a valid email + non-empty org only
  on opt-in.

### Rules
- Trial is OPT-IN; the survey submits anonymously without it.
- License issuance stays **manual** (locked decision — never auto-issue). Duration: **one month**.
- email + organization are stored in the trial/lead table only, kept structurally separate from the
  anonymous responses (two-table anonymity, ADR-034 / ADR-041 / ADR-046). NO foreign key back to the
  response.

---

## 4. Implementation ripples

### Website repo
- `src/features/survey/content/surveyContent.ts` — 6 questions, `kind` discriminator, `.length(6)`.
- `src/features/survey/core/answers.ts` — new option-id constants + schema keys; `primaryUse` array;
  `improvement` optional string; drop assessment-interest; update `getMockSurveyAnswers`.
- `src/features/survey/core/surveyMachine.ts` — step over text + multiselect question kinds.
- `src/features/survey/core/summarizeSurvey.ts` — array-membership tally for multiselect; free-text
  list (separate from tallies).
- `src/features/survey/components/*` (`SurveyQuestion`, `SurveyForm`, `SurveyIntro`, `SurveyCharts`) —
  render single-choice / multiselect / free-text; intro hook (section 2); trial opt-in form with the
  `organization` field (section 3).
- `supabase/functions/submit-survey/index.ts` + `supabase/_shared/surveyNotificationEmail.ts` —
  accept the new answer shape, the `organization` field on trial opt-in, surface org in the team
  notification email to `survey.answer@letpeople.work`.
- Trial/lead table — add an `organization` column.
- All corresponding tests (content schema, answers schema, summarizer, components, edge fn).

### Lighthouse repo (docs / SSOT)
- `docs/feature/lighthouse-user-survey/feature-delta.md` + relevant `distill/*.feature` — new question
  set, the `organization` field, the trial copy.
- ADO #5124 sync per `/ado-sync`.

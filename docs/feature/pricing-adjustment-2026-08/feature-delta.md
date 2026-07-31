# Feature Delta — pricing-adjustment-2026-08

**ADO**: User Story [#5563 "Adjust Pricing"](https://dev.azure.com/letpeoplework/Lighthouse/_workitems/edit/5563) (Active)
**Repo under change**: `letpeople.work` website repo (`/storage/repos/website`) + Stripe. **The Lighthouse
product repo is not touched** — no price string exists anywhere under `Lighthouse/` (verified by grep).
**Density**: lean (`~/.nwave/global-config.json` → `documentation.density = "lean"`, `expansion_prompt = "ask-intelligent"`)

---

## Wave: DISCUSS / [REF] Prior-Wave Reading Confirmation

- ✓ ADO 5563 (`az boards work-item show --id 5563`) — description, state, board column
- ✓ `docs/product/jobs.yaml` (3885 lines) — **no** pricing/purchase job existed; two added by this wave
- ✓ `docs/product/personas/forecasting-prospect.yaml` — buyer persona, reused
- ✓ `docs/product/personas/lighthouse-maintainer.yaml` — operator persona, reused
- ⊘ `docs/product/vision.md` (not found)
- ⊘ `docs/project-brief.md` (not found)
- ⊘ `docs/stakeholders.yaml` (not found)
- ⊘ `docs/feature/pricing-adjustment-2026-08/discover/` (not found — DISCUSS is the entry wave)
- ⊘ `docs/feature/pricing-adjustment-2026-08/diverge/` (not found)

No DISCOVER evidence exists to contradict, so no `## Changed Assumptions` section is required.

---

## Wave: DISCUSS / [REF] Persona IDs

| Persona | Role in this feature |
|---|---|
| `forecasting-prospect` | The buyer reading the pricing section and clicking through to Stripe checkout. Cares that the number on the page is the number charged. |
| `lighthouse-maintainer` | Benjamin. Ships the change, owns the Stripe account, and must not be awake at midnight to do it. |

---

## Wave: DISCUSS / [REF] JTBD One-Liners

- **`job-price-cutover-unattended`** (`lighthouse-maintainer`) — When our list price changes on a
  calendar date, I want the website and the checkout to switch by themselves at that exact moment, so I
  can ship the change the day before and not perform a manual cutover at midnight.
- **`job-price-shown-is-price-charged`** (`forecasting-prospect`) — When I decide to buy Lighthouse, I
  want the price on the pricing page to be the amount Stripe actually charges me, so I can put a
  defensible number in a budget request without fearing a surprise at checkout.

Both jobs are written in full (three dimensions, four forces, opportunity score) into
`docs/product/jobs.yaml`.

### Opportunity Scores

| Job | Importance | Current satisfaction | Gap | Note |
|---|---|---|---|---|
| `job-price-cutover-unattended` | 4 | 1 | **3** | Every price string on the site is a hardcoded literal in 9 places across 6 files, plus one hardcoded Stripe price id. There is no cutover mechanism at all today; a date change means a manual edit + deploy at the moment the date turns. |
| `job-price-shown-is-price-charged` | 5 | 2 | **3** | Already broken **today**: the JSON-LD offers on `src/pages/Lighthouse.tsx:534` and `src/pages/Index.tsx:86` already publish `"price": "2000"` while every visible surface says CHF 999. Search engines and AI assistants may already be quoting the higher price against a page showing the lower one. |

Highest leverage first: `job-price-shown-is-price-charged` — it is a live correctness defect, not just a
scheduling convenience. Slice 01 serves both jobs at once.

---

## Wave: DISCUSS / [REF] Current-State Surface Inventory

Every place a price is stated today. Grepped, not recalled.

| # | Surface | File:line | States today | Auto-flips? |
|---|---|---|---|---|
| S1 | Stripe checkout line item | `supabase/functions/create-payment/index.ts:24` | `price_1RrMcgKzDcGH6xxwg9ABCbwz` (CHF 999) | ❌ hardcoded id |
| S2 | Pre-launch banner | `src/pages/Lighthouse.tsx:1055-1063` | "Until August 2026 … CHF 999 … becomes CHF 2,000" | ✅ React runtime |
| S3 | Self-Service tier card | `src/pages/Lighthouse.tsx:1099` | CHF 999 | ✅ React runtime |
| S4 | Tier-card rate chip | `src/pages/Lighthouse.tsx:1102` | "Current rate · CHF 2,000 from August 2026" | ✅ React runtime |
| S5 | Checkout block price | `src/pages/Lighthouse.tsx:1386-1393` | CHF 999 + lock-in note | ✅ React runtime |
| S6 | JSON-LD offer (Lighthouse page) | `src/pages/Lighthouse.tsx:534` | **already `"2000"`** ⚠ | ✅ React runtime |
| S7 | JSON-LD offer (Index) | `src/pages/Index.tsx:86-95` | **already `"2000"`** ⚠ | ✅ React runtime |
| S8 | Comparison table | `public/compare/index.html:99` | "CHF 999/year today (CHF 2,000 from August 2026)" | ❌ static file |
| S9 | Machine-readable facts | `public/llms.txt:63,82` | same dual statement | ❌ static file |
| S10 | Crawler `<noscript>` copy | `index.html:76` | same dual statement | ❌ static file |
| S11 | Pilot / BYOD workshop | `src/components/EngagementPath.tsx:50` | CHF 2,000 (a **service**, not the licence) | ✅ React runtime |
| S12 | Flow Clarity Assessment | `src/components/ExpertiseAndServices.tsx:126` | "CHF 3,500 · Includes Lighthouse Premium License" | ✅ React runtime |

Confirmed by inspection: `scripts/prerender-meta.mjs` only rewrites `<head>` title/description/OG tags,
and `grep -c priceCurrency dist/index.html` = 0 — so **JSON-LD is client-rendered and does flip at
runtime**. S8/S9/S10 are plain static files and cannot flip without a deploy.

---

## Wave: DISCUSS / [REF] Locked Decisions

| ID | Decision | Rationale |
|---|---|---|
| **D1** | Self-Service list price CHF 999 → **CHF 2,000 / year**. Enterprise stays CHF 10,000. | ADO 5563; already pre-announced on the live site since the July release. |
| **D2** | **Hard cutoff.** No grandfathering mechanism, no promo code, no grace link. Anyone mid-conversation is handled manually over email. | User decision. The site's "lock CHF 999 for your full term" promise applies to licences already *sold*, which need no system support — their term is already paid. |
| **D3** | Cutover instant = **`2026-07-31T22:00:00Z`** = 2026-08-01 00:00 Europe/Zurich (CEST, UTC+2). | "Midnight" means Swiss midnight — LetPeopleWork GmbH is a Swiss company pricing in CHF. Pinning the instant in UTC removes any host-timezone ambiguity in both the browser and the Deno edge runtime. |
| **D4** | The flip is **date-gated in code, deployed 2026-07-31**, and takes effect with **no deploy at the cutover instant**. | User decision (mid-wave): "I wanna do most changes now … but it should auto-flip at midnight". |
| **D5** | **Benjamin creates the CHF 2,000 Stripe price object** and hands over the `price_...` id; the wave wires it. | User decision. No Stripe credential is used from this session; live billing config is not touched by an agent. |
| **D6** | The gate lives in **one module** (`src/lib/pricing.ts`) exporting the cutover instant, both licence prices, and both service prices. Every surface reads from it. | Nine literals in six files is the defect that made this a project instead of an edit. One source of truth also makes the D9 cleanup a one-file deletion. |
| **D7** | `create-payment` selects the Stripe price id by the **same** cutover instant, server-side. | The edge function is the authority on what is charged. A client-side price is a display concern only. |
| **D8** | Banner (S2), rate chip (S4) and lock-in note (S5) are **deleted entirely** post-cutover — not reworded. | User decision. Least copy to maintain; the transitional framing has no audience after 1 Aug. |
| **D9** | ~~Static surfaces S8/S9/S10 are updated on 2026-08-01 in the cleanup slice.~~ **REVISED 2026-07-31 (user)**: S8/S9/S10 state **CHF 2,000 flat, today**, in slice 02. | They cannot auto-flip, so the only choice is which side of the instant they are wrong on. Stating 2,000 early is the better error: crawlers and LLM indexes re-read on their own slow schedule, so the copy they cache should be the *durable* one — a machine quoting 2,000 a few hours early is harmless where one quoting 999 a week late is not. The React surfaces still gate, so no human-facing price is overstated at checkout. |
| **D10** | Services reprice in the same cutover: **Pilot/Implement CHF 2,000 → CHF 3,000**; **Flow Clarity Assessment CHF 3,500 → CHF 4,500**. Transform (CHF 10,000) and Flow Health Check (CHF 200/500) unchanged. | User decision. Two drivers: the pilot would otherwise print the same CHF 2,000 as the licence on the same site, and the Assessment bundles a licence — leaving it at 3,500 would shrink its service portion from CHF 2,501 to CHF 1,500. CHF 4,500 preserves ~CHF 2,500 of service value. |

---

## Wave: DISCUSS / [REF] Journey (lean)

Two arcs meeting at the cutover instant. Full schema: `docs/product/journeys/pricing-adjustment-2026-08.yaml`.

**Arc A — the maintainer (31 July, daytime)**
1. Creates the CHF 2,000 recurring/one-off price in Stripe → copies the `price_...` id. *(confident, in control)*
2. Hands the id over → it is wired into `pricing.ts` + `create-payment`. *(confident)*
3. Runs the fake-clock tests: same code, two clocks, two prices. *(reassured — the flip is proven before it happens)*
4. Deploys once, in daylight. *(done, not on standby)*
5. Sleeps. At 22:00Z the site and checkout change by themselves. *(unattended — the job's whole point)*
6. 1 August, at leisure: deletes the gate and the transitional copy, updates the three static files. *(tidy)*

**Arc B — the buyer**
- *Before the instant*: page says CHF 999; Stripe charges CHF 999. Banner explains the rise is coming.
- *After the instant*: page says CHF 2,000; Stripe charges CHF 2,000; the banner is gone. No "price changed" surprise mid-flow.

**Emotional arc**: the maintainer moves confident → reassured → unattended (rising, and the peak is
*absence* of work). The buyer's arc is deliberately flat: nothing about a price change should feel
eventful to someone mid-evaluation.

### Shared artifacts

| Artifact | Source of truth | Consumers | Risk |
|---|---|---|---|
| `PRICE_CUTOVER_INSTANT` (`2026-07-31T22:00:00Z`) | `src/lib/pricing.ts` | every price surface; `create-payment` (duplicated as a const in the edge function — Supabase functions do not import from `src/`) | **HIGH** — two copies of one instant. They must be literally identical or page and checkout disagree for the drift window. AC-1.5 pins this. |
| `STRIPE_PRICE_ID_2000` | Stripe dashboard (Benjamin, D5) | `create-payment` only | **HIGH** — a wrong/test-mode id breaks checkout for everyone after the instant |
| `selfServicePrice` | `src/lib/pricing.ts` | S2-S7 | MEDIUM — display only, but a mismatch against Stripe is the exact defect this feature exists to prevent |
| `pilotPrice`, `assessmentPrice` | `src/lib/pricing.ts` | S11, S12 | LOW — copy only, no payment path |

---

## Wave: DISCUSS / [REF] User Stories

### US-01 — The licence price flips itself at midnight

**As** the Lighthouse maintainer, **I want** every price statement on the website to switch from CHF 999
to CHF 2,000 at a pinned instant without a deploy, **so that** I can ship the change on 31 July in
daylight and let the calendar do the cutover.
`job_id: job-price-cutover-unattended`

#### Elevator Pitch
Before: the maintainer cannot change the list price without editing nine literals in six files and
deploying at the moment the date turns.
After: open `https://letpeople.work/lighthouse` → before 2026-07-31T22:00Z the Self-Service card reads
`CHF 999`; reload after that instant, with no deploy in between, and the same card reads `CHF 2,000`.
Decision enabled: the maintainer decides to ship on 31 July and go to bed, instead of holding a
manual cutover open at midnight.

**Acceptance criteria**
- **AC-1.1** With the clock faked to `2026-07-31T21:59:00Z`, the Self-Service tier card renders `CHF 999`.
- **AC-1.2** With the clock faked to `2026-07-31T22:00:00Z` (the instant itself, inclusive), the same
  card renders `CHF 2,000`. The boundary is `>=`, not `>`.
- **AC-1.3** At the post-cutover clock, the pre-launch banner (S2), the rate chip (S4) and the lock-in
  note (S5) do not render at all — no leftover "from August 2026" phrasing anywhere in the DOM.
- **AC-1.4** At both clocks, the JSON-LD `Offer` for Self-Service on `/lighthouse` and on `/` carries the
  **same** number the visible card shows. (Today it does not — both already say `2000`; this AC closes
  that live defect.)
- **AC-1.5** A test asserts the cutover constant in `src/lib/pricing.ts` and the one in
  `supabase/functions/create-payment/index.ts` are byte-identical strings.
- **AC-1.6** `grep -rnE "CHF ?999|CHF ?2,?000" src/` returns hits only in `src/lib/pricing.ts` and its
  test — no price literal survives in a component.

### US-02 — Checkout charges what the page showed

**As** a forecasting prospect, **I want** Stripe to charge the price the pricing page displayed, **so
that** I can send a budget request without a surprise at the payment step.
`job_id: job-price-shown-is-price-charged`

#### Elevator Pitch
Before: `create-payment` always bills the hardcoded CHF 999 price id, so on 1 August the page and the
checkout would disagree by CHF 1,001.
After: fill the licence form on `/lighthouse` and click "Get your license" → the Stripe checkout page
opens showing `CHF 2,000.00` after the cutover instant and `CHF 999.00` before it.
Decision enabled: the buyer commits to the purchase at a price they can defend, instead of abandoning
checkout on a number they did not expect.

**Acceptance criteria**
- **AC-2.1** `create-payment` resolves the line-item price id from the cutover instant: the CHF 999 id
  before it, the CHF 2,000 id at-or-after it.
- **AC-2.2** The resolver is a pure function taking `now` as an argument, unit-tested at three clocks
  (one minute before, the exact instant, one minute after).
- **AC-2.3** A real Stripe **test-mode** checkout session created after the cutover instant shows
  `CHF 2,000.00`. Evidence: the session URL / dashboard amount, captured in the DELIVER log.
- **AC-2.4** The CHF 999 price object is **not** archived on 31 July — it must stay resolvable for the
  pre-cutover branch until the D9 cleanup removes that branch.
- **AC-2.5** A checkout session created in the pre-cutover branch still succeeds end-to-end (the
  existing flow is not regressed by the gate).

### US-03 — Services ladder moves with the licence

**As** a forecasting prospect, **I want** the pilot and the assessment to carry prices that are
distinguishable from the licence, **so that** I can tell what I am buying when two lines on the same
page both quote CHF 2,000.
`job_id: job-price-shown-is-price-charged`

#### Elevator Pitch
Before: after the licence flip, the pilot workshop and the annual licence would both read "CHF 2,000"
on letpeople.work, and the Assessment would bundle a CHF 2,000 licence inside a CHF 3,500 price.
After: open `https://letpeople.work/` → the Implement step of the engagement path reads `CHF 3,000` and
the Flow Clarity Assessment reads `CHF 4,500 · Includes Lighthouse Premium License`, at-or-after the
cutover instant.
Decision enabled: a prospect comparing "buy the licence" against "run a pilot with us" can see they are
two different-sized commitments, and picks the right entry point.

**Acceptance criteria**
- **AC-3.1** At the post-cutover clock, `EngagementPath` Implement renders `CHF 3,000`; at the
  pre-cutover clock it renders `CHF 2,000`.
- **AC-3.2** At the post-cutover clock, Flow Clarity Assessment renders `CHF 4,500`; pre-cutover,
  `CHF 3,500`. The "Includes Lighthouse Premium License" wording is retained verbatim.
- **AC-3.3** Transform (`CHF 10,000`, `priceFrom: true`) and Flow Health Check (`from CHF 200/team ·
  CHF 500/portfolio`) are unchanged at both clocks.
- **AC-3.4** At the post-cutover clock, no two *different* offerings on the same rendered page state the
  same CHF figure for the licence and the pilot.
- **AC-3.5** (revised D9) `public/llms.txt`, `public/compare/index.html` and root `index.html` state
  Self-Service at **CHF 2,000/year flat** — no "today", no "from August 2026" qualifier — and the
  services figures they mention, if any, match slice 02's numbers. Ships 31 July, ungated: these are
  static files with no runtime.
- **AC-3.6** The three files agree with each other verbatim on the price sentence (the root
  `index.html` comment above the `<noscript>` block requires it).

### US-04 — Cleanup on 1 August

**As** the Lighthouse maintainer, **I want** the date gate and the transitional copy gone the day after,
**so that** the codebase carries no dead branch and no crawler reads a stale price.
`job_id: job-price-cutover-unattended`

#### Elevator Pitch
Before: after the cutover, three static files still tell crawlers "CHF 999 today", and the code still
carries a pre-cutover branch nobody can ever reach again.
After: `curl -s https://letpeople.work/llms.txt | grep -i self-service` → reports `CHF 2,000/year` with
no "today" / "from August 2026" qualifier, and the same for `/compare/` and the `<noscript>` block.
Decision enabled: an AI assistant asked "what does Lighthouse cost?" quotes CHF 2,000 flat, so the
maintainer stops fielding "is it 999 or 2,000?" mails.

**Acceptance criteria**
- **AC-4.1** ~~moved to slice 02 (AC-3.5) by revised D9.~~ Re-verify after the gate removal that
  `public/llms.txt`, `public/compare/index.html` and root `index.html` still state CHF 2,000/year with
  no transitional qualifier.
- **AC-4.2** `grep -rn "999" src public index.html | grep -v 999999` returns zero price hits.
- **AC-4.3** The cutover constant, the pre-cutover branch, and the CHF 999 price id are deleted from
  both `src/lib/pricing.ts` and `create-payment`; the clock-dependent tests are replaced by flat
  assertions.
- **AC-4.4** `pnpm test` and `pnpm build` pass on the website repo after the deletion.
- **AC-4.5** The CHF 999 Stripe price object is archived (manual, Benjamin) once AC-4.3 has shipped —
  not before.

---

## Wave: DISCUSS / [REF] Out of Scope

- **Enterprise price** (CHF 10,000) — unchanged (D1).
- **Transform** engagement (from CHF 10,000) and **Flow Health Check** (CHF 200/500) — unchanged (D10).
- **Any change in the Lighthouse product repo** — the app has no price string; the licence file it
  imports is price-agnostic. Verified by grep across `docs/` and `Lighthouse.Frontend/`.
- **Grandfathering machinery** — promo codes, grace links, dual live prices (D2). `allow_promotion_codes`
  stays `true` as it is today, but no coupon is created by this feature.
- **Existing customers' renewals** — handled per contract, out of band.
- **Refactoring `create-payment` to read the price id from a Supabase secret** — offered and not chosen;
  the id stays in code so the change is reviewable in the diff.
- **Currency other than CHF**, VAT handling, regional pricing.

---

## Wave: DISCUSS / [REF] Walking-Skeleton Strategy

**Strategy B — extend an existing walking skeleton.** The purchase flow (form → `create-payment` →
Stripe checkout → `stripe-webhook` → licence mail) already runs in production end-to-end. This feature
threads one new decision (which price, by clock) through that live path. No new skeleton is built, and
no slice may introduce a second payment path.

---

## Wave: DISCUSS / [REF] Driving Ports

| Port | Surface |
|---|---|
| Browser (React SPA) | `https://letpeople.work/lighthouse` pricing section; `https://letpeople.work/` engagement path |
| HTTP (Supabase edge fn) | `POST /functions/v1/create-payment` → returns Stripe checkout URL |
| Static HTTP | `GET /llms.txt`, `GET /compare/`, the root `<noscript>` block |
| Stripe dashboard (human) | price-object creation + archival (D5, AC-4.5) — outside the codebase |

---

## Wave: DISCUSS / [REF] Pre-requisites

1. **BLOCKING for slice 01** — Benjamin creates the CHF 2,000 Stripe price object in **live** mode and
   hands over the `price_...` id. Nothing in slice 01 can be verified end-to-end without it.
2. Stripe **test-mode** access for AC-2.3 (a test-mode twin of the CHF 2,000 price).
3. Deploy access to the website repo on 31 July (the gate must be live *before* the instant, or the
   feature degenerates into a manual midnight cutover).
4. Slice 02 has **no** Stripe dependency and can proceed while (1) is outstanding.

---

## Wave: DISCUSS / [REF] Outcome KPIs

| KPI | Target | Measurement |
|---|---|---|
| Checkout sessions created at the CHF 999 price after `2026-07-31T22:00:00Z` | **0** | Stripe dashboard: sessions filtered by the old price id, `created >` cutover |
| Deploys required between the 31 Jul deploy and the 1 Aug cleanup | **0** | `git log` on the website repo `main` between the two timestamps |
| Surfaces stating CHF 999 after the cleanup ships | **0** | `grep -rn "999" src public index.html \| grep -v 999999` |
| Page price vs. Stripe charged amount agreement | **100%** at both clocks | AC-1.x + AC-2.3 evidence (fake-clock tests + one live test-mode session) |
| Price literals outside `src/lib/pricing.ts` | **0** | AC-1.6 grep |
| Maintainer minutes spent awake at the cutover instant | **0** | self-reported on 1 Aug |

---

## Wave: DISCUSS / [REF] Scope Assessment

**PASS — right-sized.** Against the oversize heuristics: 4 user stories (<10); one repo, three
technologies (React SPA, Deno edge function, Stripe) — at the ≥3 line but a single bounded context
("pricing statement"); the skeleton already exists so integration points added = 0; effort ≈ 1 day
across three slices; the licence flip and the services reprice are separable outcomes and are split
accordingly. No split beyond the three slices below is warranted.

---

## Wave: DISCUSS / [REF] Story Map & Slices

**Backbone**: *State the price* → *Charge the price* → *Publish the price to machines*.

| Slice | Stories | Ships | Blocked by |
|---|---|---|---|
| [slice-01](../../feature/pricing-adjustment-2026-08/slices/slice-01-licence-price-auto-flip.md) — licence price auto-flip, page + checkout | US-01, US-02 | 31 Jul | Stripe price id (D5) |
| [slice-02](../../feature/pricing-adjustment-2026-08/slices/slice-02-services-ladder-reprice.md) — services ladder reprice on the same gate **+ static surfaces S8/S9/S10 flat to CHF 2,000** (revised D9) | US-03 | 31 Jul | slice-01's `pricing.ts` |
| [slice-03](../../feature/pricing-adjustment-2026-08/slices/slice-03-post-cutover-cleanup.md) — gate + transitional copy removal, static surfaces | US-04 | 1 Aug | the cutover instant passing |

**Execution order & rationale**
1. **slice-01 first** — highest learning leverage: it is the only slice that can disprove the central
   premise (that one gate can drive both a browser render and a server-side Stripe decision), and it is
   the one with an external dependency, so any handover delay surfaces earliest. It is also the
   deadline-bearing slice.
2. **slice-02 second** — pure reuse of slice-01's module; zero external dependency. If the Stripe
   handover stalls, this ships anyway and the day is not lost.
3. **slice-03 last, next day** — cannot be verified until the instant has actually passed. Deliberately
   *not* merged into slice-01: doing so would mean deleting the pre-cutover branch before it has run.

### Carpaccio taste tests

| Test | Verdict |
|---|---|
| "Ships 4+ new components" → not thin | **Pass** — slice-01 adds one module (`pricing.ts`); slices 02/03 add none. |
| "Every slice depends on a new abstraction → ship it first" | **Documented deviation.** `pricing.ts` *is* the new abstraction and it ships inside slice-01 rather than ahead of it — because alone it would be an `@infrastructure`-only slice, which the slice-composition gate forbids. It arrives with US-01's visible price change in the same slice. |
| "No slice disproves a pre-commitment" | **Pass** — see the hypothesis on each brief; slice-01's would falsify the whole D4 approach. |
| "Synthetic data only → proves plumbing, not value" | **Pass** — AC-2.3 requires a real Stripe checkout session (test mode), not a mocked one. Fake clocks are used for *time*, which has no production equivalent to sample. |
| "2+ slices identical except scale → merge" | **Pass** — 01 = payment path, 02 = copy only, 03 = deletion. Different risk, different day. |

**Slice-composition gate**: each of the three slices contains at least one user-visible value story. No
slice is `@infrastructure`-only.

---

## Wave: DISCUSS / [REF] Definition of Ready

| # | Item | Evidence |
|---|---|---|
| 1 | Business value articulated | List price +100%; the pre-announcement has been live on the site since the July release, so the commitment is already public. |
| 2 | User stories in LeanUX format with job traceability | US-01…US-04, each carrying a `job_id` resolving in `docs/product/jobs.yaml`. |
| 3 | Acceptance criteria testable | 20 ACs, each with a named clock, a named file, or a named grep. No "works correctly". |
| 4 | Dependencies identified | Pre-requisites section: Stripe price id (blocking, human), test-mode twin, deploy window. |
| 5 | Sized / right-sized | Scope Assessment = PASS; three slices, each ≤1 day. |
| 6 | No open blockers besides the declared one | Only the Stripe id (D5). Slice 02 is unblocked, so the wave is not idle. |
| 7 | Design / approach agreed | D1-D10 all locked by the user in this wave, including the two service numbers. |
| 8 | Data / environment available | Website repo local at `/storage/repos/website`, `pnpm test` + `pnpm build` runnable; vitest with fake timers already in the stack (`vitest.config.ts`, existing `*.test.tsx`). |
| 9 | Stakeholder agreement | Benjamin is the buyer, the maintainer, and the Stripe account owner; every decision above is his direct answer this session. |

**Requirements completeness: 0.97.** The single residue is the exact Stripe `price_...` id (D5), which is
a handover value, not an unknown requirement.

---

## Wave: DISCUSS / [REF] Known Risks

| # | Risk | Handling |
|---|---|---|
| R1 | **Two copies of the cutover instant** — `src/lib/pricing.ts` and `create-payment` cannot share a module (Supabase edge functions are deployed standalone). A drift means the page and the checkout disagree. | AC-1.5 asserts the two strings are identical. |
| R2 | **The stale-tab window** — a visitor who loads the page at 23:58 and clicks buy at 00:01 sees CHF 999 and is charged CHF 2,000. | Accepted, not mitigated. Exposure is minutes on one night; the edge function must stay authoritative. Recorded here so it is a decision, not a surprise. |
| R3 | **Static surfaces do not flip** (S8/S9/S10) — under revised D9 they state CHF 2,000 from 31 July, a few hours *ahead* of the real price. | Accepted (user, D9). A crawler-facing overstatement for one afternoon; the human path (page + checkout) stays correct throughout because it is gated. |
| R4 | **JSON-LD already publishes 2000** while the page shows 999 — a live inconsistency predating this work. | Closed by AC-1.4, which binds both to the same source. |
| R5 | **Wrong-mode Stripe id** (test id pasted into the live path) breaks all checkout after the instant. | AC-2.3 requires an observed session amount; the live id is verified against the live dashboard before the 31 Jul deploy. |
| R6 | **Archiving CHF 999 too early** kills the pre-cutover branch on 31 July. | AC-2.4 forbids it; AC-4.5 sequences the archival after slice-03. |

---

## Wave: DISCUSS / [REF] Handoff

**To**: `nw-solution-architect` (DESIGN) — full artifact set.
**To**: `nw-platform-architect` (DEVOPS) — the Outcome KPIs section only.

DESIGN's open questions, in order: where `pricing.ts` sits relative to the existing `src/lib`
conventions; how the two cutover constants are kept identical (test-asserted duplication vs. a generated
file vs. a Supabase secret); and whether the fake-clock tests inject `now` as a parameter or use
`vi.setSystemTime`.

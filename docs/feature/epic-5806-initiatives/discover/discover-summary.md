# DISCOVER Summary — Epic 5806 "Visualize Initiatives"

**2026-09-29 · desk research only · no interviews · no GitHub trail (issues are disabled on the repo)**

## Gates

| Gate | Status | Why |
|---|---|---|
| G1 Problem validated | **FAIL** | 0 interviews. The problem exists only in the maintainer's words |
| G2 Opportunities scored | Not reached | No importance or satisfaction data |
| G3 Solution tested | Not reached | — |
| G4 Viability | Not reached. Early warning on the Premium thesis | Free tier is capped at 1 Portfolio; licence price is flat (E9, E10) |

## Recommendation: NO-GO on building the page now, GO on a two-week validation sprint

The Epic is plausible but unproven. It also overlaps more existing capability than its text
suggests:
- The Parent column on each Portfolio is free. (Group Features by Parent was removed in 2025-11; its docs are stale.)
- Rule-based Deliveries on `ParentReferenceId` already give a per-Initiative forecast (Premium).
- The Features page is already the instance-wide "snappy overview".

The real gap is a **cross-Portfolio roll-up per parent**. Whether anyone needs it depends on three
facts we don't have: does the parent level exist in customers' data, does it cross Portfolios, and
who reads it.

Two structural findings challenge the Premium intent and need a decision before DISCUSS:

1. **It can't sell itself on the free tier.** A free instance has one Portfolio, so a
   cross-Portfolio Initiative view has nothing to cross. It can only convert through marketing and
   demos, not through try-then-buy.
2. **Flat licence price.** With one price, the feature earns money only through new conversions or
   renewals. Target the Premium KPI at renewal or evaluation conversion, not "more revenue per
   customer".

### Validation sprint (cheapest first)

1. ~~GitHub search~~: checked 2026-09-29. Issues are disabled on `LetPeopleWork/Lighthouse`, so
   there is no public trail to mine. Demand evidence has to come from the maintainer's own customer
   conversations.
2. **Data count (tests A3, A6, A7).** On the dev instance and 3–5 friendly Premium instances:
   - share of open Features with a resolvable parent,
   - parents spanning 2+ Portfolios,
   - parent work-item types.
3. **Five Mom Test calls with Premium customers (tests A2, A4, A5).** Ask:
   - "Tell me about the last strategy or leadership review."
   - "Who prepared what, from which tool?"
   - "What decision came out of it?"
   - "What did you do about the Initiatives Lighthouse doesn't show?"
4. **Instrument what exists.** Nothing records whether anyone reads the Parent column. A name-only
   event (Parent column seen or parent link clicked) would tell us whether the free parent view is
   used at all.
5. **Optional fake door (tests A1).** A disabled "Initiatives (Premium)" entry on free instances.

If steps 2 and 3 come back positive, the smallest candidate slice to take into DIVERGE is:
- a **Parent column plus grouping by parent on the existing Features page** (instance-wide, reusing
  X1, X2 and X4),
- with a roll-up per parent that reuses the Delivery or joint-likelihood machinery,
- plus an export, if A2 says executives read artefacts rather than pages.

## Questions only the maintainer can answer

1. **Who asked?** Name the customers or conversations behind "we miss the executive level". What did
   they *do* (not say) that showed the gap?
2. **Who is the reader?** Does any Premium customer have an executive with a Lighthouse login? Or is
   the user the RTE or Delivery Lead who prepares the review?
3. **What is the Premium goal?** New conversions, renewals or enterprise deals? With a flat price
   and a 1-Portfolio free cap, which one should this feature move?
4. **What question should the view answer?** When each Initiative lands (forecast), whether it is on
   track (status), or where WIP and priorities should shift between Initiatives (Flight Level 3
   balancing)?
5. **Do your customers' Initiatives span several Portfolios?** If not, the per-Portfolio Parent column
   already covers it.
6. **Why is "somewhat manage with Deliveries" not enough?** Setup effort, missing auto-creation,
   wrong place in the navigation, or no cross-Portfolio view?
7. **Premium boundary.** Would the Parent column stay free, with only the new
   roll-up gated?
8. **Order.** Flight Level 3 is about prioritising Initiatives. Should Initiatives take part in the
   instance-wide Feature order, or is this read-only?

## Artefacts

- `problem-validation.md`: statement, candidate users, existing capabilities, 12 graded evidence
  items, G1 status
- `assumptions.md`: 10 scored assumptions, test-first set, hypotheses for the top three

## Maintainer answers (2026-09-29)

| Question | Answer | Consequence |
|---|---|---|
| Who reads it? | **Both, RTE first.** The RTE or Delivery Lead is the user; the page must also be clean enough to show or share with an executive directly | Design for the preparer; export or share polish matters, a separate exec persona does not |
| Which question? | **"When will it land?" and "Is it on track?"** "Signals" and "Reports" are later and live in other Epics, so they are out of scope here | Forecast plus health per Initiative. WIP balancing and generic flow dashboards are out |
| Do Initiatives span Portfolios? | **Mostly one Portfolio** | A cross-Portfolio roll-up is not the core. The value is an overview of *all* Initiatives, each rolled up within its Portfolio, and the multi-Portfolio case must not break |
| Premium goal? | **Enterprise deals** | Sold through demos and sponsor conversations, not try-then-buy. The free tier's 1-Portfolio cap matters less; the demo data must tell the story |

Consequence for A1 (Premium sales): the sales motion is the enterprise demo, so the riskiest part is the
demo narrative, not free-tier discovery. A3 (is the parent level populated) is still unmeasured.

**Framing hint (maintainer, 2026-09-29):** SAFe describes three levels, Epic → Feature → Story. In
Lighthouse, Story maps to the Team level and Feature maps to the Portfolio level, so SAFe's Epic is the
level this Epic adds. Lighthouse is deliberately not SAFe-only: it must stay wide and flexible, so the
level above Feature may be an Epic, an Initiative, an Objective or OKR, a Theme, or whatever an org uses.
Its name must be configurable, not a hardcoded tracker or framework word. This also weakens A7 ("one
consistent parent type"): mixed parent types across Portfolios are expected, not an edge case.

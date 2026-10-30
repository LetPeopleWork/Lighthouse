# Story 6452 — Delivery Metrics explain themselves

**Shipped:** 2026-10-30 · ADO User Story #6452

## The problem

A customer could not explain the Delivery **Metrics** charts to their own clients. They could not say what
the Predictability y-axis meant or how to read the Burnup, and without that they could not roll Lighthouse
out. The flow-metric widgets already had an ⓘ popover with a Learn More link; the delivery charts had
nothing.

## What changed

- **An ⓘ popover on each of the four charts** (Burnup, Predictability, Delivery Progress, Features over
  Time), using the same `InfoPopoverButton` as the flow-metric widgets. Each popover has a one-line
  purpose, a **What you see** list, a **How to read it** list that says which shapes are good or bad news,
  and a Learn More link to that chart's section of `docs/portfolios/detail.md`. The copy lives in
  `deliveryChartInfo.ts` and renders the user's Terminology words for Work Items, Feature(s) and
  Delivery.
- **Every axis labelled.**
  - Burnup y-axis: *Work Items*.
  - Features over Time: *Date* along the bottom, *Work Items* and *Features* replacing "Items" and
    "Count".
  - Two layout fixes found while regenerating the screenshots:
    - Features over Time's date ticks had disappeared once the axis gained a title.
    - The left axes of Features over Time and Predictability (When?) clipped their numbers and dates.
- **Docs:**
  - A paragraph introduces the ⓘ.
  - Burnup, Predictability and Delivery Progress each gained a "How to read it" passage.
  - The Predictability text no longer claims "coloured confidence bands": the line itself takes the level
    colour.
  - The fever-chart heading now matches the UI title: "Delivery Progress (Fever Chart)".

## Decisions worth keeping

- **Popover only, no visible subtitle.** This matches the flow-metric widgets. The first review of the
  running build turned the paragraph into the purpose / see / read structure.
- **Text no tool reads, kept under one rule:** an article never stands before a renamed term ("A Epic"),
  so bullets use the "label: meaning" form.
- **Titles still say "Delivery …"** while the popover text follows the Delivery term. Whether titles
  should follow Terminology too was left open for the maintainer.
- **The repeated `About <title>` names** when two Deliveries are open were accepted, the same as the
  existing `Enlarge <title>` buttons.

## Gates

- **Mutation:** frontend 80.00 % on the changed lines, then 82.28 % after the restructure. The survivors
  are layout-only `sx` literals, which jsdom cannot observe. See `mutation-results.md`.
- **Process:** one adversarial review, NEEDS_REVISION, resolved in the same story.
- **Waves:** the maintainer skipped DISCOVER to DEVOPS explicitly, and ran DISTILL and DELIVER at the
  `lean` rigor profile.
- **Out of this story:** a project skill, `.claude/skills/visualization`, was written alongside it. It
  distils Storytelling with Data, The Big Book of Dashboards and #MakeoverMonday for future data views.

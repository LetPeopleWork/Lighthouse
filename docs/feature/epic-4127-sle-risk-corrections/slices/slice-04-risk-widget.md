# Slice 04 — SLE Risk widget with RAG derived from the team's SLE

**ADO**: User Story #6036 · **Story**: US-R2-04 · **Job**: `job-flow-coach-act-before-sle-breach`
**Estimate**: ≤1 day.

## Goal

The at-risk count becomes a widget with a status of its own, so the WIP count stops carrying two meanings.

## Learning hypothesis

**Disproved if** the derived allowance flips a team's RAG on a rounding artifact, or if Observe turns out to be reachable at the WIP real teams run — either would mean the rule was tuned against arithmetic rather than against a board.

**Confirmed if** a team on 85% @ 4 days and a team on 70% @ 10 days each read a status that matches what their own coach would say about the same list.

## IN scope

- **A widget**, replacing the "N at risk" subtitle on the Work Items In Progress card. Remove the `atRisk` prop from `WipOverviewWidget` (this reverts #6017, never released).
- **At risk = 70% or more.** Fixed and explainable; it belongs in the widget info description and in the docs. `sleRiskAtRiskSummary` simplifies to the single threshold.
- **`computeSleRiskRag`**, alongside the existing rules in `ragRules.ts`, evaluated in order:
  1. no SLE configured → **Act** (consistent with `computeWipOverviewRag` and `computeBlockedMaxAgeRag` surfacing missing configuration)
  2. `atRiskCount / wipCount >= (100 - ServiceLevelExpectationProbability)` → **Act**
  3. `atRiskCount >= 1` → **Observe**
  4. otherwise → **Sustain**

  `wipCount == 0` short-circuits to Sustain, which also avoids the division by zero.
- **The allowance is derived, never configured separately** — 85% allows 15%, 70% allows 30%. **Compare the raw ratio, not a rounded percentage**, or a 14.6% share rounds to 15 and flips Act on an artifact.
- **Tooltip split** — the 70% appears only in the info icon (description, `statusGuidance`, Learn More); the derived allowance appears only in the RAG `tipText`, computed live for that team. The two numbers must never share a tooltip. `statusGuidance` therefore carries no figures, matching the wording style of the existing entries in `widgetInfoMetadata.ts`.
- **View data** — every in-progress item with its risk, not only the at-risk ones. Same item set as the WIP card, so the existing dialog config is reusable.
- **The Epic's Release Notes copy**, rewritten once here for the whole round (D29): Epic #4127's description still promises risk zones and an at-risk chip, both removed before release.

## Open question for DESIGN — CLOSED

**Resolved during DISCUSS, 2026-09-19. The maintainer is the confirmer, directly.**

The question was who had confirmed *"Observe is unreachable at low WIP"* as intended, and against
which team's WIP — the ADO description asserted the confirmation without attributing it. Benjamin
Huser-Berta confirmed it in as many words and declined to re-open the rule. With a 15% allowance one
at-risk item out of six is 16.7% and lands on Act; Observe needs a WIP of seven or more. **That is
accepted, not a gap to close.** This slice implements the rule as the story states it.

Recorded here rather than left as an open question, because a question that is answered elsewhere and
still reads as open is one the next reader asks again. The resolution is also in the feature delta's
DISCUSS section.

## OUT of scope

- The risk arithmetic — slice 02.
- Dialog width — slice 03.
- Any portfolio surface. Unchanged from round 1 D4.

## Dependencies

**Upstream: slice 02 blocks this**, in the story's own words. The 70% rule counts items that today return null and after slice 02 return 100. **Slice 03** should land first so this slice's View Data call site lands on an already-swept pattern.

## Watch-outs

- **Verify the widget on demo data rather than asserting it.** The demo teams carry 85% @ 7 days as of round 1 — but "the demo teams already carry an SLE" was asserted in three round-1 checklists and was false at the time. Open the demo instance and look.
- **RTL name matchers are unanchored** — a `/at risk/i` matcher also matches "not at risk". Use `toHaveAccessibleName` for the widget's status.
- **`ragRules.ts` is shared.** Extend the existing test factory before editing it, and grep for the rules' callers to bound the blast radius.
- **Website marketing surface** — re-check here. A new widget is the kind of thing that warrants a marketing screenshot; slice 01 cleared the site of any SLE-risk dependency, so anything added now is additive.

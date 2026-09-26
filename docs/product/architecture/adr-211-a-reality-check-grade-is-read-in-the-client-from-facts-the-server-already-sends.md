# ADR-211: A reality-check grade is read in the client from facts the server already sends; whether a forecast held stays the server's

**Status**: Accepted (2026-09-26 — Morgan, DESIGN wave, interaction mode PROPOSE, maintainer AFK; taken
within the scope DISCUSS delegated as P-5, because it changes nothing a user sees).

**Feature**: `epic-4172-forecast-backtest-sweep`, Story #6094 — the graded results dialog (decision
6094-DES-1 in `docs/feature/epic-4172-forecast-backtest-sweep/feature-delta.md`).

**Decider**: Morgan (Solution Architect)

---

## Context

Story #6094 grades every forecast in the reality check on Nick Brown's scale (*The Full Monte*): whether it
held, and how close it landed as a percentage of the actual, banded at ≤ 10%, > 10-25% and > 25%, shown as
one of six shades beside the signed miss in Work Items. Each level's line adds "within 10% in N" and, when one
grade covers more than half of that level's checks, a "usually …" clause.

Every input already travels on the shipped response: `forecastValue`, `held` per level outcome, and
`actualCompleted` per evaluable cell. *Held* is decided by `RealityCheckVerdictPolicy.Held` on the server
(ADR-210). The closeness band is a new rule, and nothing on the server uses it.

One acceptance criterion constrains where the rule can live: the whole-number percentage a user sees must
never contradict its band (a true 10.3% shown as "10%" in the 10-25% band). The percentage is necessarily
formatted in the browser.

## Decision

**The margin, the band, the grade, the shown percentage and the per-level closeness counts are computed by
one pure, string-free TypeScript module in the frontend, over the shipped fields. The client does not
recompute *held*: the grade's hue is the wire's `held`.** No field is added to the wire for grading.

## Alternatives considered

1. **Backend closed-enum fields** (`grade` per level outcome; `withinTenPercentCount` and `usualGrade` per
   level). Rejected. Four new wire members, each a C# enum, a TypeScript union and a zod enum; enums travel as
   strings, so a member spelled differently on each side compiles on both and fails only on a real parse,
   which the hand-built frontend fixture never performs. And the band would be decided in C# while the
   percentage it must agree with is formatted in TypeScript — the invariant would span two tested ends and
   live in the seam between them.
2. **Hybrid: band on the server, percentage in the client.** Rejected for the same seam, with less reason.

## Consequences

- **Positive**: the band and its shown percentage are decided in one function, so the no-contradiction rule
  is a single exhaustive unit test. No wire change, no enum mirroring. The deferred client-side one-pager
  (slice 03) reuses the same module. ADR-210's *held* stays single-sourced.
- **Negative**: a consumer that is not this browser client — an MCP tool, the CLI, a server-side PDF
  renderer — would have to re-implement the band. Today none exists (Epic decision D12), and any such
  consumer must already compose every sentence itself because the response carries facts, never sentences.
- **Revisit trigger**: the first non-browser consumer of the grade. The move is contained: the pure module
  becomes a pure static policy beside `RealityCheckVerdictPolicy`, plus additive response fields. No
  migration, no stored state (ADR-209).
- **Precedent**: the Epic already keeps the client from re-deriving a *backend* rule (`minimumActiveDays`,
  `unevaluatedWindowDays` are echoed for that reason). This ADR draws the same line from the other side: a
  rule only a screen needs is not moved onto the server to sit beside the ones it does need.

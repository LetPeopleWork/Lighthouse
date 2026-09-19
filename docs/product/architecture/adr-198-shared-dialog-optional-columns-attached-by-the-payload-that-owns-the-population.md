# ADR-198: A Shared Dialog's Optional Columns Are Attached by the Payload That Owns the Population, Never Decided Per Call Site

**Status**: Accepted (2026-09-19 — Morgan, DESIGN wave, interaction mode PROPOSE). No code implements it yet; `epic-4127-sle-risk-corrections` slice 03 (ADO User Story #6035) is the first commit that will.
**Date**: 2026-09-19
**Feature**: epic-4127-sle-risk-corrections (ADO User Story #6035)
**Decider**: Morgan (Solution Architect)

---

## Context

`WorkItemsDialog` (`Lighthouse.Frontend/src/components/Common/WorkItemsDialog/WorkItemsDialog.tsx`) is
the one list of work items in the product. Sixteen production render sites open it: ten charts on a
data-point click, `WidgetShell` for every widget's View Data, the metrics view's cumulative-state
drill-down, three Feature work-item lists, and one component that is no longer reachable.

It has grown four optional column descriptors — a highlight column, a time-in-state column, a pace-band
column (ADR-188) and an SLE Risk column (ADR-192) — and the feature that prompted this ADR observes
that it will grow more. Each is an optional prop. A caller that wants the column passes a descriptor; a
caller that does not, omits it. Nothing in the type system, the compiler, Biome or any test observes
the omission.

Story #6035 was raised because clicking a bubble on the Work Item Aging chart opened a dialog with no
SLE Risk column, while the same team's View Data list had one. Enumerating all sixteen call sites
during DESIGN found the reported miss and **two more nobody had reported**: `buildViewData`
(`pages/Common/MetricsView/BaseMetricsView.tsx`) hands the identical array — `inputs.inProgressItems` —
to four widget payloads, and attaches the risk descriptor to two of them. `totalWorkItemAge` and
`workItemAgePercentiles` list the same items, for the same team, on the same day, without the column.

So the question this ADR settles is not "how do we fix three call sites". It is: **what shape stops a
fifth one from happening, given that most callers legitimately have no such column to show?**

Three facts bound the answer.

First, the omission is correct for thirteen of the sixteen. A Feature's child items span several teams
and several targets; a run chart's drill-down lists a past day's population against a number that is an
as-of-today statement; a closed-item list has no chance of missing a target because it either did or
did not. These are not oversights and a design that forces each of them to announce its absence is
paying sixteen times for one bug.

Second, the callers are not peers. One of them — `WidgetShell` — is a single render site fronting more
than twenty payloads built in one pure function, `buildViewData`. The decision on that path is made
twenty times in one place, and it was there that it went missing four times. The other fifteen each own
exactly one population, and for fourteen of them the answer is a fixed "no".

Third, `WorkItemsDialog` deliberately knows nothing about cycle times, targets or teams. ADR-188
established that shape for the pace-band column and slice 02 of this feature restated it for the risk
column: the dialog takes finished answers from a descriptor and never learns the domain behind them. A
dialog that fetched its own risk would need a team context on the Portfolio detail page, the Team
Feature list and the delivery grid, none of which has one.

## Decision

**Keep the optional descriptor props. Delete the repetition of the decision, and enforce the invariant
where the population is enumerable.**

Three moves, in descending order of strength:

1. **Where one function owns many payloads, the decision is made once, in a named base that carries the
   population.** `buildViewData` grows an `inFlight` object literal holding the in-flight items, the
   age highlight and the risk descriptor. The four payloads that list today's WIP spread it and add
   only what differs — a title, a time-in-state column, a band column. The population and the column
   that belongs to it are the same three characters apart, so they cannot be written separately.

2. **Where a component owns exactly one population and has exactly one caller, the descriptor's inputs
   are a required prop.** `WorkItemAgingChart` plots today's in-flight items and renders its own
   dialog on a bubble click. `sleRiskValues` becomes required on its props, so the one call site is a
   compile error until it passes them — which is the strongest instrument available and costs one line
   because there is one caller. An empty array remains the honest value for a team with no published
   target; the descriptor factory already answers that with no column.

3. **Where the payloads are a finite, inspectable value, the invariant is an exhaustive partition of
   their keys.** `buildViewData` is exported and a Vitest test names two sets of payload keys — those
   that must carry a risk descriptor and those that must deliberately not — then asserts that each set
   holds, **and that their union is exactly the record's key set**. A payload key nobody has written
   yet fails the test because the test does not know about it, so the author must put it in one list or
   the other. The decision is forced once per payload, which is where the misses were, rather than once
   per call site, which is where they were not.

   The partition deliberately inspects **no property of the items**. An earlier draft of this decision
   keyed the predicate on referential identity of the in-flight array; peer review observed that this
   is Option D's mechanism wearing a test's clothes, and that an author writing
   `items: [...inputs.inProgressItems]` defeats it exactly as silently in a test as in a wrapper. That
   is correct, and it is why the partition is over key names alone.

**The three are deliberately not one mechanism, and the split is the point.** Move 1 removes the
opportunity; move 2 removes the possibility on the one path where removal is cheap; move 3 catches what
survives both. A bypass of any one is caught by at least one of the others: hand-writing a fifth
payload defeats move 1 and is caught by move 3; adding a second caller to the chart defeats nothing,
because move 2 is a type.

**What this does not claim.** Move 1 is a convention; move 3 is what makes it safe to rely on, and it
covers a hand-written payload as readily as a spread one because it never looks at the items. What
neither covers is **a new component with its own dialog and an in-flight population** — a seventeenth
call site, outside `buildViewData` and outside the aging chart. The instrument for that is the sweep: a
table in the feature's DESIGN record naming every call site and, for each "no", the reason, read by a
person at review. That is weaker than a test and it is written down as weaker rather than implied to be
equivalent.

**Why the invariant is testable here when ADR-188's sibling was not.** ADR-188 records, of its own
enforcement: *"Not enforced by a test — the agreement property compares outputs, so a correct
re-implementation passes it… The shared type and code review are the whole mechanism here"*, and notes
that gating the call would need an import-level or lint rule the repository does not have for
TypeScript. That is correct for its question, which is *did you call the shared function* — a question
about a program's text, which a program cannot ask about itself. This ADR's question is *does this
finite record's key set match a stated partition*, which it can. Same family of worry, different
instrument, because the shape of the guarded thing is different.

## Alternatives Considered

**Option A — Make the descriptor props required, with an explicit "nothing to show" value.**

- Pros: the compiler enforces it. Every call site must state a position, and a new call site cannot be
  written without deciding. This is the only option that makes the omission structurally impossible
  rather than merely unlikely.
- Cons: thirteen of sixteen call sites would carry a ceremonial declaration of absence for a column they
  could never show. The same argument applies with identical force to the three other optional
  descriptors, none of which is being made required — so the rule would exist because one column
  happened to have a bug, which is an asymmetry a future reader cannot justify and will eventually
  "tidy". And it buys little where the bug actually occurred: all four payloads sit in one function,
  within seventy lines of each other, and a required prop there is satisfied by typing the absence
  value as readily as by typing the descriptor. Move 3 forces the same decision at the same place and
  charges nothing to the other fifteen call sites.
- **Rejected**, and it is the closest call in this ADR. The deciding fact is that the enforcement it
  buys is weakest exactly where the defect was, and the cost it charges is heaviest exactly where there
  was never a defect.

**Option B — `WorkItemsDialog` derives or fetches the risk itself.**

- Pros: one place, no prop, no call-site decision at all.
- Cons: the dialog is rendered on surfaces with no team context and no metrics fetch — the Portfolio
  detail page, the Team Feature list, the delivery grid — so it would need either a provider wired
  through all of them or a fetch per dialog instance, sixteen times over. It also destroys the property
  ADR-188 and ADR-192 both rely on: the dialog takes finished answers and never learns what a cycle
  time is. A dialog that knows about SLE targets is a dialog that will next be asked to know about
  forecasts.
- **Rejected** as a larger architectural regression than the defect it cures.

**Option C — Fix the three call sites and add the sweep table, with no shape change.**

- Pros: the smallest possible diff. The three misses are real and fixing them is what the story asked
  for.
- Cons: it leaves the generator intact. The story's own learning hypothesis says the design needs
  changing if the class of miss survives the sweep — and the sweep found *three* instances of it, two
  unreported, which is evidence that the repetition produces them faster than bug reports find them.
  The next column added to this dialog would start the count again.
- **Rejected.** A fix that leaves the next author able to make the same mistake is a weaker outcome
  than one that cannot be made.

**Option D — Key the decision off referential identity of the items array, applied by a wrapper.**

- Pros: fully automatic. Any payload passing the in-flight array gets the column without anyone
  deciding.
- Cons: referential identity as the carrier of a semantic ("this is the in-flight population") is
  invisible to a reader, survives only as long as nobody writes `[...inputs.inProgressItems]`, and
  fails silently and totally when someone does. It converts an explicit decision into an implicit one
  that is harder to audit than the problem it solves.
- **Rejected**, and the rejection is load-bearing: move 3 was redrafted during peer review precisely so
  that it does not rely on this mechanism either. An identity check is no more honest inside a test
  than inside a wrapper, which is why the partition is over key names and inspects no item at all.

## Consequences

**Positive**

- The decision "does this list carry an SLE Risk column" existed in four places inside one function and
  now exists in one. Three surfaces gain a column they should always have had.
- A payload added later that lists today's in-flight work fails a test rather than shipping a gap.
- The aging chart's dialog cannot be constructed without its risks, enforced by the compiler.
- Every call site is enumerated with its reason, which is a map the next author did not have.
- The dialog's ignorance of the domain is preserved, so ADR-188's and ADR-192's shapes are unchanged.

**Negative**

- Move 3 is scoped to `buildViewData`. A new component rendering its own dialog over an in-flight
  population is covered by none of the three moves and remains a review-time concern, named rather than
  papered over.
- The partition must be maintained: a payload renamed rather than added fails the test, correctly but
  noisily, and the author has to edit a list in a test file. That is the price of the third assertion
  and it is paid deliberately.
- `buildViewData` becomes exported to be testable. It is a pure function with no other consumer, and
  the export exists for the test.

**Neutral**

- No route, no persistence, no premium gate, no RBAC surface and no dependency is touched. The decision
  is entirely about a frontend view-model's shape.
- The three other optional descriptors keep their current shape. This ADR states a rule about *where a
  decision lives*, not a rule that every optional column must be required.

## Architectural Enforcement

| Rule | Mechanism |
|---|---|
| Every `buildViewData` payload listing today's in-flight items carries the SLE Risk descriptor, and every other payload deliberately does not | Vitest naming both key sets and asserting that their union is exactly `Object.keys(buildViewData(…))`. A payload key not in either set fails the test because the test does not know about it — no property of the items is inspected, so nothing about how they were built can spoof it |
| The aging chart's bubble-click dialog cannot be built without the risks | The prop type. `sleRiskValues` is required on `WorkItemAgingChartProps`; the single call site is a compile error until it passes them |
| The population and its column are written together | The `inFlight` base literal in `buildViewData`. A convention, and recorded as one — the test above is what makes it safe to rely on |
| Every `WorkItemsDialog` render site is accounted for | The sweep table in `docs/feature/epic-4127-sle-risk-corrections/feature-delta.md`, *DESIGN — slice 03*: one row per call site, with the reason written for each deliberate "no". A review gate read by a person |
| The dialog learns nothing about the domain behind a column | The prop types. Every descriptor is a set of finished answers; the dialog imports no metrics service, no team and no terminology for these columns |

## Cross-feature impact

- **ADR-188** (one pace-band ladder, read by both the chart's geometry and the dialog's cell): unchanged
  and not superseded. ADR-188 settles where a *rule* lives once two surfaces need it; this ADR settles
  how a *column* reaches a shared dialog. ADR-188's enforcement note — that its own "did you call the
  shared function" invariant has no available instrument in this repository — is quoted above as the
  reason this ADR's instrument is different rather than as a precedent for not having one.
- **ADR-192** (SLE Risk as a pure conditional over the cycle-time population) and **ADR-194** (a number
  per item, never a background ladder): unchanged. Neither is about the dialog's prop surface, which is
  precisely why this decision needs an entry of its own rather than a third amendment to ADR-192.
- **ADR-018** (no shared per-state aggregation service): not reached, and worth saying so. That chain
  refuses to share a mechanism between two consumers with deliberately different membership semantics.
  Here nothing is shared that was not already shared — one descriptor factory, already in one place —
  and what changes is only where the decision to use it is made.

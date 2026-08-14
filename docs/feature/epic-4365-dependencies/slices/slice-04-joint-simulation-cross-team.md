# Slice 04 — Dependencies that cross teams count too (premium)

**Feature**: epic-4365-dependencies · **ADO**: Epic #4365 · **Stories**: US-07 `@infrastructure`
(precursor commit), US-08 · **Estimate**: ~6h **if** OQ-2 comes back cheap — see the probe below
**Reference class**: none. This is the highest-risk slice in the epic and the only one whose estimate
is conditional.

## Goal

The warning that said "waits on another team's Feature — not included in the forecast" disappears,
because the date now sits behind that Feature.

## IN scope

**Precursor commit (US-07, `@infrastructure`, lands first and alone):**

- `RunMonteCarloSimulation` stops grouping into one `Task.Run` per team. One trial advances a shared
  day clock; on each simulated day every team with throughput draws its own throughput and consumes
  from its own `SimulationResult` rows.
- Concurrency moves from per-team to per-trial (or trial batches — OQ-2).
- A team with no throughput stays excluded exactly as today (AC-7.3).
- Verification is a fixed-seed equality assertion against the pre-change run (AC-7.1) plus a wall-clock
  bound (AC-7.2). It ships no user-visible behaviour **by design** — that is the correctness argument,
  not a gap.

**The story commit (US-08):**

- Cross-team edges become honourable: the eligibility rule from slice 03 now sees every team's rows
  within the trial, so a blocker on team Y constrains a dependent on team X.
- Slice 03's cross-team warning is deleted for honoured edges (AC-8.2).
- D7 and D8 extended to cross-team: a cross-team cycle is warned, a cross-team blocker on a
  throughput-less team is dropped and warned (AC-8.4, AC-8.5).
- Throughput stays per-team. A joint clock shares **time**, never throughput (AC-8.3) — this is the
  single most likely place to introduce a silent forecasting bug.

## OUT of scope

- Any change to the eligibility rule itself. Slice 03 wrote it; this slice widens what it can see.
- Cross-Portfolio honouring (D6, permanently out for this epic).
- Jira and Linear (slice 05).

## Learning hypothesis

**Disproves** "the joint simulation is affordable" **if** forecast wall-clock time on the dogfood
instance's full Feature set exceeds the multiple DESIGN sets in OQ-2. Today the work is spread across
one task per team and each task ends as soon as *its* team is done; jointly, every trial runs until
the **slowest** team finishes, and per-trial concurrency has a different contention profile.

If it fails, either concurrency moves to trial batches, or the joint clock is restricted to the teams
actually connected by a dependency — a smaller joint set per run, which is more code but bounded work.
Either way the fallback is known before the slice starts, which is why the probe comes first.

**Confirms**, if it holds, that the epic's central promise is delivered and KPI-7 is measurable.

## Verify the premise first (2h probe, before any code change — REQUIRED)

Prototype the joint loop behind the existing one on `:5169` and measure: wall-clock for the full
Feature set, and a fixed-seed percentile diff against the current implementation. **Two outputs**: the
number AC-7.2 asserts, and a go/no-go on the slice's shape. If the probe says the restructure is
larger than a day, this brief is re-cut before dispatch rather than run over — DoR item 5 records this
slice as the epic's one conditional estimate.

## Acceptance criteria

AC-7.1 … AC-7.3 (precursor) and AC-8.1 … AC-8.6 verbatim from `feature-delta.md`. The three that carry
the slice:

- Fixed-seed percentiles match the pre-change run with no cross-team dependency present (AC-7.1).
- Team X's Feature B never completes before team Y's Feature A within a trial (AC-8.1).
- Team X's throughput is still drawn from team X's own history (AC-8.3).

## Dependencies

Slice 03's eligibility rule confirmed on real data — reversing the order would mean debugging a new
simulation loop and a new eligibility rule simultaneously. The cross-team Predecessor link created in
ADO alongside slice 03's shapes. Premium licence. A recorded pre-change forecast wall-clock baseline
and a fixed-seed percentile snapshot, both captured **before** the precursor commit.

## Dogfood moment

Same day: re-run the forecast on `:5169` and diff every percentile against the snapshot. Record both
the timing and the count of honoured-vs-detected edges (KPI-7) in this brief.

## Commit gate

**No commit without the maintainer's explicit approval** — the precursor commit especially. It changes
the loop every date in the product comes from and is, by design, invisible in the output.

## Learning hypothesis verdict

_Not yet run._

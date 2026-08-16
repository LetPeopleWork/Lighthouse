# ADR-156: A multi-team feature's completion day is observed per trial, not multiplied afterwards — ADR-110's product of CDFs is superseded because dependencies break its independence assumption

- **Status**: **Deferred** (maintainer, 2026-08-14). ADR-110 stands unchanged and
  `JointCompletionDistribution` is kept. Two reasons, in order of weight:

  1. **This ADR's original premise was wrong in its direction.** It argued the product of CDFs becomes
     an *over*-estimate under correlation, and therefore optimistic. It is the opposite. For
     positively dependent variables the true joint CDF is **greater than or equal to** the product of
     the marginals, so the product *under*-states P(all done by d) and the reported date lands
     **later** than the truth. The bias is conservative, not optimistic — the safe direction, and the
     opposite of the defect ADR-110 exists to fix. Corrected in the Context below.
  2. **One change to forecasting at a time** (maintainer). This epic already rewrites the simulation
     loop; also replacing the aggregation would mean two changes to the core forecast in one release,
     and this ADR's own commit is the only one in the epic that moves a date on a Feature with no
     dependency at all.

  Kept rather than withdrawn: the Decision and its costing remain sound on their own merits, and the
  door ADR-110 left open is still open. Revisit if the conservative residual below is ever measured to
  matter, or if per-trial max is wanted as a simplification in its own right.
- **Date**: 2026-08-14
- **Feature**: epic-5792-dependency-aware-forecasting (ADO Epic #5792, slice 02) — recorded 2026-08-14
  under epic-4365-dependencies, re-homed 2026-08-16 when the forecasting half became its own epic
- **Deciders**: Benjamin Huser-Berta (maintainer)

## Context

ADR-110 computes a multi-team Feature's forecast as the product of its contributing teams' empirical
CDFs, `CDF_f(d) = ∏ᵢ CDFᵢ(d)`. It says plainly why that is allowed:

> **Independence is now explicit.** `∏ᵢ CDFᵢ(d)` is exact only if team completion times are
> independent. The simulation already models them that way, so this change makes the *reported number*
> consistent with the *model*.

**This epic breaks that premise.** Once Feature B waits on Feature A, B's contributing teams are no
longer independent: within a trial they are all held back by the same event — the day A's last row
reached zero — and then all released together.

**Corrected 2026-08-14 — the direction of the resulting bias.** An earlier draft of this ADR claimed
the product becomes an *over*-estimate and therefore optimistic. That is backwards. Writing
`d_X = d_A + x` and `d_Z = d_A + z` for two of B's teams sharing the same blocker completion `d_A`:

```
true     P(B done by d) = E_A[ F_x(d - d_A) * F_z(d - d_A) ]
product  CDF_X(d)*CDF_Z(d) = E_A[F_x(d - d_A)] * E_A[F_z(d - d_A)]
```

Both factors are non-increasing in `d_A`, so `E[fg] >= E[f]E[g]` and the true joint CDF is at least
the product — the general statement of positive quadrant dependence. A lower CDF at a given day means
a **later** date, so `∏ᵢ CDFᵢ(d)` is *under*-stated and the reported date is **pessimistic**, not
optimistic. Worked example: if each team's own part takes 10 or 20 days with equal probability and a
shared blocker adds 0 or 100 days with equal probability, the true `P(done <= 20)` is `0.5` while the
product gives `0.25`.

That is the safe direction, and it is why this ADR is deferred rather than adopted. The residual is a
bounded conservative inaccuracy affecting only Features that are **both** multi-team **and**
dependent — the intersection of two minority cases. It is documented rather than corrected.

The correlation is not confined to the dependent Feature. Excluding a waiting Feature hands its
capacity to the Features below it (the whole point of the mechanic), so a blocker's completion day
moves *other* Features' completion days on the same team, and the shared clock ties those to every
other team in the run. Trying to enumerate which Features are "correlated enough to matter" is a
correctness rule that would fail silently when it guessed wrong.

ADR-110 already named the alternative and the reason it was not taken then:

> **Per-trial max inside the Monte Carlo.** Align trial indices across teams, record each team's
> per-trial completion day, take `max` per trial. Produces an identical distribution under
> independence, and would leave room to model *correlated* teams later. **Rejected for now** — it
> rewrites the hot loop of the core forecasting path and needs trial-level storage (10 000 ints ×
> teams per feature) to buy nothing today. **Deferred, not refused: if cross-team correlation ever
> needs modelling, this is the door.**

Both stated costs are now obsolete. The hot loop is being rewritten anyway
([ADR-155](./adr-155-joint-trial-clock-replaces-per-team-simulation.md)), and trial-level storage is
not needed: with a shared clock, the maximum over a Feature's rows is a running count, not a retained
array.

## Decision

**Walk through ADR-110's door. Record a multi-team Feature's completion day directly inside the trial —
the day on which the *last* of its rows reached zero remaining — and delete
`JointCompletionDistribution`.**

Four points that are part of the decision:

1. **The per-trial max costs one integer per Feature per trial**, not `10 000 × teams`. `TrialState`
   holds an outstanding-row count per Feature; when a row reaches zero the count is decremented, and
   when the count reaches zero the current simulated day is recorded into that Feature's histogram.
   The per-team histograms are recorded exactly as today, unchanged, and stay on the per-team
   `WhenForecast` rows for the per-team surfaces.

2. **Single-team Features are byte-identical.** The max over one row *is* that row's completion day.
   Combined with ADR-154's addressable draws, a single-team Feature's histogram is unchanged to the
   bit — which matters because ADR-110's own SPIKE-00 found single-team Features are the overwhelming
   majority.

3. **A Feature that is not wholly in the run records nothing.** If a contributing team is absent from
   the run because it has no throughput, that Feature's outstanding-row count never reaches zero, so no
   completion day is recorded and its aggregate histogram is empty. That is not a special case bolted
   on; it is [ADR-112](./adr-112-unknown-forecast-when-contributor-cannot-be-forecast.md)'s unknown
   state falling out of the mechanic. ADR-112's detection rule — any contributor with
   `TotalTrials == 0` — is unchanged and continues to be the carrier.

4. **`JointCompletionDistribution` is deleted, together with its largest-remainder allocation and its
   canonical-multiplication-order handling.** Those exist to make floating-point CDF arithmetic
   deterministic and to make a scaled histogram sum to exactly the trial count. Counting observed
   completions needs neither: the histogram sums to the trial count because it is one entry per trial.
   `AggregatedWhenForecast` keeps its flag aggregation (`FilterApplied` Any, `HasSufficientData` All,
   `ExcludedSummary` distinct-join) and its null team/provenance behaviour per
   [ADR-111](./adr-111-aggregate-forecast-field-provenance.md), and is fed the recorded histogram
   instead of computing one.

**Commit placement is part of this decision.** This is the only change in the epic that moves a date on
a Feature with no dependency, and it lands as its own commit inside slice 04, **after** the joint loop
and its parallelism are proved by exact equality and **before** cross-team dependency honouring. Before,
rather than after, so that no release ever exists in which a cross-team dependency is honoured while
the aggregate is still computed under an independence assumption the dependency has just violated.

## Alternatives considered

- **Keep the product of CDFs everywhere and document the bias.** Cheapest by a wide margin, changes no
  dates, and the bias only appears where a dependency exists. **Rejected.** The epic's stated purpose
  is to stop the forecast being confidently wrong with nothing on screen to suggest it. Shipping the
  mechanic *and* a known optimistic bias in the same feature, disclosed only in a document, reproduces
  the defect one level up — the same argument ADR-112 used against the partial forecast.
- **Hybrid: product of CDFs when the run contains no honoured dependency, per-trial max when it does.**
  Preserves byte-identical multi-team dates for instances with no dependencies, forever. Genuinely
  attractive, and it is the **named fallback** if the dogfood comparison shows unacceptable movement.
  **Rejected as the primary** on three grounds: it keeps two definitions of one number alive, which is
  exactly the "two independent implementations of the same verdict" failure mode KPI-5 exists to catch;
  its selection rule is a *correctness* rule whose wrong answer is a silently optimistic date; and in
  practice almost every instance that adopts dependencies would cross to the second branch anyway, so
  it buys a permanent fork to defer a one-time move.
- **Per-Feature correlation analysis — use the product where a Feature's teams are provably
  uncorrelated, the max otherwise.** **Rejected** — the correlation closure runs through capacity
  redistribution and the shared clock, so computing it honestly is harder than simulating it, and
  computing it dishonestly is the bias with extra steps.
- **Model correlation analytically (copulas, a correlation coefficient per team pair).** **Rejected** —
  no evidence of demand, no data to fit it from, and a configurable forecast-semantics surface is a
  trust liability on the product's core output (ADR-110 rejected the configurable-strategy alternative
  for the same reason).

## Consequences

- **Positive**: the reported number and the model agree again, for the first time since a dependency
  became expressible. The aggregation is no longer an assumption layered on top of the simulation; it
  is an observation of it.
- **Positive**: a subtle numeric path leaves the codebase — floating-point CDF products with a
  canonical ordering, and an integer allocation that must be made to sum exactly. Counting is easier to
  be right about than arithmetic that must be defended against IEEE 754.
- **Multi-team Features move once, by Monte Carlo noise, on the release that lands this.** Same
  distribution, different sample. Single-team Features do not move at all. This is the one commit in
  the epic that owes a release-note line and a before/after date comparison on the dogfood instance.
- **AC-7.1's exact-equality assertion applies to commits 2 and 3 of slice 04 and stops at this one.**
  Stated so that a reviewer does not read a loosened assertion as a weakened one: it is loosened for
  exactly one commit, deliberately, and that commit is the reason.
- **`DeliveryMetricSnapshot` history shows a second one-time step** at this release boundary for
  multi-team Features, for the same reason and with the same non-remedy as ADR-110's first step:
  snapshots are forward-only (ADR-048/ADR-049) and store dates, not histograms.
- **The aggregate histogram must be persisted**, because `Feature.Forecast` is a computed property
  today and there is no longer arithmetic to compute it from. It joins `Feature.Forecasts` as the row
  whose `TeamId` is null — the shape `AggregatedWhenForecast` already declares (ADR-111). Additive,
  expand-only, generated with the `CreateMigration` script.
- **Reuse verdict**: `AggregatedWhenForecast` → **EXTEND** (flag aggregation and provenance kept; the
  distribution is supplied rather than derived). `JointCompletionDistribution` → **DELETE**.
  `CompletionHistogram` → **EXTEND (narrowed)** if its remaining members still have callers, deleted
  with the distribution otherwise. No new type: the recorder is a field on `TrialState`, which ADR-155
  creates.
- Cross-refs [ADR-110](./adr-110-multi-team-forecast-joint-probability.md) (superseded Decision; its
  Context and its two other rejections stand),
  [ADR-111](./adr-111-aggregate-forecast-field-provenance.md) (the aggregate's null team and provenance
  fields, unchanged),
  [ADR-112](./adr-112-unknown-forecast-when-contributor-cannot-be-forecast.md) (the unknown state this
  makes structural rather than filtered),
  [ADR-113](./adr-113-delivery-grain-joint-completion.md) (delivery-grain joint completion, which
  consumes the aggregate and is unaffected by how it is produced),
  [ADR-155](./adr-155-joint-trial-clock-replaces-per-team-simulation.md) (the shared clock that makes
  a per-trial max well defined).

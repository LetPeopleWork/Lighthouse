# DISTILL slice 04 — upstream issues (Story #5577)

Raised by the wave-decision reconciliation gate before any scenario was written. Neither blocks, but
both are recorded rather than silently resolved (project rule: no silent N/A).

## U-1 — The slice's own learning hypothesis is formally DISPROVEN, and was accepted anyway

`slices/slice-04-transition-history.md` states:

> **Disproves** "ServiceNow state history is affordably readable by a normal integration account"
> **if** the only viable source needs an elevated role […]

SPIKE Q8 measured `metric_instance` and `metric_definition` at **403 for every read-only role**,
opening only at `itil` / `itil_admin` / `metric_admin`. **By the brief's own terms the hypothesis is
disproven and the slice should have been cancelled.**

It was not cancelled: the maintainer ruled on 2026-07-30 to **accept the role escalation as a
documented adoption cost**, on the reasoning that throughput and forecasting still work read-only and
only time-in-state carries the `itil` ask. The learning is therefore *recorded as a negative result* —
ServiceNow history is **not** affordable on a least-privilege account — rather than treated as a
stop signal.

**This matters for US-06's viability verdict**, which is the epic's actual deliverable. The verdict
must carry this negative result, not just the fact that slice 04 shipped.

## U-2 — The metric-definition prerequisite brushes against D11 (no instance-side setup)

D11 and the out-of-scope list forbid requiring instance-side configuration. ADR-118 decision 5's
second rung tells a customer to *activate a Field value duration definition on the state field* when
none is found.

**Assessed as NOT a contradiction, for two reasons**, but it is close enough to be worth writing down:

1. **"Incident State Duration" is out-of-box and active** on a default instance — measured on the
   PDI. The default path requires no setup at all.
2. **It is a report, not a prerequisite.** The connector downgrades at runtime and the team keeps its
   throughput, forecast and (queue-inflated) cycle time. Nothing is gated behind the customer acting.
   The out-of-scope list forbids instance-side setup *"as a prerequisite for basic team metrics"* —
   time-in-state is not basic team metrics; that is slice 02, which works read-only.

If a future slice makes any metric *depend* on the customer activating a definition, this ruling does
not cover it and D11 must be re-opened.

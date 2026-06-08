# Slice 05 - Portfolio scope for named cycle times (D7)

**Type:** vertical | **Est:** ~1 day | **Stories:** US-05

## Learning hypothesis

Named cycle times extend to Portfolio scope with no new concepts - the same `CycleTimeDefinitions` on
the shared settings owner, the same Portfolio metrics endpoints (twins of the Team ones), the same
scatterplot selector and cumulative scope switch - confirming the Team build generalises cleanly to
the portfolio surface.

## What ships

- Backend: the Portfolio metrics path gets the per-definition scatter/percentile read endpoint and the
  cumulative-scope option (Portfolio twins of the Team endpoints from Slices 01/04); definitions persist
  on the Portfolio settings owner via the existing Portfolio settings write.
- Frontend: the Cycle Times config, scatterplot selector, and cumulative scope switch appear on the
  Portfolio settings and Portfolio metrics surfaces, with the same premium + config-admin (portfolio-
  admin) gating via `useRbac()`.

## IN scope

- Full feature parity at Portfolio scope: CRUD, scatterplot selector, cumulative scope, invalid-on-
  removal, sparse-series low-sample state.

## OUT of scope

- Forecasting (out of scope for the whole feature); any new analysis beyond the Team build.

## Production-data AC

- Given Portfolio Atlas, when a portfolio-admin defines "Idea to Live" (Backlog->Released) and saves,
  then it appears in the Portfolio scatterplot selector and re-plots dots over that window.
- Given the same definition, when a delivery lead turns on the cumulative scope switch on the Portfolio
  chart, then the bars recompute over the Backlog->Released span.
- Given a non-premium viewer at Portfolio scope, when they open the metrics, then the selector and
  scope switch are gated off and the Default scatterplot is unaffected.

## Taste tests

- Value-bearing: portfolio leads get the same custom-window read as team leads. PASS.
- Right-sized: scope mirror of an already-proven Team build, 3 scenarios. PASS.

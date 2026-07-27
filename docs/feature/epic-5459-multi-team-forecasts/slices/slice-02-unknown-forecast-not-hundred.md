# Slice 02 — "Cannot forecast" instead of a false 100 %

**Story**: US-02 (ADO #5570) · **Job**: `job-forecast-multi-team-joint-probability` · **Effort**: ≤ 1 day
**Blocked by**: slice-01 (the unknown rule is only reachable once the aggregate stops selecting a
single team).
**Design**: [ADR-112](../../../product/architecture/adr-112-unknown-forecast-when-contributor-cannot-be-forecast.md)
— filed as **Proposed**, not Accepted. The detection rule (`any contributor with TotalTrials == 0`),
the explicit-state requirement and the `GetLikelihood` suppression are settled; **the DTO carrier
shape is deliberately open** (nullable `LikelihoodPercentage` vs a companion `CanBeForecast` flag) and
is the first thing to settle when this slice starts, because it depends on a CLI/MCP client check that
belongs here rather than to slice-01.

This slice also **replaces** slice-01's zero-trial filter (DDD-3) rather than layering on top of it.

## Goal

When a team the feature depends on has no usable throughput, report the feature forecast as unknown —
and make sure that unknown never renders as 100 % likely.

## Context — the sharp edge (Hazard C)

`ForecastBase.GetLikelihood` (`:70-95`) ends with:

```csharp
if (trialCounter > 0) { return 100 / ((double)TotalTrials) * trialCounter; }
return 100;
```

An empty aggregate therefore reports **100 % likelihood of hitting the target date**. Today
`MaxBy` hides this (a zero-trial contributor loses the selection, `GetProbability` returns `-1`).
D3 removes that accident. If this slice only "returns an empty forecast", the visible result is the
worst possible one: maximum confidence on a feature that cannot be forecast at all. The unknown state
must be carried explicitly through to the likelihood surface.

## IN scope

- Detect the case: any contributing `WhenForecast` with `TotalTrials == 0` ⇒ feature forecast is
  unknown (D3), which the aggregate carries as explicit state rather than as an empty histogram that
  callers must interpret.
- `Feature.GetLikelhoodForDate` and `DeliveryWithLikelihoodDto.CalculateFeatureLikelihoods`
  (`:151-161`): the unknown state must not fall through to `GetLikelihood`.
- DTO carriage of the unknown state to the frontend — DESIGN picks the shape (a discriminated
  likelihood value vs. a companion flag). Additive; existing `HasSufficientData` is **not** reused
  for this, it composes with it (AC-02.4).
- Frontend: likelihood cell renders the unknown state naming the team(s), instead of a percentage.
  `ForecastInfoList.tsx:25` already tolerates an empty `forecasts` array, so the date columns need no
  guard — verify, do not assume.
- Clients (CLI/MCP): DESIGN confirms whether they render a likelihood to a human. If they do, they
  must honour the unknown state; file the follow-up task, do not block this slice.

## OUT of scope

- Changing the sufficiency threshold or any part of `forecast-minimum-data-guard` — this composes
  with it (AC-02.4).
- The joint-probability maths itself — slice-01.
- Suppressing forecasts for reasons other than a zero-trial contributor.

## Learning hypothesis

**Disproves** "an empty forecast degrades safely" — the pre-commitment this slice exists to test. If
the codebase turns out to have several independent paths that read a forecast and infer confidence
from silence (not just `GetLikelihood`), the unknown state is not a local change and DESIGN must
introduce an explicit forecast-state concept rather than a flag.

**Confirms** the scoping if the only false-confidence path is `GetLikelihood`'s `return 100`, and one
carried state closes it everywhere.

## Acceptance criteria

Full text in `feature-delta.md` US-02. Summary:

- AC-02.1 no percentile dates when any contributor has `TotalTrials == 0`.
- AC-02.2 **likelihood is not 100** in that case — reaching `return 100` is a test failure.
- AC-02.3 a feature with no remaining work still reads 100 % / Done (fact, not forecast).
- AC-02.4 `HasSufficientData` still `false`; the two signals compose.
- AC-02.5 frontend renders the unknown state without error.
- AC-02.6 the message names the team(s) that could not be forecast.

## Dependencies

- Slice-01 merged.
- A test fixture with a multi-team feature where one team has zero throughput (backend integration
  test; a demo-data variant if the E2E surface is covered).

## Gates before commit

1. `dotnet build` zero warnings, `dotnet test` green; `pnpm test` / `pnpm build` / Biome clean.
2. Mutation testing ≥ 80 % backend, ≥ 80 % frontend on the changed surface.
3. Playwright run locally.
4. **Maintainer diff review** (D8 gate 2) — no commit before it.

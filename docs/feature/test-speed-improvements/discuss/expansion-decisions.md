# DISCUSS expansion-prompt decisions — test-speed-improvements

Density resolved from `~/.nwave/global-config.json`: `mode = "lean"`, `expansion_prompt = "ask-intelligent"`.

Canonical `scripts/shared/telemetry.py:write_density_event` helper is not installed in this nWave deployment (only `~/.nwave/global-config.json`, `des-config.json`, and `hooks/` are present). Telemetry events are therefore recorded inline in this file so the audit trail survives.

## Trigger evaluation (per the ask-intelligent table)

| Trigger | Fired? | Reason |
|---|---|---|
| AC ambiguity | **No** | Every AC has a numeric threshold (`< 60 s`, `< 120 s`, `< 5 min`, `≤ 10 min`, `≥ 50 %`, `≥ 80 %`, `100 %`) or a binary gate (`coverage-map` exit code, artifact present, test name diff). |
| Cross-context complexity | **Yes** | Feature touches ≥ 3 bounded contexts (Backend test project, Frontend test project, CI workflow set) AND ≥ 3 distinct technologies (NUnit/.runsettings, Vitest, Playwright, GitHub Actions YAML). |
| Multi-stakeholder need | **No** | Single persona — `lighthouse-developer`. |
| Compliance / regulatory | **No** | No GDPR/HIPAA/SOX/audit/PII surface; CI artifacts contain only test names and durations. |
| Walking Skeleton = Configurable (D) | **No** | D2 = "No walking skeleton". |

## Scoped expansion menu (offered for the cross-context trigger)

The cross-context trigger suggested rendering `alternatives-considered`.

**First pass (initial DISCUSS run)**: declined under "work without stopping" to keep DISCUSS lean.

**Second pass (user feedback 2026-05-17 — "consider if there are other ways than splitting the CI jobs, rewriting the tests somehow ... don't narrow your view just yet")**: expansion explicitly accepted. The `[WHY] Alternatives considered (catalog)` section was rendered in `feature-delta.md`, enumerating CS-A through CS-F + combinations.

## Telemetry events (would-be `DocumentationDensityEvent` records)

Recorded as-if for downstream replay if a telemetry harness is later installed:

```json
[
  {
    "event_type": "DOCUMENTATION_DENSITY",
    "data": {
      "feature_id": "test-speed-improvements",
      "wave": "DISCUSS",
      "expansion_id": "*",
      "choice": "skip",
      "timestamp": "2026-05-17T11:00:00Z"
    },
    "note": "First pass: cross-context trigger fired; scoped menu suggested alternatives-considered; initially declined under 'work without stopping'."
  },
  {
    "event_type": "DOCUMENTATION_DENSITY",
    "data": {
      "feature_id": "test-speed-improvements",
      "wave": "DISCUSS",
      "expansion_id": "alternatives-considered",
      "choice": "expand",
      "timestamp": "2026-05-17T15:00:00Z"
    },
    "note": "Second pass: user explicitly requested 'consider other ways than splitting CI jobs, rewriting tests somehow ... don't narrow your view just yet'. Catalog rendered in feature-delta.md as [WHY] Alternatives considered (CS-A through CS-F + combinations)."
  }
]
```

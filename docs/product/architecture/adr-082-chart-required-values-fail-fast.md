# ADR-082: Chart Required-Value Validation Fails Fast and Names the Key — `values.schema.json` for Structure/Types/Unconditional-Required, `{{ required }}` for Cross-Field Conditionals

**Status**: **Accepted** (2026-06-21 — accepted by Benjamin)
**Date**: 2026-06-21
**Feature**: epic-5306-k8s-productization (ADO Epic #5306, story #5199)
**Decider**: Benjamin (product owner) + System Designer (PROPOSE)

---

## Context

US-01 requires: *"omitting a required value fails fast naming the key; no partial release"* (e.g. a missing Postgres password). Two Helm mechanisms exist: `values.schema.json` (JSON Schema validated at install/template time, names the offending key, gives editor support and type checking) and the template `{{ required "msg" .Values.x }}` function (evaluated during rendering, good for conditional/cross-field rules a flat schema cannot express). A flat schema alone cannot say "password required only when `postgresql.enabled=false`"; `required` alone gives no structural typing or IDE help.

## Decision

**Ship both, each for what it does best:**

- **`values.schema.json`** declares the chart's value structure, types, enums (`frontend.mode: embedded|split`, `database` shape), and **unconditional required** keys. Helm validates values against it before rendering and fails naming the violating path. This is the primary, declarative guard.
- **`{{ required "<key> is required when <condition>" .Values… }}`** in templates covers the **conditional** cases the schema cannot express — chiefly the database password: required when `postgresql.enabled` (bundled) **and** when `externalDatabase` is used (BYO). The bundled-DB password is **required explicitly — no auto-generated secret** (per the user's call): predictable, no hidden rotation surprise.

Both fail at `helm install`/`helm template` time → **no partial release is ever created** (the AC's "no half-broken release").

## Consequences

- **Positive**: the missing-key error names the exact key (schema path or the `required` message); typed/enumerated values catch typos and wrong shapes early; conditional rules (password-when-X) are expressible; editors that read `values.schema.json` autocomplete and validate.
- **Negative / cost**: two places encode "required" — kept disjoint (schema = unconditional/structural, `required` = conditional) so there is no double-maintenance of the same rule; the explicit-password decision means the very first install must set a password (documented in the quick-start).
- **Standalone gate**: N/A — this is chart-only input validation.

## Alternatives considered

1. **`values.schema.json` only.** Rejected — cannot express "password required only when bundled/BYO selected".
2. **`{{ required }}` only.** Rejected — no structural typing, no enum validation, no editor support; errors only surface during render, key-by-key.
3. **Auto-generate the bundled-DB password into a Secret if unset.** Rejected by the product owner — convenient but hides credentials and complicates rotation/visibility; explicit-required is preferred for a production-oriented chart.

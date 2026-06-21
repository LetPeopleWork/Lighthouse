# ADR-084: The Chart Config Reference Is Generated From `values.yaml` Comments by helm-docs (Single Source of Truth); a CI `git diff` Gate Catches Drift; Narrative Enterprise Docs Are Hand-Authored

**Status**: **Accepted** (2026-06-21 — accepted by Benjamin)
**Date**: 2026-06-21
**Feature**: epic-5306-k8s-productization (ADO Epic #5306, story #5200)
**Decider**: Benjamin (product owner) + System Designer (PROPOSE)

---

## Context

US-02 requires the enterprise docs to carry a **full config reference** in which *every documented option maps to a real chart value* (the "0 documented-but-nonexistent knobs" / "0 phantom keys" KPI), and a **drift check** that flags a stale reference when a values key is renamed — at finalization, before publish. A config reference maintained by hand inevitably drifts from `values.yaml`.

## Decision

**`values.yaml` inline comments are the single source of truth for the config reference. `helm-docs` generates the reference table from them; a CI gate fails on drift.**

- Each value in `values.yaml` carries a `# -- <description>` comment. `helm-docs` reads those + `Chart.yaml` and renders the chart README's configuration table (and the install snippet, from `Chart.yaml`).
- **Drift gate (finalization):** CI runs `helm-docs` and then `git diff --exit-code` on the generated output. If the committed reference is stale (a key renamed/added/removed without regenerating), the diff is non-empty and CI fails — drift is caught before publish. Because the table is *generated* from `values.yaml`, a documented-but-nonexistent key is impossible by construction.
- **Scope split:** helm-docs owns the **config-reference section only**. The richer narrative enterprise docs (architecture diagram, prerequisites, quick-start prose, the install→auth→MCP→scaling demo walkthrough) are **hand-authored** under `docs/` and covered by the existing per-feature docs/screenshot discipline (CLAUDE.md DELIVER checklist). The drift gate guards the generated table, not the prose.

## Consequences

- **Positive**: 0 phantom keys guaranteed by construction; renaming a value forces a regenerate (CI red otherwise); the install snippet and config table can never disagree with `Chart.yaml`/`values.yaml`; authors still write rich narrative docs freely.
- **Negative / cost**: a new tool dependency (`helm-docs`) in CI and the local toolchain; authors must keep `# --` comments accurate (but that *is* the doc now, so the incentive is aligned).
- **Standalone gate**: N/A (documentation tooling).

## Alternatives considered

1. **Hand-write the config reference + a CI script diffing documented keys vs `values.yaml` keys.** Rejected — detects drift but does not *prevent* it, gives more prose freedom at the cost of a second source of truth, and the key-diff script is its own maintenance burden. helm-docs makes drift structurally impossible for the table.
2. **Template the entire enterprise doc page from values.** Rejected — the narrative (diagram, walkthrough, prerequisites) is genuinely prose and not derivable from `values.yaml`; only the config table benefits from generation.

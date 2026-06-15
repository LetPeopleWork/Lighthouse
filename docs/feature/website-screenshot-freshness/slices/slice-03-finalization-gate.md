# Slice 03 — Finalization website-freshness gate

**Goal:** Feature finalization explicitly checks whether the website needs a screenshot refresh, so drift never re-accumulates.

## IN scope
- Add a **Website-freshness gate** item to the in-repo CLAUDE.md "DELIVER Wave — Docs & Screenshots
  at Finalization" mandate, mirroring the existing per-feature docs-screenshot discipline.
- Reference the gate from `nw-finalize` so it surfaces during finalization.
- The gate names the concrete check: changed marketed UI surface → regenerate canonical `docs/assets`
  asset (auto-served per D1/D2) → confirm the website references the correct asset path.
- The gate requires an explicit answer (including "N/A, because…") recorded in finalization output —
  it cannot be silently skipped.

## OUT scope
- Automated drift detection / CI enforcement (named as the escalation trigger, not built here).
- Any change to the screenshot pipeline or website markup (slices 01–02 own those).

## Learning hypothesis
Disproves *"a lightweight manual gate prevents re-drift"* if the next finalized UI-changing feature
still ships with a stale website image → escalate to an automated check.

## Acceptance criteria
- CLAUDE.md DELIVER mandate carries the Website-freshness gate; `nw-finalize` references it.
- The gate is recorded per-finalization (explicit "N/A, because…" allowed) — verifiably not skippable.

## Dependencies
slices 01–02 (the mechanism the gate points at must exist and be true before the gate can reference it).

## Effort / reference class
~1–2h. Reference class: the existing CLAUDE.md docs/screenshot mandate and `nw-finalize` checklist edits.

## Pre-slice SPIKE
None — documentation/process change over an already-proven mechanism.
</content>

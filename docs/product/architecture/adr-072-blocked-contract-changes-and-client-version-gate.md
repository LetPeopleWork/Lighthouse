# ADR-072: Epic-5074 Contract Changes and Client Version-Gate Matrix — `blockedRuleSet` Replaces `blockedStates`/`blockedTags` (CHANGED ⇒ Gate), Additive Read Fields (No Gate), New Over-Time Endpoint (Gate)

**Status**: Accepted (2026-06-12 — Morgan, interaction mode PROPOSE)
**Date**: 2026-06-12
**Feature**: epic-5074-blocked-items (cross-slice contract + client consequences)
**Decider**: Morgan (Solution Architect)
**Relationship to prior ADRs**: applies the ADR-055/ADR-062 client version-gate rule (additive field/param on an existing contract ⇒ NO gate; NEW route or REMOVED/CHANGED-shape field ⇒ gate, because an old server returns an opaque 404 / a missing field). Consolidates the Lighthouse-Clients consequences of ADR-067/068/069/070/071.

---

## Context

Epic 5074 touches several wire contracts across five slices. The CLAUDE.md cross-cutting rule + ADR-055/062 require each NEW endpoint or CHANGED contract wrapped by the CLI/MCP clients to be version-gated against a baseline strictly newer than the LAST RELEASED Lighthouse version. Verified last released version: **v26.6.7.1** (git tag, calver `YY.M.D.Patch`). The gate decision is per-contract: ADR-062 established that ADDITIVE fields/params need NO gate (old servers degrade gracefully), while NEW routes DO (old servers 404).

This ADR enumerates every contract change and its gate verdict so the clients-repo work is unambiguous. It does NOT edit the clients repo (separate repo).

---

## Decision — Contract Change Matrix

| # | Contract change | Slice | Shape | Old-server behaviour | Gate verdict |
|---|---|---|---|---|---|
| 1 | `blockedRuleSet` (a `WorkItemRuleSet` JSON string) **REPLACES** `blockedStates`/`blockedTags` on the team/portfolio settings DTO (`SettingsOwnerDtoBase`) | 01 | **CHANGED** (fields removed, field added) | Old server has `blockedStates`/`blockedTags` and NO `blockedRuleSet`; a new client writing `blockedRuleSet` to an old server silently loses the config (the field is ignored, the old lists stay) — a **silent data divergence**, worse than a 404 | **GATE** — `FEATURE_REQUIRES_SERVER_NEWER_THAN > v26.6.7.1`. The client must pre-check server version and refuse with "upgrade Lighthouse" before reading/writing blocked config. |
| 2 | `blockedSince : DateTime?` added to `WorkItemDto` | 02 | **ADDITIVE** | Old server omits the field; client reads null ⇒ "—" (graceful) | **NO GATE** (ADR-062 additive rule) |
| 3 | `GET .../metrics/blockedCountHistory` — NEW route | 03 | **NEW ENDPOINT** | Old server returns opaque 404 | **GATE** — `> v26.6.7.1`; client pre-checks version, fails with "upgrade Lighthouse" |
| 4 | `blockedStalenessThresholdDays : int` added to the settings DTO | 04 | **ADDITIVE** | Old server omits the field; client reads 0 (= disabled, the safe default) | **NO GATE** (additive; twin of `stalenessThresholdDays`) |
| 5 | `IsPredefined : bool` added to `AdditionalFieldDefinitionDto` (read-only distinction) | 05 | **ADDITIVE field, but CHANGED semantics for write** | Old server omits `IsPredefined` (client treats all as user-editable — acceptable READ). On WRITE, a new client must NOT send predefined fields as user-editable to an old server (which has no predefined concept) | **GATE for the predefined-field WRITE distinction** — `> v26.6.7.1`; the READ of the additive flag alone degrades gracefully, but the client's user-CRUD behaviour (excluding predefined from edit/delete) depends on the server understanding `IsPredefined`, so the client gates the predefined-aware additional-field behaviour. |

**Net**: three gated touch-points (#1 changed settings contract, #3 new endpoint, #5 predefined-field behaviour), two ungated additive fields (#2, #4). All gated entries pin `FEATURE_REQUIRES_SERVER_NEWER_THAN` strictly newer than **v26.6.7.1**; DELIVER bumps the baseline to the then-latest release at client-wrap time and records it in the clients' registry. Dev/unparseable versions are never blocked.

### Rationale for gating #1 even though it could be "additive-with-fallback"

A naive reading might keep `blockedStates`/`blockedTags` AND add `blockedRuleSet` (additive, no gate). REJECTED: the journey's HIGH-risk invariant (ADR-067) is ONE definition of blocked — the legacy lists are DELETED, not kept alongside. A client writing `blockedRuleSet` to an old server that only understands the lists would silently lose the rule config (the worst failure mode — silent, not loud). The gate makes the failure LOUD ("upgrade Lighthouse") instead of a silent divergence. This is the decisive reason #1 gates.

---

## Alternatives Considered

**Option A (chosen): per-contract gate matrix — gate the changed contract + new endpoint + predefined-write, leave additive read fields ungated.**
- Pros: minimal gating (only where an old server fails loudly OR silently diverges); matches ADR-062's proven additive-vs-new rule; clients degrade gracefully on the additive fields.
- Cons: five contracts to reason about individually. Documented in the matrix.

**Option B: gate everything (all five) uniformly.**
- Cons: over-gates the two additive fields (#2, #4) that degrade gracefully — needless "upgrade Lighthouse" friction for clients reading an old server that simply omits a nullable field. Rejected per ADR-062 (additive ⇒ no gate).

**Option C: gate nothing (treat the settings change as additive-with-fallback by keeping the legacy lists).**
- Cons: keeping the legacy lists violates ADR-067's single-definition invariant; a client write to an old server silently loses config. Rejected — the silent-divergence failure is unacceptable.

---

## Consequences

**Positive**:
- The clients-repo work is unambiguous: a table of what to wrap and which entries gate against `> v26.6.7.1`.
- Loud failure ("upgrade Lighthouse") on the changed settings contract + new endpoint; graceful degradation on additive read fields.

**Negative**:
- Three gated touch-points add version-pre-check code in the clients (idiomatic — the clients already wrap delivery-rules and version-gate per ADR-055/062).

**Neutral**:
- The `IsBlocked` BOOLEAN on `WorkItemDto` is UNCHANGED (still a computed bool) — no client impact from the rule-engine switch itself (ADR-067).

---

## Earned Trust — probing the gates

- **Silent-divergence probe (#1)**: a test (clients repo, DELIVER) asserts a new client refuses to write `blockedRuleSet` to a server ≤ v26.6.7.1 with a clear "upgrade Lighthouse" error, NOT a silent no-op.
- **Graceful-degradation probe (#2, #4)**: a client reading an old server gets null `blockedSince` ⇒ "—" and absent `blockedStalenessThresholdDays` ⇒ 0 (disabled), with no error.
- **404 probe (#3)**: the client pre-checks version before calling `blockedCountHistory`; an old server is never hit with the unknown route.
- **Dev-version probe**: an unparseable/dev server version is never blocked by any gate.

---

## Architectural Enforcement

| Rule | Mechanism |
|---|---|
| Changed settings contract (#1) gated `> v26.6.7.1` | Clients-repo registry entry + version-pre-check test (DELIVER) |
| New `blockedCountHistory` endpoint (#3) gated | Clients-repo registry entry + pre-check before call |
| Predefined-field write behaviour (#5) gated | Clients-repo registry entry |
| Additive `blockedSince` (#2), `blockedStalenessThresholdDays` (#4) NOT gated | Clients-repo: read off the existing DTO; no registry entry; graceful-degradation test |
| Baseline pinned strictly newer than the last RELEASED version, bumped at wrap time | DELIVER records the baseline in `FEATURE_REQUIRES_SERVER_NEWER_THAN` |

---

## Cross-feature impact

- ADR-055/062: applies the same additive-vs-new gate rule; this epic adds three gated touch-points.
- ADR-067 (settings contract change), ADR-069 (new endpoint), ADR-071 (predefined-field distinction): the gated contracts.
- Lighthouse-Clients (CLI + MCP): wrap the changed settings contract + the new endpoint + the predefined-field distinction; gate all three against `> v26.6.7.1`; read the additive fields ungated. Separate repo — not edited here.
